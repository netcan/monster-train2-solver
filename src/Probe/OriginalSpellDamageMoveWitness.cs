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
    internal static class OriginalSpellDamageMoveWitness
    {
        private const string OwnerId = RelicSpellDamageMoveModel.OwnerId;
        private const string OwnerKey = RelicSpellDamageMoveModel.OwnerKey;
        private const string TorchId = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
        private const string ControlId = "de1ef23c-c6bc-4d84-9df5-dac93a4b6a4f";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_SPELL_DAMAGE_MOVE") == "1";
        internal static bool Acquired { get; private set; }
        private static RelicEffectChanceAddEffectOnDamageCardPlayed? owner;
        private static CharacterState? survivor, lethal;
        private static CardState? card;
        private static BattleTurnState? before;
        private static PlayCardAction? action;
        private static readonly List<Test> tests = new List<Test>();
        private static readonly List<object> cases = new List<object>();
        private static int applications;

        internal static void Acquire(SaveManager save)
        {
            var definition = save.GetAllGameData().FindMutatorData(OwnerId);
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetMutators().Count != 0 || definition == null ||
                !RelicPercentageStatProbe.ValidSpellDamageMoveDefinition(definition) ||
                !save.IsDlcAvailableWhenStartingRun(definition.GetRequiredDLC()))
                throw new InvalidOperationException("Expected the unchanged available original MoveUpOnSpellDamage mutator.");
            save.AddMutator(definition);
            owner = save.GetMutators().Single().GetEffects().OfType<RelicEffectChanceAddEffectOnDamageCardPlayed>().Single();
            Acquired = true;
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var rooms = managers.GetRoomManager();
            var cards = managers.GetCardManager();
            if (!Acquired || managers.GetCombatManager().GetTurnCount() != 1)
                throw new InvalidOperationException("Expected the first ordinary native decision.");

            if (survivor == null)
            {
                var enemies = new List<CharacterState>();
                rooms.GetRoom(0).AddCharactersToList(enemies, Team.Type.Heroes);
                survivor = enemies.FirstOrDefault(unit => unit.IsAlive && !unit.IsDestroyed && unit.GetHP() > 3);
                lethal = enemies.FirstOrDefault(unit => unit.IsAlive && !unit.IsDestroyed && unit.GetHP() <= 3 &&
                    unit.GetStatusEffectStacks("armor") == 0 && unit.GetStatusEffectStacks("damageshield") == 0);
                if (survivor == null || lethal == null || ReferenceEquals(survivor, lethal))
                    throw new InvalidOperationException("Expected distinct stock survivor and lethal Torch targets on the lower floor.");
            }

            int index = cases.Count;
            CharacterState target = index == 2 ? lethal! : survivor;
            string kind = index == 0 ? "Survivor" : index == 1 ? "NonDamage" : "Lethal";
            string dataId = index == 1 ? ControlId : TorchId;
            if (before != null)
            {
                BattleTurnState after = trace.CaptureDecision();
                int targetId = action!.TargetUnitId;
                var actual = after.Spawn.Train.Rooms
                    .SelectMany(room => room.Units.Select(unit => (room.RoomIndex, Unit: unit)))
                    .SingleOrDefault(row => row.Unit.Id == targetId && row.Unit.Health > 0);
                bool moves = index == 0;
                if (tests.Count == 0 || tests.Any(test => !test.RngUnchanged) ||
                    tests.Any(test => test.Result) != moves || applications != (moves ? 1 : 0) ||
                    moves && (!tests.Any(test => test.Result && test.Targets.SequenceEqual(new[] { targetId })) ||
                        actual.Unit == null || actual.RoomIndex != action.RoomIndex + 1) ||
                    index == 1 && (actual.Unit == null || actual.RoomIndex != action.RoomIndex) ||
                    index == 2 && actual.Unit != null)
                    throw new InvalidOperationException("Original spell-damage move differed in " + kind +
                        ", test=" + string.Join(",", tests.Select(test => test.Result + "/" + string.Join("+", test.Targets))) +
                        ", applications=" + applications + ", floor=" + actual.RoomIndex + ", alive=" + (actual.Unit != null));
                cases.Add(new { Kind = kind, Before = before, Action = action, After = after, Tests = tests.ToArray(), Applications = applications });
                before = null; action = null; card = null; applications = 0; tests.Clear();
                if (cases.Count != 3) return false;
                using var archive = NativeFixtureCapture.Capture(new
                {
                    Schema = 1, Boundary = "NativeMoveUpOnSpellDamage", GameVersion = Application.version,
                    GameModuleMvid = Module, OwnerId, OwnerKey, Chance = RelicSpellDamageMoveModel.Chance,
                    Bump = RelicSpellDamageMoveModel.BumpAmount, Cases = cases
                });
                using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "spell-damage-move.mt2f"));
                archive.Write(output);
                return true;
            }

            int floor = target.GetCurrentRoomIndex();
            if (rooms.GetSelectedRoom() != floor)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(floor));
                return false;
            }
            if (card == null)
            {
                card = cards.GetHand().FirstOrDefault(item => item.GetCardDataID() == dataId);
                if (card == null)
                {
                    CardData data = save.GetAllGameData().FindCardData(dataId)
                        ?? throw new InvalidOperationException("The original control spell is unavailable.");
                    card = cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false)
                        ?? throw new InvalidOperationException("Could not add the original control spell.");
                    return false;
                }
            }
            var effect = card.GetEffectStates().Single();
            if (effect.GetTargetMode() != TargetMode.DropTargetCharacter ||
                (index == 1 ? effect.GetCardEffect() is not CardEffectAddStatusEffect : effect.GetCardEffect() is not CardEffectDamage))
                throw new InvalidOperationException("The original paid spell target definition changed.");
            SpawnPoint drop = target.GetSpawnPoint();
            if (!cards.CanPlayHandCard(card, floor, drop, null, null, out var error))
                throw new InvalidOperationException("The original paid spell failed its native feasibility check: " + error);
            before = trace.CaptureDecision();
            action = new PlayCardAction(trace.CardId(card), floor, targetUnitId: trace.UnitId(target));
            if (!cards.PlayCard(cards.GetHand().IndexOf(card), drop, ref error))
                throw new InvalidOperationException("The original paid spell was rejected: " + error);
            return false;
        }

        private static bool Observed(RelicEffectChanceAddEffectOnDamageCardPlayed instance, CardPlayedRelicEffectParams parameters) =>
            Enabled && before != null && ReferenceEquals(instance, owner) && ReferenceEquals(parameters.cardState, card) &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        private sealed class Test
        {
            public bool Result { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public bool RngUnchanged { get; set; }
        }

        [HarmonyPatch(typeof(RelicEffectChanceAddEffectOnDamageCardPlayed), nameof(RelicEffectChanceAddEffectOnDamageCardPlayed.TestEffectOnCardPlayed))]
        private static class TestPatch
        {
            private static bool Prepare() => Enabled;

            private static void Prefix(RelicEffectChanceAddEffectOnDamageCardPlayed __instance, CardPlayedRelicEffectParams relicEffectParams, out uint[]? __state)
            {
                __state = Observed(__instance, relicEffectParams) ? RngCalibration.Words(RandomManager.GetState(RngId.Battle)) : null;
            }

            private static void Postfix(RelicEffectChanceAddEffectOnDamageCardPlayed __instance, bool __result, uint[]? __state)
            {
                if (__state == null) return;
                var targets = (List<CharacterState>)AccessTools.Field(typeof(RelicEffectChanceAddEffectOnDamageCardPlayed), "damageTargets").GetValue(__instance);
                tests.Add(new Test
                {
                    Result = __result, Targets = targets.Select(FullBattleTrace.Active!.UnitId).ToArray(),
                    RngUnchanged = __state.SequenceEqual(RngCalibration.Words(RandomManager.GetState(RngId.Battle)))
                });
            }
        }

        [HarmonyPatch(typeof(RelicEffectChanceAddEffectOnDamageCardPlayed), nameof(RelicEffectChanceAddEffectOnDamageCardPlayed.ApplyEffectOnCardPlayed))]
        private static class ApplyPatch
        {
            private static bool Prepare() => Enabled;

            private static void Postfix(RelicEffectChanceAddEffectOnDamageCardPlayed __instance, CardPlayedRelicEffectParams relicEffectParams)
            {
                if (Observed(__instance, relicEffectParams)) applications++;
            }
        }
    }
}
#endif
