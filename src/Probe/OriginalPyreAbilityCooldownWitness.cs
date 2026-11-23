#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPyreAbilityCooldownWitness
    {
        private const string OwnerId = "806077f8-3495-4f36-a27f-a0454229fc57";
        private const string OwnerKey = "Echoes_PyreCanUseAbilityMultipleTimes";
        private const string PyreId = "0ff61363-ab58-4873-9a26-18db51eacd46";
        private const string AbilityId = "4dbfc527-50b2-4068-aa36-9770d4444a57";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly string? Mode = Environment.GetEnvironmentVariable("MT2_PROBE_PYRE_ABILITY_COOLDOWN_WITNESS");
        private static bool WithOwner => Mode == "WithOwner";
        internal static bool Enabled => WithOwner || Mode == "WithoutOwner";
        internal static bool Prepared { get; private set; }
        private static CooldownSample? callbackBefore, callbackAfter;

        internal static void Prepare(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(CharacterState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetGameSequence() != SaveData.GameSequence.BattleIntro ||
                save.GetMutators().Count != 0 || !save.GetMetagameSave().HasUnlockedPyreHeart(PyreId))
                throw new InvalidOperationException("Expected a live original 2.3.1 BattleIntro with Time Pyre unlocked and no mutators.");

            var definition = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("Original Pyre cooldown mutator is missing.");
            var effect = definition.GetEffects().Single();
            if (definition.name != OwnerKey || definition.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != nameof(RelicEffectModifyUnitAbilityCooldown) ||
                effect.GetParamInt() != 2 || effect.GetParamInt2() != (int)RelicEffectModifyUnitAbilityCooldown.UnitType.PyreHeart ||
                effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original Pyre cooldown mutator definition changed.");

            save.SetActiveSavePyreCharacter(PyreId, "", loading: false);
            var pyre = save.GetActiveSavePyreCharacterData();
            var ability = pyre.GetUnitAbilityCardData();
            if (pyre.GetID() != PyreId || pyre.GetAssetKey() != "PyreHeartTime" || ability == null ||
                ability.GetID() != AbilityId || ability.name != "PyreHeartTimeAbility" || !ability.IsUnitAbility() ||
                ability.GetCost() != 0 || ability.GetCooldownAtSpawn() != 0 || ability.GetCooldownAfterActivated() != 99)
                throw new InvalidOperationException("Original Time Pyre ability definition changed.");

            if (WithOwner)
            {
                save.AddMutator(definition);
                var owner = save.GetMutators().Single();
                if (owner.GetRelicDataID() != OwnerId || owner.GetEffects() is not [RelicEffectModifyUnitAbilityCooldown])
                    throw new InvalidOperationException("Native mutator did not initialize the original Pyre cooldown effect.");
            }
            Prepared = true;
        }

        internal static void Write(FullBattleTrace trace, AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var pyre = managers.GetRoomManager()?.GetPyreRoom()?.GetPyreHeart();
            string[] activeOwners = save.GetMutators().Select(owner => owner.GetRelicDataID()).ToArray();
            bool hasOwner = activeOwners.Contains(OwnerId);
            int? turn = managers.GetCombatManager()?.GetTurnCount();
            bool isPyre = pyre != null && pyre.IsPyreHeart();
            string? livePyreId = pyre?.GetSourceCharacterData()?.GetID();
            string? liveAbilityId = pyre?.GetUnitAbility()?.GetID();
            bool hasSpawnerCard = pyre?.GetSpawnerCard() != null;
            if (!Prepared || save.PreviewMode || turn != 0 || pyre == null || !isPyre ||
                livePyreId != PyreId || liveAbilityId != AbilityId || hasSpawnerCard || hasOwner != WithOwner)
                throw new InvalidOperationException($"Expected a settled original Time Pyre battle birth. " +
                    $"mode={Mode} prepared={Prepared} preview={save.PreviewMode} turn={turn} actorPresent={pyre != null} isPyre={isPyre} " +
                    $"savePyre={save.GetActiveSavePyreCharacterData()?.GetID() ?? "none"} actorPyre={livePyreId ?? "none"} " +
                    $"ability={liveAbilityId ?? "none"} spawnerCard={hasSpawnerCard} activeOwners=[{string.Join(",", activeOwners)}]");

            var settled = Capture(pyre);
            if (!settled.Matches(WithOwner ? 2 : 99) || (WithOwner
                ? callbackBefore?.Matches(99) != true || callbackAfter?.Matches(2) != true
                : callbackBefore != null || callbackAfter != null))
                throw new InvalidOperationException($"Native Time Pyre cooldown boundary differs from the original definition. " +
                    $"mode={Mode} callbackBefore={callbackBefore} callbackAfter={callbackAfter} settled={settled}");

            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalPyreAbilityCooldownBirth", GameVersion = Application.version,
                GameModuleMvid = Module, OwnerId, OwnerKey, HasOwner = hasOwner,
                Effect = nameof(RelicEffectModifyUnitAbilityCooldown), ParamInt = 2,
                ParamInt2 = (int)RelicEffectModifyUnitAbilityCooldown.UnitType.PyreHeart,
                PyreId, AbilityId, UnitId = trace.UnitId(pyre), CallbackBefore = callbackBefore,
                CallbackAfter = callbackAfter, Settled = settled, Decision = trace.CaptureDecision()
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "pyre-ability-cooldown-birth.mt2f"));
            archive.Write(stream);
        }

        private static CooldownSample Capture(CharacterState unit) => new CooldownSample(
            unit.GetUnitAbilityCooldown(), unit.GetUnitAbilityCooldownAtSpawn(), unit.GetStatusEffectStacks("cooldown"));

        private sealed class CooldownSample
        {
            public int AfterActivation { get; }
            public int AtSpawn { get; }
            public int Remaining { get; }

            public CooldownSample(int afterActivation, int atSpawn, int remaining)
            {
                AfterActivation = afterActivation;
                AtSpawn = atSpawn;
                Remaining = remaining;
            }

            internal bool Matches(int afterActivation) => AfterActivation == afterActivation && AtSpawn == 0 && Remaining == 0;

            public override string ToString() => $"(afterActivation={AfterActivation}, atSpawn={AtSpawn}, remaining={Remaining})";
        }

        [HarmonyPatch(typeof(RelicEffectModifyUnitAbilityCooldown), nameof(RelicEffectModifyUnitAbilityCooldown.OnCharacterAdded))]
        private static class CooldownCallback
        {
            private static bool Prepare() => Enabled && WithOwner;

            private static void Postfix(RelicEffectModifyUnitAbilityCooldown __instance, CharacterState character, CardState fromCard,
                ICoreGameManagers coreGameManagers, ref IEnumerator __result)
            {
                if (Prepared && __instance.SourceRelicState.GetRelicDataID() == OwnerId &&
                    !coreGameManagers.GetSaveManager().PreviewMode && character.IsPyreHeart() &&
                    character.GetSourceCharacterData().GetID() == PyreId && fromCard == null)
                    __result = Observe(__result, character);
            }

            private static IEnumerator Observe(IEnumerator original, CharacterState character)
            {
                if (callbackBefore != null) throw new InvalidOperationException("Original Pyre cooldown callback ran more than once.");
                callbackBefore = Capture(character);
                yield return original;
                callbackAfter = Capture(character);
            }
        }
    }
}
#endif
