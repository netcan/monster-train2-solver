using System;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicPercentageStatProbe
    {
        internal static string TeamName(Team.Type team) => team == Team.Type.None ? "None" :
            team == Team.Type.Monsters ? "Monsters" : team == Team.Type.Heroes ? "Heroes" :
            team == (Team.Type.Monsters | Team.Type.Heroes) ? "Both" : throw new InvalidOperationException("Unknown percentage team mask.");
        internal static RelicPercentageStat Capture(RelicEffectModifyCharacterAttackOrHealthPercentage effect, int index, SaveManager save)
        {
            object Field(string name) => AccessTools.Field(effect.GetType(), name).GetValue(effect);
            var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect);
            var subtype = (SubtypeData?)Field("characterSubtype");
            var required = (MutatorData?)Field("requiredMutator");
            return new RelicPercentageStat(index, data.GetParamInt(), data.GetParamBool(), TeamName((Team.Type)Field("sourceTeam")),
                subtype == null || subtype.IsNone ? "" : subtype.Key, subtype?.IsPyre == true,
                required == null ? null : required.GetID(), required == null || save.GetHasMutator(required), data.GetEffectConditions().Count != 0,
                (float)Field("percentModification"));
        }

        internal static RelicPercentageStat CaptureUnhealed(RelicEffectModifyCharacterMaxHealthPercentage effect, int index)
        {
            var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect);
            SubtypeData? subtype = data.GetParamCharacterSubtype();
            float multiplier = (float)AccessTools.Field(typeof(RelicEffectModifyCharacterMaxHealthPercentage), "percentModification").GetValue(effect);
            return new RelicPercentageStat(index, data.GetParamInt(), true, TeamName(data.GetParamSourceTeam()),
                subtype == null || subtype.IsNone ? "" : subtype.Key, subtype?.IsPyre == true,
                hasConditions: data.GetEffectConditions().Count != 0 ||
                    data.GetParamTrigger() != CharacterTriggerData.Trigger.OnSpawn || effect.CanApplyInPreviewMode,
                multiplier: multiplier);
        }
        // Only a fully represented combat-only roster bypasses the old broad
        // mutator guard. Companion effects and mixed Covenant projections retain
        // their ordinary validation and calibration requirements.
        internal static bool SupportedMutators(AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            return save.GetCovenants().Count == 0 && save.GetMutators().Count > 0 &&
                save.GetMutators().All(owner => OriginalStartRoomAttachmentScenario.SupportsInfernoMutator(owner, save) ||
                    SupportsEmbeddedRooms(owner) ||
                    RelicProbe.SupportsStatusMultiplier(owner) ||
                    BattleSpawnPointProbe.Enabled && RelicProbe.SupportsShuffleUnits(managers, owner) ||
                    BattleSpawnPointProbe.Enabled && SupportsLonelyRooms(owner, save) ||
                    SupportsUnhealed(owner) ||
                    RelicBothSidesUnitsProbe.Matches(owner) ||
                    RelicSkullCollectorsProbe.Matches(save, owner) ||
                    CardBranchMaskProbe.Enabled && SupportsHandOrEmber(owner, save) ||
                    owner.GetRelicDataID() == RelicUnitSpawnCountModel.DataId &&
                    owner.GetEffects() is [RelicEffectExtraSpawnsFirstUnitPerTurn] ||
                    owner.GetRelicDataID() == RelicModel.PreventMonsterDamageOwnerId &&
                    owner.GetEffects() is [RelicEffectPreventMonstersDamagingHeroes] ||
                    RandomizedWaveProbe.MatchesOriginal(owner) ||
                    owner.GetRelicDataID() == RelicModel.DisableRoomPreviewOwnerId &&
                    owner.GetEffects() is [RelicEffectDisableRoomPreview] && !save.GetBattlePreviewEnabled() ||
                    SupportsDeploymentSummonBlock(owner, save) ||
                    CardBranchMaskProbe.Enabled && SupportsBigMonsters(owner, save) ||
                    BattleSpawnPointProbe.Enabled && CardBranchMaskProbe.Enabled && SupportsSpellDamageMove(owner, save) ||
                    SupportsLoseMoneyOnHit(owner, save) ||
                    SupportsSoulSaviorStarterDeck(owner, save) ||
                    owner.GetRelicDataID() == RelicPersistentMonsterHealthModel.OwnerId &&
                    owner.GetEffects() is [RelicEffectPersistMonsterHp persistentHealth] && SupportsPersistentHealth(persistentHealth) ||
                    SupportsPyreAbilityCooldown(owner, save) ||
                    MoreTreasureUnitsProbe.MatchesOriginal(owner, save) ||
                    SupportsSecondFloorEnemyOpening(owner, save) ||
                    CardBranchMaskProbe.Enabled && SupportsRandomTargets(owner, save) ||
                    owner.GetEffects().Count > 0 && owner.GetEffects().All(effect =>
                    (effect is RelicEffectModifyCharacterAttackOrHealthPercentage || effect is RelicEffectMultiplyAttackLate ||
                        effect is RelicEffectApplyExtraPyreDamage || effect is RelicEffectAddUnitUpgradePerPile ||
                        effect is RelicEffectScaleStatusApplicationsOnSpawn || effect is RelicEffectIncreasePyreAttack ||
                        effect is RelicEffectIncreasePyreHealth ||
                        owner.GetRelicDataID() == RelicOpeningCapacityModel.MutatorId && owner.GetEffects().Count == 1 &&
                        effect is RelicEffectModifyRandomRoomCapacity ||
                        owner.GetRelicDataID() == RelicGoldMagicPowerModel.MutatorId && owner.GetEffects().Count == 1 &&
                        effect is RelicEffectModifyMagicPowerPerGold ||
                        RelicTurnEndBumpModel.IsOwner(owner.GetRelicDataID()) && owner.GetEffects().Count == 1 &&
                        effect is RelicEffectBumpOnTurnEnd ||
                        owner.GetRelicDataID() == RelicHoldoverGrantModel.OwnerId && owner.GetEffects().Count == 1 &&
                        effect is RelicEffectAddTraitToHand ||
                        owner.GetRelicDataID() is RelicResourceProbe.AfterDrawMorselsId or RelicResourceProbe.AfterDrawCultLeaderId &&
                        owner.GetEffects().Count == 1 && effect is RelicEffectSpawnMonsterOnTurnStart) &&
                    ((RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect)).GetEffectConditions().Count == 0));
        }

        private static bool SupportsEmbeddedRooms(RelicState owner) =>
            owner.GetRelicDataID() == RelicModel.AlwaysEmbeddedRoomsOwnerId &&
            owner.GetAssetName() == RelicModel.AlwaysEmbeddedRoomsOwnerKey && !owner.DisallowedInPlacementPhase &&
            owner.GetEffects() is [RelicEffectTrainRoomAttachmentsAlwaysEmbedded effect] && effect.GetIsAlwaysEmbedded() &&
            ((RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect)).GetEffectConditions().Count == 0;

        internal static bool SupportsLonelyRooms(RelicState owner, SaveManager save) =>
            owner.GetRelicDataID() == RelicModel.LonelyRoomsOwnerId &&
            owner.GetAssetName() == RelicModel.LonelyRoomsOwnerKey && !owner.DisallowedInPlacementPhase &&
            owner.GetEffects() is [RelicEffectRoomMonsterCapacityModifier effect] &&
            effect.CapacityModifier == RelicModel.LonelyRoomsPlayerSlots &&
            save.GetMonstersNumSpawnPointsOverrideFromRelics() == RelicModel.LonelyRoomsPlayerSlots;

        internal static bool SupportsUnhealed(RelicState owner) =>
            owner.GetRelicDataID() == RelicPercentageStatModel.UnhealedOwnerId &&
            owner.GetAssetName() == RelicPercentageStatModel.UnhealedOwnerKey && !owner.DisallowedInPlacementPhase &&
            owner.GetEffects() is [RelicEffectModifyCharacterMaxHealthPercentage effect] &&
            CaptureUnhealed(effect, 0) is { Percent: 100, SourceTeam: "Monsters", SubtypeKey: "", HasConditions: false, Multiplier: 2f };

        private static bool SupportsSoulSaviorStarterDeck(RelicState owner, SaveManager save) =>
            save.GetRunType() == RunType.RegionRun && owner.GetRelicDataID() == RelicModel.SoulSaviorStarterDeckId &&
            owner.GetAssetName() == RelicModel.SoulSaviorStarterDeckKey && !owner.DisallowedInPlacementPhase &&
            owner.GetEffects() is [RelicEffectPurgeStarterDeck, RelicEffectAddCardSetStartOfRun] &&
            owner.GetSourceRelicData().GetEffects() is [var purge, var add] &&
            purge.GetEffectConditions().Count == 0 && add.GetEffectConditions().Count == 0 && add.GetParamInt() == 1;

        private static bool SupportsSecondFloorEnemyOpening(MutatorState owner, SaveManager save) =>
            owner.GetRelicDataID() == RelicSecondFloorEnemyOpeningModel.OwnerId &&
            typeof(CombatManager).Module.ModuleVersionId == new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") &&
            owner.GetEffects() is [RelicEffectChangeHeroSpawnRoom { HeroesStarterRoomIndex: 1 }] &&
            save.GetAllGameData().FindMutatorData(RelicSecondFloorEnemyOpeningModel.OwnerId) is MutatorData definition &&
            ValidSecondFloorEnemyOpeningDefinition(definition);

        internal static bool ValidSecondFloorEnemyOpeningDefinition(MutatorData definition) =>
            definition.name == RelicSecondFloorEnemyOpeningModel.OwnerKey && !definition.DisallowInDeploymentPhase &&
            definition.GetEffects() is [var effect] && effect.GetEffectClassName() == RelicSecondFloorEnemyOpeningModel.Effect &&
            effect.GetParamInt() == -1 && effect.GetParamInt2() == 1 && effect.GetEffectConditions().Count == 0;

        private static bool SupportsLoseMoneyOnHit(MutatorState owner, SaveManager save) =>
            owner.GetRelicDataID() == RelicGoldLossOnHitModel.OwnerId &&
            typeof(RelicEffectAddTrigger).Module.ModuleVersionId == new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") &&
            owner.GetEffects() is [RelicEffectAddTrigger] &&
            save.GetAllGameData().FindMutatorData(RelicGoldLossOnHitModel.OwnerId) is MutatorData definition &&
            ValidLoseMoneyOnHitDefinition(definition);

        internal static bool ValidLoseMoneyOnHitDefinition(MutatorData definition)
        {
            return definition.name == RelicGoldLossOnHitModel.OwnerKey && !definition.DisallowInDeploymentPhase &&
                definition.GetEffects() is [var effect] && effect.GetEffectClassName() == RelicModel.AddTrigger &&
                effect.GetParamBool() && effect.GetParamSourceTeam() == Team.Type.Monsters && effect.GetParamCharacters().Count == 0 &&
                effect.GetParamCharacterSubtype()?.IsNone == true && effect.GetEffectConditions().Count == 0 &&
                effect.GetTriggers() is [var trigger] && trigger.GetTrigger() == CharacterTriggerData.Trigger.OnHit &&
                !trigger.GetTriggerOnce() && trigger.GetHideVisualAndIgnoreSilence() && !trigger.GetOnlyTriggerIfEquipped() &&
                trigger.GetEffects() is [var gold] && gold.GetEffectStateName() == RelicGoldLossOnHitModel.Effect &&
                gold.GetParamInt() == -1 && !RelicProbe.UnsupportedTriggeredGoldCost(gold);
        }

        internal static bool BigMonstersActive(SaveManager save) =>
            save.GetMutators().Any(owner => SupportsBigMonsters(owner, save));

        private static bool SupportsHandOrEmber(MutatorState owner, SaveManager save)
        {
            string id = owner.GetRelicDataID();
            if (id is not (RelicHandAndEmberModel.HandOwnerId or RelicHandAndEmberModel.EmberOwnerId) ||
                typeof(SaveManager).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d"))
                return false;
            MutatorData? definition = save.GetAllGameData().FindMutatorData(id);
            if (definition == null || definition.DisallowInDeploymentPhase ||
                definition.GetEffects().Any(effect => effect.GetEffectConditions().Count != 0)) return false;

            if (id == RelicHandAndEmberModel.HandOwnerId)
                return definition.name == "HandDoesntDiscard" &&
                    owner.GetEffects() is [RelicEffectChangeStartingHandSize, RelicEffectSuppressHandDiscard] &&
                    definition.GetEffects()[0].GetParamInt() == -2;

            if (definition.name != "EmberAtStartOfTurnOnly" ||
                owner.GetEffects() is not [RelicEffectModifyEnergyStartOfCombat, RelicEffectConserveEnergyStartOfTurn, RelicEffectAddRelicStartOfRun] ||
                definition.GetEffects()[0].GetParamInt() != 20 || definition.GetEffects()[2].GetParamRelic()?.GetID() != RelicHandAndEmberModel.ConserveOwnerId)
                return false;
            return save.GetCollectedRelics().Count(relic => relic.GetRelicDataID() == RelicHandAndEmberModel.ConserveOwnerId &&
                relic.GetEffects() is [RelicEffectConserveEnergyEndOfTurn]) == 1;
        }

        private static bool SupportsSpellDamageMove(RelicState owner, SaveManager save)
        {
            if (owner.GetRelicDataID() != RelicSpellDamageMoveModel.OwnerId ||
                typeof(CardState).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                owner.GetEffects() is not [RelicEffectChanceAddEffectOnDamageCardPlayed]) return false;
            MutatorData? definition = save.GetAllGameData().FindMutatorData(RelicSpellDamageMoveModel.OwnerId);
            return definition != null && ValidSpellDamageMoveDefinition(definition);
        }

        internal static bool ValidSpellDamageMoveDefinition(MutatorData definition)
        {
            if (definition.name != RelicSpellDamageMoveModel.OwnerKey || definition.DisallowInDeploymentPhase ||
                definition.GetEffects() is not [RelicEffectData effect] ||
                effect.GetEffectClassName() != RelicSpellDamageMoveModel.Effect || effect.GetEffectConditions().Count != 0 ||
                effect.GetParamInt() != RelicSpellDamageMoveModel.Chance ||
                effect.GetParamCardEffectData() is not [CardEffectData move]) return false;
            return move.GetEffectStateName() == nameof(CardEffectBump) && move.GetTargetMode() == TargetMode.Self &&
                move.GetTargetTeamType() == (Team.Type.Heroes | Team.Type.Monsters) &&
                move.GetParamInt() == RelicSpellDamageMoveModel.BumpAmount &&
                move.GetParamMinInt() == 0 && move.GetParamMaxInt() == 0;
        }

        private static bool SupportsBigMonsters(RelicState owner, SaveManager save)
        {
            if (owner.GetRelicDataID() != RelicModel.BigMonstersOwnerId ||
                typeof(CardState).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                owner.GetEffects() is not [RelicEffectModifyMonsterCapacity])
                return false;
            MutatorData? definition = save.GetAllGameData().FindMutatorData(RelicModel.BigMonstersOwnerId);
            if (definition == null || definition.name != RelicModel.BigMonstersOwnerKey ||
                definition.DisallowInDeploymentPhase || definition.GetEffects().Count != 1)
                return false;
            RelicEffectData effect = definition.GetEffects()[0];
            return effect.GetParamInt() == 1 && effect.GetParamBool() && effect.GetEffectConditions().Count == 0 &&
                string.IsNullOrEmpty(effect.GetParamString()) && effect.GetParamCharacterSubtype()?.IsNone == true;
        }

        private static bool SupportsDeploymentSummonBlock(RelicState owner, SaveManager save)
        {
            if (owner.GetRelicDataID() != RelicModel.DeploymentSummonBlockOwnerId ||
                owner.GetEffects() is not [RelicEffectBlockRoomSummon])
                return false;
            MutatorData? definition = save.GetAllGameData().FindMutatorData(RelicModel.DeploymentSummonBlockOwnerId);
            if (definition == null || definition.name != RelicModel.DeploymentSummonBlockOwnerKey ||
                definition.DisallowInDeploymentPhase || definition.GetEffects().Count != 1)
                return false;
            RelicEffectData effect = definition.GetEffects()[0];
            return effect.GetParamInt() == RelicModel.DeploymentSummonBlockMask &&
                effect.GetParamBool() && effect.GetEffectConditions().Count == 0;
        }

        private static bool SupportsPersistentHealth(RelicEffectPersistMonsterHp effect)
        {
            var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect);
            return data.GetEffectConditions().Count == 0 && data.GetParamCharacterSubtype()?.IsNone == true;
        }

        private static bool SupportsPyreAbilityCooldown(RelicState owner, SaveManager save)
        {
            if (owner.GetRelicDataID() != RelicPyreAbilityCooldownModel.OwnerId ||
                owner.GetEffects() is not [RelicEffectModifyUnitAbilityCooldown]) return false;
            MutatorData? definition = save.GetAllGameData().FindMutatorData(RelicPyreAbilityCooldownModel.OwnerId);
            if (definition == null || definition.name != RelicPyreAbilityCooldownModel.OwnerKey || definition.DisallowInDeploymentPhase ||
                definition.GetEffects().Count != 1) return false;
            RelicEffectData effect = definition.GetEffects()[0];
            return effect.GetParamInt() == RelicPyreAbilityCooldownModel.Cooldown &&
                effect.GetParamInt2() == (int)RelicEffectModifyUnitAbilityCooldown.UnitType.PyreHeart && effect.GetEffectConditions().Count == 0;
        }

        internal static bool RandomSpellTargetsActive(SaveManager save) => save.GetMutators().Any(owner => SupportsRandomTargets(owner, save));

        private static bool SupportsRandomTargets(RelicState owner, SaveManager save)
        {
            if (owner.GetRelicDataID() != RelicRandomSpellTargetsModel.OwnerId ||
                typeof(CardState).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                owner.GetEffects() is not [RelicEffectModifyTargetsRandom, RelicEffectDisableUndo]) return false;
            MutatorData? definition = save.GetAllGameData().FindMutatorData(RelicRandomSpellTargetsModel.OwnerId);
            return definition != null && definition.name == RelicRandomSpellTargetsModel.OwnerKey && !definition.DisallowInDeploymentPhase &&
                definition.GetEffects().All(effect => effect.GetParamCardFilter()?.name == "OnlySpells" && effect.GetEffectConditions().Count == 0);
        }
    }
}
