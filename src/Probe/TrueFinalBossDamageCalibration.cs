using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrueFinalBossDamageCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_DAMAGE") == "1";
        internal static bool AllTargets => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_DAMAGE_ALL_TARGETS") == "1";
        internal static bool SpellSource => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_SPELL_DAMAGE") == "1";
        internal static IEnumerator Capture(IReadOnlyList<CharacterState> actors)
        {
            AllGameManagers managers = AllGameManagers.Instance;
            FullBattleTrace trace = FullBattleTrace.Active!;
            if (actors.Count != 5 || actors.Any(actor => actor.PreviewMode || actor.TemporaryPreviewsEnabled))
                throw new InvalidOperationException("Paid TFB damage requires the five original live actors.");
            object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
                new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
            var rows = new List<object>();
            CardState? sourceCard = SpellSource ? managers.GetCardManager().GetAllCards(new List<CardState>()).First(card =>
                card.GetCardType() == CardType.Spell && card.GetTraits().Count == 0 &&
                card.GetCardStateModifiers().GetCardUpgrades().Count == 0 &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage")) : null;
            int sourceCardId = sourceCard == null ? 0 : trace.CardId(sourceCard);
            var inputs = new[] { (3, false, 19), (4, false, 4000), (3, false, 9), (4, true, 100),
                (3, true, int.MaxValue), (4, false, 0), (3, false, 3500), (4, true, 0), (3, true, 100),
                (4, false, 1), (3, false, 4000), (4, true, int.MaxValue) };
            if (AllTargets) inputs = inputs.Concat(new[] { (0, false, 19), (0, false, 4000), (0, false, 9),
                (0, true, 100), (0, true, int.MaxValue), (1, false, 19), (1, false, 4000), (1, false, 9),
                (1, true, 100), (1, true, int.MaxValue), (2, false, 4000), (2, true, int.MaxValue) }).ToArray();
            if (SpellSource) inputs = new[] { (0, false, 19), (0, false, 19), (0, false, 19), (0, false, 19),
                (0, false, 19), (0, false, 4000), (0, true, int.MaxValue), (1, false, 4000), (1, true, int.MaxValue),
                (2, false, 4000), (4, true, int.MaxValue), (3, false, 4000), (4, false, 9), (4, true, int.MaxValue) };
            foreach (var input in inputs)
            {
                CharacterState target = actors[input.Item1];
                RoomState room = target.GetCurrentRoom();
                RoomCombatState before = trace.Capture(room);
                object beforeRng = Rng();
                if (input.Item2) yield return target.ApplyHeal(input.Item3);
                else yield return managers.GetCombatManager().ApplyDamageToTarget(input.Item3, target,
                    new CombatManager.ApplyDamageToTargetParameters { damageType = Damage.Type.Default, finalEffectInSequence = true, playedCard = sourceCard });
                if (actors.Any(actor => !actor.IsAlive || actor.IsDestroyed))
                    throw new InvalidOperationException("Paid TFB damage unexpectedly entered terminal removal.");
                rows.Add(new { Index = rows.Count, TargetId = trace.UnitId(target), Operation = input.Item2 ? "Heal" : "Damage", Value = input.Item3,
                    Before = before, Actual = trace.Capture(room), RngBefore = beforeRng, RngAfter = Rng(),
                    AllBossesAlive = actors.All(actor => actor.IsAlive),
                    Health = actors.Select(actor => new LinkedHealthValue(trace.UnitId(actor), actor.GetHP(), actor.GetMaxHP())).ToArray() });
            }
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = SpellSource ? "OriginalLiveTrueFinalBossSpellDamageCoroutines" : AllTargets ? "OriginalLiveAllTrueFinalBossDamageAndHealCoroutines" : "OriginalLiveEntropyDamageAndHealCoroutines", OriginalActorsAndStartingStats = true,
                DamageType = "Default", SourceCard = sourceCardId, SourceCardDataId = sourceCard?.GetCardDataID(), Attacker = 0,
                TargetRooms = inputs.Select(input => actors[input.Item1].GetCurrentRoomIndex()).Distinct().OrderBy(room => room).ToArray(),
                ControllerExcluded = true, NativeCompleted = true, Rows = rows
            }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                SpellSource ? "true-final-boss-spell-damage-calibration.mt2f" : AllTargets ? "true-final-boss-all-target-damage-calibration.mt2f" : "true-final-boss-damage-calibration.mt2f"))) document.Write(output);
        }
    }
}
