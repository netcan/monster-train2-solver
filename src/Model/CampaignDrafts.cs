using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CampaignDraftCard
    {
        public string DataId { get; }
        public int ClassType { get; }
        public int Rarity { get; }
        public CampaignDraftCard(string dataId, int classType, int rarity)
        { DataId = dataId; ClassType = classType; Rarity = rarity; }
    }

    public sealed class CampaignDraftResult
    {
        public IReadOnlyList<string> Cards { get; }
        public UnityRng Rng { get; }
        internal CampaignDraftResult(IReadOnlyList<string> cards, UnityRng rng)
        { Cards = Array.AsReadOnly(cards.ToArray()); Rng = rng; }
    }

    // Bounded campaign consumer: flattened Common..Rare drafts from an ordered
    // pool of already unlocked, DLC-eligible original cards. The UI choice/grant
    // and run rarity-ticket buckets are separate consumers.
    public static class CampaignDraftModel
    {
        public static int OverrideClass(int classes, int replace, int replacement) =>
            (classes & replace) != 0 ? (classes & ~replace) | replacement : classes;

        public static CampaignDraftResult Select(IReadOnlyList<CampaignDraftCard> pool, int classes,
            int floor, int count, UnityRng rng, bool alliedOnly = false, bool rareOnly = false,
            bool ignoreRarityOverride = false, int? explicitClass = null)
        {
            if (classes < 1 || classes > 7 || floor < 0 || floor > 2 || count < 1 || count > 3 ||
                explicitClass.HasValue && explicitClass != 1 && explicitClass != 2 ||
                pool.Any(card => card.Rarity < 0 || card.Rarity > 2 || card.ClassType != 1 && card.ClassType != 2 && card.ClassType != 4) ||
                pool.Select(card => card.DataId).Distinct().Count() != pool.Count)
                throw new ArgumentException("Unsupported flattened campaign draft inputs.");
            bool exact = rareOnly && !ignoreRarityOverride;
            if (exact) floor = 2;
            if (!explicitClass.HasValue && alliedOnly) classes = OverrideClass(classes, 1, 2);
            var primary = new List<CampaignDraftCard>();
            if (explicitClass.HasValue) primary.AddRange(pool.Where(card => card.ClassType == explicitClass));
            else
                foreach (int type in new[] { 1, 2 })
                    if ((classes & type) != 0) primary.AddRange(pool.Where(card => card.ClassType == type));
            // Native DraftRewardData always supplies the neutral fallback pool,
            // including when NonClass is absent from the authored class flags.
            var candidates = primary.Concat(pool.Where(card => card.ClassType == 4))
                .Where(card => exact ? card.Rarity == floor : card.Rarity >= floor).ToArray();
            var selected = new List<string>();
            for (int index = 0; index < count; index++)
            {
                RngDraw rarity = rng.Range(floor, 3); rng = rarity.State;
                for (int level = rarity.Value; level >= 0; level--)
                {
                    var choices = candidates.Where(card => card.Rarity == level && !selected.Contains(card.DataId)).ToArray();
                    if (choices.Length == 0) continue;
                    RngDraw choice = rng.Range(0, choices.Length); rng = choice.State;
                    selected.Add(choices[choice.Value].DataId); break;
                }
            }
            return new CampaignDraftResult(selected, rng);
        }

        public static (int RawHealth, int Maximum) AcquireTowerHealth(int rawHealth, int maximum, int additional)
        {
            int nextMaximum = unchecked(maximum + additional);
            int visible = Math.Min(nextMaximum, Math.Max(0, rawHealth));
            int nextHealth = additional > 0 ? Math.Min(nextMaximum, Math.Max(0, unchecked(visible + additional))) : rawHealth;
            return (nextHealth, nextMaximum);
        }
    }
}
