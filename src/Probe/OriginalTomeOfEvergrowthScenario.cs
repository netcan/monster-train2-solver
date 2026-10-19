using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalTomeOfEvergrowthScenario
    {
        internal const string Name = "original-tome-of-evergrowth";
        internal const string CardDataId = "e3774f6e-ae41-4563-97f8-ee0b0a475a62";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData tome = all.FindCardData(CardDataId) ?? throw new InvalidOperationException("Original Tome of Evergrowth CardData lookup failed.");
            CardEffectData[] effects = tome.GetEffects().ToArray();
            CardTraitData[] traits = tome.GetTraits().ToArray();
            CardData ability = effects.SingleOrDefault()?.GetParamCardData() ?? throw new InvalidOperationException("Tome of Evergrowth has no native unit ability definition.");
            if (tome.name != "TomeOfEvergrowth" || tome.GetCardType() != CardType.Spell || effects.Length != 1 ||
                effects[0].GetEffectStateName() != "CardEffectSetUnitAbility" || effects[0].GetTargetMode() != TargetMode.DropTargetCharacter ||
                !ability.IsUnitAbility() || traits.Length != 1 || traits[0].GetTraitStateName() != "CardTraitExhaustState")
                throw new InvalidOperationException($"Original Tome of Evergrowth definition changed: name={tome.name}, type={tome.GetCardType()}, cost={tome.GetCost()}, effects=[{string.Join(";", effects.Select(effect => effect.GetEffectStateName() + ":" + effect.GetTargetMode()))}], ability={ability.name}:{ability.GetID()} unitAbility={ability.IsUnitAbility()}, traits=[{string.Join(";", traits.Select(trait => trait.GetTraitStateName()))}].");
            log.LogInfo($"ORIGINAL-TOME-OF-EVERGROWTH-DEFINITION cost={tome.GetCost()} costType={tome.GetCostType()} target={effects[0].GetTargetMode()} team={effects[0].GetTargetTeamType()} ability={ability.name}:{ability.GetID()} cooldown={ability.GetCooldownAfterActivated()} cooldownAtSpawn={ability.GetCooldownAtSpawn()}.");

            CardData steward = all.FindCardData(StewardId) ?? throw new InvalidOperationException("Train Steward CardData lookup failed.");
            CharacterData unit = steward.GetSpawnCharacterData() ?? throw new InvalidOperationException("Train Steward has no spawn character.");
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 9999);
            Set(unit, "health", 9999);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState state in cards.GetAllCards(new List<CardState>()).Where(state => state.GetCardDataID() == steward.GetID()))
                state.Setup(steward, save);
            foreach (CardState state in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(state);
            cards.GetDrawPile().Clear();
            cards.GetDiscardPile().Clear();
            cards.GetExhaustedPile().Clear();

            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(StewardId), Add(StewardId), Add(CardDataId) };
            if (created.Any(card => card == null || !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Original Tome of Evergrowth scenario could not add its controlled cards.");
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-TOME-OF-EVERGROWTH-PREPARED unchanged ability-setting Tome with two controlled original Stewards on separate floors.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            for (int index = 0; index < ids.Length; index++)
            {
                if (context.Statistics!.Value(ids[index], "TimesPlayed", "ThisBattle") != 0 ||
                    !context.Cards.Hand.Any(card => card.InstanceId == ids[index])) continue;
                int room = index == 1 ? 1 : 0;
                int target = 0;
                if (index == 2)
                    target = source.Spawn.Train.Rooms[0].Units.Single(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).Id;
                var action = new PlayCardAction(ids[index], room, targetUnitId: target, playerPosition: index < 2 ? 0 : -1);
                if (BattleActionModel.PlayCard(source, action).Supported) return action;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
