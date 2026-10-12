using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;

namespace MonsterTrain2Poju.Probe
{
    internal static class CovenantCharacterFilterScenario
    {
        internal const string Name = "covenant-character-filter";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }

        internal static void ConfigureBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (!Enabled || Prepared) return;
            SaveManager save = managers.GetSaveManager();
            if (save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Covenant character-filter capture requires an otherwise covenant-free run.");

            CovenantData covenant = save.GetAllGameData().GetAllCovenantDatas()
                .Single(item => item.name == "Covenant04_StrongerBosses");
            var matches = covenant.GetEffects().Select((effect, index) => new { Effect = effect, Index = index })
                .Where(item => item.Effect.GetEffectClassName() == "RelicEffectAddTrigger" &&
                    (item.Effect.GetParamCharacters() ?? new List<CharacterData>()).Any(character =>
                        character != null && character.GetID() == "c2f466ae-fbc5-4209-aeff-73ce04d625eb" &&
                        character.GetAssetKey() == "Boss_Level1_Basic"))
                .ToArray();
            var effects = covenant.GetEffects();
            if (matches.Length != 1 || effects.Count != 25 ||
                effects.Count(effect => effect.GetEffectClassName() == "RelicEffectAddTrigger") != 11 ||
                effects.Count(effect => effect.GetEffectClassName() == "RelicEffectModifyCharacterMaxHealth") != 10 ||
                effects.Count(effect => effect.GetEffectClassName() == "RelicEffectModifyCharacterAttackDamage") != 2 ||
                effects.Count(effect => effect.GetEffectClassName() == "RelicEffectAddStatusEffectOnSpawn") != 2)
                throw new InvalidOperationException("Covenant04_StrongerBosses definition no longer matches the captured full-definition contract.");

            var active = (IList<CovenantState>)save.GetCovenants();
            active.Add(new CovenantState(covenant));
            if (save.GetCovenants().Count != 1 || save.GetCovenants()[0].GetRelicDataID() != covenant.GetID())
                throw new InvalidOperationException("Covenant04_StrongerBosses was not installed as an active CovenantState.");

            Prepared = true;
            log.LogInfo("COVENANT-CHARACTER-FILTER-PREPARED full original Covenant04_StrongerBosses; add-trigger effect=" +
                matches[0].Index + "; character=Boss_Level1_Basic; effects=" + covenant.GetEffects().Count);
        }
    }
}
