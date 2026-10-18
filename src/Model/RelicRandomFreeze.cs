using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicRandomFreeze
    {
        public int Count { get; }
        public int ConditionCount { get; }
        public bool CanApplyInPreviewMode { get; }
        public RelicRandomFreeze(int count, int conditionCount, bool canApplyInPreviewMode)
        { Count = count; ConditionCount = conditionCount; CanApplyInPreviewMode = canApplyInPreviewMode; }
    }
    internal static class RelicRandomFreezeModel
    {
        internal const string Effect = "RelicEffectFreezeRandomCards";
        internal const string DataId = "c07618e6-b679-4e2c-87a2-9203f52989cf";
        internal const string AssetKey = "FreezeRandomEndOfTurn";
        internal static string? Validate(CombatRelicState owner, RelicCardModifier modifier) =>
            owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == true &&
            owner.EffectTypes.SequenceEqual(new[] { Effect }) && modifier.EffectIndex == 0 && modifier.Rule == null && modifier.Upgrade == null &&
            modifier.TraitReplacement == null && modifier.NoUpgradeCost == null && modifier.MoonPhaseHandUpgrade == null &&
            modifier.FreezeHandUpgrade == null && !modifier.ApplyToCardlessSpawns && modifier.Conditions.Count == 0 && modifier.ConditionCount == 0 &&
            modifier.RandomFreeze?.Count == 1 && modifier.RandomFreeze.ConditionCount == 0 && !modifier.RandomFreeze.CanApplyInPreviewMode
                ? null : "Missing or changed original FreezeRandomEndOfTurn definition.";
        internal static int[]? Candidates(CombatContext context, out string? error)
        {
            error = null; var result = new List<int>();
            foreach (CardToken token in context.Cards.Hand)
            {
                CardInstanceState? card = context.FindCard(token.InstanceId);
                if (card?.MaskDescriptor == null || card.DataId != token.DataId)
                { error = "Random freeze requires matching owned hand-card traits and types."; return null; }
                if (card.MaskDescriptor.Definition.CardType is "Blight" or "Junk") continue;
                try { if (CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitFreeze")) continue; }
                catch (InvalidOperationException failure) { error = failure.Message; return null; }
                result.Add(card.InstanceId);
            }
            return result.ToArray();
        }
        internal static RelicMoonPhaseHandUpgradeResult Apply(CombatContext source, bool deployment, bool preview)
        {
            CombatContext context = source;
            foreach (CombatRelicState owner in source.Relics ?? Array.Empty<CombatRelicState>())
            {
                if (!owner.EffectTypes.Contains(Effect)) continue;
                RelicCardModifier? modifier = owner.CardModifiers?.SingleOrDefault();
                string? error = modifier == null ? "Missing random-freeze rule." : Validate(owner, modifier);
                if (error != null) return new(context, error);
                if (deployment || preview) continue;
                int[]? candidates = Candidates(context, out error);
                if (error != null) return new(context, error);
                if (candidates!.Length == 0) continue;
                ShuffleResult<int> shuffled = context.BattleRng.Shuffle(candidates);
                context = context.WithBattleRng(shuffled.State);
                foreach (int id in shuffled.Items.Take(modifier!.RandomFreeze!.Count))
                {
                    CardInstanceState card = context.FindCard(id)!; CardMaskDescriptor descriptor = card.MaskDescriptor!;
                    CardInstanceState frozen = card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                        descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft,
                        descriptor.TemporaryTraits.Concat(new[] { new CardTraitValue("CardTraitFreeze", "CardTraitFreeze", 0, 0, true, 0) }).ToArray(),
                        descriptor.PermanentReplacements));
                    try { context = CardMagicPowerModel.Refresh(context, frozen, out _); }
                    catch (InvalidOperationException failure) { return new(context, failure.Message); }
                }
            }
            return new(context);
        }
        internal static TrainCombatResult ApplySelected(TrainCombatState source)
        {
            if (source.Context == null) return new(null, RoomOutcome.Unsupported, [], "Missing random-freeze context.");
            var result = Apply(source.Context, false, false);
            if (!result.Supported) return new(null, RoomOutcome.Unsupported, [], result.UnsupportedReason);
            return new(new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units, room.ExternalInteractions, result.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, result.Context), RoomOutcome.Exchanged, []);
        }
    }
}
