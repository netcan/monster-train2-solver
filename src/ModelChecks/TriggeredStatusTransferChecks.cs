using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class TriggeredStatusTransferChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(901);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10);
        CombatStatus Pyregel(int stacks) => new("pyregel", stacks, stackable: true, hidden: false, displayCategory: "Negative");
        CombatUnit Unit(int id, CombatTeam team, int hp, int stacks = 0, bool untouchable = false)
        {
            CombatStatus[] statuses = (stacks > 0 ? new[] { Pyregel(stacks) } : Array.Empty<CombatStatus>())
                .Concat(untouchable ? new[] { new CombatStatus("untouchable", 1, hidden: true) } : Array.Empty<CombatStatus>()).ToArray();
            return new(id, "carrier", team, 0, hp, 20, false, false, false, statuses, statusRegistry: statuses);
        }
        var source = Unit(1, CombatTeam.Player, 0, stacks: 5);
        var front = Unit(2, CombatTeam.Enemy, 20, stacks: 2);
        var back = Unit(3, CombatTeam.Enemy, 20);
        var dead = Unit(4, CombatTeam.Enemy, 0, stacks: 9);
        var untouchable = Unit(5, CombatTeam.Enemy, 20, stacks: 7, untouchable: true);
        var ally = Unit(6, CombatTeam.Player, 20, stacks: 4);
        var room = new RoomCombatState(0, false, [source, front, back, dead, untouchable, ally], [], context);
        var original = JsonSerializer.Serialize(room);
        RoomCombatResult result = TriggeredStatusTransferModel.Apply(room, source.Id, Action(), 0);
        Require(result.Supported, result.UnsupportedReason ?? "Even distribution was unsupported.");
        Require(result.State!.Units.Single(unit => unit.Id == source.Id).Status("pyregel") == null &&
            result.State.Units.Single(unit => unit.Id == front.Id).Status("pyregel")?.Stacks == 5 &&
            result.State.Units.Single(unit => unit.Id == back.Id).Status("pyregel")?.Stacks == 3 &&
            result.State.Units.Single(unit => unit.Id == dead.Id).Status("pyregel")?.Stacks == 9 &&
            result.State.Units.Single(unit => unit.Id == untouchable.Id).Status("pyregel")?.Stacks == 7 &&
            result.State.Units.Single(unit => unit.Id == ally.Id).Status("pyregel")?.Stacks == 4 &&
            result.PendingCallbacks.Select(callback => (callback.Unit.Id, callback.Kind)).SequenceEqual(new[]
            {
                (source.Id, "OnStatusEffectChanged"), (back.Id, "OnStatusEffectChanged"), (back.Id, "OnPyregelAdded"),
                (back.Id, "OnNewStatusEffectAdded"), (front.Id, "OnStatusEffectChanged"), (front.Id, "OnPyregelAdded")
            }) && result.State.Context!.BattleRng.Equals(context.BattleRng),
            "Evenly distributed native Pyregel transfer changed the wrong units, stacks or RNG: " +
            string.Join(";", result.State.Units.Select(unit => unit.Id + "=" + (unit.Status("pyregel")?.Stacks ?? 0))) +
            "; callbacks=" + string.Join(",", result.PendingCallbacks.Select(callback => callback.Unit.Id + ":" + callback.Kind)) +
            "; rng=" + result.State.Context!.BattleRng.Equals(context.BattleRng));

        var noTargets = new RoomCombatState(0, false, [source], [], context);
        RoomCombatResult removed = TriggeredStatusTransferModel.Apply(noTargets, source.Id, Action(), 0);
        Require(removed.Supported && removed.State!.Units.Single().Status("pyregel") == null,
            "A source status was not removed when the native target team was empty.");

        var emptySource = Unit(1, CombatTeam.Player, 20);
        var noStatus = new RoomCombatState(0, false, [emptySource, back], [], context);
        RoomCombatResult unchanged = TriggeredStatusTransferModel.Apply(noStatus, emptySource.Id, Action(), 0);
        Require(unchanged.Supported && JsonSerializer.Serialize(unchanged.State) == JsonSerializer.Serialize(noStatus),
            "A statusless source changed its target or state.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(
            TriggeredStatusTransferModel.Apply(room, source.Id, Action(), 0).State) == JsonSerializer.Serialize(result.State),
            "Parallel status-transfer branches diverged."));
        Require(JsonSerializer.Serialize(room) == original, "Status transfer mutated the source branch.");
        Console.WriteLine("TRIGGERED-STATUS-TRANSFER-CHECKS PASS: hero-team room targeting, ceil distribution, dead/untouchable/allied controls, empty-target removal, empty source, no RNG and 32 immutable branches.");

        static CardActionEffect Action() => new("TransferAllStatusesFromUnit", "Self", 12, true, true,
            [new CombatStatus("pyregel", 1, hidden: false, displayCategory: "Negative")],
            statusTransfer: new("EvenlyDistributedAcrossHeroTeam", "BuffsAndDebuffs", "SourceRoomOnly", false, false, 0));
        static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
