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
    internal static partial class OriginalLittleFadeCostWitness
    {
        private const string ChampionId = "d2a5feba-4ecd-4d5d-a206-ec2ebfb96667";
        private const string RankId = "738b4ac0-d62b-4d79-8340-e6dc154b1b5f";
        private const string SpikesRankId = "08749cab-1cdb-451b-9164-b60be12c4b52";
        private const string SpikesUpgradeId = "be6a64f5-e88e-42d6-aa29-9e21cff98428";
        private const string EndlessShopUpgradeId = "2aade38c-4687-40ce-9507-2a9e80cb139c";
        private static readonly Guid Module = new("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static string? Mode => Environment.GetEnvironmentVariable("MT2_PROBE_LITTLE_FADE_COST_WITNESS");
        internal static bool Enabled => Mode is "1" or "BuffFriendly" or "PermanentSpikes";
        internal static bool PermanentSpikes => Mode == "PermanentSpikes";
        private static CharacterTriggerData.Trigger UpgradeTrigger => PermanentSpikes
            ? CharacterTriggerData.Trigger.OnKill : CharacterTriggerData.Trigger.OnDeath;
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
        private static (int Cost, int Attack, int Health, int Spikes) beforeDeath;
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
            string rankId = BuffFriendly ? BuffFriendlyRankId : PermanentSpikes ? SpikesRankId : RankId;
            CardUpgradeData chosen = tree.GetUpgradeTrees().Select(path => path.GetCardUpgrades().FirstOrDefault())
                .Single(candidate => candidate?.GetID() == rankId)!;
            if (character.GetAssetKey() != "MonsterFirstWaxer")
                throw new InvalidOperationException("Expected the pinned original Little Fade character.");
            if (BuffFriendly) PrepareBuffFriendly(chosen);
            else PrepareEndless(chosen);
            if (card.GetCardStateModifiers().GetCardUpgrades().Count != 0)
                throw new InvalidOperationException("Expected no existing Little Fade rank before stock upgrade.");
            var modifier = new CardUpgradeState();
            modifier.Setup(chosen);
            card.ApplyPermanentUpgrade(modifier, save, ignoreUpgradeAnimation: true);
            card.RemoveEarlierTreeUpgrades(modifier, tree);
            if (card.GetCardStateModifiers().GetCardUpgrades().Single().GetCardUpgradeDataId() != chosen.GetID())
                throw new InvalidOperationException("The native Little Fade rank did not settle on the original card.");
            if (PermanentSpikes) AttachEndlessShopUpgrade(save, card);
            rank = chosen;
        }

        private static void PrepareEndless(CardUpgradeData chosen)
        {
            string effectType = PermanentSpikes ? nameof(CardEffectAddCardUpgradeToUnits) : nameof(CardEffectAddTempCardUpgradeToUnits);
            var triggers = chosen.GetCharacterTriggerUpgrades();
            CardEffectData? effect = triggers.Where(trigger => trigger.GetTrigger() == UpgradeTrigger)
                .SelectMany(trigger => trigger.GetEffects()).SingleOrDefault(data => data.GetEffectStateName() == effectType);
            if (effect == null)
            {
                string observed = string.Join("; ", triggers.Select(trigger => trigger.GetTrigger() + ": " +
                    string.Join(", ", trigger.GetEffects().Select(data => data.GetEffectStateName()))));
                throw new InvalidOperationException($"Expected Little Fade {chosen.name} {UpgradeTrigger}/{effectType}. Authored triggers: {observed}");
            }
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
            var interactions = new List<string>();
            authoredEffect = UnitTriggerUpgradeProbe.Definition(effect, interactions);
            if (authoredEffect is not { Target: "Self", Upgrade: not null } || interactions.Count != 0)
                throw new InvalidOperationException("The attached Little Fade death-upgrade payload changed.");
            if (PermanentSpikes)
            {
                if (upgrade?.GetID() != SpikesUpgradeId || authoredEffect.Lifetime != "Permanent" ||
                    authoredEffect.Upgrade.Stats is not { Damage: 2, Health: 0, Cost: 0 } ||
                    authoredEffect.Upgrade.Statuses.ToArray() is not [{ Id: "spikes", Stacks: 2 }] ||
                    chosen.GetStatusEffectUpgrades().ToArray() is not [{ statusId: "spikes", count: 7 }])
                    throw new InvalidOperationException("The attached Little Fade permanent Spikes payload changed.");
            }
            else if (upgrade?.name != "RemnantChampIncreaseCost1" ||
                authoredEffect.Upgrade.Stats is not { Damage: 15, Health: 1, Cost: 0 } ||
                !chosen.GetStatusEffectUpgrades().Any(status => status.statusId == "endless" && status.count > 0))
                throw new InvalidOperationException("The attached Little Fade death-upgrade payload changed.");
            costUpgrade = upgrade;
        }

        private static void AttachEndlessShopUpgrade(SaveManager save, CardState card)
        {
            CardUpgradeData upgrade = save.GetAllGameData().FindCardUpgradeData(EndlessShopUpgradeId);
            if (upgrade?.name != "AddEndless" ||
                upgrade.GetStatusEffectUpgrades().ToArray() is not [{ statusId: "endless", count: 1 }])
                throw new InvalidOperationException("The original Endless shop upgrade changed.");
            // The isolated setup combines the stock champion rank and the stock shop upgrade.
            var modifier = new CardUpgradeState();
            modifier.Setup(upgrade);
            card.ApplyPermanentUpgrade(modifier, save, ignoreUpgradeAnimation: true);
            if (!PermanentUpgrades(card).SequenceEqual(new[] { SpikesRankId, EndlessShopUpgradeId }))
                throw new InvalidOperationException("The stock Endless upgrade did not settle on the original Little Fade card.");
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
            if (BuffFriendly) return TickBuffFriendly(trace, managers, room, log, advanceTurn);
            if (champion == null)
            {
                champion = cards.GetHand().Single(card => card.GetCardDataID() == ChampionId);
                deploymentBefore = trace.CaptureDecision();
                deployment = PlayDeployment(trace, cards, room, champion);
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
            string[] permanent = PermanentUpgrades(champion);
            string upgradeId = costUpgrade!.GetID();
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = PermanentSpikes ? "OriginalLittleFadePermanentSpikes" : "OriginalLittleFadeEndlessCost",
                GameVersion = Application.version, GameModuleMvid = Module,
                ChampionId, RankId = rank!.GetID(), UpgradeId = upgradeId, AuthoredEffect = authoredEffect, ActorId = actorId, CardInstanceId = cardId,
                DeploymentBefore = deploymentBefore, Deployment = deployment, DeploymentAfter = deploymentAfter,
                DeploymentPaidCost = champion.GetLastPlayedCost(), Before = before, After = after,
                CostBeforeDeath = beforeDeath.Cost, CostAfterReturn = afterReturn.Cost,
                AttackBeforeDeath = beforeDeath.Attack, AttackAfterReturn = afterReturn.Attack,
                HealthBeforeDeath = beforeDeath.Health, HealthAfterReturn = afterReturn.Health,
                SameCardInHand = sameCardInHand, TemporaryUpgrades = temporary, DeathUpgrade = PermanentSpikes ? null : callback,
                PermanentUpgrades = permanent, SpikesBeforeDeath = beforeDeath.Spikes, SpikesAfterReturn = afterReturn.Spikes,
                EndlessSupportUpgradeId = PermanentSpikes ? EndlessShopUpgradeId : null,
                AuthoredTrigger = UpgradeTrigger.ToString(), UpgradeCallback = PermanentSpikes ? callback : null
            });
            string archive = PermanentSpikes ? "little-fade-permanent-spikes.mt2f" : "little-fade-endless-cost.mt2f";
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, archive)))
                document.Write(output);
            bool upgradeSettled = PermanentSpikes ? temporary.Length == 0 && permanent.Count(id => id == upgradeId) == 1 :
                temporary.Length == 1 && temporary[0] == upgradeId;
            if (!observedDeathUpgrade || !sameCardInHand || !MatchesUpgrade(beforeDeath, afterReturn) ||
                !upgradeSettled ||
                after.Spawn.Train.Rooms.SelectMany(state => state.Units).Any(unit => unit.Id == actorId))
                throw new InvalidOperationException($"The original Little Fade did not settle the {UpgradeTrigger} upgrade and Endless hand return. " +
                    $"callback={callback != null}, sameCard={sameCardInHand}, spikes={beforeDeath.Spikes}->{afterReturn.Spikes}");
            string mode = PermanentSpikes ? "PERMANENT-SPIKES" : "ENDLESS-COST";
            log.LogInfo($"ORIGINAL-LITTLE-FADE-{mode} actor={actorId} card={cardId} upgrade={upgradeId} " +
                $"attack={beforeDeath.Attack}->{afterReturn.Attack} health={beforeDeath.Health}->{afterReturn.Health} " +
                $"cost={beforeDeath.Cost}->{afterReturn.Cost} spikes={beforeDeath.Spikes}->{afterReturn.Spikes}");
            return true;
        }

        private static PlayCardAction PlayDeployment(FullBattleTrace trace, CardManager cards, RoomState room, CardState card)
        {
            var point = room.GetFirstEmptyMonsterPoint() ?? throw new InvalidOperationException("Expected an open native deployment point.");
            var input = new PlayCardAction(trace.CardId(card), 0, playerPosition: point.GetIndexInRoom());
            if (!cards.CanPlayHandCard(card, 0, point, null, null, out var error) ||
                !cards.PlayCard(cards.GetHand().IndexOf(card), point, ref error))
                throw new InvalidOperationException("Native Little Fade witness deployment was rejected: " + error);
            return input;
        }

        private static (int Cost, int Attack, int Health, int Spikes) CardStats(AllGameManagers managers, CardState card)
        {
            var statuses = new List<StatusEffectStackData>();
            if (PermanentSpikes) card.TryGetStatusEffects(statuses);
            return (card.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(), managers.GetRelicManager()),
                card.GetTotalAttackDamage(), (int)Math.Round(card.GetHealth()),
                statuses.Where(status => status.statusId == "spikes").Sum(status => status.count));
        }

        private static bool MatchesUpgrade((int Cost, int Attack, int Health, int Spikes) before,
            (int Cost, int Attack, int Health, int Spikes) after) => after.Cost == before.Cost &&
                (PermanentSpikes ? after.Attack == before.Attack + 2 && after.Health == before.Health && before.Spikes == 7 && after.Spikes == 9 :
                    after.Attack == before.Attack + 15 && after.Health == before.Health + 1);

        private static string[] TemporaryUpgrades(CardState card) =>
            card.GetTemporaryCardStateModifiers().GetCardUpgrades().Select(upgrade => upgrade.GetCardUpgradeDataId()).ToArray();

        private static string[] PermanentUpgrades(CardState card) =>
            card.GetCardStateModifiers().GetCardUpgrades().Select(upgrade => upgrade.GetCardUpgradeDataId()).ToArray();

        [HarmonyPatch(typeof(CardEffectAddCardUpgradeToUnits), nameof(CardEffectAddCardUpgradeToUnits.ApplyEffect))]
        private static class DeathUpgradePatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                var managers = AllGameManagers.Instance;
                if (ObservingBuffFriendly && ReferenceEquals(cardEffectParams.selfTarget, actor) &&
                    cardEffectParams.sourceCharacterTriggerState?.GetTrigger() == CharacterTriggerData.Trigger.OnDeath &&
                    ReferenceEquals(cardEffectState.GetSourceCardEffectData(), buffEffect))
                {
                    __result = ObserveBuffFriendly(__result, "OnDeathUpgrade", actor!, cardEffectParams.targets.ToArray());
                    return;
                }
                if (awaitingReturn && managers != null && !managers.GetSaveManager().PreviewMode &&
                    ReferenceEquals(cardEffectParams.selfTarget, actor) &&
                    cardEffectParams.sourceCharacterTriggerState?.GetTrigger() == UpgradeTrigger &&
                    cardEffectState.GetSourceCardEffectData().GetParamCardUpgradeData()?.GetID() == costUpgrade!.GetID())
                    __result = Observe(__result, managers, cardEffectParams);
            }

            private static IEnumerator Observe(IEnumerator native, AllGameManagers managers, CardEffectParams parameters)
            {
                var card = champion!;
                var beforeCallback = CardStats(managers, card);
                string[] upgradesBefore = TemporaryUpgrades(card);
                string[] permanentBefore = PermanentUpgrades(card);
                int[] targets = parameters.targets.Select(FullBattleTrace.Active!.UnitId).ToArray();
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    var afterCallback = CardStats(managers, card);
                    var upgradesAfter = TemporaryUpgrades(card);
                    var permanentAfter = PermanentUpgrades(card);
                    string upgradeId = costUpgrade!.GetID();
                    bool placement = PermanentSpikes ? upgradesAfter.Length == 0 && permanentAfter.SequenceEqual(permanentBefore.Append(upgradeId)) :
                        upgradesAfter.Length == 1 && upgradesAfter[0] == upgradeId;
                    observedDeathUpgrade = targets.Length == 1 && targets[0] == actorId && upgradesBefore.Length == 0 &&
                        placement && MatchesUpgrade(beforeCallback, afterCallback);
                    callback = new { Trigger = UpgradeTrigger.ToString(), SourceActorId = actorId, Targets = targets,
                        CostBefore = beforeCallback.Cost, CostAfter = afterCallback.Cost,
                        AttackBefore = beforeCallback.Attack, AttackAfter = afterCallback.Attack,
                        HealthBefore = beforeCallback.Health, HealthAfter = afterCallback.Health,
                        UpgradesBefore = upgradesBefore, UpgradesAfter = upgradesAfter, PermanentUpgradesBefore = permanentBefore,
                        PermanentUpgradesAfter = permanentAfter, SpikesBefore = beforeCallback.Spikes, SpikesAfter = afterCallback.Spikes };
                }
                finally { (native as IDisposable)?.Dispose(); }
            }
        }
    }
}
#endif
