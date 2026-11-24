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
    internal static class OriginalLittleFadeCostWitness
    {
        private const string ChampionId = "d2a5feba-4ecd-4d5d-a206-ec2ebfb96667";
        private const string RankId = "738b4ac0-d62b-4d79-8340-e6dc154b1b5f";
        private static readonly Guid Module = new("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_LITTLE_FADE_COST_WITNESS") == "1";
        internal static bool Prepared => rank != null;
        private static CardUpgradeData? rank, costUpgrade;
        private static CardState? champion;
        private static CharacterState? actor;
        private static CardActionEffect? authoredEffect;
        private static BattleTurnState? deploymentBefore, deploymentAfter, before;
        private static PlayCardAction? deployment;
        private static object? callback;
        private static bool awaitingReturn;
        private static bool observedDeathUpgrade;
        private static (int Cost, int Attack, int Health) beforeDeath;
        private static int actorId;

        internal static void SelectChampion(RunSetupScreen setup)
        {
            var main = (RunSetupClassLevelInfoUI)AccessTools.Field(typeof(RunSetupScreen), "mainClassInfo").GetValue(setup);
            if (main.ClassData?.GetChampionCard(1)?.GetID() != ChampionId)
                throw new InvalidOperationException("Expected Little Fade in the original alternate champion slot.");
            if (main.ChampionIndex != 1)
                AccessTools.Method(typeof(RunSetupClassLevelInfoUI), "SetChampionIndex").Invoke(main, new object[] { 1 });
        }

        internal static void Prepare(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(CardData).Module.ModuleVersionId != Module || save.PreviewMode ||
                save.GetCurrentDistance() != 0 || save.GetGameSequence() != SaveData.GameSequence.BattleIntro)
                throw new InvalidOperationException("Expected the pinned original 2.3.1 Little Fade first battle.");
            CardState card = save.GetDeckState().Single(item => item.GetCardDataID() == ChampionId);
            CharacterData character = card.GetSpawnCharacterData();
            CardUpgradeTreeData tree = ChampionUpgradeRewardData.GetUpgradeTree(character, save.GetBalanceData());
            CardUpgradeData chosen = tree.GetUpgradeTrees().Select(path => path.GetCardUpgrades().FirstOrDefault())
                .Single(candidate => candidate?.GetID() == RankId)!;
            CardEffectData effect = chosen.GetCharacterTriggerUpgrades()
                .Single(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.OnDeath).GetEffects()
                .Single(data => data.GetEffectStateName() == nameof(CardEffectAddTempCardUpgradeToUnits));
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
            var interactions = new List<string>();
            authoredEffect = UnitTriggerUpgradeProbe.Definition(effect, interactions);
            if (character.GetAssetKey() != "MonsterFirstWaxer" || upgrade?.name != "RemnantChampIncreaseCost1" ||
                authoredEffect is not { Target: "Self", Upgrade: not null } || interactions.Count != 0 ||
                authoredEffect.Upgrade.Stats is not { Damage: 15, Health: 1, Cost: 0 } ||
                !chosen.GetStatusEffectUpgrades().Any(status => status.statusId == "endless" && status.count > 0))
                throw new InvalidOperationException("The attached Little Fade death-upgrade payload changed.");
            if (card.GetCardStateModifiers().GetCardUpgrades().Count != 0)
                throw new InvalidOperationException("Expected no existing Little Fade rank before stock upgrade.");
            var modifier = new CardUpgradeState();
            modifier.Setup(chosen);
            card.ApplyPermanentUpgrade(modifier, save, ignoreUpgradeAnimation: true);
            card.RemoveEarlierTreeUpgrades(modifier, tree);
            if (card.GetCardStateModifiers().GetCardUpgrades().Single().GetCardUpgradeDataId() != RankId)
                throw new InvalidOperationException("The native Little Fade rank did not settle on the original card.");
            rank = chosen;
            costUpgrade = upgrade;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            var cards = managers.GetCardManager();
            var combat = managers.GetCombatManager();
            var rooms = managers.GetRoomManager();
            var room = rooms.GetRoom(0);
            var save = managers.GetSaveManager();
            if (rooms.GetSelectedRoom() != 0)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                return false;
            }
            if (champion == null)
            {
                champion = cards.GetHand().Single(card => card.GetCardDataID() == ChampionId);
                var point = room.GetFirstEmptyMonsterPoint() ?? throw new InvalidOperationException("Expected native Little Fade placement.");
                deploymentBefore = trace.CaptureDecision();
                deployment = new PlayCardAction(trace.CardId(champion), 0, playerPosition: point.GetIndexInRoom());
                if (!cards.CanPlayHandCard(champion, 0, point, null, null, out var error) ||
                    !cards.PlayCard(cards.GetHand().IndexOf(champion), point, ref error))
                    throw new InvalidOperationException("Native Little Fade deployment was rejected: " + error);
                return false;
            }
            if (deploymentAfter == null)
            {
                deploymentAfter = trace.CaptureDecision();
                var units = new List<CharacterState>();
                room.AddCharactersToList(units, Team.Type.Monsters);
                actor = units.Single(unit => ReferenceEquals(unit.GetSpawnerCard(), champion) && unit.IsAlive);
                if (actor.GetStatusEffectStacks("endless") <= 0)
                    throw new InvalidOperationException("The paid original Little Fade did not receive Endless.");
                actorId = trace.UnitId(actor);
                advanceTurn();
                return false;
            }
            if (actor != null && actor.IsAlive && !actor.IsDestroyed)
            {
                if (combat.GetTurnCount() >= 4)
                    throw new InvalidOperationException("Little Fade survived three ordinary native combats.");
                before = trace.CaptureDecision();
                beforeDeath = CardStats(managers, champion);
                awaitingReturn = true;
                advanceTurn();
                return false;
            }

            BattleTurnState after = trace.CaptureDecision();
            int cardId = trace.CardId(champion);
            var afterReturn = CardStats(managers, champion);
            bool sameCardInHand = cards.GetHand().Any(card => ReferenceEquals(card, champion));
            string[] temporary = TemporaryUpgrades(champion);
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalLittleFadeEndlessCost", GameVersion = Application.version, GameModuleMvid = Module,
                ChampionId, RankId, UpgradeId = costUpgrade!.GetID(), AuthoredEffect = authoredEffect, ActorId = actorId, CardInstanceId = cardId,
                DeploymentBefore = deploymentBefore, Deployment = deployment, DeploymentAfter = deploymentAfter,
                DeploymentPaidCost = champion.GetLastPlayedCost(), Before = before, After = after,
                CostBeforeDeath = beforeDeath.Cost, CostAfterReturn = afterReturn.Cost,
                AttackBeforeDeath = beforeDeath.Attack, AttackAfterReturn = afterReturn.Attack,
                HealthBeforeDeath = beforeDeath.Health, HealthAfterReturn = afterReturn.Health,
                SameCardInHand = sameCardInHand, TemporaryUpgrades = temporary, DeathUpgrade = callback
            });
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "little-fade-endless-cost.mt2f")))
                document.Write(output);
            if (!observedDeathUpgrade || !sameCardInHand || !MatchesUpgrade(beforeDeath, afterReturn) ||
                temporary.Length != 1 || temporary[0] != costUpgrade.GetID() ||
                after.Spawn.Train.Rooms.SelectMany(state => state.Units).Any(unit => unit.Id == actorId))
                throw new InvalidOperationException("The original Little Fade did not settle the OnDeath stat upgrade and Endless hand return.");
            log.LogInfo($"ORIGINAL-LITTLE-FADE-ENDLESS-COST actor={actorId} card={cardId} upgrade={temporary[0]} " +
                $"attack={beforeDeath.Attack}->{afterReturn.Attack} health={beforeDeath.Health}->{afterReturn.Health} cost={beforeDeath.Cost}->{afterReturn.Cost}");
            return true;
        }

        private static (int Cost, int Attack, int Health) CardStats(AllGameManagers managers, CardState card) =>
            (card.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(), managers.GetRelicManager()),
                card.GetTotalAttackDamage(), (int)Math.Round(card.GetHealth()));

        private static bool MatchesUpgrade((int Cost, int Attack, int Health) before, (int Cost, int Attack, int Health) after) =>
            after.Cost == before.Cost && after.Attack == before.Attack + 15 && after.Health == before.Health + 1;

        private static string[] TemporaryUpgrades(CardState card) =>
            card.GetTemporaryCardStateModifiers().GetCardUpgrades().Select(upgrade => upgrade.GetCardUpgradeDataId()).ToArray();

        [HarmonyPatch(typeof(CardEffectAddCardUpgradeToUnits), nameof(CardEffectAddCardUpgradeToUnits.ApplyEffect))]
        private static class DeathUpgradePatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                var managers = AllGameManagers.Instance;
                if (awaitingReturn && managers != null && !managers.GetSaveManager().PreviewMode &&
                    ReferenceEquals(cardEffectParams.selfTarget, actor) &&
                    cardEffectParams.sourceCharacterTriggerState?.GetTrigger() == CharacterTriggerData.Trigger.OnDeath &&
                    cardEffectState.GetSourceCardEffectData().GetParamCardUpgradeData()?.GetID() == costUpgrade!.GetID())
                    __result = Observe(__result, managers, cardEffectParams);
            }

            private static IEnumerator Observe(IEnumerator native, AllGameManagers managers, CardEffectParams parameters)
            {
                var card = champion!;
                var beforeCallback = CardStats(managers, card);
                string[] upgradesBefore = TemporaryUpgrades(card);
                int[] targets = parameters.targets.Select(FullBattleTrace.Active!.UnitId).ToArray();
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    var afterCallback = CardStats(managers, card);
                    var upgradesAfter = TemporaryUpgrades(card);
                    observedDeathUpgrade = targets.Length == 1 && targets[0] == actorId && upgradesBefore.Length == 0 &&
                        upgradesAfter.Length == 1 && upgradesAfter[0] == costUpgrade!.GetID() && MatchesUpgrade(beforeCallback, afterCallback);
                    callback = new { Trigger = "OnDeath", SourceActorId = actorId, Targets = targets,
                        CostBefore = beforeCallback.Cost, CostAfter = afterCallback.Cost,
                        AttackBefore = beforeCallback.Attack, AttackAfter = afterCallback.Attack,
                        HealthBefore = beforeCallback.Health, HealthAfter = afterCallback.Health,
                        UpgradesBefore = upgradesBefore, UpgradesAfter = upgradesAfter };
                }
                finally { (native as IDisposable)?.Dispose(); }
            }
        }
    }
}
#endif
