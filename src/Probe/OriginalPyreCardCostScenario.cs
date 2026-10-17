using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPyreCardCostScenario
    {
        internal const string Name = "original-pyre-health-card-cost";
        private const string SyntheticPrefix = "7f4c8a90-2518-4a41-8c8c-0f5ca0b0a475-";
        private static readonly string[] Labels = ["Affordable", "PyrePaid", "XCost"];
        private static readonly string[] CardIds = [SyntheticPrefix + "01", SyntheticPrefix + "02", SyntheticPrefix + "03"];
        private static readonly HashSet<int> scenarioCardIds = [];
        private static int step;
        private static int initialEnergy;
        private static int initialPyreHp;
        private static int highCost;
        private static int lastPlayedStep = -1;

        internal sealed class Eligibility
        {
            public string Label { get; set; } = "";
            public int CardInstanceId { get; set; }
            public int Cost { get; set; }
            public string CostType { get; set; } = "";
            public int Energy { get; set; }
            public bool Affordable { get; set; }
            public bool RelicTest { get; set; }
            public bool CanPlayHandCard { get; set; }
            public int LastPlayedCost { get; set; }
        }

        internal sealed class Result
        {
            public string Label { get; set; } = "";
            public int CardInstanceId { get; set; }
            public int Energy { get; set; }
            public int PyreHp { get; set; }
            public int LastPlayedCost { get; set; }
        }

        internal sealed class QueuedHit
        {
            public int CardInstanceId { get; set; }
            public int ActorId { get; set; }
            public string Trigger { get; set; } = "";
            public int ParamInt { get; set; }
            public int ParamInt2 { get; set; }
        }

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static readonly List<Eligibility> EligibilityChecks = [];
        internal static readonly List<Result> Results = [];
        internal static readonly List<QueuedHit> QueuedHits = [];
        internal static int ActiveActionCardId { get; private set; }
        internal static object Evidence => new
        {
            OwnerId = RelicPayCardCostWithPyreHealthModel.DataId,
            CardIds,
            InitialEnergy = initialEnergy,
            InitialPyreHp = initialPyreHp,
            AffordableCost = 1,
            HighCost = highCost,
            Eligibility = EligibilityChecks,
            Results,
            QueuedHits
        };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Pyre card-cost relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicPayCardCostWithPyreHealthModel.DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == relic.GetID()))
                throw new InvalidOperationException("Pyre card-cost owner is already present in the test run.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-PYRE-CARD-COST-ACQUIRED unchanged owner=" + relic.GetID() + " scale=1.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            CombatManager combat = managers.GetCombatManager()!;
            if (!Acquired || combat.GetTurnCount() < 1 || managers.GetSaveManager().PreviewMode)
                throw new InvalidOperationException("Pyre card-cost controls require a regular battle turn.");
            FullBattleTrace trace = FullBattleTrace.Active ?? throw new InvalidOperationException("Full battle trace is unavailable.");
            if (trace.PendingTurnIndex != 0)
                throw new InvalidOperationException("Pyre card-cost controls must follow the deployment-to-turn-one comparison.");
            trace.CompleteEndTurn(RoomOutcome.Exchanged);
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            PlayerManager player = managers.GetPlayerManager();
            initialEnergy = player.GetEnergy();
            initialPyreHp = save.GetTowerHP();
            if (initialEnergy < 2 || initialEnergy + 1 >= initialPyreHp)
                throw new InvalidOperationException("Native battle energy or Pyre HP cannot support affordable, X-cost and nonlethal controls.");
            highCost = initialEnergy + 1;

            CardData template = cards.GetAllCards(new List<CardState>()).Select(card => data.FindCardData(card.GetCardDataID()))
                .FirstOrDefault(card => card?.GetCardType() == CardType.Spell && !card.IsUnitAbility() && !card.IsRoomAbility())
                ?? data.GetAllCardData().First(card => card != null && card.GetCardType() == CardType.Spell);
            CardData[] synthetic =
            [
                AddSpell(template, data, cards, 1, 1, CardData.CostType.Default),
                AddSpell(template, data, cards, 2, highCost, CardData.CostType.Default),
                AddSpell(template, data, cards, 3, 0, CardData.CostType.ConsumeRemainingEnergy)
            ];
            scenarioCardIds.Clear();
            for (int index = 0; index < synthetic.Length; index++)
            {
                CardState state = cards.GetAllCards(new List<CardState>()).Single(card => card.GetCardDataID() == synthetic[index].GetID());
                scenarioCardIds.Add(trace.CardId(state));
                CardState? handCard = cards.GetHand().SingleOrDefault(card => card.GetCardDataID() == synthetic[index].GetID());
                if (handCard == null) throw new InvalidOperationException("Synthetic persistent cost control did not enter hand.");
                RecordEligibility(managers, handCard, Labels[index], index == 2);
            }
            CombatRelicState captured = RelicProbe.Capture(managers).Single(owner => owner.DataId == RelicPayCardCostWithPyreHealthModel.DataId);
            if (captured.AssetKey != RelicPayCardCostWithPyreHealthModel.AssetKey ||
                captured.DisallowedInPlacementPhase != false || captured.EffectTypes.Count != 1 ||
                captured.ResourceEffects?.Count != 1 || captured.ResourceEffects[0].ParamInt != 1 ||
                captured.ResourceEffects[0].Conditions.Count != 0)
                throw new InvalidOperationException("Captured Pyre card-cost relic payload changed.");

            step = 0;
            lastPlayedStep = -1;
            Results.Clear();
            QueuedHits.Clear();
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            trace.InvalidateCardRules();
            log.LogInfo("ORIGINAL-PYRE-CARD-COST-PREPARED turn=" + combat.GetTurnCount() + " energy=" + initialEnergy +
                " controls=affordable/underfunded/X-cost; Pyre cost=" + highCost + ".");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (!Prepared) return null;
            if (lastPlayedStep >= 0)
            {
                CaptureResult(lastPlayedStep);
                lastPlayedStep = -1;
                ActiveActionCardId = 0;
            }
            if (step >= CardIds.Length) return null;
            if (source.Spawn.Turn < 1) return null;
            CombatContext context = source.Spawn.Train.Context!;
            CardToken card = context.Cards.Hand.SingleOrDefault(item => item.DataId == CardIds[step])
                ?? throw new InvalidOperationException("A persistent Pyre-cost control is missing from hand: " + Labels[step]);
            PlayCardAction? action = BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(play => play.CardInstanceId == card.InstanceId);
            if (action == null)
                throw new InvalidOperationException("Pyre-cost control has no supported play action: " + Labels[step]);
            lastPlayedStep = step;
            ActiveActionCardId = card.InstanceId;
            step++;
            return action;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => Enabled && scenarioCardIds.Contains(action.CardInstanceId);

        private static void RecordEligibility(AllGameManagers managers, CardState card, string label, bool xCost)
        {
            CardManager cards = managers.GetCardManager()!;
            MonsterManager monsterManager = managers.GetMonsterManager() ?? throw new InvalidOperationException("MonsterManager is unavailable.");
            PlayerManager player = managers.GetPlayerManager();
            RelicManager relics = managers.GetRelicManager();
            RoomState room = managers.GetRoomManager()!.GetRoom(0);
            int energy = player.GetEnergy();
            bool affordable = card.IsAffordable(energy, managers.GetCardStatistics(), monsterManager, relics, room);
            bool relicTest = relics.HasPayCardCostWithoutEmberRelicEffect(card, room, affordable, managers.GetCoreManagers());
            int index = cards.GetHand().IndexOf(card);
            bool canPlay = cards.CanPlayHandCard(card, index, null, null, null, out CommonSelectionBehavior.SelectionError error);
            if (canPlay != (affordable || !xCost) || xCost && relicTest || !xCost && affordable && relicTest ||
                !xCost && !affordable && !relicTest || canPlay && error != CommonSelectionBehavior.SelectionError.None)
                throw new InvalidOperationException("Native CanPlay/RelicTest eligibility differed for " + label + ".");
            EligibilityChecks.Add(new Eligibility
            {
                Label = label, CardInstanceId = FullBattleTrace.Active!.CardId(card),
                Cost = card.GetCost(managers.GetCardStatistics(), monsterManager, relics, room),
                CostType = card.IsConsumeRemainingEnergyCostType()
                    ? nameof(CardData.CostType.ConsumeRemainingEnergy) : nameof(CardData.CostType.Default),
                Energy = energy, Affordable = affordable,
                RelicTest = relicTest, CanPlayHandCard = canPlay, LastPlayedCost = card.GetLastPlayedCost()
            });
        }

        private static void CaptureResult(int resultStep)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            SaveManager save = managers.GetSaveManager();
            CardState card = managers.GetCardManager()!.GetAllCards(new List<CardState>())
                .Single(item => item.GetCardDataID() == CardIds[resultStep]);
            Results.Add(new Result
            {
                Label = Labels[resultStep], CardInstanceId = FullBattleTrace.Active!.CardId(card),
                Energy = managers.GetPlayerManager().GetEnergy(), PyreHp = save.GetTowerHP(),
                LastPlayedCost = card.GetLastPlayedCost()
            });
        }

        private static CardData AddSpell(CardData template, AllGameData gameData, CardManager cards, int ordinal,
            int cost, CardData.CostType costType)
        {
            string id = CardIds[ordinal - 1];
            if (gameData.FindCardData(id) != null) throw new InvalidOperationException("Synthetic Pyre-cost card id already exists.");
            CardData spell = UnityEngine.Object.Instantiate(template);
            spell.name = "PojuPyreCardCostSpell" + ordinal;
            Set(spell, "id", id); Set(spell, "cost", cost); Set(spell, "costType", costType);
            Set(spell, "targetless", true); Set(spell, "targetsRoom", false);
            Set(spell, "isUnitAbility", false); Set(spell, "isRoomAbility", false);
            var persistent = new CardTraitData(); persistent.Setup("CardTraitPersistent");
            Set(spell, "traits", new List<CardTraitData> { persistent });
            Set(spell, "effects", new List<CardEffectData>());
            Set(spell, "effectTriggers", new List<CharacterTriggerData>());
            Set(spell, "triggers", new List<CardTriggerEffectData>());
            Set(spell, "startingUpgrades", new List<CardUpgradeData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(gameData)!).Add(spell);
            CardState? added = cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false);
            if (added == null || !cards.GetHand().Contains(added))
                throw new InvalidOperationException("Synthetic Pyre-cost Spell was not added to hand.");
            return spell;
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != RelicPayCardCostWithPyreHealthModel.AssetKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicPayCardCostWithPyreHealthModel.Effect || effect.GetParamInt() != 1 ||
                effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original PyreHealthForEmberCardCost definition changed.");
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field)!.SetValue(target, value);

        private static int QueueCount(CombatManager combat) =>
            ((System.Collections.ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)!).Count;

        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.QueueTrigger), new[] { typeof(CharacterState), typeof(CharacterTriggerData.Trigger), typeof(CharacterState), typeof(bool), typeof(bool), typeof(CharacterState.FireTriggersData), typeof(int), typeof(CharacterTriggerState) })]
        private static class QueueTriggerPatch
        {
            private static void Prefix(CombatManager __instance, out int __state) => __state = QueueCount(__instance);

            private static void Postfix(CombatManager __instance, int __state, CharacterState character,
                CharacterTriggerData.Trigger trigger, CharacterState.FireTriggersData fireTriggersData)
            {
                if (!Enabled || ActiveActionCardId == 0 || trigger != CharacterTriggerData.Trigger.OnHit ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode || QueueCount(__instance) <= __state)
                    return;
                CharacterState pyre = AllGameManagers.Instance.GetRoomManager()!.GetPyreRoom().GetPyreHeart();
                if (!ReferenceEquals(character, pyre)) return;
                QueuedHits.Add(new QueuedHit
                {
                    CardInstanceId = ActiveActionCardId,
                    ActorId = FullBattleTrace.Active!.UnitId(character),
                    Trigger = trigger.ToString(),
                    ParamInt = fireTriggersData?.paramInt ?? 0,
                    ParamInt2 = fireTriggersData?.paramInt2 ?? 0
                });
            }
        }
    }
}
