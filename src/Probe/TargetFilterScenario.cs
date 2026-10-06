using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class TargetFilterScenario
    {
        private const string Steward = "SubtypesData_TrainSteward", Banner = "SubtypesData_BannerUnit";
        internal static readonly List<TargetRecord> Targets = new List<TargetRecord>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] spells = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            effects.Add(Effect("CardEffectDamage", TargetMode.Tower, Team.Type.Heroes, 0));
            var gap = Effect("CardEffectAddTempCardUpgradeToUnits", TargetMode.Tower, Team.Type.Monsters);
            Set(gap, "paramCardUpgradeData", DynamicUpgradeScenario.Upgrade("PojuFilterHealthGap", "44725811-6fd0-4c0b-bb99-238180000001", 0, 0, 0, 5, "armor", 0));
            Set(gap, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath); effects.Add(gap);
            effects.Add(Filter(Status(TargetMode.Tower, Team.Type.Monsters, "armor", 2), CardEffectData.HealthFilter.Damaged,
                required: new[] { "armor", "regen" }, excluded: new[] { "immune" }, subtype: Steward, excludedSubtypes: new[] { Banner }));
            effects.Add(Filter(Effect("CardEffectBuffDamage", TargetMode.Tower, Team.Type.Monsters, 1),
                CardEffectData.HealthFilter.Damaged, subtype: Steward, excludedSubtypes: new[] { Steward }));
            effects.Add(Filter(Effect("CardEffectHeal", TargetMode.RoomHealTargets, Team.Type.Monsters, 1), CardEffectData.HealthFilter.Damaged,
                excluded: new[] { "immune" }, excludedSubtypes: new[] { Banner }));
            effects.Add(Filter(Effect("CardEffectDamage", TargetMode.FrontInRoom, Team.Type.Heroes, 1), excluded: new[] { "armor" }, ignoreBosses: true));
            effects.Add(Filter(Effect("CardEffectDamage", TargetMode.RandomFromAnyRoom, Team.Type.Heroes, 1), ignoreBosses: true));
            effects.Add(Filter(Effect("CardEffectBuffDamage", TargetMode.RandomInRoom, Team.Type.Heroes, 1),
                subtype: "SubtypesData_AttackerEnemy_b7a79da2-ded9-4e26-8702-555817499f07"));
            effects.Add(Filter(Status(TargetMode.FrontInAllRooms, Team.Type.Heroes | Team.Type.Monsters, "armor", 1),
                CardEffectData.HealthFilter.Damaged, required: new[] { "silenced" }, ignoreBosses: true, subtype: Steward));
            effects.Add(Filter(Effect("CardEffectDamage", TargetMode.LastTargetedCharacters, Team.Type.Heroes, 1),
                CardEffectData.HealthFilter.Undamaged, required: new[] { "silenced" }, ignoreBosses: true, subtype: Steward));
            effects.Add(Filter(Effect("CardEffectDamage", TargetMode.StrongestLastTargetedCharactersRoom, Team.Type.Heroes, 1), ignoreBosses: true));
            effects.Add(Filter(Effect("CardEffectBuffMaxHealth", TargetMode.FrontInRoomAndRoomAbove, Team.Type.Monsters, 1),
                CardEffectData.HealthFilter.Damaged, excludedSubtypes: new[] { Banner }));
            effects.Add(Filter(Effect("CardEffectHeal", TargetMode.WeakestAllRooms, Team.Type.Monsters, 1), CardEffectData.HealthFilter.Damaged));
            effects.Add(Filter(Status(TargetMode.BackInRoom, Team.Type.Heroes | Team.Type.Monsters, "armor", 1),
                CardEffectData.HealthFilter.Undamaged, excluded: new[] { "regen" }));
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(data, save);
            CardState[] targeted = owned.Where(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter)).ToArray();
            CardData dropData = save.GetAllGameData().FindCardData(targeted[0].GetCardDataID())!;
            // Drop override bypasses these health/status masks; last-target selection also bypasses all masks.
            Filter(dropData.GetEffects()[0], CardEffectData.HealthFilter.Damaged, required: new[] { "silenced" }, ignoreBosses: true);
            Filter(dropData.GetEffects()[1], CardEffectData.HealthFilter.Undamaged, required: new[] { "regen" }, subtype: Steward);
            foreach (CardState card in targeted) card.Setup(dropData, save);
            foreach (CardState card in owned.Where(card => card.GetCardDataID() == "d14a50f3-728d-43e1-87f0-ef1b013f6678").Take(2))
            {
                var upgrade = new CardUpgradeState(); upgrade.Setup(); upgrade.AddStatusEffectUpgradeStacks("armor", 2); upgrade.AddStatusEffectUpgradeStacks("regen", 1);
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("TARGET-FILTERS-PREPARED live health/status/subtype/boss masks, front/random/HP selectors, subtype precedence, physical-front and last/drop bypasses; natural waves retained.");
        }
        private static CardEffectData Effect(string type, TargetMode target, Team.Type team, int amount = 0)
        {
            var effect = new CardEffectData(type, null!, team); effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", amount); return effect;
        }
        private static CardEffectData Status(TargetMode target, Team.Type team, string id, int count)
        {
            CardEffectData effect = Effect("CardEffectAddStatusEffect", target, team);
            Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = id, count = count } }); return effect;
        }
        private static CardEffectData Filter(CardEffectData effect, CardEffectData.HealthFilter health = CardEffectData.HealthFilter.Both,
            string[]? required = null, string[]? excluded = null, bool ignoreBosses = false, string subtype = "", string[]? excludedSubtypes = null)
        {
            Set(effect, "targetModeHealthFilter", health); Set(effect, "targetModeStatusEffectsFilter", required ?? Array.Empty<string>());
            Set(effect, "targetModeStatusEffectsExcludedFilter", excluded ?? Array.Empty<string>()); Set(effect, "targetIgnoreBosses", ignoreBosses);
            Set(effect, "targetCharacterSubtype", subtype); Set(effect, "targetCharacterExcludedSubtypes", (excludedSubtypes ?? Array.Empty<string>()).ToList()); return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        internal sealed class TargetRecord
        {
            public int CardId { get; set; }
            public int EffectIndex { get; set; }
            public string Mode { get; set; } = "";
            public int[] UnitIds { get; set; } = Array.Empty<int>();
        }
        [HarmonyPatch]
        private static class EffectPatch
        {
            private static IEnumerable<MethodBase> TargetMethods() => new[] { typeof(CardEffectDamage), typeof(CardEffectHeal),
                typeof(CardEffectAddStatusEffect), typeof(CardEffectBuffDamage), typeof(CardEffectBuffMaxHealth),
                typeof(CardEffectAddTempCardUpgradeToUnits) }.Select(type => AccessTools.Method(type, "ApplyEffect"));
            private static void Prefix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ICoreGameManagers coreGameManagers)
            {
                if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") != "target-filters" || coreGameManagers.GetSaveManager().PreviewMode ||
                    FullBattleTrace.Active == null || cardEffectParams.playedCard == null) return;
                Targets.Add(new TargetRecord { CardId = FullBattleTrace.Active.CardId(cardEffectParams.playedCard),
                    EffectIndex = cardEffectParams.playedCard.GetEffectStates().IndexOf(cardEffectState), Mode = cardEffectState.GetTargetMode().ToString(),
                    UnitIds = cardEffectParams.targets.Select(unit => FullBattleTrace.Active.UnitId(unit)).ToArray() });
            }
        }
    }
}
