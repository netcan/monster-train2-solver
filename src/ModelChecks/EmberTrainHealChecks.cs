using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EmberTrainHealChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, bool xCost)
    {
        CombatContext context = source.Spawn.Train.Context!;
        foreach (CardInstanceState card in (context.CardInstances ?? Array.Empty<CardInstanceState>()).OrderBy(card => card.InstanceId))
        {
            CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(rule => rule.DataId == card.DataId);
            if (rule == null || !CardModifierModel.Resolve(rule, card).OnCastEffects.Any(effect => effect.Type == "HealTrainPerEmber") ||
                context.Statistics!.Value(card.InstanceId, "TimesPlayed", "ThisBattle") != 0) continue;
            if (xCost && card.MaskDescriptor!.Definition.XCost && source.Spawn.Turn < 1) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action => action.CardInstanceId == card.InstanceId);
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("ModifierScenario").GetString() == "train-heal-per-ember" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing settled native per-ember healing controls.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        var controls = new List<(int Raw, int Paid, int Healed)>();
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            var definition = before.PlayRules.Cards.Single(rule => rule.DataId == card.DataId);
            var resolved = CardModifierModel.Resolve(definition, card);
            var effects = resolved.OnCastEffects.Where(effect => effect.Type == "HealTrainPerEmber").ToArray();
            if (effects.Length == 0) continue;
            Require(effects.Length == 1 && effects[0].Value == 1 && card.MaskDescriptor?.Definition.XCost == false,
                "Unexpected original fixed-cost healing effect.");
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int hp = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
            int healed = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health - hp;
            controls.Add((card.MaskDescriptor!.BaseCost, before.Energy - after.Energy, healed));
        }
        Require(controls.SequenceEqual(new[] { (1, 1, 1), (1, 0, 1), (3, 2, 3), (1, 0, 0) }),
            "Missing native ordinary cost, zero paid cost, raw/modified cost distinction or capped healing controls: " + string.Join(",", controls));
        RecordedBattleChecks.Native(root);
        Console.WriteLine("EMBER-TRAIN-HEAL PASS: original upgrade, raw/paid costs 1/1, 1/0, 3/2 and capped 1/0; native heals 1,1,3,0; immutable full battle/RNG chains.");
    }

    internal static void XCost(FixtureValue root)
    {
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("ModifierScenario").GetString() == "train-heal-per-ember-x" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing settled original X-cost healing controls.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        var controls = new List<(bool X, int Paid, int Bonus, int Healed)>();
        var seen = new HashSet<int>();
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            CardPlayRule rule = CardModifierModel.Resolve(before.PlayRules.Cards.Single(rule => rule.DataId == card.DataId), card);
            CardActionEffect[] effects = rule.OnCastEffects.Where(effect => effect.Type == "HealTrainPerEmber").ToArray();
            if (effects.Length == 0 || !seen.Add(card.InstanceId)) continue;
            Require(effects.Length == 1 && effects[0].Value == 1 && card.MaskDescriptor != null,
                "Original per-ember upgrade metadata changed.");
            bool variable = card.MaskDescriptor!.Definition.XCost;
            Require(card.DataId == (variable ? "a54a1fa9-8cf3-4be2-a9c6-d0e50bb82c7d" : "d14a50f3-728d-43e1-87f0-ef1b013f6678"),
                "Healing controls must retain original Vent/Steward definitions.");
            BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int paid = before.Energy - after.Energy;
            int bonus = CardModifierModel.UpgradedStat(0, "XCost", true, card.Permanent, card.Temporary);
            int hp = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
            int healed = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health - hp;
            controls.Add((variable, paid, bonus, healed));
        }
        Require(controls.SequenceEqual(new[] { (false, 1, 0, 1), (false, 0, 0, 1), (true, 3, 2, 3), (true, 0, 2, 0) }),
            "X healing must use paid3/0, excluding +2 X modifiers: " + string.Join(",", controls));
        RecordedBattleChecks.Native(root);
        Console.WriteLine("EMBER-TRAIN-HEAL-X PASS: original Vent and healing upgrade, paid3/0 heal3/0 despite +2 X, complete immutable native battle/RNG chains.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
