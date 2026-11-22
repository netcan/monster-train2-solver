#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;
using UnityEngine;
using TypeNameCache = ShinyShoe.TypeNameCache;

namespace MonsterTrain2Poju.Probe
{
    internal static class EffectOwnerProbe
    {
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            string? effectType = Environment.GetEnvironmentVariable("MT2_PROBE_EFFECT_OWNER_TYPE");
            if (string.IsNullOrEmpty(effectType)) return;
            if (TypeNameCache.GetType(effectType) is not Type implementation || !typeof(ICardEffect).IsAssignableFrom(implementation))
                throw new InvalidOperationException("Expected an installed card effect type: " + effectType);

            const int maximumRows = 128;
            var matches = new List<object>();
            var definition = managers.GetSaveManager().GetAllGameData();
            var cards = definition.GetAllCardData();
            var characters = definition.GetAllCharacterData();
            var upgrades = definition.GetAllCardUpgradeData();
            var relics = RelicCatalogProbe.OriginalOwners(definition).ToArray();
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            JToken before = JToken.FromObject(trace.CaptureDecision());
            JObject rngBefore = RngStates();
            int frame = Time.frameCount;
            foreach (CardData card in cards.OrderBy(card => card.GetID(), StringComparer.Ordinal))
            {
                Scan("Card", card, "Effects", null, card.GetEffects());
                ScanCardTriggers("Card", card, "CardTriggers", card.GetCardTriggers());
            }
            foreach (CharacterData character in characters.OrderBy(character => character.GetID(), StringComparer.Ordinal))
            {
                ScanCharacterTriggers("Character", character, "CharacterTriggers", character.GetTriggers());
                ScanRoomModifiers("Character", character, "RoomModifiers", character.GetRoomModifiersData());
                if (!character.IsOuterTrainBoss()) continue;
                foreach (var pair in character.GetBossActionData().Select((group, index) => (group, index)))
                {
                    int actionIndex = 0;
                    foreach (var action in pair.group.GetActions())
                        Scan("Character", character, $"BossActions[{pair.index}].Actions[{actionIndex++}].Effects",
                            action.GetActionBehaviorName(), action.GetActionEffectData());
                }
            }
            foreach (CardUpgradeData upgrade in upgrades.OrderBy(upgrade => upgrade.GetID(), StringComparer.Ordinal))
            {
                ScanCardTriggers("Upgrade", upgrade, "CardTriggers", upgrade.GetCardTriggerUpgrades());
                ScanCharacterTriggers("Upgrade", upgrade, "CharacterTriggers", upgrade.GetCharacterTriggerUpgrades());
                ScanRoomModifiers("Upgrade", upgrade, "RoomModifiers", upgrade.GetRoomModifierUpgrades());
            }
            foreach (var owner in relics.OrderBy(owner => owner.Collection, StringComparer.Ordinal)
                .ThenBy(owner => owner.Data.GetID(), StringComparer.Ordinal))
            {
                int effectIndex = 0;
                foreach (RelicEffectData effect in owner.Data.GetEffects())
                {
                    string path = $"{owner.Collection}.Effects[{effectIndex++}]";
                    Scan("Relic", owner.Data, path + ".CardEffects", effect.GetEffectClassName(), effect.GetParamCardEffectData());
                    ScanCharacterTriggers("Relic", owner.Data, path + ".CharacterTriggers", effect.GetTriggers());
                }
            }

            JObject rngAfter = RngStates();
            bool decisionUnchanged = JToken.DeepEquals(before, JToken.FromObject(trace.CaptureDecision()));
            if (!decisionUnchanged || Time.frameCount != frame || !JToken.DeepEquals(rngBefore, rngAfter))
                throw new InvalidOperationException("Effect owner inspection changed native decision state, RNG or frame.");
            int matchCount = counts.Values.Sum();
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Scope = "AllGameDataEffectLists", EffectType = effectType,
                GameVersion = Application.version, GameModuleMvid = typeof(CardEffectData).Assembly.ManifestModule.ModuleVersionId,
                FrameBefore = frame, FrameAfter = Time.frameCount, RngBefore = rngBefore, RngAfter = rngAfter,
                DecisionUnchanged = decisionUnchanged, Definitions = new { Cards = cards.Count, Characters = characters.Count,
                    Upgrades = upgrades.Count, Relics = relics.Length }, MatchCount = matchCount,
                MatchesTruncated = matchCount > maximumRows, MatchCounts = counts, Matches = matches
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "effect-owners.mt2f"));
            archive.Write(output);

            void Scan(string kind, GameData owner, string path, string? trigger, IEnumerable<CardEffectData>? effects)
            {
                if (effects == null) return;
                int index = 0;
                foreach (CardEffectData effect in effects)
                {
                    int effectIndex = index++;
                    if (effect?.GetEffectStateName() != effectType) continue;
                    counts.TryGetValue(kind, out int count);
                    counts[kind] = count + 1;
                    if (matches.Count == maximumRows) continue;
                    matches.Add(new
                    {
                        Kind = kind, OwnerId = owner.GetID(), OwnerKey = owner.name,
                        Path = $"{path}[{effectIndex}]", Trigger = trigger, RuntimeType = implementation.Name,
                        Target = effect.GetTargetMode().ToString(), Team = effect.GetTargetTeamType().ToString(),
                        Value = effect.GetParamInt(), Statuses = effect.GetParamStatusEffects()?.Select(status =>
                            new { status.statusId, status.count }).ToArray()
                    });
                }
            }

            void ScanCardTriggers(string kind, GameData owner, string path, IEnumerable<CardTriggerEffectData> triggers)
            {
                int index = 0;
                foreach (CardTriggerEffectData trigger in triggers)
                    Scan(kind, owner, $"{path}[{index++}].Effects", trigger.GetTrigger().ToString(), trigger.GetCardEffects());
            }

            void ScanCharacterTriggers(string kind, GameData owner, string path, IEnumerable<CharacterTriggerData>? triggers)
            {
                if (triggers == null) return;
                int index = 0;
                foreach (CharacterTriggerData trigger in triggers)
                    Scan(kind, owner, $"{path}[{index++}].Effects", trigger.GetTrigger().ToString(), trigger.GetEffects());
            }

            void ScanRoomModifiers(string kind, GameData owner, string path, IEnumerable<RoomModifierData> modifiers)
            {
                int index = 0;
                foreach (RoomModifierData modifier in modifiers)
                    Scan(kind, owner, $"{path}[{index++}].Effects", modifier.GetRoomStateModifierClassName(), modifier.GetParamCardEffectData());
            }
        }

        private static JObject RngStates()
        {
            var result = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
                result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return result;
        }
    }
}
#endif
