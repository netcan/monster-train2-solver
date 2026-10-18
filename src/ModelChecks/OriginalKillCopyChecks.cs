using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalKillCopyChecks
{
    private const string OriginalId = "e48d1711-fdff-41d4-b2a0-bc8f8b2bf8dd";
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        var context = source.Spawn.Train.Context!;
        int index = Array.FindIndex(ids, id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        int cardId;
        if (index < 0)
        {
            var copy = context.CardRegistry!.Where(card => card.DataId == OriginalId && !ids.Contains(card.InstanceId)).OrderBy(card => card.InstanceId).FirstOrDefault();
            if (copy == null || context.Statistics!.Value(copy.InstanceId, "TimesPlayed", "ThisBattle") != 0) return null;
            index = ids.Length; cardId = copy.InstanceId;
        }
        else cardId = ids[index];
        if (index >= 4 && source.Spawn.Turn < 1) return null;
        int target = index is 5 or 6 or 7 or 8 ? source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[index == 8 ? 2 : index - 4]).Id :
            index is 9 or 10 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id :
            index == ids.Length ? source.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).OrderBy(unit => unit.Health).First().Id : 0;
        var action = new PlayCardAction(cardId, index is > 0 and < 4 or 5 or 6 or 7 or 8 ? 1 : 0,
            playerPosition: index == 0 ? 0 : index < 4 ? index - 1 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalKillCopyPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed Surgical Strike witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalKillCopyIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 11 && actions.Length == 12, "Missing six original plays, one generated play or controls.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Victim(BattleTurnState state, int index) => state.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[index]);
        int Stacks(CombatUnit unit, string key) => unit.Status(key)?.Stacks ?? 0;
        var generated = new List<int>();
        for (int i = 5; i < 12; i++)
        {
            var before = Before(i); var after = After(i); var context = before.Spawn.Train.Context!; var actual = after.Spawn.Train.Context!;
            int id = actions[i].GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            var card = context.FindCard(id)!; var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId);
            Require(card.DataId == OriginalId && rule.HasKillTriggers && rule.ExternalInteractions.Count == 0 && card.CanBeCopied == true &&
                before.Energy - after.Energy == CardModifierModel.Resolve(rule, card).Cost, "Original identity, cost or copyability differs.");
            var raw = card.MaskDescriptor!.SourceTraits!.Single(trait => trait.Type == CardKillTriggerModel.SuppressTrait);
            Require(raw.Bool && raw.KillTargetTeam == "Monsters" && raw.KillTargetSubtype == "SubtypesData_FunGuy" &&
                card.MaskDescriptor.SourceTraits.Any(trait => trait.Type == "CardTraitIgnoreArmor"), "Missing native team/subtype suppression or piercing metadata.");
            var trigger = card.MaskDescriptor.KillTriggers!.Single(); var copyRule = trigger.Actions.Single().Generation!;
            Require(CardKillTriggerModel.Supported(trigger) && copyRule.Pool.Count == 0 && copyRule.CopyModifiers && !copyRule.IgnoreTemporaryModifiers &&
                copyRule.Upgrade!.Stats.Damage == 0 && copyRule.Upgrade.Lifecycle!.AddedTraits.Single().RuntimeType == "CardTraitEphemeral",
                "Self-reference, modifier copying or original Ephemeral upgrade differs.");
            var added = actual.Cards.Hand.Where(token => token.InstanceId >= context.NextCardId).ToArray();
            int count = i is 8 or 10 or 11 ? 1 : 0;
            Require(added.Length == count && actual.NextCardId - context.NextCardId == count && Json(context.BattleRng) == Json(actual.BattleRng),
                "Suppressed/nonlethal copy count, identity allocation or no-roll RNG differs.");
            foreach (var token in added)
            {
                generated.Add(token.InstanceId); var child = actual.FindCard(token.InstanceId)!;
                Require(child.DataId == OriginalId && child.Temporary.Upgrades.Count(upgrade => upgrade.DataId == copyRule.Upgrade.DataId) == (i == 11 ? 2 : 1) &&
                    child.MaskDescriptor!.TemporaryTraits.Count == 0 && child.CanBeCopied == true, "Generated source identity, inherited Ephemeral upgrades or trait reset differs.");
            }
            Require(i == 11 ? actual.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Any(token => token.InstanceId == id) :
                actual.Cards.Discard.Any(token => token.InstanceId == id), "Original discard or played Ephemeral purge differs.");
            if (i == 5) Require(Victim(before, 1).Subtypes.Contains("SubtypesData_FunGuy") && !after.Spawn.Train.Rooms[1].Units.Any(unit => unit.SpawnerCardId == ids[1]), "FunGuy lethal suppression control is missing.");
            if (i == 6) Require(Victim(before, 2).Health == 8 && Victim(after, 2).Health == 4 && Stacks(Victim(after, 2), "damage shield") == 1, "Piercing must bypass, preserve shield and deal nonlethal four.");
            if (i == 7) Require(Victim(before, 3).Subtypes.Contains("SubtypesData_FunGuy") && Victim(after, 3).Health == 1 &&
                Stacks(Victim(before, 3), "undying") == 1 && Stacks(Victim(after, 3), "undying") == 0, "Revived FunGuy must remain suppressed.");
            if (i == 8) Require(!Victim(before, 2).Subtypes.Contains("SubtypesData_FunGuy") && !after.Spawn.Train.Rooms[1].Units.Any(unit => unit.SpawnerCardId == ids[2]), "Ordinary friendly lethal must still copy.");
            if (i == 9)
            {
                var old = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
                var hit = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == old.Id);
                Require(old.Health == 8 && hit.Health == 4 && Stacks(old, "armor") > 0 && Stacks(hit, "armor") == Stacks(old, "armor"), "Enemy armor piercing/nonlethal control differs.");
            }
        }
        var discard = root.GetProperty("Turns").EnumerateArray().First(row => row.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn == 1)
            .GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        Require(generated.Count == 3 && generated.All(id => discard.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Any(token => token.InstanceId == id)),
            "Played and unplayed generated Ephemeral cards must all purge.");
        var sample = Before(8); var source = sample.Spawn.Train.Context!.FindCard(ids[8])!; var d = source.MaskDescriptor!;
        var bad = d.SourceTraits!.Single(trait => trait.Type == CardKillTriggerModel.SuppressTrait);
        var unknown = new CardSourceTrait(bad.Type, bad.Int, bad.Int2, bad.Int3, bad.Bool, bad.Str, bad.Float, bad.Scaling, bad.TrackedValue, bad.EntryDuration);
        var broken = source.WithMaskDescriptor(new(d.Definition, d.BaseCost, d.BaseTraits, d.AuthoredCastEffects, d.Purified, d.PermanentGraft,
            d.TemporaryTraits, d.PermanentReplacements, d.SourceTraits.Select(trait => trait.Type == bad.Type ? unknown : trait).ToArray(), d.KillTriggers));
        Require(CardModifierModel.Resolve(sample.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId), broken).ExternalInteractions.Any(message => message.Contains("suppression")),
            "Missing team/subtype metadata was silently accepted.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-KILL-COPY PASS: six unchanged originals and one generated play, FunGuy-only suppression/revival, ordinary friendly and enemy kills, armor/shield bypass, three deterministic self-copies, inherited Ephemeral upgrades/purge and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
