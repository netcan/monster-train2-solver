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
        private static bool CompositeDamage => DirectDamage && Environment.GetEnvironmentVariable("MT2_PROBE_COMPOSITE_DAMAGE_CARD_HOVER") == "1";
        private static bool RandomDamage => DirectDamage && Environment.GetEnvironmentVariable("MT2_PROBE_RANDOM_DAMAGE_CARD_HOVER") == "1";
        private static bool HealingDamage => DirectDamage && Environment.GetEnvironmentVariable("MT2_PROBE_HEALING_DAMAGE_CARD_HOVER") == "1";
        private static bool GroupedDamage => DirectDamage && Environment.GetEnvironmentVariable("MT2_PROBE_ONCAST_GROUPS_CARD_HOVER") == "1";
        private static bool CompositionBoundaries => DirectDamage && Environment.GetEnvironmentVariable("MT2_PROBE_DAMAGE_CARD_BOUNDARIES") == "1";
        private static bool MixedDamage => DirectDamage && (GroupedDamage || HealingDamage || CompositionBoundaries || Environment.GetEnvironmentVariable("MT2_PROBE_DAMAGE_CARD_COMPOSITIONS") == "1");
        private static bool BatchedControls => RandomDamage || MixedDamage;
        internal static bool Enabled => DirectDamage || Environment.GetEnvironmentVariable("MT2_PROBE_MAXIMUM_HEALTH_LETHAL_PREVIEW") == "1";
        private static bool EnemyTarget => Environment.GetEnvironmentVariable("MT2_PROBE_MAXIMUM_HEALTH_LETHAL_PREVIEW_ENEMY") == "1";
        private sealed class Record
        {
            public string Label = "";
            public bool Horde, Undying, ActiveHalfHealth, ZeroMaximum;
            public bool Lethal;
            public bool RoomEffect, OnCastDamage;
            public int Armor;
            public int CardId, TargetId, DropTargetId, RoomIndex, Amount;
            public int RangeCase, Composition;
            public List<RangeDraw> RangeDraws = new List<RangeDraw>();
            public CardPlayRule Rule = null!;
            public TrainCombatState Before = null!, After = null!, Cleared = null!;
            public TrainCombatState? CopiedBefore, CopiedActual;
            public CombatUnit? ActualTarget;
            public CombatUnit[] ActualTargets = Array.Empty<CombatUnit>();
            public int[] CopiedIds = Array.Empty<int>();
            public int EnableCount, DisableCount;
            public bool NativeReturned, QueueEmpty, TemporaryDisabled;
            public object RngBefore = null!, RngAfter = null!;
            public CharacterTriggerDispatch[] Dispatches = Array.Empty<CharacterTriggerDispatch>();
        }
        internal sealed class RangeDraw
        {
            public int Min, Max, Value;
            public int EffectIndex;
            public string EffectType = "";
            public float Multiplier;
            public bool Preview, Test;
            public UnityRng Before, After;
        }
        private static UnityRng TestRng()
        {
            var words = RngCalibration.Words(RandomManager.GetState(RngId.BattleTest));
            return new UnityRng(words[0], words[1], words[2], words[3]);
        }
        private static Record? active;
        private static CardEffectData[] activeEffects = Array.Empty<CardEffectData>();
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
            if (HealingDamage) HealingScenario.RegisterHealingStatus("heal immunity", "StatusEffectHealImmunityState", 0);
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
            int[] modes = GroupedDamage ? Enumerable.Range(23, 9).ToArray() : HealingDamage ? Enumerable.Range(13, 10).ToArray() : CompositionBoundaries ? new[] { 10, 11, 12 } : MixedDamage ? new[] { 4, 5, 6, 7, 8, 9 } : RandomDamage ? new[] { 0, 1, 2, 3 } : CompositeDamage ? new[] { 1, 2, 3 } : new[] { 0 };
            int[] rangeCases = MixedDamage ? new[] { 0, 1 } : RandomDamage ? new[] { 1, 2, 3, 4 } : new[] { 0 };
            bool RoomMode(int mode) => mode is 1 or 3 or 5 or 7 or 9 or 12 or 14 or 18 or 19 or 22 or 28 or 31;
            bool OnCastMode(int mode) => GroupedDamage || (HealingDamage ? mode is 17 or 18 or 20 or 21 or 22 : mode >= 2 && mode != 6 && mode != 7 && mode != 10);
            bool OnCastRoomMode(int mode) => HealingDamage ? mode == 17 : MixedDamage ? mode == 4 || mode == 9 : RoomMode(mode);
            bool NeedsDrop(int mode) => MixedDamage || !RoomMode(mode);
            var controls = new Dictionary<(int Amount, int Mode, int RangeCase), CardState>();
            int DamageAmount(bool horde, bool half, bool final) => DirectDamage
                ? final ? baseHealth * (horde ? 3 : 1) + 3 : Math.Max(1, baseHealth / 3)
                : final ? baseHealth * (horde ? 3 : 1) : (baseHealth * (horde ? 3 : 1)) / 2 + (half ? 0 : 1);
            foreach (bool horde in new[] { false, true })
            foreach (bool half in new[] { false, true })
            foreach (bool zero in new[] { false, true })
            foreach (int mode in modes)
            foreach (int rangeCase in rangeCases)
            {
                int amount = DamageAmount(horde, half, zero);
                if (controls.ContainsKey((amount, mode, rangeCase))) continue;
                if (GroupedDamage)
                {
                    var groupedData = OnCastGroupControls.Create(all, mode, amount, rangeCase, host.GetTeamType());
                    ((List<CardData>)Get(all, "cardDatas")).Add(groupedData);
                    var groupedCard = cards.AddNewCard(groupedData, CardPile.DeckPile, false, false, animate: false);
                    controls.Add((amount, mode, rangeCase), groupedCard); trace.CardId(groupedCard); cards.GetDrawPile().Remove(groupedCard);
                    continue;
                }
                var data = UnityEngine.Object.Instantiate(all.FindCardData(DevotionSpawnBattleScenario.VenerateId));
                data.name = (DirectDamage ? "PojuDamageHover" : "PojuMaximumLethalHover") + mode + "_" + amount;
                Set(data, "id", (HealingDamage ? "fc231b69" : CompositionBoundaries ? "fc231b68" : MixedDamage ? "fc231b67" : RandomDamage ? "fc231b66" : CompositeDamage ? "fc231b65" : DirectDamage ? "fc231b64" : "fc231b63") + "-0000-4000-8000-" + (rangeCase * 100000000 + mode * 1000000 + amount).ToString("D12")); Set(data, "cost", 2);
                CardEffectData Effect(int value, bool? roomOverride = null, bool bothTeams = false, bool healing = false)
                {
                    bool roomTarget = roomOverride ?? RoomMode(mode);
                    var effect = new CardEffectData(healing ? "CardEffectHeal" : DirectDamage ? "CardEffectDamage" : "CardEffectDebuffMaxHealth", null!,
                        roomTarget || bothTeams ? Team.Type.Monsters | Team.Type.Heroes : host.GetTeamType());
                    effect.Cheat_SetTargetMode(roomTarget ? TargetMode.Room : TargetMode.DropTargetCharacter);
                    Set(effect, "paramInt", value);
                    if (rangeCase != 0)
                    {
                        Set(effect, "useIntRange", true);
                        Set(effect, "paramMinInt", rangeCase == 1 ? 0 : rangeCase == 2 ? value : -value);
                        Set(effect, "paramMaxInt", rangeCase == 1 ? value + 7 : rangeCase == 2 ? value : rangeCase == 3 ? 2 : 0);
                        Set(effect, "paramMultiplier", rangeCase == 1 ? .75f : rangeCase == 2 ? 1f : rangeCase == 3 ? -.5f : .5f);
                    }
                    return effect;
                }
                var mainEffects = new List<CardEffectData> { Effect(MixedDamage || OnCastMode(mode) ? 1 : amount) };
                if (HealingDamage)
                {
                    mainEffects[0] = Effect(mode is 13 or 14 or 16 or 20 or 22 ? amount : mode == 21 ? 0 : 1,
                        healing: mode is 13 or 14 or 16 or 18 or 21 or 22);
                    if (mode is 15 or 19) mainEffects.Add(Effect(amount, false, healing: true));
                    if (mode == 16) mainEffects.Add(Effect(1, true));
                }
                else if (MixedDamage && mode >= 6 && mode <= 11) mainEffects.Add(Effect(mode <= 7 || mode == 10 ? amount : 1, !RoomMode(mode)));
                if (mode is 10 or 11)
                {
                    Set(mainEffects[0], "useIntRange", true); Set(mainEffects[0], "paramMinInt", 0); Set(mainEffects[0], "paramMaxInt", 0);
                    Set(mainEffects[0], "paramMultiplier", 1f); Set(mainEffects[0], "shouldCancelSubsequentEffectsIfTestFails", true);
                }
                Set(data, "effects", mainEffects);
                var cardTriggers = new List<CardTriggerEffectData>();
                if (OnCastMode(mode))
                {
                    var trigger = new CardTriggerEffectData(); Set(trigger, "trigger", CardTriggerType.OnCast);
                    Set(trigger, "cardEffects", new List<CardEffectData> { Effect(mode == 22 ? 1 : MixedDamage ? amount : amount - 1,
                        OnCastRoomMode(mode), mode == 12, healing: HealingDamage && mode is 17 or 20 or 21) }); cardTriggers.Add(trigger);
                }
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", cardTriggers);
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", !NeedsDrop(mode)); Set(data, "targetsRoom", true);
                ((List<CardData>)Get(all, "cardDatas")).Add(data);
                var card = cards.AddNewCard(data, BatchedControls ? CardPile.DeckPile : CardPile.HandPile, false, false, animate: false)
                    ?? throw new InvalidOperationException("Could not create isolated card-hover control.");
                controls.Add((amount, mode, rangeCase), card); trace.CardId(card);
                if (BatchedControls) cards.GetDrawPile().Remove(card);
                else cards.RefreshCardInHand(card);
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
                foreach (int mode in modes)
                foreach (int rangeCase in rangeCases)
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
                    if (HealingDamage || GroupedDamage)
                    {
                        Set(Get(host, "_primaryStateInformation"), "canBeHealed", true);
                        AddTrigger(host, "OnHeal", 17);
                    }
                    foreach (var witness in actors.Where(actor => actor != host && actor.GetCurrentRoomIndex() == host.GetCurrentRoomIndex()))
                    {
                        if (mode == 12 || GroupedDamage) witness.SetHealth(10, 10);
                        AddTrigger(witness, "OnAnyUnitDeathOnFloor", 2);
                    }
                    var hostStatuses = (Dictionary<string, CharacterState.StatusEffectStack>)Get(Get(host, "_primaryStateInformation"), "statusEffects");
                    foreach (var pair in new[] { (Id: "horde", Count: horde ? 2 : 0), (Id: "undying", Count: undying ? 1 : 0), (Id: "armor", Count: armor) })
                    {
                        if (hostStatuses.TryGetValue(pair.Id, out var status)) status.Count = pair.Count;
                        else hostStatuses.Add(pair.Id, new CharacterState.StatusEffectStack(managers.GetStatusEffectManager().Create(pair.Id, host), pair.Count));
                    }
                    if (HealingDamage && mode == 22)
                    {
                        if (hostStatuses.TryGetValue("heal immunity", out var immunity)) immunity.Count = 1;
                        else hostStatuses.Add("heal immunity", new CharacterState.StatusEffectStack(managers.GetStatusEffectManager().Create("heal immunity", host), 1));
                    }
                    int maximum = baseHealth * (horde ? 3 : 1), health = half ? maximum / 2 : maximum / 2 + 1;
                    host.SetHealth(health, maximum);
                    AccessTools.Method(typeof(CharacterState), "TryUpdateDeathwish").Invoke(host, null);
                    var queue = (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat); queue.Clear();
                    yield return managers.GetRoomManager().GetRoomUI().SetSelectedRoom(host.GetCurrentRoomIndex());
                    int amount = DamageAmount(horde, half, zero);
                    var card = controls[(amount, mode, rangeCase)];
                    var cardData = all.FindCardData(card.GetCardDataID())!;
                    activeEffects = cardData.GetEffects().Concat(cardData.GetCardTriggers()
                        .Where(trigger => trigger.GetTrigger() == CardTriggerType.OnCast).SelectMany(trigger => trigger.GetCardEffects())).ToArray();
                    // Author a single held control without drawing or spending RNG.
                    // The native hover below receives the ordinary hand-spell input.
                    if (BatchedControls) cards.GetHand().Add(card);
                    active = new Record { Label = (horde ? "horde" : "plain") + (undying ? "-undying" : "") +
                        (half ? "-active" : "-inactive") + (DirectDamage ? (zero ? "-lethal" : "-wounded") + "-armor" + armor : zero ? "-zero" : "-retained") +
                        (CompositeDamage || BatchedControls ? (RoomMode(mode) ? "-room" : "-target") + (OnCastMode(mode) ? "-oncast" : "-main") : "") +
                        (BatchedControls ? "-mode" + mode + "-range" + rangeCase : ""), Horde = horde, Undying = undying, RangeCase = rangeCase, Composition = mode,
                        ActiveHalfHealth = half, ZeroMaximum = !DirectDamage && zero, Lethal = !DirectDamage || zero, Armor = armor, Amount = amount,
                        RoomEffect = RoomMode(mode), OnCastDamage = OnCastMode(mode), DropTargetId = NeedsDrop(mode) ? trace.UnitId(host) : 0,
                        CardId = trace.CardId(card), TargetId = trace.UnitId(host), RoomIndex = host.GetCurrentRoomIndex(),
                        Rule = BattleActionProbe.Definition(all.FindCardData(card.GetCardDataID())!) };
                    active.Before = Snapshot(); active.RngBefore = HalfHealthLifecycleCalibration.Rng(); dispatches.Clear();
                    log.LogInfo((DirectDamage ? "DIRECT-DAMAGE-HOVER" : "MAXIMUM-LETHAL-HOVER") + " sample=" + samples.Count + " label=" + active.Label);
                    // CommonSelectionBehavior passes the ability activator as selfTarget;
                    // ordinary hand spells have no activator. The drop point selects the target.
                    combat.PreviewCardPlay(true, card, host.GetCurrentRoom(), null!, NeedsDrop(mode) ? host.GetSpawnPoint() : null!, null!, null!);
                    active.NativeReturned = true; active.After = Snapshot();
                    combat.PreviewCardPlay(false, card, host.GetCurrentRoom(), null!, NeedsDrop(mode) ? host.GetSpawnPoint() : null!, null!, null!);
                    active.Cleared = Snapshot(); active.RngAfter = HalfHealthLifecycleCalibration.Rng(); active.Dispatches = dispatches.ToArray();
                    active.QueueEmpty = queue.Count == 0;
                    active.TemporaryDisabled = !save.PreviewMode && actors.All(actor => !actor.PreviewMode && !actor.TemporaryPreviewsEnabled);
                    samples.Add(active);
                    if (active.EnableCount != 1 || active.DisableCount != 1 || active.CopiedBefore == null || active.CopiedActual == null ||
                        active.ActualTarget == null || !active.QueueEmpty || !active.TemporaryDisabled || actors.Any(actor => actor.IsDestroyed || !actor.IsAlive))
                        throw new InvalidOperationException("Lethal hover did not restore independent living primary actors.");
                    active = null;
                    if (BatchedControls) cards.GetHand().Remove(card);
                }
            }
            finally
            {
                active = null;
                activeEffects = Array.Empty<CardEffectData>();
                for (int index = 0; index < actors.Count; index++)
                {
                    Set(actors[index], "_primaryStateInformation", primary[index]); Set(actors[index], "triggers", triggers[index]);
                    AccessTools.Property(typeof(CharacterState), "IsEnchanter").SetValue(actors[index], enchanters[index]);
                }
                CombatPreviewControl.Set(combat, suppression);
            }
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = GroupedDamage ? "NativeOnCastGroupsCardHover" : HealingDamage ? "NativeHealingDamageCardHover" : CompositionBoundaries ? "NativeDamageCompositionBoundaryCardHover" : MixedDamage ? "NativeDamageCompositionCardHover" : RandomDamage ? "NativeRandomDamageCardHover" : CompositeDamage ? "NativeCompositeDamageCardHover" : DirectDamage ? "NativeDirectDamageCardHover" : "NativeMaximumHealthLethalCardHover",
                NativeCompleted = true, AuthoredControls = true, TargetTeam = EnemyTarget ? "Enemy" : "Player", SelfTargetId = 0, BaseHealth = baseHealth, Samples = samples,
                OriginalSuppression = originalSuppression, FinalSuppression = (int)Get(combat, "combatPreviewSuppressionCount") }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, GroupedDamage ? "oncast-groups-card-preview.mt2f" : HealingDamage ? "healing-damage-card-preview.mt2f" : CompositionBoundaries ? "damage-card-preview-boundaries.mt2f" : MixedDamage ? "damage-card-preview-compositions.mt2f" : RandomDamage ? "damage-card-preview-random.mt2f" : CompositeDamage ? "damage-card-preview-extended.mt2f" : DirectDamage ? "damage-card-preview.mt2f" : "maximum-health-lethal-preview.mt2f"))) document.Write(output);
        }
        [HarmonyPatch(typeof(CardEffectState), nameof(CardEffectState.GetIntInRange))]
        private static class RangePatch
        {
            private static void Prefix(CardEffectState __instance, out RangeDraw? __state)
            {
                __state = null;
                if (!BatchedControls || active == null || !__instance.GetUseIntRange()) return;
                __state = new RangeDraw { Min = __instance.GetParamMinInt(), Max = __instance.GetParamMaxInt(),
                    EffectIndex = Array.IndexOf(activeEffects, __instance.GetSourceCardEffectData()), EffectType = __instance.GetCardEffect().GetType().Name,
                    Multiplier = __instance.GetParamMultiplier(), Preview = AllGameManagers.Instance!.GetSaveManager().PreviewMode,
                    Test = new System.Diagnostics.StackTrace().GetFrames().Any(frame => frame.GetMethod()?.Name == "TestEffect"), Before = TestRng() };
            }
            private static void Postfix(int __result, RangeDraw? __state)
            {
                if (__state == null || active == null) return;
                __state.Value = __result; __state.After = TestRng(); active.RangeDraws.Add(__state);
            }
        }
        [HarmonyPatch(typeof(CombatManager), "SetTemporaryStateEnabled")]
        private static class CopyPatch
        {
            private static void Prefix(bool enable, List<CharacterState> characters, bool isCardPreview)
            {
                if (active == null || !isCardPreview || enable) return;
                active.CopiedActual = Snapshot(characters); active.ActualTarget = FullBattleTrace.Active!.CaptureUnit(host);
                active.ActualTargets = characters.Select(actor => FullBattleTrace.Active!.CaptureUnit(actor)).ToArray(); active.DisableCount++;
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
