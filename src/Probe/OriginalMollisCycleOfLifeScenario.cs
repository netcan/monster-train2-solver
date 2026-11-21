#if DEVELOPMENT_PROBE
using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMollisCycleOfLifeScenario
    {
        internal const string Name = "original-mollis-cycle-of-life";
        private const string SoulId = "391c87fa-5583-469d-88be-326b0afbf0b7";
        private const string CycleId = "175a7730-7a1c-4da1-97b8-d799e7570396";
        private const string MollisUpgradeId = "a2fbf5f4-ac4f-41d2-bf04-69b251d7dbe8";
        private const string HealthUpgradeId = "3c86d5f0-79a5-4984-8611-c79a871425c4";

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired => cycle != null;
        internal static bool Prepared { get; private set; }
        internal static int CardId { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => cycle == null ? null : Describe();
        internal static CardLifecycleUpgrade? InstalledUpgrade { get; private set; }
        private static CardState? cycle;
        private static CardData cycleData = null!;
        private static SoulData mollis = null!;
        private static bool played;

        private static object Describe()
        {
            var upgrade = new CardUpgradeState();
            upgrade.Setup(mollis.GetSoulCardUpgrade());
            var effects = cycleData.GetEffects();
            return new
            {
                SoulId = mollis.GetID(), SoulKey = mollis.name, Tier = mollis.GetTierLevel(),
                CardId = cycleData.GetID(), CardKey = cycleData.name,
                MainEffects = effects.Select(effect => effect.GetEffectStateName()).ToArray(),
                HealthUpgradeId = effects[0].GetParamCardUpgradeData().GetID(),
                Card = BattleActionProbe.Definition(cycleData),
                SoulUpgrade = CardUpgradeLifecycleProbe.Definition(upgrade)
            };
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            var save = managers.GetSaveManager();
            if (typeof(CardState).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetSelectedSouls().Count != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original Mollis requires an isolated base 2.3.1 checkpoint.");
            var all = save.GetAllGameData();
            mollis = all.FindSoulData(SoulId);
            cycleData = all.FindCardData(CycleId);
            var mollisUpgrade = mollis.GetSoulCardUpgrade();
            var effects = cycleData.GetEffects();
            if (mollis.name != "MollisT3" || mollis.GetTierLevel() != 3 || mollisUpgrade.GetID() != MollisUpgradeId ||
                mollisUpgrade.GetTraitDataUpgrades().Single().GetTraitStateName() != nameof(CardTraitJuice4) ||
                cycleData.name != "CycleOfLife" || cycleData.GetCardType() != CardType.Spell || cycleData.GetTraits().Count != 0 ||
                !effects.Select(effect => effect.GetEffectStateName()).SequenceEqual(new[] {
                    nameof(CardEffectAddTempCardUpgradeToUnits), nameof(CardEffectAddStatusEffect) }) ||
                effects[0].GetParamCardUpgradeData().GetID() != HealthUpgradeId ||
                effects[0].GetParamCardUpgradeData().GetBonusHP() != 10 ||
                effects[1].GetParamStatusEffects().Single() is not { statusId: "spikes", count: 8 })
                throw new InvalidOperationException("The original Mollis or Cycle of Life definition differs.");
            var originalCard = save.AddCardToDeck(cycleData, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            if (mollisUpgrade.GetFilters().Count != 2 || !mollisUpgrade.GetFilters().All(mask => mask.FilterCard(originalCard, managers.GetRelicManager())))
                throw new InvalidOperationException("Cycle of Life failed Mollis's original eligibility filters.");
            save.AddSelectedSoul(mollis);
            var soul = save.GetSelectedSouls().Single(owner => owner.GetRelicDataID() == SoulId);
            SoulHelper.AttachSoulEffectToCard(soul, originalCard, save);
            var installed = originalCard.GetCardStateModifiers().GetCardUpgrades().Single(item => item.GetCardUpgradeDataId() == MollisUpgradeId);
            InstalledUpgrade = CardUpgradeLifecycleProbe.Definition(installed);
            Definition = Describe();
            cycle = originalCard;
            log.LogInfo("ORIGINAL-MOLLIS-CYCLE-ACQUIRED traitsModified=[" + string.Join(",", InstalledUpgrade.TraitsModified) + "]");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var cards = managers.GetCardManager();
            var combat = managers.GetCombatManager();
            if (cycle == null || !combat.IsPlacementPhase ||
                (!cards.GetHand().Contains(cycle) && (!cards.GetDrawPile().Contains(cycle) || !cards.DrawSpecificCard(cycle, useEffectDelay: false))))
                throw new InvalidOperationException("Original Cycle of Life did not reach deployment.");
            // The first normal turn shuffles the draw pile. Retain this input
            // across deployment with the existing native Freeze trait.
            var freeze = new CardTraitData();
            freeze.Setup(nameof(CardTraitFreeze));
            cycle.AddTemporaryTrait(freeze, managers.GetCoreManagers());
            CardId = FullBattleTrace.Active!.CardId(cycle);
            Prepared = true;
            log.LogInfo("ORIGINAL-MOLLIS-CYCLE-PREPARED original Cycle of Life retained for the first normal turn.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (played || state.Spawn.Turn == 0)
                return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => card.InstanceId != CardId);
            if (!state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == CardId) || state.Energy < 3)
                throw new InvalidOperationException("Expected the original three-Ember Cycle of Life on the first normal turn.");
            var room = state.Spawn.Train.Rooms.OrderBy(room => room.RoomIndex).First(room =>
                room.Units.Any(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0));
            var target = room.Units.First(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0);
            played = true;
            // The ordinary replay driver checks native CanPlayHandCard before casting.
            return new PlayCardAction(CardId, room.RoomIndex, targetUnitId: target.Id);
        }
    }
}
#endif
