using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalUnitUpgradeTriggerRelicScenario
    {
        internal const string Name = "original-unit-upgrade-trigger-relic";
        private const string AssetKey = "AddAttackToFunguy";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static readonly List<TriggerObservation> TriggerObservations = new List<TriggerObservation>();
        private static string carrierCardId = "";
        private static string requiredSubtype = "";

        internal sealed class TriggerObservation
        {
            public int Sequence { get; set; }
            public int ActorId { get; set; }
            public string Kind { get; set; } = "";
            public int ParamInt { get; set; }
            public int ParamInt2 { get; set; }
            public int TriggerCount { get; set; }
            public bool CanFire { get; set; }
            public bool FromRunningQueue { get; set; }
            public string[] Subtypes { get; set; } = Array.Empty<string>();
            public int AttackBefore { get; set; }
            public int? AttackAfter { get; set; }
            public bool Completed { get; set; }
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("The unit-upgrade relic must be acquired before battle setup.");
            AllGameData data = save.GetAllGameData();
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.name == AssetKey);
            RelicEffectData[] effects = relic.GetEffects().ToArray();
            if (effects.Length != 2 || effects.Any(effect =>
                effect.GetEffectClassName() != RelicModel.AddRemoveTempUpgradeOnUnitTrigger))
                throw new InvalidOperationException("Original Funguy relic no longer has its two unit-trigger upgrade effects.");
            requiredSubtype = effects[0].GetParamCharacterSubtype()?.Key ??
                throw new InvalidOperationException("Original Funguy relic lost its subtype filter.");
            if (effects.Any(effect => effect.GetParamCharacterSubtype()?.Key != requiredSubtype))
                throw new InvalidOperationException("Original Funguy relic effects have different subtype filters.");

            CardData carrier = data.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678") ??
                throw new InvalidOperationException("The starting Steward carrier definition is missing.");
            if (!carrier.IsSpawnerCard() || carrier.GetCardType() != CardType.Monster)
                throw new InvalidOperationException("The authored Funguy carrier is not a monster card.");
            carrierCardId = carrier.GetID();
            CharacterData unit = carrier.GetSpawnCharacterData()!;
            var subtypes = (List<string>)AccessTools.Field(typeof(CharacterData), "subtypeKeys").GetValue(unit);
            if (!subtypes.Contains(requiredSubtype)) subtypes.Add(requiredSubtype);
            Set(unit, "health", 1);
            Set(unit, "attackDamage", 1);
            Set(unit, "size", 1);
            string[] triggerKinds = effects.Select(effect => effect.GetParamTrigger().ToString()).Distinct().ToArray();
            var triggers = unit.GetTriggers().Where(trigger => !triggerKinds.Contains(trigger.GetTrigger().ToString())).ToList();
            foreach (string kind in triggerKinds)
            {
                CharacterTriggerData trigger = HealingScenario.HealGold(0, once: false, ignoreSilence: true);
                Set(trigger, "trigger", Enum.Parse<CharacterTriggerData.Trigger>(kind));
                triggers.Add(trigger);
            }
            Set(unit, "triggers", triggers);
            StatusEffectStackData[] startingStatuses = unit.GetStartingStatusEffects()
                .Where(status => status.statusId != "horde")
                .Concat(new[] { new StatusEffectStackData { statusId = "horde", count = 2 } }).ToArray();
            Set(unit, "startingStatusEffects", startingStatuses);
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-UNIT-UPGRADE-TRIGGER-RELIC-ACQUIRED unchanged " + AssetKey +
                "; trigger=" + string.Join(",", triggerKinds) + "; subtype=" + requiredSubtype +
                "; carrier=" + carrier.name + "; authored two-stack Horde carrier and zero-gold trigger witnesses.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || string.IsNullOrEmpty(carrierCardId))
                throw new InvalidOperationException("The original Funguy relic missed pre-battle acquisition.");
            SaveManager save = managers.GetSaveManager();
            CombatRelicState relic = RelicProbe.Capture(managers).Single(item => item.AssetKey == AssetKey);
            RelicUnitUpgradeTrigger[] rules = relic.UnitUpgradeTriggers?.ToArray() ?? Array.Empty<RelicUnitUpgradeTrigger>();
            if (!relic.EffectTypes.SequenceEqual([RelicModel.AddRemoveTempUpgradeOnUnitTrigger, RelicModel.AddRemoveTempUpgradeOnUnitTrigger]) ||
                rules.Length != 2 || !rules.Select(rule => rule.Trigger).SequenceEqual(["OnTroopAdded", "OnTroopRemoved"]) ||
                rules.Any(rule => !rule.SourcePlayer || rule.SourceEnemy || rule.RequiredSubtype != requiredSubtype ||
                    rule.UpgradeLifetime != RelicUnitUpgradeTriggerModel.UntilUnitDeath || rule.Upgrade == null) ||
                rules[0].ShouldRemove || !rules[1].ShouldRemove || rules[0].Upgrade!.DataId != rules[1].Upgrade!.DataId)
                throw new InvalidOperationException("Captured original Funguy relic payload no longer matches its native add/remove rules.");

            CardData carrier = save.GetAllGameData().FindCardData(carrierCardId) ??
                throw new InvalidOperationException("The authored Funguy carrier definition disappeared.");
            CardManager cards = managers.GetCardManager()!;
            cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false);
            cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-UNIT-UPGRADE-TRIGGER-RELIC-PREPARED payload=" + rules[0].Upgrade!.AssetKey +
                "; attack=" + rules[0].Upgrade!.Stats.Damage + "; two authored Funguy Horde carriers in hand.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
            foreach (CardToken card in source.Spawn.Train.Context.Cards.Hand.Where(card => card.DataId == carrierCardId))
            {
                PlayCardAction? summon = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                    action.RoomIndex == 0 && action.ActivatorUnitId == 0 && action.TargetUnitId == 0);
                if (summon != null) return summon;
            }
            return null;
        }

        private static IEnumerator Observe(IEnumerator native, CharacterState actor, CharacterTriggerData.Trigger kind,
            CharacterState.FireTriggersData? data, bool canFire, bool fromRunningQueue, int triggerCount)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var observation = new TriggerObservation
            {
                Sequence = trace.NextPhaseSequence(), ActorId = trace.UnitId(actor), Kind = kind.ToString(),
                ParamInt = data?.paramInt ?? 0, ParamInt2 = data?.paramInt2 ?? 0, TriggerCount = triggerCount,
                CanFire = canFire, FromRunningQueue = fromRunningQueue,
                Subtypes = actor.GetSubtypes().Select(subtype => subtype.Key).ToArray(), AttackBefore = actor.GetAttackDamage()
            };
            TriggerObservations.Add(observation);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                observation.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true))
                    observation.AttackAfter = actor.GetAttackDamage();
            }
        }

        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class TriggerPatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger,
                CharacterState.FireTriggersData fireTriggersData, bool canFireTriggers, bool fromRunningTriggerQueue,
                int triggerCount, ref IEnumerator __result)
            {
                if (Enabled && Prepared && FullBattleTrace.Active != null &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode &&
                    (trigger is CharacterTriggerData.Trigger.OnTroopAdded or CharacterTriggerData.Trigger.OnTroopRemoved) &&
                    __instance.GetSubtypes().Any(subtype => subtype.Key == requiredSubtype))
                    __result = Observe(__result, __instance, trigger, fireTriggersData, canFireTriggers,
                        fromRunningTriggerQueue, triggerCount);
            }
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
