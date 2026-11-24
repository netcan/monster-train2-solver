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
    internal static class OriginalZarielDevotionWitness
    {
        private const string ChampionId = "d10bb18c-49e9-4895-a1c5-e4ecebbf61a0";
        private const string UpgradeId = "b3b96025-2d96-4183-8521-2bbdb5c7a13c";
        private const string SpellId = "96c9ed37-5e59-4c4f-a914-ae7040d24755";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ZARIEL_DEVOTION_WITNESS") == "1";
        private static CardState? champion, spell;
        private static CharacterState? actor;
        private static object? authored;
        private static BattleTurnState? deploymentBefore, deploymentAfter, before;
        private static PlayCardAction? deployment, action;
        private static readonly List<DamageSample> Requests = new List<DamageSample>();
        private static bool observing, effectCompleted;
        private static int levelAtTrigger, coefficient;
        private static bool includesBase;
        private static int[] targets = Array.Empty<int>();
        private static int? triggerActor, damageSourceCard;

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
                var all = managers.GetAllGameData();
                CardData definition = all.FindCardData(ChampionId);
                CardUpgradeData upgrade = all.FindCardUpgradeData(UpgradeId);
                var trigger = upgrade?.GetCharacterTriggerUpgrades().SingleOrDefault(candidate =>
                    candidate.GetTrigger() == CharacterTriggerData.Trigger.CardSpellPlayed);
                if (definition?.name != "PunishedChamp2" || upgrade?.name != "Punished2Path1a" ||
                    trigger?.GetEffects().ToArray() is not [var effect] ||
                    effect.GetEffectStateName() != nameof(CardEffectDamagePerDevotionLevel) ||
                    effect.GetTargetMode() != TargetMode.FrontInRoom || effect.GetTargetTeamType() != Team.Type.Heroes ||
                    effect.GetParamInt() != 6 || !effect.GetParamBool() || effect.GetUseIntRange() || effect.GetUseStatusEffectStackMultiplier())
                    throw new InvalidOperationException("Expected Zariel's original Tier I Devotion path and spell-play trigger.");
                var rank = new CardUpgradeState();
                rank.Setup(upgrade);
                authored = new { ChampionId, UpgradeId, SpellId, Champion = definition.name, Upgrade = upgrade.name,
                    Trigger = trigger.GetTrigger().ToString(), Effect = RuleCatalogProbe.Effect(effect),
                    UpgradePayload = CardUpgradeLifecycleProbe.Definition(rank) };
                champion = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == ChampionId) ??
                    cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false)
                    ?? throw new InvalidOperationException("Original Zariel could not enter the placement hand.");
                champion.ApplyPermanentUpgrade(rank, save);
                var point = room.GetFirstEmptyMonsterPoint() ?? throw new InvalidOperationException("Expected a placement point.");
                deploymentBefore = trace.CaptureDecision();
                deployment = new PlayCardAction(trace.CardId(champion), 0);
                if (!cards.CanPlayHandCard(champion, 0, point, null, null, out var error) ||
                    !cards.PlayCard(cards.GetHand().IndexOf(champion), point, ref error))
                    throw new InvalidOperationException("Original Zariel deployment failed: " + error);
                return false;
            }
            if (deploymentAfter == null)
            {
                deploymentAfter = trace.CaptureDecision();
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                advanceTurn();
                return false;
            }
            if (combat.GetTurnCount() != 1) throw new InvalidOperationException("Expected the first ordinary decision.");
            if (before == null)
            {
                var units = new List<CharacterState>();
                room.AddCharactersToList(units, Team.Type.Monsters);
                actor = units.Single(unit => ReferenceEquals(unit.GetSpawnerCard(), champion));
                CardData venerate = managers.GetAllGameData().FindCardData(SpellId);
                if (actor.GetHP() != 40 || DevotionProbe.Read(save, "GetDevotionLevel") != 0 ||
                    DevotionProbe.Read(save, "GetDevotionPoints") != 0 ||
                    venerate?.name != "Venerate" || venerate.GetCost() != 3 || venerate.GetEffects() is not [var award] ||
                    award.GetEffectStateName() != "CardEffectGainDevotionPoints" || award.GetParamInt() != 6)
                    throw new InvalidOperationException("Expected the original zero-Devotion opening and Venerate.");
                spell = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == SpellId) ??
                    cards.AddNewCard(venerate, CardPile.HandPile, false, false, animate: false)
                    ?? throw new InvalidOperationException("Original Venerate could not enter the first combat hand.");
                before = trace.CaptureDecision();
                action = new PlayCardAction(trace.CardId(spell), 0);
                if (!cards.CanPlayHandCard(spell, 0, null, null, null, out var error) ||
                    !cards.PlayCard(cards.GetHand().IndexOf(spell), null, ref error))
                    throw new InvalidOperationException("Original Venerate play failed: " + error);
                return false;
            }

            BattleTurnState after = trace.CaptureDecision();
            int paidCost = spell!.GetLastPlayedCost();
            int levelAfter = DevotionProbe.Read(save, "GetDevotionLevel");
            int requested = coefficient * (levelAtTrigger + (includesBase ? 1 : 0));
            int championCardId = trace.CardId(champion);
            bool observed = effectCompleted && levelAtTrigger == 1 && levelAfter == 1 && coefficient == 6 &&
                triggerActor == trace.UnitId(actor!) && damageSourceCard == championCardId && requested > 0 && targets.Length > 0 &&
                Requests.Select(request => request.TargetId).SequenceEqual(targets) &&
                Requests.All(request => request.Completed && request.Amount == requested && request.SourceCardId == championCardId) &&
                paidCost == 3 && before.Energy - after.Energy == paidCost;
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalZarielDevotion", GameVersion = Application.version, GameModuleMvid = Module,
                Authored = authored, DeploymentBefore = deploymentBefore, Deployment = deployment, DeploymentAfter = deploymentAfter,
                Before = before, Action = action, After = after, PaidCost = paidCost, LevelAfter = levelAfter,
                Trigger = new { Actor = triggerActor, SourceCardId = damageSourceCard, Level = levelAtTrigger, Coefficient = coefficient,
                    IncludesBase = includesBase, Targets = targets, Completed = effectCompleted, Requests }
            });
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "zariel-devotion-tier1.mt2f")))
                document.Write(stream);
            log.LogInfo($"ORIGINAL-ZARIEL-DEVOTION OBSERVED actor={triggerActor} sourceCard={damageSourceCard}/{championCardId} " +
                $"level={levelAtTrigger}/{levelAfter} coefficient={coefficient} " +
                $"includesBase={includesBase} targets={targets.Length} requests={Requests.Count} cost={paidCost}");
            if (!observed) throw new InvalidOperationException("The original Venerate did not settle with the expected Devotion damage trigger.");
            return true;
        }

        private sealed class DamageSample
        {
            public int TargetId { get; set; }
            public int? SourceCardId { get; set; }
            public int Amount { get; set; }
            public int HealthBefore { get; set; }
            public int HealthAfter { get; set; }
            public int ArmorBefore { get; set; }
            public int ArmorAfter { get; set; }
            public bool Completed { get; set; }
        }

        [HarmonyPatch(typeof(CardEffectDamagePerDevotionLevel), nameof(CardEffectDamagePerDevotionLevel.ApplyEffect))]
        private static class ScalePatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && before != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState, cardEffectParams);
            }

            private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
            {
                var trace = FullBattleTrace.Active!;
                triggerActor = parameters.selfTarget == null ? null : trace.UnitId(parameters.selfTarget);
                damageSourceCard = parameters.playedCard == null ? null : trace.CardId(parameters.playedCard);
                levelAtTrigger = DevotionProbe.Read(AllGameManagers.Instance!.GetSaveManager(), "GetDevotionLevel");
                coefficient = effect.GetParamInt();
                includesBase = effect.GetParamBool();
                targets = parameters.targets.Select(trace.UnitId).ToArray();
                observing = true;
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    effectCompleted = true;
                }
                finally { observing = false; (native as IDisposable)?.Dispose(); }
            }
        }

        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ApplyDamageToTarget))]
        private static class RequestPatch
        {
            private static void Postfix(int damage, CharacterState target, CombatManager.ApplyDamageToTargetParameters parameters, ref IEnumerator __result)
            {
                if (observing) __result = Observe(__result, damage, target, parameters.playedCard);
            }

            private static IEnumerator Observe(IEnumerator native, int amount, CharacterState target, CardState? playedCard)
            {
                var trace = FullBattleTrace.Active!;
                var sample = new DamageSample { TargetId = trace.UnitId(target), SourceCardId = playedCard == null ? null : trace.CardId(playedCard),
                    Amount = amount, HealthBefore = target.GetHP(), ArmorBefore = target.GetStatusEffectStacks("armor") };
                Requests.Add(sample);
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    sample.HealthAfter = target.IsDestroyed ? 0 : target.GetHP();
                    sample.ArmorAfter = target.IsDestroyed ? 0 : target.GetStatusEffectStacks("armor");
                    sample.Completed = true;
                }
                finally { (native as IDisposable)?.Dispose(); }
            }
        }
    }
}
#endif
