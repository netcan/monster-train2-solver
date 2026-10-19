using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalEnthusiasticFanChecks
{
    private const string CardDataId = "adaaaa9d-56a9-4a5f-a347-ee71d4767df5";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-enthusiastic-fan" &&
            root.GetProperty("OriginalEnthusiasticFanPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Enthusiastic Fan battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] plays = root.GetProperty("Actions").EnumerateArray().Where(entry =>
        {
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            return before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == CardDataId;
        }).ToArray();
        Require(plays.Length == 1, "Expected exactly one original Enthusiastic Fan play; got " + plays.Length);

        FixtureValue entry = plays[0];
        PlayCardAction play = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardDataId);
        CombatUnit template = rule.SpawnUnit!;
        CombatUnit born = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == play.RoomIndex).Units
            .Single(unit => unit.SpawnerCardId == play.CardInstanceId);
        CombatTrigger trigger = template.Triggers.Single(item => item.Kind == "OnSpawn");
        CombatEffect triggerEffect = trigger.Effects.Single();
        CardActionEffect effect = triggerEffect.Action!;
        CombatStatus debuff = effect.Statuses.Single();

        Require(rule.AssetKey == "EnthusiasticFan" && rule.CardType == "Monster" && rule.Cost == 1 &&
            rule.CostType == "Default" && rule.Effect == "SpawnMonster" && rule.Destination == "Standby" &&
            template.AssetKey == "MonsterEnthusiasticFan" && template.BaseAttack == 5 && template.MaxHealth == 5 && template.Size == 1 &&
            template.Triggers.Count == 1 && trigger.SkipDuringDeployment != true && triggerEffect.Type == "CardEffectAddStatusEffect" &&
            effect.Type == "AddStatus" && effect.Target == "Room" && effect.AllowEnemy && !effect.AllowPlayer &&
            debuff.Id == "debuff" && debuff.Stacks == 3 && debuff.ParamInt == 2 && !debuff.RemoveWhenTriggered &&
            born.AssetKey == template.AssetKey && born.Team == CombatTeam.Player && born.Size == 1 &&
            before.Energy - after.Energy == 1 && after.Spawn.Train.Context!.Statistics!.Value(play.CardInstanceId, "TimesPlayed", "ThisBattle") == 1,
            "Enthusiastic Fan's unchanged identity, 1-cost deployment or enemy-only OnSpawn payload changed.");

        RoomCombatState actualRoom = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == play.RoomIndex);
        CombatUnit[] enemies = actualRoom.Units.Where(unit => unit.Team == CombatTeam.Enemy && !unit.IsPyre).ToArray();
        Require(enemies.Length > 0 && enemies.Any(unit => unit.Status("debuff")?.Stacks == 3) &&
            actualRoom.Units.Where(unit => unit.Team == CombatTeam.Player).All(unit => unit.Status("debuff") == null),
            "Enthusiastic Fan did not apply its Debuff to room enemies only when spawned.");

        BattleActionResult replay = BattleActionModel.PlayCard(before, play);
        string? difference = replay.Supported
            ? ModelJson.Difference(BattleTurnChecks.Comparable(replay.State!), BattleTurnChecks.Comparable(after)) : replay.Reason;
        Require(difference == null, "Original Enthusiastic Fan does not replay to native post-spawn state: " + difference);
        Require(BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate =>
            candidate.Action.CardInstanceId == play.CardInstanceId && candidate.Action.RoomIndex == play.RoomIndex &&
            candidate.Action.PlayerPosition == play.PlayerPosition),
            "Action search did not enumerate the native Enthusiastic Fan deployment.");

        RecordedBattleChecks.Native(root);
        TrainCombatChecks.Native(root);
        BattleTurnChecks.Native(root);
        Console.WriteLine("ORIGINAL-ENTHUSIASTIC-FAN PASS: native spawn replayed its enemy-only three Debuff OnSpawn trigger.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
