using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossTrialChecks
{
    internal static string Boss(string variant)
    {
        Require(variant is "AllyBuffer" or "Fragile" or "Corrupter", "Unknown original trial variant.");
        return "Boss_Level8_" + variant + "_TitanTrial";
    }

    internal static void Control(FixtureValue battle, string variant)
    {
        string boss = Boss(variant);
        FixtureValue control = battle.GetProperty("OriginalOuterBossRoute");
        FixtureValue trial = control.GetProperty("TitanTrial");
        FixtureValue intro = battle.GetProperty("OriginalOuterBossTrialIntro");
        Require(control.GetProperty("Kind").GetString() == "SelectOriginalOuterBossScenario" &&
            control.GetProperty("Boss").GetString() == boss && control.GetProperty("Distance").GetInt32() == 7 &&
            control.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
            control.GetProperty("RngBefore").ContentEquals(control.GetProperty("RngAfter")) &&
            !trial.GetProperty("EnabledBefore").GetBoolean() && trial.GetProperty("RequestedEnabled").GetBoolean() &&
            intro.GetProperty("Kind").GetString() == "EnableOriginalTitanTrialAtIntro" &&
            !intro.GetProperty("UiBefore").GetBoolean() && intro.GetProperty("UiAfter").GetBoolean() &&
            !intro.GetProperty("EnabledBefore").GetBoolean() && intro.GetProperty("EnabledAfter").GetBoolean() &&
            intro.GetProperty("RngBefore").EnumerateArray().Zip(intro.GetProperty("RngAfter").EnumerateArray())
                .All(pair => pair.First.GetProperty("Id").GetString() == "NonDeterministic" || pair.First.ContentEquals(pair.Second)) &&
            intro.GetProperty("ChangedRngIds").EnumerateArray().All(id => id.GetString() == "NonDeterministic") &&
            trial.GetProperty("Substitutions").EnumerateArray().Any(mapping =>
                mapping.GetProperty("Before").GetString() == "Boss_Level8_" + variant && mapping.GetProperty("After").GetString() == boss),
            "Missing explicit native trial flag, original substitution mapping or unchanged scene/RNG provenance.");
        FixtureValue first = battle.GetProperty("Actions").GetArrayLength() > 0 ? battle.GetProperty("Actions")[0] : battle.GetProperty("Turns")[0];
        BattleTurnState root = first.GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatRelicState sin = root.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == trial.GetProperty("SinDataId").GetString());
        Require(root.Spawn.Turn == 0 && root.Spawn.QueuedOuterBoss?.Boss.AssetKey == boss &&
            root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 80 &&
            sin.AssetKey == trial.GetProperty("SinAssetKey").GetString() &&
            sin.EffectTypes.SequenceEqual(trial.GetProperty("EffectTypes").EnumerateArray().Select(item => item.GetString()!)) &&
            sin.EffectTypes.Contains("RelicEffectOnSpawnHeroSubstitution"),
            "The original trial sin or resolved queued boss was not captured in the deployment root.");
    }

    internal static void Native(string variant, FixtureValue battle, FixtureValue birth, FixtureValue phases, FixtureValue controller)
    {
        string boss = Boss(variant);
        Control(battle, variant);
        if (variant == "Corrupter") OuterBossCorrupterChecks.Native(battle, birth, phases, controller, boss);
        else
        {
            OuterBossBirthChecks.Native(birth);
            OuterBossPhaseChecks.Native(phases, boss, earlyDefeat: true);
            BossActionControllerChecks.Native(controller, boss, minimumOperations: 6);
            OuterBossTurnChecks.Deployment(battle, boss, minimumTurns: 3);
        }
        CombatUnit original = birth.GetProperty("Births")[0].GetProperty("Before").Deserialize<OuterBossCreationState>()!.CreatedUnit;
        Require(original.AssetKey == boss && original.BaseAttack == (variant == "AllyBuffer" ? 35 : variant == "Fragile" ? 30 : 20) &&
            original.MaxHealth == (variant == "AllyBuffer" ? 3800 : variant == "Fragile" ? 3000 : 2800),
            "Original trial character identity or stats changed.");
        var states = battle.GetProperty("Turns").EnumerateArray()
            .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
        if (variant != "Corrupter")
        {
            EnchantmentRule aura = original.Triggers.SelectMany(trigger => trigger.Effects).Single(effect => effect.Enchantment != null).Enchantment!;
            string status = variant == "AllyBuffer" ? "debuff" : "buff";
            int stacks = variant == "AllyBuffer" ? 10 : 6;
            Require(aura.StatusPool.Single().Id == status && aura.StatusPool.Single().Stacks == stacks &&
                states.Any(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Status(status)?.Stacks == stacks)), "Missing the strengthened original trial aura and live target witness.");
        }
        else Require(original.Triggers.Single(trigger => trigger.Kind == "OnTurnBegin").Effects.Single().Action!.Statuses.Single().Stacks == 4,
            "Trial Corrupter must apply four original corruption stacks at turn begin.");
        Require(phases.GetProperty("QueueControls").GetArrayLength() == 0 && battle.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Normal trial casting calibration contains a mid-turn control or unsettled effects.");
        Console.WriteLine($"OUTER-BOSS-TRIAL PASS variant={variant}: original native trial flag/sin/substitution, exact strengthened payload, full birth/controller/phases and complete paid native chain; no natural progression or victory claimed.");
    }

    internal static void Pair(string variant, FixtureValue source, FixtureValue replay)
    {
        Control(source, variant);
        Control(replay, variant);
        OuterBossCorrupterChecks.Pair(source, replay, Boss(variant));
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
