using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitSummonChoice
    {
        public CombatUnit Unit { get; }
        public CardCreationRule? FallbackCreation { get; }
        public bool FallbackLookupComplete { get; }
        public UnitSummonChoice(CombatUnit unit, CardCreationRule? fallbackCreation = null, bool fallbackLookupComplete = false)
        { Unit = unit; FallbackCreation = fallbackCreation; FallbackLookupComplete = fallbackLookupComplete; }
    }

    public sealed class UnitSummonRule
    {
        public int Count { get; }
        public CardCreationRule Creation { get; }
        public CombatStatus CardlessStatus { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public bool IgnoreCardUpgrades { get; }
        public CardCreationRule? FallbackCreation { get; }
        public UnitSummonChoice? Additional { get; }
        public IReadOnlyList<UnitSummonChoice> Pool { get; }
        public int? NativeBaseSize { get; }
        public bool TriggersPaidRally { get; }
        public bool FallbackLookupComplete { get; }
        public UnitSummonRule(int count, CardCreationRule creation, CombatStatus cardlessStatus,
            CardUpgradeModifier? upgrade = null, bool ignoreCardUpgrades = false, CardCreationRule? fallbackCreation = null,
            UnitSummonChoice? additional = null, IReadOnlyList<UnitSummonChoice>? pool = null, int? nativeBaseSize = null,
            bool triggersPaidRally = true, bool fallbackLookupComplete = false)
        { Count = count; Creation = creation; CardlessStatus = cardlessStatus; Upgrade = upgrade; IgnoreCardUpgrades = ignoreCardUpgrades; FallbackCreation = fallbackCreation; Additional = additional;
            Pool = Array.AsReadOnly((pool ?? Array.Empty<UnitSummonChoice>()).ToArray()); NativeBaseSize = nativeBaseSize; TriggersPaidRally = triggersPaidRally; FallbackLookupComplete = fallbackLookupComplete; }
    }

    public sealed class UnitBirthResult
    {
        public RoomCombatResult Result { get; }
        public int UnitId { get; }
        public bool Supported => Result.Supported;
        internal UnitBirthResult(RoomCombatResult result, int unitId = 0) { Result = result; UnitId = unitId; }
    }

    public static class UnitBirthModel
    {
        public static UnitBirthResult Spawn(RoomCombatState source, CardPlayRule definition, int spawnerCardId,
            int position, bool isCardless, CombatStatus? cardlessStatus = null)
            => SpawnCore(source, definition, spawnerCardId, position, isCardless, cardlessStatus, null, null, true);
        internal static UnitBirthResult SpawnQueued(RoomCombatState source, CardPlayRule definition, int spawnerCardId,
            int position, bool isCardless, CombatStatus cardlessStatus, Action<RoomCombatModel.QueuedCharacterTrigger> enqueue,
            IReadOnlyList<CombatUnit> positionActors, bool selectedSlot = true)
            => SpawnCore(source, definition, spawnerCardId, position, isCardless, cardlessStatus, enqueue, positionActors, selectedSlot);
        private static UnitBirthResult SpawnCore(RoomCombatState source, CardPlayRule definition, int spawnerCardId,
            int position, bool isCardless, CombatStatus? cardlessStatus, Action<RoomCombatModel.QueuedCharacterTrigger>? enqueue,
            IReadOnlyList<CombatUnit>? positionActors, bool selectedSlot)
        {
            CombatContext? context = source.Context;
            CombatUnit? raw = definition.SpawnUnit;
            CardInstanceState? card = context?.FindCard(spawnerCardId);
            if (context == null || raw == null || raw.IsPyre || raw.Size < 0 || spawnerCardId < 0 ||
                spawnerCardId > 0 && card == null || isCardless && (cardlessStatus?.Id != "cardless" || cardlessStatus.Stacks != 1))
                return Unsupported("Unit birth requires its template, source reference and cardless metadata.");
            CombatUnit initialized = AbilityLifecycleModel.InitialAtSpawn(raw, card, context, out string? error);
            if (error != null) return Unsupported(error);
            CardPlayRule birthDefinition = card == null || card.DataId == definition.DataId ||
                definition.Summon?.IgnoreCardUpgrades != true ||
                definition.Summon.FallbackCreation?.DataId != card.DataId && definition.Summon.Additional?.FallbackCreation?.DataId != card.DataId &&
                !definition.Summon.Pool.Any(choice => choice.FallbackCreation?.DataId == card.DataId)
                ? definition : new CardPlayRule(card.DataId, definition.AssetKey,
                definition.Cost, definition.Effect, definition.Destination, definition.SpawnUnit, definition.ExternalInteractions,
                definition.Effects, definition.UpgradeInteractions, definition.HandDiscardInteractions, definition.HandConsumeInteractions,
                definition.CostType, definition.Equipment, definition.Ability, definition.Summon);
            CardPlayRule resolved = card == null ? birthDefinition.WithSpawn(initialized) : CardModifierModel.Resolve(birthDefinition.WithSpawn(initialized), card);
            if (resolved.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", resolved.ExternalInteractions));
            CombatUnit template = resolved.SpawnUnit!;
            error = UnitIdentityModel.Validate(context, source.Units);
            if (error != null) return Unsupported(error);
            UnitIdentityAllocation allocated = UnitIdentityModel.Allocate(context);
            if (!allocated.Supported) return Unsupported(allocated.UnsupportedReason!);
            context = allocated.Context!;
            var players = source.Units.Where(unit => unit.Team == CombatTeam.Player).ToList();
            if (context.SpawnPoints == null && (position < 0 || position > players.Count))
                return Unsupported("Unit birth has an invalid insertion position.");
            var spawned = new CombatUnit(allocated.UnitId, template.AssetKey, CombatTeam.Player, template.BaseAttack,
                template.Health, template.MaxHealth, template.CanAttack, false, false, isCardless ? template.Statuses.Concat(new[] { cardlessStatus! }).ToArray() : template.Statuses, template.Triggers,
                spawnerCardId, template.Size, template.StatusImmunities, template.Subtypes, template.Modifiers, template.IsBoss,
                template.LastAttackerId, template.StatusRegistry, template.EquipmentCards, template.NextTriggerId,
                template.Ability, template.StatusDictionary, template.AbilityRules, template.HordeDefinition, true,
                template.SacrificeCardId, template.DeathState);
            if (context.SpawnPoints != null)
            {
                var scope = new RoomCombatState(source.RoomIndex, source.Deployment, positionActors ?? source.Units, source.ExternalInteractions, context, source.Preview);
                var physical = BattleSpawnPointModel.Birth(context.SpawnPoints, scope, spawned, position, shift: selectedSlot);
                if (!physical.Supported) return Unsupported(physical.Error!);
                context = context.WithSpawnPoints(physical.State!);
                players.Add(spawned);
                players = BattleSpawnPointModel.Order(context.SpawnPoints, source.RoomIndex, players).ToList();
            }
            else players.Insert(position, spawned);
            context = context.WithStatistics(context.Statistics?.Spawn(source.RoomIndex, template.Subtypes));
            var entered = new RoomCombatState(source.RoomIndex, source.Deployment,
                source.Units.Where(unit => unit.Team == CombatTeam.Enemy).Concat(players).ToArray(), source.ExternalInteractions, context, source.Preview);
            IReadOnlyList<CombatStatus> starting = raw.StatusRegistry ?? raw.Statuses;
            foreach (CardModifiers group in card == null ? Array.Empty<CardModifiers>() : new[] { card.Permanent, card.Temporary })
                starting = StatusCallbackModel.MergeStartingStatuses(starting, group.Upgrades.SelectMany(upgrade => upgrade.Statuses));
            if (isCardless) starting = starting.Concat(new[] { cardlessStatus! }).ToArray();
            RoomCombatResult result = RoomCombatModel.ApplySpawnTriggers(entered, spawned.Id, spawnerCardId > 0, starting, enqueue);
            if (!result.Supported) return new UnitBirthResult(result);
            var events = result.Events.ToList();
            RoomOutcome outcome = result.Outcome;
            var queue = result.State!.Units.Where(unit => unit.Team == CombatTeam.Player)
                .Select(unit => new RoomCombatModel.QueuedCharacterTrigger(source.RoomIndex, unit, "AfterSpawnEnchant")).ToList();
            if (isCardless)
                queue.AddRange(result.State.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Id != spawned.Id)
                    .Select(unit => new RoomCombatModel.QueuedCharacterTrigger(source.RoomIndex, unit,
                        "CardMonsterPlayed", lastSpawnedOverrideUnitId: spawned.Id)));
            bool drained;
            if (enqueue != null) { foreach (var queued in queue) enqueue(queued); drained = true; }
            else drained = RoomCombatModel.DrainCharacterQueue(queue, queued =>
            {
                result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, queued, queue.Add);
                if (!result.Supported) return false;
                events.AddRange(result.Events);
                if (result.Outcome != RoomOutcome.Exchanged) outcome = result.Outcome;
                return true;
            }, queued => { result = RoomCombatModel.SettleQueuedSpawner(result.State!, queued.Unit); return result.Supported; });
            if (!drained) return new UnitBirthResult(result);
            RoomCombatState final = result.State!;
            final = new RoomCombatState(final.RoomIndex, final.Deployment, final.Units.Select(unit => unit.Id == spawned.Id
                ? HordeStatusModel.WithSpawning(unit, false) : unit).ToArray(), final.ExternalInteractions, final.Context, final.Preview);
            return new UnitBirthResult(new RoomCombatResult(final, outcome, 0, events), spawned.Id);
        }
        private static UnitBirthResult Unsupported(string error) => new UnitBirthResult(new RoomCombatResult(null,
            RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error));
    }

    internal static class UnitSummonModel
    {
        internal static UnitBirthResult Apply(RoomCombatState source, CardPlayRule definition, int cardId, int position, int slots)
        {
            UnitSummonRule rule = definition.Summon!;
            if (rule.IgnoreCardUpgrades && (rule.Pool.Count == 0 && rule.FallbackCreation == null && !rule.FallbackLookupComplete ||
                rule.Additional != null && rule.Additional.FallbackCreation == null && !rule.Additional.FallbackLookupComplete ||
                rule.Pool.Any(choice => choice.FallbackCreation == null && !choice.FallbackLookupComplete)))
                return Unsupported("Missing fallback spawner definition.");
            if (rule.IgnoreCardUpgrades && source.Context?.OtherPiles?.Any(pile => pile.Name == "Standby" && pile.UnitConditions != null) != true)
                return Unsupported("Fresh fallback summons require the original card's standby binding metadata.");
            if (source.Context?.FindCard(cardId)?.PlayedRoomUnitIds == null)
                return Unsupported("Repeated summons require the source card's native room cache.");
            RoomCombatState state = source;
            int requested = Math.Max(1, rule.Count);
            if (rule.Additional != null) requested = unchecked(requested + requested);
            int count = Math.Min(requested, Math.Max(0, slots - state.Units.Count(unit => unit.Team == CombatTeam.Player)));
            var events = new List<CombatEvent>();
            int firstId = 0;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            for (int index = 0; index < count && position + index < slots; index++)
            {
                int sourceId = cardId;
                if (!rule.IgnoreCardUpgrades && index > 0)
                {
                    CardGenerationResult clone = CardGenerationModel.CloneDetached(state.Context!, rule.Creation, cardId);
                    if (!clone.Supported) return Unsupported(clone.UnsupportedReason!);
                    sourceId = clone.AddedCards.Single().InstanceId;
                    state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions, clone.Context, state.Preview);
                }
                // Native selection uses the capped total, before the insertion-index loop limit.
                UnitSummonChoice? selected = null;
                if (rule.Pool.Count > 0)
                {
                    RngDraw draw = state.Context!.BattleRng.Range(0, rule.Pool.Count);
                    selected = rule.Pool[draw.Value];
                    state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions,
                        state.Context.WithBattleRng(draw.State), state.Preview);
                }
                if (rule.Additional != null && index >= count / 2) selected = rule.Additional;
                if (rule.IgnoreCardUpgrades)
                {
                    CardCreationRule? fallback = selected == null ? rule.FallbackCreation : selected.FallbackCreation;
                    sourceId = 0;
                    if (fallback != null)
                    {
                        CardGenerationResult created = CardGenerationModel.CreateDetached(state.Context!, fallback);
                        if (!created.Supported) return Unsupported(created.UnsupportedReason!);
                        sourceId = created.AddedCards.Single().InstanceId;
                        state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions, created.Context, state.Preview);
                    }
                }
                UnitBirthResult born = UnitBirthModel.Spawn(state, selected == null ? definition : definition.WithSpawn(selected.Unit),
                    sourceId, position + index, index > 0 || sourceId == 0, rule.CardlessStatus);
                if (!born.Supported) return born;
                if (firstId == 0) firstId = born.UnitId;
                state = born.Result.State!; events.AddRange(born.Result.Events);
                if (born.Result.Outcome != RoomOutcome.Exchanged) outcome = born.Result.Outcome;
                if (rule.Upgrade != null)
                {
                    RoomCombatResult upgraded = SpawnUpgradeModel.Apply(state, born.UnitId, sourceId, rule.Upgrade);
                    if (!upgraded.Supported) return new UnitBirthResult(upgraded);
                    state = upgraded.State!; events.AddRange(upgraded.Events);
                    if (upgraded.Outcome != RoomOutcome.Exchanged) outcome = upgraded.Outcome;
                }
            }
            return new UnitBirthResult(new RoomCombatResult(state, outcome, 0, events), firstId);
        }
        private static UnitBirthResult Unsupported(string error) => new UnitBirthResult(new RoomCombatResult(null,
            RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error));
    }

    public static class SpawnUpgradeModel
    {
        // SpawnMonster first runs the direct character API (including its callbacks),
        // then always writes the descriptor to the source's temporary modifiers.
        // A unique/capacity rejection on the unit does not skip that source write.
        public static RoomCombatResult Apply(RoomCombatState source, int unitId, int spawnerCardId, CardUpgradeModifier upgrade)
            => ApplyCore(source, unitId, spawnerCardId, upgrade, false);
        internal static RoomCombatResult ApplyQueued(RoomCombatState source, int unitId, int spawnerCardId, CardUpgradeModifier upgrade)
            => ApplyCore(source, unitId, spawnerCardId, upgrade, true);
        private static RoomCombatResult ApplyCore(RoomCombatState source, int unitId, int spawnerCardId, CardUpgradeModifier upgrade,
            bool deferCallbacks)
        {
            if (spawnerCardId < 0 || source.Context == null ||
                spawnerCardId > 0 && source.Context.FindCard(spawnerCardId) == null)
                return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), "Missing extra spawn upgrade source.");
            RoomCombatResult applied = RoomCombatModel.ApplyDirectUnitUpgrade(source, unitId, upgrade, false, "", null, deferCallbacks);
            if (!applied.Supported || source.Preview || spawnerCardId == 0) return applied;
            CardInstanceState card = applied.State!.Context!.FindCard(spawnerCardId)!;
            var changed = new CardInstanceState(card.InstanceId, card.DataId, card.Permanent,
                UnitModifierModel.Add(card.Temporary, upgrade), card.LastPlayedCost, card.LastForgedAmount, card.PlayCount,
                card.ExternalInteractions, card.EffectCounters, card.DamageScalingTraits, card.StatusScalingTraits,
                card.UnitUpgradeScalingTraits, card.CapacityScalingTraits, card.EquippedUnitId, card.PlayedRoomUnitIds, card.RawPlayedRoomUnitIds);
            var state = new RoomCombatState(applied.State.RoomIndex, applied.State.Deployment, applied.State.Units,
                applied.State.ExternalInteractions, applied.State.Context.WithCard(changed), applied.State.Preview);
            return new RoomCombatResult(state, applied.Outcome, applied.Rounds, applied.Events.ToList(),
                applied.UnsupportedReason, applied.PendingCallbacks, applied.RetainedUnits, applied.Dispatches);
        }
    }

}
