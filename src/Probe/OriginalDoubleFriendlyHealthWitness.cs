#if DEVELOPMENT_PROBE
using System;
using System.Collections;
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
    internal static class OriginalDoubleFriendlyHealthWitness
    {
        private const string OwnerId = "ca916209-1309-4e3f-985c-f09b87583f93";
        private const string OwnerKey = "Echoes_DoubleFriendlyUnitHealthUnhealed";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private const string FileName = "original-double-friendly-health.mt2f";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_DOUBLE_FRIENDLY_HEALTH_WITNESS") == "1";
        internal static bool FullBattle => Environment.GetEnvironmentVariable("MT2_PROBE_DOUBLE_FRIENDLY_HEALTH_BATTLE") == "1";
        internal static bool Acquired => installed != null;
        private static RelicEffectModifyCharacterMaxHealthPercentage? installed;
        private static object? ownerDefinition;
        private static CardState? steward;
        private static BattleTurnState? before;
        private static BirthSample? birth;

        internal static void Acquire(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the pinned original rank-zero Ring 1 run before combat.");
            var definition = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("Original double-friendly-health mutator was missing.");
            var data = definition.GetEffects().Single();
            if (definition.name != OwnerKey || definition.DisallowInDeploymentPhase ||
                data.GetEffectClassName() != nameof(RelicEffectModifyCharacterMaxHealthPercentage) ||
                data.GetEffectConditions().Count != 0 || data.GetParamSourceTeam() != Team.Type.Monsters ||
                data.GetParamCharacterSubtype()?.IsNone != true || data.GetParamInt() != 100 ||
                data.GetParamTrigger() != CharacterTriggerData.Trigger.OnSpawn || data.GetTargetMode() != TargetMode.Room)
                throw new InvalidOperationException("Original double-friendly-health authored payload changed.");
            save.AddMutator(definition);
            var owner = save.GetMutators().Single();
            if (owner.GetRelicDataID() != OwnerId ||
                owner.GetEffects().Single() is not RelicEffectModifyCharacterMaxHealthPercentage effect ||
                effect.CanApplyInPreviewMode)
                throw new InvalidOperationException("Native double-friendly-health effect did not initialize.");
            installed = effect;
            ownerDefinition = new
            {
                Id = OwnerId, AssetKey = OwnerKey, Type = effect.GetType().Name, EffectIndex = 0,
                Percent = data.GetParamInt(), Team = data.GetParamSourceTeam().ToString(),
                Target = data.GetTargetMode().ToString(), Subtype = data.GetParamCharacterSubtype().Key,
                Trigger = data.GetParamTrigger().ToString(), Conditions = data.GetEffectConditions().Count,
                definition.DisallowInDeploymentPhase, effect.CanApplyInPreviewMode
            };
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            if (!Acquired || save.PreviewMode || !combat.IsPlacementPhase || combat.GetTurnCount() != 0)
                throw new InvalidOperationException("Expected the settled original deployment decision.");
            if (rooms.GetSelectedRoom() != 0)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                return false;
            }
            if (before == null)
            {
                var card = cards.GetHand().FirstOrDefault(item => item.GetCardDataID() == StewardId)
                    ?? throw new InvalidOperationException("No original Train Steward is in the native placement hand.");
                var drop = rooms.GetRoom(0).GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("No native floor-zero placement point.");
                if (!cards.CanPlayHandCard(card, 0, drop, null, null, out var error))
                    throw new InvalidOperationException("Original Train Steward was unavailable: " + error);
                before = trace.CaptureDecision();
                steward = card;
                if (!cards.PlayCard(cards.GetHand().IndexOf(card), drop, ref error))
                    throw new InvalidOperationException("Original Train Steward was rejected: " + error);
                return false;
            }

            var after = trace.CaptureDecision();
            var action = new PlayCardAction(trace.CardId(steward!), 0);
            var actor = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                .Single(unit => unit.SpawnerCardId == action.CardInstanceId);
            var priorCard = before.Spawn.Train.Context!.FindCard(action.CardInstanceId);
            var actualCard = after.Spawn.Train.Context!.FindCard(action.CardInstanceId);
            if (cards.GetHand().Contains(steward!) || birth is not { After: not null } ||
                birth.UnitId != actor.Id || birth.CardId != action.CardInstanceId ||
                birth.Before.Health != 25 || birth.Before.MaxHealth != 25 || birth.Before.TemporaryHealth != 0 ||
                birth.After.Health != 25 || birth.After.MaxHealth != 50 || birth.After.TemporaryHealth != 25 ||
                !JToken.DeepEquals(birth.Before.GameplayRng, birth.After.GameplayRng) ||
                actor.Health != 25 || actor.MaxHealth != 50 || priorCard?.Temporary.Offsets.Health != 0 ||
                actualCard?.Temporary.Offsets.Health != 25)
                throw new InvalidOperationException("Native Train Steward birth did not complete the original 25/25 to 25/50 callback.");
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalDoubleFriendlyUnitHealthUnhealed", GameVersion = Application.version,
                GameModuleMvid = Module, Owner = ownerDefinition, Before = before, Action = action, After = after, Birth = birth
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName));
            document.Write(stream);
            log.LogInfo("ORIGINAL-DOUBLE-FRIENDLY-HEALTH OBSERVED Train Steward 25/25 -> 25/50, source temporary health 0 -> 25");
            return true;
        }

        private sealed class BirthSample
        {
            public int UnitId { get; }
            public int CardId { get; }
            public BirthState Before { get; }
            public BirthState? After { get; set; }
            internal BirthSample(FullBattleTrace trace, CharacterState unit, CardState card)
            {
                UnitId = trace.UnitId(unit);
                CardId = trace.CardId(card);
                Before = new BirthState(unit, card);
            }
        }

        private sealed class BirthState
        {
            public int Health { get; }
            public int MaxHealth { get; }
            public int TemporaryHealth { get; }
            public JToken GameplayRng { get; }
            internal BirthState(CharacterState unit, CardState card)
            {
                Health = unit.GetHP();
                MaxHealth = unit.GetMaxHP();
                TemporaryHealth = CardModifierProbe.Modifiers(card.GetTemporaryCardStateModifiers()).Offsets.Health;
                GameplayRng = new JArray(new[] { RngId.Battle, RngId.BattleTest }
                    .Select(id => JObject.FromObject(new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) })));
            }
        }

        [HarmonyPatch(typeof(RelicEffectModifyCharacterMaxHealthPercentage), nameof(RelicEffectModifyCharacterMaxHealthPercentage.OnCharacterAdded))]
        private static class BirthPatch
        {
            private static void Postfix(RelicEffectModifyCharacterMaxHealthPercentage __instance, CharacterState character,
                CardState? fromCard, ICoreGameManagers coreGameManagers, ref IEnumerator __result)
            {
                if (Enabled && ReferenceEquals(__instance, installed) && fromCard != null &&
                    ReferenceEquals(fromCard, steward) && !coreGameManagers.GetSaveManager().PreviewMode &&
                    !character.PreviewMode)
                    __result = Observe(__result, character, fromCard);
            }

            private static IEnumerator Observe(IEnumerator native, CharacterState unit, CardState card)
            {
                var sample = new BirthSample(FullBattleTrace.Active!, unit, card);
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    sample.After = new BirthState(unit, card);
                    if (birth != null) throw new InvalidOperationException("Expected one original live birth callback.");
                    birth = sample;
                }
                finally { (native as IDisposable)?.Dispose(); }
            }
        }
    }
}
#endif
