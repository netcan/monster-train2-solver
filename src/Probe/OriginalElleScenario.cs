#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalElleScenario
    {
        internal const string Name = "original-elle";
        private const string TorchId = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        internal static object? Acquisition { get; private set; }
        internal static object? Witness { get; private set; }

        private static SoulData? original;
        private static CardState? carrier;
        private static int carrierId;
        private static int[] defenders = Array.Empty<int>();

        private sealed class DeckCard
        {
            public CardState Native { get; }
            public int Slot { get; }
            public int Id { get; }
            public string DataId { get; }
            public bool IsChampion { get; }
            public bool Eligible { get; }
            public string[] UpgradeIds { get; }

            internal DeckCard(CardState card, int slot, bool eligible)
            {
                Native = card;
                Slot = slot;
                Id = FullBattleTrace.Active!.CardId(card);
                DataId = card.GetCardDataID();
                IsChampion = card.IsChampionCard();
                Eligible = eligible;
                UpgradeIds = card.GetCardStateModifiers().GetCardUpgrades().Select(upgrade => upgrade.GetCardUpgradeDataId()).ToArray();
            }

            internal object Evidence() => new { Slot, Id, DataId, IsChampion, Eligible, UpgradeIds };
        }

        private static DeckCard[] ReadDeck(SaveManager save, CardUpgradeMaskData mask, RelicManager relics) =>
            save.GetDeckState().Select((card, index) => new DeckCard(card, index, !card.IsChampionCard() && mask.FilterCard(card, relics))).ToArray();

        private static UnityRng RewardsRng()
        {
            var words = RngCalibration.Words(RandomManager.GetState(RngId.Rewards));
            return new UnityRng(words[0], words[1], words[2], words[3]);
        }

        private static object Describe(SoulData soul)
        {
            var effect = soul.GetEffects().Single(value => value.GetEffectClassName() == nameof(RelicEffectCardUpgradeRandomCardsInDeck));
            var pool = effect.GetParamCardUpgradePool();
            return new { Id = soul.GetID(), Key = soul.name, Tier = soul.GetTierLevel(), Placement = soul.DisallowInDeploymentPhase,
                Effects = soul.GetEffects().Select(value => value.GetEffectClassName()).ToArray(), Count = effect.GetParamInt(),
                Mask = effect.GetParamCardUpgradeMaskData().name, Pool = pool.name,
                Upgrades = pool.GetAllChoices(new List<CardUpgradeData>()).Select(upgrade => new { Id = upgrade.GetID(), Key = upgrade.name,
                    Cast = upgrade.GetCardTriggerUpgrades().Where(trigger => trigger.GetTrigger() == CardTriggerType.OnCast)
                        .SelectMany(trigger => trigger.GetCardEffects()).Select(action => new { Type = action.GetEffectStateName(),
                            Target = action.GetTargetMode().ToString(), Amount = action.GetParamInt() }).ToArray() }).ToArray() };
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (original != null) return;
            var save = managers.GetSaveManager();
            var relics = managers.GetRelicManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetSelectedSouls().Count != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original Elle requires a fresh isolated run.");
            int tier = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_ELLE_TIER")!);
            string id = tier switch
            {
                1 => "52a5b3a3-f00e-480b-ac86-2349dd127b4f",
                2 => "8e6a9fa8-87ff-4825-9715-96c6ce56d365",
                3 => "a9024031-7d28-4fe7-8956-8ff02194a42a",
                _ => throw new InvalidOperationException("Expected Elle tier 1, 2 or 3.")
            };
            var all = save.GetAllGameData();
            var soul = all.FindSoulData(id)!;
            var effect = soul.GetEffects().Single(value => value.GetEffectClassName() == nameof(RelicEffectCardUpgradeRandomCardsInDeck));
            var mask = effect.GetParamCardUpgradeMaskData();
            var pool = effect.GetParamCardUpgradePool();
            if (soul.name != "ElleT" + tier || soul.GetTierLevel() != tier || effect.GetParamInt() != tier + 1 ||
                mask.name != "OnlyStarterCards" || pool.name != "Elle_UpgradePool")
                throw new InvalidOperationException("The original Elle acquisition payload changed.");
            var choices = pool.GetAllChoices(new List<CardUpgradeData>());
            var upgrade = choices.Single();
            if (upgrade.name != "Elle_UpgradeDrawAndEmber") throw new InvalidOperationException("The original Elle upgrade pool changed.");
            var torch = all.FindCardData(TorchId)!;
            if (torch.name != "StarterTorch" || torch.GetEffects().Single().GetEffectStateName() != nameof(CardEffectDamage))
                throw new InvalidOperationException("The original Torch control changed.");

            // Keep a small pool of distinct owned instances of the same known
            // starter spell. Champions and other ineligible cards stay as controls.
            foreach (var card in save.GetDeckState().ToArray())
                if (!card.IsChampionCard() && card.GetCardDataID() != TorchId && mask.FilterCard(card, relics))
                    save.RemoveCardFromDeck(card);
            while (save.GetDeckState().Count(card => card.GetCardDataID() == TorchId) < 4)
                save.AddCardToDeck(torch, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            var before = ReadDeck(save, mask, relics);
            if (before.Count(card => card.Eligible) < 4 || before.Where(card => card.Eligible).Any(card => card.DataId != TorchId) ||
                !before.Any(card => card.IsChampion && !card.Eligible))
                throw new InvalidOperationException("The isolated native Elle test needs eligible Torch and excluded Champion controls.");
            UnityRng beforeRng = RewardsRng();
            save.AddSelectedSoul(soul);
            var state = save.GetSelectedSouls().Single(owner => owner.GetRelicDataID() == id);
            SoulHelper.AttachSoulEffect(state, playerInitiated: false, relics, save);
            var after = ReadDeck(save, mask, relics);
            UnityRng afterRng = RewardsRng();
            var changed = after.Where(card => card.UpgradeIds.Contains(upgrade.GetID()) &&
                !before[card.Slot].UpgradeIds.Contains(upgrade.GetID())).ToArray();
            if (changed.Length != tier + 1 || changed.Any(card => !card.Eligible || card.DataId != TorchId) || beforeRng.Equals(afterRng))
                throw new InvalidOperationException("The original Elle attachment did not permanently upgrade the expected native starter instances.");

            SoulHelper.AttachSoulEffect(state, playerInitiated: false, relics, save);
            var afterReattach = ReadDeck(save, mask, relics);
            UnityRng reattachRng = RewardsRng();
            if (!afterRng.Equals(reattachRng) || afterReattach.Length != after.Length || afterReattach.Any(card =>
                !ReferenceEquals(card.Native, after[card.Slot].Native) || !card.UpgradeIds.SequenceEqual(after[card.Slot].UpgradeIds)))
                throw new InvalidOperationException("Repeated original Elle attachment must preserve native upgrades and Rewards RNG.");
            original = soul;
            carrier = changed[0].Native;
            Definition = Describe(soul);
            Acquisition = new { Before = new { RewardsRng = beforeRng, Cards = before.Select(card => card.Evidence()).ToArray() },
                After = new { RewardsRng = afterRng, Cards = after.Select(card => card.Evidence()).ToArray() },
                AfterReattach = new { RewardsRng = reattachRng, Cards = afterReattach.Select(card => card.Evidence()).ToArray() },
                UpgradedIds = changed.Select(card => card.Id).ToArray() };
            log.LogInfo($"ORIGINAL-ELLE-ACQUIRED {soul.name} permanently upgraded {changed.Length} native cards.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var cards = managers.GetCardManager();
            if (original == null || carrier == null || !managers.GetCombatManager().IsPlacementPhase ||
                !cards.GetAllCards(new List<CardState>()).Any(card => ReferenceEquals(carrier, card)))
                throw new InvalidOperationException("An actually upgraded native Elle carrier must enter deployment.");
            var controls = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, 3, 1);
            if (!cards.DrawSpecificCard(carrier, useEffectDelay: false))
                throw new InvalidOperationException("Could not draw the owned Elle-upgraded Torch.");
            carrierId = FullBattleTrace.Active!.CardId(carrier);
            defenders = controls.Select(FullBattleTrace.Active!.CardId).ToArray();
            Witness = new { CarrierId = carrierId, CarrierDataId = carrier.GetCardDataID(), DefenderIds = defenders };
            Prepared = true;
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            log.LogInfo("ORIGINAL-ELLE-PREPARED original upgraded Torch and deployment defenders.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var statistics = state.Spawn.Train.Context!.Statistics!;
            for (int floor = 0; floor < defenders.Length; floor++)
                if (statistics.Value(defenders[floor], "TimesPlayed", "ThisBattle") == 0)
                    return BattleActionModel.EnumerateSupportedPlays(state, defenders[floor]).First(action => action.RoomIndex == floor);
            if (statistics.Value(carrierId, "TimesPlayed", "ThisBattle") > 0) return null;
            int target = state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                .Single(unit => unit.SpawnerCardId == defenders[0]).Id;
            return BattleActionModel.EnumerateSupportedPlays(state, carrierId).First(action => action.RoomIndex == 0 && action.TargetUnitId == target);
        }

        internal static bool IsScenarioAction(PlayCardAction action) => action.CardInstanceId == carrierId || defenders.Contains(action.CardInstanceId);
    }
}
#endif
