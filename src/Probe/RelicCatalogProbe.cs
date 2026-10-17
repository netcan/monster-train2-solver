using System;
using System.Collections;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;
using TypeNameCache = ShinyShoe.TypeNameCache;

namespace MonsterTrain2Poju.Probe
{
    // Definitions only: no relic is acquired, initialized or fired by this capture.
    internal static class RelicCatalogProbe
    {
        internal static void Capture(AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CATALOG") != "1") return;
            JObject before = RngStates();
            int frame = UnityEngine.Time.frameCount;
            AllGameData gameData = managers.GetSaveManager().GetAllGameData();
            string[] collections = { "collectableRelicDatas", "sinsDatas", "covenantDatas", "mutatorDatas",
                "pyreArtifactDatas", "endlessMutatorDatas", "enhancerDatas", "soulDatas" };
            var relics = collections.SelectMany(collection => ((IEnumerable)Field(gameData, collection)).Cast<RelicData>()
                .Select(data => new { Collection = collection, Data = data })).Select(entry => new
            {
                entry.Collection, Id = entry.Data.GetID(), AssetKey = entry.Data.name, DisallowedInPlacementPhase = entry.Data.DisallowInDeploymentPhase,
                Effects = entry.Data.GetEffects().Select(effect => new
                {
                    ClassName = effect.GetEffectClassName(), RuntimeType = TypeNameCache.GetType(effect.GetEffectClassName())?.Name,
                    SourceTeam = effect.GetParamSourceTeam().ToString(), TargetMode = effect.GetTargetMode().ToString(),
                    Int = effect.GetParamInt(), Int2 = effect.GetParamInt2(),
                    Int3 = effect.GetParamInt3(), MinInt = effect.GetParamMinInt(), MaxInt = effect.GetParamMaxInt(),
                    Float = effect.GetParamFloat(), Bool = effect.GetParamBool(), Bool2 = effect.GetParamBool2(),
                    Bool3 = effect.GetParamBool3(), Bool4 = effect.GetParamBool4(), Str = effect.GetParamString(),
                    CardType = effect.GetParamCardType().ToString(),
                    CardFilter = effect.GetParamCardFilter() == null ? null : CardUpgradeMaskProbe.Definition(effect.GetParamCardFilter()),
                    CardFilterSecondary = effect.GetParamCardFilterSecondary() == null ? null : CardUpgradeMaskProbe.Definition(effect.GetParamCardFilterSecondary()),
                    SourceTrait = effect.GetSourceTrait(), TargetTrait = effect.GetTargetTrait(),
                    Trigger = effect.GetParamTrigger().ToString(), ExcludedCardTriggers = effect.GetCardTriggers().Select(trigger => trigger.ToString()).ToArray(),
                    RequiredTraits = effect.GetTraits().Select(trait => trait.GetTraitStateName()).ToArray(),
                    RequiredSubtype = effect.GetParamCharacterSubtype()?.Key,
                    CardEffects = CardEffectDefinitions(effect),
                    CharacterFilters = effect.GetParamCharacters()?.Select(character => character == null ? null : new { Id = character.GetID(), AssetKey = character.GetAssetKey() }).ToArray(),
                    AddedTriggers = effect.GetTriggers()?.Select(trigger => new
                    {
                        Kind = trigger.GetTrigger().ToString(), Once = trigger.GetTriggerOnce(), IgnoreSilence = trigger.GetHideVisualAndIgnoreSilence(),
                        OnlyIfEquipped = trigger.GetOnlyTriggerIfEquipped(),
                        Effects = trigger.GetEffects().Select(action => new
                        {
                            Type = action.GetEffectStateName(), Target = action.GetTargetMode().ToString(), Team = action.GetTargetTeamType().ToString(),
                            Value = action.GetParamInt(), Bool = action.GetParamBool(), Bool2 = action.GetParamBool2(),
                            Statuses = (action.GetParamStatusEffects() ?? Array.Empty<StatusEffectStackData>()).Select(status => new { status.statusId, status.count }).ToArray(),
                            Upgrade = action.GetParamCardUpgradeData()?.name
                        }).ToArray()
                    }).ToArray(),
                    Statuses = (effect.GetParamStatusEffects() ?? Array.Empty<StatusEffectStackData>()).Select(status => new { status.statusId, status.count }).ToArray(),
                    CardUpgrade = Upgrade(effect.GetParamCardUpgradeData()),
                    Conditions = (effect.GetEffectConditions() ?? new System.Collections.Generic.List<RelicEffectCondition>()).Select(condition => new
                    {
                        TrackedValue = Field(condition, "paramTrackedValue").ToString(), CardType = Field(condition, "paramCardType").ToString(),
                        TrackTriggerCount = (bool)Field(condition, "paramTrackTriggerCount"), Duration = Field(condition, "paramEntryDuration").ToString(),
                        Comparator = Convert.ToInt32(Field(condition, "paramComparator")), Int = (int)Field(condition, "paramInt"),
                        AllowMultiple = (bool)Field(condition, "allowMultipleTriggersPerDuration"), Subtype = Field(condition, "paramSubtype")
                    }).ToArray(),
                    PlayerClanCardPool = effect.GetEffectClassName() == "RelicEffectAddPrecachedBattleCardToHand"
                        ? CaptureCardPool(effect.GetParamCardPool()) : null
                }).ToArray()
            }).ToArray();
            JObject after = RngStates();
            if (UnityEngine.Time.frameCount != frame || !JToken.DeepEquals(before, after))
                throw new InvalidOperationException("Relic definition inspection changed native RNG or frame.");
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 8, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(RelicState).Assembly.ManifestModule.ModuleVersionId,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = before, RngAfter = after, Relics = relics
            }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "relic-catalog.mt2f")))
                document.Write(stream);
        }
        private static object? CaptureCardPool(CardPool? pool)
        {
            if (pool == null) return null;
            var cards = pool.GetAllChoices(new System.Collections.Generic.List<CardData>());
            return new
            {
                Name = pool.name,
                Cards = cards.Select((card, index) => new
                {
                    Index = index, Id = card.GetID(), AssetKey = card.name,
                    LinkedClan = card.GetLinkedClassID(), CardType = card.GetCardType().ToString()
                }).ToArray()
            };
        }

        private static object? Upgrade(CardUpgradeData? data) => data == null ? null : new
        {
            Id = data.GetID(), AssetKey = data.name, Unique = data.IsUnique(), ExcludeFromClones = data.GetExcludeFromClones(),
            Damage = data.GetBonusDamage(), Health = data.GetBonusHP(), UnhealedHealth = data.GetUnhealedBonusHP(),
            CostReduction = data.GetCostReduction(), XCostReduction = data.GetXCostReduction(), Heal = data.GetBonusHeal(),
            Size = data.GetBonusSize(), EquipmentLimit = data.GetBonusEquipment(), UpgradeSlots = data.GetBonusUpgradeSlotCount(),
            Statuses = data.GetStatusEffectUpgrades().Select(status => new { status.statusId, status.count, status.fromPermanentUpgrade }).ToArray(),
            AddedTraits = data.GetTraitDataUpgrades().Select(trait => trait.GetTraitStateName()).ToArray(), RemovedTraits = data.GetRemoveTraitUpgrades().ToArray(),
            CharacterTriggers = data.GetCharacterTriggerUpgrades().Select(trigger => trigger.GetTrigger().ToString()).ToArray(),
            CardTriggers = data.GetCardTriggerUpgrades().Select(trigger => trigger.GetTrigger().ToString()).ToArray(),
            RoomModifierCount = data.GetRoomModifierUpgrades().Count,
            ReplacedUpgradeIds = data.GetUpgradesToRemove().Select(upgrade => upgrade.GetID()).ToArray(),
            UnitAbilityId = data.GetUnitAbilityUpgrade()?.GetID(), RoomAbilityId = data.GetRoomAbilityUpgrade()?.GetID(),
            Filters = data.GetFilters().Select(CardUpgradeMaskProbe.Definition).ToArray()
        };
        private static object[] CardEffectDefinitions(RelicEffectData relicEffect) =>
            (relicEffect.GetParamCardEffectData() ?? new System.Collections.Generic.List<CardEffectData>())
                .Select(effect =>
                {
                    CharacterData? character = effect.GetParamCharacterData();
                    CharacterData? additionalCharacter = effect.GetParamAdditionalCharacterData();
                    RoomData? room = effect.GetParamRoomData();
                    CardData? card = effect.GetParamCardData();
                    SubtypeData? subtype = effect.GetParamSubtype();
                    SubtypeData? targetSubtype = effect.GetTargetCharacterSubtype();
                    CardPool? pool = effect.GetParamCardPool();
                    return (object)new
                    {
                        Type = effect.GetEffectStateName(), Target = effect.GetTargetMode().ToString(),
                        Team = effect.GetTargetTeamType().ToString(), Int = effect.GetParamInt(),
                        AdditionalInt = effect.GetAdditionalParamInt(), AdditionalInt1 = effect.GetAdditionalParamInt1(),
                        MinInt = effect.GetParamMinInt(), MaxInt = effect.GetParamMaxInt(),
                        Bool = effect.GetParamBool(), Bool2 = effect.GetParamBool2(), Bool3 = effect.GetParamBool3(),
                        Float = effect.GetParamFloat(), Str = effect.GetParamStr(),
                        Character = character == null ? null : new { Id = character.GetID(), AssetKey = character.GetAssetKey() },
                        AdditionalCharacter = additionalCharacter == null ? null : new { Id = additionalCharacter.GetID(), AssetKey = additionalCharacter.GetAssetKey() },
                        CharacterPool = (effect.GetParamCharacterDataPool() ?? new System.Collections.Generic.List<CharacterData>())
                            .Select(item => new { Id = item.GetID(), AssetKey = item.GetAssetKey() }).ToArray(),
                        Room = room == null ? null : new { Id = room.GetID(), AssetKey = room.GetAssetKey() },
                        Card = card == null ? null : new { Id = card.GetID(), AssetKey = card.GetAssetKey() },
                        CardPool = pool?.name,
                        Subtype = subtype?.Key, TargetSubtype = targetSubtype?.Key,
                        Statuses = (effect.GetParamStatusEffects() ?? System.Array.Empty<StatusEffectStackData>())
                            .Select(status => new { status.statusId, status.count }).ToArray(),
                        Upgrade = Upgrade(effect.GetParamCardUpgradeData())
                    };
                }).ToArray();
        private static object Field(object source, string name) => AccessTools.Field(source.GetType(), name).GetValue(source);
        private static JObject RngStates()
        {
            var result = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
                if (id != RngId.NonDeterministic && id != RngId.Chatter)
                    result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return result;
        }
    }
}
