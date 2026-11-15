#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class SpawnStatusScalingScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "spawn-status-scaling";
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definition { get; private set; }
        private static MutatorData? original;
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        private static object Describe(MutatorData data) => new { Id = data.GetID(), Key = data.name,
            Effects = data.GetEffects().Select(effect => new { Type = effect.GetEffectClassName(), Multiplier = effect.GetParamFloat(),
                Starting = effect.GetParamBool(), Applied = effect.GetParamBool2(), Listed = effect.GetParamBool3(),
                Statuses = effect.GetParamStatusEffects(), Excluded = effect.GetParamExcludeCharacterSubtypes().Select(value => value.Key).ToArray(),
                Conditions = effect.GetEffectConditions().Count }).ToArray() };
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData();
            if (original != null) return;
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original birth scaling must be acquired before battle setup.");
            original = SpawnStatusScalingMechanismBatch.Owners(all).OfType<MutatorData>().Single(value => value.GetID() == "78b4c7f0-dcae-4db5-8f69-f9f6b969348e");
            if (original.name != "SoulSavior_RunDistance4" || original.GetEffects().Count != 3 ||
                original.GetEffects().Count(effect => effect.GetEffectClassName() == RelicSpawnStatusScalingModel.Effect) != 1)
                throw new InvalidOperationException("Original endless birth scaling owner changed.");
            Definition = Describe(original); save.AddMutator(original);
            log.LogInfo("SPAWN-STATUS-SCALING-ACQUIRED unchanged original endless mutator before Pyre setup.");
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var cards = managers.GetCardManager(); var all = save.GetAllGameData();
            if (save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Birth status scaling requires isolated deployment.");
            if (original == null || save.GetMutators().Count != 1 || save.GetMutators()[0].GetRelicDataID() != original.GetID())
                throw new InvalidOperationException("Birth scaling missed original pre-battle acquisition.");
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            foreach (CharacterData enemy in waves.SelectMany(wave => ((System.Collections.IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups").GetValue(wave))
                .Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss())).Distinct())
            {
                var statuses = enemy.GetStartingStatusEffects().Where(status => status.statusId != "buff").ToList();
                statuses.Add(new StatusEffectStackData { statusId = "buff", count = 3 });
                AccessTools.Field(typeof(CharacterData), "startingStatusEffects").SetValue(enemy, statuses.ToArray());
                var triggers = enemy.GetTriggers().ToList(); triggers.Add(SpawnStatusScalingMechanismBatch.Trigger(3, false));
                AccessTools.Field(typeof(CharacterData), "triggers").SetValue(enemy, triggers);
            }
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            AccessTools.Field(typeof(CardData), "cost").SetValue(steward, 0);
            AccessTools.Field(typeof(CharacterData), "attackDamage").SetValue(steward.GetSpawnCharacterData(), 80);
            AccessTools.Field(typeof(CharacterData), "health").SetValue(steward.GetSpawnCharacterData(), 5000);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            var ids = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                var card = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
                var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers());
                ids.Add(FullBattleTrace.Active!.CardId(card));
            }
            CardIds = ids.ToArray(); Prepared = true;
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("SPAWN-STATUS-SCALING-PREPARED unchanged original endless mutator; authored enemy rage/trigger and three durable defender controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, card => source.Spawn.Train.Context!.Statistics!.Value(card, "TimesPlayed", "ThisBattle") == 0);
            return index < 0 ? null : BattleActionModel.EnumerateSupportedPlays(source, CardIds[index]).FirstOrDefault(play => play.RoomIndex == index);
        }
    }
}
#endif
