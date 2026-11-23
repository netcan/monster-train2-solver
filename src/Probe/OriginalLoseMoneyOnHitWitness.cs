#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalLoseMoneyOnHitWitness
    {
        internal const string FileName = "lose-money-on-hit.mt2f";
        private const string OwnerId = RelicGoldLossOnHitModel.OwnerId;
        private const string OwnerKey = RelicGoldLossOnHitModel.OwnerKey;
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_LOSE_MONEY_ON_HIT") == "1";
        internal static bool Acquired { get; private set; }
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Failure { get; private set; }
        private static CardState? deployedCard;

        internal static void Acquire(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(RelicEffectAddTrigger).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the pinned original 2.3.1 run before combat.");
            MutatorData definition = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("Missing original LoseMoneyOnHit mutator.");
            if (!RelicPercentageStatProbe.ValidLoseMoneyOnHitDefinition(definition))
                throw new InvalidOperationException("Original LoseMoneyOnHit payload changed.");
            save.AddMutator(definition);
            var owner = save.GetMutators().Single();
            if (owner.GetRelicDataID() != OwnerId || owner.GetEffects() is not [RelicEffectAddTrigger])
                throw new InvalidOperationException("Native LoseMoneyOnHit mutator did not initialize.");
            Acquired = true;
        }

        internal static void TryStart(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            if (Started || !Acquired) return;
            var combat = managers.GetCombatManager();
            int turn = combat.GetTurnCount();
            var save = managers.GetSaveManager();
            if (save.PreviewMode) return;
            var rooms = managers.GetRoomManager();
            var cards = managers.GetCardManager();
            if (deployedCard == null)
            {
                int selected = rooms.GetSelectedRoom();
                SpawnPoint drop = rooms.GetRoom(selected).GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("No native room point for the original gold-loss carrier.");
                deployedCard = cards.GetHand().First(card => card.GetCardType() == CardType.Monster &&
                    cards.CanPlayHandCard(card, selected, drop, null, null, out _));
                if (!cards.CanPlayHandCard(deployedCard, selected, drop, null, null, out var error) ||
                    !cards.PlayCard(cards.GetHand().IndexOf(deployedCard), drop, ref error))
                    throw new InvalidOperationException("Native gold-loss carrier deployment was rejected: " + error);
                return;
            }
            if (turn == 0 && save.GetBalanceData().GetDisallowedDeploymentPhaseCharacterTriggers()
                .Contains(CharacterTriggerData.Trigger.OnHit))
            {
                advanceTurn();
                return;
            }
            var actors = new List<CharacterState>();
            for (int floor = 0; floor < rooms.GetNumRooms(); floor++)
                rooms.GetRoom(floor).AddCharactersToList(actors, Team.Type.Monsters);
            CharacterState? target = actors.FirstOrDefault(unit => unit.IsAlive && !unit.IsDestroyed && !unit.IsPyreHeart() &&
                unit.GetHP() > 3 && ReferenceEquals(unit.GetSpawnerCard(), deployedCard) &&
                unit.GetDamageToTarget(1, null, null, out _) == 1);
            if (target == null)
                throw new InvalidOperationException("Expected one eligible stock player unit for the gold-loss component.");
            int room = target.GetCurrentRoomIndex();
            int actor = trace.UnitId(target);
            Started = true;
            save.StartCoroutine(Observe());

            IEnumerator Observe()
            {
                var player = managers.GetPlayerManager();
                yield return player.AdjustGold(2 - save.GetGold());
                var rows = new List<object>();
                var observations = new List<(int HealthLost, int GoldBefore, int GoldAfter)>();
                for (int index = 0; index < 3; index++)
                {
                    BattleTurnState before = trace.CaptureDecision();
                    int health = target.GetHP(), gold = save.GetGold();
                    yield return combat.ApplyDamageToTarget(1, target, new CombatManager.ApplyDamageToTargetParameters());
                    yield return combat.RunTriggerQueue();
                    BattleTurnState actual = trace.CaptureDecision();
                    rows.Add(new { Before = before, Actual = actual, HealthBefore = health, HealthAfter = target.GetHP(),
                        GoldBefore = gold, GoldAfter = save.GetGold() });
                    observations.Add((health - target.GetHP(), gold, save.GetGold()));
                }
                using var archive = NativeFixtureCapture.Capture(new
                {
                    Schema = 1, Boundary = "NativeLoseMoneyOnHit", GameVersion = Application.version,
                    GameModuleMvid = Module, OwnerId, OwnerKey, RoomIndex = room, ActorId = actor, Damage = 1, Rows = rows
                });
                using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName));
                archive.Write(stream);
                log.LogInfo("ORIGINAL-LOSE-MONEY-ON-HIT OBSERVED turn=" + turn + " finalGold=" + save.GetGold());
                if (observations.SequenceEqual(new[] { (1, 2, 1), (1, 1, 0), (1, 0, 0) }))
                    Completed = true;
                else Failure = "Native LoseMoneyOnHit did not record the pinned health and gold boundaries.";
            }
        }
    }
}
#endif
