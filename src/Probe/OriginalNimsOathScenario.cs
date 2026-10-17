using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalNimsOathScenario
    {
        internal const string Name = "original-nims-oath";
        internal const string OwnerId = "727e69a6-3a8c-4029-895c-99e9dac0bc0a";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static object Evidence
        {
            get
            {
                string[] nativeInterfaces = typeof(RelicEffectAddEffectStartOfTurn).GetInterfaces()
                    .Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
                return new
                {
                    OwnerId,
                    AssetKey = "Inkbound_NimsOath",
                    RoomMode = "ApplyToRandomRoom",
                    TimingEffect = "RelicEffectAddEffectStartOfTurn",
                    NativeInterfaces = nativeInterfaces,
                    TriggerMetadata = new
                    {
                        Kind = "OnKill",
                        Once = false,
                        IgnoreSilence = false,
                        OnlyIfEquipped = false,
                        Effect = "CardEffectAddStatusEffect",
                        Target = "Self",
                        Team = "Monsters",
                        Status = "conduit",
                        Stacks = 1,
                        RuntimeCallbackSupported = nativeInterfaces.Contains("ICharacterKilledRelicEffect")
                    }
                };
            }
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Nim's Oath must be acquired before combat.");
            RelicData owner = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = owner.GetEffects().Single();
            IReadOnlyList<CardEffectData> nested = effect.GetParamCardEffectData();
            CharacterTriggerData trigger = effect.GetTriggers().Single();
            CardEffectData triggerEffect = trigger.GetEffects().Single();
            StatusEffectStackData status = nested.Single().GetParamStatusEffects().Single();
            StatusEffectStackData triggerStatus = triggerEffect.GetParamStatusEffects().Single();
            if (owner.name != "Inkbound_NimsOath" || owner.DisallowInDeploymentPhase != false ||
                effect.GetEffectClassName() != "RelicEffectAddEffectStartOfTurn" || effect.GetParamInt() != 2 ||
                nested.Count != 1 || nested[0].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                nested[0].GetTargetMode() != TargetMode.FrontInRoom ||
                nested[0].GetTargetTeamType() != Team.Type.Heroes || !nested[0].GetParamBool() ||
                status.statusId != "dazed" || status.count != 1 || trigger.GetTrigger() != CharacterTriggerData.Trigger.OnKill ||
                trigger.GetTriggerOnce() || trigger.GetHideVisualAndIgnoreSilence() || trigger.GetOnlyTriggerIfEquipped() ||
                triggerEffect.GetEffectStateName() != "CardEffectAddStatusEffect" ||
                triggerEffect.GetTargetMode() != TargetMode.Self || triggerEffect.GetTargetTeamType() != Team.Type.Monsters ||
                triggerStatus.statusId != "conduit" || triggerStatus.count != 1)
                throw new InvalidOperationException("Original Inkbound_NimsOath definition changed.");
            save.AddRelic(owner);
            Acquired = true;
            Prepared = true;
            log.LogInfo("ORIGINAL-NIMS-OATH-ACQUIRED unchanged owner=" + OwnerId + ".");
        }
    }
}
