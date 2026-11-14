using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TenaciousCalibration
    {
        internal static object Run(AllGameManagers managers)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            SaveManager save = managers.GetSaveManager();
            CharacterState[] actors = trace.KnownUnits.Where(unit => unit.IsAlive && !unit.IsDestroyed && !unit.IsPyreHeart()).ToArray();
            CharacterState player = actors.First(unit => unit.GetTeamType() == Team.Type.Monsters);
            CharacterState enemy = actors.First(unit => unit.GetTeamType() == Team.Type.Heroes);
            CardState card = player.GetSpawnerCard();
            RelicState relic = save.GetCollectedRelics().Single(owner => owner.GetRelicDataID() == TenaciousBattleScenario.OwnerId);
            var primary = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation");
            object originalPlayer = primary.GetValue(player), originalEnemy = primary.GetValue(enemy);
            TrainCombatState original = trace.CaptureTrain(); object rng = HalfHealthLifecycleCalibration.Rng();
            var samples = new List<object>(); var admissions = new List<object>();
            try
            {
                foreach (bool targetEnemy in new[] { false, true })
                foreach (string source in new[] { "none", "card", "relic", "card-relic", "self", "other", "self-card", "other-card-relic" })
                foreach (int amount in new[] { 9, 10, 25 }) Record(targetEnemy, source, 10, amount, 1, 0);
                Record(false, "card", 1, 5, 1, 0);
                Record(false, "card", 0, 5, 1, 0);
                Record(false, "card", -1, 5, 1, 0);
                Record(false, "card", 10, 25, 0, 0);
                Record(false, "card", 10, 25, 5, 0);
                Record(false, "card", 10, -2, 1, 0);
                Record(false, "card", 10, 0, 1, 0);
                Record(false, "card", 10, 10, 1, 7);
                Record(false, "card", 1, int.MaxValue, 1, 0);
                Record(true, "self", 1, 5, 1, 0);
                foreach (bool targetEnemy in new[] { false, true })
                foreach (int stacks in new[] { 0, 1 }) Admission(targetEnemy, stacks);
            }
            finally { primary.SetValue(player, originalPlayer); primary.SetValue(enemy, originalEnemy); }
            string? restoration = HalfHealthLifecycleCalibration.Difference(original, trace.CaptureTrain()) ??
                HalfHealthLifecycleCalibration.Difference(rng, HalfHealthLifecycleCalibration.Rng());
            if (restoration != null) throw new InvalidOperationException("Tenacious stage restoration: " + restoration);
            return new { Completed = true, LiveContextUnchanged = true, RestorationDifference = restoration,
                RngBefore = rng, RngAfter = HalfHealthLifecycleCalibration.Rng(), Samples = samples, Admissions = admissions };

            void Admission(bool targetEnemy, int stacks)
            {
                primary.SetValue(player, originalPlayer); primary.SetValue(enemy, originalEnemy);
                CharacterState target = targetEnemy ? enemy : player;
                object live = primary.GetValue(target);
                object shadow = AccessTools.Method(live.GetType(), "DeepCopyForPreview").Invoke(live, null);
                primary.SetValue(target, shadow);
                AccessTools.Field(shadow.GetType(), "statusEffects").SetValue(shadow,
                    new Dictionary<string, CharacterState.StatusEffectStack> { ["tenacious"] =
                        new CharacterState.StatusEffectStack(managers.GetStatusEffectManager().Create("tenacious", target)!, stacks) });
                var effectData = new CardEffectData("CardEffectAddStatusEffect", null!, target.GetTeamType());
                effectData.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
                AccessTools.Field(typeof(CardEffectData), "paramStatusEffects").SetValue(effectData,
                    new[] { new StatusEffectStackData { statusId = "tenacious", count = 1 } });
                var effect = new CardEffectState(); effect.Setup(effectData, Team.Type.Monsters, save);
                var interactions = new List<string>();
                CardActionEffect action = BattleActionProbe.DescribeTriggerEffect(effectData, interactions)!;
                if (interactions.Count != 0) throw new InvalidOperationException(string.Join(";", interactions));
                TrainCombatState before = trace.CaptureTrain();
                bool native = effect.GetCardEffect().TestEffect(effect, new CardEffectParams {
                    targets = new List<CharacterState> { target }, selectedRoom = target.GetCurrentRoomIndex() }, managers.GetCoreManagers());
                SpellCastCheck model = CardSpellModel.TestPlay(before, target.GetCurrentRoomIndex(), new[] { action }, trace.UnitId(target));
                string? difference = !model.Supported ? model.UnsupportedReason : model.CanPlay != native ? "Nonstackable admission differs." :
                    HalfHealthLifecycleCalibration.Difference(before, trace.CaptureTrain());
                admissions.Add(new { TargetId = trace.UnitId(target), Room = target.GetCurrentRoomIndex(), Stacks = stacks,
                    Before = before, Action = action, NativeTest = native, Difference = difference, Completed = true });
                if (difference != null) throw new InvalidOperationException(difference);
            }

            void Record(bool targetEnemy, string source, int health, int damage, int stacks, int blocked)
            {
                primary.SetValue(player, originalPlayer); primary.SetValue(enemy, originalEnemy);
                CharacterState target = targetEnemy ? enemy : player;
                object live = primary.GetValue(target);
                object shadow = AccessTools.Method(live.GetType(), "DeepCopyForPreview").Invoke(live, null);
                primary.SetValue(target, shadow);
                AccessTools.Property(shadow.GetType(), "Hp").SetValue(shadow, health);
                var status = managers.GetStatusEffectManager().Create("tenacious", target)!;
                AccessTools.Field(shadow.GetType(), "statusEffects").SetValue(shadow,
                    new Dictionary<string, CharacterState.StatusEffectStack> { ["tenacious"] = new CharacterState.StatusEffectStack(status, stacks) });
                CharacterState? attacker = source.StartsWith("self", StringComparison.Ordinal) ? target :
                    source.StartsWith("other", StringComparison.Ordinal) ? (targetEnemy ? player : enemy) : null;
                bool hasCard = source.Contains("card"), hasRelic = source.Contains("relic");
                var input = new StatusEffectState.InputTriggerParams { attacked = target, attacker = attacker, damage = damage,
                    damageSourceCard = hasCard ? card : null, damageSourceRelic = hasRelic ? relic : null };
                var output = new StatusEffectState.OutputTriggerParams { damage = damage, damageBlocked = blocked };
                bool nativeFriendly = (bool)AccessTools.Method(typeof(CharacterState).Assembly.GetType("DamageHelper"), "IsFriendlySourcedDamage")
                    .Invoke(null, new object?[] { target, attacker, input.damageSourceCard, input.damageSourceRelic });
                managers.GetStatusEffectManager().TestTriggerStatusEffects((StatusEffectData.TriggerStage)Enum.Parse(
                    typeof(StatusEffectData.TriggerStage), "OnPreAttackedTenacious"), target, input, output);
                CombatTeam team = targetEnemy ? CombatTeam.Enemy : CombatTeam.Player;
                CombatTeam? attackerTeam = attacker == null ? null : attacker.GetTeamType() == Team.Type.Monsters ? CombatTeam.Player : CombatTeam.Enemy;
                bool friendly = TenaciousModel.IsFriendlySource(team, attackerTeam, hasCard, hasRelic);
                TenaciousDamageResult expected = TenaciousModel.Adjust(health, damage, friendly, stacks > 0);
                string? difference = friendly != nativeFriendly ? "Friendly damage attribution differs." :
                    output.damage != expected.Damage || output.lethalDamagePrevented != expected.Prevented ||
                    output.damageBlocked != unchecked(blocked + expected.Prevented) ? "Native Tenacious stage differs." : null;
                samples.Add(new { TargetTeam = team, AttackerTeam = attackerTeam, Source = source, Health = health,
                    Damage = damage, Stacks = stacks, Blocked = blocked, HasCard = hasCard, HasRelic = hasRelic,
                    NativeFriendly = nativeFriendly, ActualDamage = output.damage, ActualBlocked = output.damageBlocked,
                    ActualPrevented = output.lethalDamagePrevented, Difference = difference, Completed = true });
                if (difference != null) throw new InvalidOperationException("Tenacious " + source + ": " + difference);
            }
        }
    }
}
