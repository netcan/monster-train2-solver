using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalNefariousOverflowChecks
{
    internal const string RelicId = "b0575799-1511-489d-a05d-b5b2e6bf84c2";
    internal const string RelicKey = "NefariousOverflow";

    internal static void Run()
    {
        RelicUnstableRebalanceModifier Rule(int team = 255, bool onlyOne = false, int proc = 0, int explosion = 1,
            int growth = 0) => new(0, team, proc, explosion, growth, onlyOne, "unstable", 1);
        CombatRelicState Relic(RelicUnstableRebalanceModifier rule) => new(RelicId, RelicKey,
            [RelicUnstableRebalanceModel.Effect], unstableRebalances: [rule]);
        CombatUnit Unit(int id, CombatTeam team, int unstable, int health = 5, int undying = 0, bool dead = false)
        {
            var statuses = new List<CombatStatus>();
            if (unstable > 0) statuses.Add(new CombatStatus("unstable", unstable));
            if (undying > 0) statuses.Add(new CombatStatus("undying", undying));
            return new CombatUnit(id, "unstable-probe-" + id, team, 0, health, 5, false, false, false,
                statuses, deathState: dead ? new UnitDeathState(true, false, false) : null);
        }
        CombatContext Context(params CombatRelicState[] relics)
        {
            UnityRng rng = UnityRng.Seed(3107);
            return new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 0, 10, relics: relics);
        }
        void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }

        CombatUnit carrier = Unit(1, CombatTeam.Player, 5);
        var relic = Relic(Rule());
        Require(RelicModel.Validate([relic]) == null &&
            RelicUnstableRebalanceModel.GetAdditionalExplosionDamagePerStack(Context(relic), carrier, [carrier]) == 1,
            "NefariousOverflow did not add its native per-stack explosion damage.");
        Require(RelicUnstableRebalanceModel.GetAdditionalExplosionDamagePerStack(Context(), carrier, [carrier]) == 0,
            "The baseline unstable explosion changed without its relic.");
        Require(RelicUnstableRebalanceModel.GetAdditionalExplosionDamagePerStack(Context(Relic(Rule(team: 1))), carrier,
            [carrier]) == 0 && RelicUnstableRebalanceModel.GetAdditionalExplosionDamagePerStack(
                Context(Relic(Rule(team: 0))), carrier, [carrier]) == 1,
            "Unstable explosion team-mask semantics changed.");

        CombatUnit liveAlly = Unit(2, CombatTeam.Player, 1);
        CombatUnit enemy = Unit(3, CombatTeam.Enemy, 1);
        var onlyOne = Relic(Rule(onlyOne: true));
        Require(RelicUnstableRebalanceModel.GetAdditionalExplosionDamagePerStack(Context(onlyOne), carrier,
            [carrier, liveAlly]) == 0 && RelicUnstableRebalanceModel.GetAdditionalExplosionDamagePerStack(
                Context(onlyOne), carrier, [carrier, enemy]) == 1,
            "Only-one-unstable gating included the wrong team.");
        CombatUnit deadAlly = Unit(4, CombatTeam.Player, 1, health: 0, dead: true);
        CombatUnit undyingAlly = Unit(5, CombatTeam.Player, 1, health: 0, undying: 1, dead: true);
        Require(RelicUnstableRebalanceModel.GetAdditionalExplosionDamagePerStack(Context(onlyOne), carrier,
            [carrier, deadAlly]) == 1 && RelicUnstableRebalanceModel.GetAdditionalExplosionDamagePerStack(
                Context(onlyOne), carrier, [carrier, undyingAlly]) == 0,
            "Only-one-unstable gating mishandled dead and Undying allies.");

        Require(relic.WithStatusMagnitudes(Array.Empty<RelicStatusMagnitude>()).UnstableRebalances?.Single().AdditionalExplosionDamage == 1,
            "Copying another relic payload dropped the unstable rebalance definition.");
        Require(RelicModel.Validate([new CombatRelicState(RelicId, RelicKey, [RelicUnstableRebalanceModel.Effect])]) != null &&
            RelicModel.Validate([Relic(Rule(proc: 1))])?.Contains("proc-damage or growth", StringComparison.Ordinal) == true &&
            RelicModel.Validate([Relic(Rule(growth: 5))])?.Contains("proc-damage or growth", StringComparison.Ordinal) == true,
            "Uncaptured or unmodeled unstable hooks were silently accepted.");
        Console.WriteLine("ORIGINAL-NEFARIOUS-OVERFLOW-CHECKS PASS: per-stack explosion, team mask, same-room uniqueness, dead/Undying gates, copied payload and unsupported proc/growth boundaries.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 172 &&
            root.GetProperty("OriginalNefariousOverflowPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == MixScenarioName &&
            root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "The original NefariousOverflow battle is incomplete or has native/model failures.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length >= 2, "The Mix-based native Unstable witness lacks its setup card plays.");
        BattleTurnState firstBefore = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatRelicState relic = firstBefore.Spawn.Train.Context!.Relics!.Single(item => item.DataId == RelicId);
        RelicUnstableRebalanceModifier rule = relic.UnstableRebalances!.Single();
        Require(relic.AssetKey == RelicKey && relic.EffectTypes.SequenceEqual([RelicUnstableRebalanceModel.Effect]) &&
            rule.EffectIndex == 0 && rule.SourceTeam == 255 && rule.AdditionalProcDamage == 0 &&
            rule.AdditionalExplosionDamage == 1 && rule.AdditionalGrowthRate == 0 &&
            !rule.RequireOnlyOneUnitWithUnstable && rule.StatusId == "unstable" && rule.StatusStacks == 1 &&
            RelicModel.Validate([relic]) == null,
            "The runtime owner differs from the unchanged NefariousOverflow payload.");

        UnstableExplosionChecks.Native(root, damagePerStack: 2);
        Console.WriteLine("ORIGINAL-NEFARIOUS-OVERFLOW NATIVE PASS: unchanged both-team payload adds one explosion damage per Unstable stack.");
    }

    private const string MixScenarioName = "original-nefarious-overflow";

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
