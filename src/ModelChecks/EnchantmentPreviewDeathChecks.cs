using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EnchantmentPreviewDeathChecks
{
    private const string Reason = "Native enchantment preview target flags after summon.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        Check(fixture, true);
        if (replay != null)
        {
            Check(replay.Value, false);
            NativeBranchObservationChecks.Pair(fixture, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("ENCHANTMENT-PREVIEW-DEATH PASS: 12 complete native successor states/forecasts/RNG; primary enchantments retained; three preview flags withdrawn;" +
            (replay == null ? " independent replay pending." : " independent corrected native pair."));
    }
    private static void Check(FixtureValue fixture, bool historicalMismatch)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, historicalMismatch ? HistoricalMismatch : null);
        Require(rows.Length == 12 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native summon boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(before.Spawn.Turn == 2 && after.Spawn.Turn == 2 && action.CardInstanceId == 3 && action.RoomIndex == 0 &&
            Actor(before, 14).AssetKey == "EnchanterT2_Titanskin" && Actor(after, 14).Health == 10 &&
            Actor(after, 15).AssetKey == "TrainStewardBig" && Actor(after, 15).Health == 25 &&
            State(before).PrimaryTargets.All(target => target.IsEnchanted) && State(before).PreviewTargets.All(target => target.IsEnchanted) &&
            State(after).PrimaryTargets.Select(target => target.UnitId).SequenceEqual(new[] { 11, 12, 13 }) &&
            State(after).PrimaryTargets.All(target => target.IsEnchanted && target.NextAction == 0) &&
            State(after).PreviewTargets.Select(target => target.UnitId).SequenceEqual(new[] { 11, 12, 13 }) &&
            State(after).PreviewTargets.All(target => !target.IsEnchanted && target.NextAction == 0) &&
            !State(after).PreviewRequiresSync && new[] { 11, 12, 13 }.All(id => Actor(after, id).Status("titanskin")?.Stacks == 5) &&
            after.Spawn.Train.Context!.Enchantments!.EnchanterIds.SequenceEqual(new[] { 14 }),
            "Missing original primary/preview enchantment separation after the upgraded steward summon.");
        var controlled = EnchantmentWorldModel.Rebase(before.Spawn.Train, allowUpdates: false);
        var context = controlled.Context!;
        var frame = context.Enchantments!.Frame(context);
        var updated = EnchantmentCombatModel.UpdateDeathwish(frame);
        Require(updated.Supported && !updated.State!.AllowUpdates && updated.State.EnchanterIds.SequenceEqual(frame.EnchanterIds) &&
            updated.Requests.Count == 0 && State(before).PreviewTargets.All(target => target.IsEnchanted),
            "Deathwish-only update lost the aura registry/gate or invoked an aura callback.");
    }
    private static void HistoricalMismatch(BattleTurnState historical, BattleTurnState actual) =>
        Require(State(historical).PreviewTargets.All(target => target.IsEnchanted) &&
            State(actual).PreviewTargets.All(target => !target.IsEnchanted) &&
            historical.Spawn.Train.Context!.Enchantments!.EnchanterIds.Count == 0 &&
            actual.Spawn.Train.Context!.Enchantments!.EnchanterIds.SequenceEqual(new[] { 14 }),
            "Historical incorrect preview flags or lost aura registry were replaced.");
    private static EnchantmentState State(BattleTurnState state) => Actor(state, 14).Triggers[0].Effects[0].Enchantment!.State;
    private static CombatUnit Actor(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
