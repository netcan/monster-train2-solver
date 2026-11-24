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
        private const string Boundary = "OriginalZarielPain";
        private const int StartingPain = 10;
        private const string ShieldGuard = "ShieldGuardT1_BasicNonAttack";
        private static readonly (string Label, string UpgradeId, string UpgradeKey, string AbilityId, string AbilityKey, int HealthBonus, int DamagePerPain)[] Ranks =
        {
            ("I", "dd5e71e2-6e0d-44fc-a6d9-00fee69cf093", "Punished2Path3a", "26aafce5-a9c7-4244-84f2-b24b99a92f87", "ZarielPainAbility", 10, 1),
            ("II", "63cc5b53-8269-49bf-8966-4621fe81cf34", "Punished2Path3b", "1638d032-3ec9-402c-85ce-195aaf56e17d", "ZarielPainAbility2", 30, 2),
            ("III", "1b2f3a61-a56b-4769-8048-fe3bede6c6c8", "Punished2Path3c", "ec73275b-2578-4e2b-babb-ced3b23886c3", "ZarielPainAbility3", 50, 3)
        };
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly int Tier = int.TryParse(Environment.GetEnvironmentVariable("MT2_PROBE_ZARIEL_PAIN_WITNESS"), out int tier) ? tier : 0;
        internal static bool Enabled => Tier >= 1 && Tier <= Ranks.Length;
        internal static string TierLabel => Ranks[Tier - 1].Label;
        private static CardState? champion, ability;
        private static object? authored;
        private static bool turnAdvanced;
        private static BattleTurnState? beforeDeployment, afterDeployment, before;
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
            var rank = Ranks[Tier - 1];
            if (before == null)
            {
                int pain = zariel.GetStatusEffectStacks("pain");
                ability = zariel.GetUnitAbilityCardState();
                log.LogInfo($"ORIGINAL-ZARIEL-PAIN READY tier={Tier} card={trace.CardId(champion)} health={zariel.GetHP()}/{zariel.GetMaxHP()} " +
                    $"pain={pain} ability={ability?.GetCardDataID()}");
                if (pain != StartingPain || zariel.GetHP() != 30 + rank.HealthBonus || zariel.GetMaxHP() != zariel.GetHP() ||
                    ability?.GetCardDataID() != rank.AbilityId || !zariel.CanActivateUnitAbility() || !zariel.CanTargetUnitAbilityInRoom(0))
                    throw new InvalidOperationException($"Expected the original starting health, Pain and available Tier {TierLabel} ability.");
                var enemies = new List<CharacterState>();
                room.AddCharactersToList(enemies, Team.Type.Heroes);
                var target = enemies.SingleOrDefault(item => item.IsAlive && item.GetSpawnPoint() != null &&
                    item.GetSourceCharacterData().name == ShieldGuard)
                    ?? throw new InvalidOperationException("Expected the original first-wave Shield Guard.");
                if (target.GetHP() != 5 || target.GetStatusEffectStacks("armor") != 25)
                    throw new InvalidOperationException("The original first-wave Shield Guard's health or Armor changed.");
                before = trace.CaptureDecision();
                action = new PlayCardAction(trace.CardId(ability), 0, targetUnitId: trace.UnitId(target), activatorUnitId: trace.UnitId(zariel));
                if (!cards.PlayAnyCard(ability, out var error, zariel, target.GetSpawnPoint(), ignoreCost: false,
                    discardCard: false, fromDirectPlay: true, characterThatActivatedAbility: zariel))
                    throw new InvalidOperationException("Original Zariel activation failed: " + error);
                return false;
            }

            BattleTurnState after = trace.CaptureDecision();
            int paidCost = ability!.GetLastPlayedCost();
            bool paid = paidCost == 0 && before.Energy == after.Energy;
            bool observedScaling = sample is { Completed: true, Pain: StartingPain } &&
                sample.Coefficient == rank.DamagePerPain &&
                sample.ActorId == action!.ActivatorUnitId && sample.TargetId == action.TargetUnitId &&
                sample.Requested == sample.Pain * rank.DamagePerPain;
            bool observedDamage = sample is { Completed: true, Requested: int requested } &&
                sample.TargetArmorAfter == Math.Max(0, sample.TargetArmorBefore - requested) &&
                sample.TargetHealthAfter == Math.Max(0, sample.TargetHealthBefore - Math.Max(0, requested - sample.TargetArmorBefore));
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary, GameVersion = Application.version, GameModuleMvid = Module,
                Authored = authored, DeploymentBefore = beforeDeployment, Deployment = deployment, DeploymentAfter = afterDeployment,
                Tier, StartingPain, PaidCost = paidCost, Paid = paid, ObservedScaling = observedScaling, ObservedDamage = observedDamage,
                Before = before, Action = action, After = after, Damage = sample
            });
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, $"zariel-pain-tier{Tier}.mt2f")))
                document.Write(stream);
            log.LogInfo($"ORIGINAL-ZARIEL-PAIN OBSERVED complete={sample?.Completed} actor={sample?.ActorId}/{action?.ActivatorUnitId} " +
                $"target={sample?.TargetId}/{action?.TargetUnitId} pain={sample?.Pain} coefficient={sample?.Coefficient} " +
                $"damage={sample?.Requested} hp={sample?.TargetHealthBefore}->{sample?.TargetHealthAfter} " +
                $"armor={sample?.TargetArmorBefore}->{sample?.TargetArmorAfter} cost={paidCost} energy={before.Energy}->{after.Energy}");
            if (!paid || !observedScaling || !observedDamage)
                throw new InvalidOperationException($"Original Zariel Tier {TierLabel} settlement differed: paid={paid}, scaling={observedScaling}, damage={observedDamage}.");
            return true;
        }

        private static CardState Prepare(AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            var all = managers.GetAllGameData();
            var expected = Ranks[Tier - 1];
            CardData definition = all.FindCardData(ChampionId) ?? throw new InvalidOperationException("Original PunishedChamp2 was missing.");
            CardUpgradeData rank = all.FindCardUpgradeData(expected.UpgradeId)
                ?? throw new InvalidOperationException($"Original Zariel Tier {TierLabel} upgrade was missing.");
            CardData skill = rank.GetUnitAbilityUpgrade();
            var effects = skill.GetEffects();
            if (definition.name != "PunishedChamp2" || definition.GetSpawnCharacterData()?.IsChampion() != true ||
                rank.name != expected.UpgradeKey || skill.GetID() != expected.AbilityId || skill.name != expected.AbilityKey ||
                !skill.IsUnitAbility() || skill.GetCost() != 0 || skill.GetCooldownAtSpawn() != 0 ||
                skill.GetCooldownAfterActivated() != 1 || effects is not [var damage] ||
                damage.GetEffectStateName() != nameof(CardEffectDamagePerStatusEffect) ||
                damage.GetTargetMode() != TargetMode.DropTargetCharacter || damage.GetTargetTeamType() != Team.Type.Heroes ||
                damage.GetParamInt() != expected.DamagePerPain || damage.GetUseIntRange() ||
                damage.GetParamStatusEffects() is not [{ statusId: "pain", count: 1 }] ||
                rank.GetCharacterTriggerUpgrades() is not [var trigger] || trigger.GetTrigger() != CharacterTriggerData.Trigger.OnBloodshed ||
                !trigger.GetEffects().Any(effect => effect.GetEffectStateName() == FriendlyDamageStatusModel.NativeEffect))
                throw new InvalidOperationException($"The original Zariel Tier {TierLabel} payload changed.");
            CardUpgradeData extra = damage.GetParamCardUpgradeData();
            var extraState = new CardUpgradeState();
            extraState.Setup(extra ?? throw new InvalidOperationException("The original damage effect lost its auxiliary upgrade parameter."));
            var state = new CardUpgradeState();
            state.Setup(rank);
            var startingStatuses = state.GetStatusEffectUpgrades();
            if (state.GetAdditionalHP() != expected.HealthBonus || startingStatuses.Count != 1 ||
                startingStatuses[0].statusId != "pain" || startingStatuses[0].count != StartingPain)
                throw new InvalidOperationException($"The original Zariel Tier {TierLabel} health upgrade or starting Pain changed.");
            var card = cards.GetHand().FirstOrDefault(item => item.GetCardDataID() == ChampionId) ??
                cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false)
                ?? throw new InvalidOperationException("Original Zariel could not enter the placement hand.");
            card.ApplyPermanentUpgrade(state, save);
            authored = new
            {
                ChampionId, expected.UpgradeId, expected.AbilityId, Champion = definition.name, Upgrade = rank.name, Ability = skill.name,
                Cost = skill.GetCost(), CooldownAtSpawn = skill.GetCooldownAtSpawn(), CooldownAfterUse = skill.GetCooldownAfterActivated(),
                UpgradePayload = CardUpgradeLifecycleProbe.Definition(state), AuxiliaryDamageUpgrade = CardUpgradeLifecycleProbe.Definition(extraState),
                Damage = RuleCatalogProbe.Effect(damage), Bloodshed = trigger.GetEffects().Select(RuleCatalogProbe.Effect).ToArray()
            };
            return card;
        }

        private sealed class DamageSample
        {
            public int ActorId { get; set; }
            public int TargetId { get; set; }
            public int Pain { get; set; }
            public int Coefficient { get; set; }
            public int? Requested { get; set; }
            public int TargetHealthBefore { get; set; }
            public int TargetHealthAfter { get; set; }
            public int TargetArmorBefore { get; set; }
            public int TargetArmorAfter { get; set; }
            public bool TargetDestroyed { get; set; }
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
                    TargetHealthBefore = target.GetHP(), TargetArmorBefore = target.GetStatusEffectStacks("armor") };
                observing = true;
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    sample.TargetDestroyed = target.IsDestroyed;
                    sample.TargetHealthAfter = sample.TargetDestroyed ? 0 : target.GetHP();
                    sample.TargetArmorAfter = sample.TargetDestroyed ? 0 : target.GetStatusEffectStacks("armor");
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
