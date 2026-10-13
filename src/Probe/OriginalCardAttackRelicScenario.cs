using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCardAttackRelicScenario
    {
        internal const string Name = "original-card-attack-relic";
        internal const string AssetKey = "WhelpDragonAttack_DH";
        internal const string DataId = "72706202-526e-4377-9745-7df40a26ba7d";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        private static readonly string[] CarrierIds = { "fc119000-0000-4000-8000-000000000201", "fc119000-0000-4000-8000-000000000202" };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Card-attack relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("Card-attack relic is already present.");
            save.AddRelic(relic);
            AddCarrierDefinitions(save);
            Acquired = true;
            log.LogInfo("ORIGINAL-CARD-ATTACK-RELIC-ACQUIRED unchanged WhelpDragonAttack_DH; two +8 attack effects target Whelp and Dragon spawn cards.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Card-attack relic missed the native deployment boundary.");
            SaveManager save = managers.GetSaveManager();
            if (save.GetCollectedRelics().Count(item => item.GetRelicDataID() == DataId) != 1)
                throw new InvalidOperationException("Card-attack relic is missing or duplicated at battle setup.");
            CheckOriginal(save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId));

            AllGameData data = save.GetAllGameData();
            CardState[] witnesses = CarrierIds.Select(id => new CardState(data.FindCardData(id)!, save,
                setupStartingUpgrades: false)).ToArray();
            CardManager cards = managers.GetCardManager()!;
            if (witnesses.Length != 2 || witnesses.Any(card => card.GetCardType() != CardType.Monster || card.GetSpawnCharacterData() == null))
                throw new InvalidOperationException("Original card-attack relic lacks its Whelp and Dragon monster witnesses: " +
                    string.Join(";", witnesses.Select(card => card.GetCardDataID() + ":" + card.GetCardType() + ":" + card.GetSpawnCharacterData()?.GetAssetKey())) + ".");

            CaptureReady = true;
            foreach (CardState card in witnesses)
            {
                managers.GetRelicManager().ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                managers.GetRelicManager().ApplyCardStateModifiers(card);
            }
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-CARD-ATTACK-RELIC-PREPARED native Whelp and Dragon subtype probes; source relic's Spell type is bypassed by native subtype precedence; records=" +
                string.Join(";", witnesses.Select(card => card.GetCardDataID() + ":" +
                    string.Join(",", card.GetSpawnCharacterData()!.GetSubtypes().Select(item => item.Key)))) + ".");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            CardToken? card = state.Spawn.Train.Context?.Cards.Hand.FirstOrDefault(item => CarrierIds.Contains(item.DataId));
            return card == null ? null : BattleActionModel.EnumerateSupportedPlays(state)
                .FirstOrDefault(action => action.CardInstanceId == card.InstanceId);
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData[] effects = relic.GetEffects().ToArray();
            string[] subtypes = { "SubtypesData_Whelp", "SubtypesData_Dragon" };
            if (relic.name != AssetKey || relic.DisallowInDeploymentPhase || effects.Length != 2)
                throw new InvalidOperationException("Original WhelpDragonAttack_DH identity or effect count changed.");
            for (int index = 0; index < effects.Length; index++)
            {
                RelicEffectData effect = effects[index];
                if (effect.GetEffectClassName() != "RelicEffectModifyCardAttack" || effect.GetParamString().Length != 0 ||
                    effect.GetParamCharacterSubtype()?.Key != subtypes[index] || effect.GetParamInt() != 8 ||
                    effect.GetParamFloat() != 0 || effect.GetParamInt2() != 0 || effect.GetParamCardType() != CardType.Spell ||
                    effect.GetParamBool() || effect.GetParamBool2() || effect.GetParamBool3() || effect.GetParamBool4() ||
                    effect.GetParamCardFilter() != null || effect.GetEffectConditions().Count != 0)
                    throw new InvalidOperationException("Original WhelpDragonAttack_DH payload changed at effect " + index + ".");
            }
        }

        private static void AddCarrierDefinitions(SaveManager save)
        {
            AllGameData data = save.GetAllGameData();
            CardData template = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID())!)
                .First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            string[] subtypeKeys = { "SubtypesData_Whelp", "SubtypesData_Dragon" };
            for (int index = 0; index < subtypeKeys.Length; index++)
            {
                CharacterData unit = UnityEngine.Object.Instantiate(template.GetSpawnCharacterData()!);
                string label = index == 0 ? "Whelp" : "Dragon";
                unit.name = "TrainStewardPoju" + label;
                Set(unit, "id", "fc119000-0000-4000-8000-000000000211" + (index + 1));
                var unitSubtypes = unit.GetSubtypes().Select(item => item.Key).ToList();
                if (!unitSubtypes.Contains(subtypeKeys[index], StringComparer.Ordinal)) unitSubtypes.Add(subtypeKeys[index]);
                Set(unit, "subtypeKeys", unitSubtypes);
                Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(unit, "triggers", new List<CharacterTriggerData>());
                Set(unit, "unitAbility", null);
                ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit);

                CardData carrier = UnityEngine.Object.Instantiate(template);
                carrier.name = "PojuCardAttack" + label;
                Set(carrier, "id", CarrierIds[index]);
                Set(carrier, "cardType", CardType.Monster);
                Set(carrier, "cost", 0); Set(carrier, "costType", CardData.CostType.Default);
                Set(carrier, "targetless", false); Set(carrier, "targetsRoom", true);
                Set(carrier, "isUnitAbility", false); Set(carrier, "isRoomAbility", false);
                Set(carrier, "traits", new List<CardTraitData>());
                Set(carrier, "effectTriggers", new List<CharacterTriggerData>());
                Set(carrier, "triggers", new List<CardTriggerEffectData>());
                Set(carrier, "startingUpgrades", new List<CardUpgradeData>());
                var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
                spawn.Cheat_SetTargetMode(TargetMode.Room);
                Set(spawn, "paramInt", 1); Set(spawn, "paramCharacterData", unit);
                Set(carrier, "effects", new List<CardEffectData> { spawn });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(carrier);
            }
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
