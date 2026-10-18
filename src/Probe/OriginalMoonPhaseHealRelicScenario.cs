using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMoonPhaseHealRelicScenario
    {
        internal const string Name = "original-moon-phase-heal-relic";
        internal const string DataId = "6d977136-5aac-44fd-9843-54cf2a0bd01d";
        internal const string AssetKey = "MooncycleRestoreFriendlyHealth";
        private const string AcolyteId = "2b7a187f-da0b-4c34-b8c5-b18755911b43";
        private const string FaeId = "d10e2dac-fa58-4509-b0ee-112b3bf78800";
        private const int WoundCount = 2;
        private static int step;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool OnHealMode => Environment.GetEnvironmentVariable("MT2_PROBE_MOON_HEAL_CALLBACKS") == "1";
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int AcolyteCardId { get; private set; }
        internal static int[] WoundCardIds { get; private set; } = Array.Empty<int>();
        internal static int[] FaeCardIds { get; private set; } = Array.Empty<int>();

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Moon-phase heal relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("Moon-phase heal relic is already present.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-MOON-PHASE-HEAL-RELIC-ACQUIRED unchanged MooncycleRestoreFriendlyHealth; " +
                "10 health to the weakest damaged friendly unit on every moon phase.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Moon-phase heal relic missed the native deployment boundary.");
            if (managers.GetSaveManager().GetCollectedRelics().Count(item => item.GetRelicDataID() == DataId) != 1)
                throw new InvalidOperationException("Moon-phase heal relic is missing or duplicated at battle setup.");
            CheckOriginal(managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId));

            AllGameData data = managers.GetSaveManager().GetAllGameData();
            CardData acolyte = data.FindCardData(AcolyteId) ?? throw new InvalidOperationException("Original Selene Acolyte is missing.");
            CardData fae = data.FindCardData(FaeId) ?? throw new InvalidOperationException("Original Fae Meddler is missing.");
            CharacterData acolyteCharacter = acolyte.GetSpawnCharacterData() ??
                throw new InvalidOperationException("Original Selene Acolyte has no spawn character.");
            if (!fae.GetSpawnCharacterData()!.GetTriggers().Any(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.OnSpawn &&
                trigger.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectAdvanceMoonPhase")))
                throw new InvalidOperationException("Original Fae Meddler no longer advances the moon on spawn.");

            CardManager cards = managers.GetCardManager()!;
            var ownedIds = new HashSet<string>(cards.GetAllCards(new List<CardState>()).Select(card => card.GetCardDataID()), StringComparer.Ordinal);
            CardData wound = data.GetAllCardData().FirstOrDefault(card => card.GetCardType() == CardType.Spell &&
                !ownedIds.Contains(card.GetID()) && card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage"))
                ?? throw new InvalidOperationException("No unowned spell definition is available for the friendly damage control.");

            Set(acolyteCharacter, "health", 40);
            if (OnHealMode)
            {
                var triggers = acolyteCharacter.GetTriggers().ToList();
                triggers.Add(HealingScenario.HealGold(2, once: false, ignoreSilence: false));
                triggers.Add(HealingScenario.HealGold(3, once: true, ignoreSilence: false));
                Set(acolyteCharacter, "triggers", triggers);
            }
            Set(acolyte, "cost", 0);
            Set(fae, "cost", 0);
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
            damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(damage, "paramInt", 5);
            Set(wound, "effects", new List<CardEffectData> { damage });
            Set(wound, "traits", new List<CardTraitData>());
            Set(wound, "cost", 0);

            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            CardState acolyteCard = cards.AddNewCard(acolyte, CardPile.HandPile, false, false, animate: false);
            CardState[] wounds = Enumerable.Range(0, WoundCount)
                .Select(_ => cards.AddNewCard(wound, CardPile.HandPile, false, false, animate: false)).ToArray();
            CardState[] faes = Enumerable.Range(0, 2).Select(_ => cards.AddNewCard(fae, CardPile.HandPile, false, false, animate: false)).ToArray();
            if (acolyteCard == null || wounds.Any(card => card == null) || faes.Any(card => card == null) ||
                !cards.GetHand().Contains(acolyteCard) || wounds.Any(card => !cards.GetHand().Contains(card)) || faes.Any(card => !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Moon-phase heal probe could not create all five control cards in hand.");
            AcolyteCardId = FullBattleTrace.Active!.CardId(acolyteCard);
            WoundCardIds = wounds.Select(FullBattleTrace.Active.CardId).ToArray();
            FaeCardIds = faes.Select(FullBattleTrace.Active.CardId).ToArray();
            step = 0;
            Prepared = true;
            log.LogInfo("ORIGINAL-MOON-PHASE-HEAL-RELIC-PREPARED unchanged owner; two original Fae phase advances, " +
                "two controlled friendly wounds, one original Selene Acolyte; phase=" + managers.GetPlayerManager().CurrentMoonPhase);
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= 5) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction? action = null;
            if (step == 0)
                action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                    play.CardInstanceId == AcolyteCardId && play.RoomIndex == 1);
            else if (step == 1 || step == 3)
            {
                CombatUnit? target = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == AcolyteCardId);
                if (target != null)
                    action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                        play.CardInstanceId == WoundCardIds[step == 1 ? 0 : 1] && play.TargetUnitId == target.Id);
            }
            else
                action = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play =>
                    play.CardInstanceId == FaeCardIds[step == 2 ? 0 : 1] && play.RoomIndex == 0);

            if (action == null) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            BattleActionResult predicted = BattleActionModel.PlayCard(source, action);
            if (!predicted.Supported) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            step++;
            return action;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => Enabled &&
            (action.CardInstanceId == AcolyteCardId || WoundCardIds.Contains(action.CardInstanceId) || FaeCardIds.Contains(action.CardInstanceId));

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            SubtypeData? subtype = effect.GetParamCharacterSubtype();
            if (relic.name != AssetKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectHealOnMoonPhase" || effect.GetParamSourceTeam() != Team.Type.Monsters ||
                effect.GetTargetMode() != TargetMode.WeakestAllRooms || effect.GetParamInt() != 10 || effect.GetParamInt2() != -1 ||
                subtype == null || !subtype.IsNone || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original MooncycleRestoreFriendlyHealth payload changed.");
        }

        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
