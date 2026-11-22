#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStatusMagnitudeWitness
    {
        internal const string Name = "original-status-magnitude-witness";
        internal const string MoonName = "original-status-magnitude-moon-witness";
        internal const string RoomSpellsName = "original-room-status-spells";
        private const string Module = "bf93f060-beda-46b1-b808-95ebf1e9a99d";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static readonly (string Id, string Key, string Status)[] Owners =
        {
            ("622860d3-b2ca-45b0-991a-b7745e1dcd5c", "DoubleMagebladeDamage", "mageblade"),
            ("aa94ad34-ee09-464e-89ff-2a2c2867c097", "StrongerSpikes", "spikes"),
            ("b9bddf11-acaa-40fb-bc40-eee83e478424", "IncreaseRageDamage", "buff"),
            ("1fd244de-7d7b-4345-974b-4e5bf287a3df", "AwokenRegen", "regen")
        };
        private static readonly (string Id, string Key, int Cost, string Status, int Stacks, int Chance)[] RoomSpells =
        {
            ("479ceb1b-fc8c-420f-8fa0-17683cee4511", "LavaFlow", 0, "pyregel", 4, 100),
            ("47df0371-7bd9-4bc2-b8e1-de61cc0060c7", "Gooeyflask", 1, "debuff", 2, 0),
            ("75ce1442-757b-4e9f-826d-8325f3dc7d9e", "EchoSnare", 1, "corrupt_poison", 5, 0),
            ("768a53f0-2743-4298-82c3-8541cbecf935", "JarOfFrost", 0, "poison", 8, 0),
            ("9ea70171-8afd-4fab-ad54-7ff5c8b478b8", "MagicJuice", 0, "spell weakness", 2, 0)
        };
        private static readonly (string Id, string Key, int Cost)[] Cards = DirectRoomSpells
            ? new[] { (StewardId, "TrainStewardShield", 1) }.Concat(RoomSpells.Select(spell => (spell.Id, spell.Key, spell.Cost))).ToArray()
            : new[]
        {
            (StewardId, "TrainStewardShield", 1),
            Moon ? ("2b7a187f-da0b-4c34-b8c5-b18755911b43", "SeleneAcolyte", 2)
                : ("63eba1e1-3118-44a3-8906-c8bfbe559667", "LunaCovenChamp2", 0),
            ("2ed78ce2-13c3-4e82-963f-99c9ea12d0f0", "RageSerum", 0),
            ("2ff4ef73-00f4-4bd5-b868-607320188980", "Sharpen", 2),
            ("d81f1f43-f20e-4459-9fc4-19e5836a876f", "StarterHeal", 1)
        };
        private static readonly List<object> Plays = new();
        private static CardData[]? definitions;
        private static CardState[]? hand;
        private static BattleTurnState? before;
        private static PlayCardAction? action;
        private static BattleTurnState? exchange;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is Name or MoonName or RoomSpellsName;
        internal static bool Moon => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == MoonName;
        internal static bool DirectRoomSpells => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == RoomSpellsName;
        private static int DeploymentCount => DirectRoomSpells ? 1 : 2;
        internal static bool Acquired => definitions != null;
        internal static bool ObservingExchange { get; private set; }
        internal static bool Completed { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (typeof(CardState).Module.ModuleVersionId != new Guid(Module) || save.PreviewMode || save.InTestScenario ||
                save.GetGameSequence() == SaveData.GameSequence.InBattle || save.GetCurrentDistance() != 0 ||
                save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("The original magnitude witness requires a fresh, unmodified 2.3.1 Ring 1.");
            AllGameData data = save.GetAllGameData();
            var originalOwners = (DirectRoomSpells ? Owners.Take(0) : Owners).Select(expected =>
            {
                var relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == expected.Id);
                var effect = relic.GetEffects().Single();
                var status = effect.GetParamStatusEffects().Single();
                if (relic.name != expected.Key || relic.DisallowInDeploymentPhase ||
                    effect.GetEffectClassName() != nameof(RelicEffectModifyStatusMagnitude) ||
                    effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetParamInt() != 1 ||
                    effect.GetEffectConditions().Count != 0 || status.statusId != expected.Status || status.count != 0 ||
                    save.GetCollectedRelics().Any(owner => owner.GetRelicDataID() == expected.Id))
                    throw new InvalidOperationException("Original status magnitude owner changed: " + expected.Key);
                return relic;
            }).ToArray();
            definitions = Cards.Select((expected, index) =>
            {
                CardData card = data.FindCardData(expected.Id);
                if (card == null || card.name != expected.Key || card.GetCost() != expected.Cost)
                    throw new InvalidOperationException("Original status witness card changed: " + expected.Key);
                if (DirectRoomSpells && index >= DeploymentCount)
                {
                    var spell = RoomSpells[index - DeploymentCount];
                    var effect = card.GetEffects().Single();
                    var status = effect.GetParamStatusEffects().Single();
                    if (card.GetCardType() != CardType.Spell || card.GetTraits().Single().GetTraitStateName() != nameof(CardTraitExhaustState) ||
                        effect.GetEffectStateName() != nameof(CardEffectAddStatusEffect) || effect.GetTargetMode() != TargetMode.Room ||
                        effect.GetTargetTeamType() != Team.Type.Heroes || effect.GetParamInt() != spell.Chance ||
                        status.statusId != spell.Status || status.count != spell.Stacks)
                        throw new InvalidOperationException("Original room status spell changed: " + spell.Key);
                }
                return card;
            }).ToArray();
            foreach (var owner in originalOwners) save.AddRelic(owner);
        }

        internal static bool Tick(AllGameManagers managers, ManualLogSource log)
        {
            var trace = FullBattleTrace.Active!;
            var combat = managers.GetCombatManager();
            var rooms = managers.GetRoomManager();
            var cards = managers.GetCardManager();
            if (definitions == null) throw new InvalidOperationException("Original magnitude relics were not acquired.");
            if ((!Moon || hand != null) && managers.GetPlayerManager().GetHasMoonPhaseCardEffect() != Moon)
                throw new InvalidOperationException("Original magnitude witness has unexpected native moon tracking.");
            // Card controls need floor zero. The exchange successor keeps native camera focus.
            if (exchange == null && rooms.GetSelectedRoom() != 0)
            {
                managers.GetSaveManager().StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                return false;
            }

            int index = Plays.Count;
            if (before != null)
            {
                var after = trace.CaptureDecision();
                CardState played = hand![combat.IsPlacementPhase ? index : index - DeploymentCount];
                if (cards.GetHand().Contains(played) || before.Energy - after.Energy != Cards[index].Cost)
                    throw new InvalidOperationException("Original magnitude play did not settle: " + Cards[index].Key);
                Plays.Add(new { Card = Cards[index].Key, Action = action, Before = before, After = after });
                before = null;
                return false;
            }

            if (exchange != null)
            {
                ObservingExchange = false;
                var after = trace.CaptureDecision();
                if (after.Spawn.Turn != 2) throw new InvalidOperationException("Original magnitude first exchange did not settle.");
                using var document = NativeFixtureCapture.Capture(new
                {
                    Schema = 1, Boundary = DirectRoomSpells ? "OriginalRoomStatusSpells" : Moon ? "OriginalMoonStatusMagnitudeWitness" : "OriginalStatusMagnitudeWitness",
                    GameVersion = UnityEngine.Application.version,
                    GameModuleMvid = typeof(CardState).Module.ModuleVersionId, Plays, Exchange = new { Before = exchange, After = after },
                    Queries = RelicStatusMagnitudeProbe.Queries
                });
                string filename = DirectRoomSpells ? "room-status-spells.mt2f" : Moon ? "status-magnitude-moon.mt2f" : "status-magnitude.mt2f";
                using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, filename));
                document.Write(stream);
                Completed = true;
                log.LogInfo("ORIGINAL-STATUS-WITNESS captured " + Plays.Count + " unchanged original card plays and first native combat.");
                return false;
            }

            if (combat.IsPlacementPhase && index == DeploymentCount) return true;
            if (index == Cards.Length)
            {
                exchange = trace.CaptureDecision();
                ObservingExchange = true;
                return true;
            }

            bool deploying = combat.IsPlacementPhase;
            if (!deploying && combat.GetTurnCount() != 1)
                throw new InvalidOperationException("Expected original spells during the first normal turn.");
            if (hand == null || !deploying && hand.Length == DeploymentCount)
            {
                hand = (deploying ? definitions.Take(DeploymentCount) : definitions.Skip(DeploymentCount)).Select(definition =>
                    cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == definition.GetID()) ??
                    cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false) ??
                    throw new InvalidOperationException("Original magnitude card could not reach hand: " + definition.name)).ToArray();
                return false;
            }

            var room = rooms.GetRoom(0);
            CardState next = hand[deploying ? index : index - DeploymentCount];
            var allies = new List<CharacterState>();
            room.AddCharactersToList(allies, Team.Type.Monsters);
            CharacterState? target = null;
            if (!deploying && !DirectRoomSpells)
            {
                string targetKey = index == DeploymentCount ? definitions[1].GetSpawnCharacterData().GetAssetKey() : "TrainStewardBig";
                target = allies.Single(unit => unit.IsAlive && unit.GetSourceCharacterData().GetAssetKey() == targetKey);
            }
            SpawnPoint? point = deploying ? room.GetFirstEmptyMonsterPoint() : target?.GetSpawnPoint();
            before = trace.CaptureDecision();
            action = new PlayCardAction(trace.CardId(next), 0, deploying ? allies.Count : -1,
                targetUnitId: target == null ? 0 : trace.UnitId(target));
            int handIndex = cards.GetHand().IndexOf(next);
            if (handIndex < 0 || !cards.CanPlayHandCard(next, 0, point, null, null, out var error))
                throw new InvalidOperationException("Original native magnitude play unavailable: " + Cards[index].Key);
            if (!cards.PlayCard(handIndex, point, ref error))
                throw new InvalidOperationException("Original native magnitude play was rejected: " + error);
            return false;
        }
    }
}
#endif
