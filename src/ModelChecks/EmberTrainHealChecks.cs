using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EmberTrainHealChecks
{
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
        Console.WriteLine("EMBER-TRAIN-HEAL PASS: original upgrade, raw/paid costs 1/1, 1/0, 3/2 and capped 1/0; native heals 1,1,3,0; immutable full battle/RNG chains. X-cost healing remains guarded.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
