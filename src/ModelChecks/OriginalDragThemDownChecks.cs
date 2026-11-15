using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalDragThemDownChecks
{
    private const string OriginalId = "5f171aa3-e1d8-4d91-81c8-d2a05b2a2e6e";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalDragThemDownPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Drag Them Down battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        int[] ids = root.GetProperty("OriginalDragThemDownIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 7 && actions.Length == 7, "Missing positive, empty-floor or lethal original controls.");
        foreach (int index in new[] { 3, 4, 6 })
        {
            var before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId);
            Require(action.CardInstanceId == ids[index] && before.UiRngIsolated && card.DataId == OriginalId && rule.AssetKey == "DragThemDown" &&
                card.MaskDescriptor!.SourceTraits!.Count == 0 && rule.Cost == 3 && before.Energy - after.Energy == 3 &&
                rule.ExternalInteractions.Count == 0 && rule.OnCastEffects.Count == 0 && rule.Effects.Count == 1 &&
                rule.Effects[0] is { Type: DragThemDownModel.Effect, Target: "DropTargetCharacter", Value: 25, AllowEnemy: false, AllowPlayer: true,
                    Range: null } effect && effect.Statuses.Count == 1 && effect.Statuses[0].Id == "chained" && effect.Statuses[0].Stacks == 4 &&
                after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[index]),
                "Original identity, three-cost discard, friendly damage or four-chain payload differs.");
            var room = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
            var actualRoom = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
            var target = room.Units.Single(unit => unit.Id == action.TargetUnitId);
            if (index == 6)
            {
                Require(target.Health == 10 && actualRoom.Units.All(unit => unit.Id != target.Id) &&
                    after.Spawn.Train.Context!.OtherPiles!.Any(pile => pile.Name == "Exhausted" && pile.Cards.Any(token => token.InstanceId == ids[0])),
                    "Lethal original damage did not remove/return the friendly unit.");
            }
            else Require(actualRoom.Units.Single(unit => unit.Id == target.Id).Health == target.Health - 25,
                "Original must deal twenty-five damage even on an empty enemy floor.");
            var enemies = room.Units.Where(unit => unit.Team == CombatTeam.Enemy && !unit.IsPyre).ToArray();
            Require(index == 4 ? enemies.Length == 0 : enemies.Length >= (index == 3 ? 2 : 1), "Missing empty/multiple enemy controls.");
            foreach (var enemyRoom in before.Spawn.Train.Rooms)
            foreach (var enemy in enemyRoom.Units.Where(unit => unit.Team == CombatTeam.Enemy && !unit.IsPyre))
            {
                int stacks = (enemy.Status("chained")?.Stacks ?? 0) + (enemyRoom.RoomIndex == action.RoomIndex ? 4 : 0);
                int propagation = enemyRoom.RoomIndex == action.RoomIndex && stacks > 0 ? ChainedStatusModel.Damage(25, stacks) : 0;
                var current = after.Spawn.Train.Rooms.SelectMany(candidate => candidate.Units).SingleOrDefault(unit => unit.Id == enemy.Id);
                int remaining = Math.Max(0, enemy.Health + (enemy.Status("armor")?.Stacks ?? 0) - propagation);
                Require(remaining == 0 ? current == null : current != null &&
                    current.Health + (current.Status("armor")?.Stacks ?? 0) == remaining && current.MaxHealth == enemy.MaxHealth &&
                    (current.Status("chained")?.Stacks ?? 0) == stacks,
                    "Native chained propagation differs: room " + enemyRoom.RoomIndex + ", card " + index + ", enemy " + enemy.Id);
            }
            Require(Json(before.Spawn.Train.Context.BattleRng) == Json(after.Spawn.Train.Context!.BattleRng),
                "Fixed damage/single-status composite consumed Battle RNG.");
            foreach (var other in before.Spawn.Train.Rooms.Where(candidate => candidate.RoomIndex != action.RoomIndex))
                Require(Json(other.Units.Where(unit => unit.Team == CombatTeam.Player)) ==
                    Json(after.Spawn.Train.Rooms.Single(candidate => candidate.RoomIndex == other.RoomIndex).Units.Where(unit => unit.Team == CombatTeam.Player)),
                    "Composite affected a friendly unit on another floor.");
        }
        RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root);
        Console.WriteLine("ORIGINAL-DRAG-THEM-DOWN PASS: unchanged card; chains precede friendly damage, same-floor propagation and off-floor control, populated/empty floor and lethal friendly return; full winning chain/16 branches, complete successors/RNG and serial/parallel search.");
    }
}
