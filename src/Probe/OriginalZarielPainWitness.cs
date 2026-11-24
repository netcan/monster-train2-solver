#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalZarielPainWitness
    {
        private const string ChampionId = "d10bb18c-49e9-4895-a1c5-e4ecebbf61a0";
        private const string UpgradeId = "dd5e71e2-6e0d-44fc-a6d9-00fee69cf093";
        private const string AbilityId = "26aafce5-a9c7-4244-84f2-b24b99a92f87";
        private const string Boundary = "OriginalZarielPain";
        private const int ControlDamage = 2;
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ZARIEL_PAIN_WITNESS") == "1";
        private static CardState? champion, ability;
        private static object? authored;
        private static bool turnAdvanced, controlStarted, controlFinished;
        private static int healthBeforeControl, painBeforeControl;
        private static BattleTurnState? beforeDeployment, afterDeployment, beforeControl, before;
        private static PlayCardAction? deployment, action;
        private static DamageSample? sample;
        private static bool observing;

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the pinned original rank-zero Ring 1 battle.");
            if (rooms.GetSelectedRoom() != 0)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                return false;
            }
            var room = rooms.GetRoom(0);
            if (champion == null)
            {
                if (!combat.IsPlacementPhase) throw new InvalidOperationException("Expected the original placement hand.");
                champion = Prepare(managers);
                var point = room.GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("Expected the original first placement point.");
                if (!cards.CanPlayHandCard(champion, 0, point, null, null, out var error))
                    throw new InvalidOperationException("Original Zariel was unavailable: " + error);
                beforeDeployment = trace.CaptureDecision();
                deployment = new PlayCardAction(trace.CardId(champion), 0);
                if (!cards.PlayCard(cards.GetHand().IndexOf(champion), point, ref error))
                    throw new InvalidOperationException("Original Zariel deployment failed: " + error);
                return false;
            }
            if (!turnAdvanced)
            {
                afterDeployment = trace.CaptureDecision();
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                turnAdvanced = true;
                advanceTurn();
                return false;
            }
            if (combat.GetTurnCount() != 1) throw new InvalidOperationException("Expected the first ordinary decision.");
            List<CharacterState> actors = new List<CharacterState>();
            room.AddCharactersToList(actors, Team.Type.Monsters);
            CharacterState zariel = actors.Single(unit => ReferenceEquals(unit.GetSpawnerCard(), champion));
            if (!controlStarted)
            {
                beforeControl = trace.CaptureDecision();
                healthBeforeControl = zariel.GetHP();
                painBeforeControl = zariel.GetStatusEffectStacks("pain");
                if (healthBeforeControl <= ControlDamage || painBeforeControl != 0)
                    throw new InvalidOperationException("Expected a healthy original Zariel before the Bloodshed control.");
                controlStarted = true;
                save.StartCoroutine(TriggerBloodshed(combat, zariel));
                return false;
            }
            if (!controlFinished) return false;
            if (before == null)
            {
                int pain = zariel.GetStatusEffectStacks("pain");
                ability = zariel.GetUnitAbilityCardState();
                if (pain <= painBeforeControl || zariel.GetHP() != healthBeforeControl - ControlDamage ||
                    ability?.GetCardDataID() != AbilityId || !zariel.CanActivateUnitAbility() || !zariel.CanTargetUnitAbilityInRoom(0))
                    throw new InvalidOperationException("The original Bloodshed did not supply pain and the native ability.");
                var enemies = new List<CharacterState>();
                room.AddCharactersToList(enemies, Team.Type.Heroes);
                var target = enemies.FirstOrDefault(item => item.IsAlive && item.GetHP() > pain && item.GetSpawnPoint() != null)
                    ?? throw new InvalidOperationException("Expected an original first-wave target that survives Tier I pain damage.");
                before = trace.CaptureDecision();
                action = new PlayCardAction(trace.CardId(ability), 0, targetUnitId: trace.UnitId(target), activatorUnitId: trace.UnitId(zariel));
                if (!cards.PlayAnyCard(ability, out var error, zariel, target.GetSpawnPoint(), ignoreCost: false,
                    discardCard: false, fromDirectPlay: true, characterThatActivatedAbility: zariel))
                    throw new InvalidOperationException("Original Zariel activation failed: " + error);
                return false;
            }

            BattleTurnState after = trace.CaptureDecision();
            if (sample is not { Completed: true, Coefficient: 1 } || sample.Pain <= 0 ||
                sample.ActorId != action!.ActivatorUnitId || sample.TargetId != action.TargetUnitId ||
                sample.Requested != sample.Pain || sample.TargetHealthAfter != sample.TargetHealthBefore - sample.Requested ||
                before.Energy != after.Energy)
                throw new InvalidOperationException("Original Zariel damage did not settle as the paid Tier I pain-scaled ability.");
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary, GameVersion = Application.version, GameModuleMvid = Module,
                Authored = authored, DeploymentBefore = beforeDeployment, Deployment = deployment, DeploymentAfter = afterDeployment,
                BeforeControl = beforeControl, ControlDamage, HealthBeforeControl = healthBeforeControl, PainBeforeControl = painBeforeControl,
                Before = before, Action = action, After = after, Damage = sample
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "zariel-pain-tier1.mt2f"));
            document.Write(stream);
            log.LogInfo($"ORIGINAL-ZARIEL-PAIN OBSERVED pain={sample.Pain} damage={sample.Requested} target={sample.TargetId}");
            return true;
        }

        private static CardState Prepare(AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            var all = managers.GetAllGameData();
            CardData definition = all.FindCardData(ChampionId) ?? throw new InvalidOperationException("Original PunishedChamp2 was missing.");
            CardUpgradeData rank = all.FindCardUpgradeData(UpgradeId) ?? throw new InvalidOperationException("Original Zariel Tier I upgrade was missing.");
            CardData skill = rank.GetUnitAbilityUpgrade();
            var effects = skill.GetEffects();
            if (definition.name != "PunishedChamp2" || definition.GetSpawnCharacterData()?.IsChampion() != true ||
                rank.name != "Punished2Path3a" || skill.GetID() != AbilityId || skill.name != "ZarielPainAbility" ||
                !skill.IsUnitAbility() || skill.GetCost() != 0 || effects is not [var damage] ||
                damage.GetEffectStateName() != nameof(CardEffectDamagePerStatusEffect) ||
                damage.GetTargetMode() != TargetMode.DropTargetCharacter || damage.GetTargetTeamType() != Team.Type.Heroes ||
                damage.GetParamInt() != 1 || damage.GetUseIntRange() ||
                damage.GetParamStatusEffects() is not [{ statusId: "pain", count: 1 }] ||
                rank.GetCharacterTriggerUpgrades() is not [var trigger] || trigger.GetTrigger() != CharacterTriggerData.Trigger.OnBloodshed ||
                !trigger.GetEffects().Any(effect => effect.GetEffectStateName() == FriendlyDamageStatusModel.NativeEffect))
                throw new InvalidOperationException("The original Zariel Tier I payload changed.");
            CardUpgradeData extra = damage.GetParamCardUpgradeData();
            var extraState = new CardUpgradeState();
            extraState.Setup(extra ?? throw new InvalidOperationException("The original damage effect lost its auxiliary upgrade parameter."));
            var state = new CardUpgradeState();
            state.Setup(rank);
            var card = cards.GetHand().FirstOrDefault(item => item.GetCardDataID() == ChampionId) ??
                cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false)
                ?? throw new InvalidOperationException("Original Zariel could not enter the placement hand.");
            card.ApplyPermanentUpgrade(state, save);
            authored = new
            {
                ChampionId, UpgradeId, AbilityId, Champion = definition.name, Upgrade = rank.name, Ability = skill.name,
                Cost = skill.GetCost(), CooldownAtSpawn = skill.GetCooldownAtSpawn(), CooldownAfterUse = skill.GetCooldownAfterActivated(),
                UpgradePayload = CardUpgradeLifecycleProbe.Definition(state), AuxiliaryDamageUpgrade = CardUpgradeLifecycleProbe.Definition(extraState),
                Damage = RuleCatalogProbe.Effect(damage), Bloodshed = trigger.GetEffects().Select(RuleCatalogProbe.Effect).ToArray()
            };
            return card;
        }

        private static IEnumerator TriggerBloodshed(CombatManager combat, CharacterState zariel)
        {
            yield return combat.ApplyDamageToTarget(ControlDamage, zariel, new CombatManager.ApplyDamageToTargetParameters());
            controlFinished = true;
        }

        private sealed class DamageSample
        {
            public int ActorId { get; set; }
            public int TargetId { get; set; }
            public int Pain { get; set; }
            public int Coefficient { get; set; }
            public int Requested { get; set; }
            public int TargetHealthBefore { get; set; }
            public int TargetHealthAfter { get; set; }
            public bool Completed { get; set; }
        }

        [HarmonyPatch(typeof(CardEffectDamagePerStatusEffect), nameof(CardEffectDamagePerStatusEffect.ApplyEffect))]
        private static class ScalePatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && ability != null && ReferenceEquals(cardEffectParams.playedCard, ability) &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState, cardEffectParams);
            }

            private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
            {
                var trace = FullBattleTrace.Active!;
                var actor = parameters.characterThatActivatedAbility
                    ?? throw new InvalidOperationException("Native Zariel scaling lost its ability activator.");
                var target = parameters.targets.Single();
                sample = new DamageSample { ActorId = trace.UnitId(actor), TargetId = trace.UnitId(target),
                    Pain = actor.GetStatusEffectStacks("pain"), Coefficient = effect.GetParamInt(),
                    TargetHealthBefore = target.GetHP() };
                observing = true;
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    sample.TargetHealthAfter = target.GetHP();
                    sample.Completed = true;
                }
                finally { observing = false; (native as IDisposable)?.Dispose(); }
            }
        }

        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ApplyDamageToTarget))]
        private static class RequestPatch
        {
            private static void Postfix(int damage, CharacterState target, CombatManager.ApplyDamageToTargetParameters parameters)
            {
                if (observing && sample != null && ReferenceEquals(parameters.playedCard, ability) &&
                    FullBattleTrace.Active!.UnitId(target) == sample.TargetId)
                    sample.Requested = damage;
            }
        }
    }
}
#endif
