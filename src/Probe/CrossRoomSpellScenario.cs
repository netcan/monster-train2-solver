using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class CrossRoomSpellScenario
    {
        internal static readonly List<TargetRecord> Targets = new List<TargetRecord>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log, bool terminal = true)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] spells = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Where(card => card.GetCardType() == CardType.Spell &&
                    card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange")).ToArray();
            if (spells.Length == 0) throw new InvalidOperationException("Cross-room fixture requires the owned rearrangement spell.");
            CardData data = save.GetAllGameData().FindCardData(spells[0].GetCardDataID())!;
            var effects = data.GetEffects(); effects.Clear();
            effects.Add(Effect("CardEffectDamage", TargetMode.Tower, Team.Type.Heroes, terminal ? 2 : 0));
            effects.Add(Status(TargetMode.LastTargetedCharacters, Team.Type.Heroes, "pyregel", 1));
            effects.Add(Effect("CardEffectDamage", TargetMode.FrontInAllRooms, Team.Type.Heroes | Team.Type.Monsters, terminal ? 1 : 0));
            effects.Add(Effect("CardEffectDamage", TargetMode.FrontInRoomAndRoomAbove, Team.Type.Heroes | Team.Type.Monsters, terminal ? 1 : 0));
            effects.Add(Effect("CardEffectDamage", TargetMode.WeakestAllRooms, Team.Type.Heroes, 1));
            effects.Add(Effect("CardEffectDamage", TargetMode.StrongestAllRooms, Team.Type.Heroes, 1));
            effects.Add(Effect("CardEffectHeal", TargetMode.StrongestLastTargetedCharactersRoom, Team.Type.Monsters, 1));
            effects.Add(Effect("CardEffectDamage", TargetMode.RandomFromAnyRoom, Team.Type.Heroes, 1));
            effects.Add(Effect("CardEffectDamage", TargetMode.RandomFromAnyRoom, Team.Type.Heroes | Team.Type.Monsters, 1));
            effects.Add(Effect("CardEffectHeal", TargetMode.RandomFromAnyRoom, Team.Type.Monsters));
            effects.Add(Upgrade("CardEffectAddTempCardUpgradeToUnits", "Temporary", 0, 4, UnitUpgradeLifetime.TemporaryUntilUnitDeath));
            effects.Add(Effect("CardEffectDamage", TargetMode.Tower, Team.Type.Monsters, 1));
            effects.Add(Effect("CardEffectHeal", TargetMode.Tower, Team.Type.Monsters, 999));
            if (terminal) effects.Add(Effect("CardEffectDamage", TargetMode.Tower, Team.Type.Heroes, 999));
            effects.Add(Status(TargetMode.StrongestLastTargetedCharactersRoom, Team.Type.Monsters, "armor", 2));
            effects.Add(Effect("CardEffectDamage", TargetMode.RandomFromAnyRoom, Team.Type.Heroes));
            effects.Add(Upgrade("CardEffectAddCardUpgradeToUnits", "Permanent", 1, 2, UnitUpgradeLifetime.Permanent));
            effects.Add(Effect("CardEffectHeal", TargetMode.Tower, Team.Type.Monsters, 999));
            Set(data, "targetless", false); Set(data, "targetsRoom", true);
            foreach (CardState card in spells) card.Setup(data, save);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("CROSS-ROOM-SPELLS-PREPARED global/front/above/HP/random targets, live last-reference rooms and remote upgrades/healing; terminal=" + terminal + "; natural boss and waves retained.");
        }

        private static CardEffectData Upgrade(string type, string name, int damage, int health, UnitUpgradeLifetime lifetime)
        {
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade("PojuCrossRoom" + name,
                "325a50fa-0634-4f54-b745-4c768db0000" + (name == "Temporary" ? "1" : "2"), damage, 0, 0, health, "armor", 0);
            var effect = Effect(type, TargetMode.Tower, Team.Type.Monsters);
            Set(effect, "paramCardUpgradeData", upgrade); Set(effect, "additionalParamInt1", (int)lifetime);
            return effect;
        }
        private static CardEffectData Status(TargetMode target, Team.Type team, string id, int count)
        {
            var effect = Effect("CardEffectAddStatusEffect", target, team);
            Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = id, count = count } });
            return effect;
        }
        private static CardEffectData Effect(string type, TargetMode target, Team.Type team, int value = 0)
        {
            var effect = new CardEffectData(type, null!, team);
            effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", value);
            return effect;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);

        internal sealed class TargetRecord
        {
            public int CardId { get; set; }
            public int EffectIndex { get; set; }
            public int RoomIndex { get; set; }
            public int SelectedRoom { get; set; }
            public string Mode { get; set; } = "";
            public int[] UnitIds { get; set; } = Array.Empty<int>();
            public int[] LastIds { get; set; } = Array.Empty<int>();
            public int[] LastRooms { get; set; } = Array.Empty<int>();
            public bool[] LastDead { get; set; } = Array.Empty<bool>();
        }

        [HarmonyPatch(typeof(TargetHelper), nameof(TargetHelper.CollectTargets), new[] { typeof(CardEffectState), typeof(CardEffectParams), typeof(ICoreGameManagers), typeof(bool) })]
        private static class TargetPatch
        {
            private static void Postfix(CardEffectState effectState, CardEffectParams cardEffectParams, ICoreGameManagers coreGameManagers, bool isTesting)
            {
                string? scenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS");
                if (scenario != "cross-room-spells" && scenario != "cross-room-targets" || isTesting ||
                    FullBattleTrace.Active == null || coreGameManagers.GetSaveManager().PreviewMode || cardEffectParams.playedCard == null ||
                    cardEffectParams.playedCard.GetEffectStates()[0].GetTargetMode() != TargetMode.Tower) return;
                try
                {
                    var last = (List<CharacterState>)AccessTools.Field(typeof(TargetHelper), "lastTargetedCharacters").GetValue(null);
                    Targets.Add(new TargetRecord {
                        CardId = FullBattleTrace.Active.CardId(cardEffectParams.playedCard),
                        EffectIndex = cardEffectParams.playedCard.GetEffectStates().IndexOf(effectState),
                        RoomIndex = cardEffectParams.selectedRoom, SelectedRoom = coreGameManagers.GetRoomManager().GetSelectedRoom(),
                        Mode = effectState.GetTargetMode().ToString(),
                        UnitIds = cardEffectParams.targets.Select(unit => FullBattleTrace.Active.UnitId(unit)).ToArray(),
                        LastIds = last.Select(unit => FullBattleTrace.Active.UnitId(unit)).ToArray(),
                        LastRooms = last.Select(unit => unit.GetCurrentRoomIndex()).ToArray(),
                        LastDead = last.Select(unit => unit.IsDead).ToArray()
                    });
                }
                catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
            }
        }
    }
}
