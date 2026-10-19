using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMineCollapseChecks
{
    private const string CardDataId = "786b57d8-9790-4812-8c2a-d3463fe739d9";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = System.Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0) return null;
        if (index >= 5 && source.Spawn.Turn < 1) return null;
        if (index < 5)
        {
            int room = index < 3 ? index : 1;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == room);
        }
        int victimIndex = index == 5 ? 3 : index == 7 ? 4 : -1;
        int target = victimIndex < 0 ? 0 : source.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[victimIndex]).Id;
        var action = new PlayCardAction(ids[index], 1, playerPosition: -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalMineCollapsePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Mine Collapse witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalMineCollapseIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 8 && actions.Length == 8, "Missing protector deployments, lethal OnKill, ember sink or zero-X control.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Victim(BattleTurnState state, int cardIndex) => state.Spawn.Train.Rooms[1].Units.Single(unit => unit.SpawnerCardId == ids[cardIndex]);
        string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

        BattleTurnState before = Before(5), after = After(5);
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[5])!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == CardDataId);
        CardSourceTrait source = card.MaskDescriptor!.SourceTraits!.Single();
        ScalingDamageTrait scaling = card.DamageScalingTraits!.Single();
        CardLifecycleTrigger trigger = card.MaskDescriptor.KillTriggers!.Single();
        int positiveTarget = actions[5].GetProperty("Action").Deserialize<PlayCardAction>()!.TargetUnitId;
        Require(card.DataId == CardDataId && rule.AssetKey == "MineCollapse" && rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" &&
            rule.Effects!.Count == 1 && rule.Effects[0].Type == "Damage" && rule.Effects[0].Target == "DropTargetCharacter" &&
            rule.Effects[0].Value == 0 && rule.Effects[0].AllowEnemy && rule.Effects[0].AllowPlayer &&
            source.Type == "CardTraitScalingAddDamage" && source.Int == 3 && source.Int2 == 0 && source.Int3 == 0 && !source.Bool &&
            source.Str == "" && source.Float == 1 && source.Scaling && source.TrackedValue == "PlayedCost" && source.EntryDuration == "ThisTurn" &&
            scaling.Query.Type == "PlayedCost" && scaling.DamagePerStat == 3 && scaling.Multiplier == 1 && !scaling.AddToDamage &&
            trigger.Type == "OnKill" && trigger.Effects.SequenceEqual(new[] { "CardEffectGainEnergy" }) && trigger.Actions.Count == 1 &&
            trigger.Actions[0].Type == "GainEnergy" && trigger.Actions[0].Target == "Room" && trigger.Actions[0].Value == 2 &&
            !trigger.Actions[0].AllowEnemy && !trigger.Actions[0].AllowPlayer && CardKillTriggerModel.Supported(trigger),
            "Original Mine Collapse X-damage source trait or fixed OnKill payload differs.");
        Require(before.Spawn.Turn == 1 && before.Energy == 3 && after.Energy == 2 &&
            after.Spawn.Train.Context!.FindCard(ids[5])!.LastPlayedCost == 3 && positiveTarget == Victim(before, 3).Id &&
            Victim(before, 3).Health == 9 && !after.Spawn.Train.Rooms[1].Units.Any(unit => unit.SpawnerCardId == ids[3]) &&
            after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[5]),
            "X=3 must deal lethal nine damage and return exactly two Ember from the captured OnKill trigger.");
        foreach (int roomIndex in new[] { 0, 2 })
            Require(Json(Before(5).Spawn.Train.Rooms.Single(room => room.RoomIndex == roomIndex).Units) ==
                Json(After(5).Spawn.Train.Rooms.Single(room => room.RoomIndex == roomIndex).Units), "Mine Collapse changed a different floor.");

        BattleTurnState sinkBefore = Before(6), sinkAfter = After(6);
        CardPlayRule sinkRule = sinkBefore.PlayRules!.Cards.Single(candidate => candidate.DataId == sinkBefore.Spawn.Train.Context!.FindCard(ids[6])!.DataId);
        Require(sinkBefore.Energy == 2 && sinkAfter.Energy == 0 && sinkRule.Cost == 2 && sinkRule.CostType == "Default" &&
            sinkRule.Effects!.Count == 1 && sinkRule.Effects[0].Type == "Null" &&
            sinkAfter.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[6]),
            "The fixed-cost control must consume only the two Ember returned by the lethal OnKill.");

        BattleTurnState zeroBefore = Before(7), zeroAfter = After(7);
        CardInstanceState zeroCard = zeroBefore.Spawn.Train.Context!.FindCard(ids[7])!;
        PlayCardAction zeroAction = actions[7].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(zeroBefore.Energy == 0 && zeroAfter.Energy == 0 && zeroAfter.Spawn.Train.Context!.FindCard(ids[7])!.LastPlayedCost == 0 &&
            zeroAction.TargetUnitId == Victim(zeroBefore, 4).Id && Victim(zeroBefore, 4).Health == 9 && Victim(zeroAfter, 4).Health == 9 &&
            Json(zeroCard.MaskDescriptor!.SourceTraits) == Json(zeroAfter.Spawn.Train.Context!.FindCard(ids[7])!.MaskDescriptor!.SourceTraits) &&
            zeroAfter.Spawn.Train.Context.Cards.Discard.Any(token => token.InstanceId == ids[7]),
            "X=0 must preserve the second target and original source-trait provenance without another OnKill reward.");
        foreach (int roomIndex in new[] { 0, 2 })
            Require(Json(zeroBefore.Spawn.Train.Rooms.Single(room => room.RoomIndex == roomIndex).Units) ==
                Json(zeroAfter.Spawn.Train.Rooms.Single(room => room.RoomIndex == roomIndex).Units), "Zero-X Mine Collapse changed a different floor.");
        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-MINE-COLLAPSE PASS: unchanged X=3 lethal spell, exact +2 OnKill Ember, fixed-cost sink and X=0/no-kill control.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}
