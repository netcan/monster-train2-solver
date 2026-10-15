using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class CampaignDeckCopyCalibration
    {
        internal const string Id = "8ee399d2-4ee4-409d-af95-319bcd6accc4";
        private static List<CardState>? animations;
        [HarmonyPatch(typeof(SaveManager), "ShowCardAddedAnimation")]
        private static class PresentationPatch
        {
            private static bool Prefix(CardState cardState)
            {
                if (animations == null) return true;
                // The actual consumer and permanent deck mutation have run.
                // Stop before pending-animation bookkeeping and UI dispatch.
                animations.Add(cardState); return false;
            }
        }
        internal static void Capture(AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_DECK_COPY") != "1") return;
            var save = managers.GetSaveManager(); var relics = managers.GetRelicManager(); var data = save.GetAllGameData();
            var owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == Id); var effectData = owner.GetEffects().Single(); var mask = effectData.GetParamCardFilter();
            if (save.PreviewMode || save.GetAllRelics().Any(item => item.GetEffects().Any(effect => effect is ITurnPhaseEndOfBattleRelicEffect || effect is ICardModifierRelicEffect)))
                throw new InvalidOperationException("Permanent copy calibration requires isolated copy/modifier owners.");
            var deck = save.GetDeckState(); var original = deck.ToArray();
            CardState Card(CardData definition, bool modified = false)
            {
                var card = new CardState(); card.Setup(definition, save, setupStartingUpgrades: false, setPurified: modified);
                if (modified)
                {
                    var upgradeData = data.FindCardUpgradeData("069b201b-ee72-4daf-99d8-914d56f1bad5");
                    if (upgradeData == null) throw new InvalidOperationException("Original numeric permanent upgrade missing.");
                    var upgrade = new CardUpgradeState(); upgrade.Setup(upgradeData); card.GetCardStateModifiers().AddUpgrade(upgrade);
                    var temporary = new CardUpgradeState(); temporary.Setup(upgradeData); card.GetTemporaryCardStateModifiers().AddUpgrade(temporary);
                    AccessTools.Field(typeof(CardStateModifiers), "additionalDamage").SetValue(card.GetCardStateModifiers(), 3);
                    AccessTools.Field(typeof(CardStateModifiers), "additionalDamage").SetValue(card.GetTemporaryCardStateModifiers(), 7); card.SetIsEventCachedCard(true);
                }
                return card;
            }
            var champion = data.GetAllCardData().First(card => CardUpgradeMaskCalibration.Card(card, relics).Subtypes.Contains("SubtypesData_Champion_83f21cbe-9d9b-4566-a2c3-ca559ab8ff34"));
            var ordinary = original.Select(card => data.FindCardData(card.GetCardDataID())).First(card => card?.GetCardType() == CardType.Monster && mask.FilterCard(card, relics))!;
            var spell = original.Select(card => data.FindCardData(card.GetCardDataID())).First(card => card?.GetCardType() == CardType.Spell && mask.FilterCard(card, relics))!;
            var carriers = new[] { Card(champion), Card(ordinary), Card(ordinary, true), Card(spell) };
            var ids = new Dictionary<CardState, int>(); int nextId = 1;
            int Identity(CardState card) { if (!ids.TryGetValue(card, out int id)) ids[card] = id = nextId++; return id; }
            CardUpgradeMaskCard PermanentMask(CardState card)
            {
                var temporary = card.GetTemporaryCardStateModifiers();
                try { AccessTools.Field(typeof(CardState), "temporaryCardModifiers").SetValue(card, new CardStateModifiers()); return CardUpgradeMaskCalibration.Card(card, relics); }
                finally { AccessTools.Field(typeof(CardState), "temporaryCardModifiers").SetValue(card, temporary); }
            }
            CampaignCopyCard Projection(CardState card) => new CampaignCopyCard(Identity(card), card.GetCardDataID(), CardUpgradeMaskCalibration.Card(card, relics), PermanentMask(card),
                CardModifierProbe.Modifiers(card.GetCardStateModifiers()), card.IsPurified, card.IsEventCachedCard, card.IsEventCachedCardDuplicate);
            var rng = (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { RngId.Rewards }); var originalRng = new HadesRNG(rng);
            object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
                .Select(id => new { Id = id.ToString(), State = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
            UnityRng Rewards() { var words = RngCalibration.Words(RandomManager.GetState(RngId.Rewards)); return new UnityRng(words[0], words[1], words[2], words[3]); }
            JObject Snapshot() => JObject.FromObject(new { Owners = save.GetCollectedRelics().Select(item => item.GetRelicDataID()).ToArray(), Deck = deck.Select(Identity).ToArray(),
                Gold = save.GetGold(), Health = save.GetTowerHP(), Maximum = save.GetMaxTowerHP(), Frame = UnityEngine.Time.frameCount, Rng = Rng() });
            var before = Snapshot(); var rows = new List<object>();
            void Remove() => save.RemoveRelic(save.GetCollectedRelics().Single(item => item.GetRelicDataID() == Id));
            void Samples(string phase)
            {
                var sets = new[] { Array.Empty<CardState>(), new[] { carriers[0] }, new[] { carriers[1] }, new[] { carriers[2] }, carriers, new[] { carriers[3], carriers[2], carriers[0], carriers[1] } };
                for (int set = 0; set < sets.Length; set++)
                foreach (int seed in new[] { 7, 19 })
                foreach (int repetition in new[] { 0, 1 })
                {
                    deck.Clear(); deck.AddRange(sets[set]); rng.Init(seed);
                    var inputs = deck.Select(Projection).ToArray(); int next = nextId; var rngBefore = Rng(); var rewardsBefore = Rewards(); animations = new List<CardState>();
                    try
                    {
                        relics.ApplyEndOfBattleRelicEffects(null);
                        var added = deck.Skip(inputs.Length).ToArray();
                        rows.Add(new { Phase = phase, Set = set, Seed = seed, Repetition = repetition, Owned = save.GetCollectedRelics().Any(item => item.GetRelicDataID() == Id),
                            Before = inputs, NextIdentity = next, Actual = deck.Select(Projection).ToArray(), RewardsBefore = rewardsBefore, RewardsAfter = Rewards(), RngBefore = rngBefore, RngAfter = Rng(),
                            Candidates = sets[set].Where(card => mask.FilterCard(card, relics)).Select(Identity).ToArray(),
                            FreshReferences = added.All(card => sets[set].All(input => !ReferenceEquals(input, card))) && deck.Take(inputs.Length).SequenceEqual(sets[set]),
                            EmptyTemporary = added.All(card => CardModifierProbe.Modifiers(card.GetTemporaryCardStateModifiers()).Upgrades.Count == 0 && (int)AccessTools.Field(typeof(CardStateModifiers), "additionalDamage").GetValue(card.GetTemporaryCardStateModifiers()) == 0),
                            AnimationReferences = animations.SequenceEqual(added) });
                    }
                    finally { animations = null; }
                }
            }
            try { Samples("baseline"); save.AddRelic(owner); Samples("owned"); Remove(); Samples("removed"); }
            finally { animations = null; if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == Id)) Remove(); deck.Clear(); deck.AddRange(original); rng.Init(originalRng); }
            var after = Snapshot(); if (!JToken.DeepEquals(before, after)) throw new InvalidOperationException("Copy calibration changed recorded state/frame/RNG.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version, GameModuleMvid = typeof(RelicState).Assembly.ManifestModule.ModuleVersionId,
                Before = before, After = after, Owner = new { Id = owner.GetID(), Key = owner.name, Type = effectData.GetEffectClassName(), Conditions = effectData.GetEffectConditions().Count, Mask = CardUpgradeMaskProbe.Rule(mask) }, Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "campaign-deck-copy-calibration.mt2f"))) document.Write(stream);
        }
    }
}
