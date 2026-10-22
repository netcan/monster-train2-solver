using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    // Read-only observations of the temporary preview and shared aura maps.
    internal static class BossPreviewEnchantmentProbe
    {
        internal static readonly List<object> Records = new List<object>();
        private static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_BOSS_PREVIEW_ENCHANTMENTS") == "1" &&
            FullBattleTrace.Active != null && AllGameManagers.Instance!.GetCombatManager().GetTurnCount() == 5;

        private static object Map(CardEffectEnchant effect, string field)
        {
            var map = (IDictionary)AccessTools.Field(typeof(CardEffectEnchant), field).GetValue(effect);
            var rows = new List<object>();
            foreach (DictionaryEntry pair in map)
            {
                var unit = (CharacterState)pair.Key;
                FullBattleTrace.Active!.TryGetUnitId(unit, out int id);
                rows.Add(new { Id = id, Preview = unit.PreviewMode, Temporary = unit.TemporaryPreviewsEnabled,
                    Destroyed = unit.IsDestroyed,
                    Dead = !unit.IsDestroyed && unit.IsDead,
                    Applied = (bool)AccessTools.Field(pair.Value!.GetType(), "isEnchanted").GetValue(pair.Value),
                    Action = Convert.ToInt32(AccessTools.Field(pair.Value.GetType(), "nextStateAction").GetValue(pair.Value)) });
            }
            return rows;
        }

        private static void Observe(string operation, CardEffectEnchant effect)
        {
            if (!Enabled) return;
            var source = (CharacterState)AccessTools.Field(typeof(CardEffectEnchant), "enchanterCharacter").GetValue(effect);
            if (source == null || source.IsDestroyed || source.GetSourceCharacterData().name != "Boss_Level8_Fragile") return;
            Records.Add(new { Operation = operation, Frame = UnityEngine.Time.frameCount,
                SavePreview = AllGameManagers.Instance!.GetSaveManager().PreviewMode,
                Primary = Map(effect, "_primaryEnchantedTargets"), Preview = Map(effect, "_previewEnchantedTargets") });
        }

        [HarmonyPatch(typeof(CardEffectEnchant), nameof(CardEffectEnchant.OnUpdateEnchantments))]
        private static class UpdatePatch
        {
            private static void Prefix(CardEffectEnchant __instance) => Observe("BeforeUpdate", __instance);
            private static void Postfix(CardEffectEnchant __instance) => Observe("AfterUpdate", __instance);
        }

        [HarmonyPatch(typeof(CombatManager), "SetTemporaryStateEnabled")]
        private static class TemporaryPatch
        {
            private static void Postfix(bool enable, List<CharacterState> characters, bool isCardPreview)
            {
                if (!Enabled) return;
                Records.Add(new { Operation = "Temporary:" + enable, CardPreview = isCardPreview,
                    Frame = UnityEngine.Time.frameCount,
                    SavePreview = AllGameManagers.Instance!.GetSaveManager().PreviewMode,
                    Units = characters.Select(unit =>
                    {
                        FullBattleTrace.Active!.TryGetUnitId(unit, out int id);
                        return new { Id = id, Preview = unit.PreviewMode, Temporary = unit.TemporaryPreviewsEnabled };
                    }).ToArray() });
            }
        }
    }
}
