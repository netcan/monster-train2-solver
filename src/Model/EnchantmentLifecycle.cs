using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class EnchantmentStatus
    {
        public string Id { get; }
        public int Count { get; }
        public string? DisplayCategory { get; }
        public EnchantmentStatus(string id, int count, string? displayCategory)
        { Id = id; Count = count; DisplayCategory = displayCategory; }
        internal bool AffectedByDuality => DisplayCategory != null &&
            (Id == "armor" || DisplayCategory == "Positive" || DisplayCategory == "Negative");
    }

    public sealed class EnchantmentTarget
    {
        public int UnitId { get; }
        public bool IsEnchanted { get; }
        // Native values: zero = none, one = add, two = remove. Skipped targets retain this value.
        public int NextAction { get; }
        public EnchantmentTarget(int unitId, bool isEnchanted, int nextAction = 0)
        { UnitId = unitId; IsEnchanted = isEnchanted; NextAction = nextAction; }
    }

    public sealed class EnchantmentState
    {
        public IReadOnlyList<EnchantmentTarget> PrimaryTargets { get; }
        public IReadOnlyList<EnchantmentTarget> PreviewTargets { get; }
        public bool PreviewRequiresSync { get; }
        public EnchantmentStatus? CachedStatus { get; }
        public EnchantmentState(IReadOnlyList<EnchantmentTarget>? primaryTargets = null,
            IReadOnlyList<EnchantmentTarget>? previewTargets = null, bool previewRequiresSync = false,
            EnchantmentStatus? cachedStatus = null)
        {
            PrimaryTargets = Array.AsReadOnly((primaryTargets ?? Array.Empty<EnchantmentTarget>()).ToArray());
            PreviewTargets = Array.AsReadOnly((previewTargets ?? Array.Empty<EnchantmentTarget>()).ToArray());
            PreviewRequiresSync = previewRequiresSync; CachedStatus = cachedStatus;
            if (PrimaryTargets.Select(target => target.UnitId).Distinct().Count() != PrimaryTargets.Count ||
                PreviewTargets.Select(target => target.UnitId).Distinct().Count() != PreviewTargets.Count)
                throw new ArgumentException("An enchantment map cannot contain duplicate identities.");
        }
    }

    // Includes retained and unplaced actors, which are not necessarily present in the live train.
    public sealed class EnchantmentActor
    {
        public int Id { get; }
        public int RoomIndex { get; }
        public bool IsHero { get; }
        public bool IsAlive { get; }
        public bool IsDestroyed { get; }
        public bool IsDormant { get; }
        public bool Muted { get; }
        public bool Silenced { get; }
        public bool Duality { get; }
        public bool Undying { get; }
        public bool Preview { get; }
        public EnchantmentActor(int id, int roomIndex, bool isHero, bool isAlive, bool isDestroyed,
            bool isDormant, bool muted, bool silenced, bool duality, bool undying, bool preview)
        { Id = id; RoomIndex = roomIndex; IsHero = isHero; IsAlive = isAlive; IsDestroyed = isDestroyed;
            IsDormant = isDormant; Muted = muted; Silenced = silenced; Duality = duality; Undying = undying; Preview = preview; }
    }

    public sealed class EnchantmentInput
    {
        public string Operation { get; }
        public int SourceId { get; }
        public bool CanUpdate { get; }
        public bool Preview { get; }
        public UnityRng BattleRng { get; }
        public UnityRng TestRng { get; }
        public IReadOnlyList<EnchantmentStatus> StatusPool { get; }
        public IReadOnlyList<int> CollectedTargetIds { get; }
        public IReadOnlyList<EnchantmentActor> Actors { get; }
        public EnchantmentInput(string operation, int sourceId, bool canUpdate, bool preview, UnityRng battleRng, UnityRng testRng,
            IReadOnlyList<EnchantmentStatus> statusPool, IReadOnlyList<int> collectedTargetIds,
            IReadOnlyList<EnchantmentActor> actors)
        { Operation = operation; SourceId = sourceId; CanUpdate = canUpdate; Preview = preview; BattleRng = battleRng; TestRng = testRng;
            StatusPool = Array.AsReadOnly(statusPool.ToArray()); CollectedTargetIds = Array.AsReadOnly(collectedTargetIds.ToArray());
            Actors = Array.AsReadOnly(actors.ToArray()); }
    }

    // Requests at the native status API boundary. The combat status engine must apply each request
    // and its callbacks before this primitive is integrated into automatic train updates.
    public sealed class EnchantmentRequest
    {
        public int UnitId { get; }
        public string Operation { get; }
        public string StatusId { get; }
        public int Count { get; }
        public bool? SourceIsHero { get; }
        public bool? ShowNotification { get; }
        public string EffectType { get; }
        public bool HasSourceCard { get; }
        public bool HasSourceRelic { get; }
        public bool HasTriggeringCharacter { get; }
        public bool AllowModification { get; }
        public bool? Hidden { get; }
        public bool? AllowDualism { get; }
        public bool? OverrideImmunity { get; }
        public bool? SpawnEffect { get; }
        public bool? FromRoomModifier { get; }
        public bool? FromCardUpgrade { get; }
        public bool? RemoveAtEndOfTurn { get; }
        public EnchantmentState StateAtCall { get; }
        public EnchantmentRequest(int unitId, string operation, string statusId, int count,
            bool? sourceIsHero, bool? showNotification, string effectType, bool hasSourceCard, bool hasSourceRelic,
            bool hasTriggeringCharacter, bool allowModification, bool? hidden, bool? allowDualism,
            bool? overrideImmunity, bool? spawnEffect, bool? fromRoomModifier, bool? fromCardUpgrade,
            bool? removeAtEndOfTurn, EnchantmentState stateAtCall)
        { UnitId = unitId; Operation = operation; StatusId = statusId; Count = count; SourceIsHero = sourceIsHero;
            ShowNotification = showNotification; EffectType = effectType; HasSourceCard = hasSourceCard; HasSourceRelic = hasSourceRelic;
            HasTriggeringCharacter = hasTriggeringCharacter; AllowModification = allowModification; Hidden = hidden;
            AllowDualism = allowDualism; OverrideImmunity = overrideImmunity; SpawnEffect = spawnEffect; FromRoomModifier = fromRoomModifier;
            FromCardUpgrade = fromCardUpgrade; RemoveAtEndOfTurn = removeAtEndOfTurn; StateAtCall = stateAtCall; }
    }

    public sealed class EnchantmentTransition
    {
        public EnchantmentState State { get; }
        public UnityRng BattleRng { get; }
        public UnityRng TestRng { get; }
        public IReadOnlyList<EnchantmentRequest> Requests { get; }
        public EnchantmentTransition(EnchantmentState state, UnityRng battleRng, UnityRng testRng, IReadOnlyList<EnchantmentRequest> requests)
        { State = state; BattleRng = battleRng; TestRng = testRng; Requests = Array.AsReadOnly(requests.ToArray()); }
    }

    public static class EnchantmentLifecycleModel
    {
        internal sealed class PreparedUpdate
        {
            internal EnchantmentState State { get; }
            internal UnityRng BattleRng { get; }
            internal UnityRng TestRng { get; }
            internal bool SourceDuality { get; }
            internal IReadOnlyList<int> TargetOrder { get; }
            internal PreparedUpdate(EnchantmentState state, UnityRng battleRng, UnityRng testRng, bool sourceDuality,
                IReadOnlyList<int> targetOrder)
            { State = state; BattleRng = battleRng; TestRng = testRng; SourceDuality = sourceDuality;
                TargetOrder = Array.AsReadOnly(targetOrder.ToArray()); }
        }
        // Setup deliberately retains the cached status and pending preview-sync flag.
        public static EnchantmentState Setup(EnchantmentState source) => new EnchantmentState(
            previewRequiresSync: source.PreviewRequiresSync, cachedStatus: source.CachedStatus);
        public static EnchantmentState PrepareForPreview(EnchantmentState source) => new EnchantmentState(
            source.PrimaryTargets, source.PreviewTargets, true, source.CachedStatus);

        // Target collection is a separate native/model phase. This primitive consumes its ordered
        // result and independently reproduces status-pool selection, retained maps and API requests.
        // It does not yet perform status mutations, reentrant updates or queue draining.
        public static EnchantmentTransition Apply(EnchantmentState source, EnchantmentInput input)
        {
            if (input.Operation == "Setup") return new EnchantmentTransition(Setup(source), input.BattleRng, input.TestRng, Array.Empty<EnchantmentRequest>());
            if (input.Operation == "PrepareForPreview") return new EnchantmentTransition(PrepareForPreview(source), input.BattleRng, input.TestRng, Array.Empty<EnchantmentRequest>());
            if (input.Operation != "Update") throw new ArgumentException("Unknown enchantment operation: " + input.Operation);
            if (!input.CanUpdate) return new EnchantmentTransition(source, input.BattleRng, input.TestRng, Array.Empty<EnchantmentRequest>());

            PreparedUpdate prepared = Begin(source, input);
            EnchantmentState state = prepared.State;
            var actors = input.Actors.ToDictionary(actor => actor.Id);
            var requests = new List<EnchantmentRequest>();
            foreach (int id in prepared.TargetOrder)
            {
                EnchantmentRequest? request = Next(state, actors[id], input.Preview, prepared.SourceDuality, out state);
                if (request != null) requests.Add(request);
                state = Complete(state, actors[id], input.Preview);
            }
            return new EnchantmentTransition(state, prepared.BattleRng, prepared.TestRng, requests);
        }

        // A real status call can synchronously update other auras. Plan the entire map first,
        // then refresh its state and actor before each API call, and clear the action afterwards.
        internal static PreparedUpdate Begin(EnchantmentState source, EnchantmentInput input)
        {
            UnityRng rng = input.BattleRng;
            UnityRng testRng = input.TestRng;
            if (input.StatusPool.Count == 0)
                return new PreparedUpdate(new EnchantmentState(source.PrimaryTargets, source.PreviewTargets,
                    source.PreviewRequiresSync), rng, testRng, false, Array.Empty<int>());
            EnchantmentStatus status = input.StatusPool[0];
            if (input.StatusPool.Count > 1)
            {
                // RandomManager converts the requested Battle stream to BattleTest in preview.
                RngDraw draw = (input.Preview ? testRng : rng).Range(0, input.StatusPool.Count);
                if (input.Preview) testRng = draw.State; else rng = draw.State;
                status = input.StatusPool[draw.Value];
            }
            // Enchant calls GetStatusEffectStack with no targets or actor. Stack/HP/magic scaling,
            // chance/range/subtype fields and the regular AddStatus IgnoreDualism flag do not apply.
            Dictionary<int, EnchantmentActor> actors = input.Actors.ToDictionary(actor => actor.Id);
            if (!actors.TryGetValue(input.SourceId, out EnchantmentActor enchanter))
                throw new ArgumentException("A bound enchantment requires its retained source actor.");
            var primary = source.PrimaryTargets.ToList(); var preview = source.PreviewTargets.ToList();
            bool sync = source.PreviewRequiresSync;
            if (input.Preview && sync)
            {
                sync = false;
                foreach (EnchantmentTarget target in primary) Set(preview, target);
            }
            List<EnchantmentTarget> selected = input.Preview ? preview : primary;
            foreach (int id in input.CollectedTargetIds)
                if (id != input.SourceId && selected.All(target => target.UnitId != id))
                    selected.Add(new EnchantmentTarget(id, false));
            for (int i = 0; i < selected.Count; i++)
            {
                EnchantmentTarget entry = selected[i];
                if (!actors.TryGetValue(entry.UnitId, out EnchantmentActor target))
                    throw new ArgumentException("An enchantment requires retained target " + entry.UnitId + " for source " + input.SourceId + ".");
                bool valid = !enchanter.Muted && !enchanter.Silenced && !enchanter.IsDormant && !enchanter.IsDestroyed &&
                    enchanter.IsAlive && !target.IsDestroyed && target.IsAlive && target.Id != enchanter.Id &&
                    enchanter.RoomIndex == target.RoomIndex;
                selected[i] = new EnchantmentTarget(entry.UnitId, entry.IsEnchanted, valid ? 1 : 2);
            }
            return new PreparedUpdate(new EnchantmentState(primary, preview, sync, status), rng, testRng,
                enchanter.Duality, selected.Select(entry => entry.UnitId).ToArray());
        }
        internal static EnchantmentRequest? Next(EnchantmentState source, EnchantmentActor target, bool preview,
            bool sourceDuality, out EnchantmentState state)
        {
            state = source;
            if (Skip(target, preview)) return null;
            EnchantmentTarget entry = (preview ? source.PreviewTargets : source.PrimaryTargets).Single(item => item.UnitId == target.Id);
            bool add = entry.NextAction == 1 && !entry.IsEnchanted;
            bool remove = entry.NextAction == 2 && entry.IsEnchanted;
            if (!add && !remove) return null;
            EnchantmentStatus status = source.CachedStatus ?? throw new InvalidOperationException("An active update requires its selected status.");
            int count = sourceDuality && status.AffectedByDuality ? unchecked(status.Count * 2) : status.Count;
            state = Replace(source, preview, new EnchantmentTarget(target.Id, add, entry.NextAction));
            return new EnchantmentRequest(target.Id, add ? "Add" : "Remove", status.Id, count,
                add ? target.IsHero : (bool?)null, add ? (bool?)null : !preview, "CardEffectEnchant", false, false,
                false, true, add ? false : (bool?)null, add ? true : (bool?)null, add ? false : (bool?)null,
                add ? false : (bool?)null, add ? false : (bool?)null, add ? (bool?)null : false, add ? (bool?)null : false, state);
        }
        internal static EnchantmentState Complete(EnchantmentState source, EnchantmentActor target, bool preview)
        {
            if (Skip(target, preview)) return source;
            EnchantmentTarget entry = (preview ? source.PreviewTargets : source.PrimaryTargets).Single(item => item.UnitId == target.Id);
            return Replace(source, preview, new EnchantmentTarget(target.Id, entry.IsEnchanted));
        }
        private static bool Skip(EnchantmentActor actor, bool preview) => actor.IsDestroyed ||
            !actor.IsAlive && !actor.Undying || actor.Preview != preview;
        private static EnchantmentState Replace(EnchantmentState source, bool preview, EnchantmentTarget entry)
        {
            var selected = (preview ? source.PreviewTargets : source.PrimaryTargets).ToList(); Set(selected, entry);
            return new EnchantmentState(preview ? source.PrimaryTargets : selected,
                preview ? selected : source.PreviewTargets, source.PreviewRequiresSync, source.CachedStatus);
        }
        private static void Set(List<EnchantmentTarget> map, EnchantmentTarget value)
        { int index = map.FindIndex(entry => entry.UnitId == value.UnitId); if (index < 0) map.Add(value); else map[index] = value; }
    }
}
