using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class BattleTriggerBatch
    {
        public CombatTeam Team { get; }
        public string Kind { get; }
        public int SpawnerCardId { get; }
        public IReadOnlyList<CombatEffect> Effects { get; }
        public BattleTriggerBatch(CombatTeam team, string kind, int spawnerCardId, IReadOnlyList<CombatEffect> effects)
        { Team = team; Kind = kind; SpawnerCardId = spawnerCardId; Effects = Array.AsReadOnly(effects.ToArray()); }
    }

    // Native caches effect objects, grouped by team/kind, in successful-fire order.
    // Only stateless payloads are replayed until shared mutable effect identity is captured.
    public sealed class BattleTriggerHistory
    {
        public IReadOnlyList<string> CapturedKinds { get; }
        public IReadOnlyList<BattleTriggerBatch> Batches { get; }
        public BattleTriggerHistory(IReadOnlyList<string> capturedKinds, IReadOnlyList<BattleTriggerBatch> batches)
        {
            CapturedKinds = Array.AsReadOnly(capturedKinds.ToArray());
            Batches = Array.AsReadOnly(batches.OrderBy(batch => batch.Team).ThenBy(batch => batch.Kind, StringComparer.Ordinal).ToArray());
        }
        internal BattleTriggerHistory Append(BattleTriggerBatch batch) => new BattleTriggerHistory(CapturedKinds, Batches.Concat(new[] { batch }).ToArray());
    }

    public static class BattleTriggerReplayModel
    {
        private const string OriginalAggressiveImp = "5a8399dd-a958-4317-b814-de0e2972ca3d";
        private const string OriginalWelderHelper = "61d9f4f1-80f5-416b-bf22-1836925c8ccd";
        private const string OriginalCheeryDeathcap = "97260e75-4e11-439e-8eda-cd961143fc12";
        private const string OriginalEnthusiasticFan = "adaaaa9d-56a9-4a5f-a347-ee71d4767df5";
        internal static bool Calibrated(CombatEffect effect) =>
            effect.Type == "CardEffectRewardGold" && effect.Value >= 0 ||
            effect.UnitUpgrade is CardActionEffect upgrade && upgrade.Type == "UnitUpgrade" &&
            upgrade.Target == "Self" && !upgrade.ScaleUpgrade && upgrade.Range == null &&
            upgrade.Lifetime == "TemporaryUntilUnitDeath" && upgrade.Upgrade?.ExternalInteractions.Count == 0 &&
            (upgrade.Upgrade.TriggerUpgrades == null || upgrade.Upgrade.TriggerUpgrades.Count == 0) ||
            FixedAggressiveImpRage(effect) || FixedWelderHelperArmor(effect) ||
            FixedCheeryDeathcapDecay(effect) || FixedEnthusiasticFanSap(effect);

        private static bool FixedAggressiveImpRage(CombatEffect effect) =>
            FixedOriginalStatus(effect, "Room", 0, "buff", 5, 2);
        private static bool FixedWelderHelperArmor(CombatEffect effect) =>
            FixedOriginalStatus(effect, "FrontInRoom", 100, "armor", 20, 1);
        private static bool FixedCheeryDeathcapDecay(CombatEffect effect) =>
            FixedOriginalStatus(effect, "Room", 0, "decay", 7, 3, enemy: true);
        private static bool FixedEnthusiasticFanSap(CombatEffect effect) =>
            FixedOriginalStatus(effect, "Room", 0, "debuff", 3, 2, enemy: true);

        private static bool OriginalStatusFromSource(CombatEffect effect, string dataId) =>
            FixedAggressiveImpRage(effect) && dataId == OriginalAggressiveImp ||
            FixedWelderHelperArmor(effect) && dataId == OriginalWelderHelper ||
            FixedCheeryDeathcapDecay(effect) && dataId == OriginalCheeryDeathcap ||
            FixedEnthusiasticFanSap(effect) && dataId == OriginalEnthusiasticFan;

        // Only these fixed stock OnSpawn payloads captured in the 2.3.1 card catalog.
        private static bool FixedOriginalStatus(CombatEffect effect, string target, int chance,
            string status, int stacks, int paramInt, bool enemy = false) =>
            effect.Type == "CardEffectAddStatusEffect" && effect.Value == chance && effect.Counter == 0 &&
            effect.Count == 0 && effect.CardPool.Count == 0 && effect.Generation == null &&
            effect.UnitUpgrade == null && effect.Summon == null && effect.Enchantment == null &&
            effect.DamageStatusMultiplier == null && effect.BoxedPool == null &&
            effect.StatusScaling is { ActorStatus: null, MissingHealth: false, MagicPower: false,
                IgnoreDualism: false, Subtype.Length: 0, HealthChoicePercent: null, HealthChoiceAbove: false } &&
            effect.Action is { Type: "AddStatus", Range: null, Statuses.Count: 1, Upgrade: null, Generation: null,
                OnlyIfNoEnemies: false, UseSelectedRoom: false, CountStatusOnlyOnTargets: false,
                DisallowStatusStackModifiers: false, IgnoreDualism: false } action &&
            action.Target == target && action.Value == chance &&
            action.AllowEnemy == enemy && action.AllowPlayer != enemy &&
            action.Statuses[0] is { Stackable: true } payload && payload.Id == status &&
            payload.Stacks == stacks && payload.ParamInt == paramInt &&
            action.Tests is { ShouldTest: true, FailToCast: false, CancelSubsequent: false,
                StrictTargets: false, MustHaveValidTargets: false } &&
            action.Filters is { Health: "Both", IgnoreBosses: false, Subtype.Length: 0,
                RequiredStatuses.Count: 0, ExcludedStatuses.Count: 0, ExcludedSubtypes.Count: 0 };
        public static TrainSpellResult Apply(TrainCombatState source, int roomIndex, string kind, int selfUnitId,
            BattlePlayRules? definitions = null)
        {
            BattleTriggerHistory? history = source.Context?.TriggerCounts?.ReplayHistory;
            if (history == null || !history.CapturedKinds.Contains(kind)) return Unsupported("Missing captured battle trigger history.");
            if (kind != "OnSpawn") return Unsupported("Uncalibrated replay trigger kind " + kind);
            if (!source.Rooms.Any(room => room.RoomIndex == roomIndex)) return Unsupported("Missing replay room.");
            if (!source.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == selfUnitId))
                return Unsupported("Replay requires the newly spawned self target.");
            TrainCombatState state = source;
            var events = new List<CombatEvent>();
            foreach (BattleTriggerBatch batch in history.Batches.Where(batch => batch.Team == CombatTeam.Player && batch.Kind == kind))
            {
                CardInstanceState? spawner = state.Context!.FindCard(batch.SpawnerCardId);
                if (spawner == null) return Unsupported("Missing retained trigger spawner card.");
                var effects = new List<CardActionEffect>();
                foreach (CombatEffect effect in batch.Effects)
                {
                    if (!Calibrated(effect)) return Unsupported("Uncalibrated or stateful cached trigger effect " + effect.Type);
                    if (effect.Type == "CardEffectRewardGold" && effect.Value >= 0)
                        effects.Add(new CardActionEffect("RewardGold", "Self", effect.Value, false, true,
                            Array.Empty<CombatStatus>(), goldCostRequired: false));
                    else if (effect.UnitUpgrade is CardActionEffect upgrade)
                        effects.Add(upgrade);
                    else if (OriginalStatusFromSource(effect, spawner.DataId))
                        effects.Add(effect.Action!);
                    else return Unsupported("Uncalibrated or stateful cached trigger effect " + effect.Type);
                }
                if (spawner.PlayedRoomUnitIds != null)
                    state = CardSpellModel.WithContext(state, state.Context.WithCard(spawner.WithPlayedRoomUnits(
                        state.Rooms.Single(room => room.RoomIndex == roomIndex).Units.Where(unit => unit.Health > 0 && unit.IsSpawning != true)
                            .Select(unit => unit.Id).ToArray())));
                TrainSpellResult result = CardSpellModel.ApplyCachedEffects(state, roomIndex, effects, batch.SpawnerCardId, selfUnitId, definitions);
                if (!result.Supported) return result;
                state = result.State!; events.AddRange(result.Events);
                if (result.Outcome != RoomOutcome.Exchanged) return result;
            }
            return new TrainSpellResult(state, RoomOutcome.Exchanged, events, null, otherPiles: state.Context!.OtherPiles,
                playRules: definitions);
        }
        private static TrainSpellResult Unsupported(string reason) => new TrainSpellResult(null, RoomOutcome.Unsupported,
            Array.Empty<CombatEvent>(), reason);
    }
}
