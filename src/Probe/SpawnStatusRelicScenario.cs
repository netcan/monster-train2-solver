using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class SpawnStatusRelicScenario
    {
        internal static bool Prepared { get; private set; }
        internal static bool OriginalStealthWaxersRelic =>
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "spawn-status-relics-stealth-waxers";
        internal static bool OriginalTeamSpawnStatusRelic =>
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "spawn-status-relics-team-wide";
        internal static bool OriginalTunedGearRelic =>
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "spawn-status-relics-tuned-gear";
        internal static bool OriginalEggHatchRelic =>
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "spawn-status-relics-egg-hatch";
        internal static bool OriginalEggTransformation =>
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "spawn-status-relics-egg-transformation";
        internal static bool OriginalChicotRelic =>
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "spawn-status-relics-boss-mute";
        internal static bool OriginalCombatRelics =>
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "spawn-status-relics-combat";
        internal static bool OriginalCombatRelicsAcquired { get; private set; }
        private static readonly (string Id, string Key, string StatusId, int Stacks)[] CombatRelics =
        {
            ("a3dde0d6-0aa3-44e5-a6a4-0e993a5d9a15", "DantesCandelabra", "titanskin", 2),
            ("fda361aa-fecc-4adf-b19c-f92af04e0e19", "PressurizedBoiler", "burst", 4)
        };
        internal static bool Clones => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is
            "spawn-status-relics-clones" or "spawn-status-relics-mixed-cardless-order" or "spawn-status-relics-team-wide";
        internal static bool MixedCardlessOrder => Environment.GetEnvironmentVariable("MT2_PROBE_MIXED_CARDLESS_RELIC_ORDER") == "1";
        internal static void AcquireCombatBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (OriginalCombatRelicsAcquired) return;
            SaveManager save = managers.GetSaveManager();
            if (!OriginalCombatRelics || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Original Pyre spawn relics must be acquired before native battle setup.");
            foreach (var (id, key, statusId, stacks) in CombatRelics)
            {
                CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == id);
                CheckCombatRelic(relic, key, statusId, stacks);
                if (save.GetCollectedRelics().Any(owned => owned.GetRelicDataID() == id))
                    throw new InvalidOperationException("Original Pyre spawn relic already owned: " + key);
                save.AddRelic(relic);
            }
            OriginalCombatRelicsAcquired = true;
            log.LogInfo("ORIGINAL-PYRE-SPAWN-RELICS-ACQUIRED before native battle setup; unchanged Pyre-only effects.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Clones) UnitCloneScenario.Prepare(managers, log);
            else HealingScenario.Prepare(managers, log, withTriggers: true);
            SaveManager save = managers.GetSaveManager();
            CardState[] cards = managers.GetCardManager()!.GetAllCards(new List<CardState>()).Where(card =>
                card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678" ||
                OriginalEggTransformation && card.GetCardDataID() == "38e70e6a-20f3-4e36-8d31-c5110be07f87").ToArray();
            CardData data = save.GetAllGameData().FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678")!;
            CharacterData unit = data.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData();
            if (OriginalTeamSpawnStatusRelic)
            {
                var subtypeKeys = (List<string>)AccessTools.Field(typeof(CharacterData), "subtypeKeys").GetValue(unit);
                if (!subtypeKeys.Contains("SubtypesData_Hymnist")) subtypeKeys.Add("SubtypesData_Hymnist");
            }
            if (OriginalEggHatchRelic || OriginalEggTransformation)
            {
                CharacterData? originalEgg = null;
                if (OriginalEggTransformation)
                {
                    originalEgg = save.GetAllGameData().GetAllCharacterData()
                        .Single(item => item.GetAssetKey() == "MonsterKinhostVessel");
                    foreach (CardEffectData summon in originalEgg.GetTriggers()
                        .SelectMany(trigger => trigger.GetEffects())
                        .Where(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster"))
                        TriggeredSummonProbe.Definition(summon);
                    CharacterData small = save.GetAllGameData()
                        .FindCardData("38e70e6a-20f3-4e36-8d31-c5110be07f87")!
                        .GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster")
                        .GetParamCharacterData();
                    PrepareEgg(small);
                    for (int roomIndex = 0; roomIndex < managers.GetRoomManager()!.GetNumRooms(); roomIndex++)
                    {
                        RoomState room = managers.GetRoomManager().GetRoom(roomIndex);
                        object stats = AccessTools.Field(typeof(RoomState), "corruptionStats").GetValue(room);
                        AccessTools.Field(stats.GetType(), "currentCorruption").SetValue(stats, 2);
                    }
                }
                PrepareEgg(unit);

                void PrepareEgg(CharacterData carrier)
                {
                    var subtypeKeys = (List<string>)AccessTools.Field(typeof(CharacterData), "subtypeKeys").GetValue(carrier);
                    if (!subtypeKeys.Contains("SubtypesData_Egg")) subtypeKeys.Add("SubtypesData_Egg");
                    AccessTools.Field(typeof(CharacterData), "startingStatusEffects").SetValue(carrier,
                        carrier.GetStartingStatusEffects().Concat(new[] { new StatusEffectStackData {
                            statusId = "hatch", count = OriginalEggTransformation ? 5 : 3 } }).ToArray());
                    if (originalEgg != null)
                        AccessTools.Field(typeof(CharacterData), "triggers").SetValue(carrier,
                            carrier.GetTriggers().Concat(originalEgg.GetTriggers()).ToList());
                }
            }
            var armor = HealingScenario.HealGold(11, false, true);
            AccessTools.Field(typeof(CharacterTriggerData), "trigger").SetValue(armor, CharacterTriggerData.Trigger.OnArmorAdded);
            AccessTools.Field(typeof(CharacterData), "triggers").SetValue(unit, unit.GetTriggers().Concat(new[] { armor }).ToList());
            foreach (CardState card in cards)
                card.Setup(save.GetAllGameData().FindCardData(card.GetCardDataID())!, save);
            if (!MixedCardlessOrder) Acquire("68ef2523-5c2e-4660-b96d-00b1c0485f54", "SpawnWithArmor");
            Acquire("60a2a8a3-5f7a-4a9d-b427-5f261145fa1f", "FirstUnitGainDamageShield");
            Acquire("270356af-16bd-4433-a0b2-3e5bb94ef890", "FrostbiteOnEnemies");
            if (OriginalTunedGearRelic) Acquire("f12924bc-0600-4de0-ae2a-93534f251eb1", "TunedGear");
            if (OriginalEggHatchRelic || OriginalEggTransformation) AcquireEggHatch();
            if (OriginalChicotRelic) Acquire("92d66578-b148-49f8-9880-edb67f8b18de", "BalatroEventChicot");
            if (OriginalCombatRelics)
            {
                if (!OriginalCombatRelicsAcquired) throw new InvalidOperationException("Original Pyre spawn relics missed pre-battle acquisition.");
                foreach (var (id, key, statusId, stacks) in CombatRelics)
                {
                    if (save.GetCollectedRelics().Count(owned => owned.GetRelicDataID() == id) != 1)
                        throw new InvalidOperationException("Original Pyre spawn relic missing or duplicated: " + key);
                    CheckCombatRelic(save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == id),
                        key, statusId, stacks);
                }
            }
            if (OriginalStealthWaxersRelic) AcquireStealthWaxers();
            if (OriginalTeamSpawnStatusRelic) AcquireTeamSpawnStatus();
            if (Clones) AcquireCardlessUpgrade();
            if (Clones)
            {
                CollectableRelicData extraSpawn = save.GetAllGameData().GetAllCollectableRelicData().Single(item =>
                    item.GetID() == "9e0deb69-6196-44a6-8220-85bd0df25f77");
                if (extraSpawn.name != "ExtraSpawnTrigger" || extraSpawn.GetEffects().Count != 1 ||
                    extraSpawn.GetEffects()[0].GetParamTrigger() != CharacterTriggerData.Trigger.OnSpawn || extraSpawn.GetEffects()[0].GetParamInt() != 1)
                    throw new InvalidOperationException("Original extra-spawn relic changed.");
                save.AddRelic(extraSpawn);
            }
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("SPAWN-STATUS-RELICS-PREPARED original relic definitions unchanged; TunedGear=" + OriginalTunedGearRelic + "; EggHatch=" + OriginalEggHatchRelic + "; Chicot=" + OriginalChicotRelic + "; combat=" + OriginalCombatRelics + "; Hymnist carrier test=" + OriginalTeamSpawnStatusRelic + "; StealthWaxers=" + OriginalStealthWaxersRelic + "; clones/extra-spawn=" + Clones + "; native Boss/waves retained.");

            void Acquire(string id, string key)
            {
                CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == id);
                if (relic.name != key || relic.GetEffects().Count != 1 || relic.GetEffects()[0].GetEffectClassName() != "RelicEffectAddStatusEffectOnSpawn")
                    throw new InvalidOperationException("Original spawn status relic changed: " + key);
                save.AddRelic(relic);
            }

            void AcquireEggHatch()
            {
                const string id = "8c8a2a2c-500c-4b77-90fd-0d309b94bb2c";
                CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == id);
                RelicEffectData[] effects = relic.GetEffects().ToArray();
                if (relic.name != "EggHatchRemoval" || effects.Length != 2 ||
                    effects[0].GetEffectClassName() != "RelicEffectRemoveStatusEffectOnSpawn" ||
                    effects[1].GetEffectClassName() != "RelicEffectAddStatusEffectOnSpawn" ||
                    effects[0].GetParamSourceTeam() != Team.Type.Monsters ||
                    effects[1].GetParamSourceTeam() != Team.Type.Monsters ||
                    effects[0].GetParamStatusEffects().Length != 1 ||
                    effects[0].GetParamStatusEffects()[0].statusId != "hatch" ||
                    effects[0].GetParamStatusEffects()[0].count != 3 ||
                    effects[1].GetParamStatusEffects().Length != 1 ||
                    effects[1].GetParamStatusEffects()[0].statusId != "armor" ||
                    effects[1].GetParamStatusEffects()[0].count != 30 ||
                    effects.Any(effect => effect.GetParamCharacterSubtype()?.Key != "SubtypesData_Egg") ||
                    effects.Any(effect => effect.GetEffectConditions().Count != 0))
                    throw new InvalidOperationException("Original EggHatchRemoval effect order or payload changed.");
                save.AddRelic(relic);
            }

            void AcquireCardlessUpgrade()
            {
                CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.name == "PyreHeartSon");
                RelicEffectData effect = relic.GetEffects().Single();
                if (effect.GetEffectClassName() != "RelicEffectAddTempUpgrade" || effect.GetParamCardUpgradeData() == null ||
                    !effect.GetParamSourceTeam().HasFlag(Team.Type.Monsters) || !effect.GetParamBool() || effect.GetEffectConditions().Count != 0)
                    throw new InvalidOperationException("Original PyreHeartSon cardless upgrade relic changed.");
                if (MixedCardlessOrder)
                {
                    CollectableRelicData spawnRelic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.name == "SpawnWithArmor");
                    RelicEffectData statusEffect = spawnRelic.GetEffects().Single(item => item.GetEffectClassName() == "RelicEffectAddStatusEffectOnSpawn");
                    var statusCopy = (RelicEffectData)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(statusEffect, null)!;
                    relic.GetEffects().Insert(0, statusCopy);
                }
                save.AddRelic(relic);
            }

            void AcquireStealthWaxers()
            {
                CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item =>
                    item.GetID() == "22f5ff29-69be-4043-9fe1-245392ea3c95");
                RelicEffectData effect = relic.GetEffects().Single();
                var statuses = effect.GetParamStatusEffects();
                var conditions = effect.GetEffectConditions();
                if (relic.name != "StealthWaxers" || effect.GetEffectClassName() != "RelicEffectAddStatusEffectOnSpawn" ||
                    !effect.GetParamSourceTeam().HasFlag(Team.Type.Monsters) || effect.GetParamCharacterSubtype()?.IsNone != true ||
                    statuses.Length != 1 || statuses[0].statusId != "endless" || statuses[0].count != 1 || conditions.Count != 1 ||
                    Field(conditions[0], "paramTrackedValue").ToString() != "SubtypeInDeck" ||
                    Field(conditions[0], "paramEntryDuration").ToString() != "ThisTurn" ||
                    Convert.ToInt32(Field(conditions[0], "paramComparator")) != 6 || (int)Field(conditions[0], "paramInt") != 1 ||
                    !(bool)Field(conditions[0], "paramTrackTriggerCount") || (bool)Field(conditions[0], "allowMultipleTriggersPerDuration"))
                    throw new InvalidOperationException("Original StealthWaxers status/condition definition changed.");
                save.AddRelic(relic);

                static object Field(object source, string name) => AccessTools.Field(source.GetType(), name).GetValue(source);
            }

            void AcquireTeamSpawnStatus()
            {
                CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item =>
                    item.GetID() == "55cf29f3-bc0d-4b5f-929e-07d4629141b3");
                RelicEffectData effect = relic.GetEffects().Single();
                StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
                if (relic.name != "HymnistsApplyValorOnSummon" ||
                    effect.GetEffectClassName() != "RelicEffectAddStatusEffectToTeamOnSpawn" ||
                    !effect.GetParamSourceTeam().HasFlag(Team.Type.Monsters) ||
                    statuses.Length != 1 || statuses[0].statusId != "valor" || statuses[0].count != 5 ||
                    effect.GetEffectConditions().Count != 0)
                    throw new InvalidOperationException("Original HymnistsApplyValorOnSummon definition changed.");
                save.AddRelic(relic);
            }
        }

        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.GetBattlePreviewEnabled))]
        private static class EggPreviewIsolation
        {
            private static bool Prefix(ref bool __result)
            {
                if (!OriginalEggTransformation && !OriginalEggCardScenario.Enabled) return true;
                __result = false;
                return false;
            }
        }

        private static void CheckCombatRelic(CollectableRelicData relic, string key, string statusId, int stacks)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
            if (relic.name != key || effect.GetEffectClassName() != "RelicEffectAddStatusEffectOnSpawn" ||
                !effect.GetParamSourceTeam().HasFlag(Team.Type.Monsters) ||
                statuses.Length != 1 || statuses[0].statusId != statusId || statuses[0].count != stacks ||
                effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original Pyre spawn relic changed: " + key);
        }
    }
}
