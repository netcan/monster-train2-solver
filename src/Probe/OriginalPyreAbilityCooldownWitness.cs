#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
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
        private static bool WithOwner => Mode is "WithOwner" or "WithOwnerReuse";
        internal static bool Reuse => Mode is "WithOwnerReuse" or "WithoutOwnerReuse";
        internal static bool Enabled => WithOwner || Mode == "WithoutOwner" || Reuse;
        internal static bool Prepared { get; private set; }
        internal static bool ReuseCasting => pendingCast != null;
        private static readonly List<ReuseCast> casts = new List<ReuseCast>();
        private static readonly List<ReuseSnapshot> turns = new List<ReuseSnapshot>();
        private static ReuseSnapshot? pendingCast;
        private static PlayCardAction? pendingAction;
        private static CardState? abilityCard;
        private static CooldownSample? callbackBefore, callbackAfter;

        internal enum ReuseInput { Wait, EndTurn, Complete }

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
            if (Reuse && (ability.GetEffects() is not [var freeze, var remove] ||
                freeze.GetEffectStateName() != nameof(CardEffectFreezeAllCards) ||
                remove.GetEffectStateName() != nameof(CardEffectRemoveAbility) || remove.GetTargetMode() != TargetMode.Pyre ||
                remove.GetParamRelicData()?.GetID() != OwnerId || remove.GetParamBool()))
                throw new InvalidOperationException("Original Time Pyre ability removal gate changed.");

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

        internal static ReuseInput TickReuse(FullBattleTrace trace, AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager()!;
            var rooms = managers.GetRoomManager()!;
            var cards = managers.GetCardManager()!;
            var pyre = rooms.GetPyreRoom().GetPyreHeart();
            int turn = combat.GetTurnCount();
            if (turn == 0)
            {
                Write(trace, managers);
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                turns.Add(Snapshot());
                return ReuseInput.EndTurn;
            }

            int unitId = trace.UnitId(pyre);
            if (pendingCast != null)
            {
                ReuseSnapshot after = Snapshot();
                var callback = AbilityEffectProbe.Records.LastOrDefault();
                bool paid = abilityCard!.GetLastPlayedCost() == 0 && after.Decision.Energy == pendingCast.Decision.Energy;
                bool sameCards = pendingCast.Hand.Select(card => card.Id).OrderBy(id => id)
                    .SequenceEqual(after.Hand.Select(card => card.Id).OrderBy(id => id));
                bool frozeCards = after.Hand.Any(card => card.FreezeTraits > 0);
                if (callback == null || !callback.Completed || callback.SourceCardId != pendingAction!.CardInstanceId ||
                    !callback.HasRelicGate || callback.Effect.AbilityChange?.BlockedByRelic != WithOwner ||
                    !callback.Targets.SequenceEqual(new[] { unitId }) || AbilityEffectProbe.Records.Count != casts.Count + 1 ||
                    !paid || !sameCards || !frozeCards ||
                    (WithOwner ? after.AbilityId != AbilityId || after.Cooldown.Remaining != 2 || after.DisabledPermanently ||
                        after.Availability != nameof(CharacterState.UnitAbilityAvailability.Cooldown) :
                        after.AbilityId != null || after.Cooldown.Remaining != 0 || after.DisabledPermanently ||
                        after.Availability != nameof(CharacterState.UnitAbilityAvailability.NoAbility)))
                    throw new InvalidOperationException($"Native Time Pyre cast differs. mode={Mode} turn={turn} " +
                        $"ability={after.AbilityId} cooldown={after.Cooldown} availability={after.Availability} disabled={after.DisabledPermanently} " +
                        $"zeroCost={paid} sameCards={sameCards} frozeCards={frozeCards} callbackCompleted={callback?.Completed} " +
                        $"callbackCard={callback?.SourceCardId} expectedCard={pendingAction?.CardInstanceId} " +
                        $"removeCallbacks={AbilityEffectProbe.Records.Count} targets=[{string.Join(",", callback?.Targets ?? Array.Empty<int>())}]");

                casts.Add(new ReuseCast { Action = pendingAction!, Before = pendingCast, After = after, RemoveAbility = callback });
                pendingCast = null;
                pendingAction = null;
                abilityCard = null;
                if (!WithOwner || casts.Count == 2)
                {
                    using var archive = NativeFixtureCapture.Capture(new
                    {
                        Schema = 1, Boundary = "OriginalPyreAbilityCooldownReuse", GameVersion = Application.version,
                        GameModuleMvid = Module, OwnerId, HasOwner = WithOwner, PyreId, AbilityId, UnitId = unitId,
                        BirthCallbackBefore = callbackBefore, BirthCallbackAfter = callbackAfter, Turns = turns, Casts = casts
                    });
                    using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "pyre-ability-cooldown-reuse.mt2f"));
                    archive.Write(stream);
                    return ReuseInput.Complete;
                }
                return ReuseInput.EndTurn;
            }

            if (turn == 2 && WithOwner && casts.Count == 1)
            {
                var tick = Snapshot();
                if (tick.AbilityId != AbilityId || tick.Cooldown.Remaining != 1 ||
                    tick.Availability != nameof(CharacterState.UnitAbilityAvailability.Cooldown))
                    throw new InvalidOperationException($"Expected retained Time ability with one tick remaining, found {tick.Cooldown} {tick.Availability}.");
                turns.Add(tick);
                return ReuseInput.EndTurn;
            }

            if (turn != (casts.Count == 0 ? 1 : 3) || !pyre.CanActivateUnitAbility())
                throw new InvalidOperationException($"Expected available Time ability before casting, turn={turn}, " +
                    $"cooldown={Capture(pyre)} availability={pyre.GetUnitAbilityAvailability(combat)}.");
            int selected = rooms.GetSelectedRoom();
            if (!pyre.CanTargetUnitAbilityInRoom(selected))
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(pyre.GetCurrentRoomIndex()));
                return ReuseInput.Wait;
            }

            abilityCard = pyre.GetUnitAbilityCardState();
            if (abilityCard == null || abilityCard.GetCardDataID() != AbilityId ||
                (!abilityCard.GetTargetless() && !abilityCard.GetTargetsRoom()))
                throw new InvalidOperationException("Original Time Pyre ability is missing or needs a drop target.");
            var action = new PlayCardAction(trace.CardId(abilityCard), selected, activatorUnitId: unitId);
            pendingCast = Snapshot();
            pendingAction = action;
            if (!pyre.ActivateUnitAbility(cards))
                throw new InvalidOperationException($"Native Time ability rejected activation on turn {turn}, room {selected}.");
            return ReuseInput.Wait;

            ReuseSnapshot Snapshot() => new ReuseSnapshot(trace.CaptureDecision(), Capture(pyre), pyre.GetUnitAbility()?.GetID(),
                pyre.GetUnitAbilityAvailability(combat).ToString(), save.HasAbilityBeenPermanentlyDisabled(AbilityId),
                cards.GetHand().Select(card => new HandFreeze(trace.CardId(card), card.GetTraitStates().Count(trait => trait is CardTraitFreeze))).ToArray());
        }

        private sealed class HandFreeze
        {
            public int Id { get; }
            public int FreezeTraits { get; }
            internal HandFreeze(int id, int freezeTraits) { Id = id; FreezeTraits = freezeTraits; }
        }

        private sealed class ReuseSnapshot
        {
            public BattleTurnState Decision { get; }
            public CooldownSample Cooldown { get; }
            public string? AbilityId { get; }
            public string Availability { get; }
            public bool DisabledPermanently { get; }
            public HandFreeze[] Hand { get; }

            internal ReuseSnapshot(BattleTurnState decision, CooldownSample cooldown, string? abilityId, string availability,
                bool disabledPermanently, HandFreeze[] hand)
            {
                Decision = decision;
                Cooldown = cooldown;
                AbilityId = abilityId;
                Availability = availability;
                DisabledPermanently = disabledPermanently;
                Hand = hand;
            }
        }

        private sealed class ReuseCast
        {
            public PlayCardAction Action { get; set; } = null!;
            public ReuseSnapshot Before { get; set; } = null!;
            public ReuseSnapshot After { get; set; } = null!;
            public AbilityEffectProbe.Record RemoveAbility { get; set; } = null!;
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
