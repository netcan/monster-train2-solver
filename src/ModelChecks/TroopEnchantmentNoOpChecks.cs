using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TroopEnchantmentNoOpChecks
{
    internal const string Reason = "Combined troop upgrades and enchantment source ordering require native calibration.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 8 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing eight-input natural troop/enchanter witness.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = actual.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        var owner = frame.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == "MonsterSquadLeader");
        var rule = owner.Modifiers!.TroopAura!;
        var enchanter = frame.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == frame.EnchanterIds.Single());
        Require(before.Spawn.Turn == 2 && actual.Spawn.Turn == 3 && rule.Targets.Count == 0 &&
            !frame.Train.Rooms.SelectMany(room => room.Units).Any(unit => TroopAuraModel.Matches(unit, rule)) &&
            enchanter.AssetKey == "EnchanterT1_Speed" && enchanter.Triggers.SelectMany(trigger => trigger.Effects)
                .Any(effect => effect.Enchantment is { Targeting.AllowPlayer: false, Targeting.AllowEnemy: true,
                    StatusPool: [{ Id: "haste", Stacks: 1, ParamInt: 2 }] }),
            "Lost natural empty troop cache or enemy-only haste owner.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual) &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "End turn changed its parent/native state/RNG.");
        });
        foreach (string control in new[] { "NewTarget", "DepartingTarget", "DeadCache" })
        {
            var changedOwner = owner.WithModifiers(owner.Modifiers.WithTroopAura(rule.WithTargets(
                control == "DepartingTarget" ? [owner.Id] : control == "DeadCache" ? [int.MaxValue] : [])));
            if (control == "NewTarget")
            {
                var node = JsonSerializer.SerializeToNode(changedOwner, ModelJson.Options)!;
                node["Subtypes"] = JsonSerializer.SerializeToNode(new[] { rule.Subtype });
                changedOwner = node.Deserialize<CombatUnit>(ModelJson.Options)!;
            }
            var train = new TrainCombatState(frame.Train.Rooms.Select(room => room.WithUnits(room.Units
                .Select(unit => unit.Id == owner.Id ? changedOwner : unit).ToArray())).ToArray(),
                frame.Train.Movement, frame.Train.EnemySlotsPerRoom, frame.Train.Context);
            var changed = new EnchantmentCombatState(train, frame.RetainedUnits, frame.EnchanterIds, frame.AllowUpdates,
                frame.Updating, frame.Preview, frame.TestRng, frame.TemporaryPreviewUnitIds);
            var result = TroopAuraModel.UpdateAll(changed, [owner.Id]);
            if (control == "DeadCache") Require(result.Supported && result.State!.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.Id == owner.Id).Modifiers!.TroopAura!.Targets.Count == 0, "Dead-only cache cleanup was rejected.");
            else Require(!result.Supported && result.UnsupportedReason == Reason, "Actual troop modifier writes escaped the ordering boundary: " + control);
        }
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("TROOP-ENCHANTMENT-NOOP PASS:8 native inputs/full state/RNG, natural enemy haste and empty Squad Leader cache, " +
            "four immutable branches, actual modifier additions/removals still rejected, dead-cache cleanup; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
