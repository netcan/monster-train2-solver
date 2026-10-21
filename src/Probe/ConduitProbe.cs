using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Normalize the random tracker GUID only in snapshots. Native getters,
    // comparisons and card upgrades continue to use their original GUIDs.
    internal static class ConduitProbe
    {
        internal static bool Enabled => AdvisorRuntime.PlayerSession || ConduitMovementCalibration.Enabled || Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "original-conduit-relics" || OriginalMooncycleFaeScenario.Enabled ||
            PlanetariumScenario.Enabled || OriginalStrongMagicCardsScenario.Enabled || OriginalScaledMoonPhaseScenario.Enabled ||
            OriginalSupportSpellsScenario.Enabled && (OriginalSupportSpellsScenario.IsolatedIds == null || !BattleSpawnPointProbe.Enabled) ||
            OriginalXLunaSpikeScenario.Enabled || OriginalMoonRitualScenario.Prepared ||
            OriginalSupportUnitsScenario.IsolatedUnitOnly && Environment.GetEnvironmentVariable("MT2_PROBE_ISOLATED_CONDUIT") == "1";
        private static object? Modifier(CharacterState character)
        {
            var statuses = new List<CharacterState.StatusEffectStack>();
            character.GetStatusEffects(ref statuses, includeZeroStacks: true);
            var state = statuses.Select(status => status.State).OfType<StatusEffectConduitState>().SingleOrDefault();
            return state == null ? null : AccessTools.Field(typeof(StatusEffectConduitState), "conduitModifier").GetValue(state);
        }
        private static object Tracker(object modifier) => AccessTools.Field(modifier.GetType(), "_cardsWeHaveModified").GetValue(modifier);
        internal static string Normalize(string id)
        {
            id = RoomSpellUpgradeProbe.Normalize(id);
            if (!Enabled || FullBattleTrace.Active == null) return id;
            var trace = FullBattleTrace.Active;
            foreach (var character in trace.KnownUnits)
            {
                if (character == null || character.IsDestroyed) continue;
                var modifier = Modifier(character);
                if (modifier == null) continue;
                var tracker = Tracker(modifier);
                if ((string)AccessTools.Field(tracker.GetType(), "trackerId").GetValue(tracker) == id)
                    return "conduit:" + trace.UnitId(character);
            }
            return id;
        }
        internal static ConduitRoomState? CaptureRoom(int roomIndex, Team.Type team, FullBattleTrace trace)
        {
            if (!Enabled) return null;
            var managers = AllGameManagers.Instance!;
            var upgrade = new CardUpgradeState();
            upgrade.Setup(managers.GetAllGameData().GetBalanceData().GetConduitUpgradeData());
            var units = new List<ConduitUnitState>();
            var characters = new List<CharacterState>();
            managers.GetRoomManager()!.GetRoom(roomIndex).AddCharactersToList(characters, team);
            foreach (var character in characters)
            {
                if (character == null || character.IsDestroyed || character.GetTeamType() != team ||
                    character.GetCurrentRoom()?.GetRoomIndex() != roomIndex) continue;
                ConduitUnitState? unit = CaptureUnit(character, trace);
                if (unit != null) units.Add(unit);
            }
            return new ConduitRoomState(CardModifierProbe.Upgrade(upgrade, false, true), units);
        }
        internal static IReadOnlyList<RetainedConduitState>? CaptureRetained(FullBattleTrace trace)
        {
            if (!Enabled || !BattleSpawnPointProbe.Enabled) return null;
            return trace.KnownUnits.Where(character => character != null && !character.IsDestroyed &&
                    (!character.IsAlive || character.GetCurrentRoom() == null))
                .Select(character => new { Character = character, Contributor = CaptureUnit(character, trace) })
                .Where(value => value.Contributor != null)
                .Select(value => new RetainedConduitState(value.Character.GetTeamType() == Team.Type.Heroes
                    ? CombatTeam.Enemy : CombatTeam.Player, value.Contributor!)).ToArray();
        }
        internal static ConduitUnitState? CaptureUnit(CharacterState character, FullBattleTrace trace)
        {
            object? modifier = Modifier(character);
            if (modifier == null) return null;
            object tracker = Tracker(modifier);
            var cards = (IDictionary)AccessTools.Field(tracker.GetType(), "_modifiedCardMagicPower").GetValue(tracker);
            var multiplied = (IEnumerable)AccessTools.Field(tracker.GetType(), "_appliedMagicPowerMultiplier").GetValue(tracker);
            return new ConduitUnitState(trace.UnitId(character), ((RoomStateConduitModifier)modifier).MagicPower,
                character.IsAlive, cards.Keys.Cast<CardState>().Select(trace.CardId).ToArray(),
                multiplied.Cast<CardState>().Select(trace.CardId).ToArray());
        }
    }
}
