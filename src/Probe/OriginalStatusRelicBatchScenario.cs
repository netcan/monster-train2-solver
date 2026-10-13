using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStatusRelicBatchScenario
    {
        internal const string Name = "original-status-relic-batch";
        internal const string BurnoutStacksName = "original-status-relic-batch-burnout";
        internal static bool BurnoutStacks => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == BurnoutStacksName;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name || BurnoutStacks;
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData[] stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!)
                .Where(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                .Distinct().ToArray();
            if (stewards.Length < 2) throw new InvalidOperationException("Status relic batch needs two Steward definitions.");
            foreach (CardData card in stewards)
            {
                CharacterData unit = card.GetSpawnCharacterData()!;
                Set(unit, "health", 80); Set(unit, "attackDamage", 2); Set(unit, "size", 1); Set(unit, "equipmentLimit", 2);
                Set(card, "cost", 0);
                Set(unit, "startingStatusEffects", BurnoutStacks && ReferenceEquals(card, stewards[0])
                    ? new[] { Stack("armor", 2), Stack("valor", 1), Stack("ephemeral", 1) }
                    : new[] { Stack("armor", 2), Stack("valor", 1) });
                Set(unit, "triggers", unit.GetTriggers().Concat(new[]
                {
                    Trigger("OnSpawn", Status(TargetMode.Self, Team.Type.Monsters, "armor", 0),
                        Status(TargetMode.Self, Team.Type.Monsters, "armor", 1), Status(TargetMode.Self, Team.Type.Monsters, "valor", 0),
                        Status(TargetMode.Room, Team.Type.Heroes, "pyregel", 1)),
                    Trigger("PreCombat", Status(TargetMode.Self, Team.Type.Monsters, "valor", 1),
                        Status(TargetMode.Room, Team.Type.Heroes, "pyregel", 1)),
                    Trigger("OnHit", Status(TargetMode.Self, Team.Type.Monsters, "armor", 1))
                }).ToList());
            }
            CardData spell = owned.Select(card => data.FindCardData(card.GetCardDataID())!).First(card => card.GetEffects().Any(
                effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter));
            Set(spell, "effects", new List<CardEffectData>
            {
                Status(TargetMode.DropTargetCharacter, Team.Type.Monsters | Team.Type.Heroes, "armor", 1),
                Status(TargetMode.DropTargetCharacter, Team.Type.Monsters | Team.Type.Heroes, "valor", 0),
                Status(TargetMode.DropTargetCharacter, Team.Type.Monsters | Team.Type.Heroes, "pyregel", 1),
                Status(TargetMode.DropTargetCharacter, Team.Type.Monsters | Team.Type.Heroes, "corrupt_poison", 1)
            });
            Set(spell, "traits", new List<CardTraitData>()); Set(spell, "cost", 0);
            CardData gear = data.GetAllCardData().First(card => card.GetCardType() == CardType.Equipment &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectAttachEquipment");
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade("PojuStatusRelicGear", "bad72000-0000-4000-8000-000000000001", 0, 0, 0, 0, "armor", 1);
            var attach = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Monsters);
            attach.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(attach, "paramCardUpgradeData", upgrade);
            Set(gear, "effects", new List<CardEffectData> { attach }); Set(gear, "traits", new List<CardTraitData>()); Set(gear, "cost", 0);
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
                Set(enemy, "triggers", enemy.GetTriggers().Concat(new[] { Trigger("OnTurnBegin",
                    Status(TargetMode.Self, Team.Type.Heroes, "pyregel", 1), Status(TargetMode.Self, Team.Type.Heroes, "armor", 1),
                    Status(TargetMode.Self, Team.Type.Heroes, "spell weakness", 1)) }).ToList());

            foreach (CardState card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (CardData card in new[] { stewards[0], stewards[1], gear, gear, spell, spell })
                cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
            Acquire("f3e1c306-964c-4373-96c3-61e606379f43", "ApplyExtraPyregel_DH", Team.Type.None, 1, "pyregel", 1);
            Acquire("f4c76f11-37d9-4bb5-867a-4d99b7cfa270", "IncreasePyregelStacks", Team.Type.Heroes, 2, "pyregel", 6);
            Acquire("74c025fa-ddcb-494b-a790-dcadc5b5772b", "ExtraValorOnValor", Team.Type.Monsters, 0, "valor", 1);
            Acquire("ba26070b-4f6b-4af0-a1ec-2af024b6af87", "IncreaseArmorStacks", Team.Type.Monsters, 1, "armor", 4);
            Acquire("b1d918d8-1fa6-4e6d-b523-5f7f339e5af8", "ExtraSpellWeakness", Team.Type.Heroes, 1, "spell weakness", 1);
            Acquire("55c06c99-7cee-4d7e-ad17-f893722d8071", "IncreaseDecaySlimeStacks", Team.Type.None, 0, "corrupt_poison", 1);
            if (BurnoutStacks)
                Acquire("dc32326b-0952-4b3b-8bc8-cec909476c5f", "IncreaseBurnoutStacks", Team.Type.Monsters, 1, "ephemeral", 3);
            for (int index = 0; index < managers.GetRoomManager()!.GetNumRooms(); index++)
            {
                RoomState room = managers.GetRoomManager()!.GetRoom(index);
                object stats = AccessTools.Field(typeof(RoomState), "corruptionStats").GetValue(room);
                Set(stats, "currentCorruption", 2);
            }
            CaptureReady = true;
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).ToArray()) managers.GetRelicManager().ApplyCardStateModifiers(card);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo(BurnoutStacks
                ? "ORIGINAL-STATUS-RELIC-BATCH-PREPARED seven unchanged original status adders including IncreaseBurnoutStacks; one Steward starts with Ephemeral; authored spell, equipment and combat status applications; Corrupt Poison tested at room corruption 2; native Boss and wave schedule retained."
                : "ORIGINAL-STATUS-RELIC-BATCH-PREPARED six unchanged original status adders; authored starting, spell, equipment and combat status applications; Corrupt Poison tested at room corruption 2; native Boss and wave schedule retained.");

            void Acquire(string id, string key, Team.Type team, int condition, string statusId, int count)
            {
                CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == id);
                RelicEffectData effect = relic.GetEffects().Single();
                StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
                if (relic.name != key || effect.GetEffectClassName() != "RelicEffectAddStatusEffectOnStatusApplied" ||
                    effect.GetParamSourceTeam() != team || effect.GetParamInt() != condition || effect.GetEffectConditions().Count != 0 ||
                    statuses.Length != 1 || statuses[0].statusId != statusId || statuses[0].count != count)
                    throw new InvalidOperationException("Original status relic definition changed: " + key);
                save.AddRelic(relic);
            }
        }
        private static CharacterTriggerData Trigger(string kind, params CardEffectData[] effects)
        {
            var trigger = HealingScenario.HealGold(0, false, true);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
            Set(trigger, "effects", effects.ToList()); return trigger;
        }
        private static CardEffectData Status(TargetMode mode, Team.Type team, string id, int count)
        {
            var effect = new CardEffectData("CardEffectAddStatusEffect", null!, team); effect.Cheat_SetTargetMode(mode);
            Set(effect, "paramStatusEffects", new[] { Stack(id, count) }); return effect;
        }
        private static StatusEffectStackData Stack(string id, int count) => new StatusEffectStackData { statusId = id, count = count };
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
