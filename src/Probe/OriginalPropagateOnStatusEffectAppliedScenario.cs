using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPropagateOnStatusEffectAppliedScenario
    {
        internal const string Name = "original-propagate-on-status-effect-applied";
        internal const string OwnerId = "274bc27a-9be2-4908-aa84-641944188c08";
        internal const string OwnerKey = "PropagateOnStatusEffectApplied";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

        internal sealed class TestCall
        {
            public int ActorId { get; set; }
            public string ActorAssetKey { get; set; } = "";
            public string Team { get; set; } = "";
            public int ParamInt { get; set; }
            public bool Result { get; set; }
        }

        internal sealed class StatusValue
        {
            public string Id { get; set; } = "";
            public int Stacks { get; set; }
        }

        internal sealed class Application
        {
            internal CharacterState? NativeActor { get; set; }
            public int ActorId { get; set; }
            public string ActorAssetKey { get; set; } = "";
            public string Team { get; set; } = "";
            public int ParamInt { get; set; }
            public StatusValue[] Before { get; set; } = Array.Empty<StatusValue>();
            public StatusValue[] After { get; set; } = Array.Empty<StatusValue>();
            public bool Completed { get; set; }
        }

        private static readonly List<TestCall> testCalls = new List<TestCall>();
        private static readonly List<Application> applications = new List<Application>();
        private static object? definition;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int IncompleteApplications => applications.Count(record => !record.Completed || record.After.Length == 0);
        internal static object Evidence => new { Definition = definition, TestCalls = testCalls, Applications = applications };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Status-added relic probe must be prepared before battle setup.");
            CollectableRelicData data = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = data.GetEffects().Single();
            var collectedRelics = save.GetCollectedRelics();
            if (data.name != OwnerKey || effect.GetEffectClassName() != "RelicEffectAddEffectOnStatusAdded" ||
                collectedRelics?.Any(item => item.GetRelicDataID() == OwnerId) == true)
                throw new InvalidOperationException("Original PropagateOnStatusEffectApplied owner or effect changed.");

            save.AddRelic(data);
            Acquired = true;
            log.LogInfo("ORIGINAL-PROPAGATE-ON-STATUS-EFFECT-APPLIED-ACQUIRED unchanged=" + OwnerKey + ".");
        }

        internal static void PrepareCarrier(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired) throw new InvalidOperationException("Status-added relic was not acquired before battle setup.");
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            if (save.PreviewMode || save.GetGameSequence() != SaveData.GameSequence.InBattle ||
                combat.GetTurnCount() != 0 || !combat.IsPlacementPhase)
                throw new InvalidOperationException("Status-added relic carrier must be prepared on the native deployment phase.");

            CardManager cards = managers.GetCardManager() ?? throw new InvalidOperationException("CardManager is unavailable during deployment preparation.");
            CardState[] stewards = cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == StewardId).ToArray();
            if (stewards.Length == 0) throw new InvalidOperationException("Status-added relic probe needs an owned Train Steward.");
            CardData stewardData = save.GetAllGameData().FindCardData(StewardId)!;
            CharacterData character = stewardData.GetEffects().Single(item => item.GetEffectStateName() == "CardEffectSpawnMonster")
                .GetParamCharacterData();
            StatusEffectStackData[] statusSequence =
            {
                new StatusEffectStackData { statusId = "valor", count = 1 },
                new StatusEffectStackData { statusId = "spikes", count = 1 },
                new StatusEffectStackData { statusId = "regen", count = 1 },
                new StatusEffectStackData { statusId = "damage shield", count = 1 }
            };
            Set(character, "startingStatusEffects", statusSequence);
            Set(stewardData, "cost", 0);
            foreach (CardState card in stewards) card.Setup(stewardData, save);

            CollectableRelicData data = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = data.GetEffects().Single();
            CombatRelicState owner = RelicProbe.Capture(managers).Single(item => item.DataId == OwnerId);
            RelicStatusAddedEffect payload = owner.TriggerAdditions!.Single().StatusAddedEffect!;
            Team.Type sourceTeam = effect.GetParamSourceTeam();
            CardEffectData action = effect.GetParamCardEffectData().Single();
            definition = new
            {
                OwnerId,
                AssetKey = data.name,
                EffectType = effect.GetEffectClassName(),
                IsCovenant = owner.IsCovenant,
                DisallowedInPlacementPhase = owner.DisallowedInPlacementPhase,
                SourceTeam = (int)sourceTeam,
                RequiredSubtype = effect.GetParamCharacterSubtype()?.Key,
                RequiredStatusCount = payload.UniqueRequiredStatusCount,
                ExactCount = payload.TriggerAtExactCount,
                ConditionCount = effect.GetEffectConditions().Count,
                OuterTargetMode = effect.GetTargetMode().ToString(),
                CanApplyInPreviewMode = payload.CanApplyInPreviewMode,
                CardEffectType = action.GetEffectStateName(),
                TargetMode = action.GetTargetMode().ToString(),
                TargetTeam = action.GetTargetTeamType().ToString(),
                Adjustment = action.GetParamInt(),
                StatusFilter = action.GetAdditionalParamInt(),
                RequireValidTargets = action.GetParamBool(),
                StatusIds = action.GetParamStatusEffects().Select(status => status.statusId).ToArray(),
                StewardStatusSequence = statusSequence.Select(status => status.statusId).ToArray()
            };
            testCalls.Clear(); applications.Clear();
            Prepared = true;
            FullBattleTrace.Active?.InvalidateCardRules();
            log.LogInfo("ORIGINAL-PROPAGATE-ON-STATUS-EFFECT-APPLIED-PREPARED unchanged=" + OwnerKey +
                " threshold=3 exact=true stewardStatuses=valor,spikes,regen,damage shield.");
        }

        private static StatusValue[] Snapshot(CharacterState character)
        {
            var statuses = new List<CharacterState.StatusEffectStack>();
            character.GetStatusEffects(ref statuses);
            return statuses.Select(status => new StatusValue
                { Id = status.State.GetStatusId(), Stacks = status.Count }).OrderBy(status => status.Id, StringComparer.Ordinal).ToArray();
        }

        private static bool IsOwner(RelicEffectBase effect)
        {
            var relic = (RelicState?)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicState").GetValue(effect);
            return relic?.GetRelicDataID() == OwnerId;
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);

        [HarmonyPatch(typeof(RelicEffectAddEffectOnStatusAdded), nameof(RelicEffectAddEffectOnStatusAdded.TestCharacterTriggerEffect))]
        private static class TestPatch
        {
            private static void Postfix(RelicEffectAddEffectOnStatusAdded __instance,
                CharacterTriggerRelicEffectParams relicEffectParams, bool __result)
            {
                if (!Enabled || !Acquired || FullBattleTrace.Active == null || !IsOwner(__instance) ||
                    relicEffectParams.trigger != CharacterTriggerData.Trigger.OnNewStatusEffectAdded ||
                    relicEffectParams.characterState == null) return;
                CharacterState actor = relicEffectParams.characterState;
                testCalls.Add(new TestCall
                {
                    ActorId = FullBattleTrace.Active.UnitId(actor),
                    ActorAssetKey = actor.GetSourceCharacterData().GetAssetKey(),
                    Team = actor.GetTeamType().ToString(),
                    ParamInt = relicEffectParams.paramInt,
                    Result = __result
                });
            }
        }

        [HarmonyPatch(typeof(RelicEffectAddEffectOnStatusAdded), nameof(RelicEffectAddEffectOnStatusAdded.ApplyCharacterTriggerEffect))]
        private static class ApplyPatch
        {
            private static void Prefix(RelicEffectAddEffectOnStatusAdded __instance,
                CharacterTriggerRelicEffectParams relicEffectParams, out Application? __state)
            {
                __state = null;
                if (!Enabled || !Acquired || FullBattleTrace.Active == null || !IsOwner(__instance) ||
                    relicEffectParams.characterState == null) return;
                CharacterState actor = relicEffectParams.characterState;
                __state = new Application
                {
                    ActorId = FullBattleTrace.Active.UnitId(actor),
                    ActorAssetKey = actor.GetSourceCharacterData().GetAssetKey(),
                    Team = actor.GetTeamType().ToString(),
                    ParamInt = relicEffectParams.paramInt,
                    Before = Snapshot(actor),
                    NativeActor = actor
                };
            }

            private static void Postfix(ref IEnumerator __result, Application? __state)
            {
                if (__state != null) __result = Wrap(__result, __state);
            }
        }

        private static IEnumerator Wrap(IEnumerator native, Application record)
        {
            try
            {
                while (native.MoveNext()) yield return native.Current;
                record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                CharacterState? actor = record.NativeActor;
                if (actor != null) record.After = Snapshot(actor);
                applications.Add(record);
            }
        }
    }
}
