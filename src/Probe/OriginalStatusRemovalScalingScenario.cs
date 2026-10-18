using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStatusRemovalScalingScenario
    {
        internal const string Name = "original-status-removal-scaling";
        internal const string CardId = "0cdeed34-c8f5-436d-ad7d-774286333dbe";
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
            Set(unit, "startingStatusEffects", new[] { Status("cooldown", 6), Status("avarice", 1), Status("armor", 7) });
            CharacterData guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuStatusRemovalBattery"; Set(battery, "id", "ac110023-0000-4000-8000-000000000001");
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
            log.LogInfo("ORIGINAL-STATUS-REMOVAL-SCALING-PREPARED original Pyreborne Brainspike, X three/zero/ten, remaining-cooldown clamp, paired Avarice, armor/neighbor controls and Consume.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            int target = index is 2 or 3 or 5 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0) && play.TargetUnitId == target);
        }
        [HarmonyPatch(typeof(CardTraitScalingRemoveStatusEffect), nameof(CardTraitScalingRemoveStatusEffect.OnStatusEffectRemoved))]
        private static class RemovalPatch
        {
            private static void Postfix(CardTraitScalingRemoveStatusEffect __instance, CharacterState affectedCharacter, CardState thisCard, string statusId, int sourceStacks, int __result)
            {
                if (!Enabled || !Prepared || querying || AllGameManagers.Instance!.GetSaveManager().PreviewMode || thisCard.GetCardDataID() != CardId) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                Samples.Add(new { CardId = trace.CardId(thisCard), StatusId = statusId, InputStacks = sourceStacks, ActualBonus = __result,
                    Context = trace.CaptureContext(), Target = trace.CaptureUnit(affectedCharacter) });
                querying = true;
                try
                {
                    foreach (string selected in new[] { "cooldown", "armor" })
                    foreach (int input in new[] { 0, 9 })
                    {
                        CombatContext before = trace.CaptureContext(); int frame = UnityEngine.Time.frameCount;
                        int actual = __instance.OnStatusEffectRemoved(affectedCharacter, thisCard, AllGameManagers.Instance.GetCoreManagers(), selected, input);
                        Queries.Add(new { CardId = trace.CardId(thisCard), StatusId = selected, InputStacks = input, ActualBonus = actual, Context = before,
                            StateUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(trace.CaptureContext())) });
                    }
                }
                finally { querying = false; }
            }
        }
        private static StatusEffectStackData Status(string id, int count) => new StatusEffectStackData { statusId = id, count = count };
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
