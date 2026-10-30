using MonsterTrain2Poju.Model;

internal static class TentativeAdviceProgressChecks
{
    internal static void Run()
    {
        var steps = new[] {
            new BattleSearchStep(new PlayCardAction(11, 1, playerPosition: 0), false),
            new BattleSearchStep(new PlayCardAction(12, 1, targetUnitId: 9), false),
            new BattleSearchStep(null, true)
        };
        bool Match(int offset, int card, int room, int drop, int target, bool direct = true) =>
            TentativeAdviceProgress.Matches(steps, offset, card, room, drop, target, direct);
        Require(Match(0, 11, 1, 0, 42), "deployment on an occupied slot");
        Require(Match(1, 12, 1, 7, 9), "targeted spell after a second input");
        Require(!Match(0, 12, 1, 0, 0) && !Match(0, 11, 2, 0, 0) && !Match(0, 11, 1, 1, 0), "card, room or deployment mismatch");
        Require(!Match(1, 12, 1, 7, 8) && !Match(1, 12, 1, 7, -1), "wrong or unknown target");
        Require(!Match(0, 11, 1, 0, 0, false) && !Match(2, 11, 1, 0, 0), "automatic input or end turn");
        Require(!Match(-1, 11, 1, 0, 0) && !Match(3, 11, 1, 0, 0), "invalid offset");
        var skill = new[] { new BattleSearchStep(new PlayCardAction(11, 1, activatorUnitId: 1), false), steps[2] };
        Require(!TentativeAdviceProgress.Matches(skill, 0, 11, 1, -1, 0, true), "unit ability");
        var roomSkill = new[] { new BattleSearchStep(new PlayCardAction(11, 1, activatorRoomIndex: 1), false), steps[2] };
        Require(!TentativeAdviceProgress.Matches(roomSkill, 0, 11, 1, -1, 0, true), "room ability");
        var location = new[] { new BattleSearchStep(new PlayCardAction(11, 1, targetSpawnPointIndex: 0), false), steps[2] };
        Require(!TentativeAdviceProgress.Matches(location, 0, 11, 1, 0, 0, true), "targeted location");
        var recursion = new[] { new BattleSearchStep(new PlayCardAction(11, 1,
            recursionSelections: new[] { new CardRecursionSelection(0, new[] { 12 }) }), false), steps[2] };
        Require(!TentativeAdviceProgress.Matches(recursion, 0, 11, 1, -1, 0, true), "recursion selection");
        var mix = new[] { new BattleSearchStep(new PlayCardAction(11, 1,
            mixSelections: new[] { new ElixirMixSelection(0, 0, 0) }), false), steps[2] };
        Require(!TentativeAdviceProgress.Matches(mix, 0, 11, 1, -1, 0, true), "mix selection");
        var last = new[] { steps[0] };
        Require(!TentativeAdviceProgress.Matches(last, 0, 11, 1, 0, 0, true), "no remaining step");
        Console.WriteLine("Tentative advice: matching, divergence, consecutive plays and unsupported inputs passed.");
    }

    private static void Require(bool value, string scenario)
    { if (!value) throw new InvalidOperationException("Tentative advice: " + scenario); }
}
