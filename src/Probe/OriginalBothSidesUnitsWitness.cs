#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBothSidesUnitsWitness
    {
        private const string OwnerId = "b75d5726-62ef-47ce-9378-ac3721594bab";
        private const string OwnerKey = "BothSidesUnitsAreStronger";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly (string Class, string Team, string Trigger, int Amount, string CardType)[] Shape =
        {
            (nameof(RelicEffectModifyCharacterMaxHealth), "Heroes", "OnSpawn", 10, "Invalid"),
            (nameof(RelicEffectModifyCharacterMaxHealth), "Monsters", "OnSpawnNotFromCard", 10, "Invalid"),
            (nameof(RelicEffectModifyCharacterAttackDamage), "Heroes", "OnSpawn", 5, "Invalid"),
            (nameof(RelicEffectModifyCharacterAttackDamage), "Monsters", "OnSpawnNotFromCard", 5, "Invalid"),
            (nameof(RelicEffectModifyCardAttack), "Monsters", "OnSpawn", 5, "Monster"),
            (nameof(RelicEffectModifyCardHealth), "Monsters", "OnSpawn", 10, "Monster"),
            (nameof(RelicEffectTowerDamageDealtModified), "Monsters", "OnSpawn", 10, "Monster")
        };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_BOTH_SIDES_UNITS_WITNESS") == "1";
        internal static bool FullBattle => Environment.GetEnvironmentVariable("MT2_PROBE_BOTH_SIDES_UNITS_BATTLE") == "1";
        internal static bool Cardless => Environment.GetEnvironmentVariable("MT2_PROBE_BOTH_SIDES_UNITS_CARDLESS") == "1";
        internal static bool Acquired => ownerDefinition != null;
        private static object? ownerDefinition;
        private static readonly Dictionary<IRelicEffect, int> Installed = new();
        private static readonly List<BirthSample> Births = new();
        private static CardState? playedCard;
        private static BattleTurnState? beforePlay, beforeEndTurn;
        private static object? beforePyre;
        private static bool observingBirths;

        internal static void Acquire(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0 || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the pinned original rank-zero Ring 1 run before combat.");
            MutatorData definition = save.GetAllGameData().FindMutatorData(OwnerId)
                ?? throw new InvalidOperationException("Original BothSidesUnitsAreStronger mutator was missing.");
            var effects = definition.GetEffects().ToArray();
            if (definition.name != OwnerKey || definition.DisallowInDeploymentPhase || effects.Length != Shape.Length ||
                !effects.Select((effect, index) => Shape[index] == (effect.GetEffectClassName(),
                    effect.GetParamSourceTeam().ToString(), effect.GetParamTrigger().ToString(), effect.GetParamInt(),
                    effect.GetParamCardType().ToString()) && effect.GetEffectConditions().Count == 0 &&
                    effect.GetTargetMode() == TargetMode.FrontInRoom && effect.GetParamCharacterSubtype()?.IsNone == true &&
                    effect.GetParamInt2() == 0 && effect.GetParamFloat() == 0 && !effect.GetParamBool() &&
                    !effect.GetParamBool2() && !effect.GetParamBool3() && !effect.GetParamBool4() &&
                    effect.GetParamMutatorData() == null && effect.GetParamCardUpgradeData() == null &&
                    effect.GetParamCardFilter() == null && string.IsNullOrEmpty(effect.GetParamString())).All(value => value))
                throw new InvalidOperationException("Original BothSidesUnitsAreStronger authored payload changed.");
            save.AddMutator(definition);
            var owner = save.GetMutators().Single();
            var installed = owner.GetEffects().ToArray();
            if (owner.GetRelicDataID() != OwnerId || !installed.Select(effect => effect.GetType().Name).SequenceEqual(Shape.Select(row => row.Class)))
                throw new InvalidOperationException("Original stats effects did not initialize in authored order.");
            for (int index = 0; index < installed.Length; index++) Installed.Add(installed[index], index);
            ownerDefinition = new
            {
                Id = OwnerId, AssetKey = OwnerKey, definition.DisallowInDeploymentPhase,
                Effects = effects.Select((effect, index) => new
                {
                    Index = index, Type = effect.GetEffectClassName(), Team = effect.GetParamSourceTeam().ToString(),
                    Trigger = effect.GetParamTrigger().ToString(), Amount = effect.GetParamInt(),
                    CardType = effect.GetParamCardType().ToString(), TargetMode = effect.GetTargetMode().ToString(),
                    Subtype = effect.GetParamCharacterSubtype().Key, Conditions = effect.GetEffectConditions().Count,
                    installed[index].CanApplyInPreviewMode
                }).ToArray()
            };
        }

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log, Action advanceTurn)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            if (!Acquired || save.PreviewMode)
                throw new InvalidOperationException("Expected a settled original live decision.");
            if (rooms.GetSelectedRoom() != 0)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(0));
                return false;
            }
            if (Cardless) return TickCardless(trace, managers, log);
            if (beforePlay == null)
            {
                if (!combat.IsPlacementPhase)
                    throw new InvalidOperationException("Expected the original turn-zero placement hand.");
                playedCard = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == StewardId)
                    ?? throw new InvalidOperationException("No original Train Steward in the native placement hand.");
                var point = rooms.GetRoom(0).GetFirstEmptyMonsterPoint()
                    ?? throw new InvalidOperationException("No native floor-zero placement point.");
                if (!cards.CanPlayHandCard(playedCard, 0, point, null, null, out var error))
                    throw new InvalidOperationException("Original Train Steward was unavailable: " + error);
                beforePlay = trace.CaptureDecision();
                beforePyre = CapturePyre(managers);
                if (!cards.PlayCard(cards.GetHand().IndexOf(playedCard), point, ref error))
                    throw new InvalidOperationException("Original Train Steward was rejected: " + error);
                return false;
            }

            var action = new PlayCardAction(trace.CardId(playedCard!), 0);
            if (beforeEndTurn == null)
            {
                beforeEndTurn = trace.CaptureDecision();
                var priorCard = beforePlay.Spawn.Train.Context!.FindCard(action.CardInstanceId);
                var actor = beforeEndTurn.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                    .Single(unit => unit.SpawnerCardId == action.CardInstanceId);
                if (!combat.IsPlacementPhase || cards.GetHand().Contains(playedCard!) ||
                    priorCard?.Temporary.Offsets.Damage != 5 || priorCard?.Temporary.Offsets.Health != 10 ||
                    actor.Attack != 13 || actor.Health != 35 || actor.MaxHealth != 35)
                    throw new InvalidOperationException("Original paid Train Steward did not inherit +5 attack and +10 health.");
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                observingBirths = true;
                advanceTurn();
                return false;
            }

            observingBirths = false;
            var afterWave = trace.CaptureDecision();
            var enemyIds = afterWave.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(actor => actor.Team == CombatTeam.Enemy).Select(actor => actor.Id).ToHashSet();
            var births = CaptureBirths(trace);
            if (combat.GetTurnCount() != 1 || enemyIds.Count == 0 ||
                !births.Any(health => health.EffectIndex == 0 && enemyIds.Contains(health.UnitId) &&
                    health.After.Health - health.Before.Health == 10 && health.After.MaxHealth - health.Before.MaxHealth == 10 &&
                    births.Any(attack => attack.UnitId == health.UnitId && attack.EffectIndex == 2 &&
                        attack.After.DamageBuff - attack.Before.DamageBuff == 5)))
                throw new InvalidOperationException("No first-wave original enemy settled with both original birth bonuses.");
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = OwnerKey, GameVersion = Application.version, GameModuleMvid = Module, Owner = ownerDefinition,
                BeforePlay = beforePlay, Play = action, BeforeEndTurn = beforeEndTurn, AfterWave = afterWave,
                PyreBeforePlay = beforePyre, PyreAfterWave = CapturePyre(managers), Births = births
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "both-sides-units.mt2f"));
            archive.Write(stream);
            log.LogInfo("ORIGINAL-BOTH-SIDES-UNITS OBSERVED paid Steward, enemy callback pairs=" + births.Length + ", native Pyre base query");
            return true;
        }

        private static bool TickCardless(FullBattleTrace trace, AllGameManagers managers, ManualLogSource log)
        {
            var cards = managers.GetCardManager();
            if (!managers.GetCombatManager().IsPlacementPhase)
                throw new InvalidOperationException("Expected the original turn-zero placement hand.");
            if (beforePlay == null)
            {
                CardData definition = managers.GetAllGameData().FindCardData(OriginalEagerConscriptScenario.CardId)
                    ?? throw new InvalidOperationException("Original Eager Conscript was missing.");
                CardEffectData effect = definition.GetEffects().Single();
                if (definition.name != "EagerConscript" || definition.GetCardType() != CardType.Spell || definition.GetCost() != 1 ||
                    effect.GetEffectStateName() != nameof(CardEffectSpawnMonsterOrAddStatusEffect) ||
                    effect.GetTargetMode() != TargetMode.Room || effect.GetTargetTeamType() != Team.Type.Monsters ||
                    effect.GetTargetCharacterSubtype()?.Key != "SubtypesData_Horde" ||
                    effect.GetParamCharacterData()?.name != "MonsterFunguy" ||
                    effect.GetParamStatusEffects() is not [{ statusId: "horde", count: 1 }])
                    throw new InvalidOperationException("Original Eager Conscript authored birth changed.");
                playedCard = cards.GetHand().FirstOrDefault(card => card.GetCardDataID() == definition.GetID()) ??
                    cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false)
                    ?? throw new InvalidOperationException("Original Eager Conscript could not enter the native hand.");
                if (!cards.CanPlayHandCard(playedCard, 0, null, null, null, out var error))
                    throw new InvalidOperationException("Original Eager Conscript was unavailable: " + error);
                beforePlay = trace.CaptureDecision();
                observingBirths = true;
                if (!cards.PlayCard(cards.GetHand().IndexOf(playedCard), null, ref error))
                    throw new InvalidOperationException("Original Eager Conscript was rejected: " + error);
                return false;
            }

            observingBirths = false;
            var after = trace.CaptureDecision();
            var action = new PlayCardAction(trace.CardId(playedCard!), 0);
            var births = CaptureBirths(trace);
            var fungus = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.AssetKey == "MonsterFunguy");
            if (beforePlay.Energy - after.Energy != 1 || cards.GetHand().Contains(playedCard!) ||
                fungus.SpawnerCardId != 0 || fungus.Status("cardless")?.Stacks != 1 || fungus.Status("horde")?.Stacks != 1 ||
                fungus.Modifiers?.IsClone != false ||
                !births.Select(row => row.EffectIndex).SequenceEqual(new[] { 1, 4, 3 }) ||
                births.Any(row => row.UnitId != fungus.Id || row.FromCard))
                throw new InvalidOperationException("Original paid Eager Conscript did not settle the three cardless birth callbacks.");
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = OwnerKey + "Cardless", GameVersion = Application.version, GameModuleMvid = Module,
                Owner = ownerDefinition, DonorCardDataId = OriginalEagerConscriptScenario.CardId, BeforePlay = beforePlay,
                Play = action, AfterPlay = after, Births = births
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "both-sides-units-cardless.mt2f"));
            archive.Write(stream);
            log.LogInfo("ORIGINAL-BOTH-SIDES-UNITS OBSERVED paid Eager Conscript cardless birth with three callback states");
            return true;
        }

        private static object CapturePyre(AllGameManagers managers)
        {
            SaveManager save = managers.GetSaveManager();
            var pyre = managers.GetRoomManager().GetPyreRoom().GetPyreHeart();
            var before = BattleRng();
            int authored = save.GetActiveSavePyreCharacterData().GetPyreHeartData().GetAttack();
            int persistent = save.GetRelicPersistentState()?.GetPyreDamageIncrease() ?? 0;
            int queried = save.GetBasePyreAttack();
            int combat = managers.GetCombatManager().GetModifiedPyreDamage();
            int upgraded = pyre.GetAttackDamageAddedFromUpgrades();
            int raw = UnitModifierProbe.Capture(pyre).AttackDamage;
            var after = BattleRng();
            if (queried != authored + persistent + 10 || combat != queried + upgraded || raw != combat ||
                !JToken.DeepEquals(before, after))
                throw new InvalidOperationException("Native Pyre base query did not retain the original +10 exactly once.");
            return new { AuthoredBase = authored, PersistentBonus = persistent, BaseAttack = queried, CombatAttack = combat,
                RawAttack = raw, UpgradeAttack = upgraded, SavedAttackOffset = save.GetModifiedPyreBaseAttack(),
                RngBefore = before, RngAfter = after };
        }

        private static JArray BattleRng() => new JArray(new[] { RngId.Battle, RngId.BattleTest }
            .Select(id => JObject.FromObject(new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) })));

        private sealed class ActorStats
        {
            public int Health { get; }
            public int MaxHealth { get; }
            public int DamageBuff { get; }
            public ActorStats(CharacterState actor)
            {
                Health = actor.GetHP();
                MaxHealth = actor.GetMaxHP();
                DamageBuff = UnitModifierProbe.Capture(actor).DamageBuff;
            }
        }

        private sealed class BirthSample
        {
            public CharacterState Actor = null!;
            public int EffectIndex;
            public string Trigger = "";
            public bool FromCard;
            public ActorStats Before = null!, After = null!;
        }

        private sealed class CapturedBirth
        {
            public int UnitId { get; }
            public string AssetKey { get; }
            public int EffectIndex { get; }
            public string Trigger { get; }
            public bool FromCard { get; }
            public ActorStats Before { get; }
            public ActorStats After { get; }
            public CapturedBirth(FullBattleTrace trace, BirthSample sample)
            {
                UnitId = trace.UnitId(sample.Actor);
                AssetKey = sample.Actor.GetSourceCharacterData().GetAssetKey();
                EffectIndex = sample.EffectIndex;
                Trigger = sample.Trigger;
                FromCard = sample.FromCard;
                Before = sample.Before;
                After = sample.After;
            }
        }

        private static CapturedBirth[] CaptureBirths(FullBattleTrace trace) => Births.Select(row =>
            new CapturedBirth(trace, row)).ToArray();

        private static bool ObserveActor(IRelicEffect effect, CharacterState actor, int effectIndex, ICoreGameManagers managers) =>
            observingBirths && Installed.TryGetValue(effect, out var index) && index == effectIndex &&
            !managers.GetSaveManager().PreviewMode && !actor.PreviewMode &&
            (Cardless ? actor.GetTeamType() == Team.Type.Monsters && actor.HasStatusEffect("cardless") :
                actor.GetTeamType() == Team.Type.Heroes);

        private static IEnumerator Observe(IEnumerator native, CharacterState actor, int index, string trigger, bool fromCard)
        {
            ActorStats before = new ActorStats(actor);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                Births.Add(new BirthSample { Actor = actor, EffectIndex = index, Trigger = trigger, FromCard = fromCard,
                    Before = before, After = new ActorStats(actor) });
            }
            finally { (native as IDisposable)?.Dispose(); }
        }

        [HarmonyPatch(typeof(RelicEffectModifyCharacterMaxHealth), nameof(RelicEffectModifyCharacterMaxHealth.OnCharacterAdded))]
        private static class HealthPatch
        {
            private static void Postfix(RelicEffectModifyCharacterMaxHealth __instance, CharacterState character,
                CardState? fromCard, ICoreGameManagers coreGameManagers, ref IEnumerator __result)
            {
                int index = Cardless ? 1 : 0;
                if (ObserveActor(__instance, character, index, coreGameManagers))
                    __result = Observe(__result, character, index, nameof(RelicEffectModifyCharacterMaxHealth.OnCharacterAdded), fromCard != null);
            }
        }

        [HarmonyPatch(typeof(RelicEffectModifyCardAttack), nameof(RelicEffectModifyCardAttack.OnCharacterAdded))]
        private static class CardAttackPatch
        {
            private static void Postfix(RelicEffectModifyCardAttack __instance, CharacterState character,
                CardState? fromCard, ICoreGameManagers coreGameManagers, ref IEnumerator __result)
            {
                if (Cardless && ObserveActor(__instance, character, 4, coreGameManagers))
                    __result = Observe(__result, character, 4, nameof(RelicEffectModifyCardAttack.OnCharacterAdded), fromCard != null);
            }
        }

        [HarmonyPatch(typeof(RelicEffectModifyCharacterAttackDamage), nameof(RelicEffectModifyCharacterAttackDamage.ApplyCharacterTriggerEffect))]
        private static class AttackPatch
        {
            private static void Postfix(RelicEffectModifyCharacterAttackDamage __instance, CharacterTriggerRelicEffectParams relicEffectParams,
                ICoreGameManagers coreGameManagers, ref IEnumerator __result)
            {
                var actor = relicEffectParams.characterState;
                int index = Cardless ? 3 : 2;
                var trigger = Cardless ? CharacterTriggerData.Trigger.OnSpawnNotFromCard : CharacterTriggerData.Trigger.OnSpawn;
                if (actor != null && relicEffectParams.trigger == trigger && ObserveActor(__instance, actor, index, coreGameManagers))
                    __result = Observe(__result, actor, index, trigger.ToString(), actor.GetSpawnerCard() != null);
            }
        }
    }
}
#endif
