using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class AbilityCardCacheEntry
    {
        public string DataId { get; }
        public int InstanceId { get; }
        public AbilityCardCacheEntry(string dataId, int instanceId) { DataId = dataId; InstanceId = instanceId; }
    }

    public sealed class AbilityCardResult
    {
        public CombatContext? Context { get; }
        public CardInstanceState? Card { get; }
        public bool Created { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal AbilityCardResult(CombatContext? context, CardInstanceState? card, bool created, string? error = null)
        { Context = context; Card = card; Created = created; UnsupportedReason = error; }
    }

    // This cache owns references, but never owns a card in the player's deck or piles.
    public static class AbilityCardModel
    {
        public static string? Validate(CombatContext source)
        {
            if (source.AbilityCardCache == null) return null; // Historical captures did not observe the cache.
            if (source.CardRegistry == null) return "Ability cache requires a captured card registry.";
            if (source.AbilityCardCache.Select(entry => entry.DataId).Distinct().Count() != source.AbilityCardCache.Count ||
                source.AbilityCardCache.Select(entry => entry.InstanceId).Distinct().Count() != source.AbilityCardCache.Count)
                return "Duplicate ability cache definitions or identities.";
            foreach (AbilityCardCacheEntry entry in source.AbilityCardCache)
            {
                CardInstanceState? card = source.FindCard(entry.InstanceId);
                if (entry.DataId.Length == 0 || entry.InstanceId <= 0 || entry.InstanceId >= source.NextCardId || card?.DataId != entry.DataId)
                    return "Invalid ability cache identity or definition.";
                if (source.CardInstances?.Any(owned => owned.InstanceId == entry.InstanceId) == true ||
                    source.Cards.Hand.Concat(source.Cards.Draw).Concat(source.Cards.Discard).Any(token => token.InstanceId == entry.InstanceId) ||
                    source.OtherPiles?.Any(pile => pile.Cards.Any(token => token.InstanceId == entry.InstanceId)) == true)
                    return "A cached ability card cannot belong to an ordinary card pile.";
                if (source.Statistics?.TrackedCards.Contains(entry.InstanceId) == true ||
                    source.Statistics?.StoredCards?.Contains(entry.InstanceId) == true ||
                    source.Statistics?.DeckCards?.Contains(entry.InstanceId) == true)
                    return "A cached ability card cannot belong to ordinary deck statistics.";
            }
            return null;
        }

        public static AbilityCardResult Get(CombatContext source, CardCreationRule creation)
        {
            string? error = Validate(source);
            if (error != null) return Unsupported(error);
            if (source.AbilityCardCache == null || source.CardRegistry == null)
                return Unsupported("Missing captured ability cache or card registry.");
            AbilityCardCacheEntry? entry = source.AbilityCardCache.FirstOrDefault(item => item.DataId == creation.DataId);
            if (entry != null) return new AbilityCardResult(source, source.FindCard(entry.InstanceId), false);
            if (creation.DataId.Length == 0 || source.NextCardId <= 0 || source.NextCardId == int.MaxValue ||
                source.CardRegistry.Any(card => card.InstanceId >= source.NextCardId)) return Unsupported("Invalid ability card allocation.");
            var card = new CardInstanceState(source.NextCardId, creation.DataId, creation.StartingModifiers, CardModifiers.Empty(),
                0, 0, 0, creation.ExternalInteractions, creation.EffectCounters, creation.DamageScalingTraits,
                creation.StatusScalingTraits, creation.UnitUpgradeScalingTraits, creation.CapacityScalingTraits, creation.EquippedUnitId,
                source.CardRegistry?.Any(card => card.PlayedRoomUnitIds != null) == true ? Array.Empty<int>() : null);
            error = CardModifierModel.UnsupportedReason(card);
            if (error != null) return Unsupported(error);
            CombatContext context = Copy(source, source.NextCardId + 1, source.CardRegistry.Concat(new[] { card }).ToArray(),
                source.AbilityCardCache.Concat(new[] { new AbilityCardCacheEntry(creation.DataId, card.InstanceId) }).ToArray());
            return new AbilityCardResult(context, card, true);
        }

        // Native Clear drops cache membership, while already observed references retain their identity.
        public static CombatContext Clear(CombatContext source) => Copy(source, source.NextCardId, source.CardRegistry,
            source.AbilityCardCache == null ? null : Array.Empty<AbilityCardCacheEntry>());

        private static CombatContext Copy(CombatContext source, int nextId, IReadOnlyList<CardInstanceState>? registry,
            IReadOnlyList<AbilityCardCacheEntry>? cache) => new CombatContext(source.Cards, source.BattleRng, source.Gold, nextId,
                source.MaxHandSize, source.StatusRules, source.Statistics, source.CardInstances, registry, source.AllScenarioBossesDead,
                source.NextAddedTemporaryUpgrades, source.OtherPiles, source.QueryFrame, source.KillCamActivated, source.MagicPower,
                source.IsolatedBattlePreview, source.EnergyState, source.RoomCapacities, cache, source.LastAbilityActivatorUnitId, source.PermanentlyDisabledAbilities, source.LastSpawnedUnitId, source.NextUnitId, source.SpawnPoints, source.SummonCatalog);
        private static AbilityCardResult Unsupported(string error) => new AbilityCardResult(null, null, false, error);
    }
}
