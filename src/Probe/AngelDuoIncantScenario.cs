#if DEVELOPMENT_PROBE
using System;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class AngelDuoIncantScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ANGEL_DUO_INCANT") == "1";
        internal static object? Control => Enabled ? new { Kind = "LegalSpellAtOriginalAngelDuo",
            PolicyOverrideOnly = true, DeckAndCharacterAssetsUnchanged = true } : null;

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            int[] rooms = state.Spawn.Train.Rooms.Where(room => room.Units.Any(unit =>
                OuterBossDefinitions.IsAngelDuo(unit.AssetKey) && unit.Health > 0) &&
                state.Spawn.Train.Rooms.Where(other => other.RoomIndex != room.RoomIndex)
                    .SelectMany(other => other.Units).Any(unit => unit.Health > 0 &&
                        unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack"))
                .Select(room => room.RoomIndex).ToArray();
            if (rooms.Length == 0) return null;
            foreach (CardToken card in state.Spawn.Train.Context!.Cards.Hand)
            {
                if (state.PlayRules?.Cards.Any(rule => rule.CardType == "Spell" && rule.IsAnyAbility == false && rule.DataId == card.DataId) != true)
                    continue;
                PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(state, card.InstanceId)
                    .FirstOrDefault(action => rooms.Contains(action.RoomIndex));
                if (play != null) return play;
            }
            return null;
        }
    }
}
#endif
