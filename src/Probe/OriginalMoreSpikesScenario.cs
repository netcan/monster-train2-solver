using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMoreSpikesScenario
    {
        internal const string Name = "original-more-spikes";
        internal const string Id = "a6c957cc-e9f5-49b7-a6a3-cd7e95393289";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); var data = save.GetAllGameData();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("MoreSpikes requires isolated pre-battle acquisition.");
            var definition = data.GetAllCollectableRelicData().Single(owner => owner.GetID() == Id);
            var effect = definition.GetEffects().Single();
            if (definition.name != "MoreSpikes" || definition.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectAdjustAttackByStatusEffect" ||
                effect.GetParamStatusEffects().Single().statusId != "spikes" || effect.GetParamInt() != 1 ||
                effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Unchanged MoreSpikes source parameters differ.");
            bool addedSpikeWitness = false;
            foreach (var card in save.GetDeckState())
            {
                var cardDefinition = data.FindCardData(card.GetCardDataID());
                var unit = cardDefinition?.GetSpawnCharacterData();
                if (unit == null) continue;
                Set(unit, "health", 80); Set(unit, "attackDamage", 20); Set(unit, "size", 1);
                Set(cardDefinition!, "cost", 0);
                if (!addedSpikeWitness)
                {
                    var statuses = unit.GetStartingStatusEffects().ToList();
                    if (statuses.Any(status => status.statusId == "spikes"))
                        throw new InvalidOperationException("MoreSpikes witness expects an independent authored Spike stack.");
                    statuses.Add(new StatusEffectStackData { statusId = "spikes", count = 4 });
                    Set(unit, "startingStatusEffects", statuses.ToArray()); addedSpikeWitness = true;
                }
            }
            if (!addedSpikeWitness) throw new InvalidOperationException("No unchanged deck monster can carry the authored Spike witness.");
            save.AddRelic(definition); Acquired = true;
            log.LogInfo("ORIGINAL-MORE-SPIKES-ACQUIRED unchanged owner; +1 additional attack per friendly Spike stack.");
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0 || managers.GetSaveManager().PreviewMode)
                throw new InvalidOperationException("MoreSpikes missed its native deployment boundary.");
            var owner = RelicProbe.Capture(managers).Single(item => item.DataId == Id);
            if (owner.StatusAttackModifiers?.Count != 1 || owner.StatusAttackModifiers[0].EffectIndex != 0 ||
                owner.StatusAttackModifiers[0].StatusId != "spikes" || owner.StatusAttackModifiers[0].Team != "Monsters" ||
                owner.StatusAttackModifiers[0].Multiplier != 1)
                throw new InvalidOperationException("Native MoreSpikes attack query metadata differs.");
            log.LogInfo("ORIGINAL-MORE-SPIKES-PREPARED native dynamic attack query; preserved separate from base and status buffs.");
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
