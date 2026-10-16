using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMoreSpikesChecks
{
    private const string Id = "a6c957cc-e9f5-49b7-a6a3-cd7e95393289";
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-more-spikes" &&
            root.GetProperty("RelicStatusAttackSchema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "MoreSpikes native battle incomplete or from another build.");
        int calls = 0, positives = 0, controls = 0;
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var owner = before.Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == Id);
            Require(owner.AssetKey == "MoreSpikes" && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(["RelicEffectAdjustAttackByStatusEffect"]) && owner.StatusAttackModifiers!.Count == 1,
                "Unchanged MoreSpikes owner did not retain its ordered dynamic attack definition.");
            foreach (var unit in before.Spawn.Train.Rooms.SelectMany(room => room.Units))
            {
                var definition = unit.Modifiers!.StatusAttackModifiers!.Single();
                Require(definition.EffectIndex == 0 && definition.StatusId == "spikes" && definition.Team == "Monsters" && definition.Multiplier == 1,
                    "Unit attack lost the original relic's separate native modifier.");
                int stacks = unit.Status("spikes")?.Stacks ?? 0;
                int actualAdditional = unit.Attack - unit.AttackBeforeStatuses - unit.AttackFromStatuses;
                int expectedAdditional = unit.Team == CombatTeam.Player ? stacks : 0;
                Require(actualAdditional == expectedAdditional, "Dynamic attack query differs for unit " + unit.Id + ".");
                if (unit.Team == CombatTeam.Player && stacks > 0) positives++;
                if (unit.Team == CombatTeam.Enemy) controls++;
            }
            var predicted = BattleActionModel.PlayCard(before, row.GetProperty("Action").Deserialize<PlayCardAction>()!);
            Require(predicted.Supported, "MoreSpikes native action no longer resolves: " + predicted.Reason);
            Equal(predicted.State, actual, "full native action"); calls++;
        }
        Require(calls >= 10 && positives > 0 && controls > 0, "Missing positive friendly Spike or enemy control witnesses.");
        Console.WriteLine($"ORIGINAL-MORE-SPIKES-NATIVE PASS: {calls} exact native actions, {positives} positive friendly Spike observations, {controls} enemy controls, separate dynamic attack and zero differential errors.");
    }
    private static void Equal<T>(T a, T b, string label) => Require(System.Text.Json.JsonSerializer.Serialize(a) == System.Text.Json.JsonSerializer.Serialize(b), label + " differs.");
    private static void Require(bool condition, string message) { if (!condition) throw new System.InvalidOperationException(message); }
}
