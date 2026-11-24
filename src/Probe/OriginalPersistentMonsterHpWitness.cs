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
    internal static class OriginalPersistentMonsterHpWitness
    {
        private const string OwnerId = RelicPersistentMonsterHealthModel.OwnerId;
        private const string OwnerKey = RelicPersistentMonsterHealthModel.OwnerKey;
        private const string ArchiveName = "persistent-monster-hp.mt2f";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_PERSISTENT_MONSTER_HP_WITNESS") == "1";
        internal static bool FullBattle => Environment.GetEnvironmentVariable("MT2_PROBE_PERSISTENT_MONSTER_HP_BATTLE") == "1";
        private static readonly bool aboveMaximumAndDeath = Environment.GetEnvironmentVariable("MT2_PROBE_PERSISTENT_HP_ABOVE_MAX_AND_DEATH") == "1";
        internal static bool Acquired { get; private set; }
        private static CardState? source;
        private static CharacterState? actor;
        private static BattleTurnState? beforeDeployment, beforeHit, afterHit;
        private static PlayCardAction? deployment;
        private static int health, persistentHealth, actorId, written;
        private static bool hitFinished;
        private static CardState? restoredSource;
        private static CharacterState? restoredActor;
        private static BattleTurnState? beforeRestoredBirth, afterRestoredBirth;
        private static PlayCardAction? restoredBirth;
        private static int restoredRoomIndex, restoredActorId, restoredHealth, restoredMaximumHealth, restoredSeedHealth;
        private static bool followupFinished;

        internal static void Acquire(SaveManager save)
        {
            if (Application.version != "2.3.1" ||
                typeof(RelicEffectPersistMonsterHp).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the original 2.3.1 run before battle.");
            MutatorData definition = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("Original persistent-health mutator is missing.");
            if (OriginalPersistentMonsterHpCampaignScenario.Continue)
                OriginalPersistentMonsterHpCampaignScenario.RequireAvailable(save, definition);
            RelicEffectData effect = definition.GetEffects().Single();
            if (definition.name != OwnerKey || definition.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != nameof(RelicEffectPersistMonsterHp) || effect.GetEffectConditions().Count != 0 ||
                effect.GetParamCharacterSubtype()?.IsNone != true)
                throw new InvalidOperationException("Original persistent-health mutator payload changed.");
            save.AddMutator(definition);
            var owner = save.GetMutators().Single();
            if (owner.GetRelicDataID() != OwnerId || owner.GetEffects() is not [RelicEffectPersistMonsterHp])
                throw new InvalidOperationException("Native mutator did not initialize persistent health.");
            Acquired = true;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log)
        {
            var combat = managers.GetCombatManager();
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            if (!Acquired || save.PreviewMode || combat.GetTurnCount() != 0)
                throw new InvalidOperationException("Expected the original live persistent-health deployment.");
            if (source == null)
            {
                source = cards.GetHand().Where(card => card.GetCardType() == CardType.Monster)
                    .OrderByDescending(card => card.GetCardDataID() == MagneticRelayScenario.StewardId).First();
                (beforeDeployment, deployment) = Deploy(source);
                return false;
            }
            if (beforeHit == null)
            {
                actor = FindActor(source, deployment!);
                actorId = trace.UnitId(actor);
                health = actor.GetHP();
                persistentHealth = source.GetCardStateModifiers().GetPersistentHP();
                beforeHit = trace.CaptureDecision();
                if (actor.IsPyreHeart() || health <= 1 || persistentHealth != 0 ||
                    CapturedPersistentHealth(beforeHit, deployment!) != persistentHealth)
                    throw new InvalidOperationException("Expected a live card-backed unit and fresh permanent health value.");
                save.StartCoroutine(Hit());
                return false;
            }
            if (!hitFinished) return false;

            if (afterHit == null)
            {
                afterHit = trace.CaptureDecision();
                written = source.GetCardStateModifiers().GetPersistentHP();
                if (actor!.GetHP() != health - 1 || written != actor.GetHP() ||
                    CapturedPersistentHealth(afterHit, deployment!) != written ||
                    !afterHit.Spawn.Train.Context!.Relics!.Any(owner => owner.DataId == OwnerId &&
                        owner.EffectTypes.SequenceEqual(new[] { nameof(RelicEffectPersistMonsterHp) })))
                    throw new InvalidOperationException("Original damage and captured permanent HP do not match the same source card.");

                restoredSource = cards.GetHand().First(card => card.GetCardDataID() == source.GetCardDataID());
                if (restoredSource.GetCardStateModifiers().GetPersistentHP() != 0)
                    throw new InvalidOperationException("Expected the second original card to have no previously stored HP.");
                restoredRoomIndex = Enumerable.Range(0, rooms.GetNumRooms()).First(index =>
                    index != deployment!.RoomIndex && rooms.GetRoom(index).IsRoomEnabled() && !rooms.GetRoom(index).GetIsPyreRoom());
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(restoredRoomIndex));
                return false;
            }
            if (restoredBirth == null)
            {
                if (rooms.GetSelectedRoom() != restoredRoomIndex) return false;
                restoredSeedHealth = aboveMaximumAndDeath ? checked(actor!.GetMaxHP() + 1) : written;
                restoredSource!.GetCardStateModifiers().SetPersistentHP(restoredSeedHealth);
                (beforeRestoredBirth, restoredBirth) = Deploy(restoredSource!);
                if (CapturedPersistentHealth(beforeRestoredBirth, restoredBirth) != restoredSeedHealth)
                    throw new InvalidOperationException("The seeded hand-card modifier is missing from the recorded root.");
                return false;
            }
            if (afterRestoredBirth == null)
            {
                restoredActor = FindActor(restoredSource!, restoredBirth!);
                restoredActorId = trace.UnitId(restoredActor);
                restoredHealth = restoredActor.GetHP();
                restoredMaximumHealth = restoredActor.GetMaxHP();
                afterRestoredBirth = trace.CaptureDecision();
                if (restoredActor.IsPyreHeart() || restoredHealth != restoredSeedHealth ||
                    restoredMaximumHealth != (aboveMaximumAndDeath ? restoredSeedHealth : actor!.GetMaxHP()) ||
                    CapturedPersistentHealth(afterRestoredBirth, restoredBirth!) != restoredSeedHealth)
                    throw new InvalidOperationException("Paid original birth did not restore the stored HP and expected maximum.");
                save.StartCoroutine(Followup());
                return false;
            }
            if (!followupFinished) return false;

            BattleTurnState afterFollowup = trace.CaptureDecision();
            int finalPersistent = restoredSource!.GetCardStateModifiers().GetPersistentHP();
            int finalHealth = aboveMaximumAndDeath ? 0 : restoredActor!.GetHP();
            if (CapturedPersistentHealth(afterFollowup, restoredBirth!) != finalPersistent)
                throw new InvalidOperationException("Native follow-up was not recorded on the same source card.");
            if (aboveMaximumAndDeath)
            {
                if (finalPersistent != 0 || afterFollowup.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == restoredActorId && unit.Health > 0))
                    throw new InvalidOperationException("Native lethal damage did not clear the live actor and its stored HP.");
            }
            else if (finalHealth != restoredHealth + 1 || finalPersistent != finalHealth ||
                restoredActor!.GetMaxHP() != restoredMaximumHealth)
                throw new InvalidOperationException("Native heal did not write the same original source card's current HP.");

            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalPersistentMonsterHealthOnHit", GameVersion = Application.version,
                GameModuleMvid = typeof(RelicEffectPersistMonsterHp).Module.ModuleVersionId, OwnerId, OwnerKey,
                CardDataId = source.GetCardDataID(), ActorDataId = actor!.GetSourceCharacterData().GetID(), ActorId = actorId,
                Deployment = deployment, BeforeDeployment = beforeDeployment, BeforeHit = beforeHit, AfterHit = afterHit,
                Hit = new { Damage = 1, BeforeHealth = health, AfterHealth = written, BeforePersistent = persistentHealth, AfterPersistent = written },
                RestoredBirth = new { Setup = aboveMaximumAndDeath ? "StoredHpOneAboveOriginalMaximum" : "ObservedHpSeededIntoSecondOwnedHandCard",
                    Before = beforeRestoredBirth,
                    Action = restoredBirth, After = afterRestoredBirth, ActorId = restoredActorId,
                    SeedPersistentHealth = restoredSeedHealth, Health = restoredHealth, MaximumHealth = restoredMaximumHealth },
                Heal = aboveMaximumAndDeath ? null : new { Before = afterRestoredBirth, After = afterFollowup,
                    ActorId = restoredActorId, Amount = 1, BeforeHealth = restoredHealth, AfterHealth = finalHealth,
                    BeforePersistent = restoredSeedHealth, AfterPersistent = finalPersistent },
                Death = !aboveMaximumAndDeath ? null : new { Before = afterRestoredBirth, After = afterFollowup,
                    ActorId = restoredActorId, Damage = restoredHealth, BeforeHealth = restoredHealth,
                    BeforePersistent = restoredSeedHealth, AfterPersistent = finalPersistent }
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, ArchiveName));
            archive.Write(stream);
            log.LogInfo("ORIGINAL-PERSISTENT-MONSTER-HP PASS hitActor=" + actorId + " hit=" + health + "->" + written +
                " restoredActor=" + restoredActorId + " birth=" + restoredHealth + '/' + restoredMaximumHealth +
                " " + (aboveMaximumAndDeath ? "death" : "heal") + "=" + restoredSeedHealth + "->" + finalPersistent);
            return true;

            (BattleTurnState Before, PlayCardAction Action) Deploy(CardState card)
            {
                int roomIndex = rooms.GetSelectedRoom();
                RoomState room = rooms.GetRoom(roomIndex);
                SpawnPoint drop = room.GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("No native room point for persistent-health deployment.");
                BattleTurnState before = trace.CaptureDecision();
                var action = new PlayCardAction(trace.CardId(card), roomIndex, drop.GetIndexInRoom());
                if (!room.IsRoomEnabled())
                    throw new InvalidOperationException("Original persistent-health deployment room is disabled: " + roomIndex);
                if (!cards.CanPlayHandCard(card, roomIndex, drop, null, null, out var error))
                    throw new InvalidOperationException("Original persistent-health deployment card is unavailable in room " + roomIndex + ": " + error);
                if (!cards.PlayCard(cards.GetHand().IndexOf(card), drop, ref error))
                    throw new InvalidOperationException("Original persistent-health deployment was rejected: " + error);
                return (before, action);
            }

            CharacterState FindActor(CardState card, PlayCardAction action)
            {
                var units = new List<CharacterState>();
                rooms.GetRoom(action.RoomIndex).AddCharactersToList(units, Team.Type.Monsters);
                return units.Single(unit => unit.IsAlive && !unit.IsDestroyed && ReferenceEquals(unit.GetSpawnerCard(), card));
            }

            IEnumerator Hit()
            {
                yield return combat.ApplyDamageToTarget(1, actor!, new CombatManager.ApplyDamageToTargetParameters());
                yield return combat.RunTriggerQueue();
                hitFinished = true;
            }

            IEnumerator Followup()
            {
                if (aboveMaximumAndDeath)
                    yield return combat.ApplyDamageToTarget(restoredHealth, restoredActor!, new CombatManager.ApplyDamageToTargetParameters());
                else
                    yield return restoredActor!.ApplyHeal(1);
                yield return combat.RunTriggerQueue();
                followupFinished = true;
            }
        }

        private static int? CapturedPersistentHealth(BattleTurnState state, PlayCardAction action) =>
            state.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.Permanent.PersistentHealth;
    }
}
#endif
