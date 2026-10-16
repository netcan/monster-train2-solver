using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFusedPyreshardChecks
{
    private const string Id = "eae7f78f-fe81-4418-a491-d75413dbb55d";
    private const string Subtype = "SubtypesData_Pyre";
    private const int Attack = 5;

    internal static void Native(FixtureValue root, bool required)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out FixtureValue scenario) &&
            scenario.GetString() == "original-fused-pyreshard";
        if (!eligible)
        {
            Require(!required, "Expected the original FusedPyreshard native battle fixture.");
            return;
        }
        Require(root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Mismatches").GetInt32() == 0 &&
            root.GetProperty("Unsupported").GetInt32() == 0 && root.GetProperty("Pending").GetInt32() == 0,
            "FusedPyreshard requires a settled winning battle with zero capture/mismatch/unsupported/pending counters.");

        var first = root.GetProperty("Stages")[0].GetProperty("Before").Deserialize<RoomCombatState>()!;
        CombatRelicState relic = first.Context!.Relics!.Single(owner => owner.DataId == Id);
        Require(relic.AssetKey == "FusedPyreshard" && relic.EffectTypes.SequenceEqual([RelicModel.AddTrigger]) &&
            relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false && relic.TriggerAdditions?.Count == 1,
            "FusedPyreshard owner identity or acquisition timing differs from the unchanged native relic.");
        RelicCharacterTriggers addition = relic.TriggerAdditions!.Single();
        CombatTrigger definition = addition.Triggers.Single();
        CombatEffect effect = definition.Effects.Single();
        CardActionEffect action = effect.Action!;
        Require(addition.EffectIndex == 0 && addition.SourcePlayer && !addition.SourceEnemy &&
            addition.RequiredSubtype == Subtype && !addition.SubtypeIsNone && addition.SubtypeIsPyre &&
            addition.CharacterAssetKeys.Count == 0 && addition.Conditions.Count == 0 && addition.ExternalInteractions.Count == 0 &&
            definition.Kind == "OnKill" && !definition.Once && definition.IgnoreSilence && definition.TriggerAtThreshold == 0 &&
            definition.Origin?.OnlyIfEquipped == false && effect.Type == RelicResourceModel.PermanentPyreAttackAdjustment &&
            effect.Value == Attack && action.Type == "AdjustPyreAttack" && action.Target == "Self" && action.Value == Attack &&
            action.Range == null && action.Statuses.Count == 0,
            "FusedPyreshard's unchanged Pyre-only OnKill +5 definition differs from native data.");

        int installations = 0, pyreKills = 0, pyreKillStages = 0, controlStages = 0, ablations = 0;
        foreach (FixtureValue row in root.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            FixtureValue events = row.GetProperty("Predicted").GetProperty("Events");
            CombatUnit? pyre = before.Units.SingleOrDefault(unit => unit.IsPyre);
            int[] nonPyrePlayerKills = events.EnumerateArray()
                .Where(item => item.GetProperty("Kind").GetString() == "Death")
                .Select(item => item.GetProperty("Actor").GetInt32())
                .Distinct()
                .Where(id => before.Units.Any(unit => unit.Id == id && unit.Team == CombatTeam.Player && !unit.IsPyre))
                .ToArray();
            foreach (int killerId in nonPyrePlayerKills)
                Require(!before.Units.Single(unit => unit.Id == killerId).Triggers.Any(IsFusedTrigger),
                    "FusedPyreshard's Pyre-only trigger was installed on a non-Pyre killer.");
            if (pyre == null)
            {
                if (nonPyrePlayerKills.Length > 0)
                {
                    Require(before.Context?.Resources != null && actual.Context?.Resources != null &&
                        actual.Context.Resources.PyreModifiedBaseAttack == before.Context.Resources.PyreModifiedBaseAttack,
                        "A non-Pyre player kill changed the saved Pyre attack modifier.");
                    controlStages++;
                }
                continue;
            }
            CombatTrigger[] installed = pyre.Triggers.Where(IsFusedTrigger).ToArray();
            Require(installed.Length == 1 && before.Context?.Resources != null && actual.Context?.Resources != null,
                "The native FusedPyreshard callback did not install one trigger on the live Pyre with captured resource state.");
            installations++;

            int beforeAttack = before.Context!.Resources!.PyreModifiedBaseAttack;
            int afterAttack = actual.Context!.Resources!.PyreModifiedBaseAttack;
            int nativeDeaths = EventCount(events, "Death", pyre.Id);
            int expectedAttack = beforeAttack;
            for (int index = 0; index < nativeDeaths; index++) expectedAttack = Math.Max(0, unchecked(expectedAttack + Attack));
            Require(afterAttack == expectedAttack,
                "Saved Pyre attack delta did not match its OnKill count at stage " + row.GetProperty("Index").GetInt32() + ".");

            if (nativeDeaths > 0)
            {
                pyreKills += nativeDeaths; pyreKillStages++;
                CombatUnit afterPyre = actual.Units.Single(unit => unit.Id == pyre.Id);
                int beforeDamage = RelicResourceModel.AttackDamage(pyre, before.Context);
                int afterDamage = RelicResourceModel.AttackDamage(afterPyre, actual.Context);
                Require(afterDamage == unchecked(beforeDamage + nativeDeaths * Attack),
                    "The live Pyre attack query did not refresh synchronously after OnKill.");

                bool exchange = row.GetProperty("Kind").GetString() == "Exchange";
                RoomCombatState ablatedBefore = Remove(before, installed[0]);
                RoomCombatResult ablated = exchange ? RoomCombatModel.Exchange(ablatedBefore) : RoomCombatModel.Resolve(ablatedBefore);
                Require(ablated.Supported && ablated.State!.Context!.Resources!.PyreModifiedBaseAttack == beforeAttack,
                    "Ablating the unchanged FusedPyreshard trigger retained its persistent Pyre attack adjustment.");
                ablations++;
            }
            else if (nonPyrePlayerKills.Length > 0)
            {
                Require(afterAttack == beforeAttack, "A non-Pyre kill incorrectly advanced FusedPyreshard.");
                controlStages++;
            }
        }
        Require(installations > 0 && pyreKills > 0 && pyreKillStages > 0 && controlStages > 0 && ablations > 0,
            "FusedPyreshard is missing its installed-trigger, Pyre-kill, non-Pyre control, or ablation witness.");
        Console.WriteLine($"NATIVE-FUSED-PYRESHARD PASS: unchanged Pyre-only OnKill +{Attack}; {pyreKills} Pyre kills across {pyreKillStages} stages, {controlStages} non-Pyre control stages, {ablations} trigger ablations.");
    }

    private static bool IsFusedTrigger(CombatTrigger trigger) => trigger.Kind == "OnKill" &&
        trigger.Effects.Count == 1 && trigger.Effects[0].Type == RelicResourceModel.PermanentPyreAttackAdjustment &&
        trigger.Effects[0].Value == Attack && trigger.Effects[0].Action?.Type == "AdjustPyreAttack";

    private static int EventCount(FixtureValue events, string kind, int actor) => events.EnumerateArray().Count(item =>
        item.GetProperty("Kind").GetString() == kind && item.GetProperty("Actor").GetInt32() == actor);

    private static RoomCombatState Remove(RoomCombatState state, CombatTrigger trigger) => new(state.RoomIndex, state.Deployment,
        state.Units.Select(unit => unit.WithTriggers(unit.Triggers.Where(item => item.StateId != trigger.StateId).ToArray())).ToArray(),
        state.ExternalInteractions, state.Context, state.Preview, state.ActiveCompanionBossIds);

    private static void Require(bool value, string error)
    { if (!value) throw new InvalidDataException(error); }
}
