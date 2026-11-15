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
    internal static class OriginalPunishedSpikeScenario
    {
        internal const string Name = "original-punished-spike";
        internal const string SpikeId = "1f703e6f-ef57-4904-8d3f-ddd38040ad1a";
        internal const string ImprisonId = "a037328d-1ecb-4657-b741-0909d08b08fe";
        internal const string SentenceId = "de1ef23c-c6bc-4d84-9df5-dac93a4b6a4f";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definitions { get; private set; }
        internal static readonly List<object> Counts = new List<object>();
        internal sealed class Application
        {
            public int CardId { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Application> Applications = new List<Application>();
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Punished Spike requires isolated deployment.");
            Definitions = new[] { SpikeId, ImprisonId, SentenceId }.Select(id => all.FindCardData(id)!).Select(card => new {
                Id = card.GetID(), Key = card.name, Cost = card.GetCost(), CostType = card.GetCostType().ToString(),
                Traits = card.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(), Rule = BattleActionProbe.Definition(card),
                Effects = card.GetEffects().Select(RuleCatalogProbe.Effect).ToArray() }).ToArray();
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!; var anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 1000); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 1000);
            Set(all.GetBalanceData(), "maxHandSize", 32);
            CardData Unit(int suffix, int health)
            {
                var data = UnityEngine.Object.Instantiate(steward); data.name = "PojuSpikeUnit" + suffix;
                Set(data, "id", "fc231b88-0000-4000-8000-" + suffix.ToString("D12"));
                var unit = UnityEngine.Object.Instantiate(anchor); unit.name = data.name + "Body";
                Set(unit, "attackDamage", 0); Set(unit, "health", health);
                var spawn = new CardEffectData("CardEffectSpawnMonster", unit, Team.Type.Monsters); spawn.Cheat_SetTargetMode(TargetMode.Room);
                Set(data, "effects", new List<CardEffectData> { spawn }); definitions.Add(data); return data;
            }
            CardData Control(int suffix, string type, int value, bool room = false, string? status = null, bool enemy = false)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalSpareScenario.PainId)); data.name = "PojuSpikeControl" + suffix;
                Set(data, "id", "fc231b88-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", room); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, room ? Team.Type.None : enemy ? Team.Type.Heroes : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(room ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", status == null ? value : 0);
                if (status != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = value } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(10, "CardEffectGainEnergy", 40, true); var drain = Control(11, "CardEffectAdjustEnergy", -99, true);
            var one = Control(12, "CardEffectGainEnergy", 1, true); var three = Control(13, "CardEffectGainEnergy", 3, true);
            var armor = Control(14, "CardEffectAddStatusEffect", 5, status: "armor");
            var kill = Control(15, "CardEffectDamage", 5000, enemy: true); var blank = Control(16, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing Punished Spike input: " + data.name);
            var spike = all.FindCardData(SpikeId)!;
            var created = new[] { Add(steward), Add(Unit(1, 30)), Add(Unit(2, 30)), Add(Unit(3, 4)), Add(battery),
                Add(all.FindCardData(SentenceId)!), Add(all.FindCardData(ImprisonId)!), Add(drain), Add(spike), Add(one), Add(spike),
                Add(three), Add(armor), Add(spike), Add(three), Add(spike), Add(three), Add(spike), Add(three), Add(spike),
                Add(battery), Add(kill) };
            var damage = new CardUpgradeState(); damage.Setup(); damage.SetAttackDamage(3);
            created[19].ApplyPermanentUpgrade(damage, save, ignoreUpgradeAnimation: true);
            foreach (var card in created.Skip(4))
            { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PUNISHED-SPIKE-PREPARED three unchanged originals; zero/one/three X, once-only zeal, armor, enemy/remote/last-target lethal damage and natural immobile ascent control.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 4 && source.Spawn.Turn < 1 || index == 21 && source.Spawn.Turn < 2) return null;
            int floor = index is 0 or 4 or 20 ? 2 : index is 2 or 17 ? 1 : 0;
            int target = index is 5 or 6 or 15 or 21 ? source.Spawn.Train.Rooms[floor].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id :
                index is 8 or 10 or 12 or 13 or 17 or 19 ? source.Spawn.Train.Rooms[floor].Units.Single(unit =>
                    unit.SpawnerCardId == CardIds[index == 17 ? 2 : index == 19 ? 3 : 1]).Id : 0;
            // This is a fixed native calibration, so only validate the selected
            // action here. The independent checker enumerates every recorded
            // frontier and compares bounded serial/parallel search afterward.
            var action = new PlayCardAction(CardIds[index], floor,
                playerPosition: index <= 3 ? index == 3 ? 1 : 0 : -1, targetUnitId: target);
            var predicted = BattleActionModel.PlayCard(source, action);
            if (!predicted.Supported) throw new InvalidOperationException("Punished Spike input " + index + " unsupported: " + predicted.Reason);
            return action;
        }
        [HarmonyPatch(typeof(CardEffectDamage), nameof(CardEffectDamage.GetEffectApplicationCount))]
        private static class CountPatch
        {
            private static void Postfix(CardEffectParams _unused1, int __result)
            {
                var card = _unused1.playedCard;
                if (!Enabled || !Prepared || AllGameManagers.Instance!.GetSaveManager().PreviewMode || card?.GetCardDataID() != SpikeId) return;
                var trace = FullBattleTrace.Active!; var before = trace.CaptureContext(); int frame = UnityEngine.Time.frameCount;
                int direct = card.GetTraitStates().OfType<CardTraitScalingAddEffectApplications>().Single().GetApplyCount(AllGameManagers.Instance.GetCardStatistics());
                Counts.Add(new { CardId = trace.CardId(card), ActualCount = __result, DirectCount = direct, Context = before,
                    StateUnchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(trace.CaptureContext())) });
            }
        }
        [HarmonyPatch(typeof(CardEffectDamage), nameof(CardEffectDamage.ApplyEffect))]
        private static class ApplicationPatch
        {
            private static void Postfix(CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled && Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode && cardEffectParams.playedCard?.GetCardDataID() == SpikeId)
                    __result = Observe(__result, cardEffectParams);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectParams args)
        {
            var trace = FullBattleTrace.Active!;
            var row = new Application { CardId = trace.CardId(args.playedCard), Targets = args.targets.Select(trace.UnitId).ToArray(), Before = trace.CaptureCanonicalTrain() };
            Applications.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureCanonicalTrain(); }
        }
    }
}
