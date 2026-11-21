#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class BeforeDrawOrderScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS")?.StartsWith("before-draw-order-", StringComparison.Ordinal) == true;
        internal static bool HealFirst => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "before-draw-order-heal-first";
        internal static bool Prepared { get; private set; }
        internal static int[] Cards { get; private set; } = Array.Empty<int>();
        private static RelicData[] originals = Array.Empty<RelicData>();
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => originals.Length == 0 ? null : Describe();
        private static object Describe() => originals.Select(owner => new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
            Effects = owner.GetEffects().Select(effect => new { Type = effect.GetEffectClassName(), Int = effect.GetParamInt(), Int2 = effect.GetParamInt2(),
                Min = effect.GetParamMinInt(), Max = effect.GetParamMaxInt(), Float = effect.GetParamFloat(), Conditions = effect.GetEffectConditions().Count }).ToArray() }).ToArray();
        private static void Set(object target, string field, object value) => SpawnStatusScalingMechanismBatch.Set(target, field, value);
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (originals.Length != 0) return;
            var save = managers.GetSaveManager(); var all = save.GetAllGameData();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Before-draw ordering requires isolated pre-battle acquisition.");
            var energy = all.GetAllCollectableRelicData().Single(owner => owner.GetID() == DamagedFloorEnergyBattleScenario.OwnerId);
            var healing = all.GetAllCollectableRelicData().Single(owner => owner.GetID() == OriginalChampionStartTurnHealScenario.OwnerId);
            if (energy.GetEffects().Single().GetEffectClassName() != RelicDamagedFloorEnergyModel.Effect ||
                healing.GetEffects().Single().GetEffectClassName() != RelicStartTurnHealModel.Effect)
                throw new InvalidOperationException("Original ordering pair changed.");
            originals = HealFirst ? new[] { healing, energy } : new[] { energy, healing };
            Definition = Describe(); foreach (var owner in originals) save.AddRelic(owner);
            log.LogInfo("BEFORE-DRAW-ORDER-ACQUIRED " + string.Join(",", originals.Select(owner => owner.name)));
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var cards = managers.GetCardManager(); var all = save.GetAllGameData();
            if (originals.Length != 2 || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Before-draw ordering requires isolated deployment.");
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!; var unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "attackDamage", 80); Set(unit, "health", 1000); Set(unit, "size", 1);
            Set(unit, "subtypeKeys", new List<string> { RelicStartTurnHealModel.ChampionSubtype });
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null!);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            var damage = UnityEngine.Object.Instantiate(all.GetAllCardData().First(data => data.GetCardType() == CardType.Spell));
            damage.name = "PojuBeforeDrawWoundControl"; Set(damage, "id", "fc119000-0000-4000-8000-000000001201");
            Set(damage, "cost", 0); Set(damage, "costType", CardData.CostType.Default); Set(damage, "targetless", false); Set(damage, "targetsRoom", true);
            Set(damage, "traits", new List<CardTraitData>()); Set(damage, "startingUpgrades", new List<CardUpgradeData>());
            Set(damage, "triggers", new List<CardTriggerEffectData>()); Set(damage, "effectTriggers", new List<CharacterTriggerData>());
            var hit = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters); hit.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(hit, "paramInt", 1);
            Set(damage, "effects", new List<CardEffectData> { hit });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(damage);
            var controls = new[] { steward, steward, steward, damage, damage, damage }.Select(data => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false)).ToArray();
            foreach (var card in controls) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            Cards = controls.Select(FullBattleTrace.Active!.CardId).ToArray(); Prepared = true;
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("BEFORE-DRAW-ORDER-PREPARED three authored champion defenders, three actual native damage controls; expected first income=" + (HealFirst ? 3 : 4));
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            int index = Array.FindIndex(Cards, id => state.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0) return null;
            int room = index % 3;
            int target = index < 3 ? 0 : state.Spawn.Train.Rooms.Single(floor => floor.RoomIndex == room).Units.Single(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).Id;
            return BattleActionModel.EnumerateSupportedPlays(state, Cards[index]).FirstOrDefault(action => action.RoomIndex == room && action.TargetUnitId == target);
        }
    }
}
#endif
