using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class FloorDamageBattleScenario
    {
        internal const string Name = "floor-damage-battle";
        internal const string DivineId = "83e4e7bd-e285-4239-8e99-6987b49db839";
        internal const string DanteId = "6314e127-6a79-4b81-9d0e-9e95b6dab168";
        internal const string CovenantId = "97c99633-b02b-4f84-a0c1-a9dd37ac1858";
        internal const string CandleId = "913694e9-bace-4480-8691-24f699ae583e";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>(), candles = Array.Empty<int>();
        private static int step;
        private static Sweep? active;
        private static readonly List<Sweep> sweeps = new List<Sweep>();
        private static readonly List<object> timeline = new List<object>();
        private static readonly List<object> statisticRefreshes = new List<object>();
        private static bool inCombat;
        private static object[] definitions = Array.Empty<object>();
        private static AllGameManagers Managers => AllGameManagers.Instance!;
        internal static object Evidence => new { Prepared, ControlCardIds = cards, CandleIds = candles,
            CompletedControlPlays = step, OriginalCards = definitions, Sweeps = sweeps, Timeline = timeline, StatisticRefreshes = statisticRefreshes };
        private sealed class Sweep
        {
            public int Index { get; set; }
            public int Card { get; set; }
            public string DataId { get; set; } = "";
            public int Turn { get; set; }
            public int OriginalRoom { get; set; }
            public int BeforeRoom { get; set; }
            public int AfterRoom { get; set; }
            public int Level { get; set; }
            public int[] Collected { get; set; } = Array.Empty<int>();
            public bool Completed { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!ConditionalActivationProbe.HasVanguard || managers.GetCombatManager().GetTurnCount() != 0 || managers.GetSaveManager().PreviewMode)
                throw new InvalidOperationException("Floor damage requires isolated 2.3.1 deployment.");
            var all = managers.GetAllGameData(); var manager = managers.GetCardManager();
            CardData divine = all.FindCardData(DivineId), dante = all.FindCardData(DanteId), covenant = all.FindCardData(CovenantId);
            definitions = new[] { divine, dante, covenant }.Select(card => (object)new {
                Id = card.GetID(), Key = card.name, Cost = card.GetCost(),
                Effects = card.GetEffects().Select(effect => new { Type = effect.GetEffectStateName(), Int = effect.GetParamInt(),
                    Target = effect.GetTargetMode().ToString(), Team = effect.GetTargetTeamType().ToString() }).ToArray(),
                Traits = card.GetTraits().Select(trait => new { Type = trait.GetTraitStateName(), Int = trait.GetParamInt(),
                    Tracked = trait.GetParamTrackedValue().ToString() }).ToArray(),
                Triggers = card.GetCardTriggers().Select(trigger => new { Kind = trigger.GetTrigger().ToString(),
                    Effects = trigger.GetCardEffects().Select(effect => new { Type = effect.GetEffectStateName(), Int = effect.GetParamInt(),
                        Target = effect.GetTargetMode().ToString(), Team = effect.GetTargetTeamType().ToString() }).ToArray() }).ToArray() }).ToArray();
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData tank = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(tank, "health", 100); Set(tank, "attackDamage", 0); Set(tank, "size", 1);
            Set(tank, "triggers", new List<CharacterTriggerData>()); Set(tank, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(tank, "unitAbility", null!);
            var finisher = UnityEngine.Object.Instantiate(steward); finisher.name = "PojuFloorSweepFinisher";
            Set(finisher, "id", "fc231f20-0000-4000-8000-000000000001");
            var finisherUnit = UnityEngine.Object.Instantiate(tank); finisherUnit.name = "PojuFloorSweepFinisherUnit";
            Set(finisherUnit, "id", "fc231f21-0000-4000-8000-000000000001"); Set(finisherUnit, "attackDamage", 60);
            Set(finisher.GetEffects().Single(), "paramCharacterData", finisherUnit);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Add(finisherUnit);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(finisher);
            CharacterData heavy = all.GetAllCharacterData().Single(unit => unit.name == "MageT1_Junker");
            Set(heavy, "health", 40);
            var death = HealingScenario.HealGold(2, false, false); Set(death, "trigger", CharacterTriggerData.Trigger.OnDeath);
            Set(heavy, "triggers", heavy.GetTriggers().Concat(new[] { death }).ToList());
            Set(heavy, "startingStatusEffects", heavy.GetStartingStatusEffects().Concat(new[] {
                new StatusEffectStackData { statusId = "armor", count = 7 } }).ToArray());
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            manager.GetDrawPile().Clear();
            CardState Add(CardData data, CardPile pile = CardPile.HandPile, bool permanent = false) =>
                manager.AddNewCard(data, pile, false, permanent, animate: false) ?? throw new InvalidOperationException("Unable to add floor damage input.");
            void Freeze(CardState card) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); manager.AddTemporaryTraitToCard(card, freeze, true); }
            CardState reserved = Add(covenant); Freeze(reserved); manager.MoveCardFromHandToTopOfDeck(reserved);
            var fuelData = UnityEngine.Object.Instantiate(all.FindCardData(DevotionSpawnBattleScenario.VenerateId)); fuelData.name = "PojuFloorSweepEnergy";
            Set(fuelData, "id", "fc231f22-0000-4000-8000-000000000001"); Set(fuelData, "cost", 0);
            var energy = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.Monsters); energy.Cheat_SetTargetMode(TargetMode.Room); Set(energy, "paramInt", 1);
            Set(fuelData, "effects", new List<CardEffectData> { energy }); Set(fuelData, "traits", new List<CardTraitData>());
            Set(fuelData, "triggers", new List<CardTriggerEffectData>()); Set(fuelData, "effectTriggers", new List<CharacterTriggerData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(fuelData);
            CardState fuel = Add(fuelData); Freeze(fuel); manager.MoveCardFromHandToTopOfDeck(fuel);
            candles = Enumerable.Range(0, 3).Select(_ => {
                CardState candle = Add(all.FindCardData(CandleId), CardPile.DiscardPile, true);
                manager.GetDiscardPile().Remove(candle); manager.GetExhaustedPile().Add(candle);
                return FullBattleTrace.Active!.CardId(candle);
            }).ToArray();
            CardState[] units = new[] { steward, steward, steward, finisher }.Select(data => Add(data)).ToArray();
            CardState[] spells = new[] { divine, all.FindCardData(DevotionSpawnBattleScenario.VenerateId), divine, dante,
                all.FindCardData(DevotionSpawnBattleScenario.VenerateId), divine }.Select(data => Add(data)).ToArray();
            foreach (CardState card in spells) Freeze(card);
            cards = units.Concat(spells.Take(4)).Concat(new[] { fuel, reserved }).Concat(spells.Skip(4)).Select(FullBattleTrace.Active!.CardId).ToArray();
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData { trackedValue = CardStatistics.TrackedValueType.TimesPlayed,
                cardState = units[0], entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active!.InvalidateCardRules(); Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("FLOOR-DAMAGE-PREPARED original Divine/Dante/Covenant, levels 0/1/2, three permanent exhausted candles, armored enemies and death gold.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length || state.Spawn.Turn < new[] { 0, 0, 0, 0, 1, 2, 3, 4, 4, 4, 5, 6 }[step]) return null;
            if (!state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == cards[step]))
                throw new InvalidOperationException("Missing scheduled floor damage card " + step);
            var action = new PlayCardAction(cards[step], step == 9 ? 0 : step < 3 ? step : 2, playerPosition: step < 4 ? 0 : -1);
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
        private static bool Recording => Enabled && Prepared && !Managers.GetSaveManager().PreviewMode;
        [HarmonyPatch(typeof(CombatManager), "ResolveCombat")]
        private static class CombatPatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(ref IEnumerator __result) { if (Recording) __result = Observe(__result); }
            private static IEnumerator Observe(IEnumerator native)
            {
                inCombat = true;
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); inCombat = false; }
            }
        }
        [HarmonyPatch(typeof(CardStatistics), nameof(CardStatistics.GetStatValue))]
        private static class StatisticPatch
        {
            private static bool Prepare() => Enabled;
            private static readonly FieldInfo stats = AccessTools.Field(typeof(CardStatistics), "deckStats");
            private static int Count(CardStatistics owner) => ((System.Collections.IDictionary)stats.GetValue(owner)).Count;
            private static void Prefix(CardStatistics __instance, out int __state) => __state = Recording && inCombat ? Count(__instance) : -1;
            private static void Postfix(CardStatistics __instance, CardStatistics.StatValueData statValueData, int __state)
            {
                if (__state < 0 || __state == Count(__instance)) return;
                statisticRefreshes.Add(new { Turn = Managers.GetCombatManager().GetTurnCount(), Type = statValueData.trackedValue.ToString(),
                    Before = __state, After = Count(__instance), Stack = new System.Diagnostics.StackTrace(1, false).ToString() });
            }
        }
        [HarmonyPatch]
        private static class SweepPatch
        {
            private static bool Prepare() => Enabled;
            private static MethodBase TargetMethod() => AccessTools.Method(typeof(CardState).Assembly.GetType("CardEffectDamageFloorByFloor"), "ApplyEffect");
            private static void Postfix(CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (Recording) __result = Observe(__result, cardEffectParams); }
            private static IEnumerator Observe(IEnumerator native, CardEffectParams parameters)
            {
                var previous = active;
                var row = new Sweep { Index = sweeps.Count, Card = FullBattleTrace.Active!.CardId(parameters.playedCard),
                    DataId = parameters.playedCard.GetCardDataID(), Turn = Managers.GetCombatManager().GetTurnCount(),
                    OriginalRoom = parameters.selectedRoom, BeforeRoom = Managers.GetRoomManager().GetSelectedRoom(),
                    Level = DevotionProbe.Read(Managers.GetSaveManager(), "GetDevotionLevel"),
                    Collected = parameters.targets.Select(FullBattleTrace.Active.UnitId).ToArray() };
                sweeps.Add(row); active = row;
                try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
                finally { (native as IDisposable)?.Dispose(); row.AfterRoom = Managers.GetRoomManager().GetSelectedRoom(); active = previous; }
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ApplyDamageToTarget))]
        private static class DamagePatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(int damage, CharacterState target, ref IEnumerator __result)
            { if (Recording && active != null) __result = Observe(__result, active, damage, target); }
            private static IEnumerator Observe(IEnumerator native, Sweep sweep, int damage, CharacterState target)
            {
                int id = FullBattleTrace.Active!.UnitId(target), room = target.GetCurrentRoomIndex(), position = target.GetSpawnPoint().GetIndexInRoom();
                timeline.Add(new { Phase = "Hit", Sweep = sweep.Index, UnitId = id, Room = room, Position = position,
                    Damage = damage, Focus = Managers.GetRoomManager().GetSelectedRoom(), Before = FullBattleTrace.Active.CaptureUnit(target) });
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); timeline.Add(new { Phase = "HitDone", Sweep = sweep.Index, UnitId = id,
                    After = FullBattleTrace.Active.CaptureUnit(target) }); }
            }
        }
        [HarmonyPatch(typeof(CharacterState), "CalcBonusDamage")]
        private static class TraitDamagePatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(CharacterState __instance, int damage, int __result)
            {
                if (Recording && active != null) timeline.Add(new { Phase = "TraitDamage", Sweep = active.Index,
                    UnitId = FullBattleTrace.Active!.UnitId(__instance), Input = damage, Damage = __result,
                    Focus = Managers.GetRoomManager().GetSelectedRoom() });
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.RemoveDeadCharacters))]
        private static class RemovalPatch
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(ref IEnumerator __result)
            { if (Recording && active != null) __result = Observe(__result, active); }
            private static IEnumerator Observe(IEnumerator native, Sweep sweep)
            {
                void Mark(string phase) => timeline.Add(new { Phase = phase, Sweep = sweep.Index,
                    Focus = Managers.GetRoomManager().GetSelectedRoom(), Gold = Managers.GetSaveManager().GetGold(),
                    Dead = FullBattleTrace.Active!.CaptureTrain().Rooms.SelectMany(room => room.Units).Where(unit => unit.Health <= 0).Select(unit => unit.Id).ToArray() });
                Mark("Remove");
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); Mark("RemoveDone"); }
            }
        }
    }
}
