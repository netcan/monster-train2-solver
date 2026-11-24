using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class OriginalAllSubtypesModel
    {
        internal const string OwnerId = "3894bbe3-6ad6-42ed-9f63-6b47371b4583";
        internal const string Effect = "RelicEffectMonstersAreAllSubtypes";
        private const string Horde = "SubtypesData_Horde";
        private const string None = "SubtypeData_None";
        private const string Pyre = "SubtypesData_Pyre";

        internal static bool IsOwner(CombatRelicState relic) => relic.DataId == OwnerId &&
            relic.AssetKey == "AllUnitsAllSubtypes" && relic.IsCovenant == false && relic.EffectTypes is [Effect];

        internal static bool Active(CombatContext? context, bool deployment) => context?.Relics?.Any(relic =>
            IsOwner(relic) && (!deployment || relic.DisallowedInPlacementPhase == false)) == true;

        internal static bool MatchesCharacter(CombatUnit unit, string subtype) =>
            MatchesCharacter(unit.Team, unit.IsPyre, unit.Subtypes, subtype, subtype == Pyre, allSubtypes: true);

        internal static bool MatchesCharacter(CombatTeam team, bool isPyre, IReadOnlyList<string> rawSubtypes,
            string? subtype, bool pyreSubtype, bool allSubtypes)
        {
            if (string.IsNullOrEmpty(subtype) || subtype == None) return team != CombatTeam.Player || !isPyre;
            bool rawMatch = rawSubtypes.Contains(subtype);
            if (team != CombatTeam.Player) return rawMatch;
            if (pyreSubtype) return isPyre && rawMatch;
            return rawMatch || (allSubtypes && !isPyre && subtype != Horde);
        }

        internal static bool MatchesDeckCard(CardMaskDefinition card, string subtype, bool allSubtypes) =>
            card.HasSpawnData && (card.Subtypes.Contains(subtype) ||
                (allSubtypes && subtype != RelicStartTurnHealModel.ChampionSubtype));
    }
}
