#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSproutWitness
    {
        internal const string FileName = "sprout-t1-hivemind.mt2f";
        private const string OwnerId = "89f86b55-f867-42d4-9128-0ebc6c82bcaf";
        private const string UpgradeId = "7bdad45c-2981-452c-ac06-3aa5137f8ce4";
        private const string CarrierCardId = "b9788a77-50da-4654-acfa-960c23649984";
        private const string KillerStatus = "hivemindkiller";
        private const string NativeGrowth = "RoomStateHivemindKillerModifierUpgrade";
        private const int CarrierRoom = 2;
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly List<Observation> observations = new();
        private static bool requestedFloor, deployed;
        private static BattleTurnState? before;
        private static int firstWaveObservation;
        internal static bool Acquired { get; private set; }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_SPROUT_WITNESS") == "1";

        internal static void Acquire(SaveManager save)
        {
            if (UnityEngine.Application.version != "2.3.1" || typeof(CardState).Assembly.ManifestModule.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.GetCurrentDistance() != 3 ||
                save.GetCurrentScenarioData()?.name != "SoulSavior_R2_Battle_TroopBuffFeed" ||
                save.GetSelectedSouls().Any(owner => owner.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Expected the original TroopBuffFeed checkpoint before adding Sprout.");
            SoulData soul = save.GetAllGameData().FindSoulData(OwnerId);
            CardUpgradeData upgrade = soul.GetSoulCardUpgrade();
            if (soul.name != "SproutT1" || soul.GetTierLevel() != 1 ||
                soul.GetEffects() is not [var effect] || effect.GetEffectClassName() != nameof(RelicEffectCardUpgrade) ||
                effect.GetParamCardType() != CardType.Monster || effect.GetParamInt() != 0 ||
                upgrade.GetID() != UpgradeId || upgrade.GetCharacterTriggerUpgrades().Count != 0 ||
                upgrade.GetStatusEffectUpgrades() is not [{ statusId: KillerStatus, count: 1 }])
                throw new InvalidOperationException("The original SproutT1 binding payload changed.");

            CardState card = save.GetDeckState().Single(candidate => candidate.GetCardDataID() == CarrierCardId);
            save.AddSelectedSoul(soul);
            SoulState owner = save.GetSelectedSouls().Single(item => item.GetRelicDataID() == OwnerId);
            SoulHelper.AttachSoulEffectToCard(owner, card, save);
            if (!card.GetCardStateModifiers().HasUpgrade(UpgradeId))
                throw new InvalidOperationException("The original Soul Helper did not bind SproutT1 to the native carrier.");
            Acquired = true;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, Action advanceTurn)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager();
            RoomManager rooms = managers.GetRoomManager();
            CardManager cards = managers.GetCardManager();
            if (!Acquired || save.PreviewMode) return false;
            int turn = combat.GetTurnCount();
            if (turn == 0 && !deployed)
            {
                if (rooms.GetSelectedRoom() != CarrierRoom)
                {
                    if (!requestedFloor)
                    {
                        requestedFloor = true;
                        save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(CarrierRoom));
                    }
                    return false;
                }
                CardState card = cards.GetHand().Single(candidate => candidate.GetCardDataID() == CarrierCardId &&
                    candidate.GetCardStateModifiers().HasUpgrade(UpgradeId));
                SpawnPoint point = rooms.GetRoom(CarrierRoom).GetMonsterPoint(0);
                if (!cards.CanPlayHandCard(card, CarrierRoom, point, null, null, out var error) ||
                    !cards.PlayCard(cards.GetHand().IndexOf(card), point, ref error))
                    throw new InvalidOperationException("The original Sprout carrier could not deploy: " + error);
                deployed = true;
                return false;
            }
            if (turn == 0)
            {
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                advanceTurn();
                return false;
            }
            CharacterState carrier = managers.GetMonsterManager().GetCharactersReadOnly().Single(IsCarrier);
            if (carrier.GetCurrentRoomIndex() != CarrierRoom || carrier.GetStatusEffectStacks(KillerStatus) != 1)
                throw new InvalidOperationException("The active native Sprout carrier lost its room or status.");
            if (turn == 1)
            {
                before = trace.CaptureDecision();
                firstWaveObservation = observations.Count;
                advanceTurn();
                return false;
            }

            BattleTurnState actual = trace.CaptureDecision();
            int expected = actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(actor => actor.Team == CombatTeam.Enemy && actor.Health > 0)
                .Sum(actor => actor.Status("hivemind")?.Stacks ?? 0);
            int carrierUnitId = trace.UnitId(carrier);
            Observation? final = observations.Skip(firstWaveObservation).LastOrDefault(row => row.ActorId == carrierUnitId);
            if (turn != 2 || before == null || expected <= 0 || final == null || final.TowerStacks != expected ||
                final.After.ModifierAttack != expected || ReadGrowth(carrier).ModifierAttack != expected ||
                final.After.TotalAttack - final.Before.TotalAttack !=
                    final.After.ModifierAttack - final.Before.ModifierAttack)
                throw new InvalidOperationException("The native Sprout modifier did not track the original Hivemind wave.");
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "NativeSproutT1Hivemind", GameVersion = UnityEngine.Application.version,
                GameModuleMvid = Module, OwnerId, UpgradeId, CarrierCardId, CarrierRoom, CarrierUnitId = carrierUnitId,
                Before = before, FirstWaveObservation = firstWaveObservation, Observations = observations, Actual = actual
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName));
            document.Write(output);
            return true;
        }

        private static bool IsCarrier(CharacterState actor) => actor.GetSpawnerCard()?.GetCardDataID() == CarrierCardId &&
            actor.GetSpawnerCard()?.GetCardStateModifiers().HasUpgrade(UpgradeId) == true;

        private static GrowthState ReadGrowth(CharacterState carrier) => new(
            carrier.GetAppliedCardUpgrades().SingleOrDefault(upgrade => upgrade.GetCardUpgradeDataId() == NativeGrowth)?.GetAttackDamage(),
            carrier.GetAttackDamage(), RngCalibration.Words(RandomManager.GetState(RngId.Battle)),
            RngCalibration.Words(RandomManager.GetState(RngId.BattleTest)), RngCalibration.Words(RandomManager.GetState(RngId.CardDraw)));

        private sealed class GrowthState(int? modifierAttack, int totalAttack, uint[] battle, uint[] battleTest, uint[] cardDraw)
        {
            public int? ModifierAttack { get; } = modifierAttack;
            public int TotalAttack { get; } = totalAttack;
            public uint[] Battle { get; } = battle;
            public uint[] BattleTest { get; } = battleTest;
            public uint[] CardDraw { get; } = cardDraw;
        }

        private sealed class Observation(int turn, int actorId, int towerStacks, GrowthState before, GrowthState after)
        {
            public int Turn { get; } = turn;
            public int ActorId { get; } = actorId;
            public int TowerStacks { get; } = towerStacks;
            public GrowthState Before { get; } = before;
            public GrowthState After { get; } = after;
        }

        [HarmonyPatch(typeof(RoomStateHivemindKillerModifier), "SyncUnitUpgradeStats")]
        private static class GrowthPatch
        {
            private static void Postfix(CharacterState character, int hivemindStacks, ref IEnumerator __result)
            {
                if (Enabled && Acquired && FullBattleTrace.Active is FullBattleTrace trace && IsCarrier(character) &&
                    !AllGameManagers.Instance.GetSaveManager().PreviewMode)
                    __result = Observe(__result, trace, character, hivemindStacks);
            }

            private static IEnumerator Observe(IEnumerator native, FullBattleTrace trace, CharacterState actor, int stacks)
            {
                int turn = AllGameManagers.Instance.GetCombatManager().GetTurnCount();
                int id = trace.UnitId(actor);
                GrowthState before = ReadGrowth(actor);
                try { while (native.MoveNext()) yield return native.Current; }
                finally { (native as IDisposable)?.Dispose(); }
                observations.Add(new Observation(turn, id, stacks, before, ReadGrowth(actor)));
            }
        }
    }
}
#endif
