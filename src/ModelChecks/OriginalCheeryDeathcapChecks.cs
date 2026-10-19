using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalCheeryDeathcapChecks
{
    private const string CardDataId = "97260e75-4e11-439e-8eda-cd961143fc12";

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-cheery-deathcap" &&
            root.GetProperty("OriginalCheeryDeathcapPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Cheery Deathcap battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] plays = root.GetProperty("Actions").EnumerateArray().Where(entry =>
        {
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            return before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == CardDataId;
        }).ToArray();
        Require(plays.Length == 1, "Expected exactly one original Cheery Deathcap play; got " + plays.Length);

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
        CombatStatus decay = effect.Statuses.Single();

        Require(rule.AssetKey == "CheeryDeathcap" && rule.CardType == "Monster" && rule.Cost == 1 &&
            rule.CostType == "Default" && rule.Effect == "SpawnMonster" && rule.Destination == "Standby" &&
            template.AssetKey == "MonsterCheeryDeathcap" && template.BaseAttack == 5 && template.MaxHealth == 3 && template.Size == 1 &&
            template.Triggers.Count == 1 && trigger.SkipDuringDeployment != true && triggerEffect.Type == "CardEffectAddStatusEffect" &&
            effect.Type == "AddStatus" && effect.Target == "Room" && effect.AllowEnemy &&
            !effect.AllowPlayer && decay.Id == "decay" && decay.Stacks == 7 && decay.ParamInt == 3 &&
            born.AssetKey == template.AssetKey && born.Team == CombatTeam.Player && born.Size == 1 &&
            before.Energy - after.Energy == 1 && after.Spawn.Train.Context!.Statistics!.Value(play.CardInstanceId, "TimesPlayed", "ThisBattle") == 1,
            "Cheery Deathcap's unchanged identity, 1-cost deployment or OnSpawn Decay payload changed.");

        RoomCombatState actualRoom = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == play.RoomIndex);
        CombatUnit[] enemies = actualRoom.Units.Where(unit => unit.Team == CombatTeam.Enemy && !unit.IsPyre).ToArray();
        Require(enemies.Length > 0 && enemies.All(unit => unit.Status("decay")?.Stacks == 7) &&
            actualRoom.Units.Where(unit => unit.Team == CombatTeam.Player).All(unit => unit.Status("decay") == null),
            "Cheery Deathcap did not apply seven Decay to enemies only when spawned.");

        BattleActionResult replay = BattleActionModel.PlayCard(before, play);
        string? difference = replay.Supported
            ? ModelJson.Difference(BattleTurnChecks.Comparable(replay.State!), BattleTurnChecks.Comparable(after)) : replay.Reason;
        Require(difference == null, "Original Cheery Deathcap does not replay to native post-spawn state: " + difference);
        Require(BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate =>
            candidate.Action.CardInstanceId == play.CardInstanceId && candidate.Action.RoomIndex == play.RoomIndex &&
            candidate.Action.PlayerPosition == play.PlayerPosition),
            "Action search did not enumerate the native Cheery Deathcap deployment.");

        RecordedBattleChecks.Native(root);
        TrainCombatChecks.Native(root);
        BattleTurnChecks.Native(root);
        Console.WriteLine("ORIGINAL-CHEERY-DEATHCAP PASS: native spawn replayed its enemy-only seven Decay OnSpawn trigger.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
