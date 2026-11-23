using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class TriggeredStatusScaling
    {
        public string? ActorStatus { get; }
        public bool MissingHealth { get; }
        public bool MagicPower { get; }
        public bool IgnoreDualism { get; }
        public string Subtype { get; }
        public int? HealthChoicePercent { get; }
        public bool HealthChoiceAbove { get; }
        public TriggeredStatusScaling(string? actorStatus = null, bool missingHealth = false, bool magicPower = false,
            bool ignoreDualism = false, string subtype = "", int? healthChoicePercent = null, bool healthChoiceAbove = false)
        { ActorStatus = actorStatus; MissingHealth = missingHealth; MagicPower = magicPower; IgnoreDualism = ignoreDualism; Subtype = subtype;
            HealthChoicePercent = healthChoicePercent; HealthChoiceAbove = healthChoiceAbove; }
    }

    internal static class TriggeredStatusModel
    {
        internal const string OtherStatusEffect = "CardEffectAddStatusEffectPerOtherEffect";
        internal const string OtherStatusAction = "AddStatusPerOtherEffect";
        internal const string HealthPercentStatusEffect = "CardEffectAddStatusEffectDependingOnHealthPercent";
        internal const string OriginalSpurned = "Boss_SoulSavior_R4_Spurned_ArmorPerDebuff";
        internal const string OriginalSpurnedWitherbloom = "Boss_SoulSavior_R4_Spurned_WitherbloomOnAction";
        internal const string OriginalSpurnedDuality = "Boss_SoulSavior_R4_Spurned_DualismAt50";

        internal static bool MatchesSpurnedAttack(CombatUnit actor, CombatTrigger trigger)
        {
            if (trigger.Kind != "OnAttacking" || trigger.Effects.Count == 0 ||
                !MatchesOtherStatus(trigger.Effects[0], "LastAttackedCharacter", RoomCombatModel.Witherbloom, 1))
                return false;
            return actor.AssetKey switch
            {
                OriginalSpurned => trigger.Effects.Count == 2 && MatchesOtherStatus(trigger.Effects[1], "Self", "armor", 3),
                OriginalSpurnedWitherbloom or OriginalSpurnedDuality => trigger.Effects.Count == 1 && trigger.RemoveOnRelentlessChange,
                _ => false
            };
        }

        internal static bool MatchesSpurnedTurnBegin(CombatUnit actor, CombatTrigger trigger) =>
            trigger.Kind == "OnTurnBegin" && !trigger.Once && !trigger.IgnoreSilence && trigger.RemoveOnRelentlessChange &&
            trigger.Effects.Count == 2 &&
            MatchesFixedRoomStatus(trigger.Effects[0], StatusAttackDamageModel.Malaise, 2, bothTeams: false) &&
            (actor.AssetKey == OriginalSpurnedWitherbloom
                ? MatchesFixedRoomStatus(trigger.Effects[1], RoomCombatModel.Witherbloom, 1, bothTeams: true)
                : actor.AssetKey == OriginalSpurnedDuality && MatchesSpurnedHealthPercent(trigger.Effects[1]));

        private static bool MatchesSpurnedHealthPercent(CombatEffect effect) =>
            effect.Type == HealthPercentStatusEffect &&
            effect.StatusScaling is { HealthChoicePercent: 50, HealthChoiceAbove: false, ActorStatus: null,
                MissingHealth: false, MagicPower: false, IgnoreDualism: false, Subtype.Length: 0 } &&
            effect.Action is { Type: "AddStatus", Target: "Self", Value: 50, AllowPlayer: true, AllowEnemy: true,
                Range: null, CountStatusOnlyOnTargets: false, Statuses.Count: 2 } action &&
            action.Statuses[0] is { Id: StatusAttackDamageModel.Malaise, Stacks: 0 } &&
            action.Statuses[1] is { Id: "duality", Stacks: 1 } &&
            action.Tests is { ShouldTest: true, FailToCast: false, CancelSubsequent: false, StrictTargets: false,
                MustHaveValidTargets: false, DevotionRequirement: 0, DevotionRequirementMax: 0 } &&
            HasNoTargetFilters(action.Filters);

        private static bool MatchesFixedRoomStatus(CombatEffect effect, string statusId, int stacks, bool bothTeams) =>
            effect.Type == "CardEffectAddStatusEffect" && NoStatusScaling(effect.StatusScaling) &&
            effect.Action is { Type: "AddStatus", Target: "Room", Value: 0, AllowPlayer: true, Range: null, Statuses.Count: 1 } action &&
            action.AllowEnemy == bothTeams && action.Statuses[0].Id == statusId && action.Statuses[0].Stacks == stacks &&
            action.Tests is { StrictTargets: false, DevotionRequirement: 0, DevotionRequirementMax: 0 } &&
            HasNoTargetFilters(action.Filters);

        private static bool MatchesOtherStatus(CombatEffect effect, string target, string output, int multiplier) =>
            effect.Type == OtherStatusEffect && NoStatusScaling(effect.StatusScaling) &&
            effect.Action is CardActionEffect action && action.Type == OtherStatusAction && action.Target == target &&
            action.AllowEnemy && action.AllowPlayer && action.CountStatusOnlyOnTargets && action.Value == multiplier &&
            action.Range == null && action.Statuses.Count == 2 && action.Statuses[0].Id == output &&
            action.Statuses[1].Id == StatusAttackDamageModel.Malaise && action.Statuses.All(status => status.Stacks == 1) &&
            action.Tests is { ShouldTest: true, FailToCast: false, CancelSubsequent: false, StrictTargets: false,
                CanPlayAfterBossDead: true, MustHaveValidTargets: false, DevotionRequirement: 0, DevotionRequirementMax: 0 } &&
            HasNoTargetFilters(action.Filters);

        private static bool NoStatusScaling(TriggeredStatusScaling? scaling) => scaling is { ActorStatus: null,
            MissingHealth: false, MagicPower: false, IgnoreDualism: false, Subtype.Length: 0, HealthChoicePercent: null, HealthChoiceAbove: false };

        private static bool HasNoTargetFilters(CardTargetFilters? filters) => filters is { Health: "Both",
            SelfHealthFilter: true, IgnoreBosses: false, Subtype.Length: 0, RequiredStatuses.Count: 0,
            ExcludedStatuses.Count: 0, ExcludedSubtypes.Count: 0 };

        internal static string? Validate(CombatEffect effect, bool observedInfestedPreview = false)
        {
            CardActionEffect action = effect.Action!;
            if (action.Statuses.Count == 0) return "Triggered status requires a nonempty status pool.";
            if (action.Statuses.Any(status => !RoomCombatModel.KnowsStatus(status.Id) &&
                !(observedInfestedPreview && status.Id == HivemindTurnUpgrade.StatusId))) return "Unmodeled triggered status definition.";
            if (effect.Type == OtherStatusEffect &&
                !MatchesOtherStatus(effect, "LastAttackedCharacter", RoomCombatModel.Witherbloom, 1) &&
                !MatchesOtherStatus(effect, "Self", "armor", 3))
                return "Unmodeled status-per-other triggered payload.";
            if (effect.Type == DevotionStatusModel.NativeEffect &&
                (action.Type != DevotionStatusModel.Effect || action.Target != "Self" || action.Statuses.Count != 1 ||
                action.Value != 0 || action.Range != null || action.DevotionLevelsPerGrant == null || effect.StatusScaling != null))
                return "Devotion status triggers require a fixed single Self status and captured level divisor.";
            if (effect.Type == "CardEffectMultiplyStatusEffect")
            {
                TriggeredStatusScaling? scaling = effect.StatusScaling;
                if (action.Type != "AddStatus" || action.Target != "LastAttackedCharacter" || !action.AllowPlayer || action.AllowEnemy ||
                    action.Value != 0 || action.Range != null || action.Statuses.Count != 1 ||
                    action.Statuses[0].Id != "corruption" || action.Statuses[0].Stacks != 2 || action.Tests?.StrictTargets == true ||
                    scaling?.ActorStatus != null || scaling?.MissingHealth == true || scaling?.MagicPower == true ||
                    scaling?.IgnoreDualism == true || (scaling?.Subtype.Length ?? 0) != 0 || scaling?.HealthChoicePercent != null)
                    return "Triggered status multiplication requires the original fixed Corrupter attack payload.";
            }
            if (effect.Type == "CardEffectAddStatusEffectBasedOnHealth")
            {
                var health = effect.StatusScaling;
                if (health?.HealthChoicePercent == null || health.HealthChoicePercent < 0 || health.HealthChoicePercent > 100 ||
                    health.ActorStatus != null || health.MissingHealth || health.MagicPower ||
                    health.IgnoreDualism || health.Subtype.Length != 0 || action.Range != null || action.Statuses.Count > 2)
                    return "Unsupported health-dependent status definition.";
                return null;
            }
            if (effect.Type == HealthPercentStatusEffect)
                return MatchesSpurnedHealthPercent(effect) ? null : "Unmodeled Spurned health-percent status payload.";
            if (effect.StatusScaling?.HealthChoicePercent != null) return "Health-dependent status metadata on another effect.";
            if (effect.Type == FriendlyDamageStatusModel.NativeEffect && action.Range != null)
                return "Damage-taken status scaling does not support range initialization.";
            if (effect.Type == "CardEffectAddStatusEffectPerLastAttackDamage" && action.Range != null)
                return "Last-attack status scaling does not use an integer range.";
            if (action.Tests?.StrictTargets == true || action.Target == "DropTargetCharacter")
            {
                if (action.Statuses.Any(status => status.Stackable == null)) return "Strict status legality requires stackability definitions.";
                if (action.Statuses.Count > 1 && action.Statuses.Any(status => status.Stackable == false))
                    return "Random nonstackable legality requires the native BattleTest stream.";
                if (CardTargetModel.IsRandom(action.Target) && action.Statuses.Any(status => status.Stackable == false))
                    return "Random nonstackable targets require the native BattleTest stream.";
            }
            return null;
        }

        internal static bool Test(RoomCombatState room, CombatEffect effect, IReadOnlyList<int> targets, out string? error, CombatStatus? testedStatus = null)
        {
            error = null; CardActionEffect action = effect.Action!;
            if (effect.Type == DevotionStatusModel.NativeEffect && DevotionStatusModel.Grants(room.Context, action) < 1) return false;
            if (effect.Type == "CardEffectAddStatusEffectBasedOnHealth")
                return room.Units.Any(unit => targets.Contains(unit.Id) && HealthChoice(unit.Health, unit.MaxHealth,
                    effect.StatusScaling!.HealthChoicePercent!.Value, effect.StatusScaling.HealthChoiceAbove) < action.Statuses.Count);
            // Stackable pools share legality, so their test-stream selection cannot change gameplay.
            if (action.Tests?.StrictTargets != true && action.Target != "DropTargetCharacter") return true;
            if (targets.Count == 0) return false;
            CombatStatus status = testedStatus ?? action.Statuses[0];
            if (status.Stackable == true) return true;
            foreach (int id in targets)
            {
                CombatUnit? target = room.Units.FirstOrDefault(unit => unit.Id == id);
                if (target == null || target.Status(status.Id)?.Stacks > 0) continue;
                string subtype = effect.StatusScaling?.Subtype ?? "";
                if (subtype.Length > 0 && !target.Subtypes.Contains(subtype)) continue;
                if (action.Filters?.IgnoreBosses == true)
                {
                    if (!target.IsBoss.HasValue) { error = "Status legality requires captured boss state."; return false; }
                    if (target.IsBoss.Value) continue;
                }
                return true;
            }
            return false;
        }

        // The engine supplies a fixed collection and retains dying actor/target snapshots.
        // Native TestEffect selects on BattleTest; only actual application advances Battle here.
        internal static RoomCombatResult Apply(RoomCombatState source, int actorId, CombatEffect effect, IReadOnlyList<int> targetIds,
            int? sourceCardId = null, bool isFromHiddenTrigger = false, int? damageActorId = null,
            bool observedInfestedPreview = false)
        {
            string? error = Validate(effect, observedInfestedPreview);
            CombatUnit? actor = source.Units.FirstOrDefault(unit => unit.Id == actorId);
            if (error != null || actor == null || source.Context == null || targetIds.Any(id => source.Units.All(unit => unit.Id != id)))
                return Unsupported(error ?? "Triggered status requires captured actor, targets and battle context.");
            CardActionEffect action = effect.Action!;
            if (effect.Type is "CardEffectAddStatusEffectBasedOnHealth" or HealthPercentStatusEffect)
            {
                if (effect.Type == HealthPercentStatusEffect &&
                    (actor.AssetKey != OriginalSpurnedDuality || targetIds.Any(id => id != actorId)))
                    return Unsupported("The original Spurned health-percent status requires its Self target.");
                return ApplyHealthChoice(source, actor, effect, targetIds, sourceCardId, isFromHiddenTrigger);
            }
            bool perFriendlyDamage = effect.Type == FriendlyDamageStatusModel.NativeEffect;
            bool perSelfAttack = effect.Type == "CardEffectAddStatusEffectPerSelfAttack";
            bool perLastAttack = effect.Type == "CardEffectAddStatusEffectPerLastAttackDamage";
            bool perOther = effect.Type == OtherStatusEffect;
            if (perLastAttack && source.Context.Statistics == null)
                return Unsupported("Last-attack status scaling requires captured battle statistics.");
            if (!perOther && source.Preview && source.Context.IsolatedBattlePreview != true &&
                (!perSelfAttack && !perLastAttack && !perFriendlyDamage && (action.Range != null || action.Value != 0 && action.Value < 100) ||
                    action.Statuses.Count > 1 || CardTargetModel.IsRandom(action.Target)))
                return Unsupported("Randomized status previews require the explicit isolated preview RNG protocol.");
            CombatContext context = source.Context;
            CombatStatus status = action.Statuses[0];
            if (effect.Type == DevotionStatusModel.NativeEffect)
            {
                int grants = DevotionStatusModel.Grants(context, action);
                if (grants < 1) return new RoomCombatResult(source, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
                status = status.WithStacks(unchecked(status.Stacks * grants));
            }
            if (perOther)
            {
                if (targetIds.Count != 1) return Unsupported("Status-per-other attack needs its one captured target.");
                status = StatusScalingModel.CountOtherStatus(action,
                    source.Units.Where(unit => unit.Health > 0 && unit.Id == targetIds[0]));
            }
            else if (action.Statuses.Count > 1)
            {
                RngDraw choice = context.BattleRng.Range(0, action.Statuses.Count);
                context = context.WithBattleRng(choice.State); status = action.Statuses[choice.Value];
            }
            TriggeredStatusScaling? scaling = effect.StatusScaling;
            CombatUnit? first = targetIds.Count == 0 ? null : source.Units.First(unit => unit.Id == targetIds[0]);
            bool multiplied = scaling?.ActorStatus != null || first != null && (scaling?.MissingHealth == true || scaling?.MagicPower == true);
            if (multiplied)
            {
                int multiplier = scaling?.ActorStatus == null ? 0 : actor.Status(scaling.ActorStatus)?.Stacks ?? 0;
                if (first != null && scaling?.MissingHealth == true) multiplier = unchecked(multiplier + Math.Max(0, first.MaxHealth - first.Health));
                if (first != null && scaling?.MagicPower == true)
                {
                    RoomMagicPower[]? powers = context.MagicPower?.Where(room => room.RoomIndex == source.RoomIndex && room.Team == first.Team).ToArray();
                    if (powers?.Length != 1) return Unsupported("Status scaling requires one captured room/team magic power value.");
                    multiplier = unchecked(multiplier + powers[0].Value);
                }
                status = status.WithStacks(unchecked(status.Stacks * multiplier));
            }
            if (perSelfAttack)
            {
                int divisor = action.Value == 0 ? 1 : action.Value;
                int mageblade = 0;
                if (actor.Modifiers?.MagebladeDamagePerMagicPower is int coefficient)
                {
                    RoomMagicPower? power = context.MagicPower?.FirstOrDefault(value =>
                        value.RoomIndex == source.RoomIndex && value.Team == actor.Team);
                    mageblade = unchecked(coefficient * (power?.Value ?? 0));
                }
                int pyre = actor.IsPyre ? context.Resources?.PyreModifiedBaseAttack ?? 0 : 0;
                int attack = UnitAttackOverrideModel.Resolve(actor.Statuses, actor.MaxHealth) ?? RelicLateAttackModel.Apply(actor, unchecked(actor.AttackBeforeStatuses + actor.AttackFromStatuses +
                    RelicStatusAttackModel.AdditionalAttack(actor.Modifiers?.StatusAttackModifiers, actor.Team, actor.Statuses) +
                    UnitSelfAttackModel.Additional(actor, context) + RoomHealthAttackModel.Additional(actor, context, source) +
                    UnitRoomStatusModel.AdditionalAttack(source, actor) + mageblade + pyre));
                status = status.WithStacks(attack / divisor);
            }
            if (perLastAttack)
            {
                // Native GetStatValue refreshes deckStats even for this scalar.
                if (context.Statistics!.StoredCards != null)
                {
                    var query = StatisticQueryModel.Evaluate(context, new CardStatisticQuery("LastAttackDamageDealt"));
                    if (!query.Supported) return Unsupported(query.UnsupportedReason!);
                    context = query.Context!;
                }
                status = status.WithStacks(context.Statistics!.LastAttackDamageDealt / (action.Value == 0 ? 1 : action.Value));
            }
            if (perFriendlyDamage)
            {
                CombatUnit? damaged = damageActorId.HasValue ? source.Units.FirstOrDefault(unit => unit.Id == damageActorId.Value) : actor;
                if (damaged?.DamageHistory == null) return Unsupported("Damage-taken status scaling requires the damaged unit's captured hit history.");
                int damage = damaged.DamageHistory.DamageTakenBeforeArmor;
                // The native status-pool choice precedes the zero-damage guard.
                if (damage <= 0) return new RoomCombatResult(WithContext(source, context), RoomOutcome.Exchanged, 0, new List<CombatEvent>());
                int stacks = unchecked(damage * status.Stacks) / (action.Value == 0 ? 1 : action.Value);
                if (stacks <= 0) return new RoomCombatResult(WithContext(source, context), RoomOutcome.Exchanged, 0, new List<CombatEvent>());
                status = status.WithStacks(stacks);
            }
            bool duality = effect.Type != "CardEffectMultiplyStatusEffect" && !isFromHiddenTrigger &&
                scaling?.IgnoreDualism != true && actor.Status("duality")?.Stacks > 0;
            if (duality && status.Id != "armor" && status.DisplayCategory == null)
                return Unsupported("Triggered Duality requires the captured status display category.");
            int chance = perSelfAttack || perLastAttack || perFriendlyDamage || perOther ? 0 : action.Value;
            if (!perSelfAttack && !perLastAttack && !perFriendlyDamage && action.Range != null)
            {
                RngDraw draw = action.Range.Sample(context.BattleRng); context = context.WithBattleRng(draw.State); chance = draw.Value;
            }
            RoomCombatState state = WithContext(source, context); var events = new List<CombatEvent>();
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (int id in perFriendlyDamage ? targetIds.AsEnumerable() : targetIds.Reverse())
            {
                // Guaranteed chances still consume the native preview's test
                // stream, but never the live Battle stream. A captured native
                // enchantment world retains that stream even for a fixed result.
                // Isolated previews model their mirrored stream separately.
                if (chance >= 100 && source.Preview && source.Context.IsolatedBattlePreview != true &&
                    state.Context!.Enchantments is { AutomaticLifecycle: true } world)
                    state = WithContext(state, state.Context.WithEnchantments(world.WithTestRng(world.TestRng.Range(0, 100).State)));
                if (chance != 0 && !(source.Preview && source.Context.IsolatedBattlePreview != true && chance >= 100))
                {
                    RngDraw draw = state.Context!.BattleRng.Range(0, 100);
                    state = WithContext(state, state.Context.WithBattleRng(draw.State));
                    if (draw.Value >= chance) continue;
                }
                CombatUnit before = state.Units.First(unit => unit.Id == id);
                CombatStatus incoming = status;
                bool multiply = effect.Type == "CardEffectMultiplyStatusEffect";
                if (multiply)
                {
                    int current = before.Status(status.Id)?.Stacks ?? 0;
                    int product = unchecked(current * status.Stacks);
                    int delta = unchecked(product - current);
                    // Native only adds a positive difference. Overflow and zero
                    // stacks do not remove statuses or publish callbacks.
                    if (product <= 0 || delta <= 0) continue;
                    incoming = status.WithStacks(delta);
                }
                RoomCombatResult added = StatusApplicationModel.ApplyRetained(state, id, incoming, sourceCardId ?? actor.SpawnerCardId,
                    overrideImmunity: action.Target == "Pyre", fromEffectType: multiply || perOther ? effect.Type : null,
                    doubleForDuality: duality && AffectedByDuality(incoming));
                if (!added.Supported) return added;
                callbacks.AddRange(added.PendingCallbacks);
                state = added.State!;
                CombatUnit changed = state.Units.First(unit => unit.Id == id);
                // Captures and target filters expose only positive native status counts.
                changed = CardSpellModel.Copy(changed, changed.Health, changed.Statuses.Where(item => item.Stacks > 0).ToArray());
                state = new RoomCombatState(state.RoomIndex, state.Deployment, state.Units.Select(unit => unit.Id == id ? changed : unit).ToArray(),
                    state.ExternalInteractions, state.Context, state.Preview, state.ActiveCompanionBossIds);
                events.Add(new CombatEvent(0, "TriggeredStatus:" + status.Id, actorId, id,
                    (changed.Status(status.Id)?.Stacks ?? 0) - (before.Status(status.Id)?.Stacks ?? 0)));
            }
            return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, events, pendingCallbacks: callbacks);
        }
        private static RoomCombatState WithContext(RoomCombatState source, CombatContext context) =>
            source.WithContext(context);
        internal static int HealthChoice(int health, int maximum, int percent, bool above)
        {
            int threshold = (int)Math.Ceiling((float)percent / 100f * maximum);
            return (above ? health >= threshold : health < threshold) ? 0 : 1;
        }
        private static bool AffectedByDuality(CombatStatus status) => status.Id == "armor" ||
            status.DisplayCategory is "Positive" or "Negative";

        private static RoomCombatResult ApplyHealthChoice(RoomCombatState source, CombatUnit actor, CombatEffect effect,
            IReadOnlyList<int> targets, int? sourceCardId, bool isFromHiddenTrigger)
        {
            var state = source; var events = new List<CombatEvent>();
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (int id in targets)
            {
                var target = state.Units.First(unit => unit.Id == id);
                int percent = effect.StatusScaling!.HealthChoicePercent!.Value;
                bool healthPercent = effect.Type == HealthPercentStatusEffect;
                int selected = healthPercent ? ((float)target.Health / Math.Max(1, target.MaxHealth) > percent / 100f ? 0 : 1) :
                    HealthChoice(target.Health, target.MaxHealth, percent, effect.StatusScaling.HealthChoiceAbove);
                if (selected >= effect.Action!.Statuses.Count) continue;
                var status = effect.Action.Statuses[selected];
                bool duality = !isFromHiddenTrigger && actor.Status("duality")?.Stacks > 0;
                if (duality && status.Id != "armor" && status.DisplayCategory == null)
                    return Unsupported("Triggered Duality requires the captured status display category.");
                var added = StatusApplicationModel.ApplyRetained(state, id, status, healthPercent ? 0 : sourceCardId ?? actor.SpawnerCardId,
                    doubleForDuality: duality && AffectedByDuality(status));
                if (!added.Supported) return added;
                state = added.State!; callbacks.AddRange(added.PendingCallbacks);
                events.Add(new CombatEvent(0, "TriggeredStatus:" + status.Id, actor.Id, id,
                    (state.Units.First(unit => unit.Id == id).Status(status.Id)?.Stacks ?? 0) - (target.Status(status.Id)?.Stacks ?? 0)));
            }
            return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, events, pendingCallbacks: callbacks);
        }
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
