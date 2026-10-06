using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Read definitions and candidate pools, never select a future group or consume RNG.
    internal static class RuleCatalogProbe
    {
        internal static void Capture(AllGameManagers managers)
        {
            JObject before = RngStates();
            SaveManager save = managers.GetSaveManager();
            HeroManager heroes = managers.GetHeroManager()!;
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(heroes);
            var waves = new List<SpawnGroupPoolData>();
            pattern.GetUnlockedWaves(save, waves);
            var characters = new Dictionary<string, CharacterData>(StringComparer.Ordinal);
            var waveDefinitions = waves.Select(wave =>
            {
                var groups = ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                    .GetValue(wave)).Cast<SpawnGroupData>().ToArray();
                return groups.Select(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save,
                    save.GetGeneratedBoss()).Select(unit => Add(characters, unit)).ToArray()).ToArray();
            }).ToArray();
            var specials = (Dictionary<SpawnPatternData.SpecialCharacterType, CharacterData[]>)
                AccessTools.Field(typeof(SpawnPatternData), "specialCharacters").GetValue(pattern);
            var specialDefinitions = specials.ToDictionary(pair => pair.Key.ToString(),
                pair => (pair.Value ?? Array.Empty<CharacterData>()).Select(unit => Add(characters, unit)).ToArray());
            RoomManager rooms = managers.GetRoomManager()!;
            for (int index = 0; index < rooms.GetNumRooms(); index++)
            {
                var units = new List<CharacterState>();
                rooms.GetRoom(index).AddCharactersToList(units, Team.Type.Heroes | Team.Type.Monsters);
                foreach (CharacterState unit in units) Add(characters, unit.GetSourceCharacterData());
            }
            var cardDefinitions = new Dictionary<string, CardData>(StringComparer.Ordinal);
            CardManager cardManager = managers.GetCardManager()!;
            foreach (CardState card in cardManager.GetHand().Concat(cardManager.GetDrawPile()).Concat(cardManager.GetDiscardPile()))
                cardDefinitions[card.GetCardDataID()] = save.GetAllGameData().FindCardData(card.GetCardDataID())
                    ?? throw new InvalidOperationException("Card definition not found: " + card.GetCardDataID());
            foreach (CharacterData unit in characters.Values.ToArray())
                foreach (CardEffectData effect in unit.GetTriggers().SelectMany(trigger => trigger.GetEffects()))
                    if (effect.GetParamCardPool() is CardPool pool)
                        for (int index = 0; index < pool.GetNumCards(); index++)
                        { CardData card = pool.GetCardAtIndex(index); cardDefinitions[card.GetID()] = card; }
            foreach (CardData card in cardDefinitions.Values)
                if (card.GetSpawnCharacterData() is CharacterData unit) Add(characters, unit);
            var catalog = new
            {
                Schema = 2,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                BossType = pattern.GetBossType().ToString(),
                Looping = pattern.GetIsLoopingScenario(),
                Waves = waveDefinitions,
                Specials = specialDefinitions,
                StatusRules = new[] { "armor", "valor", "pyregel" }.Select(id =>
                {
                    StatusEffectData rule = StatusEffectManager.Instance.GetStatusEffectDataById(id)!;
                    return new { Id = id, Type = rule.GetStatusEffectStateName(), Value = rule.GetParamInt(),
                        Stage = rule.GetPrimaryTriggerStage().ToString(), AdditionalStages = rule.GetAdditionalTriggerStages().Select(stage => stage.ToString()).ToArray(),
                        RemoveWhenTriggered = rule.GetRemoveWhenTriggered(), RemoveStackAtEnd = rule.GetRemoveStackAtEndOfTurn(),
                        RemoveAllAtEnd = rule.GetRemoveAtEndOfTurn(), AfterPostCombat = rule.GetRemoveAtEndOfTurnAfterPostCombat(),
                        SkipDuringDeployment = rule.GetSkipTriggerDuringDeployment(), RemoveDuringDeployment = rule.GetRemoveDuringDeployment() };
                }).ToArray(),
                Cards = cardDefinitions.Values.OrderBy(card => card.GetID(), StringComparer.Ordinal).Select(card => new
                {
                    Id = card.GetID(), AssetKey = card.name, Cost = card.GetCost(), Type = card.GetCardType().ToString(),
                    SpawnCharacter = card.GetSpawnCharacterData()?.GetAssetKey(),
                    Effects = card.GetEffects().Select(Effect).ToArray(),
                    Traits = card.GetTraits().Select(trait => new
                    {
                        Type = trait.GetTraitStateName(), Int = trait.GetParamInt(), Int2 = trait.GetParamInt2(),
                        Int3 = trait.GetParamInt3(), Bool = trait.GetParamBool(), Str = trait.GetParamStr(),
                        Float = trait.GetParamFloat(), Scaling = trait.GetUseScalingParams(),
                        TrackedValue = trait.GetParamTrackedValue().ToString(),
                        EntryDuration = trait.GetParamEntryDuration().ToString()
                    }).ToArray(),
                    Triggers = card.GetCardTriggers().Select(trigger => new
                    {
                        Kind = trigger.GetTrigger().ToString(),
                        Effects = trigger.GetCardEffects().Select(Effect).ToArray(),
                        Buffs = trigger.GetTriggerEffects().Select(buff => new
                        { Type = buff.cardTriggerEffect, Int = buff.paramInt, EffectType = buff.buffEffectType }).ToArray()
                    }).ToArray()
                }).ToArray(),
                Characters = characters.Values.OrderBy(unit => unit.GetAssetKey(), StringComparer.Ordinal)
                    .Select(unit => new
                    {
                        AssetKey = unit.GetAssetKey(), Attack = unit.GetAttackDamage(), Health = unit.GetHealth(),
                        Size = unit.GetSize(), CanAttack = unit.GetCanAttack(),
                        Ascends = unit.GetAscendsTrainAutomatically(), Loops = unit.GetLoopsBetweenTrainFloors(),
                        Statuses = unit.GetStartingStatusEffects(),
                        Triggers = unit.GetTriggers().Select(trigger => new
                        {
                            Kind = trigger.GetTrigger().ToString(), Once = trigger.GetTriggerOnce(),
                            Threshold = trigger.GetTriggerAtThreshold(),
                            IgnoreSilence = trigger.GetHideVisualAndIgnoreSilence(),
                            OnlyIfEquipped = trigger.GetOnlyTriggerIfEquipped(),
                            RequiredStatuses = trigger.GetRequiredStatusEffects(),
                            RequiredDyingStatuses = trigger.GetRequiredStatusEffectsForDyingCharacter(),
                            Effects = trigger.GetEffects().Select(Effect).ToArray()
                        }).ToArray()
                    }).ToArray()
            };
            string json = JsonConvert.SerializeObject(catalog, Formatting.Indented);
            if (!JToken.DeepEquals(before, RngStates()))
                throw new InvalidOperationException("Rule catalog capture changed gameplay RNG.");
            File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "rule-catalog.json"), json);
        }

        private static string Add(Dictionary<string, CharacterData> characters, CharacterData unit)
        { characters[unit.GetAssetKey()] = unit; return unit.GetAssetKey(); }

        internal static object Effect(CardEffectData effect)
        {
            var candidates = new List<object>();
            CardPool? pool = effect.GetParamCardPool();
            if (pool != null)
                for (int index = 0; index < pool.GetNumCards(); index++)
                {
                    CardData card = pool.GetCardAtIndex(index);
                    candidates.Add(new { Id = card.GetID(), AssetKey = card.name,
                        Triggers = card.GetCardTriggers().Select(trigger => new
                        {
                            Kind = trigger.GetTrigger().ToString(),
                            Effects = trigger.GetCardEffects().Select(nested => new
                            { Type = nested.GetEffectStateName(), Target = nested.GetTargetMode().ToString(), Value = nested.GetParamInt() }).ToArray()
                        }).ToArray() });
                }
            return new
            {
                Type = effect.GetEffectStateName(), Target = effect.GetTargetMode().ToString(),
                Team = effect.GetTargetTeamType().ToString(), HealthFilter = effect.GetTargetModeHealthFilter().ToString(),
                StatusFilter = effect.GetTargetModeStatusEffectsFilter(),
                ExcludedStatusFilter = effect.GetTargetModeStatusEffectsExcludedFilter(),
                Int = effect.GetParamInt(), AdditionalInt = effect.GetAdditionalParamInt(),
                AdditionalInt1 = effect.GetAdditionalParamInt1(), UseRange = effect.GetUseIntRange(),
                Min = effect.GetParamMinInt(), Max = effect.GetParamMaxInt(), Multiplier = effect.GetParamMultiplier(),
                Str = effect.GetParamStr(), Bool = effect.GetParamBool(), Bool2 = effect.GetParamBool2(),
                Bool3 = effect.GetParamBool3(), Float = effect.GetParamFloat(),
                Statuses = effect.GetParamStatusEffectStackData(), Card = effect.GetParamCardData()?.GetID(),
                Character = effect.GetParamCharacterData()?.GetAssetKey(),
                CardPool = candidates, HasCardFilter = effect.GetParamCardFilter() != null,
                HasUpgrade = effect.GetParamCardUpgradeData() != null
            };
        }

        private static JObject RngStates()
        {
            var states = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
                if (id != RngId.NonDeterministic && id != RngId.Chatter)
                    states[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return states;
        }
    }
}
