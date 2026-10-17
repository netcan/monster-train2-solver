using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFirstImpBonusScenario
    {
        internal const string Name = "original-first-imp-bonus";
        internal const string OwnerId = "0a6cff92-aefa-4d7f-ae71-615c8e30739b";
        internal const string AssetKey = "FirstImpBonus";
        internal const string Effect = "RelicEffectEnergyAndCardDrawOnUnitSpawned";
        internal const string ImpSubtype = "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c";
        internal const string SyntheticImpCardId = "fc119000-0000-4000-8000-000000000513";

        private static object? definitionEvidence;
        private static object? syntheticWitnessEvidence;
        private static CombatRelicState? ownerEvidence;
        private static int witnessInstanceId;
        private static bool witnessPlayed;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static object Evidence => new { Definition = definitionEvidence, Owner = ownerEvidence, SyntheticWitness = syntheticWitnessEvidence };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("FirstImpBonus must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = relic.GetEffects().Single();
            Team.Type sourceTeam = effect.GetParamSourceTeam();
            if (relic.name != AssetKey || !relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != Effect || effect.GetParamInt() != 1 ||
                sourceTeam != Team.Type.Monsters ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId) ||
                effect.GetEffectConditions().Count != 1)
                throw new InvalidOperationException("Original FirstImpBonus definition or condition changed.");

            save.AddRelic(relic);
            ownerEvidence = RelicProbe.Capture(managers).Single(item => item.DataId == OwnerId);
            RelicConditionState condition = ownerEvidence.SpawnStatuses!.Single().Conditions.Single();
            CardStatisticQuery query = condition.Query;
            if (condition.TrackTriggerCount || condition.Comparator != 2 || condition.Value != 1 ||
                condition.AllowMultiple || query.Type != "MonsterSubtypePlayed" || query.Duration != "ThisTurn" ||
                query.Subtype != ImpSubtype || query.SubtypeIsNone || query.TypeMatchDataIds?.Count == 0 ||
                query.SubtypeMatchDataIds?.Count == 0)
                throw new InvalidOperationException("Original FirstImpBonus condition is not captured exactly.");
            definitionEvidence = new
            {
                OwnerId,
                AssetKey = relic.name,
                EffectType = effect.GetEffectClassName(),
                DisallowedInPlacementPhase = relic.DisallowInDeploymentPhase,
                ParamInt = effect.GetParamInt(),
                ParamSourceTeam = sourceTeam.ToString(),
                CanApplyInPreviewMode = false,
                Conditions = ownerEvidence.SpawnStatuses!.Single().Conditions
            };
            Acquired = true;
            log.LogInfo("ORIGINAL-FIRST-IMP-BONUS-ACQUIRED unchanged=" + relic.name +
                " energy=" + effect.GetParamInt() + " team=" + sourceTeam + " condition=MonsterSubtypePlayed:Imp==1/ThisTurn.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() <= 0)
                throw new InvalidOperationException("FirstImpBonus synthetic witness must be prepared after deployment phase.");
            CombatRelicState owner = RelicProbe.Capture(managers).Single(item => item.DataId == OwnerId);
            if (owner.AssetKey != AssetKey || owner.EffectTypes.Single() != Effect)
                throw new InvalidOperationException("Unchanged FirstImpBonus owner was not retained for the battle.");
            ownerEvidence = owner;
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            CardData template = data.GetAllCardData().Where(card => card != null && card.GetCardType() == CardType.Monster &&
                card.GetSpawnCharacterData()?.GetSubtypes().Any(subtype => subtype.Key == ImpSubtype) == true)
                .OrderBy(card => card.GetID(), StringComparer.Ordinal).FirstOrDefault()
                ?? throw new InvalidOperationException("No native Monster card spawning an Imp is available for the synthetic witness.");
            if (data.FindCardData(SyntheticImpCardId) != null)
                throw new InvalidOperationException("Synthetic FirstImpBonus witness card id already exists.");
            CardData witness = UnityEngine.Object.Instantiate(template);
            witness.name = "PojuFirstImpBonusSyntheticWitness";
            Set(witness, "id", SyntheticImpCardId);
            Set(witness, "cost", 0);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(witness);
            CardState? added = managers.GetCardManager()!.AddNewCard(witness, CardPile.HandPile, false, false, animate: false);
            if (added == null || !managers.GetCardManager()!.GetHand().Contains(added))
                throw new InvalidOperationException("Synthetic FirstImpBonus Imp witness did not enter the native hand.");
            witnessInstanceId = FullBattleTrace.Active!.CardId(added);
            syntheticWitnessEvidence = new
            {
                IsSynthetic = true,
                CardDefinitionId = SyntheticImpCardId,
                CardInstanceId = witnessInstanceId,
                SourceCardDefinitionId = template.GetID(),
                SourceCardAssetKey = template.name,
                SpawnCharacterId = template.GetSpawnCharacterData()!.GetID(),
                Subtype = ImpSubtype,
                Cost = witness.GetCost(),
                PreparedTurn = managers.GetCombatManager()!.GetTurnCount()
            };
            FullBattleTrace.Active.InvalidateCardRules();
            CaptureReady = true;
            Prepared = true;
            log.LogInfo("ORIGINAL-FIRST-IMP-BONUS-PREPARED synthetic-imp=true card=" + SyntheticImpCardId +
                " instance=" + witnessInstanceId + " source=" + template.GetID() + " cost=" + witness.GetCost() + ".");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!Prepared || witnessPlayed) return null;
            CardToken? token = source.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.InstanceId == witnessInstanceId);
            if (token == null) throw new InvalidOperationException("Synthetic FirstImpBonus Imp witness is absent from the decision hand.");
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(item => item.CardInstanceId == witnessInstanceId);
            if (action == null) throw new InvalidOperationException("Synthetic FirstImpBonus Imp witness has no supported play.");
            witnessPlayed = true;
            return action;
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
