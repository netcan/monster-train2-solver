using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalScaledBuffScenario
    {
        internal const string Name = "original-scaled-buff";
        internal const string CardId = "73289203-d973-4552-9500-4aaa66a6b621";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Samples = new List<object>();
        internal static readonly List<object> Queries = new List<object>();
        private static bool querying;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuScaledBuffBattery"; Set(battery, "id", "ac110027-0000-4000-8000-000000000001");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 10);
            Set(battery, "effects", new List<CardEffectData> { energy });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(battery);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(CardId), Add(CardId), Add(battery.GetID()), Add(CardId) };
            foreach (CardState card in created.Skip(2)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SCALED-BUFF-PREPARED original Pyre Plating, X three/zero/ten, attack buff replacement, paired armor through Pyre immunity, floor/unit controls and Consume.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
        }
        [HarmonyPatch(typeof(CardTraitScalingBuffDamage), nameof(CardTraitScalingBuffDamage.OnApplyingBuffDamageToUnit))]
        private static class BuffPatch
        {
            private static void Postfix(CardTraitScalingBuffDamage __instance, CardTraitState.ApplyingDamageParameters parameters, int __result)
            {
                if (!Enabled || !Prepared || querying || AllGameManagers.Instance!.GetSaveManager().PreviewMode || __instance.GetCard().GetCardDataID() != CardId) return;
                FullBattleTrace trace = FullBattleTrace.Active!; CardState card = __instance.GetCard();
                Samples.Add(new { CardId = trace.CardId(card), InputDamage = parameters.damage, ActualDamage = __result, Context = trace.CaptureContext() });
                querying = true;
                try
                {
                    foreach (int input in new[] { 0, 99 })
                    {
                        CombatContext before = trace.CaptureContext(); int frame = UnityEngine.Time.frameCount;
                        int actual = __instance.OnApplyingBuffDamageToUnit(new CardTraitState.ApplyingDamageParameters { damage = input, damageType = Damage.Type.Default }, AllGameManagers.Instance.GetCoreManagers());
                        Queries.Add(new { CardId = trace.CardId(card), InputDamage = input, ActualDamage = actual, Context = before,
                            StateUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(trace.CaptureContext())) });
                    }
                }
                finally { querying = false; }
            }
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
