using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShinyShoe;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class UnitPlayModelProbe
    {
        private const string StewardCardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private readonly ManualLogSource log;
        private readonly Dictionary<CardState, int> cardIds =
            new Dictionary<CardState, int>(ReferenceComparer<CardState>.Instance);
        private readonly Dictionary<CharacterState, int> unitIds =
            new Dictionary<CharacterState, int>(ReferenceComparer<CharacterState>.Instance);
        private int nextCardId = 1;
        private int nextUnitId = 1;
        private CombatProjection? before;
        private CombatProjection? predicted;
        private SimpleUnitPlay? action;

        internal UnitPlayModelProbe(ManualLogSource log)
        {
            this.log = log;
        }

        internal bool Begin(AllGameManagers managers, SaveManager save, CombatManager combat,
            CardManager cards, int handIndex, out string reason)
        {
            before = Capture(managers, save, combat, cards);
            predicted = null;
            action = null;
            CardState card = cards.GetHand()[handIndex];
            RoomManager rooms = managers.GetRoomManager()
                ?? throw new InvalidOperationException("Room manager is unavailable.");
            RoomState room = rooms.GetRoom(0)
                ?? throw new InvalidOperationException("Room zero is unavailable.");
            if (before.Scenario != "Level1BattleJunker" || combat.GetTurnCount() != 0)
            {
                reason = "Only the turn-zero natural Level1BattleJunker fixture is modeled.";
                return false;
            }
            if (combat.IsRunningTriggerQueue || managers.GetReplayManager().IsCardPlaying())
            {
                reason = "The capture is not at a quiet decision point.";
                return false;
            }
            if (card.GetCardDataID() != StewardCardId || card.GetCardType() != CardType.Monster)
            {
                reason = "Only the starting Train Steward unit card is modeled.";
                return false;
            }
            if (save.GetCollectedRelics().Count != 0 || before.Heroes.Count != 0 ||
                before.Monsters.Count != 1 || before.Monsters[0].AssetKey != "PyreHeartStarter" ||
                before.Monsters.Any(unit => unit.Room == 0))
            {
                reason = "Relics, enemies, and occupied summon rooms are not modeled.";
                return false;
            }
            if (card.GetCardStateModifiers().GetCardUpgrades().Count != 0 ||
                card.GetTemporaryCardStateModifiers().GetCardUpgrades().Count != 0 ||
                card.GetTraitStates().Count != 0 || card.GetTriggers().Count != 0)
            {
                reason = "Card upgrades, traits, and triggers are not modeled.";
                return false;
            }
            List<CardEffectState> effects = card.GetEffectStates();
            if (effects.Count != 1 || !(effects[0].GetCardEffect() is CardEffectSpawnMonster))
            {
                reason = "The card is not a single simple spawn effect.";
                return false;
            }
            CardEffectState effect = effects[0];
            CharacterData? spawned = effect.GetParamCharacterData();
            if (spawned == null || effect.GetParamAdditionalCharacterData() != null ||
                effect.GetParamCharacterDataPool().Count != 0 || effect.GetParamInt() > 1 ||
                spawned.GetTriggers().Count != 0 || spawned.GetRoomModifiersData().Count != 0 ||
                spawned.GetStartingStatusEffects().Length != 0 || spawned.GetGraftedEquipment() != null ||
                card.GetSize() != spawned.GetSize())
            {
                reason = "The spawn has extra characters, modifiers, statuses, or triggers.";
                return false;
            }
            int cost = card.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(),
                managers.GetRelicManager(), room);
            action = new SimpleUnitPlay(handIndex, before.Hand[handIndex].InstanceId, 0,
                cost, card.GetSize(), spawned.GetAssetKey(), spawned.GetAttackDamage(), spawned.GetHealth());
            ModelStep step = SimpleUnitPlayModel.Apply(before, action);
            if (!step.Supported)
            {
                reason = step.UnsupportedReason ?? "Unsupported model transition.";
                return false;
            }
            predicted = step.State;
            reason = string.Empty;
            log.LogInfo("MODEL-BEGIN card=" + card.GetCardDataID() + " instance=" + action.CardInstanceId +
                " cost=" + cost + " size=" + action.Size + " unit=" + action.UnitAssetKey);
            return true;
        }

        internal bool Complete(AllGameManagers managers, SaveManager save, CombatManager combat,
            CardManager cards, out string reason)
        {
            if (before == null || predicted == null || action == null)
            {
                reason = "No supported model transition was started.";
                return false;
            }
            CombatProjection actual = Capture(managers, save, combat, cards);
            string? difference = FirstDifference(JToken.FromObject(predicted), JToken.FromObject(actual), "$");
            string output = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "model-unit-play.json");
            File.WriteAllText(output, JsonConvert.SerializeObject(new
            {
                Schema = 1,
                GameVersion = Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Before = before,
                Action = action,
                Predicted = predicted,
                Actual = actual,
                Difference = difference
            }, Formatting.Indented));
            if (difference != null)
            {
                reason = "Native result differs at " + difference + "; see " + output;
                log.LogError("MODEL-MISMATCH " + reason);
                return false;
            }
            reason = string.Empty;
            log.LogInfo("MODEL-PASS simple unit play matched native state projection: " + output);
            return true;
        }

        private CombatProjection Capture(AllGameManagers managers, SaveManager save,
            CombatManager combat, CardManager cards)
        {
            var data = new CombatProjectionData
            {
                Scenario = save.GetCurrentScenarioData()?.name ?? string.Empty,
                Turn = combat.GetTurnCount(),
                Energy = managers.GetPlayerManager().GetEnergy(),
                PyreHealth = save.GetTowerHP(),
                PyreMaxHealth = save.GetMaxTowerHP(),
                Gold = save.GetGold(),
                ForgePoints = save.GetForgePoints(),
                DragonsHoard = save.GetDragonsHoardAmount(),
                DrawModifier = cards.GetDrawCountModifier(),
                GameplayRng = CaptureGameplayRng(),
                Hand = CaptureCards(cards.GetHand()),
                Draw = CaptureCards(cards.GetDrawPile()),
                Discard = CaptureCards(cards.GetDiscardPile()),
                DiscardBuffer = CaptureCards(cards.GetDiscardBufferPile()),
                Exhausted = CaptureCards(cards.GetExhaustedPile()),
                Eaten = CaptureCards(cards.GetEatenPile()),
                Purged = CaptureCards(cards.GetPurgedPile()),
                Heroes = CaptureUnits(managers.GetHeroManager()),
                Monsters = CaptureUnits(managers.GetMonsterManager())
            };
            RoomManager rooms = managers.GetRoomManager()
                ?? throw new InvalidOperationException("Room manager is unavailable.");
            for (int index = 0; index < rooms.GetNumRooms(); index++)
            {
                RoomState room = rooms.GetRoom(index)
                    ?? throw new InvalidOperationException("A room disappeared during model capture.");
                CapacityInfo heroes = room.GetCapacityInfo(Team.Type.Heroes);
                CapacityInfo monsters = room.GetCapacityInfo(Team.Type.Monsters);
                data.Rooms.Add(new RoomToken(index, heroes.count, heroes.max, heroes.nextSpawn,
                    monsters.count, monsters.max, monsters.nextSpawn, room.GetCurrentCorruption()));
            }
            return new CombatProjection(data);
        }

        private List<CardToken> CaptureCards(List<CardState> cards)
        {
            var captured = new List<CardToken>(cards.Count);
            foreach (CardState card in cards)
            {
                if (!cardIds.TryGetValue(card, out int id))
                {
                    id = nextCardId++;
                    cardIds.Add(card, id);
                }
                captured.Add(new CardToken(id, card.GetCardDataID()));
            }
            return captured;
        }

        private List<UnitToken> CaptureUnits(ICharacterManager? manager)
        {
            var captured = new List<UnitToken>();
            if (manager == null)
            {
                return captured;
            }
            for (int index = 0; index < manager.GetNumCharacters(); index++)
            {
                CharacterState unit = manager.GetCharacter(index);
                if (!unitIds.TryGetValue(unit, out int id))
                {
                    id = nextUnitId++;
                    unitIds.Add(unit, id);
                }
                SpawnPoint? point = unit.GetSpawnPoint();
                var statuses = new List<CharacterState.StatusEffectStack>();
                unit.GetStatusEffects(ref statuses);
                string signature = string.Join(";", statuses.Select(status =>
                    status.State.GetStatusId() + ":" + status.Count.ToString(CultureInfo.InvariantCulture) +
                    ":" + status.State.GetParamInt().ToString(CultureInfo.InvariantCulture) +
                    ":" + status.State.GetParamSecondaryInt().ToString(CultureInfo.InvariantCulture) +
                    ":" + status.State.GetParamStr() +
                    ":" + status.State.GetParamFloat().ToString("R", CultureInfo.InvariantCulture))
                    .OrderBy(value => value, StringComparer.Ordinal));
                captured.Add(new UnitToken(id, unit.GetSourceCharacterData()?.GetAssetKey() ?? string.Empty,
                    point?.GetRoomOwner()?.GetRoomIndex() ?? -1, point?.GetIndexInRoom() ?? -1,
                    unit.GetAttackDamage(), unit.GetHP(), unit.GetMaxHP(), unit.GetSize(), signature));
            }
            return captured;
        }

        private static string CaptureGameplayRng()
        {
            var result = new StringBuilder();
            FieldInfo[] fields = typeof(UnityEngine.Random.State).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Array.Sort(fields, (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
            {
                if (id == RngId.NonDeterministic || id == RngId.Chatter)
                {
                    continue;
                }
                object state = RandomManager.GetState(id);
                result.Append(id).Append('=');
                foreach (FieldInfo field in fields)
                {
                    result.Append(field.Name).Append(':').Append(field.GetValue(state)).Append(',');
                }
                result.Append(';');
            }
            return result.ToString();
        }

        private static string? FirstDifference(JToken expected, JToken actual, string path)
        {
            if (expected.Type != actual.Type)
            {
                return path;
            }
            if (expected is JObject expectedObject && actual is JObject actualObject)
            {
                foreach (JProperty property in expectedObject.Properties())
                {
                    JToken? actualProperty = actualObject[property.Name];
                    if (actualProperty == null)
                    {
                        return path + "." + property.Name;
                    }
                    string? difference = FirstDifference(property.Value, actualProperty, path + "." + property.Name);
                    if (difference != null)
                    {
                        return difference;
                    }
                }
                return expectedObject.Count == actualObject.Count ? null : path;
            }
            if (expected is JArray expectedArray && actual is JArray actualArray)
            {
                if (expectedArray.Count != actualArray.Count)
                {
                    return path + ".Length";
                }
                for (int index = 0; index < expectedArray.Count; index++)
                {
                    string? difference = FirstDifference(expectedArray[index], actualArray[index],
                        path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]");
                    if (difference != null)
                    {
                        return difference;
                    }
                }
                return null;
            }
            return JToken.DeepEquals(expected, actual) ? null : path;
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            internal static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();

            public bool Equals(T? left, T? right) => ReferenceEquals(left, right);
            public int GetHashCode(T value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
