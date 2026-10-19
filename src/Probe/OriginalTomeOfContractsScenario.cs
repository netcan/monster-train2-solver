using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalTomeOfContractsScenario
    {
        internal const string Name = "original-tome-of-contracts";
        internal const string CardId = "2a6c6ce2-f2c7-4be6-8417-8d8e72b82673";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            CardData tome = all.FindCardData(CardId)!;
            if (tome.name != "TomeOfContracts" || tome.GetCardType() != CardType.Spell || tome.GetCost() != 1 ||
                tome.GetEffects().Count != 1 || tome.GetEffects()[0].GetEffectStateName() != "CardEffectAdjustAbilityCooldown" ||
                tome.GetEffects()[0].GetParamInt() != -1 || tome.GetEffects()[0].GetParamBool() ||
                tome.GetEffects()[0].GetTargetMode() != TargetMode.DropTargetCharacter ||
                !tome.GetTraits().Any(trait => trait.GetTraitStateName() == "CardTraitExhaustState"))
                throw new InvalidOperationException("Original Tome of Contracts definition changed.");

            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            CardData ability = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            Set(ability, "isUnitAbility", true); Set(ability, "cooldownAtSpawn", 2); Set(ability, "cooldownAfterActivated", 3);
            ability.name = "PojuTomeOfContractsAbility";
            AccessTools.Field(typeof(GameData), "id").SetValue(ability, "c2f6ed7f-18ce-4070-b65f-7dd9f5160066");
            ((System.Collections.Generic.List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(ability);
            Set(unit, "unitAbility", ability);

            foreach (CardState card in cards.GetAllCards(new System.Collections.Generic.List<CardState>())
                .Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();

            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward), Add(steward), Add(tome) };
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-TOME-OF-CONTRACTS-PREPARED unchanged 1-cost cooldown -1 Spell and two friendly ability controls.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (CardIds.Length != 3) return null;
            if (CardIds.Take(2).Any(id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0))
                return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);

            CardToken? tome = source.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.DataId == CardId);
            bool targetReady = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                .Any(unit => unit.Team == CombatTeam.Player && unit.Ability?.HasAbility == true);
            if (tome == null || !targetReady) return null;
            PlayCardAction? selected = BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            return selected?.CardInstanceId == tome.InstanceId ? selected : null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
