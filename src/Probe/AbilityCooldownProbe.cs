using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class AbilityCooldownProbe
    {
        internal static bool Known(string type) => type == "CardEffectResetCooldown" || type == "CardEffectAdjustAbilityCooldown" || type == "CardEffectRemoveStatusEffect";
        internal static UnitAbilityState? Capture(CharacterState unit)
        {
            object state = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(unit);
            CardData? ability = unit.GetUnitAbility();
            int cooldown = unit.GetUnitAbilityCooldown(), spawn = unit.GetUnitAbilityCooldownAtSpawn();
            CardData? previous = (CardData?)AccessTools.Field(state.GetType(), "prevUnitAbilityCardData").GetValue(state);
            bool equipment = unit.GetUnitAbilityIsFromEquipment(), resolving = (bool)AccessTools.Field(state.GetType(), "isUnitAbilityResolving").GetValue(state);
            if (ability == null && cooldown == 0 && spawn == 0 && previous == null && !equipment && !resolving) return null;
            return new UnitAbilityState(ability?.GetID() ?? "", cooldown, spawn, equipment, resolving, previous?.GetID(),
                ability == null ? null : CardGenerationProbe.Creation(ability), ability == null ? null : AbilityLifecycleProbe.Definition(ability),
                previous == null ? null : AbilityLifecycleProbe.Definition(previous));
        }
        internal static StatusDictionaryState CaptureDictionary(CharacterState unit)
        {
            object state = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(unit);
            object dictionary = AccessTools.Field(state.GetType(), "statusEffects").GetValue(state);
            object? Field(object target, string name) => (AccessTools.Field(target.GetType(), name) ?? AccessTools.Field(target.GetType(), "_" + name)).GetValue(target);
            int count = (int)Field(dictionary, "count")!;
            Array? entries = (Array?)Field(dictionary, "entries");
            if (count == 0) return new StatusDictionaryState(Array.Empty<string>(), Array.Empty<int>());
            string?[] slots = Enumerable.Range(0, count).Select(index => (string?)Field(entries!.GetValue(index)!, "key")).ToArray();
            var free = new List<int>();
            int next = (int)Field(dictionary, "freeList")!;
            while (next >= 0)
            {
                if (free.Contains(next) || next >= count) throw new InvalidOperationException("Invalid native dictionary free list.");
                free.Add(next); next = (int)Field(entries!.GetValue(next)!, "next")!;
            }
            return new StatusDictionaryState(slots, free);
        }
        internal static CardActionEffect Describe(CardEffectData effect, int value, bool parameter)
        {
            var excluded = new List<SubtypeData>(); effect.GetTargetCharacterExcludedSubtypes(excluded);
            string type = effect.GetEffectStateName().Substring("CardEffect".Length);
            bool remove = type == "RemoveStatusEffect";
            return new CardActionEffect(remove ? "RemoveStatus" : type, effect.GetTargetMode().ToString(), value,
                effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters),
                remove ? effect.GetParamStatusEffects().Take(1).Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray() : Array.Empty<CombatStatus>(),
                tests: new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(), effect.GetShouldCancelSubsequentEffectsIfTestFails(), false),
                filters: new CardTargetFilters(effect.GetTargetModeHealthFilter().ToString(), effect.GetTargetModeStatusEffectsFilter(),
                    effect.GetTargetModeStatusEffectsExcludedFilter(), effect.GetTargetIgnoreBosses(),
                    effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key,
                    excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray()), cooldownParameter: remove ? null : (bool?)parameter);
        }
        internal sealed class Record
        {
            public int SourceCardId { get; set; }
            public CardActionEffect Effect { get; set; } = null!;
            public int[] Targets { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        [HarmonyPatch]
        private static class ApplyPatch
        {
            private static IEnumerable<MethodBase> TargetMethods() => new[] { typeof(CardEffectResetCooldown), typeof(CardEffectAdjustAbilityCooldown),
                typeof(CardEffectRemoveStatusEffect) }.Select(type => AccessTools.Method(type, "ApplyEffect"));
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (AbilityCooldownScenario.Prepared && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState, cardEffectParams); }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            RoomState room = AllGameManagers.Instance!.GetRoomManager()!.GetRoom(parameters.selectedRoom);
            var record = new Record { SourceCardId = parameters.playedCard == null ? 0 : trace.CardId(parameters.playedCard),
                Effect = Describe(effect.GetSourceCardEffectData(), effect.GetParamInt(), effect.GetParamBool()),
                Targets = parameters.targets.Select(trace.UnitId).ToArray(), Before = trace.Capture(room) };
            Records.Add(record);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); record.After = trace.Capture(room); }
        }
    }
}
