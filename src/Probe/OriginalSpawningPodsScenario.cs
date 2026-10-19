using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSpawningPodsScenario
    {
        internal const string Name = "original-spawning-pods";
        internal const string CardId = "7c772030-474d-414c-939d-9581c6a70741";
        internal const string EagerConscriptId = "0c8ff108-478c-4c1c-9c49-f6a297b4e0d8";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData pods = all.FindCardData(CardId) ?? throw new InvalidOperationException("Original Spawning Pods CardData lookup failed.");
            CardData eager = all.FindCardData(EagerConscriptId) ?? throw new InvalidOperationException("Eager Conscript CardData lookup failed.");
            CardEffectData effect = pods.GetEffects().Single();
            StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
            CardTraitData[] traits = pods.GetTraits().ToArray();
            if (pods.name != "SpawningPods" || pods.GetCardType() != CardType.Spell || pods.GetCost() != 2 ||
                effect.GetEffectStateName() != "CardEffectSpawnMonsterOrAddStatusEffect" || effect.GetTargetMode() != TargetMode.Room ||
                effect.GetTargetTeamType() != Team.Type.Monsters || effect.GetTargetCharacterSubtype()?.Key != "SubtypesData_Horde" ||
                effect.GetParamInt() != 4 || !effect.GetParamBool() || statuses.Length != 1 ||
                statuses[0].statusId != "horde" || statuses[0].count != 4 || effect.GetParamCharacterData()?.name != "MonsterFunguy" ||
                traits.Length != 1 || traits[0].GetTraitStateName() != "CardTraitExhaustState" || traits[0].GetParamInt() != 0 ||
                traits[0].GetParamTrackedValue().ToString() != "SubtypeInDeck" || traits[0].GetParamEntryDuration().ToString() != "ThisTurn" ||
                pods.GetCardTriggers().Count != 0 || eager.name != "EagerConscript" || eager.GetCost() != 1 || eager.GetTraits().Count != 0)
                throw new InvalidOperationException("Original Spawning Pods definition changed.");
            CardEffectData eagerEffect = eager.GetEffects().Single();
            StatusEffectStackData[] eagerStatuses = eagerEffect.GetParamStatusEffects();
            if (eagerEffect.GetEffectStateName() != "CardEffectSpawnMonsterOrAddStatusEffect" || eagerEffect.GetTargetMode() != TargetMode.Room ||
                eagerEffect.GetTargetCharacterSubtype()?.Key != "SubtypesData_Horde" || eagerEffect.GetParamInt() != 1 ||
                eagerStatuses.Length != 1 || eagerStatuses[0].statusId != "horde" || eagerStatuses[0].count != 1 ||
                eagerEffect.GetParamCharacterData()?.name != "MonsterFunguy")
                throw new InvalidOperationException("Eager Conscript control definition changed.");

            CardData steward = all.FindCardData(StewardId) ?? throw new InvalidOperationException("Train Steward CardData lookup failed.");
            CharacterData unit = steward.GetSpawnCharacterData() ?? throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0); Set(unit, "attackDamage", 9999); Set(unit, "health", 9999); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState existing in cards.GetAllCards(new List<CardState>()).Where(item => item.GetCardDataID() == steward.GetID()))
                existing.Setup(steward, save);
            foreach (CardState existing in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(existing);
            cards.GetDrawPile().Clear(); cards.GetDiscardPile().Clear(); cards.GetExhaustedPile().Clear();

            CardState anchor = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState setup = cards.AddNewCard(eager, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(pods, CardPile.HandPile, false, false, animate: false);
            if (anchor == null || setup == null || spell == null || !cards.GetHand().Contains(anchor) ||
                !cards.GetHand().Contains(setup) || !cards.GetHand().Contains(spell))
                throw new InvalidOperationException("Original Spawning Pods scenario could not add its controlled cards.");
            CardIds = new[] { anchor, setup, spell }.Select(FullBattleTrace.Active!.CardId).ToArray();

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SPAWNING-PODS-PREPARED unchanged 2-cost Horde +4 spell and consumed Eager Conscript control to seed one matching target.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
            {
                var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }
            if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[1]))
            {
                var seed = new PlayCardAction(ids[1], 0);
                if (BattleActionModel.PlayCard(source, seed).Supported) return seed;
            }
            bool hordeReady = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit =>
                unit.Team == CombatTeam.Player && unit.AssetKey == "MonsterFunguy" && (unit.Status("horde")?.Stacks ?? 0) == 1);
            if (hordeReady && context.Statistics.Value(ids[2], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[2]))
            {
                var cast = new PlayCardAction(ids[2], 0);
                if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
