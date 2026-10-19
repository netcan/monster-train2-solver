using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFertileGroundScenario
    {
        internal const string Name = "original-fertile-ground";
        internal const string CardId = "0c718572-ef91-4152-b6a9-0fdf907a9b06";
        internal const string EagerConscriptId = "0c8ff108-478c-4c1c-9c49-f6a297b4e0d8";
        internal const string SpawningPodsId = "7c772030-474d-414c-939d-9581c6a70741";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData fertile = all.FindCardData(CardId) ?? throw new InvalidOperationException("Original Fertile Ground CardData lookup failed.");
            CardData eager = all.FindCardData(EagerConscriptId) ?? throw new InvalidOperationException("Eager Conscript CardData lookup failed.");
            CardData pods = all.FindCardData(SpawningPodsId) ?? throw new InvalidOperationException("Spawning Pods CardData lookup failed.");
            CardEffectData effect = fertile.GetEffects().Single();
            StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
            CardTraitData[] traits = fertile.GetTraits().ToArray();
            CardTraitData scaling = traits.SingleOrDefault(trait => trait.GetTraitStateName() == "CardTraitScalingAddStatusEffect")!;
            if (fertile.name != "FertileGround" || fertile.GetCardType() != CardType.Spell || fertile.GetCost() != 2 ||
                fertile.GetCardTriggers().Count != 0 || effect.GetEffectStateName() != "CardEffectSpawnMonsterOrAddStatusEffect" ||
                effect.GetTargetMode() != TargetMode.Room || effect.GetTargetTeamType() != Team.Type.Monsters ||
                effect.GetTargetCharacterSubtype()?.Key != "SubtypesData_Horde" || effect.GetParamInt() != 1 ||
                statuses.Length != 1 || statuses[0].statusId != "horde" || statuses[0].count != 1 ||
                effect.GetParamCharacterData()?.name != "MonsterFunguy" || traits.Length != 2 || scaling == null ||
                scaling.GetParamInt() != 1 || scaling.GetParamInt2() != 2 || scaling.GetParamInt3() != 1 ||
                scaling.GetParamTrackedValue().ToString() != "TypeInExhaustPile" || scaling.GetParamEntryDuration().ToString() != "ThisBattle" ||
                traits.Count(trait => trait.GetTraitStateName() == "CardTraitExhaustState") != 1)
                throw new InvalidOperationException("Original Fertile Ground definition or scaling trait changed.");

            CardEffectData eagerEffect = eager.GetEffects().Single();
            StatusEffectStackData[] eagerStatuses = eagerEffect.GetParamStatusEffects();
            CardEffectData podsEffect = pods.GetEffects().Single();
            StatusEffectStackData[] podsStatuses = podsEffect.GetParamStatusEffects();
            if (eager.name != "EagerConscript" || eager.GetCost() != 1 || eager.GetTraits().Count != 0 ||
                eagerEffect.GetEffectStateName() != "CardEffectSpawnMonsterOrAddStatusEffect" || eagerEffect.GetTargetMode() != TargetMode.Room ||
                eagerEffect.GetTargetCharacterSubtype()?.Key != "SubtypesData_Horde" || eagerEffect.GetParamInt() != 1 ||
                eagerStatuses.Length != 1 || eagerStatuses[0].statusId != "horde" || eagerStatuses[0].count != 1 ||
                eagerEffect.GetParamCharacterData()?.name != "MonsterFunguy" || pods.name != "SpawningPods" || pods.GetCost() != 2 ||
                podsEffect.GetEffectStateName() != "CardEffectSpawnMonsterOrAddStatusEffect" || podsEffect.GetTargetMode() != TargetMode.Room ||
                podsEffect.GetTargetCharacterSubtype()?.Key != "SubtypesData_Horde" || podsEffect.GetParamInt() != 4 ||
                podsStatuses.Length != 1 || podsStatuses[0].statusId != "horde" || podsStatuses[0].count != 4 ||
                podsEffect.GetParamCharacterData()?.name != "MonsterFunguy" ||
                pods.GetTraits().Count(trait => trait.GetTraitStateName() == "CardTraitExhaustState") != 1)
                throw new InvalidOperationException("Original Fertile Ground control card definitions changed.");

            CardData steward = all.FindCardData(StewardId) ?? throw new InvalidOperationException("Train Steward CardData lookup failed.");
            CharacterData unit = steward.GetSpawnCharacterData() ?? throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 0);
            Set(unit, "health", 500);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState existing in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID()))
                existing.Setup(steward, save);
            foreach (CardState existing in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(existing);
            cards.GetDrawPile().Clear();
            cards.GetDiscardPile().Clear();
            cards.GetExhaustedPile().Clear();

            CardState host = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState setup = cards.AddNewCard(eager, CardPile.HandPile, false, false, animate: false);
            CardState exhaustSpell = cards.AddNewCard(pods, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(fertile, CardPile.HandPile, false, false, animate: false);
            if (host == null || setup == null || exhaustSpell == null || spell == null ||
                new[] { host, setup, exhaustSpell, spell }.Any(card => !cards.GetHand().Contains(card)))
                throw new InvalidOperationException("Original Fertile Ground scenario could not add its controlled cards.");

            CardIds = new[] { host, setup, exhaustSpell, spell }.Select(FullBattleTrace.Active!.CardId).ToArray();
            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-FERTILE-GROUND-PREPARED original Horde scaling; Eager Conscript seeds one stack and Spawning Pods supplies one exhausted Spell.");
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

            CombatUnit? horde = source.Spawn.Train.Rooms[0].Units.SingleOrDefault(unit =>
                unit.Team == CombatTeam.Player && unit.AssetKey == "MonsterFunguy");
            if (horde?.Status("horde")?.Stacks == 1 && context.Statistics.Value(ids[2], "TimesPlayed", "ThisBattle") == 0 &&
                context.Cards.Hand.Any(card => card.InstanceId == ids[2]))
            {
                var pods = new PlayCardAction(ids[2], 0);
                if (BattleActionModel.PlayCard(source, pods).Supported) return pods;
            }

            bool spellExhausted = context.OtherPiles?.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]) == true;
            if (horde?.Status("horde")?.Stacks == 5 && spellExhausted && source.Spawn.Turn >= 1 &&
                context.Statistics.Value(ids[3], "TimesPlayed", "ThisBattle") == 0 && context.Cards.Hand.Any(card => card.InstanceId == ids[3]))
            {
                var cast = new PlayCardAction(ids[3], 0);
                if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
