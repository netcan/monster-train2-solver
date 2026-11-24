#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalLazarusDevourWitness
    {
        private static readonly Guid Module = new("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private const string AssistantSubtype = "SubtypesData_Assistant";
        private const string DonorDataId = "f4a5935a-ea84-41b8-944f-7d08cd228a58";
        private static readonly string[] AbilityIds =
        {
            "db98983f-fc62-4b7c-9be0-05a1bc270fe1",
            "c2bec532-6a5b-44fa-9c46-0e12510914eb",
            "3eba8cc7-ff26-4ed2-9f59-e7a23d977547"
        };
        internal static int Rank { get; } = int.TryParse(Environment.GetEnvironmentVariable("MT2_PROBE_LAZARUS_DEVOUR_RANK"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int rank) ? rank : 0;
        internal static bool Enabled => Rank is >= 1 and <= 3;
        internal static bool Prepared { get; private set; }
        private static readonly List<object> Upgrades = new();
        private static string? championDataId;
        private static CardState? championCard, donorCard, abilityCard;
        private static BattleTurnState? before;
        private static PlayCardAction? action;
        private static int actorId, donorId;

        internal static void SelectChampion(RunSetupScreen setup, SaveManager save)
        {
            var main = (RunSetupClassLevelInfoUI)AccessTools.Field(typeof(RunSetupScreen), "mainClassInfo").GetValue(setup);
            if (main.ClassData == null) throw new InvalidOperationException("Expected the original main class selection.");
            var matches = Enumerable.Range(0, 2).Where(index =>
            {
                CardData? card = main.ClassData.GetChampionCard(index);
                return card?.GetSpawnCharacterData() is { } character &&
                    ChampionUpgradeRewardData.GetUpgradeTree(character, save.GetBalanceData())?.GetUpgradeTrees()
                        .Any(path => path.GetCardUpgrades().Select(upgrade => upgrade.GetUnitAbilityUpgrade()?.GetID())
                            .SequenceEqual(AbilityIds)) == true;
            }).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected one original Lazarus Devour champion path.");
            int selected = matches[0];
            championDataId = main.ClassData.GetChampionCard(selected).GetID();
            if (main.ChampionIndex != selected)
                AccessTools.Method(typeof(RunSetupClassLevelInfoUI), "SetChampionIndex").Invoke(main, new object[] { selected });
        }

        internal static void Prepare(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(CardData).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetCurrentDistance() != 0 ||
                save.GetGameSequence() != SaveData.GameSequence.BattleIntro || championDataId == null)
                throw new InvalidOperationException("Expected the pinned original Lazarus Ring 1 battle intro.");
            CardState card = save.GetDeckState().Single(item => item.IsChampionCard() && item.GetCardDataID() == championDataId);
            CardUpgradeTreeData tree = ChampionUpgradeRewardData.GetUpgradeTree(card.GetSpawnCharacterData(), save.GetBalanceData());
            CardUpgradeData[] tiers = tree.GetUpgradeTrees().Single(path => path.GetCardUpgrades()
                .Select(upgrade => upgrade.GetUnitAbilityUpgrade()?.GetID()).SequenceEqual(AbilityIds)).GetCardUpgrades().ToArray();
            if (tiers.Length != AbilityIds.Length) throw new InvalidOperationException("Original Devour path lost a rank.");
            var installed = card.GetCardStateModifiers().GetCardUpgrades();
            if (installed.Count != 0 && (installed.Count != 1 || installed[0].GetCardUpgradeDataId() != tiers[0].GetID()))
                throw new InvalidOperationException("The original champion already has another upgrade path.");
            for (int index = 0; index < Rank; index++)
            {
                CardUpgradeData definition = tiers[index];
                CardData skill = definition.GetUnitAbilityUpgrade();
                CardEffectData[] effects = skill.GetEffects().ToArray();
                if (skill.GetID() != AbilityIds[index] || !skill.IsUnitAbility() || skill.GetCost() != 0 ||
                    effects.Length != 2 || effects[0].GetEffectStateName() != "CardEffectTransferAttackFromUnit" ||
                    effects[0].GetTargetMode() != TargetMode.DropTargetCharacter || effects[0].GetParamInt() != 1 ||
                    effects[0].GetAdditionalParamInt() != index + 1 ||
                    effects[0].GetTargetCharacterSubtype().Key != AssistantSubtype ||
                    effects[1].GetEffectStateName() != "CardEffectSacrifice" ||
                    effects[1].GetTargetMode() != TargetMode.LastTargetedCharacters ||
                    effects[1].GetTargetCharacterSubtype().Key != AssistantSubtype ||
                    effects[1].GetParamSubtype().Key != AssistantSubtype)
                    throw new InvalidOperationException("The pinned original Devour ability payload changed at rank " + (index + 1));
                bool alreadyOwned = card.GetCardStateModifiers().GetCardUpgrades()
                    .Any(item => item.GetCardUpgradeDataId() == definition.GetID());
                if (!alreadyOwned)
                {
                    var upgrade = new CardUpgradeState();
                    upgrade.Setup(definition);
                    card.ApplyPermanentUpgrade(upgrade, save, ignoreUpgradeAnimation: true);
                    card.RemoveEarlierTreeUpgrades(upgrade, tree);
                }
                if (!card.GetCardStateModifiers().GetCardUpgrades()
                    .Select(item => item.GetCardUpgradeDataId()).SequenceEqual(new[] { definition.GetID() }))
                    throw new InvalidOperationException("Original Devour rank did not settle on the champion.");
                Upgrades.Add(new { Id = definition.GetID(), AssetKey = definition.name, AbilityDataId = skill.GetID(),
                    Multiplier = index + 1, AlreadyOwned = alreadyOwned });
            }
            Prepared = true;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            var combat = managers.GetCombatManager();
            if (!Prepared || save.PreviewMode) throw new InvalidOperationException("Expected settled original Devour preparation.");
            if (rooms.GetSelectedRoom() != 2)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(2));
                return false;
            }
            if (combat.IsPlacementPhase)
            {
                if (donorCard == null)
                {
                    CardData donor = managers.GetAllGameData().FindCardData(DonorDataId);
                    if (donor?.name != "OverworkedAssistant" || donor.GetCost() != 0 ||
                        donor.GetSpawnCharacterData() is not { } source || source.GetAttackDamage() != 3 ||
                        !source.GetSubtypes().Any(subtype => subtype.Key == AssistantSubtype) || !cards.GetBelowHandSize())
                        throw new InvalidOperationException("Expected the unchanged original positive-attack Assistant donor.");
                    donorCard = Deploy(cards.AddNewCard(donor, CardPile.HandPile, false, false, animate: false));
                }
                else if (championCard == null)
                    championCard = Deploy(cards.GetHand().Single(card => card.GetCardDataID() == championDataId));
                else advanceTurn();
                return false;
            }
            if (combat.GetTurnCount() != 1 || donorCard == null || championCard == null)
                throw new InvalidOperationException("Expected the first hero turn after two original deployments.");
            if (before == null)
            {
                CharacterState actor = Find(championCard), donor = Find(donorCard);
                actorId = trace.UnitId(actor); donorId = trace.UnitId(donor);
                abilityCard = actor.GetUnitAbilityCardState();
                var targets = new List<CharacterState>();
                combat.GetTargetCharacters(targets, abilityCard, rooms.GetRoom(2), actor);
                if (actor.GetUnitAbility()?.GetID() != AbilityIds[Rank - 1] ||
                    abilityCard.GetCardDataID() != AbilityIds[Rank - 1] ||
                    donor.GetAttackDamage() <= 0 || !donor.GetSubtypes().Any(subtype => subtype.Key == AssistantSubtype) ||
                    !targets.Contains(donor) || !actor.CanActivateUnitAbility() || !actor.CanTargetUnitAbilityInRoom(2))
                    throw new InvalidOperationException("Expected a legal original Devour activation and positive donor attack.");
                action = new PlayCardAction(trace.CardId(abilityCard), 2, targetUnitId: donorId, activatorUnitId: actorId);
                before = trace.CaptureDecision();
                if (!cards.PlayAnyCard(abilityCard, out var error, actor, donor.GetSpawnPoint(), ignoreCost: false,
                    discardCard: false, fromDirectPlay: true, characterThatActivatedAbility: actor, roomIndexOverride: 2))
                    throw new InvalidOperationException("Original Devour activation failed: " + error);
                return false;
            }

            BattleTurnState actual = trace.CaptureDecision();
            CombatUnit oldActor = before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == actorId);
            CombatUnit oldDonor = before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == donorId);
            CombatUnit newActor = actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == actorId);
            CombatUnit? newDonor = actual.Spawn.Train.Rooms[2].Units.SingleOrDefault(unit => unit.Id == donorId);
            bool undyingSpent = oldDonor.Status("undying")?.Stacks == 1 && newDonor?.Health == 1 &&
                newDonor.MaxHealth == oldDonor.MaxHealth && newDonor.Status("undying") == null &&
                actual.OtherPiles?.Any(pile => pile.Name == "Standby" &&
                pile.Cards.Any(card => card.InstanceId == trace.CardId(donorCard))) == true;
            bool amountMatched = newActor.Attack - oldActor.Attack == oldDonor.Attack * Rank;
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalLazarusDevourAbility", GameVersion = Application.version,
                GameModuleMvid = Module, Rank, ChampionDataId = championDataId, AbilityDataId = AbilityIds[Rank - 1],
                Upgrades, DonorDataId, ChampionCardInstanceId = trace.CardId(championCard),
                DonorCardInstanceId = trace.CardId(donorCard),
                ActorId = actorId, DonorId = donorId, DonorAttack = oldDonor.Attack,
                DonorHealthBefore = oldDonor.Health, DonorHealthAfter = newDonor?.Health,
                DonorUndyingSpent = undyingSpent, AttackDeltaMatched = amountMatched,
                Before = before, Action = action, Actual = actual
            });
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "lazarus-devour-ability.mt2f"))) archive.Write(stream);
            if (!undyingSpent || !amountMatched || actual.Energy != before.Energy)
                throw new InvalidOperationException("Original Devour did not transfer ranked attack and consume the donor's Undying.");
            log.LogInfo("ORIGINAL-LAZARUS-DEVOUR rank=" + Rank + " actor=" + actorId + " donor=" + donorId +
                " attack=" + oldActor.Attack + "->" + newActor.Attack);
            return true;

            CardState Deploy(CardState card)
            {
                var drop = rooms.GetRoom(2).GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("No native monster position for Devour deployment.");
                if (!cards.CanPlayHandCard(card, 2, drop, null, null, out var error) ||
                    !cards.PlayCard(cards.GetHand().IndexOf(card), drop, ref error))
                    throw new InvalidOperationException("Original Devour deployment was rejected: " + error);
                return card;
            }

            CharacterState Find(CardState card)
            {
                var units = new List<CharacterState>();
                rooms.GetRoom(2).AddCharactersToList(units, Team.Type.Monsters);
                return units.Single(unit => unit.IsAlive && !unit.IsDestroyed && ReferenceEquals(unit.GetSpawnerCard(), card));
            }
        }
    }
}
#endif
