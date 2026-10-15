using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class ConduitChecks
{
    internal static void Run(CardUpgradeModifier template)
    {
        CardInstanceState Card(int id, bool? scale, bool stronger = false) => new(id, "spell", CardModifiers.Empty(),
            CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: new(new("spell", "Spell", "Common", false,
                false, false, [], [], [], [], null, false, 0, 0, false, false, false, scale), 1,
                stronger ? [new("CardTraitStrongerMagicPower", "CardTraitStrongerMagicPower", 0, 0, true, 0)] : [], []));
        var rng = UnityRng.Seed(832);
        var cards = new[] { Card(1, true), Card(2, true, true), Card(3, false) };
        var cycle = new CardCycleState(cards.Select(card => new CardToken(card.InstanceId, card.DataId)).ToArray(), [], [], rng, 0, []);
        var source = new CombatContext(cycle, rng, 0, 4, 10, cardInstances: cards, cardRegistry: cards,
            queryFrame: new(selectedRoomIndex: 0), magicPower: Enumerable.Range(0, 2).SelectMany(room =>
                new[] { CombatTeam.Enemy, CombatTeam.Player }.Select(team => new RoomMagicPower(room, team, 0, new(template, [])))).ToArray());
        var unit = new CombatUnit(10, "conductor", CombatTeam.Player, 1, 10, 10, true, false, false,
            [new("conduit", 4)], []);
        string parent = JsonSerializer.Serialize(source);
        var active = ConduitModel.Change(source, 0, unit, false);
        Require(active.FindCard(1)!.Temporary.Upgrades.Single().Stats.Damage == 4 &&
            active.FindCard(2)!.Temporary.Upgrades.Single().Stats.Damage == 20 &&
            active.FindCard(2)!.Temporary.Upgrades.Single().Stats.Heal == 20 &&
            active.FindCard(3)!.Temporary.Upgrades.Count == 0, "Conduit eligibility or trait multiplier differs.");
        Require(ConduitModel.Validate(source) == null && ConduitModel.Validate(source.WithCard(Card(3, null))) != null,
            "Conduit silently accepted missing magic-power eligibility.");
        var repeated = ConduitModel.Drawn(active, [1, 2]);
        Require(JsonSerializer.Serialize(repeated) == JsonSerializer.Serialize(active), "Drawing an already tracked card duplicated Conduit.");
        var discarded = ConduitModel.Discarded(active, 2);
        Require(discarded.FindCard(2)!.Temporary.Upgrades.Count == 0 && discarded.FindCard(1)!.Temporary.Upgrades.Count == 1,
            "Discard did not clear exactly the discarded card.");
        var selected = active.WithQueryFrame(active.QueryFrame!.With(selectedRoomIndex: 1));
        Require(selected.FindCard(1)!.Temporary.Upgrades.Count == 0 && selected.FindCard(2)!.Temporary.Upgrades.Count == 0,
            "Leaving a room retained Conduit upgrades.");
        var restored = selected.WithQueryFrame(selected.QueryFrame!.With(selectedRoomIndex: 0));
        Require(restored.FindCard(2)!.Temporary.Upgrades.Single().Stats.Damage == 20,
            "Room re-selection lost or repeated the multiplier.");
        var reduced = CardSpellModel.Copy(unit, unit.Health, [new("conduit", 2)]);
        var unselectedRemoved = ConduitModel.Change(selected, 0, reduced, false, refreshSelection: false);
        Require(unselectedRemoved.FindCard(1)!.Temporary.Upgrades.Single().Stats.Damage == 2,
            "Native stack removal should reapply hand tracking even when unselected.");
        var unselectedAdded = ConduitModel.Change(unselectedRemoved, 0, unit, false);
        Require(unselectedAdded.FindCard(1)!.Temporary.Upgrades.Count == 0,
            "Stack addition did not refresh and clear the unselected room's tracking.");
        var dead = ConduitModel.Alive(active, 0, CardSpellModel.Copy(unit, 0, unit.Statuses));
        Require(dead.MagicPower!.Single(room => room.RoomIndex == 0 && room.Team == CombatTeam.Player).Value == 0,
            "Dead conductor continued contributing room magic power.");
        var removed = ConduitModel.Change(dead, 0, unit, false, remove: true);
        Require(removed.FindCard(1)!.Temporary.Upgrades.Count == 0 && removed.MagicPower!.All(room => room.Conduit!.Units.Count == 0),
            "Physical removal retained a Conduit tracker.");
        var preview = ConduitModel.Change(source, 0, unit, true);
        Require(preview.MagicPower!.Single(room => room.RoomIndex == 0 && room.Team == CombatTeam.Player).Value == 4 &&
            preview.CardInstances!.All(card => card.Temporary.Upgrades.Count == 0), "Preview mutated shared hand upgrades.");
        Require(JsonSerializer.Serialize(source) == parent, "Conduit mutated its parent context.");
        Parallel.For(0, 16, _ => Require(JsonSerializer.Serialize(ConduitModel.Change(source, 0, unit, false)) ==
            JsonSerializer.Serialize(active), "Parallel Conduit branches diverged."));
        Console.WriteLine("CONDUIT-CHECKS PASS: eligibility, trait scaling, room selection, draw/discard, stack removal, death, preview and immutable parallel branches.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
