using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Private modifier state survives removal from native room enumeration.
    public sealed class RetainedConduitState
    {
        public CombatTeam Team { get; }
        public ConduitUnitState Contributor { get; }
        public RetainedConduitState(CombatTeam team, ConduitUnitState contributor)
        { Team = team; Contributor = contributor; }
    }
    public sealed class ConduitUnitState
    {
        public int UnitId { get; }
        public int Power { get; }
        public bool Active { get; }
        public IReadOnlyList<int> Cards { get; }
        public IReadOnlyList<int> MultipliedCards { get; }
        public ConduitUnitState(int unitId, int power, bool active, IReadOnlyList<int> cards, IReadOnlyList<int> multipliedCards)
        {
            UnitId = unitId; Power = power; Active = active;
            Cards = Array.AsReadOnly(cards.ToArray());
            MultipliedCards = Array.AsReadOnly(multipliedCards.OrderBy(id => id).ToArray());
        }
    }
    public sealed class ConduitRoomState
    {
        public CardUpgradeModifier Upgrade { get; }
        public IReadOnlyList<ConduitUnitState> Units { get; }
        public ConduitRoomState(CardUpgradeModifier upgrade, IReadOnlyList<ConduitUnitState> units)
        { Upgrade = upgrade; Units = Array.AsReadOnly(units.ToArray()); }
    }
    internal static class ConduitModel
    {
        private static string Id(int unitId) => "conduit:" + unitId;
        internal static bool Captured(CombatContext? context) => context?.MagicPower?.Any(room => room.Conduit != null) == true;
        internal static string? Validate(CombatContext? context)
        {
            if (!Captured(context)) return null;
            if (context!.QueryFrame?.SelectedRoomIndex == null || context.CardInstances == null)
                return "Conduit requires captured room selection and card instances.";
            if (context.SpawnPoints != null && context.ConduitRetained == null)
                return "Conduit physical movement requires complete retained tracker capture.";
            foreach (var room in context.MagicPower!)
            {
                if (room.Conduit == null) return "Conduit requires complete room/team contributor state.";
                var upgrade = room.Conduit.Upgrade;
                if (upgrade.AssetKey != "ConduitUpgrade" || upgrade.ExternalInteractions.Count != 0 ||
                    upgrade.CloneDamageBase.HasValue || upgrade.CloneHealBase.HasValue || upgrade.Lifecycle == null)
                    return "Unmodeled Conduit upgrade definition.";
            }
            if (context.CardInstances.Any(card => card.MaskDescriptor?.Definition.CanScaleWithMagicPower == null))
                return "Conduit requires captured card magic-power eligibility.";
            return null;
        }
        private static CombatContext Put(CombatContext context, int roomIndex, CombatTeam team, IReadOnlyList<ConduitUnitState> units)
        {
            return context.WithMagicPower(context.MagicPower!.Select(room =>
            {
                if (room.RoomIndex != roomIndex || room.Team != team || room.Conduit == null) return room;
                int before = room.Conduit.Units.Where(unit => unit.Active).Sum(unit => unit.Power);
                int after = units.Where(unit => unit.Active).Sum(unit => unit.Power);
                return new RoomMagicPower(roomIndex, team, unchecked(room.Value - before + after),
                    new ConduitRoomState(room.Conduit.Upgrade, units));
            }).ToArray());
        }
        private static CombatContext Clear(CombatContext context, int roomIndex, CombatTeam team, int unitId, int? cardId = null)
        {
            var room = context.MagicPower!.Single(value => value.RoomIndex == roomIndex && value.Team == team);
            var unit = room.Conduit!.Units.Single(value => value.UnitId == unitId);
            foreach (int id in unit.Cards.Where(id => !cardId.HasValue || id == cardId.Value))
            {
                var card = context.FindCard(id);
                if (card == null) continue;
                var temporary = new CardModifiers(card.Temporary.Offsets,
                    card.Temporary.Upgrades.Where(upgrade => upgrade.DataId != Id(unitId)).ToArray(),
                    card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions);
                context = context.WithCard(card.WithModifiers(card.Permanent, temporary));
            }
            return Put(context, roomIndex, team, room.Conduit.Units.Select(value => value.UnitId != unitId ? value :
                new ConduitUnitState(unitId, value.Power, value.Active,
                    value.Cards.Where(id => cardId.HasValue && id != cardId.Value).ToArray(),
                    value.MultipliedCards.Where(id => cardId.HasValue && id != cardId.Value).ToArray())).ToArray());
        }
        private static CombatContext Add(CombatContext context, int roomIndex, CombatTeam team, int unitId, IEnumerable<int> cards)
        {
            var room = context.MagicPower!.Single(value => value.RoomIndex == roomIndex && value.Team == team);
            var unit = room.Conduit!.Units.Single(value => value.UnitId == unitId);
            var tracked = unit.Cards.ToList(); var multiplied = unit.MultipliedCards.ToList();
            foreach (int id in cards)
            {
                var card = context.FindCard(id);
                if (tracked.Contains(id) || card?.MaskDescriptor?.Definition.CanScaleWithMagicPower != true) continue;
                var existing = card.Temporary.Upgrades.FirstOrDefault(upgrade => upgrade.DataId == Id(unitId));
                if (existing == null)
                {
                    int multiplier = CardMagicPowerModel.Multiplier(card);
                    int amount = unit.Power;
                    if (multiplier > 1 && !multiplied.Contains(id))
                    { amount = unchecked(amount * multiplier); multiplied.Add(id); }
                    var template = room.Conduit.Upgrade.WithRuntimeIdentity(Id(unitId));
                    var stats = template.Stats;
                    var upgrade = template.WithStats(new CardStatModifier(amount, stats.Health, stats.Cost, amount,
                        stats.Size, stats.XCost, stats.EquipmentLimit, stats.UpgradeSlotCount));
                    context = context.WithCard(card.WithModifiers(card.Permanent, UnitModifierModel.Add(card.Temporary, upgrade)));
                }
                tracked.Add(id);
            }
            return Put(context, roomIndex, team, room.Conduit.Units.Select(value => value.UnitId == unitId ?
                new ConduitUnitState(unitId, unit.Power, unit.Active, tracked, multiplied) : value).ToArray());
        }
        internal static CombatContext Select(CombatContext context, int? before, int? after)
        {
            context = RoomSpellUpgradeModel.Select(context, before, after);
            if (!Captured(context) || before == after) return context;
            foreach (var room in context.MagicPower!.ToArray())
            foreach (var unit in room.Conduit!.Units)
            {
                if (room.RoomIndex == before) context = Clear(context, room.RoomIndex, room.Team, unit.UnitId);
                if (room.RoomIndex == after) context = ApplyHand(context, room.RoomIndex, room.Team, unit.UnitId);
            }
            return context;
        }
        private static CombatContext ApplyHand(CombatContext context, int roomIndex, CombatTeam team, int unitId)
        {
            foreach (var token in context.Cards.Hand)
            {
                var row = context.MagicPower!.Single(room => room.RoomIndex == roomIndex && room.Team == team);
                if (row.Conduit!.Units.Single(unit => unit.UnitId == unitId).Cards.Contains(token.InstanceId)) continue;
                context = Add(context, roomIndex, team, unitId, [token.InstanceId]);
                // Native RefreshCardInHand redraws the UI; it does not dispatch
                // CardUpdated. That callback belongs to trait scaling/hand entry.
            }
            return context;
        }
        private static (CombatContext Context, ConduitUnitState Contributor) MoveCapturedContributor(
            CombatContext context, ConduitUnitState contributor, CombatTeam team,
            SpawnPointReference? from, SpawnPointReference? to, bool dead, bool preview)
        {
            if (!Captured(context))
                throw new InvalidOperationException("Conduit movement requires captured trackers.");
            string? error = Validate(context);
            if (error != null) throw new InvalidOperationException(error);
            foreach (RoomMagicPower row in context.MagicPower!.ToArray())
                if (row.Conduit!.Units.Any(unit => unit.UnitId == contributor.UnitId))
                    context = Put(context, row.RoomIndex, row.Team,
                        row.Conduit.Units.Where(unit => unit.UnitId != contributor.UnitId).ToArray());
            int holder = to?.RoomIndex ?? from?.RoomIndex ?? 0;
            RoomMagicPower destination = context.MagicPower!.Single(row => row.RoomIndex == holder && row.Team == team);
            context = Put(context, holder, team, destination.Conduit!.Units.Append(contributor).ToArray());
            int selected = context.QueryFrame!.SelectedRoomIndex!.Value;
            if (!preview)
            {
                if (to?.RoomIndex == selected) context = ApplyHand(context, holder, team, contributor.UnitId);
                else if (from?.RoomIndex == selected || dead) context = Clear(context, holder, team, contributor.UnitId);
            }
            RoomMagicPower updated = context.MagicPower!.Single(row => row.RoomIndex == holder && row.Team == team);
            ConduitUnitState result = updated.Conduit!.Units.Single(unit => unit.UnitId == contributor.UnitId);
            // Native room enumeration excludes dead actors even while their
            // private modifier and current point still exist.
            if (to == null || !contributor.Active) context = Put(context, holder, team,
                updated.Conduit.Units.Where(unit => unit.UnitId != contributor.UnitId).ToArray());
            if (context.ConduitRetained != null)
            {
                var retained = context.ConduitRetained.Where(value => value.Contributor.UnitId != contributor.UnitId);
                if (to == null || !contributor.Active) retained = retained.Append(new RetainedConduitState(team, result));
                context = context.WithConduitRetained(retained.ToArray());
            }
            return (context, result);
        }
        // Resolve the modifier entirely from the parent snapshot, including
        // detached/dead actors omitted from room contribution enumeration.
        internal static CombatContext MovePhysicalContributor(CombatContext context, int unitId, CombatTeam team,
            SpawnPointReference? from, SpawnPointReference? to, bool preview = false)
        {
            if (context.ConduitRetained == null)
                throw new InvalidOperationException("Conduit movement requires captured retained private trackers.");
            if (SamePoint(from, to)) return context;
            ConduitUnitState? contributor = context.MagicPower?.Where(row => row.Team == team)
                .SelectMany(row => row.Conduit?.Units ?? Array.Empty<ConduitUnitState>())
                .Concat(context.ConduitRetained.Where(value => value.Team == team).Select(value => value.Contributor))
                .SingleOrDefault(value => value.UnitId == unitId);
            if (contributor == null) return context;
            return MoveCapturedContributor(context, contributor, team, from, to, !contributor.Active, preview).Context;
        }
        internal static CombatContext Physical(CombatContext before, CombatContext after)
        {
            if (!Captured(after) || after.ConduitRetained == null || before.SpawnPoints == null || after.SpawnPoints == null)
                return after;
            if (after.ConduitRetained.Count == 0 && !after.MagicPower!.Any(row => row.Conduit!.Units.Count > 0)) return after;
            CombatContext original = after;
            bool preview = before.SpawnPoints.ActiveCopies.Count > 0 || after.SpawnPoints.ActiveCopies.Count > 0;
            foreach (UnitSpawnPointState unit in after.SpawnPoints.Units)
            {
                SpawnPointReference? previous = before.SpawnPoints.Units.FirstOrDefault(value => value.UnitId == unit.UnitId)?.Current;
                if (SamePoint(previous, unit.Current)) continue;
                CombatTeam team = unit.Current?.Team ?? previous?.Team ?? CombatTeam.Player;
                after = MovePhysicalContributor(after, unit.UnitId, team, previous, unit.Current, preview);
            }
            if (ReferenceEquals(after, original)) return after;
            foreach (RoomMagicPower row in after.MagicPower!.ToArray())
            {
                if (row.Conduit!.Units.Count <= 1) continue;
                SpawnPointGroupState? group = after.SpawnPoints.SelectedGroup(row.RoomIndex, row.Team);
                if (group == null) continue;
                int Position(int id)
                {
                    for (int index = 0; index < group.Occupants.Count; index++) if (group.Occupants[index] == id) return index;
                    return int.MaxValue;
                }
                var ordered = row.Conduit.Units.OrderBy(value => Position(value.UnitId)).ToArray();
                if (!ordered.SequenceEqual(row.Conduit.Units)) after = Put(after, row.RoomIndex, row.Team, ordered);
            }
            return after;
        }
        private static bool SamePoint(SpawnPointReference? first, SpawnPointReference? second) => first == null
            ? second == null : second != null && first.RoomIndex == second.RoomIndex && first.Team == second.Team &&
                first.Index == second.Index && first.PreviewCopyId == second.PreviewCopyId &&
                first.OuterBossAnchor == second.OuterBossAnchor;
        internal static CombatContext Updated(CombatContext context, int cardId)
        {
            context = RoomSpellUpgradeModel.Updated(context, cardId);
            context = Drawn(context, [cardId]);
            if (!Captured(context)) return context;
            foreach (var room in context.MagicPower!.Where(room => room.RoomIndex == context.QueryFrame?.SelectedRoomIndex).ToArray())
            foreach (var unit in room.Conduit!.Units.Where(unit => unit.Cards.Contains(cardId)))
            {
                context = Clear(context, room.RoomIndex, room.Team, unit.UnitId, cardId);
                context = Add(context, room.RoomIndex, room.Team, unit.UnitId, [cardId]);
            }
            return context;
        }
        internal static CombatContext Drawn(CombatContext context, IEnumerable<int> cards)
        {
            context = RoomSpellUpgradeModel.Drawn(context, cards);
            if (!Captured(context)) return context;
            int? selected = context.QueryFrame?.SelectedRoomIndex;
            foreach (var room in context.MagicPower!.Where(room => room.RoomIndex == selected).ToArray())
            foreach (var unit in room.Conduit!.Units)
                context = Add(context, room.RoomIndex, room.Team, unit.UnitId, cards);
            return context;
        }
        internal static CombatContext Discarded(CombatContext context, int cardId)
        {
            context = RoomSpellUpgradeModel.Discarded(context, cardId);
            if (!Captured(context)) return context;
            foreach (var room in context.MagicPower!.Where(room => room.RoomIndex == context.QueryFrame?.SelectedRoomIndex).ToArray())
            foreach (var unit in room.Conduit!.Units.Where(unit => unit.Cards.Contains(cardId)))
                context = Clear(context, room.RoomIndex, room.Team, unit.UnitId, cardId);
            return context;
        }
        internal static CombatContext Change(CombatContext context, int roomIndex, CombatUnit unit, bool preview, bool remove = false,
            bool refreshSelection = true, IReadOnlyList<int>? order = null)
        {
            if (!Captured(context)) return context;
            var row = context.MagicPower!.Single(room => room.RoomIndex == roomIndex && room.Team == unit.Team);
            var previous = row.Conduit!.Units.FirstOrDefault(value => value.UnitId == unit.Id);
            if (previous != null && !preview) context = Clear(context, roomIndex, unit.Team, unit.Id);
            row = context.MagicPower!.Single(room => room.RoomIndex == roomIndex && room.Team == unit.Team);
            var units = row.Conduit!.Units.Where(value => value.UnitId != unit.Id).ToList();
            if (!remove) units.Add(new ConduitUnitState(unit.Id, unit.Status("conduit")?.Stacks ?? 0,
                unit.Health > 0, [], []));
            if (order != null) units = units.OrderBy(value => order.ToList().IndexOf(value.UnitId)).ToList();
            context = Put(context, roomIndex, unit.Team, units);
            if (!remove && !preview)
            {
                context = ApplyHand(context, roomIndex, unit.Team, unit.Id);
                // OnStacksAdded re-applies tracking before the room's selection
                // callback clears it again for an unselected room.
                if (refreshSelection)
                    foreach (var contributorRoom in context.MagicPower!.Where(room => room.RoomIndex == roomIndex).ToArray())
                    foreach (var contributor in contributorRoom.Conduit!.Units)
                        context = context.QueryFrame?.SelectedRoomIndex == roomIndex
                            ? ApplyHand(context, roomIndex, contributorRoom.Team, contributor.UnitId)
                            : Clear(context, roomIndex, contributorRoom.Team, contributor.UnitId);
            }
            return context;
        }
        internal static CombatContext Alive(CombatContext context, int roomIndex, CombatUnit unit)
        {
            if (!Captured(context)) return context;
            var row = context.MagicPower!.Single(room => room.RoomIndex == roomIndex && room.Team == unit.Team);
            if (row.Conduit!.Units.All(value => value.UnitId != unit.Id)) return context;
            return Put(context, roomIndex, unit.Team, row.Conduit.Units.Select(value => value.UnitId == unit.Id ?
                new ConduitUnitState(value.UnitId, value.Power, unit.Health > 0, value.Cards, value.MultipliedCards) : value).ToArray());
        }
        internal static CombatContext? Order(CombatContext? context, int roomIndex, IReadOnlyList<CombatUnit> units)
        {
            if (!Captured(context)) return context;
            var positions = units.Select((unit, index) => (unit.Id, index)).ToDictionary(item => item.Id, item => item.index);
            foreach (var row in context!.MagicPower!.Where(room => room.RoomIndex == roomIndex).ToArray())
                context = Put(context, roomIndex, row.Team, row.Conduit!.Units.OrderBy(unit =>
                    positions.TryGetValue(unit.UnitId, out int position) ? position : int.MaxValue).ToArray());
            return context;
        }
    }
}
