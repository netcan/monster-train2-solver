using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class EquipmentAbilityScenario
    {
        internal static bool Prepared;
        internal static readonly List<BattleActionProbe.Record> InitialSpawns = new List<BattleActionProbe.Record>();
        private static CardData b = null!, c = null!, d = null!;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            var counts = owned.Select(card => managers.GetSaveManager().GetAllGameData().FindCardData(card.GetCardDataID())!)
                .Distinct().ToDictionary(card => card, card => card.GetEffects().Count);
            AbilityCooldownScenario.Prepare(managers, log, true);
            foreach (var item in counts.Where(item => item.Key.GetEffects().Count > item.Value))
            {
                item.Key.GetEffects().RemoveRange(item.Value, item.Key.GetEffects().Count - item.Value);
                foreach (CardState card in owned.Where(card => card.GetCardDataID() == item.Key.GetID())) card.Setup(item.Key, managers.GetSaveManager());
            }
            AllGameData game = managers.GetSaveManager().GetAllGameData();
            CardData original = game.FindCardData("c2f6ed7f-18ce-4070-b65f-7dd9f5160063")!;
            b = Clone("EquipmentSkillB", "c2f6ed7f-18ce-4070-b65f-7dd9f5160074", 4, 0);
            Set(b, "cost", 0);
            c = Clone("UpgradedInitialSkillC", "c2f6ed7f-18ce-4070-b65f-7dd9f5160075", 6, 2);
            d = Clone("KeptExistingSkillD", "c2f6ed7f-18ce-4070-b65f-7dd9f5160076", 8, 3);
            Prepared = true;
            CardData Clone(string name, string id, int cooldown, int spawn)
            {
                CardData data = UnityEngine.Object.Instantiate(original); data.name = name;
                Set(data, "id", id); Set(data, "cooldownAfterActivated", cooldown); Set(data, "cooldownAtSpawn", spawn);
                var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Heroes); damage.Cheat_SetTargetMode(TargetMode.FrontInRoom);
                Set(damage, "paramInt", 2);
                var heal = new CardEffectData("CardEffectHeal", null!, Team.Type.Heroes); heal.Cheat_SetTargetMode(TargetMode.Self);
                Set(heal, "paramInt", 1); Set(data, "effects", new List<CardEffectData> { damage, heal });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(game)).Add(data); return data;
            }
        }
        internal static void Configure(CardUpgradeData gearUpgrade, CardState[] owned, CardState[] gear, CardData steward, SaveManager save)
        {
            Set(gearUpgrade, "unitAbilityUpgrade", b);
            CharacterTriggerData available = HealingScenario.HealGold(5, false, true);
            Set(available, "trigger", CharacterTriggerData.Trigger.OnUnitAbilityAvailable);
            gearUpgrade.GetCharacterTriggerUpgrades().Add(available);
            Apply(gear[1], Definition("GearSkillC", "c2f6ed7f-18ce-4070-b65f-7dd9f5160077", c));
            Apply(gear[2], Definition("GearKeepD", "c2f6ed7f-18ce-4070-b65f-7dd9f5160078", d, true));
            CardState[] stewards = owned.Where(card => card.GetCardDataID() == steward.GetID()).ToArray();
            for (int index = 0; index < stewards.Length; index++)
            {
                if (index == 1) Apply(stewards[index], Definition("InitialKeepB", "c2f6ed7f-18ce-4070-b65f-7dd9f5160079", b, true));
                else Apply(stewards[index], Definition("InitialC", "c2f6ed7f-18ce-4070-b65f-7dd9f5160080", index == 0 ? b : c));
            }
            CardState[] smallStewards = owned.Where(card => card.GetCardDataID() == "38e70e6a-20f3-4e36-8d31-c5110be07f87").ToArray();
            if (smallStewards.Length == 0) throw new InvalidOperationException("Equipment ability setup needs later small Steward summons.");
            CardData small = save.GetAllGameData().FindCardData(smallStewards[0].GetCardDataID())!;
            Set(small.GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster").GetParamCharacterData(),
                "unitAbility", save.GetAllGameData().FindCardData("c2f6ed7f-18ce-4070-b65f-7dd9f5160063")!);
            foreach (CardState card in smallStewards)
            {
                card.Setup(small, save);
                Apply(card, Definition("InitialDisabledC", "c2f6ed7f-18ce-4070-b65f-7dd9f5160080", c));
            }
            void Apply(CardState card, CardUpgradeData data) => card.ApplyPermanentUpgrade(State(data), save, ignoreUpgradeAnimation: true);
        }
        internal static void BeforeInitial(CardState card)
        {
            card.ApplyPermanentUpgrade(State(Definition("InitialB", "c2f6ed7f-18ce-4070-b65f-7dd9f5160085", b)),
                AllGameManagers.Instance!.GetSaveManager(), ignoreUpgradeAnimation: true);
            card.ApplyTemporaryUpgrade(State(Definition("TemporaryC", "c2f6ed7f-18ce-4070-b65f-7dd9f5160081", c)), AllGameManagers.Instance!.GetSaveManager());
            FullBattleTrace trace = FullBattleTrace.Active!;
            BattleTurnState before = trace.CaptureDecision(); var action = new PlayCardAction(trace.CardId(card), 0, 0);
            InitialSpawns.Add(new BattleActionProbe.Record { Before = before, Action = action, Predicted = BattleActionModel.PlayCard(before, action) });
        }
        internal static void AfterInitial()
        {
            var record = InitialSpawns.Single(); record.Actual = FullBattleTrace.Active!.CaptureDecision();
            record.ActualOutcome = RoomOutcome.Exchanged;
            if (!record.Predicted.Supported || !JToken.DeepEquals(BattleTurnProbe.Comparable(record.Predicted.State!), BattleTurnProbe.Comparable(record.Actual)))
                record.Difference = "Initial upgraded ability spawn differs: " + record.Predicted.Reason;
        }
        internal static IEnumerator DisableInitial(CharacterState host)
        {
            yield return host.RemoveUnitAbility(true);
            yield return Ready();
            yield return host.SetUnitAbility(c, false);
            yield return Ready();
        }
        internal static IEnumerator DirectCases(CharacterState host)
        {
            CardUpgradeData keep = Definition("DirectKeepD", "c2f6ed7f-18ce-4070-b65f-7dd9f5160082", d, true);
            CardUpgradeData replace = Definition("DirectC", "c2f6ed7f-18ce-4070-b65f-7dd9f5160083", c);
            CardUpgradeData plain = Definition("DirectB", "c2f6ed7f-18ce-4070-b65f-7dd9f5160084", b);
            yield return Run("ability-keep-existing", keep, false);
            yield return Run("ability-remove-nonmatching", keep, true);
            yield return Run("ability-direct-clears-equipment-history", replace, false);
            yield return Run("ability-remove-current", replace, true);
            yield return Run("ability-add-to-empty", plain, false);
            yield return Run("ability-remove-added", plain, true);
            yield return Run("ability-explicit-reassign-disabled", replace, false);
            IEnumerator Run(string label, CardUpgradeData data, bool remove)
            {
                FullBattleTrace trace = FullBattleTrace.Active!;
                var upgrade = State(data);
                var record = new DirectUnitUpgradeScenario.Record { Label = label, UnitId = trace.UnitId(host),
                    Before = trace.Capture(host.GetCurrentRoom()), Upgrade = CardModifierProbe.Upgrade(upgrade), Remove = remove };
                DirectUnitUpgradeScenario.Records.Add(record);
                var predicted = UnitModifierModel.ApplyDirect(record.Before, record.UnitId, record.Upgrade, remove);
                record.UnsupportedReason = predicted.UnsupportedReason;
                yield return remove ? host.RemoveCardUpgrade(upgrade) : host.ApplyCardUpgrade(upgrade);
                record.After = trace.Capture(host.GetCurrentRoom());
                if (predicted.Supported && !JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After)))
                    record.Difference = "Ability upgrade API room/context differs";
                yield return Ready();
            }
        }
        internal static IEnumerator RestoreEquipmentOriginal(CharacterState host, CardState gear)
        {
            var core = AllGameManagers.Instance!.GetCoreManagers();
            yield return host.RemoveEquipment(core, gear);
            yield return Ready();
            yield return host.AddEquipment(gear, core, false);
            yield return Ready();
        }
        private static CardUpgradeData Definition(string name, string id, CardData ability, bool keep = false)
        {
            var data = DynamicUpgradeScenario.Upgrade(name, id, 0, 0, 0, 0, "armor", 0);
            Set(data, "unitAbilityUpgrade", ability); Set(data, "doNotReplaceExistingUnitAbility", keep); return data;
        }
        private static CardUpgradeState State(CardUpgradeData data) { var state = new CardUpgradeState(); state.Setup(data); return state; }
        private static IEnumerator Ready()
        {
            CombatManager combat = AllGameManagers.Instance!.GetCombatManager()!;
            while (combat.IsRunningTriggerQueue || ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count > 0)
                yield return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
