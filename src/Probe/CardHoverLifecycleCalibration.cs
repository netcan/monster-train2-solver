using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardHoverLifecycleCalibration
    {
        internal static bool DirectDamage => Environment.GetEnvironmentVariable("MT2_PROBE_DIRECT_DAMAGE_CARD_HOVER") == "1";
        internal static bool Enabled => DirectDamage || Environment.GetEnvironmentVariable("MT2_PROBE_MAXIMUM_HEALTH_LETHAL_PREVIEW") == "1";
        private static bool EnemyTarget => Environment.GetEnvironmentVariable("MT2_PROBE_MAXIMUM_HEALTH_LETHAL_PREVIEW_ENEMY") == "1";
        private sealed class Record
        {
            public string Label = "";
            public bool Horde, Undying, ActiveHalfHealth, ZeroMaximum;
            public bool Lethal;
            public int Armor;
            public int CardId, TargetId, RoomIndex, Amount;
            public CardPlayRule Rule = null!;
            public TrainCombatState Before = null!, After = null!, Cleared = null!;
            public TrainCombatState? CopiedBefore, CopiedActual;
            public CombatUnit? ActualTarget;
            public int[] CopiedIds = Array.Empty<int>();
            public int EnableCount, DisableCount;
            public bool NativeReturned, QueueEmpty, TemporaryDisabled;
            public object RngBefore = null!, RngAfter = null!;
            public CharacterTriggerDispatch[] Dispatches = Array.Empty<CharacterTriggerDispatch>();
        }
        private static Record? active;
        private static CharacterState host = null!;
        private static readonly List<CharacterTriggerDispatch> dispatches = new List<CharacterTriggerDispatch>();
        private static object Get(object owner, string field) => AccessTools.Field(owner.GetType(), field).GetValue(owner);
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        private static TrainCombatState Snapshot(IReadOnlyList<CharacterState>? copied = null)
        {
            var trace = FullBattleTrace.Active!;
            var state = trace.Capture(host.GetCurrentRoom());
            var context = state.Context!;
            if (copied != null) context = context.WithEnchantments(context.Enchantments!.WithTemporaryPreviewUnits(copied.Select(trace.UnitId).ToArray()));
            var world = context.Enchantments!;
            return new TrainCombatState(world.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units, room.ExternalInteractions, context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                world.Movement, world.EnemySlotsPerRoom, context);
        }
        internal static IEnumerator Capture(CharacterState target, ManualLogSource log)
        {
            host = target;
            var managers = AllGameManagers.Instance!; var trace = FullBattleTrace.Active!;
            var cards = managers.GetCardManager(); var combat = managers.GetCombatManager(); var save = managers.GetSaveManager();
            while (save.PreviewMode || managers.GetReplayManager().IsCardPlaying() || combat.IsRunningTriggerQueue ||
                managers.GetHandUI().AreAnyCardsAnimating() || !CardAnimationSettlement.Ready()) yield return null;
            var all = managers.GetAllGameData();
            var actors = new List<CharacterState>();
            for (int room = 0; room < managers.GetRoomManager().GetNumRooms(); room++)
            {
                var floor = new List<CharacterState>(); managers.GetRoomManager().GetRoom(room).AddCharactersToList(floor, Team.Type.Heroes | Team.Type.Monsters);
                actors.AddRange(floor);
            }
            actors = actors.Distinct().ToList();
            if (EnemyTarget)
                host = actors.FirstOrDefault(actor => actor.GetTeamType() == Team.Type.Heroes &&
                    actor.GetCurrentRoomIndex() == target.GetCurrentRoomIndex() && actor.GetSourceCharacterData().GetHealth() >= 2)
                    ?? throw new InvalidOperationException("Enemy lethal hover requires an original enemy beside the deployed Steward.");
            var stateType = AccessTools.Inner(typeof(CharacterState), "StateInformation");
            object Copy(object state) => AccessTools.Method(stateType, "DeepCopyForPreview").Invoke(state, null);
            var primary = actors.Select(actor => Get(actor, "_primaryStateInformation")).ToArray();
            var triggers = actors.Select(actor => Get(actor, "triggers")).ToArray();
            bool[] enchanters = actors.Select(actor => actor.IsEnchanter).ToArray();
            int baseHealth = host.GetSourceCharacterData().GetHealth();
            if (baseHealth < 2 || actors.Any(actor => actor.PreviewMode || actor.TemporaryPreviewsEnabled))
                throw new InvalidOperationException("Lethal hover requires independent original primary actors and positive Horde base health.");
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            var controls = new Dictionary<int, CardState>();
            int DamageAmount(bool horde, bool half, bool final) => DirectDamage
                ? final ? baseHealth * (horde ? 3 : 1) + 3 : Math.Max(1, baseHealth / 3)
                : final ? baseHealth * (horde ? 3 : 1) : (baseHealth * (horde ? 3 : 1)) / 2 + (half ? 0 : 1);
            foreach (bool horde in new[] { false, true })
            foreach (bool half in new[] { false, true })
            foreach (bool zero in new[] { false, true })
            {
                int amount = DamageAmount(horde, half, zero);
                if (controls.ContainsKey(amount)) continue;
                var data = UnityEngine.Object.Instantiate(all.FindCardData(DevotionSpawnBattleScenario.VenerateId));
                data.name = (DirectDamage ? "PojuDamageHover" : "PojuMaximumLethalHover") + amount;
                Set(data, "id", (DirectDamage ? "fc231b64" : "fc231b63") + "-0000-4000-8000-" + amount.ToString("D12")); Set(data, "cost", 2);
                var effect = new CardEffectData(DirectDamage ? "CardEffectDamage" : "CardEffectDebuffMaxHealth", null!, host.GetTeamType()); effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", amount); Set(data, "effects", new List<CardEffectData> { effect });
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", false); Set(data, "targetsRoom", true);
                ((List<CardData>)Get(all, "cardDatas")).Add(data);
                var card = cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
                controls.Add(amount, card); trace.CardId(card); cards.RefreshCardInHand(card);
            }
            trace.InvalidateCardRules();
            bool suppression = CombatPreviewControl.Save(combat);
            int originalSuppression = (int)Get(combat, "combatPreviewSuppressionCount");
            CombatPreviewControl.Set(combat, true);
            var samples = new List<Record>();
            try
            {
                foreach (bool horde in new[] { false, true })
                foreach (bool undying in new[] { false, true })
                foreach (bool half in new[] { false, true })
                foreach (bool zero in new[] { false, true })
                foreach (int armor in DirectDamage ? new[] { 0, 3 } : new[] { 0 })
                {
                    for (int index = 0; index < actors.Count; index++)
                    {
                        Set(actors[index], "_primaryStateInformation", Copy(primary[index]));
                        Set(actors[index], "triggers", new List<CharacterTriggerState>());
                        AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actors[index], false);
                        var statuses = (Dictionary<string, CharacterState.StatusEffectStack>)Get(Get(actors[index], "_primaryStateInformation"), "statusEffects");
                        foreach (var status in statuses.Values) status.Count = 0;
                    }
                    void AddTrigger(CharacterState actor, string kind, int gold)
                    {
                        var data = HealingScenario.HealGold(gold, false, false); Set(data, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind));
                        var trigger = new CharacterTriggerState(); trigger.Setup(data, actor.GetTeamType(), save); actor.GetTriggers().Add(trigger);
                    }
                    AddTrigger(host, "OnDeathwish", 5); AddTrigger(host, "OnDeathwishLost", 7);
                    AddTrigger(host, "OnReanimated", 11); AddTrigger(host, "OnDeath", 13);
                    foreach (var witness in actors.Where(actor => actor != host && actor.GetCurrentRoomIndex() == host.GetCurrentRoomIndex()))
                        AddTrigger(witness, "OnAnyUnitDeathOnFloor", 2);
                    var hostStatuses = (Dictionary<string, CharacterState.StatusEffectStack>)Get(Get(host, "_primaryStateInformation"), "statusEffects");
                    foreach (var pair in new[] { (Id: "horde", Count: horde ? 2 : 0), (Id: "undying", Count: undying ? 1 : 0), (Id: "armor", Count: armor) })
                    {
                        if (hostStatuses.TryGetValue(pair.Id, out var status)) status.Count = pair.Count;
                        else hostStatuses.Add(pair.Id, new CharacterState.StatusEffectStack(managers.GetStatusEffectManager().Create(pair.Id, host), pair.Count));
                    }
                    int maximum = baseHealth * (horde ? 3 : 1), health = half ? maximum / 2 : maximum / 2 + 1;
                    host.SetHealth(health, maximum);
                    AccessTools.Method(typeof(CharacterState), "TryUpdateDeathwish").Invoke(host, null);
                    var queue = (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat); queue.Clear();
                    yield return managers.GetRoomManager().GetRoomUI().SetSelectedRoom(host.GetCurrentRoomIndex());
                    int amount = DamageAmount(horde, half, zero);
                    var card = controls[amount];
                    active = new Record { Label = (horde ? "horde" : "plain") + (undying ? "-undying" : "") +
                        (half ? "-active" : "-inactive") + (DirectDamage ? (zero ? "-lethal" : "-wounded") + "-armor" + armor : zero ? "-zero" : "-retained"), Horde = horde, Undying = undying,
                        ActiveHalfHealth = half, ZeroMaximum = !DirectDamage && zero, Lethal = !DirectDamage || zero, Armor = armor, Amount = amount,
                        CardId = trace.CardId(card), TargetId = trace.UnitId(host), RoomIndex = host.GetCurrentRoomIndex(),
                        Rule = BattleActionProbe.Definition(all.FindCardData(card.GetCardDataID())!) };
                    active.Before = Snapshot(); active.RngBefore = HalfHealthLifecycleCalibration.Rng(); dispatches.Clear();
                    log.LogInfo((DirectDamage ? "DIRECT-DAMAGE-HOVER" : "MAXIMUM-LETHAL-HOVER") + " sample=" + samples.Count + " label=" + active.Label);
                    // CommonSelectionBehavior passes the ability activator as selfTarget;
                    // ordinary hand spells have no activator. The drop point selects the target.
                    combat.PreviewCardPlay(true, card, host.GetCurrentRoom(), null!, host.GetSpawnPoint(), null!, null!);
                    active.NativeReturned = true; active.After = Snapshot();
                    combat.PreviewCardPlay(false, card, host.GetCurrentRoom(), null!, host.GetSpawnPoint(), null!, null!);
                    active.Cleared = Snapshot(); active.RngAfter = HalfHealthLifecycleCalibration.Rng(); active.Dispatches = dispatches.ToArray();
                    active.QueueEmpty = queue.Count == 0;
                    active.TemporaryDisabled = !save.PreviewMode && actors.All(actor => !actor.PreviewMode && !actor.TemporaryPreviewsEnabled);
                    samples.Add(active);
                    if (active.EnableCount != 1 || active.DisableCount != 1 || active.CopiedBefore == null || active.CopiedActual == null ||
                        active.ActualTarget == null || !active.QueueEmpty || !active.TemporaryDisabled || actors.Any(actor => actor.IsDestroyed || !actor.IsAlive))
                        throw new InvalidOperationException("Lethal hover did not restore independent living primary actors.");
                    active = null;
                }
            }
            finally
            {
                active = null;
                for (int index = 0; index < actors.Count; index++)
                {
                    Set(actors[index], "_primaryStateInformation", primary[index]); Set(actors[index], "triggers", triggers[index]);
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actors[index], enchanters[index]);
                }
                CombatPreviewControl.Set(combat, suppression);
            }
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = DirectDamage ? "NativeDirectDamageCardHover" : "NativeMaximumHealthLethalCardHover",
                NativeCompleted = true, AuthoredControls = true, TargetTeam = EnemyTarget ? "Enemy" : "Player", SelfTargetId = 0, BaseHealth = baseHealth, Samples = samples,
                OriginalSuppression = originalSuppression, FinalSuppression = (int)Get(combat, "combatPreviewSuppressionCount") }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, DirectDamage ? "damage-card-preview.mt2f" : "maximum-health-lethal-preview.mt2f"))) document.Write(output);
        }
        [HarmonyPatch(typeof(CombatManager), "SetTemporaryStateEnabled")]
        private static class CopyPatch
        {
            private static void Prefix(bool enable, List<CharacterState> characters, bool isCardPreview)
            {
                if (active == null || !isCardPreview || enable) return;
                active.CopiedActual = Snapshot(characters); active.ActualTarget = FullBattleTrace.Active!.CaptureUnit(host); active.DisableCount++;
            }
            private static void Postfix(bool enable, List<CharacterState> characters, bool isCardPreview)
            {
                if (active == null || !isCardPreview || !enable) return;
                active.CopiedBefore = Snapshot(characters); active.CopiedIds = characters.Select(FullBattleTrace.Active!.UnitId).ToArray(); active.EnableCount++;
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class DispatchPatch
        {
            private static void Prefix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                bool fromRunningTriggerQueue, CharacterState.FireTriggersData fireTriggersData, int triggerCount, CharacterTriggerState exclusiveTrigger)
            {
                if (active == null || !fromRunningTriggerQueue) return;
                var trace = FullBattleTrace.Active!;
                dispatches.Add(new CharacterTriggerDispatch(trace.UnitId(__instance), trigger.ToString(), dyingCharacter == null ? 0 : trace.UnitId(dyingCharacter),
                    triggerCount, fireTriggersData?.paramInt ?? 0, fireTriggersData?.paramInt2 ?? 0, fireTriggersData?.paramString,
                    fireTriggersData?.overrideTargetCharacter == null ? 0 : trace.UnitId(fireTriggersData.overrideTargetCharacter),
                    exclusiveTriggerStateId: exclusiveTrigger == null ? (int?)null : trace.TriggerStateId(__instance, exclusiveTrigger)));
            }
        }
    }
}
