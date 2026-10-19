using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDeathroarChecks
{
    private const string CardDataId = "73da2bc8-68a5-41b2-a639-754c2a98b6fd";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static int[] CardIds(FixtureValue root) => root.GetProperty("Actions").EnumerateArray()
        .Select(action => action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).ToArray();

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        for (int index = 0; index < ids.Length; index++)
        {
            if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0 ||
                !context.Cards.Hand.Any(card => card.InstanceId == ids[index])) continue;
            int room = index == 2 ? 1 : 0;
            if (index == 3)
            {
                if (source.Spawn.Turn < 1) return null;
                CombatUnit? target = source.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player &&
                    !unit.IsPyre && unit.SpawnerCardId == ids[1]);
                return target == null ? null : new PlayCardAction(ids[index], room, targetUnitId: target.Id, playerPosition: -1);
            }
            int position = index == 1 ? 1 : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                play.RoomIndex == room && play.PlayerPosition == position);
        }
        return null;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalDeathroarPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-deathroar" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Missing completed original Deathroar witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int[] ids = CardIds(root);
        Require(actions.Length == 4 && ids.Length == 4, "Expected three controlled Stewards and one Deathroar play.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(Enumerable.Range(0, 3).All(index => Before(index).Spawn.Train.Context!.FindCard(ids[index])!.DataId == StewardId) &&
            Before(3).Spawn.Train.Context!.FindCard(ids[3])!.DataId == CardDataId,
            "The controlled action sequence did not play three original Stewards before Deathroar.");

        BattleTurnState before = Before(3), after = After(3);
        PlayCardAction action = actions[3].GetProperty("Action").Deserialize<PlayCardAction>()!;
        RoomCombatState roomBefore = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0);
        RoomCombatState roomAfter = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0);
        CombatUnit sourceBefore = roomBefore.Units.Single(unit => unit.SpawnerCardId == ids[0]);
        CombatUnit transferTargetBefore = roomBefore.Units.Single(unit => unit.SpawnerCardId == ids[1]);
        CombatUnit transferTargetAfter = roomAfter.Units.Single(unit => unit.Id == transferTargetBefore.Id);
        CombatUnit sourceAfter = roomAfter.Units.Single(unit => unit.Id == sourceBefore.Id);
        CombatUnit controlBefore = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.SpawnerCardId == ids[2]);
        CombatUnit controlAfter = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == controlBefore.Id);
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[3])!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(candidate => candidate.DataId == CardDataId);
        CardActionEffect transfer = rule.Effects[0];
        CardActionEffect[] damages = rule.Effects.Skip(1).ToArray();
        int Pyregel(CombatUnit unit) => unit.Status("pyregel")?.Stacks ?? 0;
        int transferredBefore = roomBefore.Units.Sum(Pyregel);
        int transferredAfter = roomAfter.Units.Sum(Pyregel);

        Require(card.DataId == CardDataId && card.MaskDescriptor!.SourceTraits is { Count: 0 } &&
            rule.AssetKey == "Deathroar" && rule.CardType == "Spell" && rule.Cost == 3 && rule.CostType == "Default" &&
            rule.Effects.Select(effect => effect.Type).SequenceEqual(new[] {
                "TransferAllStatuses", "Damage", "Damage", "Damage", "Damage", "Damage" }) &&
            transfer.Target == "DropTargetCharacter" && transfer.AllowPlayer && transfer.AllowEnemy && transfer.Value == 0 &&
            transfer.Statuses.Count == 1 && transfer.Statuses[0].Id == "pyregel" && transfer.Statuses[0].Stacks == 1 &&
            damages[0].Target == "DropTargetCharacter" && damages.Skip(1).All(effect => effect.Target == "LastTargetedCharacters") &&
            damages.All(effect => effect.Value == 5 && effect.AllowPlayer && effect.AllowEnemy) &&
            rule.ExternalInteractions.Count == 0 && action.CardInstanceId == ids[3] && action.RoomIndex == 0 &&
            action.PlayerPosition == -1 && action.TargetUnitId == transferTargetBefore.Id &&
            transferTargetBefore.Team == CombatTeam.Player && Pyregel(sourceBefore) == 3 && Pyregel(transferTargetBefore) == 3 &&
            transferredBefore == 6 && Pyregel(sourceAfter) == 0 && Pyregel(transferTargetAfter) == transferredBefore &&
            transferredAfter == transferredBefore && Pyregel(controlBefore) == 3 && Pyregel(controlAfter) == 3 &&
            transferTargetAfter.Health < transferTargetBefore.Health && sourceAfter.Health == sourceBefore.Health &&
            controlAfter.Health == controlBefore.Health && before.Energy - after.Energy == rule.Cost &&
            after.Spawn.Train.Context!.Statistics!.Value(ids[3], "TimesPlayed", "ThisBattle") == 1 &&
            after.Spawn.Train.Context.Cards.Discard.Any(item => item.InstanceId == ids[3]),
            "Deathroar changed its native cost/effect order, failed to transfer room-local Pyregel, missed damage/floor isolation or routed incorrectly.");

        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(after),
            "The recorded Deathroar action does not replay to the native state: " + replay.Reason);
        Require(BattleActionModel.EnumerateActions(before).SupportedCandidates.Any(candidate => candidate.Action.CardInstanceId == ids[3] &&
            candidate.Action.RoomIndex == 0 && candidate.Action.TargetUnitId == transferTargetBefore.Id),
            "Action search did not enumerate the native Deathroar play and selected target.");
        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-DEATHROAR PASS: transferred six room-local Pyregel stacks, preserved the outside-floor control and replayed five ordered 5-damage effects.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new System.InvalidOperationException(message); }
}
