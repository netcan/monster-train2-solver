using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class TypedDrawScenario
    {
        internal static bool Prepared { get; private set; }
        private static string[] priorityCards = Array.Empty<string>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            AllGameData gameData = save.GetAllGameData();
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardState[] rallies = owned.Where(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (rallies.Length == 0) throw new InvalidOperationException("Typed draw fixture requires Rally cards.");
            CardData rally = gameData.FindCardData(rallies[0].GetCardDataID())!;
            CardUpgradeMaskData monsters = Mask("PojuTypedDrawMonster", CardType.Monster);
            CardUpgradeMaskData spells = Mask("PojuTypedDrawSpell", CardType.Spell);
            CardUpgradeMaskData absent = Mask("PojuTypedDrawAbsentEquipment", CardType.Equipment);
            CardUpgradeData basic = Upgrade("PojuTypedDrawShared", "b610ad23-06d1-47ee-8000-000000000001", 2, 3);
            CardUpgradeData unique = Upgrade("PojuTypedDrawUnique", "b610ad23-06d1-47ee-8000-000000000002", 1, 1);
            Set(unique, "isUnique", true);
            CardUpgradeData filtered = Upgrade("PojuTypedDrawFiltered", "b610ad23-06d1-47ee-8000-000000000003", 3, 2);
            Set(filtered, "filters", new List<CardUpgradeMaskData> { monsters });
            CardUpgradeData final = Upgrade("PojuTypedDrawAfterFilter", "b610ad23-06d1-47ee-8000-000000000004", 4, 0);
            CardEffectData ranged = Draw(0, CardType.Spell);
            Set(ranged, "useIntRange", true); Set(ranged, "paramMinInt", -2); Set(ranged, "paramMaxInt", 4); Set(ranged, "paramMultiplier", .75f);
            rally.GetEffects().Clear();
            rally.GetEffects().AddRange(new[]
            {
                HandUpgrade(basic),
                Next(basic), Next(basic), Next(unique), Next(unique),
                Draw(1, CardType.Monster), Draw(1, CardType.Spell), HandUpgrade(basic),
                Next(filtered), Next(final),
                Draw(1, CardType.Monster), Draw(2, CardType.Invalid, absent, spells),
                Draw(0, CardType.Spell), Draw(-2, CardType.Monster), ranged,
                Draw(1, CardType.Equipment), Draw(99, CardType.Invalid, spells, monsters),
                Draw(1, CardType.Invalid, monsters, spells), Draw(1, CardType.Spell)
            });
            Set(rally, "targetless", false); Set(rally, "targetsRoom", true); Set(rally, "cost", 1);
            // A drawing source can redraw a different copy. SelfPurge keeps this
            // coverage policy finite after Gifts makes its drawn spells free.
            var consume = new CardTraitData(); consume.Setup("CardTraitSelfPurge");
            Set(rally, "traits", rally.GetTraits().Concat(new[] { consume }).ToList());
            foreach (CardState card in rallies) card.Setup(rally, save);

            CardData damage = gameData.FindCardData(owned.First(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage")).GetCardDataID())!;
            var pool = ScriptableObject.CreateInstance<CardPool>(); pool.name = "PojuTypedDrawStrongSpellPool";
            object poolCards = AccessTools.Field(typeof(CardPool), "cardDataList").GetValue(pool);
            AccessTools.Method(poolCards.GetType(), "Add", new[] { typeof(CardData) }).Invoke(poolCards, new object[] { damage });
            var generation = new CardEffectData("CardEffectAddBattleCard", null!, Team.Type.None);
            generation.Cheat_SetTargetMode(TargetMode.Room); Set(generation, "paramCardPool", pool);
            Set(generation, "paramInt", (int)CardPile.DeckPile); Set(generation, "additionalParamInt", 3);
            Set(generation, "paramCardUpgradeData", basic); rally.GetEffects().Add(generation);
            var bonus = new CardEffectData("CardEffectDrawAdditionalNextTurn", null!, Team.Type.None);
            bonus.Cheat_SetTargetMode(TargetMode.Room); Set(bonus, "paramInt", 2); Set(bonus, "paramCardUpgradeData", basic);
            rally.GetEffects().Add(bonus);
            foreach (CardState card in rallies) card.Setup(rally, save);
            var stronger = new CardTraitData(); stronger.Setup("CardTraitStrongerMagicPower");
            Set(damage, "traits", damage.GetTraits().Concat(new[] { stronger }).ToList());
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == damage.GetID()))
                card.Setup(damage, save);
            // Both originals retain their authored effects, costs and upgrades.
            CardData channelsong = gameData.GetAllCardData().Single(card => card.name == "Channelsong");
            CardData gifts = gameData.GetAllCardData().Single(card => card.name == "GiftsForAGuard");
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            for (int index = 0; index < 5; index++) cards.AddNewCard(damage, CardPile.DeckPile, false, false, animate: false);
            cards.AddNewCard(channelsong, CardPile.HandPile, false, false, animate: false);
            cards.AddNewCard(gifts, CardPile.HandPile, false, false, animate: false);
            if (!cards.DrawSpecificCard(rallies[0])) throw new InvalidOperationException("Could not draw the typed-draw source.");
            // The relic's acquisition resets temporary modifiers. Install the
            // duplicate-ID roots afterward so the actual draw encounters them.
            if (RelicCardModifierProbe.Enabled && !RelicCardModifierScenario.Prepared)
                RelicCardModifierScenario.Prepare(managers, log);
            if (rallies.Length < 4) throw new InvalidOperationException("Typed draw alias coverage requires four source copies.");
            CardUpgradeData strongRecipient = Upgrade("PojuTypedDrawStrongRecipient", "b610ad23-06d1-47ee-8000-000000000005", 0, 0);
            Set(strongRecipient, "traitDataUpgrades", new List<CardTraitData> { stronger });
            // One later source copy has a different multiplier. Subsequent
            // native hand upgrades refresh its shared queue objects after the
            // previous play cleared the queue, including their other owners.
            var strongState = new CardUpgradeState(); strongState.Setup(strongRecipient);
            rallies[rallies.Length - 2].ApplyTemporaryUpgrade(strongState);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == damage.GetID()))
            {
                var existing = new CardUpgradeState(); existing.Setup(basic);
                existing.SetAttackDamage(7); card.GetTemporaryCardStateModifiers().AddUpgrade(existing);
                var existingUnique = new CardUpgradeState(); existingUnique.Setup(unique);
                existingUnique.SetAttackDamage(9); card.GetTemporaryCardStateModifiers().AddUpgrade(existingUnique);
            }
            priorityCards = new[] { channelsong.GetID(), gifts.GetID(), rally.GetID() };
            managers.GetPlayerManager().AddEnergy(6);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            Prepared = true;
            log.LogInfo("TYPED-DRAW-PREPARED native Channelsong/GiftsForAGuard, type/primary/secondary selection, signed/ranged/absent/full-hand draws, shared and unique upgrades, existing duplicate IDs, filter short-circuit, hand/generation/future-draw refresh and fivefold magic power.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            foreach (string dataId in priorityCards)
            foreach (CardToken card in source.Spawn.Train.Context!.Cards.Hand.Where(card => card.DataId == dataId))
            {
                var action = new PlayCardAction(card.InstanceId, 0);
                if (BattleActionModel.PlayCard(source, action).Supported) return action;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }

        private static CardUpgradeMaskData Mask(string name, CardType cardType)
        {
            var mask = ScriptableObject.CreateInstance<CardUpgradeMaskData>(); mask.name = name;
            Set(mask, "cardType", cardType); return mask;
        }
        private static CardUpgradeData Upgrade(string name, string id, int damage, int hp)
        {
            CardUpgradeData data = DynamicUpgradeScenario.Upgrade(name, id, damage, hp, 0, 0, "armor", 0);
            Set(data, "statusEffectUpgrades", new List<StatusEffectStackData>()); Set(data, "bonusHeal", damage);
            return data;
        }
        private static CardEffectData Next(CardUpgradeData upgrade)
        {
            var effect = new CardEffectData("CardEffectAddTempCardUpgradeToNextDrawnCard", null!, Team.Type.None);
            effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramCardUpgradeData", upgrade); return effect;
        }
        private static CardEffectData HandUpgrade(CardUpgradeData upgrade)
        {
            var effect = new CardEffectData("CardEffectAddTempCardUpgradeToCardsInHand", null!, Team.Type.None);
            effect.Cheat_SetTargetMode(TargetMode.Hand); Set(effect, "paramCardUpgradeData", upgrade); return effect;
        }
        private static CardEffectData Draw(int count, CardType type, CardUpgradeMaskData? primary = null, CardUpgradeMaskData? secondary = null)
        {
            var effect = new CardEffectData("CardEffectDrawType", null!, Team.Type.None);
            effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", count); Set(effect, "targetCardType", type);
            Set(effect, "paramCardFilter", primary!); Set(effect, "paramCardFilterSecondary", secondary!); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
