using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalConduitScenario
    {
        internal const string Name = "original-conduit-relics";
        internal static bool Enabled => ConduitProbe.Enabled;
        internal static bool Prepared { get; private set; }
        internal static readonly string[] Ids = { "b0972732-ea21-42d0-844d-05fb5ff028e6", "394a559b-0c45-4bac-880a-522b04f1780c", "357c7265-74d0-4567-b793-d2fbac464d82" };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (managers.GetCombatManager()!.GetTurnCount() != 0 || save.PreviewMode)
                throw new InvalidOperationException("Conduit originals require deployment setup.");
            var owned = cards.GetAllCards(new List<CardState>()).ToArray();
            var stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!).Where(card =>
                card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true).Distinct().Take(2).ToArray();
            if (stewards.Length != 2) throw new InvalidOperationException("Two distinct Steward carriers required.");
            for (int index = 0; index < 2; index++)
            {
                var unit = stewards[index].GetSpawnCharacterData()!;
                Set(unit, "health", index == 0 ? 60 : 8); Set(unit, "attackDamage", 20); Set(unit, "size", 1); Set(unit, "equipmentLimit", 2);
                Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null);
                Set(stewards[index], "cost", 0);
            }
            var gear = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Equipment &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectAttachEquipment"));
            gear.name = "PojuConduitGear"; Set(gear, "id", "fc119000-0000-4000-8000-000000000381");
            var upgrade = DynamicUpgradeScenario.Upgrade("PojuConduitGearUpgrade", "fc119000-0000-4000-8000-000000000382", 1, 0, 0, 0, "armor", 0);
            Set(upgrade, "statusEffectUpgrades", new List<StatusEffectStackData>());
            var attach = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Monsters);
            attach.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(attach, "paramCardUpgradeData", upgrade);
            Set(gear, "effects", new List<CardEffectData> { attach }); Set(gear, "traits", new List<CardTraitData>());
            Set(gear, "triggers", new List<CardTriggerEffectData>()); Set(gear, "effectTriggers", new List<CharacterTriggerData>());
            Set(gear, "startingUpgrades", new List<CardUpgradeData>()); Set(gear, "cost", 0);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(gear);
            foreach (var card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (var definition in new[] { stewards[0], stewards[1], gear, gear })
                if (cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false) == null)
                    throw new InvalidOperationException("Conduit carrier creation failed.");
            foreach (var id in Ids)
            {
                var relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == id);
                var effect = relic.GetEffects().Single();
                if (relic.DisallowInDeploymentPhase || effect.GetParamSourceTeam() != Team.Type.Monsters ||
                    !effect.GetParamCharacterSubtype().IsNone ||
                    effect.GetEffectClassName() != (id == Ids[2] ? "RelicEffectAddStatusEffectOnSpawn" : "RelicEffectAddTrigger") ||
                    save.GetCollectedRelics().Any(item => item.GetRelicDataID() == id))
                    throw new InvalidOperationException("Original Conduit owner definition changed.");
                save.AddRelic(relic);
            }
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-CONDUIT-RELICS-PREPARED three unchanged owners: slay, equipment and first-summon; authored Steward and equipment carriers, original spells/waves/boss.");
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
