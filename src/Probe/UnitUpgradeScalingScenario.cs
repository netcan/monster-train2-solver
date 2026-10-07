using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitUpgradeScalingScenario
    {
        internal static readonly List<Sample> Samples = new List<Sample>();
        private static bool calibrating;
        internal static bool CalibrationContextUnchanged { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); save.SetForgePoints(2);
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] spells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Upgrade scaling fixture requires the owned rearrangement spell.");
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            data.GetTraits().Clear();
            data.GetTraits().Add(Trait("Damage", "TimesPlayed", 2, 0, "ThisBattle"));
            data.GetTraits().Add(Trait("Health", "AnyCharacter", 3, 1));
            data.GetTraits().Add(Trait("Damage", "MoonPhase", 1, 1));
            data.GetTraits().Add(Trait("Health", "TurnCount", 1, 0));
            data.GetTraits().Add(Trait("Damage", "ForgePoints", -1, 99));
            CardUpgradeData temporary = Upgrade("PojuScalingTemporary", "cdf091ee-b111-4db1-b5e1-b69f15a50301", 2, 3);
            CardUpgradeData permanent = Upgrade("PojuScalingPermanent", "cdf091ee-b111-4db1-b5e1-b69f15a50302", 1, 1);
            CardUpgradeData unique = Upgrade("PojuScalingUnique", "cdf091ee-b111-4db1-b5e1-b69f15a50303", 2, 2); Set(unique, "isUnique", true);
            CardUpgradeData magic = Upgrade("PojuScalingMagicOnly", "cdf091ee-b111-4db1-b5e1-b69f15a50304", 1, 1); Set(magic, "magicPowerTraitScalingOnly", true);
            CardUpgradeData blocked = Upgrade("PojuScalingBlocked", "cdf091ee-b111-4db1-b5e1-b69f15a50305", 1, 1);
            Set(blocked, "bonusSize", 99); Set(blocked, "restrictSizeToRoomCapacity", true);
            data.GetEffects().Clear();
            data.GetEffects().Add(Effect("CardEffectAddTempCardUpgradeToUnits", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle, true));
            data.GetEffects().Add(Effect("CardEffectAddCardUpgradeToUnits", permanent, UnitUpgradeLifetime.Permanent));
            data.GetEffects().Add(Effect("CardEffectAddTempCardUpgradeToUnits", unique, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            data.GetEffects().Add(Effect("CardEffectAddTempCardUpgradeToUnits", unique, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            data.GetEffects().Add(Effect("CardEffectAddTempCardUpgradeToUnits", magic, UnitUpgradeLifetime.TemporaryUntilUnitDeath));
            data.GetEffects().Add(Effect("CardEffectAddTempCardUpgradeToUnits", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            data.GetEffects().Add(Effect("CardEffectRemoveTempUpgradeFromUnit", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            data.GetEffects().Add(Effect("CardEffectAddTempCardUpgradeToUnits", blocked, UnitUpgradeLifetime.TemporaryUntilEndOfBattle));
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(data, save);
            CardState[] stewards = owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").ToArray();
            CardData steward = save.GetAllGameData().FindCardData(stewards[0].GetCardDataID())!;
            steward.GetTraits().Add(Trait("Health", "AnyCharacter", 2, 0));
            foreach (CardState card in stewards) card.Setup(steward, save);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("UNIT-UPGRADE-SCALING-PREPARED ordered attack/health traits, signed bonuses, turn/moon/local history, base-valued removal, unique/capacity no-ops and magic-only gates; natural boss/waves retained.");
        }
        internal static CardTraitData Trait(string stat, string type, int amount, int restriction, string duration = "ThisTurn")
        {
            CardTraitData data = DamageScalingScenario.Trait(type, duration, amount, 1, false);
            data.Setup(stat == "Damage" ? "CardTraitScalingUpgradeUnitAttack" : "CardTraitScalingUpgradeUnitHealth"); data.SetParamInt(amount);
            Set(data, "paramTrackedValue", Enum.Parse(typeof(CardStatistics.TrackedValueType), type));
            Set(data, "paramEntryDuration", Enum.Parse(typeof(CardStatistics.EntryDuration), duration));
            Set(data, "paramCardType", CardStatistics.CardTypeTarget.Any); Set(data, "paramInt3", restriction);
            return data;
        }
        private static CardUpgradeData Upgrade(string name, string id, int damage, int health)
        {
            CardUpgradeData data = DynamicUpgradeScenario.Upgrade(name, id, damage, health, 0, 0, "armor", 0);
            data.GetStatusEffectUpgrades().Clear(); return data;
        }
        private static CardEffectData Effect(string type, CardUpgradeData upgrade, UnitUpgradeLifetime lifetime, bool first = false)
        {
            var effect = new CardEffectData(type, null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(first ? TargetMode.DropTargetCharacter : TargetMode.LastTargetedCharacters);
            Set(effect, "paramCardUpgradeData", upgrade); Set(effect, "additionalParamInt1", (int)lifetime); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        internal static void Calibrate(FullBattleTrace trace)
        {
            CombatContext original = trace.CaptureContext();
            AllGameManagers managers = AllGameManagers.Instance!;
            CardState owner = managers.GetCardManager()!.GetAllCards(new List<CardState>()).First(card =>
                card.GetTraitStates().Any(trait => trait is CardTraitScalingUpgradeUnitAttack));
            CardState other = managers.GetCardManager()!.GetAllCards(new List<CardState>()).First(card => card != owner);
            var units = new List<CharacterState>(); managers.GetRoomManager()!.GetRoom(3).AddCharactersToList(units, Team.Type.Monsters);
            CardUpgradeData ordinary = Upgrade("PojuCallbackUpgrade", "cdf091ee-b111-4db1-b5e1-b69f15a50306", 7, 11);
            CardUpgradeData magic = Upgrade("PojuCallbackMagic", "cdf091ee-b111-4db1-b5e1-b69f15a50307", 7, 11); Set(magic, "magicPowerTraitScalingOnly", true);
            CardStatistics statistics = managers.GetCardStatistics();
            var originalEntries = (Dictionary<CardState, CardStatsEntry>)AccessTools.Field(typeof(CardStatistics), "deckStats").GetValue(statistics);
            var entries = new Dictionary<CardState, CardStatsEntry>();
            foreach (var item in originalEntries)
            {
                var copy = new CardStatsEntry();
                var values = (Dictionary<CardStatistics.EntryDuration, Dictionary<CardStatistics.TrackedValueType, int>>)
                    AccessTools.Field(typeof(CardStatsEntry), "entryValues").GetValue(item.Value);
                foreach (var duration in values)
                foreach (var counter in duration.Value) copy.IncrementValue(counter.Key, counter.Value, duration.Key);
                entries.Add(item.Key, copy);
            }
            if (!entries.ContainsKey(owner)) entries.Add(owner, new CardStatsEntry());
            entries[owner].IncrementValue(CardStatistics.TrackedValueType.TimesPlayed, 17, CardStatistics.EntryDuration.ThisBattle);
            AccessTools.Field(typeof(CardStatistics), "deckStats").SetValue(statistics, entries);
            calibrating = true;
            try
            {
                foreach (string? kind in new string?[] { null, "OnSpawn", "OnHeal" })
                foreach (CardUpgradeData definition in new[] { ordinary, magic })
                foreach (CardTraitState trait in owner.GetTraitStates().Where(trait => UnitUpgradeScalingProbe.Known(trait.GetType().Name)))
                {
                    CharacterTriggerState? trigger = null;
                    if (kind != null) { trigger = new CharacterTriggerState(); Set(trigger, "trigger", Enum.Parse(typeof(CharacterTriggerData.Trigger), kind)); }
                    var upgrade = new CardUpgradeState(); upgrade.Setup(definition);
                    // Native queries the trait owner, even if the callback argument names another card.
                    trait.OnApplyingCardUpgradeToUnit(other, units[0], trigger, upgrade, managers.GetCoreManagers());
                }
            }
            finally { calibrating = false; AccessTools.Field(typeof(CardStatistics), "deckStats").SetValue(statistics, originalEntries); }
            CalibrationContextUnchanged = JToken.DeepEquals(JToken.FromObject(original), JToken.FromObject(trace.CaptureContext()));
            if (!CalibrationContextUnchanged) throw new InvalidOperationException("Unit-upgrade callback calibration changed live context.");
        }
        internal sealed class Sample
        {
            public string Origin { get; set; } = "";
            public int ActionIndex { get; set; }
            public int OwnerCardId { get; set; }
            public int ThisCardId { get; set; }
            public string? TriggerKind { get; set; }
            public ScalingUnitUpgradeTrait Trait { get; set; } = null!;
            public CardUpgradeModifier BeforeUpgrade { get; set; } = null!;
            public CardUpgradeModifier? AfterUpgrade { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public string? Difference { get; set; }
            public string? CaptureError { get; set; }
        }
        [HarmonyPatch]
        private static class ApplyingPatch
        {
            private static IEnumerable<MethodBase> TargetMethods() => new[] { typeof(CardTraitScalingUpgradeUnitAttack), typeof(CardTraitScalingUpgradeUnitHealth) }
                .Select(type => (MethodBase)AccessTools.Method(type, "OnApplyingCardUpgradeToUnit"));
            private static void Prefix(CardTraitState __instance, CardState thisCard, CharacterTriggerState? characterTriggerState, CardUpgradeState upgradeState, out Sample? __state)
            {
                __state = null; FullBattleTrace? trace = FullBattleTrace.Active;
                string? scenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS");
                if (scenario != "unit-upgrade-scaling" && scenario != "unit-trigger-upgrades" && scenario != "spawn-triggers" && scenario != "spawn-triggers-lethal" && scenario != "unit-turn-begin" && scenario != "team-turn-begin" && scenario != "pre-hand-discard" && scenario != "pre-hand-discard-lethal" && scenario != "clone-upgrade-refresh" && scenario != "pre-combat" && scenario != "pre-combat-lethal" && scenario != "triggered-healing" && scenario != "post-combat-healing" && scenario != "triggered-damage" && scenario != "damage-death-queue" || trace == null ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode || !calibrating && !trace.PendingActionIndex.HasValue && !trace.PendingTurnIndex.HasValue) return;
                var sample = new Sample { Origin = calibrating ? "Calibration" : "Live", ActionIndex = trace.PendingActionIndex ?? -1,
                    TriggerKind = characterTriggerState?.GetTrigger().ToString() };
                __state = sample; Samples.Add(sample);
                try
                {
                    CardState owner = __instance.GetCard(); sample.OwnerCardId = trace.CardId(owner); sample.ThisCardId = trace.CardId(thisCard);
                    int index = owner.GetTraitStates().Where(trait => UnitUpgradeScalingProbe.Known(trait.GetType().Name)).ToList().IndexOf(__instance);
                    sample.Trait = UnitUpgradeScalingProbe.Capture(owner)![index];
                    sample.BeforeUpgrade = CardModifierProbe.Upgrade(upgradeState); sample.Before = trace.CaptureContext();
                }
                catch (Exception exception) { sample.CaptureError = exception.ToString(); }
            }
            private static void Postfix(CardUpgradeState upgradeState, Sample? __state)
            {
                if (__state == null) return;
                try
                {
                    __state.AfterUpgrade = CardModifierProbe.Upgrade(upgradeState); __state.After = FullBattleTrace.Active!.CaptureContext();
                    if (__state.CaptureError != null) return;
                    UnitUpgradeScalingResult predicted = UnitUpgradeScalingModel.ApplyTrait(__state.Before, __state.Trait, __state.OwnerCardId,
                        __state.BeforeUpgrade, __state.TriggerKind);
                    __state.Difference = !predicted.Supported ? predicted.UnsupportedReason :
                        !JToken.DeepEquals(JToken.FromObject(predicted.Upgrade!), JToken.FromObject(__state.AfterUpgrade)) ? "Native scaled upgrade differs." :
                        !JToken.DeepEquals(JToken.FromObject(predicted.Context!), JToken.FromObject(__state.After)) ? "Native upgrade trait context differs." : null;
                }
                catch (Exception exception) { __state.CaptureError = exception.ToString(); }
            }
        }
    }
}
