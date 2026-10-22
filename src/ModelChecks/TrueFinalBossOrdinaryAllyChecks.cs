using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossOrdinaryAllyChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Native(FixtureValue root)
    {
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            !route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("Policy").GetString() == "units-and-junk" && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            !root.GetProperty("NativeWon").GetBoolean(), "Missing original TFB route with explicit added-card control.");
        FixtureValue control = root.GetProperty("TrueFinalBossOrdinaryAllyControl");
        string kind = control.GetProperty("Kind").GetString()!;
        Require(kind is "Steward" or "Warrior" or "Animus", "Unknown ordinary allied signature.");
        int cardId = control.GetProperty("CardId").GetInt32();
        int anchorId = control.GetProperty("AnchorCardId").GetInt32();
        BattleTurnState prepared = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState before = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        CardPlayRule definition = control.GetProperty("Definition").Deserialize<CardPlayRule>()!;
        Require(definition.DataId == control.GetProperty("CardDataId").GetString() && definition.SpawnUnit != null &&
            definition.Effect == "SpawnMonster" && prepared.Spawn.Turn == 0 && prepared.Spawn.QueuedTrueFinalBoss != null &&
            prepared.Energy == before.Energy && prepared.Energy == 4 && prepared.Spawn.Train.Rooms.SelectMany(room => room.Units).Single() is { IsPyre: true, Health: 80 } &&
            prepared.Spawn.Train.Context!.Cards.Hand.Count == before.Spawn.Train.Context!.Cards.Hand.Count + 1 &&
            prepared.Spawn.Train.Context.FindCard(cardId)!.DataId == definition.DataId,
            "Missing an original unit card added before the unchanged deployment boundary.");
        FixtureValue actions = root.GetProperty("Actions");
        FixtureValue turns = root.GetProperty("Turns");
        Require(actions.GetArrayLength() == 2 && turns.GetArrayLength() == 6, "Expected two paid original allies and six EndTurns.");
        BattleTurnState deployed = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatUnit ally = deployed.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == cardId);
        int attack = kind == "Steward" ? 14 : kind == "Warrior" ? 15 : 3;
        int hits = kind == "Steward" ? 1 : kind == "Warrior" ? 2 : 3;
        Require(ally.BaseAttack == attack && ally.MaxHealth == (kind == "Steward" ? 5 : 8) && ally.Size == 2 &&
            ally.AssetKey == definition.SpawnUnit!.AssetKey && TrueFinalBossCombatModel.SupportsAlly(ally, deployed.Spawn.Train.Context) &&
            !TrueFinalBossCombatModel.SupportsAlly(ally), "Missing calibrated ordinary attack signature and original source definition.");
        CardInstanceState original = deployed.Spawn.Train.Context!.FindCard(cardId)!;
        var extra = new CardUpgradeModifier("unverified-upgrade", "unverified-upgrade", new CardStatModifier(), [], false, false, false, 0, 0, []);
        CombatContext modified = deployed.Spawn.Train.Context.WithCard(original.WithPermanentUpgrade(extra, false));
        Require(!TrueFinalBossCombatModel.SupportsAlly(ally, modified), "Ordinary signatures must still reject an unverified upgrade.");
        JsonNode missingHealth = JsonNode.Parse(Json(original))!;
        missingHealth["MaskDescriptor"]!["Definition"]!["UnmodifiedSpawnHealth"] = null;
        CombatContext legacy = deployed.Spawn.Train.Context.WithCard(missingHealth.Deserialize<CardInstanceState>(ModelJson.Options)!);
        Require(!TrueFinalBossCombatModel.SupportsAlly(ally, legacy), "Missing original spawn HP metadata must retain the ordinary-ally guard.");
        Require((ally.Status("multistrike")?.Stacks ?? 0) == hits - 1, "Missing original native repeated-attack status.");
        LifeLinkContext health = turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.LifeLinks!;
        Require(health.Actors.Any(actor => !actor.OuterBoss && actor.Health == 2750 - hits * attack),
            "Ordinary native repeated attacks must reduce Savagery by the complete damage.");
        string parent = Json(prepared);
        void Replay()
        {
            BattleTurnState current = prepared;
            int index = 0;
            foreach (FixtureValue entry in actions.EnumerateArray())
            {
                PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
                Require(action.CardInstanceId == (index == 0 ? anchorId : cardId) && action.RoomIndex == 0 && action.PlayerPosition == index,
                    "Expected unchanged Steward in front and selected ordinary ally behind it.");
                Require(BattleTurnChecks.Comparable(current) == BattleTurnChecks.Comparable(entry.GetProperty("Before").Deserialize<BattleTurnState>()!),
                    "Independent ordinary-ally input differs at play " + index);
                BattleActionResult played = BattleActionModel.PlayCard(current, action);
                Require(played.Supported, "Independent ordinary summon rejected: " + played.Reason);
                current = played.State!;
                Require(BattleTurnChecks.Comparable(current) == BattleTurnChecks.Comparable(entry.GetProperty("Actual").Deserialize<BattleTurnState>()!),
                    "Independent ordinary-ally successor differs at play " + index);
                index++;
            }
            BattleTurnChecks.RunIndependentChain(current, turns, compareRoomEnabled: true);
        }
        Replay();
        Parallel.For(0, 16, _ => Replay());
        Require(Json(prepared) == parent, "Independent ordinary-ally chains mutated their prepared root.");
        TrueFinalBossAllyUpgradeChecks.Previews(root);
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Native ordinary-ally acceptance requires all four failure counters zero.");
        Console.WriteLine("TRUE-FINAL-BOSS-ORDINARY-ALLY PASS kind=" + kind + "; original inert summon/attack signature, native damage=" + hits * attack +
            "; two paid allies, six EndTurns, five UI previews and 16 independent immutable full chains.");
    }
}
