using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalGraftedEquipmentScenario
    {
        internal const string Name = "original-grafted-equipment";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly string[] Originals = { "c2689249-1886-423d-98d0-cd3857289b3e", "dc7ff295-83fd-47e4-93c7-a133e7058ac8",
            "42d238d8-7180-48b0-b0e8-51f2bbb7ffb4", "0a928329-666c-4ce6-a1c3-413977e37e46", "96f46995-23fe-4c0b-8325-54826e95c333" };
        internal static readonly List<object> Resets = new List<object>();
        internal static bool ControlsUnchanged { get; private set; }
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 30);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData victim = UnityEngine.Object.Instantiate(steward); victim.name = "PojuGraftedEquipmentVictim";
            Set(victim, "id", "ac110038-0000-4000-8000-000000000001");
            CharacterData body = UnityEngine.Object.Instantiate(anchor); body.name = "PojuGraftedEquipmentVictimBody"; Set(body, "attackDamage", 1); Set(body, "health", 1);
            var spawn = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters); spawn.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode());
            Set(victim, "effects", new List<CardEffectData> { spawn });
            CardData Control(int suffix, string effectType, int value, TargetMode target, Team.Type team)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(item => item.GetCardType() == CardType.Spell));
                data.name = "PojuGraftedEquipmentControl" + suffix; Set(data, "id", "ac110038-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", target == TargetMode.Room); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectType, null!, team); effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", value);
                Set(data, "effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(victim);
            CardData battery = Control(2, "CardEffectGainEnergy", 10, TargetMode.Room, Team.Type.None),
                remove = Control(3, "CardEffectRemoveEquipment", 0, TargetMode.DropTargetCharacter, Team.Type.Monsters),
                kill = Control(4, "CardEffectDamage", 2, TargetMode.DropTargetCharacter, Team.Type.Monsters),
                consume = Control(5, "CardEffectDiscardHand", 1, TargetMode.Room, Team.Type.None),
                killAnchor = Control(6, "CardEffectDamage", 1000, TargetMode.DropTargetCharacter, Team.Type.Monsters);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(victim.GetID()), Add(battery.GetID()) }
                .Concat(Originals.Select(Add)).Concat(new[] { Add(Originals[4]), Add(remove.GetID()), Add(kill.GetID()), Add(Originals[1]), Add(killAnchor.GetID()), Add(consume.GetID()) }).ToArray();
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            CaptureResets(managers, created[4]);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-GRAFTED-EQUIPMENT-PREPARED five unchanged original equipment definitions, single-slot replacements, explicit removal, equipped host death and detached native ResetTraits controls.");
        }
        private static void CaptureResets(AllGameManagers managers, CardState prototype)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; JObject before = JObject.FromObject(trace.CaptureDecision()), rng = Rng(); int frame = UnityEngine.Time.frameCount;
            foreach (string id in Originals)
            {
                CardState card = CardUpgradeLifecycleProbe.Copy(prototype); card.Setup(managers.GetAllGameData().FindCardData(id)!, managers.GetSaveManager());
                card.GetTraitStates().OfType<CardTraitGraftedEquipment>().Single().Severed = true;
                foreach (int repeat in new[] { 0, 1 })
                {
                    CardInstanceState input = CardModifierProbe.Capture(new[] { card }, _ => 1).Single();
                    card.ResetTraits();
                    Resets.Add(new { Before = input, Actual = CardModifierProbe.Capture(new[] { card }, _ => 1).Single(), Repeat = repeat });
                }
            }
            ControlsUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(before, JObject.FromObject(trace.CaptureDecision())) && JToken.DeepEquals(rng, Rng());
            if (!ControlsUnchanged) throw new InvalidOperationException("Detached graft reset controls changed live decision, RNG or frame.");
        }
        private static JObject Rng()
        {
            var result = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
                if (id != RngId.NonDeterministic && id != RngId.Chatter) result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return result;
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Enumerable.Range(0, ids.Length).Where(i => i != 12 && source.Spawn.Train.Context!.Statistics!.Value(ids[i], "TimesPlayed", "ThisBattle") == 0).DefaultIfEmpty(-1).First();
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            int target = index >= 4 && index <= 11 || index == 13 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[index is 9 or 11 ? 2 : 0]).Id : 0;
            var action = new PlayCardAction(ids[index], index == 1 ? 1 : 0, playerPosition: index < 3 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-GRAFTED-EQUIPMENT-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
