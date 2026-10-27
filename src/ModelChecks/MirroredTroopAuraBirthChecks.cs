using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MirroredTroopAuraBirthChecks
{
    internal const string Reason = "Witchweave slay-born Funguy health differs by one.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, historicalBoundaryMismatch: (old, actual) => {
            var predicted = Funguy(old); var native = Funguy(actual);
            Require(predicted.Health == 15 && predicted.MaxHealth == 15 && native.Health == 16 && native.MaxHealth == 16 &&
                AuraCount(predicted) == 3 && AuraCount(native) == 4,
                "Preserve the original extra troop-aura birth write witness.");
        });
        Require(rows.Length == 7 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong Witchweave death-birth boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var born = Funguy(actual);
        Require(born.Status("horde")?.Stacks == 3 && born.Health == 16 && AuraCount(born) == 4 &&
            actual.Spawn.Train.Context!.NextUnitId == 11,
            "Mirrored birth must retain one Funguy identity, three Horde stacks and four aura writes.");
        string parent = NativeBranchObservationChecks.Key(before);
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Mirrored troop-aura birth differs from complete native state/forecast/RNG.");
        });
        Require(parent == NativeBranchObservationChecks.Key(before), "Mirrored troop-aura birth mutated its parent.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("MIRRORED-TROOP-AURA-BIRTH PASS:7 native inputs/full state/forecast/RNG; original 15 vs16 HP and three vsfour aura writes; four immutable branches; independentPair=" + replay.HasValue);
    }
    private static CombatUnit Funguy(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
        .Single(unit => unit.Id == 10 && unit.AssetKey == "MonsterFunguy");
    private static int AuraCount(CombatUnit unit) => unit.Modifiers!.Upgrades.Count(upgrade => upgrade.AssetKey == "Underlegion1RegenOnHarvest1_Upgrade");
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
