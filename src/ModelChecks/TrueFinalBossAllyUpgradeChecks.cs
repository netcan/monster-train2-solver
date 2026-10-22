using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossAllyUpgradeChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static BattleTurnState Prepare(BattleTurnState state, int cardId, CardUpgradeModifier upgrade)
    {
        TrainCombatState train = state.Spawn.Train;
        CombatContext context = train.Context!;
        CardInstanceState card = context.FindCard(cardId)!;
        context = CardMagicPowerModel.Refresh(context, card.WithPermanentUpgrade(upgrade, false), out _);
        return TrueFinalBossLethalChecks.WithTrain(state, CardSpellModel.WithContext(train, context));
    }
    internal static void Native(FixtureValue root)
    {
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            !route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("Policy").GetString() == "units-and-junk" && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            !root.GetProperty("NativeWon").GetBoolean(), "Missing original TFB route with explicit card upgrade control.");
        FixtureValue control = root.GetProperty("TrueFinalBossAllyUpgradeControl");
        string kind = control.GetProperty("Kind").GetString()!;
        int cardId = control.GetProperty("CardId").GetInt32();
        CardUpgradeModifier upgrade = control.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!;
        Require(kind is "Health" or "Damage" && upgrade.DataId == control.GetProperty("UpgradeDataId").GetString() &&
            upgrade.AssetKey == control.GetProperty("UpgradeName").GetString() && upgrade.ExternalInteractions.Count == 0 &&
            upgrade.Statuses.Count == 0 && upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.AbilityUpgrade == null,
            "Missing original pure numeric upgrade provenance.");
        BattleTurnState before = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState prepared = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState modeledPreparation = Prepare(before, cardId, upgrade);
        Require(Json(modeledPreparation) == Json(prepared), "Explicit permanent upgrade differs: " + ModelJson.Difference(Json(modeledPreparation), Json(prepared)));
        FixtureValue actions = root.GetProperty("Actions");
        FixtureValue turns = root.GetProperty("Turns");
        Require(actions.GetArrayLength() == 1 && turns.GetArrayLength() == 6, "Expected one upgraded original Steward play and six EndTurns.");
        PlayCardAction action = actions[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == cardId && action.RoomIndex == 0 && action.PlayerPosition == 0 &&
            BattleTurnChecks.Comparable(prepared) == BattleTurnChecks.Comparable(actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!),
            "Missing paid upgraded Steward at the unchanged deployment boundary.");
        TrainCombatState summoned = actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train;
        CombatUnit ally = summoned.Rooms.SelectMany(room => room.Units).Single(unit => !unit.IsPyre);
        Require(ally.BaseAttack == (kind == "Damage" ? 18 : 8) && ally.MaxHealth == (kind == "Health" ? 45 : 25) &&
            TrueFinalBossCombatModel.SupportsAlly(ally, summoned.Context) && !TrueFinalBossCombatModel.SupportsAlly(ally),
            "Upgraded ally must have its calibrated stats and original spawner upgrade provenance.");
        CardInstanceState originalCard = summoned.Context!.FindCard(cardId)!;
        CombatContext noUpgrade = summoned.Context.WithCard(originalCard.WithModifiers(CardModifiers.Empty(), originalCard.Temporary));
        Require(!TrueFinalBossCombatModel.SupportsAlly(ally, noUpgrade), "Matching numeric stats alone must not admit an unproven ally.");
        string parent = Json(before);
        BattleTurnState Replay()
        {
            BattleActionResult played = BattleActionModel.PlayCard(Prepare(before, cardId, upgrade), action);
            Require(played.Supported, "Independent upgraded summon rejected: " + played.Reason);
            BattleTurnState nativePlay = actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(BattleTurnChecks.Comparable(played.State!) == BattleTurnChecks.Comparable(nativePlay),
                "Paid upgraded summon differs: " + ModelJson.Difference(BattleTurnChecks.Comparable(played.State!), BattleTurnChecks.Comparable(nativePlay)));
            return BattleTurnChecks.RunIndependentChain(played.State!, turns, compareRoomEnabled: true);
        }
        Replay();
        Parallel.For(0, 16, _ => Replay());
        Require(Json(before) == parent, "Independent upgraded chains mutated their original root.");
        Previews(root);
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Native upgraded-ally acceptance requires all four failure counters zero.");
        Console.WriteLine("TRUE-FINAL-BOSS-ALLY-UPGRADE PASS kind=" + kind + "; original numeric upgrade and paid Steward; six EndTurns, native UI previews and 16 independent immutable full chains.");
    }
    internal static void Previews(FixtureValue root)
    {
        FixtureValue turns = root.GetProperty("Turns");
        Require(root.GetProperty("TrueFinalBossPreviews").GetArrayLength() == 5, "Missing five native UI previews.");
        foreach (FixtureValue preview in root.GetProperty("TrueFinalBossPreviews").EnumerateArray())
        {
            TrainCombatState input = preview.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = preview.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            BattlePlayRules rules = turns.EnumerateArray().Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .First(state => state.Spawn.Turn == preview.GetProperty("Turn").GetInt32()).PlayRules!;
            TrainCombatResult result = TrueFinalBossPreviewModel.Refresh(input, rules);
            Require(result.Supported && TrueFinalBossTrainChecks.Comparable(result.State!, "Preview") == TrueFinalBossTrainChecks.Comparable(actual, "Preview"),
                "Upgraded allied UI preview differs: " + (result.Supported ? ModelJson.Difference(TrueFinalBossTrainChecks.Comparable(result.State!, "Preview"),
                    TrueFinalBossTrainChecks.Comparable(actual, "Preview")) : result.UnsupportedReason));
        }
    }
}
