using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStatusStackDamageScenario
    {
        internal const string Name = "original-status-stack-damage";
        internal const string CardId = "67e742a7-d9b6-46e4-b9dc-37edd4dfb872";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal sealed class DamageSample
        {
            public int CardId { get; set; }
            public int InputDamage { get; set; }
            public int ActualDamage { get; set; }
            public CombatContext Context { get; set; } = null!;
            public CombatUnit Target { get; set; } = null!;
            public bool StateUnchanged { get; set; }
            internal int Frame;
        }
        internal static readonly List<DamageSample> Samples = new List<DamageSample>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", new[] { Status("debuff", 5), Status("poison", 4), Status("armor", 7), Status("buff", 2) });
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            CardData control = UnityEngine.Object.Instantiate(steward); control.name = "PojuStatusStackPositiveControl";
            Set(control, "id", "ac110020-0000-4000-8000-000000000001");
            CharacterData positive = UnityEngine.Object.Instantiate(unit); positive.name = "PojuStatusStackPositiveCharacter";
            Set(positive, "id", "ac110020-0000-4000-8000-000000000002"); Set(positive, "attackDamage", 0);
            Set(positive, "startingStatusEffects", new[] { Status("armor", 7), Status("buff", 2) });
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters); spawn.Cheat_SetTargetMode(TargetMode.Room);
            Set(spawn, "paramCharacterData", positive); Set(control, "effects", new List<CardEffectData> { spawn }); definitions.Add(control);
            CardData sap = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            sap.name = "PojuStatusStackSap"; Set(sap, "id", "ac110020-0000-4000-8000-000000000003");
            Set(sap, "cost", 0); Set(sap, "costType", CardData.CostType.Default); Set(sap, "targetless", false); Set(sap, "targetsRoom", true);
            Set(sap, "traits", new List<CardTraitData>()); Set(sap, "startingUpgrades", new List<CardUpgradeData>());
            Set(sap, "triggers", new List<CardTriggerEffectData>()); Set(sap, "effectTriggers", new List<CharacterTriggerData>());
            var add = new CardEffectData("CardEffectAddStatusEffect", null!, Team.Type.Monsters); add.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(add, "paramStatusEffects", new[] { Status("debuff", 2) }); Set(sap, "effects", new List<CardEffectData> { add }); definitions.Add(sap);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(control.GetID()), Add(CardId), Add(CardId), Add(sap.GetID()), Add(CardId) };
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-STATUS-STACK-DAMAGE-PREPARED original Wracking Pain, zero-debuff positive-status control, live multi-debuff damage, added Sap and outside-floor control.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            int owner = index == 3 ? 2 : index >= 4 ? 0 : -1;
            int target = owner < 0 ? 0 : source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[owner]).Id;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] &&
                play.RoomIndex == (index == 1 ? 1 : 0) && play.TargetUnitId == target);
        }
        [HarmonyPatch(typeof(CardTraitAddDamagePerStatusEffect), nameof(CardTraitAddDamagePerStatusEffect.OnApplyingDamage))]
        private static class DamagePatch
        {
            private static void Prefix(CardTraitAddDamagePerStatusEffect __instance, CardTraitState.ApplyingDamageParameters damageParams, out DamageSample? __state)
            {
                __state = null;
                if (!Enabled || !Prepared || AllGameManagers.Instance!.GetSaveManager().PreviewMode || __instance.GetCard().GetCardDataID() != CardId) return;
                __state = new DamageSample { CardId = FullBattleTrace.Active!.CardId(__instance.GetCard()), InputDamage = damageParams.damage,
                    Context = FullBattleTrace.Active.CaptureContext(), Target = FullBattleTrace.Active.CaptureUnit(damageParams.targetCharacter!), Frame = UnityEngine.Time.frameCount };
            }
            private static void Postfix(int __result, DamageSample? __state)
            {
                if (__state == null) return;
                __state.ActualDamage = __result;
                __state.StateUnchanged = __state.Frame == UnityEngine.Time.frameCount &&
                    JToken.DeepEquals(JToken.FromObject(__state.Context), JToken.FromObject(FullBattleTrace.Active!.CaptureContext()));
                Samples.Add(__state);
            }
        }
        private static StatusEffectStackData Status(string id, int count) => new StatusEffectStackData { statusId = id, count = count };
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
