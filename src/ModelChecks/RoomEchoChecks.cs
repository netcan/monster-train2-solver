using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomEchoChecks
{
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains("d74c257e-d38b-44da-b3d4-0a67934803a7")) return;
        var copies = new List<int>(); int controls = 0, replays = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            RoomEchoState? old = before.Spawn.Train.Context!.RoomCapacities!.SelectMany(room => room.Attachments ?? []).SingleOrDefault(attachment => attachment.Echo != null)?.Echo;
            RoomEchoState? next = after.Spawn.Train.Context!.RoomCapacities!.SelectMany(room => room.Attachments ?? []).SingleOrDefault(attachment => attachment.Echo != null)?.Echo;
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == before.Spawn.Train.Context.FindCard(action.CardInstanceId)!.DataId);
            if (next == null || rule.CardType != "Spell") continue;
            if (action.RoomIndex != 0)
            {
                Require(old != null && old.CopyCardId == next.CopyCardId && old.OriginalCardId == next.OriginalCardId,
                    "A spell on another floor must not overwrite Echo Vault's remembered spell.");
                controls++; continue;
            }
            CombatContext context = after.Spawn.Train.Context;
            CardInstanceState copy = context.FindCard(next.CopyCardId)!;
            Require(next.OriginalCardId == action.CardInstanceId && next.CopyCardId >= before.Spawn.Train.Context.NextCardId &&
                copy.DataId == rule.DataId && copy.PlayCount == 0 && copy.LastPlayedCost == next.PaidCost &&
                context.CardInstances!.All(card => card.InstanceId != copy.InstanceId) &&
                !context.Cards.Hand.Concat(context.Cards.Draw).Concat(context.Cards.Discard).Any(card => card.InstanceId == copy.InstanceId) &&
                next.Effects?.Count == rule.Effects.Count,
                "Echo must allocate a distinct non-owned spell copy with original effects and paid cost.");
            copies.Add(copy.InstanceId);
        }
        foreach (FixtureValue row in root.GetProperty("Turns").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext old = before.Spawn.Train.Context!, next = after.Spawn.Train.Context!;
            RoomEchoState? echo = old.RoomCapacities!.SelectMany(room => room.Attachments ?? []).SingleOrDefault(attachment => attachment.Echo != null)?.Echo;
            if (echo?.CopyCardId is not > 0) continue;
            RoomEchoState cleared = next.RoomCapacities!.SelectMany(room => room.Attachments ?? []).Single(attachment => attachment.Echo != null).Echo!;
            Require(cleared is { CopyCardId: 0, OriginalCardId: 0, RoomIndex: -1, PaidCost: 0, Corruption: -1 } &&
                next.Statistics!.Value(echo.CopyCardId, "TimesPlayed", "ThisBattle") == 0 &&
                next.Statistics.Value(old.Statistics!.TrackedCards[0], "AnyCardPlayed", "ThisBattle") -
                    old.Statistics.Value(old.Statistics.TrackedCards[0], "AnyCardPlayed", "ThisBattle") == 1 &&
                copies.Where(id => id != echo.CopyCardId).All(id => next.Statistics.Value(id, "TimesPlayed", "ThisBattle") == 0) &&
                next.FindCard(echo.CopyCardId)!.PlayCount == 0,
                "Only the last remembered copy must replay once, without local discard history, then clear the room cache.");
            replays++;
        }
        int damageWitnesses = 0;
        foreach (FixtureValue row in root.GetProperty("RoomPreCombatRecords").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, after = row.GetProperty("After").Deserialize<TrainCombatState>()!;
            RoomEchoState? echo = before.Context!.RoomCapacities!.SelectMany(room => room.Attachments ?? []).SingleOrDefault(attachment => attachment.Echo != null)?.Echo;
            if (echo?.CopyCardId is not > 0) continue;
            Require(row.GetProperty("Completed").GetBoolean(), "Incomplete native Echo pre-combat callback.");
            int targetId = before.Context.SpawnPoints!.Groups.Single(group => group.RoomIndex == echo.RoomIndex &&
                group.Team == echo.DropLocation!.Team && group.PreviewCopyId == echo.DropLocation.PreviewCopyId).Occupants[echo.DropLocation!.Index];
            CombatUnit target = before.Rooms[0].Units.Single(unit => unit.Id == targetId);
            CombatUnit next = after.Rooms[0].Units.Single(unit => unit.Id == target.Id);
            Require(target.Health == next.Health && (target.Status("armor")?.Stacks ?? 0) - (next.Status("armor")?.Stacks ?? 0) == 3,
                "Remembered original Torch must positively deal three damage during native room pre-combat.");
            damageWitnesses++;
        }
        Require(copies.Count >= 2 && controls > 0 && replays > 0 && damageWitnesses > 0, "Missing overwrite, other-floor control or positive Echo replay.");
        CheckPassiveSilverBoundary(root);
        Console.WriteLine($"ORIGINAL-ROOM-ECHO PASS: {copies.Count} independent copies, {controls} other-floor controls and {replays} native free replays with cache cleanup.");
    }
    private static void CheckPassiveSilverBoundary(FixtureValue root)
    {
        FixtureValue row = root.GetProperty("Actions").EnumerateArray().First(value =>
        {
            BattleTurnState turn = value.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction play = value.GetProperty("Action").Deserialize<PlayCardAction>()!;
            return turn.Spawn.Train.Context!.RoomCapacities!.Any(room => room.RoomIndex == play.RoomIndex &&
                room.Attachments?.Any(attachment => attachment.Echo?.CopyCardId > 0) == true) &&
                turn.PlayRules!.Cards.Any(rule => rule.DataId == turn.Spawn.Train.Context.FindCard(play.CardInstanceId)!.DataId &&
                    rule.CardType == "Spell");
        });
        BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext context = before.Spawn.Train.Context!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(value => value.DataId == context.FindCard(action.CardInstanceId)!.DataId);
        CombatRelicState Silver(int ember = 1, string? id = null) => new(
            id ?? "1a69fbff-9f1d-4c98-92ff-a63e0ac520c5", "SilverIncreasedEmber",
            [RelicResourceModel.Energy, RelicResourceModel.DeploymentEnergy], isCovenant: false,
            disallowedInPlacementPhase: false, resourceEffects:
            [new RelicResourceEffect(0, RelicResourceModel.Energy, ember, 0, ember, false),
                new RelicResourceEffect(1, RelicResourceModel.DeploymentEnergy, 1, 0, 1, false)]);
        CombatRelicState silver = Silver();
        Require(RoomEchoModel.PassiveSilverIncreasedEmber(silver) &&
            !RoomEchoModel.PassiveSilverIncreasedEmber(Silver(2)) &&
            !RoomEchoModel.PassiveSilverIncreasedEmber(Silver(id: "unknown-relic")),
            "Only the original one-Ember Silver resource signature may bypass the Echo callback guard.");
        TrainCombatState WithRelics(params CombatRelicState[] relics) =>
            CardSpellModel.WithContext(before.Spawn.Train, context.WithRelics(relics));
        Require(RoomEchoModel.Remember(WithRelics(silver), before, action, rule).Supported,
            "Original Silver resource effects must permit the remembered Echo spell.");
        Require(!RoomEchoModel.Remember(WithRelics(Silver(2)), before, action, rule).Supported &&
            !RoomEchoModel.Remember(WithRelics(new CombatRelicState("unknown-relic", "UnknownRelic",
                ["RelicEffectAddTrigger"])), before, action, rule).Supported,
            "Modified Silver and unrelated callback relics must retain the Echo replay guard.");

        if (context.Relics?.Any(RoomEchoModel.PassiveSilverIncreasedEmber) == true)
        {
            TrainCombatState preCombat = root.GetProperty("RoomPreCombatRecords").EnumerateArray()
                .Select(value => value.GetProperty("Before").Deserialize<TrainCombatState>()!)
                .First(train => train.Context!.RoomCapacities!.Any(room => room.Attachments?.Any(attachment =>
                    attachment.Echo?.CopyCardId > 0) == true));
            RoomCombatState echoRoom = preCombat.Rooms.Single(room => room.RoomIndex == 0);
            RoomCombatResult Recast(params CombatRelicState[] relics) =>
                RoomEchoModel.PreCombat(echoRoom.WithContext(echoRoom.Context!.WithRelics(relics)));
            Require(Recast(silver).Supported && !Recast(Silver(2)).Supported &&
                !Recast(new CombatRelicState("unknown-relic", "UnknownRelic", ["RelicEffectAddTrigger"])).Supported,
                "Only pinned Silver may pass the free Echo recast callback guard.");
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
