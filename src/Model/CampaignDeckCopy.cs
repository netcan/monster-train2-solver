using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CampaignCopyCard
    {
        public int Identity { get; }
        public string DataId { get; }
        public CardUpgradeMaskCard Mask { get; }
        public CardUpgradeMaskCard PermanentMask { get; }
        public CardModifiers Permanent { get; }
        public bool Purified { get; }
        public bool EventCached { get; }
        public bool EventCachedDuplicate { get; }
        public CampaignCopyCard(int identity, string dataId, CardUpgradeMaskCard mask, CardUpgradeMaskCard permanentMask, CardModifiers permanent,
            bool purified, bool eventCached, bool eventCachedDuplicate)
        { Identity = identity; DataId = dataId; Mask = mask; PermanentMask = permanentMask; Permanent = permanent; Purified = purified; EventCached = eventCached; EventCachedDuplicate = eventCachedDuplicate; }
    }
    public sealed class CampaignCopyResult
    {
        public IReadOnlyList<CampaignCopyCard> Deck { get; }
        public UnityRng Rng { get; }
        public int? SourceIdentity { get; }
        internal CampaignCopyResult(IReadOnlyList<CampaignCopyCard> deck, UnityRng rng, int? source) { Deck = deck; Rng = rng; SourceIdentity = source; }
    }
    public static class CampaignDeckCopyModel
    {
        // The permanent-copy interface ends before reward animation. The host
        // allocates a fresh logical identity; native CardState identity is a CLR
        // object reference, not a deterministic gameplay RNG draw.
        public static CampaignCopyResult EndBattle(IReadOnlyList<CampaignCopyCard> deck, UnityRng rng,
            bool owned, CardUpgradeMaskRule? filter, int nextIdentity)
        {
            if (deck.Any(card => card.Identity <= 0) || deck.Select(card => card.Identity).Distinct().Count() != deck.Count)
                throw new ArgumentException("Campaign deck needs unique card identities.");
            var candidates = owned ? deck.Where(card => filter == null || CardUpgradeMaskModel.FilterCard(filter, card.Mask)).ToArray() : Array.Empty<CampaignCopyCard>();
            if (candidates.Length == 0) return new CampaignCopyResult(deck, rng, null);
            if (nextIdentity <= 0 || deck.Any(card => card.Identity == nextIdentity)) throw new ArgumentException("Copy requires a fresh identity.");
            var selected = rng.Range(0, candidates.Length); var source = candidates[selected.Value];
            if (source.Permanent.ExternalInteractions.Count > 0) throw new ArgumentException("Copying merged/grafted/boxed/trait-replaced modifiers requires their permanent interface.");
            // Native SetIsEventCachedCardDuplicate writes isEventCachedCard in
            // this build. Preserve the observed flag, including its false
            // duplicate getter, rather than interpreting the method's name.
            var copy = new CampaignCopyCard(nextIdentity, source.DataId, source.PermanentMask, source.PermanentMask, source.Permanent, source.Purified, source.EventCached, false);
            return new CampaignCopyResult(Array.AsReadOnly(deck.Concat(new[] { copy }).ToArray()), selected.State, source.Identity);
        }
    }
}
