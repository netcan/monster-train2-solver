using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPyreRevengeScenario
    {
        internal const string Name = "original-pyre-revenge";
        internal const string OwnerId = "82b22203-9da1-4f83-9e84-826fb8d1d280";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static InstallationRecord? Installation { get; private set; }

        internal sealed class InstallationRecord
        {
            public int UnitId { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public bool Completed { get; set; }
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("PyreRevengeAddPyregel must be acquired before battle in an isolated save.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("PyreRevengeAddPyregel is already acquired.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-PYRE-REVENGE-ACQUIRED unchanged OnHit Pyre trigger.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            if (Prepared || !Acquired || save.PreviewMode || combat.GetTurnCount() != 0 ||
                !save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("PyreRevengeAddPyregel requires its isolated deployment state.");

            var relics = new List<RelicState>();
            AccessTools.Method(typeof(RelicManager), "GetCurrentRelics")!
                .Invoke(managers.GetRelicManager(), new object[] { relics });
            RelicState owner = relics.Single(item => item.GetRelicDataID() == OwnerId);
            CharacterState pyre = managers.GetRoomManager()!.GetPyreRoom().GetPyreHeart();
            if (pyre == null || !pyre.IsPyreHeart()) throw new InvalidOperationException("The live native Pyre is required.");

            FullBattleTrace trace = FullBattleTrace.Active ?? throw new InvalidOperationException("Full battle trace is unavailable.");
            RoomState room = pyre.GetCurrentRoom() ?? throw new InvalidOperationException("The native Pyre has no room.");
            var record = new InstallationRecord { UnitId = trace.UnitId(pyre), Before = trace.Capture(room) };
            IEnumerator callback = owner.OnCharacterAdded(pyre, null!, managers.GetCoreManagers(), managers.GetRelicManager(), save,
                managers.GetCardStatistics(), managers.GetPlayerManager(), managers.GetRoomManager()!, combat, managers.GetCardManager()!);
            Drain(callback);
            record.After = trace.Capture(room);
            record.Completed = true;
            Installation = record;

            CharacterTriggerState? installed = pyre.GetTriggers().FirstOrDefault(trigger =>
                trigger.GetTrigger() == CharacterTriggerData.Trigger.OnHit && trigger.GetEffectStates().Any(effect =>
                    effect.GetCardEffect() is CardEffectAddStatusEffect && effect.GetSourceCardEffectData().GetParamStatusEffects()
                        .Any(status => status.statusId == "pyregel" && status.count == 2)));
            if (installed == null) throw new InvalidOperationException("Native Pyre callback did not install the +2 Pyregel OnHit trigger.");

            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged")!.SetValue(combat, true);
            log.LogInfo("ORIGINAL-PYRE-REVENGE-PREPARED one unchanged OnHit trigger installed on the live Pyre; Tower Heroes receive 2 Pyregel.");
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            if (relic.name != "PyreRevengeAddPyregel" || relic.GetEffects().Count != 1 ||
                relic.GetEffects()[0].GetEffectClassName() != "RelicEffectAddTrigger" ||
                relic.GetEffects()[0].GetParamCharacterSubtype()?.IsPyre != true ||
                relic.GetEffects()[0].GetTriggers()?.Count != 1)
                throw new InvalidOperationException("PyreRevengeAddPyregel native definition changed.");
            CharacterTriggerData trigger = relic.GetEffects()[0].GetTriggers()![0];
            CardEffectData effect = trigger.GetEffects().Single();
            if (trigger.GetTrigger() != CharacterTriggerData.Trigger.OnHit || trigger.GetTriggerOnce() ||
                effect.GetEffectStateName() != "CardEffectAddStatusEffect" || effect.GetTargetMode() != TargetMode.Tower ||
                !effect.GetTargetTeamType().HasFlag(Team.Type.Heroes) ||
                !effect.GetParamStatusEffects().Select(status => (status.statusId, status.count)).SequenceEqual(new[] { ("pyregel", 2) }))
                throw new InvalidOperationException("PyreRevengeAddPyregel native OnHit payload changed.");
        }

        private static void Drain(IEnumerator routine)
        {
            try
            {
                while (routine.MoveNext())
                {
                    if (routine.Current is IEnumerator nested) Drain(nested);
                    else if (routine.Current != null) throw new InvalidOperationException("Pyre trigger installation unexpectedly yielded asynchronous work.");
                }
            }
            finally { (routine as IDisposable)?.Dispose(); }
        }
    }
}
