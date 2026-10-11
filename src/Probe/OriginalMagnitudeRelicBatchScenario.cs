using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMagnitudeRelicBatchScenario
    {
        internal const string Name = "original-magnitude-relic-batch";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData[] stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!)
                .Where(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                .Distinct().ToArray();
            if (stewards.Length < 2) throw new InvalidOperationException("Magnitude relic batch needs two Steward definitions.");

            foreach (CardData card in stewards)
            {
                CharacterData unit = card.GetSpawnCharacterData()!;
                Set(unit, "health", 80); Set(unit, "attackDamage", 2); Set(unit, "size", 1);
                Set(card, "cost", 0);
                Set(unit, "startingStatusEffects", new[] { Stack("buff", 2), Stack("spikes", 2), Stack("regen", 2) });
                Set(unit, "triggers", unit.GetTriggers().Concat(new[]
                {
                    Trigger("OnSpawn", Status("buff", 1), Status("spikes", 1), Status("regen", 1)),
                    Trigger("PreCombat", Status("buff", 1), Status("regen", 1)),
                    Trigger("OnHit", Status("buff", 1), Status("spikes", 1))
                }).ToList());
            }

            // The normal verification policy targets allies for this spell. Its
            // real damage leaves missing health for amplified Regen to restore.
            CardData wound = owned.Select(card => data.FindCardData(card.GetCardDataID())!).First(card => card.GetEffects().Any(
                effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter));
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
            damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(damage, "paramInt", 12);
            Set(wound, "effects", new List<CardEffectData> { damage });
            Set(wound, "traits", new List<CardTraitData>()); Set(wound, "cost", 0);

            foreach (CardState card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (CardData card in new[] { stewards[0], stewards[1], wound, wound })
                cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);

            Acquire("b9bddf11-acaa-40fb-bc40-eee83e478424", "IncreaseRageDamage", "buff");
            Acquire("aa94ad34-ee09-464e-89ff-2a2c2867c097", "StrongerSpikes", "spikes");
            Acquire("1fd244de-7d7b-4345-974b-4e5bf287a3df", "AwokenRegen", "regen");
            CaptureReady = true; Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-MAGNITUDE-RELIC-BATCH-PREPARED three unchanged original magnitude relics; authored starting and combat status applications, decay and friendly wounds; native Boss and wave schedule retained.");

            void Acquire(string id, string key, string statusId)
            {
                CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == id);
                RelicEffectData effect = relic.GetEffects().Single();
                StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
                if (relic.name != key || effect.GetEffectClassName() != "RelicEffectModifyStatusMagnitude" ||
                    effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetParamInt() != 1 ||
                    effect.GetEffectConditions().Count != 0 || statuses.Length != 1 ||
                    statuses[0].statusId != statusId || statuses[0].count != 0)
                    throw new InvalidOperationException("Original magnitude relic definition changed: " + key);
                save.AddRelic(relic);
            }
        }

        private static CharacterTriggerData Trigger(string kind, params CardEffectData[] effects)
        {
            var trigger = HealingScenario.HealGold(0, false, true);
            Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
            Set(trigger, "effects", effects.ToList()); return trigger;
        }

        private static CardEffectData Status(string id, int count)
        {
            var effect = new CardEffectData("CardEffectAddStatusEffect", null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.Self);
            Set(effect, "paramStatusEffects", new[] { Stack(id, count) }); return effect;
        }

        private static StatusEffectStackData Stack(string id, int count) => new StatusEffectStackData { statusId = id, count = count };
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
