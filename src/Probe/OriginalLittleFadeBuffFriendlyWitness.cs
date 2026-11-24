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
        private const string BuffFriendlyRank = "LittleFadeBuffFriendlyOnDeath";
        private const string BuffFriendlyRankId = "eb4f67b9-f3ad-40db-9212-4ed647e17296";
        private const string BuffFriendlyUpgradeId = "42849eec-c299-40ce-a344-3fc490ee052b";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool BuffFriendly => Mode == "BuffFriendly";
        private static CardState? recipientCard;
        private static CharacterState? recipient;
        private static CardEffectData? buffEffect, ephemeralGrant;
        private static object[]? buffFriendlyEffects;
        private static readonly List<DeploymentSample> deployments = new();
        private static readonly List<CallbackSample> buffFriendlyCallbacks = new();
        private static readonly List<EndTurnSample> buffFriendlyEndTurns = new();

        private sealed class DeploymentSample
        {
            public BattleTurnState Before { get; set; } = null!;
            public PlayCardAction Input { get; set; } = null!;
            public BattleTurnState? After { get; set; }
        }

        private sealed class EndTurnSample
        {
            public BattleTurnState Before { get; set; } = null!;
            public BattleTurnState? After { get; set; }
        }

        private sealed class CallbackSample
        {
            public int Sequence { get; set; }
            public string Kind { get; set; } = "";
            public int SourceActorId { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public CombatUnit[] BeforeUnits { get; set; } = Array.Empty<CombatUnit>();
            public CombatUnit[]? AfterUnits { get; set; }
        }

        private static void PrepareBuffFriendly(CardUpgradeData chosen)
        {
            CardEffectData[] effects = chosen.GetCharacterTriggerUpgrades()
                .Single(trigger => trigger.GetTrigger() == CharacterTriggerData.Trigger.OnDeath).GetEffects().ToArray();
            if (effects is not [var upgrade, var grant] ||
                chosen.GetID() != BuffFriendlyRankId ||
                upgrade.GetEffectStateName() != nameof(CardEffectAddTempCardUpgradeToUnits) ||
                upgrade.GetParamCardUpgradeData()?.GetID() != BuffFriendlyUpgradeId ||
                grant.GetEffectStateName() != nameof(CardEffectAddStatusEffect) ||
                grant.GetParamStatusEffects().ToArray() is not [{ statusId: "ephemeral", count: 2 }])
                throw new InvalidOperationException("The original Little Fade BuffFriendly OnDeath effect order changed.");
            buffEffect = upgrade;
            ephemeralGrant = grant;
            buffFriendlyEffects = effects.Select(RuleCatalogProbe.Effect).ToArray();
        }

        private static bool TickBuffFriendly(FullBattleTrace trace, AllGameManagers managers, RoomState room, ManualLogSource log, Action advanceTurn)
        {
            var cards = managers.GetCardManager();
            int turn = managers.GetCombatManager().GetTurnCount();
            if (turn == 0)
            {
                if (deployments.Count != 0) deployments.Last().After = trace.CaptureDecision();
                if (champion == null)
                {
                    champion = cards.GetHand().Single(card => card.GetCardDataID() == ChampionId);
                    Deploy(champion);
                    return false;
                }
                if (recipientCard == null)
                {
                    recipientCard = cards.GetHand().First(card => card.GetCardDataID() == StewardId);
                    Deploy(recipientCard);
                    return false;
                }
                var units = new List<CharacterState>();
                room.AddCharactersToList(units, Team.Type.Monsters);
                actor = units.Single(unit => ReferenceEquals(unit.GetSpawnerCard(), champion) && unit.IsAlive);
                recipient = units.Single(unit => ReferenceEquals(unit.GetSpawnerCard(), recipientCard) && unit.IsAlive);
                actorId = trace.UnitId(actor);
                if (actor.GetSpawnPoint().GetIndexInRoom() >= recipient.GetSpawnPoint().GetIndexInRoom())
                    throw new InvalidOperationException("Expected Little Fade ahead of the original same-floor Steward.");
            }
            else
            {
                var last = buffFriendlyEndTurns.Last();
                last.After = trace.CaptureDecision();
                if (buffFriendlyCallbacks.Any(sample => sample.Kind == "OnDeathEphemeralGrant" && sample.AfterUnits != null) || turn >= 4)
                    return WriteBuffFriendly(trace, log);
            }

            buffFriendlyEndTurns.Add(new EndTurnSample { Before = trace.CaptureDecision() });
            advanceTurn();
            return false;

            void Deploy(CardState card)
            {
                BattleTurnState start = trace.CaptureDecision();
                deployments.Add(new DeploymentSample { Before = start, Input = PlayDeployment(trace, cards, room, card) });
            }
        }

        private static bool WriteBuffFriendly(FullBattleTrace trace, ManualLogSource log)
        {
            int recipientId = trace.UnitId(recipient!);
            string upgradeId = buffEffect!.GetParamCardUpgradeData().GetID();
            var last = buffFriendlyEndTurns.Last();
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalLittleFadeBuffFriendly", GameVersion = Application.version, GameModuleMvid = Module,
                ChampionId, RankId = rank!.GetID(), RankAssetKey = BuffFriendlyRank, UpgradeId = upgradeId,
                AuthoredEffects = buffFriendlyEffects, ActorId = actorId, RecipientId = recipientId,
                CardInstanceId = trace.CardId(champion!), RecipientCardInstanceId = trace.CardId(recipientCard!),
                Deployments = deployments, Before = last.Before, After = last.After, EndTurns = buffFriendlyEndTurns,
                Callbacks = buffFriendlyCallbacks
            });
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "little-fade-buff-friendly.mt2f")))
                document.Write(output);

            if (buffFriendlyCallbacks is not [var upgrade, var grant, var ephemeral] ||
                !OnRecipient(upgrade, "OnDeathUpgrade", actorId, recipientId) ||
                !OnRecipient(grant, "OnDeathEphemeralGrant", actorId, recipientId) ||
                !OnRecipient(ephemeral, "Ephemeral", recipientId, recipientId) ||
                upgrade.AfterUnits![0].BaseAttack != upgrade.BeforeUnits[0].BaseAttack + 5 ||
                upgrade.AfterUnits[0].MaxHealth != upgrade.BeforeUnits[0].MaxHealth + 5 ||
                EphemeralStacks(grant.BeforeUnits[0]) != 0 || EphemeralStacks(grant.AfterUnits![0]) != 2 ||
                EphemeralStacks(ephemeral.BeforeUnits[0]) != 1 || EphemeralStacks(ephemeral.AfterUnits![0]) != 1 ||
                last.After!.Spawn.Train.Rooms.SelectMany(floor => floor.Units).Any(unit => unit.Id == actorId))
                throw new InvalidOperationException("The original Little Fade did not settle the ordered same-floor buff and Ephemeral callbacks.");
            CombatUnit buffed = upgrade.AfterUnits[0];
            log.LogInfo($"ORIGINAL-LITTLE-FADE-BUFF-FRIENDLY actor={actorId} recipient={recipientId} upgrade={upgradeId} " +
                $"attack={upgrade.BeforeUnits[0].BaseAttack}->{buffed.BaseAttack} health={upgrade.BeforeUnits[0].MaxHealth}->{buffed.MaxHealth} " +
                $"ephemeral=0->2->1");
            return true;
        }

        private static bool OnRecipient(CallbackSample sample, string kind, int sourceId, int recipientId) =>
            sample.Kind == kind && sample.SourceActorId == sourceId && sample.Targets is [var target] && target == recipientId &&
            sample.BeforeUnits.Length == 1 && sample.AfterUnits is { Length: 1 };

        private static int EphemeralStacks(CombatUnit unit) => unit.Status("ephemeral")?.Stacks ?? 0;

        [HarmonyPatch(typeof(CardEffectAddStatusEffect), nameof(CardEffectAddStatusEffect.ApplyEffect))]
        private static class BuffFriendlyGrantPatch
        {
            private static bool Prepare() => BuffFriendly;
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (ObservingBuffFriendly && ReferenceEquals(cardEffectParams.selfTarget, actor) &&
                    cardEffectParams.sourceCharacterTriggerState?.GetTrigger() == CharacterTriggerData.Trigger.OnDeath &&
                    ReferenceEquals(cardEffectState.GetSourceCardEffectData(), ephemeralGrant))
                    __result = ObserveBuffFriendly(__result, "OnDeathEphemeralGrant", actor!, cardEffectParams.targets.ToArray());
            }
        }

        private static bool ObservingBuffFriendly => BuffFriendly && buffFriendlyEndTurns.Count != 0 &&
            FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        private static IEnumerator ObserveBuffFriendly(IEnumerator native, string kind, CharacterState source, CharacterState[] targets)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var sample = new CallbackSample { Sequence = trace.NextPhaseSequence(), Kind = kind, SourceActorId = trace.UnitId(source),
                Targets = targets.Select(trace.UnitId).ToArray(), BeforeUnits = targets.Select(target => trace.CaptureUnit(target)).ToArray() };
            buffFriendlyCallbacks.Add(sample);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                sample.AfterUnits = targets.Select(target => trace.CaptureUnit(target)).ToArray();
            }
            finally { (native as IDisposable)?.Dispose(); }
        }

        [HarmonyPatch(typeof(StatusEffectEphemeralState), "OnTriggered")]
        private static class BuffFriendlyEphemeralPatch
        {
            private static bool Prepare() => BuffFriendly;
            private static void Postfix(StatusEffectState.InputTriggerParams inputTriggerParams, ref IEnumerator __result)
            {
                CharacterState unit = inputTriggerParams.associatedCharacter;
                if (ObservingBuffFriendly && ReferenceEquals(unit, recipient))
                    __result = ObserveBuffFriendly(__result, "Ephemeral", unit, new[] { unit });
            }
        }
    }
}
#endif
