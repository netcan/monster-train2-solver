#if DEVELOPMENT_PROBE
using System;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Select a legal native spell; never edit the deck, units or authored data.
    internal static class ChosenIncantSpellScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_CHOSEN_INCANT_SPELL") == "1";
        internal static object? Control => Enabled ? new { Kind = "LegalSpellAtOriginalChosenCleric",
            PolicyOverrideOnly = true, DeckAndCharacterAssetsUnchanged = true } : null;

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var rooms = state.Spawn.Train.Rooms.Where(room =>
                room.Units.Any(unit => unit.AssetKey == "ClericT3_IncantAttack_Chosen" && unit.Health > 0) &&
                state.Spawn.Train.Rooms.Where(other => other.RoomIndex != room.RoomIndex)
                    .SelectMany(other => other.Units).Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0 &&
                        unit.IsBoss != true && unit.CanAttack) &&
                state.Spawn.Train.Rooms.SelectMany(other => other.Units)
                    .Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0 && unit.IsBoss == true))
                .Select(room => room.RoomIndex).ToArray();
            if (rooms.Length == 0) return null;
            return BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(action =>
                rooms.Contains(action.RoomIndex) && state.PlayRules?.Cards.Any(rule => rule.CardType == "Spell" &&
                    rule.IsAnyAbility == false && rule.DataId == state.Spawn.Train.Context?.FindCard(action.CardInstanceId)?.DataId) == true);
        }
    }
}
#endif
