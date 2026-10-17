using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalJudgesScalesScenario
    {
        internal const string Name = "original-judges-scales";
        internal const string OwnerId = "3511a804-42be-465c-9d40-56b988c8a940";
        internal const string EligibleCarrierId = "fc119000-0000-4000-8000-000000000497";
        internal const string PersistentGuardId = "fc119000-0000-4000-8000-000000000498";
        internal const string InfusionGuardId = "fc119000-0000-4000-8000-000000000499";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        private static object? definitionEvidence;
        internal static object Evidence => definitionEvidence ?? throw new InvalidOperationException("Judges Scales definition was not captured.");

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Judges Scales must be acquired before battle setup.");
            CollectableRelicData owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = owner.GetEffects().Single();
            if (owner.name != "JudgesScales" || effect.GetEffectClassName() != "RelicEffectChooseDiscardsDrawReplacementsOnPostOpeningDraw" ||
                effect.GetEffectConditions().Count != 0 || save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Original Judges Scales definition or acquisition changed.");

            CardData template = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell);
            CardData MakeCarrier(string name, string id, params string[] traits)
            {
                CardData card = UnityEngine.Object.Instantiate(template);
                card.name = name;
                Set(card, "id", id);
                Set(card, "cost", 0);
                Set(card, "traits", new List<CardTraitData>());
                Set(card, "effects", new List<CardEffectData>());
                Set(card, "triggers", new List<CardTriggerEffectData>());
                Set(card, "effectTriggers", new List<CharacterTriggerData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>());
                foreach (string traitName in new[] { "CardTraitDrawInDeploymentPhase" }.Concat(traits))
                {
                    var trait = new CardTraitData();
                    trait.Setup(traitName);
                    card.GetTraits().Add(trait);
                }
                return card;
            }
            CardData[] carriers =
            {
                MakeCarrier("PojuJudgesScalesCarrier", EligibleCarrierId),
                MakeCarrier("PojuJudgesScalesPersistentGuard", PersistentGuardId, "CardTraitPersistent"),
                MakeCarrier("PojuJudgesScalesInfusionGuard", InfusionGuardId, "CardTraitInfusion")
            };
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).AddRange(carriers);
            foreach (CardData card in carriers) save.AddCardToDeck(card);

            definitionEvidence = new
            {
                OwnerId,
                AssetKey = owner.name,
                EffectType = effect.GetEffectClassName(),
                DisallowedInPlacementPhase = owner.DisallowInDeploymentPhase,
                ParamInt = effect.GetParamInt(),
                ParamInt2 = effect.GetParamInt2(),
                ParamMinInt = effect.GetParamMinInt(),
                ParamFloat = effect.GetParamFloat(),
                ConditionCount = effect.GetEffectConditions().Count,
                EligibleCarrierId,
                PersistentGuardId,
                InfusionGuardId
            };
            save.AddRelic(owner);
            Acquired = true;
            Prepared = true;
            log.LogInfo("ORIGINAL-JUDGES-SCALES-ACQUIRED unchanged owner=" + OwnerId +
                " with deployment-drawn eligible, Persistent and Infusion test cards.");
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
