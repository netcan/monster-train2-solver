using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DamageOverflowChecks
{
    private const int SourceCardId = 90;

    internal static void Run()
    {
        int[] expectedTeamByMode = [0, 1, 0, 1];
        for (int mode = 0; mode < expectedTeamByMode.Length; mode++)
        {
            RoomCombatResult result = Hit(mode, [Unit(10, CombatTeam.Enemy, 5), Unit(11, CombatTeam.Enemy, 30),
                Unit(12, CombatTeam.Player, 30)], 12);
            Require(result.Supported, "Damage overflow rejected target mode " + mode + ".");
            CombatTeam selectedTeam = expectedTeamByMode[mode] == 0 ? CombatTeam.Enemy : CombatTeam.Player;
            CombatTeam otherTeam = selectedTeam == CombatTeam.Enemy ? CombatTeam.Player : CombatTeam.Enemy;
            Require(Find(result, selectedTeam == CombatTeam.Enemy ? 11 : 12).Health == 23 &&
                Find(result, otherTeam == CombatTeam.Enemy ? 11 : 12).Health == 30,
                "Damage overflow target-team mode differed: " + mode + ".");
        }

        RoomCombatState recursionRoot = Root(0, [Unit(10, CombatTeam.Enemy, 5), Unit(11, CombatTeam.Enemy, 3),
            Unit(12, CombatTeam.Enemy, 20)]);
        string parent = JsonSerializer.Serialize(recursionRoot);
        RoomCombatResult noRecursion = RoomCombatModel.ApplyCardDamage(recursionRoot, 10, 12, SourceCardId);
        Require(noRecursion.Supported && !noRecursion.State!.Units.Any(unit => unit.Id == 11) &&
            Find(noRecursion, 12).Health == 13 && JsonSerializer.Serialize(recursionRoot) == parent,
            "Spill damage retriggered overflow or failed to visit every sibling with the original spill amount.");

        RoomCombatResult exactLethal = Hit(0, [Unit(10, CombatTeam.Enemy, 5), Unit(11, CombatTeam.Enemy, 30)], 5);
        Require(exactLethal.Supported && Find(exactLethal, 11).Health == 30,
            "Exact lethal damage incorrectly overflowed.");

        RoomCombatResult armored = Hit(0, [Unit(10, CombatTeam.Enemy, 5, new CombatStatus("armor", 10, 1)),
            Unit(11, CombatTeam.Enemy, 30)], 12);
        Require(armored.Supported && Find(armored, 10).Health == 3 && Find(armored, 11).Health == 30,
            "Overflow was calculated before armor reduced the applied damage.");

        Console.WriteLine("DAMAGE-OVERFLOW-CHECKS PASS: four team modes, ordered same-floor spill, recursion suppression, exact lethal and armor.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out FixtureValue scenario) || scenario.GetString() != "damage-overflow") return;
        int witnesses = 0;
        foreach (FixtureValue entry in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState? card = before.Spawn.Train.Context!.CardInstances?.FirstOrDefault(item => item.InstanceId == action.CardInstanceId);
            CardTraitValue? overflow = card?.MaskDescriptor?.BaseTraits.FirstOrDefault(trait => trait.RuntimeType == "CardTraitDamageOverflow");
            if (overflow == null) continue;
            Require(overflow.ParamInt == 0, "Native overflow witness used an unexpected target mode.");
            RoomCombatState room = before.Spawn.Train.Rooms.Single(item => item.RoomIndex == action.RoomIndex);
            CombatUnit target = room.Units.Single(unit => unit.Id == action.TargetUnitId);
            CombatUnit[] siblings = room.Units.Where(unit => unit.Team == target.Team && unit.Id != target.Id).ToArray();
            if (siblings.Length == 0 || target.Health >= 20) continue;
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            RoomCombatState? afterRoom = actual.Spawn.Train.Rooms.FirstOrDefault(item => item.RoomIndex == action.RoomIndex);
            bool spilled = siblings.Any(sibling =>
            {
                CombatUnit? after = afterRoom?.Units.FirstOrDefault(unit => unit.Id == sibling.Id);
                return after == null || after.Health < sibling.Health;
            });
            if (spilled) witnesses++;
        }
        Require(witnesses > 0, "Native DamageOverflow scenario did not damage a same-team sibling.");
        Console.WriteLine($"NATIVE-DAMAGE-OVERFLOW-CHECKS PASS: {witnesses} played overflow card witnesses damaged same-team siblings.");
    }

    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        CombatContext? context = source.Spawn.Train.Context;
        if (context?.CardInstances == null || source.PlayRules == null)
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        HashSet<int> cards = context.Cards.Hand.Where(token => context.CardInstances.Any(card =>
            card.InstanceId == token.InstanceId && card.MaskDescriptor?.BaseTraits.Any(trait =>
                trait.RuntimeType == "CardTraitDamageOverflow") == true)).Select(token => token.InstanceId).ToHashSet();
        if (cards.Count == 0) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);

        PlayCardAction? overflowPlay = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
        {
            if (!cards.Contains(action.CardInstanceId) || action.TargetUnitId <= 0) return false;
            RoomCombatState? room = source.Spawn.Train.Rooms.FirstOrDefault(item => item.RoomIndex == action.RoomIndex);
            CombatUnit? target = room?.Units.FirstOrDefault(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Enemy);
            if (room == null || target == null || target.Health <= 0 ||
                target.Statuses.Any(status => status.Id is "armor" or "damage shield" or "titanskin" or "untouchable") ||
                !room.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Id != target.Id)) return false;
            CardToken token = context.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId);
            CardActionEffect? damage = source.PlayRules.Cards.Single(rule => rule.DataId == token.DataId).Effects
                .FirstOrDefault(effect => effect.Type == "Damage");
            return damage != null && damage.Value > target.Health;
        });
        return overflowPlay ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    private static RoomCombatResult Hit(int targetMode, IReadOnlyList<CombatUnit> units, int damage)
        => RoomCombatModel.ApplyCardDamage(Root(targetMode, units), 10, damage, SourceCardId);

    private static RoomCombatState Root(int targetMode, IReadOnlyList<CombatUnit> units)
    {
        var rng = UnityRng.Seed(321);
        var trait = new CardTraitValue("CardTraitDamageOverflow", "CardTraitDamageOverflow", targetMode,
            targetMode, true, 0);
        var definition = new CardMaskDefinition("overflow-spell", "Spell", "Common", false, false, false,
            [], [], [], [], null, false, 0, 0, false, false, false);
        var descriptor = new CardMaskDescriptor(definition, 0, [trait], []);
        var sourceCard = new CardInstanceState(SourceCardId, definition.DataId, CardModifiers.Empty(),
            CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: descriptor);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0,
            SourceCardId + 1, 10, cardInstances: [sourceCard]);
        return new RoomCombatState(0, false, units, [], context);
    }

    private static CombatUnit Unit(int id, CombatTeam team, int health, params CombatStatus[] statuses) =>
        new(id, "unit-" + id, team, 0, health, health, true, false, false, statuses);

    private static CombatUnit Find(RoomCombatResult result, int id) => result.State!.Units.Single(unit => unit.Id == id);

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
