using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPunishedSpikeChecks
{
    private const string Spike = "1f703e6f-ef57-4904-8d3f-ddd38040ad1a";
    private const string Imprison = "a037328d-1ecb-4657-b741-0909d08b08fe";
    private const string Sentence = "de1ef23c-c6bc-4d84-9df5-dac93a4b6a4f";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static IEnumerable<CombatUnit> Units(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units);
    private static int Status(CombatUnit unit, string id) => unit.Status(id)?.Stacks ?? 0;
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("OriginalPunishedSpikePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete or unexpected original Punished Spike native battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definitions = root.GetProperty("OriginalPunishedSpikeDefinitions").EnumerateArray().ToArray();
        string[] originals = { Spike, Imprison, Sentence }; int[] costs = { 0, 2, 1 };
        Require(definitions.Length == 3, "Missing three original definitions.");
        var rules = definitions.Select(row => row.GetProperty("Rule").Deserialize<CardPlayRule>()!).ToArray();
        for (int i = 0; i < 3; i++) Require(definitions[i].GetProperty("Id").GetString() == originals[i] &&
            definitions[i].GetProperty("Cost").GetInt32() == costs[i] && rules[i].ExternalInteractions.Count == 0,
            "Changed original card identity/cost/projection at " + i);
        var traits = definitions[0].GetProperty("Traits").EnumerateArray().Select(row => row.Deserialize<CardSourceTrait>()!).ToArray();
        Require(rules[0].CostType == "ConsumeRemainingEnergy" && traits.Length == 3 &&
            traits[0].Type == "CardTraitExhaustState" && traits[1] is { Type: CardEffectApplicationsModel.Trait, Int: 1,
                TrackedValue: "PlayedCost", EntryDuration: "ThisTurn", Float: 1 } &&
            traits[2] is { Type: "CardTraitScalingAddStatusEffect", Int: 8, TrackedValue: "PlayedCost", EntryDuration: "ThisTurn" } &&
            traits[2].StatusEffectIds!.SequenceEqual(new[] { "zeal" }) && rules[0].Effects.Count == 2 &&
            rules[0].Effects[0] is { Type: "AddStatus", Target: "DropTargetCharacter", AllowEnemy: true, AllowPlayer: true } &&
            rules[0].Effects[0].Statuses.Single() is { Id: "zeal", Stacks: 0 } &&
            rules[0].Effects[1] is { Type: "Damage", Target: "LastTargetedCharacters", Value: 1, AllowEnemy: true, AllowPlayer: true },
            "Punished Spike must grant eight zeal per X once, then repeat one damage per X.");
        Require(rules[1].Effects.Count == 2 && rules[1].Effects[0] is { Type: "AddStatus", Target: "DropTargetCharacter", AllowPlayer: false } &&
            rules[1].Effects[0].Statuses.Single() is { Id: "chained", Stacks: 4 } &&
            rules[1].Effects[1].Target == "LastTargetedCharacters" && rules[1].Effects[1].Statuses.Single() is { Id: "immobile", Stacks: 1 } &&
            rules[2].Effects.Single().Statuses.Single() is { Id: "chained", Stacks: 1 }, "Original chain spell sequence/status changed.");
        int[] ids = root.GetProperty("OriginalPunishedSpikeIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 22 && ids.Distinct().Count() == 22 && actions.Length == 22 &&
            actions.Select(row => row.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId).SequenceEqual(ids),
            "Missing 22 ordered native inputs.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int card) => Units(state).Single(unit => unit.SpawnerCardId == ids[card]);
        CombatUnit Guard(BattleTurnState state) => Units(state).Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
        foreach (var (i, card) in new[] { (5, 2), (6, 1) })
            Require(Before(i).Spawn.Train.Context!.FindCard(ids[i])!.DataId == originals[card] && Before(i).Energy - After(i).Energy == costs[card],
                "Original chain cast identity or cost differs.");
        Require(Status(Guard(After(5)), "chained") == 1 && Status(Guard(After(6)), "chained") == 5 &&
            Status(Guard(After(6)), "immobile") == 1, "Sentence then Imprison did not retain the same enemy target.");
        int[] casts = { 8, 10, 13, 15, 17, 19 }, xs = { 0, 1, 3, 3, 3, 3 };
        foreach (int i in casts)
        {
            int x = xs[Array.IndexOf(casts, i)]; var before = Before(i); var after = After(i);
            Require(before.Energy == x && after.Energy == 0 && before.Spawn.Train.Context!.FindCard(ids[i])!.DataId == Spike &&
                after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]),
                "X payment, original identity or consume routing differs at " + i);
            if (i == 19) continue;
            var old = i == 15 ? Guard(before) : Unit(before, i == 17 ? 2 : 1);
            var next = i == 15 ? Guard(after) : Unit(after, i == 17 ? 2 : 1);
            Require(Status(next, "zeal") - Status(old, "zeal") == 8 * x && next.MaxHealth - old.MaxHealth == 8 * x &&
                old.Health + Status(old, "armor") - next.Health - Status(next, "armor") == x,
                "Zeal must apply once without healing before X individual hits at " + i);
        }
        Require(Status(Unit(After(13), 1), "armor") == 2 && Unit(After(13), 1).Health == Unit(Before(13), 1).Health &&
            Status(Guard(Before(13)), "armor") - Status(Guard(After(13)), "armor") == 9,
            "Each armor-absorbed one-damage hit must separately chain ceil(5/2)=3.");
        Require(Status(Guard(Before(17)), "armor") == Status(Guard(After(17)), "armor") &&
            Guard(Before(17)).Health == Guard(After(17)).Health &&
            Unit(Before(17), 2).Health - Unit(After(17), 2).Health == 3,
            "A different floor's friendly hits must leave this floor's chained enemy untouched.");
        Require(Unit(Before(19), 3).Health == 4 && !Units(After(19)).Any(unit => unit.SpawnerCardId == ids[3]) &&
            Unit(After(19), 1).Health == Unit(Before(19), 1).Health,
            "Upgraded lethal first hit must not retarget the surviving friendly.");
        Require(Before(21).Spawn.Turn == 2 && Before(21).Spawn.Train.Rooms[0].Units.Any(unit => unit.Id == Guard(Before(21)).Id) &&
            Status(Guard(Before(21)), "immobile") == 1 && Status(Guard(Before(21)), "chained") == 4 &&
            Status(Guard(Before(21)), "zeal") == 23,
            "Natural EndTurn must retain Imprison's floor while decaying chained and zeal once.");
        var counts = root.GetProperty("OriginalPunishedSpikeCounts").EnumerateArray().ToArray();
        Require(counts.Length == 6 && counts.Select(row => row.GetProperty("ActualCount").GetInt32()).SequenceEqual(xs),
            "Missing zero/one/three direct native damage counts.");
        foreach (var row in counts)
        {
            int id = row.GetProperty("CardId").GetInt32(), count = row.GetProperty("ActualCount").GetInt32();
            var context = row.GetProperty("Context").Deserialize<CombatContext>()!; var card = context.FindCard(id)!;
            var descriptor = card.MaskDescriptor!;
            int amount = CardModifierModel.UpgradedStat(CardModifierModel.UpgradedStat(1, "Damage", true, card.Permanent), "Damage", true, card.Temporary);
            var damage = new CardActionEffect("Damage", "LastTargetedCharacters", amount, true, true, Array.Empty<CombatStatus>());
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            void Check()
            {
                var actual = CardEffectApplicationsModel.Count(context, id, damage);
                var status = CardEffectApplicationsModel.Count(context, id, rules[0].Effects[0]);
                Require(actual.Supported && actual.Value == count && status.Supported && status.Value == 1,
                    "Immutable direct damage/status counts differ from native.");
            }
            Check(); Parallel.For(0, 32, _ => Check());
            Require(row.GetProperty("DirectCount").GetInt32() == count && row.GetProperty("StateUnchanged").GetBoolean() &&
                context.Statistics!.PlayedCosts.Single(value => value.CardId == id).Cost == count, "Native count getter mutated its parent or ignored payment.");
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition,
                descriptor.BaseCost, descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft,
                descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            var changed = descriptor.SourceTraits!.ToArray(); var source = changed[1];
            changed[1] = new CardSourceTrait(source.Type, source.Int, source.Int2, source.Int3, source.Bool, source.Str,
                source.Float, source.Scaling, source.TrackedValue, "ThisBattle");
            Require(!CardEffectApplicationsModel.Count(context.WithCard(WithSource(null)), id, damage).Supported &&
                !CardEffectApplicationsModel.Count(context.WithCard(WithSource(changed)), id, damage).Supported &&
                !CardEffectApplicationsModel.Count(context, id, new CardActionEffect("Damage", "Room", amount, true, true, Array.Empty<CombatStatus>())).Supported,
                "Missing source, altered query or uncalibrated target escaped the guard.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Count query or branch mutated the parent.");
        }
        var applications = root.GetProperty("OriginalPunishedSpikeApplications").EnumerateArray().ToArray();
        Require(applications.Length == 13 && applications.All(row => row.GetProperty("Completed").GetBoolean() && row.GetProperty("After").ValueKind == FixtureKind.Object),
            "An individual native damage application was left incomplete.");
        int offset = 0;
        foreach (int i in casts)
        {
            int x = xs[Array.IndexOf(casts, i)];
            int target = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!.TargetUnitId;
            Require(applications.Skip(offset).Take(x).All(row => row.GetProperty("CardId").GetInt32() == ids[i] &&
                row.GetProperty("Targets").Deserialize<int[]>()!.SequenceEqual(new[] { target })),
                "A repeated damage application changed its native retained target.");
            offset += x;
        }
        foreach (var row in applications.Skip(11))
            Require(row.GetProperty("Before").ContentEquals(row.GetProperty("After")),
                "An application against the retained dead target changed the complete train.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        Console.WriteLine($"ORIGINAL-PUNISHED-SPIKE PASS: three unchanged original cards, X=0/1/3, once-only zeal, armor/enemy/remote/lethal targets, natural immobile ascent, six direct getters/32 branches each, {applications.Length} native damage applications, full chain/16 branches and zero-unsupported serial/parallel search.");
    }
}
