#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSaviorChampionWitness
    {
        private const string ChampionDataId = "19705414-816d-4549-bcf5-866f338a551f";
        private static readonly Guid Module = new("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly string[] AbilityIds =
        {
            "1cbb4a0b-9ad5-4d05-91e5-d94da55d21e5",
            "4252e8ed-4f8f-474f-80be-622eb6dae7fa",
            "2479c736-4463-4346-8464-d517dae23f40"
        };
        internal static int Rank { get; } = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_SAVIOR_CHAMPION_RANK") ?? "0",
            CultureInfo.InvariantCulture);
        internal static bool Enabled => Rank != 0;
        internal static bool Prepared { get; private set; }
        private static readonly List<object> Upgrades = new();
        private static CardState? frontCard, felCard, otherFloorCard;
        private static BattleTurnState? before;
        private static PlayCardAction? action;
        private static int actorId, sameFloorAllyId, otherFloorAllyId;

        internal static void Prepare(SaveManager save)
        {
            if (Rank is not (2 or 3) || Application.version != "2.3.1" || typeof(CardData).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetCurrentDistance() != 0 || save.GetGameSequence() != SaveData.GameSequence.BattleIntro)
                throw new InvalidOperationException("Expected original 2.3.1 Ring 1 battle intro for Savior II or III.");
            CardState champion = save.GetDeckState().Single(card => card.IsChampionCard() && card.GetCardDataID() == ChampionDataId);
            CardUpgradeTreeData tree = ChampionUpgradeRewardData.GetUpgradeTree(champion.GetSpawnCharacterData(), save.GetBalanceData());
            var path = tree.GetUpgradeTrees().Single(candidate => candidate.GetCardUpgrades()
                .Select(upgrade => upgrade.GetUnitAbilityUpgrade()?.GetID()).SequenceEqual(AbilityIds));
            var tiers = path.GetCardUpgrades();
            var installed = champion.GetCardStateModifiers().GetCardUpgrades();
            if (installed.Count != 0 && (installed.Count != 1 || installed[0].GetCardUpgradeDataId() != tiers[0].GetID()))
                throw new InvalidOperationException("The original champion already has a different upgrade path.");

            foreach (var definition in tiers.Take(Rank))
            {
                var upgrade = champion.GetCardStateModifiers().GetCardUpgrades()
                    .SingleOrDefault(item => item.GetCardUpgradeDataId() == definition.GetID());
                bool alreadyOwned = upgrade != null;
                if (upgrade == null)
                {
                    upgrade = new CardUpgradeState();
                    upgrade.Setup(definition);
                    champion.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
                    champion.RemoveEarlierTreeUpgrades(upgrade, tree);
                }
                if (!champion.GetCardStateModifiers().GetCardUpgrades().Select(item => item.GetCardUpgradeDataId())
                    .SequenceEqual(new[] { definition.GetID() }))
                    throw new InvalidOperationException("Native champion upgrade did not settle to the requested tier.");
                Upgrades.Add(new { Id = definition.GetID(), AssetKey = definition.name, AbilityDataId = definition.GetUnitAbilityUpgrade().GetID(), AlreadyOwned = alreadyOwned });
            }
            Prepared = true;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            var combat = managers.GetCombatManager();
            if (!Prepared || save.PreviewMode)
                throw new InvalidOperationException("Expected a settled decision with the original upgraded Fel.");

            if (combat.IsPlacementPhase)
            {
                if (frontCard == null || felCard == null)
                {
                    if (!SelectFloor(0)) return false;
                    if (frontCard == null)
                        frontCard = Deploy(cards.GetHand().First(card => !card.IsChampionCard() && card.GetCardType() == CardType.Monster));
                    else
                        felCard = Deploy(cards.GetHand().Single(card => card.GetCardDataID() == ChampionDataId));
                }
                else if (otherFloorCard == null)
                {
                    if (!SelectFloor(1)) return false;
                    otherFloorCard = Deploy(cards.GetHand().First(card => !card.IsChampionCard() && card.GetCardType() == CardType.Monster));
                }
                else
                    advanceTurn();
                return false;
            }
            if (combat.GetTurnCount() != 1 || felCard == null || frontCard == null || otherFloorCard == null)
                throw new InvalidOperationException("Expected the first ordinary decision after the three original deployments.");
            if (!SelectFloor(0)) return false;

            if (before == null)
            {
                CharacterState actor = FindActor(0, felCard);
                actorId = trace.UnitId(actor);
                sameFloorAllyId = trace.UnitId(FindActor(0, frontCard));
                otherFloorAllyId = trace.UnitId(FindActor(1, otherFloorCard));
                CardState ability = actor.GetUnitAbilityCardState();
                string abilityId = AbilityIds[Rank - 1];
                if (actor.GetUnitAbility()?.GetID() != abilityId || ability.GetCardDataID() != abilityId ||
                    !actor.CanActivateUnitAbility())
                    throw new InvalidOperationException("Ranked Fel has no matching original available unit ability.");
                action = new PlayCardAction(trace.CardId(ability), 0, activatorUnitId: actorId);
                before = trace.CaptureDecision();
                var allies = Allies(before);
                if (!allies.Select(unit => unit.Id).SequenceEqual(new[] { sameFloorAllyId, actorId }) ||
                    allies.Any(unit => (unit.Status("armor")?.Stacks ?? 0) != 0) ||
                    (allies[1].Status("valor")?.Stacks ?? 0) != 0 ||
                    (OtherFloorAlly(before).Status("armor")?.Stacks ?? 0) != 0)
                    throw new InvalidOperationException("Expected an unarmored front ally, Fel behind it without Valor, and a remote armor control.");
                if (!actor.ActivateUnitAbility(cards))
                    throw new InvalidOperationException("Original ranked Savior activation was rejected.");
                return false;
            }

            BattleTurnState actual = trace.CaptureDecision();
            var afterAllies = Allies(actual);
            if (!afterAllies.Select(unit => unit.Id).SequenceEqual(new[] { actorId, sameFloorAllyId }) ||
                afterAllies[0].Status("valor")?.Stacks != 15 || afterAllies[0].Status("armor")?.Stacks != 15 ||
                (afterAllies[1].Status("armor")?.Stacks ?? 0) != (Rank == 3 ? 15 : 0) ||
                (OtherFloorAlly(actual).Status("armor")?.Stacks ?? 0) != 0)
                throw new InvalidOperationException("Original Savior did not move Fel to the front and grant the expected Valor and Armor.");

            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalSaviorChampionAbility", GameVersion = Application.version, GameModuleMvid = Module,
                Rank, ChampionDataId, ChampionCardInstanceId = trace.CardId(felCard), AbilityDataId = AbilityIds[Rank - 1],
                Upgrades, ActorId = actorId, SameFloorAllyId = sameFloorAllyId, OtherFloorAllyId = otherFloorAllyId,
                Before = before, Action = action, Actual = actual
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "savior-champion-ability.mt2f"));
            archive.Write(stream);
            log.LogInfo("ORIGINAL-SAVIOR-CHAMPION rank=" + Rank + " actor=" + actorId + " allyArmor=" + (Rank == 3 ? 15 : 0));
            return true;

            bool SelectFloor(int index)
            {
                if (rooms.GetSelectedRoom() == index) return true;
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(index));
                return false;
            }

            CardState Deploy(CardState card)
            {
                int index = rooms.GetSelectedRoom();
                var drop = rooms.GetRoom(index).GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("No native monster position for Savior deployment.");
                if (!cards.CanPlayHandCard(card, index, drop, null, null, out var error) ||
                    !cards.PlayCard(cards.GetHand().IndexOf(card), drop, ref error))
                    throw new InvalidOperationException("Native Savior deployment was rejected on floor " + index + ": " + error);
                return card;
            }

            CharacterState FindActor(int floor, CardState source)
            {
                var units = new List<CharacterState>();
                rooms.GetRoom(floor).AddCharactersToList(units, Team.Type.Monsters);
                return units.Single(unit => unit.IsAlive && !unit.IsDestroyed && ReferenceEquals(unit.GetSpawnerCard(), source));
            }

            CombatUnit[] Allies(BattleTurnState state) => state.Spawn.Train.Rooms[0].Units
                .Where(unit => unit.Team == CombatTeam.Player).ToArray();

            CombatUnit OtherFloorAlly(BattleTurnState state) => state.Spawn.Train.Rooms[1].Units
                .Single(unit => unit.Id == otherFloorAllyId);
        }
    }
}
#endif
