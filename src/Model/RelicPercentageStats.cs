using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicPercentageStat
    {
        public int EffectIndex { get; }
        public int Percent { get; }
        public bool Health { get; }
        public string SourceTeam { get; }
        public string SubtypeKey { get; }
        public bool SubtypeIsPyre { get; }
        public string? RequiredMutatorId { get; }
        public bool RequiredMutatorPresent { get; }
        public bool HasConditions { get; }
        public float? Multiplier { get; }
        public RelicPercentageStat(int effectIndex, int percent, bool health, string sourceTeam,
            string subtypeKey, bool subtypeIsPyre = false, string? requiredMutatorId = null,
            bool requiredMutatorPresent = true, bool hasConditions = false, float? multiplier = null)
        { EffectIndex = effectIndex; Percent = percent; Health = health; SourceTeam = sourceTeam;
            SubtypeKey = subtypeKey; SubtypeIsPyre = subtypeIsPyre; RequiredMutatorId = requiredMutatorId;
            RequiredMutatorPresent = requiredMutatorPresent; HasConditions = hasConditions; Multiplier = multiplier; }
    }

    // Keep the native single-precision operations and the two rounding steps.
    // The preview query uses equality; the birth callback uses HasFlag/None.
    internal static class RelicPercentageStatModel
    {
        internal const string Effect = "RelicEffectModifyCharacterAttackOrHealthPercentage";
        internal const string UnhealedEffect = "RelicEffectModifyCharacterMaxHealthPercentage";
        internal const string UnhealedOwnerId = "ca916209-1309-4e3f-985c-f09b87583f93";
        internal const string UnhealedOwnerKey = "Echoes_DoubleFriendlyUnitHealthUnhealed";

        internal static bool IsOriginalUnhealed(CombatRelicState relic) => relic is
        {
            DataId: UnhealedOwnerId, AssetKey: UnhealedOwnerKey, IsCovenant: false,
            DisallowedInPlacementPhase: false, EffectTypes: [UnhealedEffect],
            PercentageStats: [{ EffectIndex: 0, Percent: 100, Health: true, SourceTeam: "Monsters", SubtypeKey: "",
                SubtypeIsPyre: false, RequiredMutatorId: null, RequiredMutatorPresent: true, HasConditions: false, Multiplier: 2f }]
        };
        // Capture the initialized native float: Mono's intermediate precision
        // can differ from the offline .NET runtime for large source integers.
        internal static int Scale(int value, float multiplier) =>
            unchecked((int)Math.Ceiling((float)value * multiplier));
        internal static int BirthAttack(int raw, int additional, float late, float multiplier) =>
            Scale(unchecked((int)Math.Ceiling((float)unchecked(raw + additional) * late)), multiplier);
        internal static int? Query(RelicPercentageStat rule, string team, bool subtypeMatches, int attack, int health)
        {
            if (!rule.RequiredMutatorPresent || team != rule.SourceTeam || !subtypeMatches) return null;
            int original = rule.Health ? health : attack;
            return unchecked(Scale(original, rule.Multiplier!.Value) - original);
        }
        internal static bool BirthApplies(RelicPercentageStat rule, string team, bool subtypeMatches) =>
            rule.RequiredMutatorPresent && subtypeMatches && (rule.SourceTeam == "None" ||
                rule.SourceTeam == team || team == "Both" && rule.SourceTeam is "Monsters" or "Heroes");
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type is Effect or UnhealedEffect).Select(item => item.index).ToArray();
            var rules = relic.PercentageStats;
            if (indices.Length == 0) return rules?.Count > 0 ? "Orphan percentage birth rules." : null;
            if (rules == null || !rules.Select(rule => rule.EffectIndex).OrderBy(index => index).SequenceEqual(indices))
                return "Missing or duplicate percentage birth rules.";
            if (relic.IsCovenant == null || relic.DisallowedInPlacementPhase == null || rules.Any(rule =>
                rule.HasConditions || !rule.Multiplier.HasValue || float.IsNaN(rule.Multiplier.Value) || float.IsInfinity(rule.Multiplier.Value) ||
                rule.SourceTeam is not ("None" or "Monsters" or "Heroes" or "Both") || rule.SubtypeKey == null ||
                rule.RequiredMutatorId == null && !rule.RequiredMutatorPresent))
                return "Incomplete percentage birth filters.";
            return null;
        }

        internal static RoomCombatResult ApplyUnhealedBirth(RoomCombatState source, int unitId, int fromCardId,
            RelicPercentageStat rule)
        {
            CombatUnit? unit = source.Units.FirstOrDefault(actor => actor.Id == unitId);
            if (unit == null) return Fail("Missing unhealed percentage birth unit.");
            if (source.Preview || fromCardId == 0 || unit.Team != CombatTeam.Player || unit.IsPyre ||
                unit.Health <= 0 || unit.Status("cardless") != null) return Match(source);
            int increment = Scale(unit.MaxHealth, rule.Multiplier!.Value) - unit.MaxHealth;
            CombatContext? context = UnitHealthModel.RecordCardBuff(source.Context, fromCardId, increment, out string? error);
            if (error != null) return Fail(error);
            return UnitHealthModel.ApplyNative(source.WithContext(context), unitId, increment, heal: false);
        }
        internal static RoomCombatResult Apply(RoomCombatState source, int unitId, RelicPercentageStat rule)
        {
            CombatUnit? unit = source.Units.FirstOrDefault(actor => actor.Id == unitId);
            if (unit == null) return Fail("Missing percentage birth unit.");
            if (RelicLateAttackModel.Validate(unit) is string lateError) return Fail(lateError);
            string team = unit.Team == CombatTeam.Player ? "Monsters" : "Heroes";
            bool subtype = rule.SubtypeKey.Length == 0 ? unit.Team != CombatTeam.Player || !unit.IsPyre :
                unit.Subtypes.Contains(rule.SubtypeKey) && (!rule.SubtypeIsPyre || unit.Team != CombatTeam.Player || unit.IsPyre);
            if (!BirthApplies(rule, team, subtype)) return Match(source);
            CombatUnit changed;
            if (rule.Health)
            {
                int hp = Math.Min(99999, Scale(unit.MaxHealth, rule.Multiplier!.Value));
                changed = unit.WithHealthAndMaxHealth(hp, hp);
            }
            else
            {
                if (unit.Modifiers == null) return Fail("Percentage birth attack requires captured raw attack.");
                int raw = UnitAttackOverrideModel.Resolve(unit.Statuses, unit.MaxHealth) ?? unit.Modifiers.AttackDamage;
                int additional = RelicStatusAttackModel.AdditionalAttack(unit.Modifiers.StatusAttackModifiers, unit.Team, unit.Statuses);
                changed = unit.WithRawAttack(BirthAttack(raw, additional,
                    RelicLateAttackModel.Multiplier(unit, applyPyreSkip: false), rule.Multiplier!.Value));
            }
            return Match(source.WithUnits(source.Units.Select(actor => actor.Id == unitId ? changed : actor).ToArray()));
        }
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Fail(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}
