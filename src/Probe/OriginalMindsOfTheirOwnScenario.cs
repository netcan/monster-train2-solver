using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMindsOfTheirOwnScenario
    {
        internal const string Name = "original-minds-of-their-own";
        internal const string CardId = "809b4e83-cbd6-4b59-a263-d0ad76252066";
        internal const string FortifyId = "1dd23b59-fa5e-44bc-83ca-de119ede4f21";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData minds = all.FindCardData(CardId) ?? throw new InvalidOperationException("Original Minds of Their Own CardData lookup failed.");
            CardData fortify = all.FindCardData(FortifyId) ?? throw new InvalidOperationException("Original Fortify CardData lookup failed.");
            CardEffectData attack = minds.GetEffects().Single();
            CardEffectData attach = fortify.GetEffects().Single();
            if (minds.name != "MindsOfTheirOwn" || minds.GetCardType() != CardType.Spell || minds.GetCost() != 3 ||
                minds.GetTraits().Count != 0 || minds.GetCardTriggers().Count != 0 ||
                attack.GetEffectStateName() != "CardEffectAttackWithUnit" || attack.GetTargetMode() != TargetMode.Room ||
                attack.GetTargetTeamType() != Team.Type.Monsters || !attack.GetParamBool() || attack.GetParamBool2())
                throw new InvalidOperationException("Original Minds of Their Own definition changed.");
            if (fortify.name != "Fortify" || fortify.GetCardType() != CardType.Equipment || fortify.GetCost() != 0 ||
                fortify.GetTraits().Count != 0 || attach.GetEffectStateName() != "CardEffectAttachEquipment" ||
                attach.GetTargetMode() != TargetMode.DropTargetCharacter || attach.GetTargetTeamType() != Team.Type.Monsters ||
                attach.GetParamCardUpgradeData() == null)
                throw new InvalidOperationException("Original Fortify equipment definition changed.");

            CardData steward = all.FindCardData(StewardId) ?? throw new InvalidOperationException("Train Steward CardData lookup failed.");
            CharacterData unit = steward.GetSpawnCharacterData() ?? throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 20);
            Set(unit, "health", 500);
            Set(unit, "size", 1);
            Set(unit, "equipmentLimit", 4);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState existing in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                existing.Setup(steward, save);
            foreach (CardState existing in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(existing);
            cards.GetDrawPile().Clear();
            cards.GetDiscardPile().Clear();
            cards.GetExhaustedPile().Clear();

            CardState host = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState gear = cards.AddNewCard(fortify, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(minds, CardPile.HandPile, false, false, animate: false);
            if (host == null || gear == null || spell == null || !cards.GetHand().Contains(host) ||
                !cards.GetHand().Contains(gear) || !cards.GetHand().Contains(spell))
                throw new InvalidOperationException("Original Minds of Their Own scenario could not add its controlled cards.");

            CardIds = new[] { host, gear, spell }.Select(FullBattleTrace.Active!.CardId).ToArray();
            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-MINDS-OF-THEIR-OWN-PREPARED unchanged 3-cost card; one Train Steward receives original Fortify before the equipped-attack play.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
            {
                var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }

            CombatUnit? host = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                .SingleOrDefault(unit => unit.SpawnerCardId == ids[0]);
            if (host == null) return null;
            if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[1]))
            {
                var equip = new PlayCardAction(ids[1], 0, targetUnitId: host.Id);
                if (BattleActionModel.PlayCard(source, equip).Supported) return equip;
            }

            bool equipped = host.EquipmentCards?.Contains(ids[1]) == true;
            bool enemies = source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy);
            if (equipped && enemies && source.Spawn.Turn >= 1 && context.Statistics.Value(ids[2], "TimesPlayed", "ThisBattle") == 0 &&
                context.Cards.Hand.Any(card => card.InstanceId == ids[2]))
            {
                var cast = new PlayCardAction(ids[2], 0);
                if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
