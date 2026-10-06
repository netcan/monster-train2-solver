using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class EnemySpawningProbe
    {
        private readonly ManualLogSource log;
        private readonly FullBattleTrace trace;
        private readonly TrainCombatProbe train;
        private static EnemySpawningProbe? active;
        private readonly List<Record> records = new List<Record>();
        internal IReadOnlyList<Record> Records => records;
        internal int Mismatches => records.Count(record => record.Difference != null);
        internal int Unsupported => records.Count(record => !record.Predicted.Supported);
        internal EnemySpawningProbe(ManualLogSource log, FullBattleTrace trace, TrainCombatProbe train)
        { this.log = log; this.trace = trace; this.train = train; active = this; }

        internal EnemySpawnState Capture()
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            SaveManager save = managers.GetSaveManager();
            HeroManager heroes = managers.GetHeroManager()!;
            TrainCombatState state = train.Capture();
            var interactions = new List<string>();
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(heroes);
            if (pattern.GetBossType() != SpawnPatternData.BossType.None) interactions.Add("Outer or queued final boss spawning");
            if (save.GetCollectedRelics().Count > 0 || save.GetMutators().Count > 0) interactions.Add("Spawn relics/mutators");
            if (save.GetCovenantsForSpawnPattern().Count > 0) interactions.Add("Spawn covenant effects");
            var cache = (Dictionary<int, SpawnGroupData>)AccessTools.Field(typeof(SpawnPatternData),
                "groupIndexToGroupData").GetValue(pattern);
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            var selectedGroups = new List<int>();
            EnemyWave[] definitions = waves.Select((wave, index) =>
            {
                SpawnGroupData[] groups = ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData),
                    "possibleGroups").GetValue(wave)).Cast<SpawnGroupData>().ToArray();
                selectedGroups.Add(cache.TryGetValue(index, out SpawnGroupData selected)
                    ? Array.IndexOf(groups, selected) : -1);
                return new EnemyWave(groups.Select(group => new EnemyGroup(group.GetCharacters(
                    save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss()).Select(Definition).ToArray())).ToArray());
            }).ToArray();
            var specials = (Dictionary<SpawnPatternData.SpecialCharacterType, CharacterData[]>)AccessTools.Field(
                typeof(SpawnPatternData), "specialCharacters").GetValue(pattern);
            var total = (Dictionary<SpawnPatternData.SpecialCharacterType, int>)AccessTools.Field(
                typeof(SpawnPatternData), "numSpecialCharactersToSpawn").GetValue(pattern);
            var spawned = (Dictionary<SpawnPatternData.SpecialCharacterType, int>)AccessTools.Field(
                typeof(SpawnPatternData), "numSpecialCharactersSpawned").GetValue(pattern);
            var treasure = SpawnPatternData.SpecialCharacterType.Treasure;
            bool easy = save.GetCurrentDistance() < save.GetBalanceData().GetDistanceToBeginDifficultCollectorBehavior();
            uint[] rng = RngCalibration.Words(RandomManager.GetState(RngId.Spawning));
            return new EnemySpawnState(state, definitions, selectedGroups,
                (int)AccessTools.Field(typeof(HeroManager), "spawnPatternPhase").GetValue(heroes),
                pattern.GetIsLoopingScenario(), new UnityRng(rng[0], rng[1], rng[2], rng[3]), trace.NextUnitId,
                (specials.TryGetValue(treasure, out CharacterData[] candidates) ? candidates : Array.Empty<CharacterData>())
                    .Select(Definition).ToArray(), total[treasure] - spawned[treasure],
                (bool)AccessTools.Field(typeof(HeroManager), "treasureAndTraitorCharactersEnabled").GetValue(heroes),
                easy ? 1 : 2, easy ? 1 : 0, managers.GetCombatManager()!.GetTurnCount(), interactions);
        }

        internal static EnemyDefinition Definition(CharacterData data)
        {
            var interactions = new List<string>();
            if (data.GetRoomModifiersData().Count > 0) interactions.Add("Spawned room modifiers");
            if (data.IsOuterTrainBoss() || data.IsCompanionBoss()) interactions.Add("Spawned boss companions/actions");
            CombatStatus[] statuses = data.GetStartingStatusEffects().Select(status =>
            {
                StatusEffectData rule = StatusEffectManager.Instance.GetStatusEffectDataById(status.statusId)!;
                if (rule.GetRemoveWhenTriggeredAfterCardPlayed() || rule.GetRemoveAtEndOfTurnIfTriggered())
                    interactions.Add("Spawned delayed status removal");
                Type? type = typeof(CardState).Assembly.GetType(rule.GetStatusEffectStateName());
                if (type?.GetProperty(nameof(StatusEffectState.PreventRemovalDuringRelentlessPhase))?.DeclaringType != typeof(StatusEffectState))
                    interactions.Add("Spawned status has custom relentless removal");
                return new CombatStatus(status.statusId, status.count, rule.GetParamInt(), rule.GetRemoveWhenTriggered(),
                    rule.GetRemoveStackAtEndOfTurn(), rule.GetRemoveAtEndOfTurn(), rule.GetRemoveAtEndOfTurnAfterPostCombat(),
                    false, rule.GetSkipTriggerDuringDeployment(), rule.GetRemoveDuringDeployment(),
                    BattleActionProbe.TriggeredVfx(rule, -1f), BattleActionProbe.TriggeredVfx(rule, 1f));
            }).ToArray();
            CombatTrigger[] triggers = data.GetTriggers().Select(trigger =>
            {
                if (trigger.GetTriggerAtThreshold() != 0 || trigger.GetOnlyTriggerIfEquipped() || trigger.GetRemoveOnRelentlessChange() ||
                    trigger.GetRequiredStatusEffects().Count > 0 || trigger.GetRequiredStatusEffectsForDyingCharacter().Count > 0)
                    interactions.Add("Spawned conditional triggers");
                CombatEffect[] effects = trigger.GetEffects().Select(effect =>
                {
                    if (effect.GetUseIntRange()) interactions.Add("Spawned random effect initialization");
                    if (effect.GetEffectStateName() == "CardEffectRewardGold" &&
                        AllGameManagers.Instance!.GetSaveManager().GetAdjustedGoldAmount(effect.GetParamInt(), isReward: true) != GoldRewardModel.Adjust(effect.GetParamInt()))
                        interactions.Add("Spawned modified gold reward rules");
                    if (effect.GetEffectStateName() == "CardEffectDespawnCharacter" && effect.GetParamInt() > 1)
                        interactions.Add("Native preview mutation of a delayed despawn counter");
                    if (effect.GetEffectStateName() != "CardEffectAddBattleCard" && (effect.GetCopyModifiersFromSource() || effect.GetFilterBasedOnMainSubClass() ||
                        effect.GetParamCardUpgradeData() != null)) interactions.Add("Spawned effect modifiers");
                    var pool = new List<CardData>();
                    CardEffectState.GetFilteredCardListFromPool(effect.GetParamCardPool(), effect.GetParamCardFilter(),
                        AllGameManagers.Instance!.GetRelicManager(), ref pool);
                    return new CombatEffect(effect.GetEffectStateName(), effect.GetParamInt(),
                        effect.GetEffectStateName() == "CardEffectDespawnCharacter" ? Math.Max(1, effect.GetParamInt()) : 0,
                        ((CardPile)effect.GetParamInt()).ToString(), effect.GetAdditionalParamInt(),
                        pool.Select(card => card.GetID()).ToArray(), effect.GetParamBool2(),
                        effect.GetEffectStateName() == "CardEffectAddBattleCard" ? CardGenerationProbe.Definition(effect) : null);
                }).ToArray();
                return new CombatTrigger(trigger.GetTrigger().ToString(), trigger.GetTriggerOnce(), false,
                    trigger.GetHideVisualAndIgnoreSilence(), 1, effects,
                    AllGameManagers.Instance!.GetSaveManager().GetBalanceData().GetDisallowedDeploymentPhaseCharacterTriggers().Contains(trigger.GetTrigger()));
            }).ToArray();
            return new EnemyDefinition(new CombatUnit(0, data.GetAssetKey(), CombatTeam.Enemy, data.GetAttackDamage(),
                data.GetHealth(), data.GetHealth(), data.GetCanAttack(), false, data.IsMiniboss(), statuses, triggers, size: data.GetSize(),
                statusImmunities: data.GetStatusEffectImmunities(), subtypes: data.GetSubtypes().Select(subtype => subtype.Key).ToArray(),
                modifiers: UnitModifierProbe.Definition(data), isBoss: data.IsMiniboss() || data.IsOuterTrainBoss()),
                data.GetAscendsTrainAutomatically(), data.GetLoopsBetweenTrainFloors(), interactions);
        }

        private IEnumerator Wrap(IEnumerator native, bool includeTreasure)
        {
            Record? record = null;
            try
            {
                if (trace.NativeWon == null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                {
                    EnemySpawnState before = Capture();
                    record = new Record { Index = records.Count, IncludeTreasure = includeTreasure, Before = before,
                        Predicted = EnemySpawningModel.Spawn(before, includeTreasure) };
                    records.Add(record);
                }
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null) Complete(record);
            }
        }
        private void Complete(Record record)
        {
            if (record.Actual != null) return;
            try
            {
                record.Actual = Capture();
                if (record.Predicted.Supported)
                {
                    record.Difference = JToken.DeepEquals(Comparable(record.Predicted.State!), Comparable(record.Actual))
                        ? null : "Spawned units, wave phase/cache, resources or RNG differ";
                    log.LogInfo("SPAWN-MODEL-" + (record.Difference == null ? "MATCH" : "MISMATCH") + " index=" + record.Index);
                }
                else log.LogInfo("SPAWN-MODEL-UNSUPPORTED " + record.Predicted.UnsupportedReason);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
        }
        internal void CompletePending()
        { foreach (Record record in records.Where(record => record.Actual == null).ToArray()) Complete(record); }
        private static JToken Comparable(EnemySpawnState state) => JToken.FromObject(new
        {
            Rooms = state.Train.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray(),
            Movement = state.Train.Movement.OrderBy(rule => rule.UnitId).ToArray(), state.Train.Context,
            state.Phase, state.SelectedGroups, state.Rng, state.NextUnitId, state.TreasuresRemaining
        });
        internal sealed class Record
        {
            public int Index { get; set; }
            public bool IncludeTreasure { get; set; }
            public EnemySpawnState Before { get; set; } = null!;
            public EnemySpawnResult Predicted { get; set; } = null!;
            public EnemySpawnState? Actual { get; set; }
            public string? Difference { get; set; }
        }
        [HarmonyPatch(typeof(HeroManager), "DoSpawning")]
        private static class WavePatch
        {
            private static void Postfix(ref IEnumerator __result)
            { if (active != null) __result = active.Wrap(__result, false); }
        }
        [HarmonyPatch(typeof(HeroManager), "DoSpawningCoroutine")]
        private static class WaveAndTreasurePatch
        {
            private static void Postfix(ref IEnumerator __result)
            { if (active != null) __result = active.Wrap(__result, true); }
        }
        [HarmonyPatch(typeof(HeroManager), "CreateHeroState")]
        private static class IdentityPatch
        {
            private static void Prefix(ref Action<CharacterState, CharacterData> afterCharacterCreated)
            {
                if (active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                Action<CharacterState, CharacterData> callback = afterCharacterCreated;
                afterCharacterCreated = (unit, data) => { active.trace.UnitId(unit); callback?.Invoke(unit, data); };
            }
        }
    }
}
