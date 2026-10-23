using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class TrueFinalBossCovenantModel
    {
        internal static bool KnownOwner(CombatRelicState owner) =>
            owner.IsCovenant == true && (owner.AssetKey == "Covenant00_FTUE" && owner.DataId == "3cc0272d-c0d7-46dc-9c7a-be9633572f70" ||
                owner.AssetKey == "Covenant01_StrongerEnemiesMoreCards" && owner.DataId == "d3b53481-f26b-4344-ae8e-ec80b28d6004" ||
                owner.AssetKey == "Covenant04_StrongerBosses" && owner.DataId == "80cfbaca-da9a-4aa8-ac0d-a6563f26fcb8" ||
                owner.AssetKey == "Covenant08_StrongerEnemies" && owner.DataId == "ad5c826a-e716-43cc-9c13-df0dd138de71");

        internal static bool MatchesOrigins(CombatContext? context, EnemyBirthStatAdjustment stats)
        {
            CombatRelicState[] owners = context?.Relics?.Where(relic => relic.IsCovenant == true).ToArray()
                ?? System.Array.Empty<CombatRelicState>();
            return owners.Length == 1 && KnownOwner(owners[0]) && stats.CovenantIds.SequenceEqual(new[] { owners[0].DataId });
        }

        // Original single-Covenant controls. Authored stats remain separate
        // from the native birth adjustments; combined ranks stay guarded.
        internal static bool Adjustment(CombatContext? context, string asset, out int attack, out int health)
        {
            attack = 0; health = 0;
            CombatRelicState[] owners = context?.Relics?.Where(relic => relic.IsCovenant == true).ToArray()
                ?? System.Array.Empty<CombatRelicState>();
            if (owners.Length == 0) return true;
            if (owners.Length != 1 || !KnownOwner(owners[0])) return false;
            CombatRelicState owner = owners[0];
            if (owner.AssetKey != "Covenant04_StrongerBosses") return true;
            if (asset == "Boss_Level9_Dominion_V2") { health = 500; return true; }
            if (asset == "Boss_Level9_Savagery_V2") { attack = 5; health = 500; return true; }
            if (asset is "Boss_Level9_Entropy_Bot_V2" or "Boss_Level9_Entropy_Mid_V2" or "Boss_Level9_Entropy_Top_V2")
            { attack = 5; health = 1000; return true; }
            return false;
        }
    }
}
