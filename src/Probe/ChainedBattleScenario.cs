using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class ChainedBattleScenario
    {
        internal static bool RemoteQueue => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "chained-remote-queue";
        internal static string Name => RemoteQueue ? "chained-remote-queue" : "chained-battle";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "chained-battle" || RemoteQueue;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static int remoteActivations, lastRemoteTurn = -1;
        private static object? definition;
        private static ManualLogSource? logger;
        private static string? rejection;
        private static readonly List<Propagation> propagations = new List<Propagation>();
        private static readonly List<Request> requests = new List<Request>();
        private static readonly List<object> timeline = new List<object>();
        internal static object Evidence => new { Prepared, OwnerDefinition = definition, ControlCardIds = cards,
            CompletedControlPlays = step, RemoteQueue, RemoteActivations = remoteActivations,
            Propagations = propagations, Requests = requests, Timeline = timeline };
        private sealed class Propagation
        {
            public bool Preview { get; set; }
            public int Damage { get; set; }
            public int DamageType { get; set; }
            public CombatUnit Before { get; set; } = null!;
            public CombatUnit After { get; set; } = null!;
            public bool Completed { get; set; }
        }
        private sealed class Request
        {
            public bool Preview { get; set; }
            public int Damage { get; set; }
            public int DamageType { get; set; }
            public bool NullCard { get; set; }
            public bool NullRelic { get; set; }
            public bool QueueRunning { get; set; }
            public CombatUnit Before { get; set; } = null!;
            public CombatUnit After { get; set; } = null!;
            public CombatUnit Source { get; set; } = null!;
            public bool Completed { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = managers.GetAllGameData(); var manager = managers.GetCardManager();
            if (!Enum.GetNames(typeof(Damage.Type)).Contains("Chained") || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                save.GetCollectedRelics().Any(relic => relic.GetRelicDataID() == ChainedStatusScenario.OwnerId))
                throw new InvalidOperationException("Chained battle requires isolated 2.3.1 deployment acquisition.");
            var owner = all.GetAllCollectableRelicData().Single(relic => relic.GetID() == ChainedStatusScenario.OwnerId);
            var effect = owner.GetEffects().Single();
            definition = new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                Type = effect.GetEffectClassName(), Team = effect.GetParamSourceTeam().ToString(),
                Statuses = effect.GetParamStatusEffects().Select(status => new { status.statusId, status.count }).ToArray() };
            if (owner.name != "ChainedDamageIncrease" || effect.GetEffectClassName() != RelicModel.AddStatusOnSpawn ||
                effect.GetParamSourceTeam() != Team.Type.Heroes || effect.GetParamStatusEffects().Single().statusId != "chained" ||
                effect.GetParamStatusEffects()[0].count != 1) throw new InvalidOperationException("Original chained owner changed.");
            var steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678"); var unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "health", 40); Set(unit, "attackDamage", 0); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(unit, "unitAbility", null!);
            var triggers = new List<CharacterTriggerData>();
            foreach (var entry in new[] { ("OnBloodshed", 1), ("OnHit", 2), ("OnPunish", 4), ("OnReanimated", 8), ("OnDeath", 16) })
            {
                var trigger = HealingScenario.HealGold(entry.Item2, false, false);
                Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), entry.Item1)); triggers.Add(trigger);
            }
            Set(unit, "triggers", triggers);
            if (RemoteQueue)
            {
                Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "tenacious", count = 1 } });
                var heal = new CardEffectData("CardEffectHeal", null!, Team.Type.Monsters);
                heal.Cheat_SetTargetMode(TargetMode.Self); Set(heal, "paramInt", 3);
                triggers.Add(new CharacterTriggerData((CharacterTriggerData.Trigger)Enum.Parse(typeof(CharacterTriggerData.Trigger), "OnBloodshed"), heal));
            }
            var finisher = UnityEngine.Object.Instantiate(steward); finisher.name = "PojuChainedFinisher";
            Set(finisher, "id", "fc231e20-0000-4000-8000-000000000001");
            var finisherUnit = UnityEngine.Object.Instantiate(unit); finisherUnit.name = "PojuChainedFinisherUnit";
            Set(finisherUnit, "id", "fc231e21-0000-4000-8000-000000000001");
            Set(finisherUnit, "health", 100); Set(finisherUnit, "attackDamage", 60);
            Set(finisherUnit, "triggers", new List<CharacterTriggerData>()); Set(finisher.GetEffects().Single(), "paramCharacterData", finisherUnit);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Add(finisherUnit);
            var cardData = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all); cardData.Add(finisher);
            var template = all.FindCardData("96c9ed37-5e59-4c4f-a914-ae7040d24755");
            if (RemoteQueue)
            {
                var ability = UnityEngine.Object.Instantiate(template); ability.name = "PojuChainedRemoteAbility";
                Set(ability, "id", "fc231e23-0000-4000-8000-000000000001"); Set(ability, "cost", 0);
                Set(ability, "isUnitAbility", true); Set(ability, "isRoomAbility", false); Set(ability, "canAbilityTargetOtherFloors", true);
                Set(ability, "cooldownAtSpawn", 0); Set(ability, "cooldownAfterActivated", 1);
                Set(ability, "targetsRoom", true); Set(ability, "targetless", false);
                var hits = new List<CardEffectData>();
                foreach (int amount in new[] { 5, 99 })
                {
                    var hit = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
                    hit.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(hit, "paramInt", amount); hits.Add(hit);
                }
                Set(ability, "effects", hits); Set(ability, "traits", new List<CardTraitData>());
                Set(ability, "triggers", new List<CardTriggerEffectData>()); Set(ability, "effectTriggers", new List<CharacterTriggerData>());
                Set(ability, "startingUpgrades", new List<CardUpgradeData>()); cardData.Add(ability); Set(finisherUnit, "unitAbility", ability);
            }
            CardData Control(int index, string type, int amount, string? status = null, bool enemy = false)
            {
                var card = UnityEngine.Object.Instantiate(template); card.name = "PojuChainedControl" + index;
                Set(card, "id", "fc231e22-0000-4000-8000-" + index.ToString("D12")); Set(card, "cost", 0);
                Set(card, "targetless", false); Set(card, "targetsRoom", true);
                var action = new CardEffectData(type, null!, enemy ? Team.Type.Heroes : Team.Type.Monsters);
                action.Cheat_SetTargetMode(enemy ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(action, "paramInt", status == null ? amount : 0);
                if (status != null) Set(action, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = amount } });
                Set(card, "effects", new List<CardEffectData> { action }); Set(card, "triggers", new List<CardTriggerEffectData>());
                Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
                cardData.Add(card); return card;
            }
            CardData LethalStatus(int index, string status)
            {
                CardData card = Control(index, "CardEffectAddStatusEffect", 1, status);
                var hit = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
                hit.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(hit, "paramInt", 99); card.GetEffects().Add(hit);
                return card;
            }
            var future = new[] { Control(4, "CardEffectAddStatusEffect", 200, "armor", true),
                Control(5, "CardEffectAddStatusEffect", 8, "armor"), Control(6, "CardEffectDamage", 5),
                Control(7, "CardEffectAddStatusEffect", 1, "damage shield"), Control(8, "CardEffectDamage", 5),
                LethalStatus(9, "tenacious"), Control(10, "CardEffectHeal", 39), LethalStatus(11, "undying"),
                Control(12, "CardEffectDamage", 99) };
            if (RemoteQueue) future = future.Take(1).ToArray();
            foreach (var card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            manager.GetDrawPile().Clear(); save.AddRelic(owner);
            CardState Add(CardData card) => manager.AddNewCard(card, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Unable to create chained battle input.");
            CardState[] controls = future.Reverse().Select(data => {
                CardState created = Add(data); manager.MoveCardFromHandToTopOfDeck(created); return created;
            }).Reverse().ToArray();
            cards = new[] { steward, steward, steward, finisher }.Select(Add).Concat(controls).Select(FullBattleTrace.Active!.CardId).ToArray();
            Set(manager, "drawCountModifier", 5);
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData { trackedValue = CardStatistics.TrackedValueType.TimesPlayed,
                cardState = manager.GetHand()[0], entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active!.InvalidateCardRules(); logger = log; Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("CHAINED-BATTLE-PREPARED unchanged original enemy-spawn owner; armor/shield/Tenacious/Undying/mortal controls, real secondary damage and trigger observations.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (RemoteQueue && step >= cards.Length && remoteActivations < 3 && state.Spawn.Turn > lastRemoteTurn)
            {
                int source = state.Spawn.Train.Rooms[2].Units.Single(unit => unit.SpawnerCardId == cards[3]).Id;
                int victim = state.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == cards[0]).Id;
                var candidate = UnitAbilityModel.EnumerateSupportedActivations(state).FirstOrDefault(action =>
                    action.ActivatorUnitId == source && action.TargetUnitId == victim && action.RoomIndex == 0);
                if (candidate == null) { logger?.LogInfo("CHAINED-REMOTE-REJECT no matching activation"); return null; }
                var predicted = BattleActionModel.PlayCard(state, candidate);
                if (!predicted.Supported) { logger?.LogInfo("CHAINED-REMOTE-REJECT " + predicted.Reason); return null; }
                remoteActivations++; lastRemoteTurn = state.Spawn.Turn; return candidate;
            }
            if (step >= cards.Length || !state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == cards[step])) return null;
            int minTurn = step >= 4 ? 1 : 0;
            if (state.Spawn.Turn < minTurn) return null;
            int target = step >= 5 ? state.Spawn.Train.Rooms[0].Units.FirstOrDefault(unit =>
                unit.SpawnerCardId == cards[step == 12 ? 2 : step == 11 ? 1 : 0])?.Id ?? 0 : 0;
            var action = new PlayCardAction(cards[step], step == 3 ? 2 : 0, playerPosition: step < 4 ? 0 : -1, targetUnitId: target);
            var result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported)
            {
                if (rejection != result.Reason) { rejection = result.Reason; logger?.LogInfo("CHAINED-BATTLE-REJECT " + step + ": " + rejection); }
                return null;
            }
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId) || RemoteQueue && action.ActivatorUnitId > 0;
        private static void Set(object target, string name, object value) => AccessTools.Field(target.GetType(), name).SetValue(target, value);
        private static void Mark(string phase, string kind, CharacterState actor, int damageType = 0) => timeline.Add(new {
            Phase = phase, Kind = kind, UnitId = FullBattleTrace.Active!.UnitId(actor), DamageType = damageType,
            Preview = AllGameManagers.Instance!.GetSaveManager().PreviewMode,
            QueueRunning = AllGameManagers.Instance!.GetCombatManager().IsRunningTriggerQueue });
        [HarmonyPatch]
        private static class PropagationPatch
        {
            private static bool Prepare() => Enabled;
            private static IEnumerable<MethodBase> TargetMethods()
            {
                if (Enabled) yield return AccessTools.Method(typeof(CombatManager), "TriggerChainedOnRoomEnemies") ??
                    throw new InvalidOperationException("Missing native Chained propagation method.");
            }
            private static void Postfix(CharacterState damagedCharacter, int damage, Damage.Type damageType, ref IEnumerator __result)
            { if (Enabled && Prepared) __result = Observe(__result, damagedCharacter, damage, damageType); }
            private static IEnumerator Observe(IEnumerator native, CharacterState source, int damage, Damage.Type type)
            {
                var trace = FullBattleTrace.Active!;
                var row = new Propagation { Preview = AllGameManagers.Instance!.GetSaveManager().PreviewMode,
                    Damage = damage, DamageType = (int)type, Before = trace.CaptureUnit(source) };
                propagations.Add(row); Mark("Enter", "Propagation", source, (int)type);
                try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
                finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureUnit(source); Mark("Exit", "Propagation", source, (int)type); }
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ApplyDamageToTarget))]
        private static class RequestPatch
        {
            private static void Postfix(int damage, CharacterState target, CombatManager.ApplyDamageToTargetParameters parameters, ref IEnumerator __result)
            { if (Enabled && Prepared && (int)parameters.damageType == ChainedStatusModel.DamageType) __result = Observe(__result, damage, target, parameters); }
            private static IEnumerator Observe(IEnumerator native, int damage, CharacterState target, CombatManager.ApplyDamageToTargetParameters parameters)
            {
                var trace = FullBattleTrace.Active!;
                var row = new Request { Preview = AllGameManagers.Instance!.GetSaveManager().PreviewMode,
                    Damage = damage, DamageType = (int)parameters.damageType, NullCard = parameters.playedCard == null,
                    QueueRunning = AllGameManagers.Instance!.GetCombatManager().IsRunningTriggerQueue,
                    NullRelic = parameters.relicState == null, Before = trace.CaptureUnit(target), Source = trace.CaptureUnit(parameters.selfTarget) };
                requests.Add(row); Mark("Enter", "Chained", target, (int)parameters.damageType);
                try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
                finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureUnit(target); Mark("Exit", "Chained", target, (int)parameters.damageType); }
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class TriggerPatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger,
                bool fromRunningTriggerQueue, CharacterState.FireTriggersData fireTriggersData, ref IEnumerator __result)
            {
                if (Enabled && Prepared && fromRunningTriggerQueue && trigger.ToString() is
                    ("OnBloodshed" or "OnHit" or "OnPunish" or "OnReanimated" or "OnDeath"))
                    __result = Observe(__result, __instance, trigger.ToString(), fireTriggersData?.paramInt2 ?? 0);
            }
            private static IEnumerator Observe(IEnumerator native, CharacterState actor, string kind, int type)
            {
                Mark("Enter", kind, actor, type);
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); Mark("Exit", kind, actor, type); }
            }
        }
    }
}
