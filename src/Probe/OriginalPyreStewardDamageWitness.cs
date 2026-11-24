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
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPyreStewardDamageWitness
    {
        private const string StewardId = "e90fc0e0-b383-48a9-bb10-d8aaa1518531";
        private const string AbilityId = "4f26edf3-6b37-4103-861f-3cdd7c7cf129";
        private const string Boundary = "OriginalPyreStewardDamage";
        private const int Floor = 0;
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_PYRE_STEWARD_DAMAGE") == "1";
        private static CardState? stewardCard, abilityCard;
        private static BattleTurnState? deploymentBefore, deploymentAfter, beforeAbility;
        private static PlayCardAction? deploymentPlay, activation;
        private static object? definitionEvidence;
        private static ScalingSample? scaling;
        private static bool observingScaledEffect;
        private static readonly List<object> DamageRequests = new();

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var rooms = managers.GetRoomManager();
            var cards = managers.GetCardManager();
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected a settled live Ring 1 decision from the pinned original game.");
            if (rooms.GetSelectedRoom() != Floor)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(Floor));
                return false;
            }

            var room = rooms.GetRoom(Floor);
            if (deploymentBefore == null)
            {
                if (!combat.IsPlacementPhase) throw new InvalidOperationException("Expected the original placement hand.");
                CardData definition = managers.GetAllGameData().FindCardData(StewardId)
                    ?? throw new InvalidOperationException("Original Pyre Steward was missing.");
                definitionEvidence = Definition(definition);
                stewardCard = cards.GetHand().FirstOrDefault(item => item.GetCardDataID() == StewardId) ??
                    cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false)
                    ?? throw new InvalidOperationException("Original Pyre Steward could not enter the placement hand.");
                var point = room.GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("No original placement point for Pyre Steward.");
                if (!cards.CanPlayHandCard(stewardCard, Floor, point, null, null, out var error))
                    throw new InvalidOperationException("Original Pyre Steward was unavailable: " + error);
                deploymentBefore = trace.CaptureDecision();
                deploymentPlay = new PlayCardAction(trace.CardId(stewardCard), Floor);
                if (!cards.PlayCard(cards.GetHand().IndexOf(stewardCard), point, ref error))
                    throw new InvalidOperationException("Original Pyre Steward was rejected: " + error);
                return false;
            }
            if (deploymentAfter == null)
            {
                deploymentAfter = trace.CaptureDecision();
                if (!combat.IsPlacementPhase || deploymentBefore.Energy - deploymentAfter.Energy != 1 ||
                    cards.GetHand().Contains(stewardCard!))
                    throw new InvalidOperationException("Expected the original Pyre Steward to settle as a paid deployment.");
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                advanceTurn();
                return false;
            }

            if (combat.GetTurnCount() != 1) throw new InvalidOperationException("Expected the first ordinary decision.");
            if (beforeAbility == null)
            {
                var actors = new List<CharacterState>();
                room.AddCharactersToList(actors, Team.Type.Monsters);
                var actor = actors.Single(unit => ReferenceEquals(unit.GetSpawnerCard(), stewardCard));
                abilityCard = actor.GetUnitAbilityCardState();
                if (abilityCard?.GetCardDataID() != AbilityId || !actor.CanActivateUnitAbility() || !actor.CanTargetUnitAbilityInRoom(Floor))
                    throw new InvalidOperationException("The original Pyre Steward ability was unavailable.");
                beforeAbility = trace.CaptureDecision();
                if (!beforeAbility.Spawn.Train.Rooms.Single(item => item.RoomIndex == Floor).Units
                    .Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0))
                    throw new InvalidOperationException("The native first wave contained no enemy damage target.");
                activation = new PlayCardAction(trace.CardId(abilityCard), Floor, activatorUnitId: trace.UnitId(actor));
                if (!actor.ActivateUnitAbility(cards))
                    throw new InvalidOperationException("Native Pyre Steward ability activation was rejected.");
                return false;
            }

            var afterAbility = trace.CaptureDecision();
            if (scaling is not { Completed: true } || scaling.DisplayedPyreAttack <= 0 ||
                !scaling.Requests.Select(request => request.TargetId).SequenceEqual(scaling.Targets) ||
                scaling.Requests.Count == 0 || scaling.Requests.Any(request =>
                    request.Amount != scaling.DisplayedPyreAttack || request.Team != "Heroes" || request.RoomIndex != Floor) ||
                beforeAbility.Energy != afterAbility.Energy)
                throw new InvalidOperationException("Original Pyre Steward damage did not settle with native displayed attack and actual enemy recipients.");
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary, GameVersion = Application.version, GameModuleMvid = Module, Definition = definitionEvidence,
                DeploymentBefore = deploymentBefore, DeploymentPlay = deploymentPlay, DeploymentAfter = deploymentAfter,
                Before = beforeAbility, Action = activation, After = afterAbility, Scaling = scaling, DamageRequests,
                SettledPyre = ObservePyre()
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "pyre-steward-damage.mt2f"));
            document.Write(stream);
            log.LogInfo($"ORIGINAL-PYRE-STEWARD-DAMAGE OBSERVED displayed attack={scaling.DisplayedPyreAttack} targets={scaling.Targets.Length}");
            return true;
        }

        private static object Definition(CardData unit)
        {
            var character = unit.GetSpawnCharacterData();
            var ability = character?.GetUnitAbilityCardData();
            if (unit.name != "PyreSteward" || unit.GetCost() != 1 || unit.GetCardType() != CardType.Monster ||
                character?.name != "MonsterPyreSteward" || ability?.GetID() != AbilityId ||
                ability.name != "PyreStewardAbility" || !ability.IsUnitAbility() || ability.GetCost() != 0 ||
                ability.GetEffects() is not [var fixedDamage, var scaled] ||
                fixedDamage.GetEffectStateName() != nameof(CardEffectDamage) || fixedDamage.GetTargetMode() != TargetMode.Pyre ||
                fixedDamage.GetParamInt() != 3 || scaled.GetEffectStateName() != nameof(CardEffectDamagePerPyreAttack) ||
                scaled.GetTargetMode() != TargetMode.Room || scaled.GetTargetTeamType() != Team.Type.Heroes ||
                scaled.GetParamInt() != 1 || scaled.GetAdditionalParamInt() != 0 || scaled.GetUseIntRange())
                throw new InvalidOperationException("Original Pyre Steward or ability authored payload changed.");
            return new
            {
                StewardId, AbilityId, Card = unit.name, Character = character.name, Ability = ability.name,
                UnitCost = unit.GetCost(), Cost = ability.GetCost(), CooldownAtSpawn = ability.GetCooldownAtSpawn(),
                CooldownAfterUse = ability.GetCooldownAfterActivated(),
                Effects = ability.GetEffects().Select(effect => new
                {
                    Type = effect.GetEffectStateName(), Target = effect.GetTargetMode().ToString(),
                    Team = effect.GetTargetTeamType().ToString(), ParamInt = effect.GetParamInt(),
                    AdditionalParamInt = effect.GetAdditionalParamInt(), UsesRange = effect.GetUseIntRange()
                }).ToArray()
            };
        }

        private static JArray BattleRng() => new JArray(new[] { RngId.Battle, RngId.BattleTest }
            .Select(id => JObject.FromObject(new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) })));

        private static object ObservePyre()
        {
            var managers = AllGameManagers.Instance!;
            var save = managers.GetSaveManager();
            var live = managers.GetRoomManager().GetPyreRoom().GetPyreHeart();
            return new { SavedHealth = save.GetTowerHP(), LiveHealth = live.GetHP(), SavePreview = save.PreviewMode,
                LivePreview = live.PreviewMode, DisplayedAttack = save.GetDisplayedPyreAttack() };
        }

        private sealed class ScalingSample
        {
            public int AuthoredPyreAttack { get; set; }
            public int BasePyreAttack { get; set; }
            public int SavedAttackOffset { get; set; }
            public int DisplayedPyreAttack { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public JArray BeforeRng { get; set; } = null!;
            public JArray? AfterRng { get; set; }
            public object? PyreBefore { get; set; }
            public object? PyreAfter { get; set; }
            public bool Completed { get; set; }
            public List<DamageRequest> Requests { get; } = new();
        }

        private sealed class DamageRequest
        {
            public int TargetId { get; set; }
            public string Team { get; set; } = "";
            public int RoomIndex { get; set; }
            public int Amount { get; set; }
            public int HealthBefore { get; set; }
            public int? HealthAfter { get; set; }
            public bool Destroyed { get; set; }
            public bool TargetPreview { get; set; }
            public bool TargetsLivePyre { get; set; }
            public object? PyreBefore { get; set; }
            public object? PyreAfter { get; set; }
        }

        [HarmonyPatch(typeof(CardEffectDamagePerPyreAttack), nameof(CardEffectDamagePerPyreAttack.ApplyEffect))]
        private static class ScalePatch
        {
            private static void Postfix(CardEffectParams cardEffectParams, ICoreGameManagers coreGameManagers, ref IEnumerator __result)
            {
                if (Enabled && abilityCard != null && ReferenceEquals(cardEffectParams.playedCard, abilityCard) &&
                    !coreGameManagers.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectParams, coreGameManagers.GetSaveManager());
            }

            private static IEnumerator Observe(IEnumerator native, CardEffectParams parameters, SaveManager save)
            {
                if (scaling != null) throw new InvalidOperationException("Expected one original scaled damage application.");
                var trace = FullBattleTrace.Active!;
                scaling = new ScalingSample
                {
                    AuthoredPyreAttack = save.GetActiveSavePyreCharacterData().GetPyreHeartData().GetAttack(),
                    BasePyreAttack = save.GetBasePyreAttack(), SavedAttackOffset = save.GetModifiedPyreBaseAttack(),
                    DisplayedPyreAttack = save.GetDisplayedPyreAttack(), Targets = parameters.targets.Select(trace.UnitId).ToArray(),
                    BeforeRng = BattleRng(), PyreBefore = ObservePyre()
                };
                observingScaledEffect = true;
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    scaling.PyreAfter = ObservePyre();
                    scaling.AfterRng = BattleRng();
                    scaling.Completed = true;
                }
                finally { observingScaledEffect = false; (native as IDisposable)?.Dispose(); }
            }
        }

        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ApplyDamageToTarget))]
        private static class DamagePatch
        {
            private static void Postfix(int damage, CharacterState target, CombatManager.ApplyDamageToTargetParameters parameters, ref IEnumerator __result)
            {
                if (Enabled && activation != null && ReferenceEquals(parameters.playedCard, abilityCard) &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, damage, target);
            }

            private static IEnumerator Observe(IEnumerator native, int damage, CharacterState target)
            {
                var request = new DamageRequest
                {
                    TargetId = FullBattleTrace.Active!.UnitId(target), Team = target.GetTeamType().ToString(),
                    RoomIndex = target.GetCurrentRoomIndex(), Amount = damage, HealthBefore = target.GetHP(),
                    TargetPreview = target.PreviewMode,
                    TargetsLivePyre = ReferenceEquals(target, AllGameManagers.Instance!.GetRoomManager().GetPyreRoom().GetPyreHeart()),
                    PyreBefore = ObservePyre()
                };
                bool scaled = observingScaledEffect;
                DamageRequests.Add(new { Scaled = scaled, Damage = request });
                if (scaled) scaling!.Requests.Add(request);
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    request.Destroyed = target.IsDestroyed;
                    request.HealthAfter = request.Destroyed ? null : target.GetHP();
                    request.PyreAfter = ObservePyre();
                }
                finally { (native as IDisposable)?.Dispose(); }
            }
        }
    }
}
#endif
