using MonsterTrain2Poju.Model;

internal static class OriginalExaltationChecks
{
    private const string OriginalId = "1a692e89-3212-4dd4-ba09-072d2ddade79";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalExaltationPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Exaltation battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        int[] ids = root.GetProperty("OriginalExaltationIds").Deserialize<int[]>()!;
        var rows = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 22 && ids.Distinct().Count() == 22 && rows.Length == 22 && rows.Select(row =>
            row.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId).SequenceEqual(ids), "Missing ordered native controls.");
        BattleTurnState Before(int index) => rows[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => rows[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit? Unit(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == id);
        int Identity(int index) => After(index).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[index]).Id;
        int first = Identity(0), second = Identity(1), neighbor = Identity(2);
        var definition = root.GetProperty("OriginalExaltationDefinition");
        var raw = definition.GetProperty("Effects").EnumerateArray().Single();
        Require(definition.GetProperty("Id").GetString() == OriginalId && definition.GetProperty("Key").GetString() == "Exaltation" &&
            definition.GetProperty("Cost").GetInt32() == 2 && definition.GetProperty("Traits").GetArrayLength() == 0 &&
            raw.GetProperty("Type").GetString() == "CardEffectExaltation" && raw.GetProperty("Int").GetInt32() == 10 &&
            raw.GetProperty("AdditionalInt").GetInt32() == 9999, "Original raw spell definition changed.");
        int[] casts = { 5, 7, 9, 10, 12, 14, 17 };
        foreach (int index in casts)
        {
            var before = Before(index); var after = After(index);
            var action = rows[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId);
            Require(card.DataId == OriginalId && rule.AssetKey == "Exaltation" && rule.Cost == 2 && rule.CostType == "Default" &&
                rule.Effects.Count == 1, "Original spell identity, cost or effect count changed.");
            var effect = rule.Effects.Single();
            Require(effect.Type == ExaltationModel.Effect && effect.Value == 10 && effect.ExaltationHeal == 9999 &&
                effect.Target == "DropTargetCharacter" && effect.AllowEnemy && effect.AllowPlayer && effect.Range == null,
                "Original heal/splash definition changed.");
            Require(before.Energy - after.Energy == 2 && card.MaskDescriptor!.SourceTraits!.Count == 0,
                "Original cast did not pay two energy or acquired a definition trait.");
            var resolved = CardModifierModel.Resolve(rule, card).Effects.Single();
            Require(resolved.Value == (index == 14 ? 15 : 10) && resolved.ExaltationHeal == 9999,
                "Spell-power upgrades changed healing or failed to change splash damage.");
            CheckSplash(index, index == 14 ? 15 : 10, immune: index == 17);
        }
        void CheckSplash(int index, int damage, bool immune = false)
        {
            var before = Before(index); var after = After(index);
            var action = rows[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatUnit priorTarget = Unit(before, action.TargetUnitId)!;
            CombatUnit healed = Unit(after, action.TargetUnitId)!;
            Require(healed != null && healed.Health == (immune ? priorTarget.Health : healed.MaxHealth),
                "Selected actor was not healed / immune and excluded from splash.");
            foreach (var room in before.Spawn.Train.Rooms)
                foreach (CombatUnit prior in room.Units.Where(unit => unit.Health > 0 && !unit.IsPyre && unit.Id != action.TargetUnitId))
                {
                    CombatUnit? actual = Unit(after, prior.Id);
                    int hit = room.RoomIndex == action.RoomIndex ? damage : 0;
                    int armor = prior.Status("armor")?.Stacks ?? 0;
                    int expectedHealth = prior.Health - Math.Max(0, hit - armor);
                    Require(actual == null ? expectedHealth <= 0 : actual.Health == expectedHealth &&
                        (actual.Status("armor")?.Stacks ?? 0) == Math.Max(0, armor - hit),
                        "Splash damaged the wrong floor or used the wrong amount: " + prior.Id + " at action " + index);
                }
            Require(after.Spawn.Train.Context!.Gold - before.Spawn.Train.Context!.Gold ==
                (priorTarget.Team == CombatTeam.Player ? 5 : 0), "OnHeal callback differs at action " + index);
        }
        Require(Unit(Before(5), first)!.Health == 493 && Unit(After(5), first)!.Health == 500 &&
            Unit(After(5), second)!.Health == 490, "Missing damaged friendly heal / friendly bystander splash.");
        var enemyAction = rows[7].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(Unit(Before(7), enemyAction.TargetUnitId)!.Team == CombatTeam.Enemy &&
            Unit(Before(7), enemyAction.TargetUnitId)!.Health == 1 && Unit(After(7), enemyAction.TargetUnitId)!.Health == 400,
            "Missing positive enemy heal control.");
        Require(Unit(Before(9), second)!.Health == 1 && Unit(After(9), second) == null, "Missing lethal friendly bystander.");
        Require(Unit(Before(10), neighbor)!.Health == 500 && Unit(After(10), neighbor)!.Health == 500 &&
            Unit(Before(12), neighbor)!.Health == 493 && Unit(After(12), neighbor)!.Health == 500,
            "Missing isolated full-health and damaged target controls.");
        Require(Unit(Before(17), first)!.Health == 493 && Unit(Before(17), first)!.Status("heal immunity")?.Stacks == 1 &&
            Unit(After(17), first)!.Health == 493, "Missing positive healing-immunity control.");
        foreach (int index in new[] { 19, 20 })
        {
            CheckSplash(index, 0);
            Require(Before(index).Spawn.Train.Context!.BattleRng.Equals(After(index).Spawn.Train.Context!.BattleRng),
                "Fixed nonpositive damage unexpectedly sampled Battle RNG.");
        }
        var draw = Before(21).Spawn.Train.Context!.BattleRng.Range(4, 10);
        CheckSplash(21, (int)Math.Floor(draw.Value * 1.5f));
        Require(draw.State.Equals(After(21).Spawn.Train.Context!.BattleRng), "Range damage did not sample exactly once after healing.");
        RecordedBattleChecks.Native(root); BattleSearchChecks.FrontierDiagnostics(root); BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        Console.WriteLine("ORIGINAL-EXALTATION PASS: seven unchanged native casts; spell power, heal immunity, callbacks, zero/negative/range damage; friendly/enemy heal, mixed splash, selected-target/floor exclusion, lethal bystander, isolated full/damaged controls, complete chain/16 branches and serial/parallel search.");
    }
}
