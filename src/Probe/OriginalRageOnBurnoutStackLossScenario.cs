using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRageOnBurnoutStackLossScenario
    {
        internal const string Name = "original-rage-on-burnout-stack-loss";
        internal const string OwnerId = "85667d60-8053-4204-8c02-ba101dab7945";
        internal const string CarrierId = "fc119000-0000-4000-8000-000000000856";
        internal const string RemovalSpellId = "fc119000-0000-4000-8000-000000000857";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        private static int carrierCardId;
        private static int removalSpellCardId;
        private static bool removalChosen;
        private static readonly HashSet<int> ScenarioActionIds = new HashSet<int>();

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("RageOnBurnoutStackLoss must be acquired before battle in an isolated save.");
            AllGameData data = save.GetAllGameData();
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = relic.GetEffects().Single();
            StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
            if (relic.name != "RageOnBurnoutStackLoss" || effect.GetEffectClassName() != RelicModel.AddStatusOnOtherStatusRemoved ||
                effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetParamString() != "ephemeral" ||
                effect.GetEffectConditions().Count != 0 || statuses.Length != 1 || statuses[0].statusId != "buff" ||
                statuses[0].count != 3 || save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("The unchanged RageOnBurnoutStackLoss owner or isolated acquisition state changed.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-RAGE-ON-BURNOUT-STACK-LOSS-ACQUIRED unchanged +3 Rage per friendly Ephemeral stack removed.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            if (Prepared || !Acquired || save.PreviewMode || combat.GetTurnCount() != 0 ||
                !save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("RageOnBurnoutStackLoss requires its isolated deployment state.");

            AllGameData data = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData carrier = owned.Select(card => data.FindCardData(card.GetCardDataID()))
                .FirstOrDefault(card => card?.GetSpawnCharacterData() != null)
                ?? throw new InvalidOperationException("A native monster carrier is required for the Ephemeral removal witness.");
            CharacterData character = carrier.GetSpawnCharacterData()!;
            Set(character, "health", 80);
            Set(character, "attackDamage", 20);
            Set(character, "size", 1);
            Set(character, "equipmentLimit", 3);
            Set(character, "startingStatusEffects", new[] { Stack("ephemeral", 1) });
            Set(character, "triggers", new List<CharacterTriggerData>());
            Set(character, "unitAbility", null);
            Set(carrier, "cost", 0);
            Set(carrier, "traits", new List<CardTraitData>());
            Set(carrier, "triggers", new List<CardTriggerEffectData>());
            Set(carrier, "effectTriggers", new List<CharacterTriggerData>());
            Set(carrier, "startingUpgrades", new List<CardUpgradeData>());

            CardData spell = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectDamage");
            var removalSpell = UnityEngine.Object.Instantiate(spell);
            Configure(removalSpell, "PojuRageOnBurnoutStackLossRemoval", RemovalSpellId);
            var removal = new CardEffectData("CardEffectRemoveStatusEffect", null!, Team.Type.Monsters);
            removal.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(removal, "paramStatusEffects", new[] { Stack("ephemeral", 1) });
            Set(removalSpell, "effects", new List<CardEffectData> { removal });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)!).Add(removalSpell);

            foreach (CardState card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            ScenarioActionIds.Clear();
            removalChosen = false;
            CardState carrierCard = cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false)
                ?? throw new InvalidOperationException("RageOnBurnoutStackLoss carrier creation failed.");
            CardState spellCard = cards.AddNewCard(removalSpell, CardPile.HandPile, false, false, animate: false)
                ?? throw new InvalidOperationException("RageOnBurnoutStackLoss removal spell creation failed.");
            carrierCardId = FullBattleTrace.Active!.CardId(carrierCard);
            removalSpellCardId = FullBattleTrace.Active.CardId(spellCard);

            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            log.LogInfo("ORIGINAL-RAGE-ON-BURNOUT-STACK-LOSS-PREPARED one native Steward starts with Ephemeral; authored zero-cost spell removes one stack after deployment.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!Prepared) return null;
            CombatContext context = state.Spawn.Train.Context!;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(state);
            CombatUnit? carrier = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == 80 && unit.Health > 0);
            if (carrier == null)
            {
                PlayCardAction? summon = plays.FirstOrDefault(play => play.CardInstanceId == carrierCardId && play.RoomIndex == 0);
                return summon == null ? null : Mark(summon);
            }
            if (!removalChosen && (carrier.Status("ephemeral")?.Stacks ?? 0) > 0)
            {
                PlayCardAction? removal = plays.FirstOrDefault(play => play.CardInstanceId == removalSpellCardId &&
                    play.TargetUnitId == carrier.Id && play.RoomIndex == state.Spawn.Train.Rooms.First(room => room.Units.Contains(carrier)).RoomIndex);
                if (removal != null) { removalChosen = true; return Mark(removal); }
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state,
                card => card.InstanceId != carrierCardId && card.InstanceId != removalSpellCardId);
        }

        internal static bool IsScenarioAction(PlayCardAction action) => Enabled && ScenarioActionIds.Remove(action.CardInstanceId);

        private static PlayCardAction Mark(PlayCardAction action)
        { ScenarioActionIds.Add(action.CardInstanceId); return action; }

        private static void Configure(CardData card, string key, string id)
        {
            card.name = key; Set(card, "id", id); Set(card, "cost", 0);
            Set(card, "traits", new List<CardTraitData>()); Set(card, "triggers", new List<CardTriggerEffectData>());
            Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }

        private static StatusEffectStackData Stack(string id, int count) => new StatusEffectStackData { statusId = id, count = count };
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
