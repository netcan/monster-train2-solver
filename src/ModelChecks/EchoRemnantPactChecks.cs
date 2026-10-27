using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EchoRemnantPactChecks
{
    private const string EndReason = "Echo Remnant Pact retains its paid2 single-curse copy and replays Endless during pre-combat.";
    internal static void Native(FixtureValue source, FixtureValue? endTurn = null, FixtureValue? replay = null)
    {
        string reason = source.GetProperty("Request").GetProperty("Reason").GetString()!;
        Require(reason.StartsWith("Echo replay needs native calibration: onCast=0, unplayed=0, interactions=, traits=CardTraitExhaustState, relics=", StringComparison.Ordinal),
            "Missing native Remnant Pact Echo boundary.");
        var rows = NativeBranchObservationChecks.Replay(source, reason);
        Require(rows.Length == 48, "Expected47 inputs followed by the native Remnant Pact cast.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = after.Spawn.Train.Context!;
        var echo = context.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!.Single(attachment => attachment.Echo != null).Echo!;
        var copy = context.FindCard(echo.CopyCardId)!;
        var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == EchoRemnantPactModel.DataId);
        var oldTarget = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 50);
        var target = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2).Units.Single(unit => unit.Id == 50);
        Require(before.Spawn.Turn == 10 && before.Energy == 3 && after.Energy == 1 &&
            action.CardInstanceId == 11 && action.RoomIndex == 2 && action.TargetUnitId == 50 &&
            echo.OriginalCardId == 11 && echo.PaidCost == 2 && echo.CopyCardId == before.Spawn.Train.Context!.NextCardId &&
            context.NextCardId == echo.CopyCardId + 1 && copy.LastPlayedCost == 2 && copy.PlayCount == 0 &&
            EchoRemnantPactModel.Known(copy, echo.Effects!) && EchoJuiceSpikeModel.KnownRelics(context) &&
            copy.Temporary.Upgrades.Count == 1 && CardJuiceModel.KnownTitanCurse(copy.Temporary.Upgrades[0]) &&
            JsonSerializer.Serialize(echo.Effects, ModelJson.Options) == JsonSerializer.Serialize(rule.Effects, ModelJson.Options) &&
            target.Status("endless")!.Stacks == (oldTarget.Status("endless")?.Stacks ?? 0) + 1 &&
            target.Modifiers!.Upgrades.Count == oldTarget.Modifiers!.Upgrades.Count + 1,
            "Native Pact must apply Endless once, consume at paid2, and remember its raw effect/single-curse copy.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(after),
                "Remnant Pact Echo differs from full native state/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Pact Echo mutated parent.");
        foreach (string control in new[] { "Card", "Trait", "Curse", "Offset", "Upgrade", "Damage", "Stacks", "Lifecycle" })
        {
            var changedCard = JsonSerializer.SerializeToNode(copy, ModelJson.Options)!;
            var changedEffects = JsonSerializer.SerializeToNode(echo.Effects, ModelJson.Options)!;
            if (control == "Card") changedCard["DataId"] = "unknown-pact";
            if (control == "Trait") changedCard["MaskDescriptor"]!["BaseTraits"]![0]!["ParamInt"] = 1;
            if (control == "Curse") changedCard["Temporary"]!["Upgrades"]![0]!["Stats"]!["Health"] = 1;
            if (control == "Offset") changedCard["Permanent"]!["Offsets"]!["Damage"] = 1;
            if (control == "Upgrade") changedEffects[0]!["Upgrade"]!["DataId"] = "unknown-upgrade";
            if (control == "Damage") changedEffects[0]!["Upgrade"]!["Stats"]!["Damage"] = 1;
            if (control == "Stacks") changedEffects[0]!["Upgrade"]!["Statuses"]![0]!["Stacks"] = 2;
            if (control == "Lifecycle") changedEffects[0]!["Upgrade"]!["Lifecycle"]!["RemovedRuntimeTypes"]!.AsArray().Add("CardTraitExhaustState");
            Require(!EchoRemnantPactModel.Known(changedCard.Deserialize<CardInstanceState>(ModelJson.Options)!,
                changedEffects.Deserialize<CardActionEffect[]>(ModelJson.Options)!), "Unknown Echo Pact payload admitted: " + control);
        }
        if (endTurn.HasValue)
        {
            var extended = NativeBranchObservationChecks.Replay(endTurn.Value, EndReason, boundaryOutcome: RoomOutcome.PlayerDefeated);
            Require(extended.Length == 49 && extended[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native Pact Echo EndTurn.");
            for (int i = 0; i < rows.Length; i++)
            {
                foreach (string field in new[] { "Action", "IsEndTurn" })
                    Require(rows[i].GetProperty(field).ContentEquals(extended[i].GetProperty(field)), "Pact EndTurn extension changed an input.");
                foreach (string field in new[] { "Before", "Actual" })
                    Require(NativeBranchObservationChecks.Key(rows[i].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(extended[i].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Pact EndTurn extension changed a complete prefix state/RNG.");
            }
            var final = extended[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(final.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!
                .Single(attachment => attachment.Echo != null).Echo!.CopyCardId == 0, "Pact Echo cache did not clear.");
            string immutable = JsonSerializer.Serialize(after, ModelJson.Options);
            Parallel.For(0, 4, _ =>
            {
                var result = BattleTurnModel.EndTurn(after);
                Require(result.Supported && result.Outcome == RoomOutcome.PlayerDefeated &&
                    NativeBranchObservationChecks.Key(result.State!, true) == NativeBranchObservationChecks.Key(final, true),
                    "Pact Echo EndTurn differs from full native terminal state/RNG.");
            });
            Require(immutable == JsonSerializer.Serialize(after, ModelJson.Options), "Pact Echo EndTurn mutated parent.");
            CheckPreCombat(endTurn.Value);
            if (replay.HasValue)
            {
                var fresh = NativeBranchObservationChecks.Replay(replay.Value, EndReason, boundaryOutcome: RoomOutcome.PlayerDefeated);
                Require(fresh.Length == extended.Length && endTurn.Value.GetProperty("Request").ContentEquals(replay.Value.GetProperty("Request")),
                    "Independent Pact Echo request changed.");
                for (int i = 0; i < extended.Length; i++)
                {
                    foreach (string field in new[] { "Action", "IsEndTurn", "Boundary" })
                        Require(extended[i].GetProperty(field).ContentEquals(fresh[i].GetProperty(field)), "Independent Pact Echo input changed.");
                    foreach (string field in new[] { "Before", "Actual" })
                        Require(NativeBranchObservationChecks.Key(extended[i].GetProperty(field).Deserialize<BattleTurnState>()!, i == extended.Length - 1 && field == "Actual") ==
                            NativeBranchObservationChecks.Key(fresh[i].GetProperty(field).Deserialize<BattleTurnState>()!, i == extended.Length - 1 && field == "Actual"),
                            "Independent Pact Echo complete state/RNG changed.");
                }
                CheckPreCombat(replay.Value);
            }
        }
        Require(!replay.HasValue || endTurn.HasValue, "Pact replay requires its source EndTurn.");
        Console.WriteLine("ECHO-REMNANT-PACT PASS:48 native inputs/full state/RNG, paid2/raw copy/single curse/Endless, four immutable branches/eight payload guards; nativeEndTurn=" + endTurn.HasValue + "; independentPair=" + replay.HasValue);
    }
    private static void CheckPreCombat(FixtureValue fixture)
    {
        var phase = fixture.GetProperty("RoomPreCombatRecords").EnumerateArray().Single(row =>
            row.GetProperty("Turn").GetInt32() == 10 && row.GetProperty("RoomIndex").GetInt32() == 2);
        Require(phase.GetProperty("Completed").GetBoolean(), "Native Pact Echo pre-combat did not complete.");
        var before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!.Rooms.Single(room => room.RoomIndex == 2);
        var after = phase.GetProperty("After").Deserialize<TrainCombatState>()!.Rooms.Single(room => room.RoomIndex == 2);
        var old = before.Units.Single(unit => unit.Id == 50);
        var next = after.Units.Single(unit => unit.Id == 50);
        Require(next.Health == old.Health && next.Attack == old.Attack &&
            next.Status("endless")!.Stacks == old.Status("endless")!.Stacks + 1 &&
            next.Modifiers!.Upgrades.Count == old.Modifiers!.Upgrades.Count + 1 &&
            next.Modifiers.Upgrades[^1].DataId == "e90daf43-a1c5-4fcb-a012-b3a2c0064fcb" &&
            next.Modifiers.Upgrades[^1].Statuses.Single().Id == "endless", "Native Pact Echo must add one Endless upgrade once to its remembered target.");
        foreach (var enemy in before.Units.Where(unit => unit.Team == CombatTeam.Enemy))
        {
            var changed = after.Units.Single(unit => unit.Id == enemy.Id);
            // The floor's native Incant cleric buffs the three ordinary allies
            // when Echo replays a spell, even though Pact targets a friendly.
            int incant = enemy.Id is 44 or 45 or 46 ? 5 : 0;
            Require(changed.Attack == enemy.Attack + incant && changed.Health == enemy.Health &&
                (changed.Status("endless")?.Stacks ?? 0) == (enemy.Status("endless")?.Stacks ?? 0) &&
                changed.Modifiers!.Upgrades.Count == enemy.Modifiers!.Upgrades.Count,
                "Native enemy Incant must buff its allies once without receiving the friendly Pact upgrade.");
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
