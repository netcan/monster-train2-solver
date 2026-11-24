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
    internal static class OriginalGluttonOnFeedWitness
    {
        private static readonly Guid Module = new("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private const string GluttonId = "aa94ef0c-0fd8-4d42-992f-25345d3717b5";
        private const string TorchId = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
        private const string MinerId = "536e6de0-7ccf-412a-9a4e-ec55ab3988e5";
        private const string FeastId = "236215da-c1dd-40b7-8793-27ce327a7e78";
        private const string Guard = "ShieldGuardT1_BasicNonAttack";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_GLUTTON_ONFEED") == "1";
        private static CardState? gluttonCard, championCard, torchCard, minerCard, feastCard;
        private static bool turnAdvanced;
        private static int gluttonId, guardId;
        private static ActionRecord? pending;
        private static readonly List<ActionRecord> Steps = new();

        private sealed class ActionRecord
        {
            public string Label { get; set; } = "";
            public BattleTurnState Before { get; set; } = null!;
            public PlayCardAction Action { get; set; } = null!;
            public BattleTurnState Actual { get; set; } = null!;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetCurrentDistance() != 0)
                throw new InvalidOperationException("Expected the pinned original Ring 1 Glutton encounter.");
            if (combat.IsPlacementPhase)
            {
                if (gluttonCard == null)
                {
                    if (!SelectFloor(0)) return false;
                    gluttonCard = Deal(GluttonId, "FeederDamageEnemiesUnit");
                    Deploy(gluttonCard, 0);
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
            if (combat.GetTurnCount() != 1 || gluttonCard == null || championCard == null || !turnAdvanced)
                throw new InvalidOperationException("Expected the first ordinary turn after original Glutton deployment.");
            if (!SelectFloor(0)) return false;

            if (pending != null)
            {
                pending.Actual = trace.CaptureDecision();
                Steps.Add(pending);
                pending = null;
            }
            if (Steps.Count == 0)
            {
                torchCard = Deal(TorchId, "StarterTorch");
                minerCard = Deal(MinerId, "MorselMiner");
                feastCard = Deal(FeastId, "Feast");
                CharacterState glutton = FindGlutton();
                gluttonId = trace.UnitId(glutton);
                BattleTurnState before = trace.CaptureDecision();
                CombatUnit source = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == gluttonId);
                CombatUnit guard = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == Guard);
                guardId = guard.Id;
                if (source.AssetKey != "MonsterGlutton" || source.Health != 50 || source.MaxHealth != 50 ||
                    source.Triggers.Single(trigger => trigger.Kind == "OnFeed").Effects.Count != 2 ||
                    guard.Status("armor")?.Stacks != 25)
                    throw new InvalidOperationException("Expected original Glutton and the natural armored first-wave guard.");
                Play(torchCard, glutton.GetSpawnPoint(), new PlayCardAction(trace.CardId(torchCard), 0,
                    targetUnitId: gluttonId), "Torch", before);
                return false;
            }
            if (Steps.Count == 1)
            {
                BattleTurnState before = trace.CaptureDecision();
                CombatUnit glutton = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == gluttonId);
                if (glutton.Health != 47 || glutton.MaxHealth != 50)
                    throw new InvalidOperationException("Original Torch did not wound Glutton by three health.");
                SpawnPoint drop = rooms.GetRoom(0).GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("No native room slot behind Glutton for the original Morsel.");
                Play(minerCard!, drop, new PlayCardAction(trace.CardId(minerCard!), 0,
                    playerPosition: drop.GetIndexInRoom()), "Miner", before);
                return false;
            }
            if (Steps.Count == 2)
            {
                BattleTurnState before = trace.CaptureDecision();
                CombatUnit glutton = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == gluttonId);
                CombatUnit guard = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == guardId);
                if (glutton.Health != 47 || glutton.MaxHealth != 50 || guard.Status("armor")?.Stacks != 25 ||
                    !before.Spawn.Train.Rooms[0].Units.Any(unit => unit.SpawnerCardId == trace.CardId(minerCard!)))
                    throw new InvalidOperationException("Expected wounded Glutton, live original Miner and natural enemy before Feast.");
                Play(feastCard!, null, new PlayCardAction(trace.CardId(feastCard!), 0), "Feast", before);
                return false;
            }
            if (Steps.Count != 3) throw new InvalidOperationException("Unexpected Glutton action sequence.");

            ActionRecord meal = Steps[2];
            CombatUnit beforeGlutton = meal.Before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == gluttonId);
            CombatUnit afterGlutton = meal.Actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == gluttonId);
            CombatUnit beforeGuard = meal.Before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == guardId);
            CombatUnit afterGuard = meal.Actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == guardId);
            bool healed = beforeGlutton.Health == 47 && beforeGlutton.MaxHealth == 50 &&
                afterGlutton.Health == 59 && afterGlutton.MaxHealth == 59;
            bool damaged = beforeGuard.Status("armor")?.Stacks == 25 && afterGuard.Status("armor")?.Stacks == 5;
            bool eaten = meal.Actual.Spawn.Train.Rooms[0].Units.All(unit => unit.SpawnerCardId != trace.CardId(minerCard!)) &&
                meal.Actual.OtherPiles?.Any(pile => pile.Name == "Eaten" &&
                    pile.Cards.Any(card => card.InstanceId == trace.CardId(minerCard!))) == true;
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalGluttonOnFeed", GameVersion = Application.version,
                GameModuleMvid = Module, GluttonId, TorchId, MinerId, FeastId,
                GluttonCardInstanceId = trace.CardId(gluttonCard), MinerCardInstanceId = trace.CardId(minerCard!),
                GluttonUnitId = gluttonId, ShieldGuardId = guardId, Healed = healed, EnemyDamaged = damaged,
                MinerEaten = eaten, Steps
            });
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "glutton-onfeed.mt2f"))) archive.Write(stream);
            if (!healed || !damaged || !eaten)
                throw new InvalidOperationException("Original Glutton meal did not heal and damage its natural enemy.");
            log.LogInfo("ORIGINAL-GLUTTON-ONFEED Glutton 47/50->59/59, Shield Guard Armor 25->5, Miner Eaten.");
            return true;

            bool SelectFloor(int floor)
            {
                if (rooms.GetSelectedRoom() == floor) return true;
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(floor));
                return false;
            }

            CardState Deal(string id, string key)
            {
                CardData definition = managers.GetAllGameData().FindCardData(id);
                if (definition?.name != key || !cards.GetBelowHandSize())
                    throw new InvalidOperationException("Expected unchanged original card " + key + " and native hand capacity.");
                return cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            }

            void Deploy(CardState card, int floor)
            {
                SpawnPoint drop = rooms.GetRoom(floor).GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("No native placement point for original unit.");
                Play(card, drop, null, "", null);
            }

            CharacterState FindGlutton()
            {
                var units = new List<CharacterState>();
                rooms.GetRoom(0).AddCharactersToList(units, Team.Type.Monsters);
                return units.Single(unit => unit.IsAlive && !unit.IsDestroyed && ReferenceEquals(unit.GetSpawnerCard(), gluttonCard));
            }

            void Play(CardState card, SpawnPoint? drop, PlayCardAction? selected, string label, BattleTurnState? captured)
            {
                int handIndex = cards.GetHand().IndexOf(card);
                if (handIndex < 0) throw new InvalidOperationException("Original card left the native hand before play.");
                if (!cards.CanPlayHandCard(card, rooms.GetSelectedRoom(), drop, null, null, out var error))
                    throw new InvalidOperationException("Original " + (label.Length == 0 ? card.GetAssetName() : label) + " is not a legal hand play: " + error);
                if (selected != null)
                    pending = new ActionRecord { Label = label, Before = captured!, Action = selected };
                if (!cards.PlayCard(handIndex, drop, ref error))
                    throw new InvalidOperationException("Native original " + (label.Length == 0 ? card.GetAssetName() : label) + " play failed: " + error);
            }
        }
    }
}
#endif
