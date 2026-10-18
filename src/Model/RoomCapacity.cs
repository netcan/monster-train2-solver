using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomCapacityAttachmentState
    {
        public string DataId { get; }
        public string AssetKey { get; }
        public string AttachmentId { get; }
        public int AttachmentType { get; }
        public int CapacityDelta { get; }
        public IReadOnlyList<CardUpgradeModifier>? CharacterUpgrades { get; }
        public bool AffectsHeroes { get; }
        public bool AffectsMonsters { get; }
        public bool IgnoreBosses { get; }
        public int SpellCostDelta { get; }
        public RoomMoonPhaseRule? MoonPhase { get; }
        public int EnergyPerTurnDelta { get; }
        public IReadOnlyList<CardUpgradeMaskRule>? CharacterFilters { get; }
        public RoomPostCombatRule? PostCombat { get; }
        public RoomPreCombatRule? PreCombat { get; }
        public RoomCapacityAttachmentState(string dataId, string assetKey, int attachmentType, int capacityDelta,
            IReadOnlyList<CardUpgradeModifier>? characterUpgrades = null, bool affectsHeroes = false,
            bool affectsMonsters = true, bool ignoreBosses = false, string attachmentId = "", int spellCostDelta = 0,
            RoomMoonPhaseRule? moonPhase = null, int energyPerTurnDelta = 0, IReadOnlyList<CardUpgradeMaskRule>? characterFilters = null,
            RoomPostCombatRule? postCombat = null, RoomPreCombatRule? preCombat = null)
        {
            DataId = dataId; AssetKey = assetKey; AttachmentId = attachmentId; AttachmentType = attachmentType; CapacityDelta = capacityDelta;
            CharacterUpgrades = characterUpgrades == null ? null : Array.AsReadOnly(characterUpgrades.ToArray());
            AffectsHeroes = affectsHeroes; AffectsMonsters = affectsMonsters; IgnoreBosses = ignoreBosses; SpellCostDelta = spellCostDelta; MoonPhase = moonPhase;
            EnergyPerTurnDelta = energyPerTurnDelta;
            CharacterFilters = characterFilters == null ? null : Array.AsReadOnly(characterFilters.ToArray());
            PostCombat = postCombat;
            PreCombat = preCombat;
        }
    }

    public sealed class RoomCapacityState
    {
        public int RoomIndex { get; }
        public int PlayerMaximum { get; }
        public int EnemyMaximum { get; }
        public bool IsPyre { get; }
        public int? Corruption { get; }
        public int? MaxCorruption { get; }
        public int? PermanentCorruption { get; }
        public IReadOnlyList<RoomCapacityAttachmentState>? Attachments { get; }
        public int? AttachmentsAddedFromCards { get; }
        public RoomCapacityState(int roomIndex, int playerMaximum, int enemyMaximum, bool isPyre, int? corruption = null,
            IReadOnlyList<RoomCapacityAttachmentState>? attachments = null, int? maxCorruption = null, int? permanentCorruption = null,
            int? attachmentsAddedFromCards = null)
        { RoomIndex = roomIndex; PlayerMaximum = playerMaximum; EnemyMaximum = enemyMaximum; IsPyre = isPyre; Corruption = corruption;
            MaxCorruption = maxCorruption; PermanentCorruption = permanentCorruption;
            Attachments = attachments == null ? null : Array.AsReadOnly(attachments.ToArray()); AttachmentsAddedFromCards = attachmentsAddedFromCards; }
    }
    public sealed class ScalingCapacityTrait
    {
        public CardStatisticQuery Query { get; }
        public int CapacityPerStat { get; }
        public ScalingCapacityTrait(CardStatisticQuery query, int capacityPerStat)
        { Query = query; CapacityPerStat = capacityPerStat; }
    }
    public sealed class RoomCapacityResult
    {
        public CombatContext? Context { get; }
        public int Amount { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal RoomCapacityResult(CombatContext? context, int amount, string? error = null)
        { Context = context; Amount = amount; UnsupportedReason = error; }
    }
    public static class RoomCapacityModel
    {
        public static bool SupportsCharacterStatus(string id, int stacks) =>
            (id is "multistrike" or "silenced" or "endless") && stacks == 1 || id == "conduit" && stacks == 5;
        public static string? ValidateCharacterFilters(IReadOnlyList<CardUpgradeMaskRule>? filters) =>
            filters?.Any(filter => filter.Statuses.Required.Count > 0 || filter.Statuses.Excluded.Count > 0 ||
                filter.RequiredSizes.Count > 0 || filter.ExcludedSizes.Count > 0 ||
                filter.Subtypes.RequiredOperator is < 0 or > 1 || filter.Subtypes.ExcludedOperator is < 0 or > 1) == true
                ? "Unmodeled room attachment character filter." : null;
        internal static int EnergyPerTurnDelta(CombatContext context) => context.RoomCapacities?
            .Sum(room => room.Attachments?.Sum(attachment => attachment.EnergyPerTurnDelta) ?? 0) ?? 0;
        internal static int SpellCostDelta(CombatContext? context, int roomIndex) =>
            context?.RoomCapacities?.FirstOrDefault(room => room.RoomIndex == roomIndex)?.Attachments?
                .Sum(attachment => attachment.SpellCostDelta) ?? 0;

        internal static int SpellPlayedCostModifier(CombatContext? context, int roomIndex) =>
            context?.RoomCapacities?.FirstOrDefault(room => room.RoomIndex == roomIndex)?.Attachments?
                .Sum(attachment => Math.Abs(attachment.SpellCostDelta)) ?? 0;

        public static string? Validate(CombatContext? context)
        {
            if (context?.RoomCapacities == null) return "Missing captured room capacity state.";
            return context.RoomCapacities.Any(room => room.RoomIndex < 0 || room.PlayerMaximum < 0 || room.EnemyMaximum < 0 || room.Corruption < 0 || room.AttachmentsAddedFromCards < 0) ||
                context.RoomCapacities.Select(room => room.RoomIndex).Distinct().Count() != context.RoomCapacities.Count ||
                context.RoomCapacities.Any(room => room.Attachments?.Any(attachment => string.IsNullOrEmpty(attachment.DataId) ||
                    string.IsNullOrEmpty(attachment.AssetKey) || attachment.AttachmentType != 0 ||
                    attachment.SpellCostDelta != 0 && attachment.SpellCostDelta != -1 ||
                    attachment.EnergyPerTurnDelta != 0 && attachment.EnergyPerTurnDelta != 1 ||
                    ValidateCharacterFilters(attachment.CharacterFilters) != null ||
                    attachment.MoonPhase != null && RoomMoonPhaseModel.Validate(attachment.MoonPhase) != null ||
                    attachment.PostCombat != null && (attachment.MoonPhase != null || RoomPostCombatModel.Validate(attachment.PostCombat) != null) ||
                    attachment.PreCombat != null && (attachment.MoonPhase != null || attachment.PostCombat != null || RoomPreCombatModel.Validate(attachment.PreCombat) != null) ||
                    attachment.AttachmentId.Length > 0 && (!Guid.TryParse(attachment.AttachmentId, out Guid attachmentGuid) || attachmentGuid == Guid.Empty) ||
                    attachment.CharacterUpgrades?.Any(upgrade => upgrade.ExternalInteractions.Count > 0 ||
                        upgrade.Stats.Health != 0 || upgrade.Stats.Size != 0 ||
                        upgrade.Statuses.Any(status => !SupportsCharacterStatus(status.Id, status.Stacks)) || upgrade.TriggerUpgrades?.Count > 0 && attachment.AttachmentId.Length == 0 ||
                        upgrade.AbilityUpgrade != null) == true) == true)
                ? "Invalid room capacity state." : null;
        }
        public static int? Maximum(CombatContext? context, int room, CombatTeam team) =>
            context?.RoomCapacities?.FirstOrDefault(item => item.RoomIndex == room) is RoomCapacityState capacity
                ? team == CombatTeam.Player ? capacity.PlayerMaximum : capacity.EnemyMaximum : (int?)null;
        public static bool Test(RoomCombatState? room, CardActionEffect effect) => room != null &&
            Test(room.Context, room.RoomIndex, room.Units.Any(unit => unit.Team == CombatTeam.Enemy), effect.OnlyIfNoEnemies);

        public static bool TestAttachment(RoomCombatState? room, CardActionEffect effect)
        {
            if (room == null || room.Preview || effect.RoomAttachment?.AttachmentType != 0) return false;
            RoomCapacityState? capacity = room.Context?.RoomCapacities?.FirstOrDefault(item => item.RoomIndex == room.RoomIndex);
            return capacity?.IsPyre == false && capacity.Attachments != null;
        }

        public static RoomCapacityResult Attach(CombatContext context, int roomIndex, RoomCapacityAttachmentState attachment)
        {
            string? error = Validate(context);
            if (error != null) return new RoomCapacityResult(null, 0, error);
            RoomCapacityState? selected = context.RoomCapacities!.FirstOrDefault(room => room.RoomIndex == roomIndex);
            if (selected == null || selected.IsPyre || selected.Attachments == null || attachment.AttachmentType != 0)
                return new RoomCapacityResult(null, 0, "Invalid capacity attachment target or type.");
            RoomCapacityAttachmentState[] attachments = selected.Attachments?.ToArray() ?? Array.Empty<RoomCapacityAttachmentState>();
            int? attachmentsAddedFromCards = selected.AttachmentsAddedFromCards.HasValue
                ? unchecked(selected.AttachmentsAddedFromCards.Value + 1) : null;
            RoomCapacityAttachmentState? previous = attachments.FirstOrDefault(item => item.AttachmentType == 0);
            if (previous != null)
                attachments = attachments.Where(item => !ReferenceEquals(item, previous)).ToArray();
            int maximum = selected.PlayerMaximum;
            if (previous != null) maximum = AdjustedMaximum(maximum, unchecked(-previous.CapacityDelta));
            maximum = AdjustedMaximum(maximum, attachment.CapacityDelta);
            var changed = new RoomCapacityState(roomIndex, maximum, selected.EnemyMaximum, selected.IsPyre,
                selected.Corruption, attachments.Concat(new[] { attachment }).ToArray(),
                selected.MaxCorruption, selected.PermanentCorruption,
                attachmentsAddedFromCards);
            return new RoomCapacityResult(context.WithRoomCapacities(context.RoomCapacities!
                .Select(room => room.RoomIndex == roomIndex ? changed : room).ToArray()), attachment.CapacityDelta);
        }
        internal static bool Test(CombatContext? context, int room, bool enemiesPresent, bool onlyIfNoEnemies) =>
            context?.RoomCapacities?.FirstOrDefault(item => item.RoomIndex == room)?.IsPyre == false && (!onlyIfNoEnemies || !enemiesPresent);
        public static int AdjustedMaximum(int maximum, int amount)
        {
            if (amount < 0 && maximum <= 1 || amount > 0 && maximum >= 30) return maximum;
            return Math.Max(1, Math.Min(30, unchecked(maximum + amount)));
        }
        public static RoomCapacityResult Apply(CombatContext context, int roomIndex, CardActionEffect effect, int sourceCardId = 0)
        {
            string? error = Validate(context);
            if (error != null) return new RoomCapacityResult(null, 0, error);
            RoomCapacityState? selected = context.RoomCapacities!.FirstOrDefault(room => room.RoomIndex == roomIndex);
            if (selected == null || selected.IsPyre) return new RoomCapacityResult(null, 0, "Missing playable capacity room.");
            int amount = effect.Value;
            if (sourceCardId > 0)
            {
                CardInstanceState? owner = context.FindCard(sourceCardId);
                if (owner == null) return new RoomCapacityResult(null, 0, "Missing capacity-trait owner.");
                foreach (ScalingCapacityTrait trait in owner.CapacityScalingTraits ?? Array.Empty<ScalingCapacityTrait>())
                {
                    StatisticQueryResult statistic = StatisticQueryModel.Evaluate(context, trait.Query, sourceCardId);
                    if (!statistic.Supported) return new RoomCapacityResult(null, 0, statistic.UnsupportedReason);
                    context = statistic.Context!; amount = unchecked(amount + trait.CapacityPerStat * statistic.Value);
                }
            }
            // Native resolves scaling statistics before selecting the effect's room,
            // and selects that room even when capacity bounds refuse the adjustment.
            if (context.QueryFrame?.SelectedRoomIndex != null)
                context = context.WithQueryFrame(context.QueryFrame.With(selectedRoomIndex: roomIndex));
            // Native exact Heroes selects enemies; None and combined team flags select players.
            bool enemy = effect.AllowEnemy && !effect.AllowPlayer;
            var changed = new RoomCapacityState(roomIndex,
                enemy ? selected.PlayerMaximum : AdjustedMaximum(selected.PlayerMaximum, amount),
                enemy ? AdjustedMaximum(selected.EnemyMaximum, amount) : selected.EnemyMaximum, selected.IsPyre,
                selected.Corruption, selected.Attachments, selected.MaxCorruption, selected.PermanentCorruption,
                selected.AttachmentsAddedFromCards);
            return new RoomCapacityResult(context.WithRoomCapacities(context.RoomCapacities!
                .Select(room => room.RoomIndex == roomIndex ? changed : room).ToArray()), amount);
        }
    }
}
