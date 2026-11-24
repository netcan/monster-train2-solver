#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBlightedTotemWitness
    {
        private static readonly Guid Module = new("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private const string TotemId = "21ad4f07-1568-4461-8be4-6fb2670bd90d";
        private const string TorchId = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
        private const string GuardKey = "ShieldGuardT1_BasicNonAttack";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_BLIGHTED_TOTEM") == "1";
        private static CardState? totemCard, championCard, torchCard;
        private static bool turnAdvanced;
        private static BattleTurnState? before;
        private static PlayCardAction? action;
        private static int totemUnitId, guardUnitId;

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetCurrentDistance() != 0)
                throw new InvalidOperationException("Expected the pinned original Stygian Ring 1 encounter.");
            if (combat.IsPlacementPhase)
            {
                if (totemCard == null)
                {
                    if (!SelectFloor(0)) return false;
                    CardData definition = managers.GetAllGameData().FindCardData(TotemId);
                    if (definition?.name != "BlightedTotem" || definition.GetCost() != 1 || !cards.GetBelowHandSize())
                        throw new InvalidOperationException("Expected unchanged original Blighted Totem and native hand capacity.");
                    totemCard = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
                    Deploy(totemCard, 0);
                }
                else if (championCard == null)
                {
                    if (!SelectFloor(2)) return false;
                    championCard = cards.GetHand().Single(card => card.IsChampionCard());
                    Deploy(championCard, 2);
                }
                else if (!turnAdvanced)
                {
                    turnAdvanced = true;
                    advanceTurn();
                }
                return false;
            }
            if (combat.GetTurnCount() != 1 || totemCard == null || championCard == null || !turnAdvanced)
                throw new InvalidOperationException("Expected the first ordinary turn after original Totem deployment.");
            if (!SelectFloor(0)) return false;

            if (before == null)
            {
                CardData definition = managers.GetAllGameData().FindCardData(TorchId);
                if (definition?.name != "StarterTorch" || definition.GetCost() != 1 || !cards.GetBelowHandSize())
                    throw new InvalidOperationException("Expected unchanged original Starter Torch and native hand capacity.");
                torchCard = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
                var allies = new List<CharacterState>();
                rooms.GetRoom(0).AddCharactersToList(allies, Team.Type.Monsters);
                CharacterState totem = allies.Single(unit => unit.IsAlive && ReferenceEquals(unit.GetSpawnerCard(), totemCard));
                totemUnitId = trace.UnitId(totem);
                var enemies = new List<CharacterState>();
                rooms.GetRoom(0).AddCharactersToList(enemies, Team.Type.Heroes);
                CharacterState guard = enemies.Single(unit => unit.IsAlive && unit.GetSourceCharacterData().name == GuardKey);
                guardUnitId = trace.UnitId(guard);
                before = trace.CaptureDecision();
                CombatUnit original = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == totemUnitId);
                CombatTrigger callback = original.Triggers.Single(trigger => trigger.Kind == "CardSpellPlayed");
                CardActionEffect effect = callback.Effects.Single().Action!;
                if (original.AssetKey != "MonsterBlightedTotem" || original.Health != 1 || original.MaxHealth != 1 ||
                    callback.HasTriggered || effect.Type != "AddStatus" || effect.Target != "Room" ||
                    !effect.AllowEnemy || effect.AllowPlayer || effect.Statuses.Single().Id != "poison" ||
                    effect.Statuses.Single().Stacks != 2 ||
                    before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == guardUnitId).Status("armor")?.Stacks != 25)
                    throw new InvalidOperationException("Expected the original inactive enemy-room Poison callback and natural armored guard.");
                action = new PlayCardAction(trace.CardId(torchCard), 0, targetUnitId: guardUnitId);
                SpawnPoint drop = guard.GetSpawnPoint();
                int handIndex = cards.GetHand().IndexOf(torchCard);
                if (handIndex < 0 || !cards.CanPlayHandCard(torchCard, 0, drop, null, null, out var error))
                    throw new InvalidOperationException("Original Torch was not a legal same-room native play.");
                if (!cards.PlayCard(handIndex, drop, ref error))
                    throw new InvalidOperationException("Native original Torch play failed: " + error);
                return false;
            }

            BattleTurnState actual = trace.CaptureDecision();
            RoomCombatState oldRoom = before.Spawn.Train.Rooms[0], nextRoom = actual.Spawn.Train.Rooms[0];
            CombatUnit oldTotem = oldRoom.Units.Single(unit => unit.Id == totemUnitId);
            CombatUnit nextTotem = nextRoom.Units.Single(unit => unit.Id == totemUnitId);
            CombatUnit oldGuard = oldRoom.Units.Single(unit => unit.Id == guardUnitId);
            CombatUnit nextGuard = nextRoom.Units.Single(unit => unit.Id == guardUnitId);
            bool callbackFired = !oldTotem.Triggers.Single(trigger => trigger.Kind == "CardSpellPlayed").HasTriggered &&
                nextTotem.Triggers.Single(trigger => trigger.Kind == "CardSpellPlayed").HasTriggered;
            bool enemyPoisoned = oldRoom.Units.Where(unit => unit.Team == CombatTeam.Enemy).All(unit => unit.Status("poison") == null) &&
                nextRoom.Units.Where(unit => unit.Team == CombatTeam.Enemy).All(unit => unit.Status("poison")?.Stacks == 2);
            bool friendlyUnaffected = nextRoom.Units.Where(unit => unit.Team == CombatTeam.Player).All(unit => unit.Status("poison") == null);
            bool guardProtected = oldGuard.Status("armor")?.Stacks == 25 && nextGuard.Status("armor")?.Stacks == 22 &&
                nextGuard.Health == oldGuard.Health;
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalBlightedTotemCardSpellPlayed", GameVersion = Application.version,
                GameModuleMvid = Module, TotemId, TorchId, TotemCardInstanceId = trace.CardId(totemCard),
                TotemUnitId = totemUnitId, ShieldGuardId = guardUnitId, CallbackFired = callbackFired,
                EnemyPoisoned = enemyPoisoned, FriendlyUnaffected = friendlyUnaffected, GuardProtected = guardProtected,
                Before = before, Action = action, Actual = actual
            });
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "blighted-totem-spell.mt2f"))) archive.Write(stream);
            if (!callbackFired || !enemyPoisoned || !friendlyUnaffected || !guardProtected)
                throw new InvalidOperationException("Original Blighted Totem did not add two enemy-room Poison on its native spell callback.");
            log.LogInfo("ORIGINAL-BLIGHTED-TOTEM CardSpellPlayed added two Poison to each bottom enemy; friendly room units unchanged.");
            return true;

            bool SelectFloor(int floor)
            {
                if (rooms.GetSelectedRoom() == floor) return true;
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(floor));
                return false;
            }

            void Deploy(CardState card, int floor)
            {
                SpawnPoint drop = rooms.GetRoom(floor).GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("No native placement point for original Totem witness.");
                if (!cards.CanPlayHandCard(card, floor, drop, null, null, out var error) ||
                    !cards.PlayCard(cards.GetHand().IndexOf(card), drop, ref error))
                    throw new InvalidOperationException("Original Totem witness deployment was rejected: " + error);
            }
        }
    }
}
#endif
