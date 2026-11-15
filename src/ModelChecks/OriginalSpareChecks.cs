using MonsterTrain2Poju.Model;

internal static class OriginalSpareChecks
{
    private const string PainId = "36077a72-c45f-464f-b652-d88af459cbdd";
    private const string TrialId = "4fe8563d-aeb8-408a-bf4a-7ed76e8e0456";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalSparePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original OnSpare battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        int[] ids = root.GetProperty("OriginalSpareIds").Deserialize<int[]>()!;
        int[] order = root.GetProperty("OriginalSpareOrder").Deserialize<int[]>()!;
        var rows = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 18 && ids.Distinct().Count() == 18 && rows.Length == 19 && order.SequenceEqual(new[] {
            0, 1, 2, 3, 4, 5, 6, 5, 7, 8, 14, 9, 10, 11, 16, 12, 15, 13, 17 }) &&
            rows.Select(row => row.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId).SequenceEqual(order.Select(index => ids[index])),
            "Missing original casts, same-instance recall or ordered controls.");
        var definitions = root.GetProperty("OriginalSpareDefinitions").EnumerateArray().ToArray();
        Require(definitions.Length == 2, "Missing two original spell definitions.");
        foreach (var definition in definitions)
        {
            string original = definition.GetProperty("Id").GetString()!;
            var effect = definition.GetProperty("Effects").EnumerateArray().Single();
            Require(original is PainId or TrialId && definition.GetProperty("Cost").GetInt32() == 1 &&
                definition.GetProperty("Traits").GetArrayLength() == 0 &&
                effect.GetProperty("Type").GetString() == "CardEffectDamage" && effect.GetProperty("Target").GetString() == "DropTargetCharacter" &&
                effect.GetProperty("Team").GetString() == "Heroes, Monsters" && effect.GetProperty("Int").GetInt32() == (original == PainId ? 5 : 10) &&
                definition.GetProperty("AssociatedCharacterTrigger").ValueKind == MonsterTrain2Poju.Fixtures.FixtureKind.Null,
                "Original cost/damage/target or native associated trigger changed.");
            var triggers = definition.GetProperty("Triggers").Deserialize<CardLifecycleTrigger[]>()!;
            Require(triggers.Length == 1 && CardSpareTriggerModel.Supported(triggers.Single()) && triggers[0].DeploymentAllowed.HasValue,
                "Missing supported original OnSpare payload or deployment rule.");
        }
        BattleTurnState Before(int index) => rows[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => rows[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit? Victim(BattleTurnState state, int source) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.SpawnerCardId == ids[source]);
        CardInstanceState Card(BattleTurnState state, int source) => state.Spawn.Train.Context!.FindCard(ids[source])!;
        int Damage(BattleTurnState state, int source) => CardModifierModel.Resolve(state.PlayRules!.Cards.Single(rule =>
            rule.DataId == Card(state, source).DataId), Card(state, source)).Effects.Single().Value;
        foreach (int index in new[] { 5, 7, 8, 9, 11, 12, 13, 15, 17, 18 })
        {
            int source = order[index]; var before = Before(index); var after = After(index); var card = Card(before, source);
            string original = source >= 10 && source <= 13 ? TrialId : PainId;
            Require(card.DataId == original && card.MaskDescriptor?.SpareTriggers?.Count == 1 &&
                before.PlayRules!.Cards.Single(rule => rule.DataId == original).HasSpareTriggers,
                "Original spell lost its owned OnSpare metadata.");
            if (original == PainId)
            {
                int growth = index == 9 ? 0 : 5;
                Require(Card(after, source).Permanent.Offsets.Damage - card.Permanent.Offsets.Damage == growth &&
                    Damage(after, source) - Damage(before, source) == growth && before.Energy - after.Energy == 1,
                    "OnSpare source growth or death suppression differs at action " + index);
            }
            else Require(after.Energy - before.Energy == (index is 12 or 17 ? 1 : -1),
                "Surviving-friendly energy gate differs at action " + index);
        }
        Require(Damage(Before(5), 5) == 5 && Damage(After(5), 5) == 10 && Damage(Before(7), 5) == 10 && Damage(After(7), 5) == 15 &&
            Victim(After(5), 1)!.Health == 45 && Victim(After(7), 1)!.Health == 35,
            "Same recalled card did not retain and accumulate its permanent damage.");
        Require(rows[6].GetProperty("Action").Deserialize<PlayCardAction>()!.RecursionSelections.Single().CardInstanceIds.SequenceEqual(new[] { ids[5] }),
            "Recall did not select the actual grown card instance.");
        Require(Victim(Before(8), 2)!.Status("undying")?.Stacks == 1 && Victim(After(8), 2)!.Health == 1 &&
            Victim(After(8), 2)!.Status("undying") == null && Victim(After(9), 3) == null,
            "Missing revived survivor / final death controls.");
        Require(Victim(Before(11), 1)!.Status("armor")?.Stacks == 20 && Victim(After(11), 1)!.Status("armor")?.Stacks == 15 &&
            Victim(After(11), 1)!.Health == 35 && Victim(After(12), 1)!.Status("armor")?.Stacks == 5 && Victim(After(15), 1) == null,
            "Missing blocked survivor and lethal partial-armor controls.");
        int enemyId = rows[13].GetProperty("Action").Deserialize<PlayCardAction>()!.TargetUnitId;
        Require(After(13).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == enemyId).Team == CombatTeam.Enemy &&
            Victim(Before(17), 2)!.Status("damage shield")?.Stacks == 1 && Victim(After(17), 2)!.Health == 1 &&
            Victim(After(17), 2)!.Status("damage shield") == null && Damage(Before(18), 17) == 0 && Damage(After(18), 17) == 5 &&
            Victim(After(18), 2)!.Health == 1, "Missing enemy gate, shield or zero-damage positive survivor controls.");
        RecordedBattleChecks.Native(root); BattleSearchChecks.FrontierDiagnostics(root); BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        Console.WriteLine("ORIGINAL-SPARE PASS: unchanged Pain Tithe/Trial By Fire, same-instance permanent growth, friendly/enemy energy gate, nonlethal/death/revived/armor/shield/zero damage, complete chain/16 branches and zero-unsupported serial/parallel search.");
    }
}
