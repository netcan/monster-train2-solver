using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalOpeningDrawScenario
    {
        internal const string Name = "original-opening-draw-relics";
        internal static readonly string[] Ids = { "7a1d5b5d-cd08-4935-b629-d3d441905100", "d245f9d5-e966-4a8e-a125-4aa5b48f1e7c", "69d88251-cfed-4fd0-a355-cba2e10e09d4" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static string Phase { get; private set; } = "natural";
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Original opening draw owners must precede battle.");
            foreach (var card in save.GetDeckState())
            {
                var unit = data.FindCardData(card.GetCardDataID())?.GetSpawnCharacterData();
                if (unit == null) continue;
                Set(unit, "health", 80); Set(unit, "attackDamage", 20); Set(unit, "size", 1);
                Set(data.FindCardData(card.GetCardDataID())!, "cost", 0);
            }
            var gear = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Equipment &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectAttachEquipment"));
            Configure(gear, "PojuOpeningDrawGear", "fc119000-0000-4000-8000-000000000491");
            var upgrade = DynamicUpgradeScenario.Upgrade("PojuOpeningDrawGearUpgrade", "fc119000-0000-4000-8000-000000000492", 1, 0, 0, 0, "armor", 0);
            Set(upgrade, "statusEffectUpgrades", new List<StatusEffectStackData>());
            var attach = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Monsters); attach.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(attach, "paramCardUpgradeData", upgrade); Set(gear, "effects", new List<CardEffectData> { attach });
            var room = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.TrainRoomAttachment));
            Configure(room, "PojuOpeningDrawRoom", "fc119000-0000-4000-8000-000000000493");
            var inert = new CardEffectData("CardEffectNULL", null!, Team.Type.Monsters); inert.Cheat_SetTargetMode(TargetMode.Room);
            Set(room, "effects", new List<CardEffectData> { inert });
            CardData Deployment(CardData template, string key, string id)
            {
                var card = UnityEngine.Object.Instantiate(template); Configure(card, key, id);
                var trait = new CardTraitData(); trait.Setup("CardTraitDrawInDeploymentPhase"); card.GetTraits().Add(trait); return card;
            }
            var deploymentGear = Deployment(gear, "PojuOpeningDeploymentGear", "fc119000-0000-4000-8000-000000000494");
            var deploymentRoom = Deployment(room, "PojuOpeningDeploymentRoom", "fc119000-0000-4000-8000-000000000495");
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).AddRange(new[] { gear, room, deploymentGear, deploymentRoom });
            foreach (var card in new[] { gear, room, deploymentGear, deploymentRoom }) save.AddCardToDeck(card);
            foreach (string id in Ids)
            {
                var owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == id);
                if (owner.DisallowInDeploymentPhase || owner.GetEffects().Count != 1 || owner.GetEffects()[0].GetEffectConditions().Count != 0 ||
                    save.GetCollectedRelics().Any(item => item.GetRelicDataID() == id)) throw new InvalidOperationException("Original opening draw owner changed.");
                save.AddRelic(owner);
            }
            Acquired = true;
            log.LogInfo("ORIGINAL-OPENING-DRAW-ACQUIRED three unchanged owners before native deployment; numeric temporary upgrades and original filters.");
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared || !Acquired || managers.GetCombatManager().GetTurnCount() != 0) throw new InvalidOperationException("Opening draw controls require deployment.");
            var cards = managers.GetCardManager();
            var parameters = new RelicEffectParams { saveManager = managers.GetSaveManager(), relicManager = managers.GetRelicManager(), screenManager = managers.GetScreenManager() };
            Phase = "fallback";
            foreach (string id in Ids.Take(2))
            {
                // The primary excludes deployment traits; the secondary permits
                // them while retaining the same Equipment/TrainRoomAttachment type.
                var type = id == Ids[0] ? CardType.Equipment : CardType.TrainRoomAttachment;
                var carrier = cards.GetHand().FirstOrDefault(card => card.GetCardType() == type && card.HasTrait<CardTraitDrawInDeploymentPhase>());
                if (carrier == null) throw new InvalidOperationException("Missing actual cascade fallback carrier.");
                cards.MoveCardFromHandToTopOfDeck(carrier);
                var effect = managers.GetSaveManager().GetCollectedRelics().Single(owner => owner.GetRelicDataID() == id).GetEffects().OfType<RelicEffectDrawOnPreOpeningDraw>().Single();
                if (!effect.TestEffectOnPreOpeningDraw(parameters, managers.GetCoreManagers())) throw new InvalidOperationException("Original cascade unexpectedly inactive.");
                Drain(effect.ApplyEffectOnPreOpeningDraw(parameters, managers.GetCoreManagers()));
            }
            Phase = "after-controls"; Prepared = true;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-OPENING-DRAW-PREPARED natural primary selections plus two actual controlled fallback consumers.");
        }
        private static void Drain(IEnumerator routine)
        { try { while (routine.MoveNext()) { if (routine.Current is IEnumerator nested) Drain(nested); else if (routine.Current != null) throw new InvalidOperationException("Asynchronous opening control."); } } finally { (routine as IDisposable)?.Dispose(); } }
        private static void Configure(CardData card, string key, string id)
        {
            card.name = key; Set(card, "id", id); Set(card, "cost", 2); Set(card, "traits", new List<CardTraitData>());
            Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
