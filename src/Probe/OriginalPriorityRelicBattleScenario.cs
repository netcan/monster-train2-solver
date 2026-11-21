#if DEVELOPMENT_PROBE
using System;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPriorityRelicBattleScenario
    {
        internal const string Name = "original-priority-relic-battle";
        private const string StewardId = "38e70e6a-20f3-4e36-8d31-c5110be07f87";
        private const string ForgeCardId = "87398615-091c-465e-8225-5de370f5953e";
        private const string EquipmentId = "a1375ed2-179f-4ede-9233-b88b604836ac";
        private const string StarterId = "de1ef23c-c6bc-4d84-9df5-dac93a4b6a4f";
        private const string VentId = "a54a1fa9-8cf3-4be2-a9c6-d0e50bb82c7d";
        private static readonly (string Id, string Key, string Effect)[] Owners =
        {
            ("fb6332bb-3e1c-44ba-951b-39fbc8d43601", "NonBannerUnitsHaveDeployable", RelicModel.AddTempUpgrade),
            ("374995fd-7619-4244-9d1f-269429258584", "MoreRares_Relic", RelicModel.AddTempUpgrade),
            ("f141d334-b503-48ff-ac21-5f2ad27105dd", "MoltenManifold", RelicModel.AddTempUpgrade),
            ("d892106c-dbbc-40ac-bf0b-902882cdc69d", "ReduceStarterCost", RelicModel.AddTempUpgrade),
            (OriginalHephsBlueprintsScenario.DataId, OriginalHephsBlueprintsScenario.AssetKey, RelicModel.ModifyForgeUpgrade)
        };
        private static int[] defenders = Array.Empty<int>();
        private static int forgeCard, equipment, starter, vent;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared => Evidence != null;
        internal static object? Evidence { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                throw new InvalidOperationException("The priority relic controls require a fresh 2.3.1 battle.");
            AllGameData data = save.GetAllGameData();
            var originals = Owners.Select(expected =>
            {
                CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == expected.Id);
                RelicEffectData effect = relic.GetEffects().Single();
                if (relic.name != expected.Key || relic.DisallowInDeploymentPhase || effect.GetEffectClassName() != expected.Effect ||
                    effect.GetEffectConditions().Count != 0 || save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == expected.Id))
                    throw new InvalidOperationException("Unexpected original priority relic " + expected.Key);
                return relic;
            }).ToArray();

            int stewards = save.GetDeckState().Count(card => card.GetCardDataID() == StewardId);
            CardData recordedSteward = Find(data, StewardId, "TrainStewardSpear");
            for (int index = stewards; index < 3; index++)
                save.AddCardToDeck(recordedSteward, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            if (!save.GetDeckState().Any(card => card.GetCardDataID() == ForgeCardId))
                save.AddCardToDeck(Find(data, ForgeCardId, "StarterForge"), applyExistingRelicModifiers: false,
                    showAnimation: false, notifyDeckCount: false);
            foreach (CollectableRelicData relic in originals)
                save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-PRIORITY-RELIC-BATTLE-ACQUIRED five unchanged original definitions.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (!Acquired || !managers.GetCombatManager().IsPlacementPhase || save.GetForgePoints() != 0)
                throw new InvalidOperationException("The priority relic controls require the original deployment and an empty Forge meter.");
            var cards = managers.GetCardManager();
            var trace = FullBattleTrace.Active!;
            CardState[] selected = cards.GetHand().Where(card => card.GetCardDataID() == StewardId &&
                cards.IsDeployableCard(card) && !cards.IsBannerCard(card) &&
                card.GetTemporaryCardStateModifiers().HasUpgrade("5ee5c04a-0fd3-4f71-8cc8-15b92d4e1fd7") &&
                card.GetTemporaryCardStateModifiers().HasUpgrade("c62d9641-8948-4c00-b67d-60f0e728c1c1")).Take(3).ToArray();
            if (selected.Length != 3)
                throw new InvalidOperationException("Three original non-banner Stewards must be drawn by the native Deployable relic, with Endless installed.");
            foreach (CardState card in cards.GetHand().ToArray())
                if (!selected.Contains(card)) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (CardState card in selected)
            {
                // Give the controlled instances enough durability to finish the native battle.
                var upgrade = new CardUpgradeState();
                upgrade.Setup();
                upgrade.SetAttackDamage(80);
                upgrade.SetAdditionalHP(1000);
                upgrade.SetCostReduction(card.GetCostWithoutTraits());
                card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
            }
            defenders = selected.Select(trace.CardId).ToArray();

            AllGameData data = save.GetAllGameData();
            CardState Add(string id, string key, CardPile pile) => cards.AddNewCard(Find(data, id, key), pile,
                false, false, animate: false) ?? throw new InvalidOperationException("Could not seed original control " + key);
            forgeCard = trace.CardId(Add(ForgeCardId, "StarterForge", CardPile.HandPile));
            CardState affectedEquipment = Add(EquipmentId, "SpitefulShackles", CardPile.HandPile);
            equipment = trace.CardId(affectedEquipment);
            CardState xSpell = Add(VentId, "Vent", CardPile.DeckPileTop);
            vent = trace.CardId(xSpell);
            CardState starterSpell = Add(StarterId, "StarterSharedSentence", CardPile.DeckPileTop);
            starter = trace.CardId(starterSpell);
            if (!xSpell.GetTemporaryCardStateModifiers().HasUpgrade("da974fcd-8ba4-4fcb-bb62-48eb07c79841") ||
                !starterSpell.GetTemporaryCardStateModifiers().HasUpgrade("cd3d2922-f28b-4950-a042-41e1d9e926ff") ||
                !CardForgeHelper.IsCardOfInterest(affectedEquipment, managers.GetCoreManagers()))
                throw new InvalidOperationException("Original Vent, starter or forge setup lacks its native relic path.");
            save.SetForgeToggleActive(true);
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            trace.InvalidateCardRules();
            Evidence = new { RelicIds = Owners.Select(owner => owner.Id).ToArray(), Defenders = defenders,
                ForgeCard = forgeCard, Equipment = equipment, Starter = starter, Vent = vent };
            log.LogInfo("ORIGINAL-PRIORITY-RELIC-BATTLE-PREPARED native deployment, Forge, starter and X-cost controls.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var context = state.Spawn.Train.Context!;
            bool Played(int card) => context.Statistics!.Value(card, "TimesPlayed", "ThisBattle") > 0;
            int floor = Array.FindIndex(defenders, id => !Played(id));
            if (floor >= 0) return RequiredPlay(defenders[floor], play => play.RoomIndex == floor);

            if (!Played(forgeCard) || !Played(equipment))
            {
                int id = !Played(forgeCard) ? forgeCard : equipment;
                CombatUnit host = state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                    .Single(unit => unit.SpawnerCardId == defenders[0]);
                return RequiredPlay(id, play => play.RoomIndex == 0 && (play.TargetUnitId == 0 || play.TargetUnitId == host.Id));
            }
            foreach (int id in new[] { starter, vent })
            {
                if (Played(id) || !context.Cards.Hand.Any(card => card.InstanceId == id)) continue;
                var occupied = state.Spawn.Train.Rooms.Where(room => room.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0))
                    .Select(room => room.RoomIndex).ToHashSet();
                if (occupied.Count > 0) return RequiredPlay(id, play => occupied.Contains(play.RoomIndex));
            }
            return null;

            PlayCardAction RequiredPlay(int id, Func<PlayCardAction, bool> choose)
            {
                var candidates = BattleActionModel.EnumerateActions(state, id);
                return candidates.SupportedActions.FirstOrDefault(choose) ?? throw new InvalidOperationException(
                    "Original priority relic control has no supported action for " + id + ". " +
                    string.Join(", ", candidates.UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct()));
            }
        }

        internal static bool IsScenarioAction(PlayCardAction action) => defenders.Contains(action.CardInstanceId) ||
            action.CardInstanceId == forgeCard || action.CardInstanceId == equipment ||
            action.CardInstanceId == starter || action.CardInstanceId == vent;

        private static CardData Find(AllGameData data, string id, string key)
        {
            CardData? definition = data.FindCardData(id);
            if (definition == null || definition.GetAssetKey() != key)
                throw new InvalidOperationException("Missing original control " + key);
            return definition;
        }
    }
}
#endif
