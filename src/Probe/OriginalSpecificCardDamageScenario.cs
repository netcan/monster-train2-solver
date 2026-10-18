using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSpecificCardDamageScenario
    {
        internal const string Name = "original-specific-card-damage";
        internal const string CardId = "6314e127-6a79-4b81-9d0e-9e95b6dab168";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool QueriesUnchanged { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static readonly List<object> Queries = new List<object>();
        internal static readonly List<object> Samples = new List<object>();
        private static bool querying;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 60); Set(unit, "health", 500); Set(unit, "size", 1);
            foreach (CharacterData enemy in ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Where(character => character.name == "MageT1_Junker" || character.name == "ShieldGuardT1_BasicNonAttack"))
            {
                Set(enemy, "health", 400);
                Set(enemy, "startingStatusEffects", enemy.GetStartingStatusEffects().Where(status => status.statusId != "armor")
                    .Concat(new[] { new StatusEffectStackData { statusId = "armor", count = 7 } }).ToArray());
            }
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData original = all.FindCardData(CardId)!;
            CardData counted = original.GetTraits().Single(trait => trait.GetTraitStateName() == CardSpecificDamageModel.Trait).GetCardDataParam();
            List<CardState> deck = save.GetDeckState();
            deck.RemoveAll(card => card.GetCardDataID() == counted.GetID());
            for (int index = 0; index < 3; index++) save.AddCardToDeck(counted, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            CardData battery = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            battery.name = "PojuSpecificCardDamageBattery"; Set(battery, "id", "ac110021-0000-4000-8000-000000000001");
            Set(battery, "cost", 0); Set(battery, "costType", CardData.CostType.Default); Set(battery, "targetless", true); Set(battery, "targetsRoom", true);
            Set(battery, "traits", new List<CardTraitData>()); Set(battery, "startingUpgrades", new List<CardUpgradeData>());
            Set(battery, "triggers", new List<CardTriggerEffectData>()); Set(battery, "effectTriggers", new List<CharacterTriggerData>());
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 10);
            Set(battery, "effects", new List<CardEffectData> { energy });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(battery);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(battery.GetID()), Add(CardId), Add(CardId) };
            var damageUpgrade = new CardUpgradeState(); damageUpgrade.Setup(); damageUpgrade.SetAttackDamage(5);
            created[4].ApplyPermanentUpgrade(damageUpgrade, save, ignoreUpgradeAnimation: true);
            foreach (CardState card in created.Skip(2)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            CardState[] originalDeck = deck.ToArray(), related = deck.Where(card => card.GetCardDataID() == counted.GetID()).ToArray(),
                unrelated = deck.Where(card => card.GetCardDataID() != counted.GetID()).ToArray();
            FullBattleTrace.Active!.CaptureDecision(); // Close references to all permanent cards before restoration comparison.
            JToken before = JToken.FromObject(FullBattleTrace.Active.CaptureDecision()); int frame = UnityEngine.Time.frameCount; JObject rng = RngStates();
            querying = true;
            try
            {
                foreach (int count in new[] { 0, 1, 3 })
                {
                    deck.Clear(); deck.AddRange(unrelated); deck.AddRange(related.Take(count));
                    foreach (CardState card in created.Skip(3))
                    foreach (int input in new[] { 0, 99 })
                    {
                        CardTraitScalingAddDamagePerCard trait = card.GetTraitStates().OfType<CardTraitScalingAddDamagePerCard>().Single();
                        CombatContext context = FullBattleTrace.Active.CaptureContext();
                        int actual = trait.OnApplyingDamage(new CardTraitState.ApplyingDamageParameters { damage = input, damageSourceCard = card }, managers.GetCoreManagers());
                        Queries.Add(new { CardId = FullBattleTrace.Active.CardId(card), CountedCardDataId = counted.GetID(), DeckCount = count,
                            InputDamage = input, ActualDamage = actual, Context = context,
                            StateUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(context), JToken.FromObject(FullBattleTrace.Active.CaptureContext())) });
                    }
                }
            }
            finally { deck.Clear(); deck.AddRange(originalDeck); querying = false; }
            QueriesUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(rng, RngStates()) &&
                JToken.DeepEquals(before, JToken.FromObject(FullBattleTrace.Active.CaptureDecision()));
            if (!QueriesUnchanged) throw new InvalidOperationException("Specific-card damage queries failed to restore permanent deck/decision/RNG/frame.");
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SPECIFIC-CARD-DAMAGE-PREPARED original Dante's Inferno, zero/one/three permanent counted cards, input override, +5 upgrade, tower piercing and Consume.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
            if (index >= 3 && !source.Spawn.Train.Rooms.Any(room => room.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0))) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
        }
        [HarmonyPatch(typeof(CardTraitScalingAddDamagePerCard), nameof(CardTraitScalingAddDamagePerCard.OnApplyingDamage))]
        private static class DamagePatch
        {
            private static void Postfix(CardTraitScalingAddDamagePerCard __instance, CardTraitState.ApplyingDamageParameters damageParams, int __result)
            {
                if (!Enabled || !Prepared || querying || AllGameManagers.Instance!.GetSaveManager().PreviewMode || __instance.GetCard().GetCardDataID() != CardId) return;
                Samples.Add(new { CardId = FullBattleTrace.Active!.CardId(__instance.GetCard()), InputDamage = damageParams.damage, ActualDamage = __result,
                    AttackerId = damageParams.attacker == null ? 0 : FullBattleTrace.Active.UnitId(damageParams.attacker),
                    Target = FullBattleTrace.Active.CaptureUnit(damageParams.targetCharacter!), Context = FullBattleTrace.Active.CaptureContext() });
            }
        }
        private static JObject RngStates()
        {
            var states = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId))) if (id != RngId.NonDeterministic && id != RngId.Chatter)
                states[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return states;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
