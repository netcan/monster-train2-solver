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
    internal static class OriginalAllSubtypesWitness
    {
        internal const string FileName = "all-unit-subtypes.mt2f";
        private const string OwnerId = OriginalAllSubtypesModel.OwnerId;
        private const string OwnerKey = OriginalAllSubtypesModel.OwnerKey;
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly List<object> Actors = new List<object>();
        private static readonly List<object> Deck = new List<object>();
        private static readonly List<object> Masks = new List<object>();
        private static readonly List<object> SpawnCounts = new List<object>();
        private static readonly AccessTools.FieldRef<CardUpgradeMaskData, List<string>> RequiredSubtypes =
            AccessTools.FieldRefAccess<CardUpgradeMaskData, List<string>>("requiredSubtypes");
        private static readonly AccessTools.FieldRef<CardUpgradeMaskData, List<string>> ExcludedSubtypes =
            AccessTools.FieldRefAccess<CardUpgradeMaskData, List<string>>("excludedSubtypes");
        private static readonly System.Reflection.MethodInfo CardsWithSubtype = AccessTools.Method(typeof(CardStatistics), "GetCardsWithCharacterSubtype");
        private static CardState? deployed;
        private static CharacterState? dying;
        private static SubtypeData[]? actorQueries;
        private static bool started;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ALL_UNIT_SUBTYPES") == "1";
        internal static bool FullBattle => Environment.GetEnvironmentVariable("MT2_PROBE_ALL_UNIT_SUBTYPES_BATTLE") == "1";
        internal static bool BattleAcquired { get; private set; }
        internal static bool Completed { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers)
        {
            SaveManager save = managers.GetSaveManager();
            if (save.GetCurrentDistance() != 0 || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.InTestScenario || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Expected original AllUnitsAllSubtypes setup before the isolated first battle.");
            Acquire(managers);
            BattleAcquired = true;
        }

        internal static void Tick(FullBattleTrace trace, AllGameManagers managers, Action advanceTurn)
        {
            if (started) return;
            SaveManager save = managers.GetSaveManager();
            if (Application.version != "2.3.1" || typeof(RelicEffectMonstersAreAllSubtypes).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0 ||
                save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected a stock first battle on the pinned original module.");

            CombatManager combat = managers.GetCombatManager();
            RoomManager rooms = managers.GetRoomManager();
            CardManager cards = managers.GetCardManager();
            int turn = combat.GetTurnCount();
            if (turn == 0)
            {
                if (deployed == null)
                {
                    int selected = rooms.GetSelectedRoom();
                    SpawnPoint point = rooms.GetRoom(selected).GetFirstEmptyMonsterPoint()!;
                    deployed = cards.GetHand().First(card => card.GetSpawnCharacterData() is CharacterData unit && !unit.IsChampion() &&
                        cards.CanPlayHandCard(card, selected, point, null, null, out _));
                    if (!cards.CanPlayHandCard(deployed, selected, point, null, null, out var error) ||
                        !cards.PlayCard(cards.GetHand().IndexOf(deployed), point, ref error))
                        throw new InvalidOperationException("Native subtype witness unit deployment failed: " + error);
                }
                else
                {
                    save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                    advanceTurn();
                }
                return;
            }

            if (turn != 1 || deployed == null)
                throw new InvalidOperationException("Expected the first ordinary decision after original deployment.");
            var friendlyActors = new List<CharacterState>();
            for (int floor = 0; floor < rooms.GetNumRooms(); floor++) rooms.GetRoom(floor).AddCharactersToList(friendlyActors, Team.Type.Monsters);
            CharacterState friendly = friendlyActors.Single(actor => actor.IsAlive && ReferenceEquals(actor.GetSpawnerCard(), deployed));
            var foes = new List<CharacterState>();
            rooms.GetRoom(0).AddCharactersToList(foes, Team.Type.Heroes);
            CharacterState foe = foes.First(actor => actor.IsAlive);
            CharacterState pyre = rooms.GetPyreRoom().GetPyreHeart();
            SubtypeData fungus = SubtypeManager.GetSubtypeData("SubtypesData_FunGuy");
            SubtypeData horde = SubtypeManager.GetSubtypeData("SubtypesData_Horde");
            SubtypeData champion = cards.GetAllCards(new List<CardState>()).SelectMany(card =>
                card.GetSpawnCharacterData()?.GetSubtypes() ?? new List<SubtypeData>()).First(subtype => subtype.IsChampion);
            SubtypeData pyreSubtype = pyre.GetSubtypes().Single(subtype => subtype.IsPyre);
            SubtypeData rawFriendly = friendly.GetSubtypes().First(subtype => !subtype.IsNone);
            actorQueries = new[] { SubtypeData.None, rawFriendly, fungus, horde, champion, pyreSubtype };

            using var masks = new QueryMasks(fungus, rawFriendly);
            Observe("Before", friendly, foe, pyre, deployed, masks, managers, trace);
            BattleTurnState before = trace.CaptureDecision();
            RelicEffectData effect = Acquire(managers);
            Observe("Active", friendly, foe, pyre, deployed, masks, managers, trace);
            BattleTurnState active = trace.CaptureDecision();
            if (HalfHealthLifecycleCalibration.Difference(before.RngStreams, active.RngStreams) != null)
                throw new InvalidOperationException("Native query preparation changed gameplay RNG.");
            started = true;
            dying = friendly;
            save.StartCoroutine(CaptureDeath());

            IEnumerator CaptureDeath()
            {
                yield return friendly.Sacrifice(null!);
                if (dying != null) yield break;
                using var archive = NativeFixtureCapture.Capture(new
                {
                    Schema = 1, Boundary = "OriginalAllUnitsAllSubtypesQueries", GameVersion = Application.version, GameModuleMvid = Module,
                    Owner = new { Id = OwnerId, AssetKey = OwnerKey, Effect = effect.GetEffectClassName(), Conditions = effect.GetEffectConditions().Count },
                    Before = before, Active = active, Actors, Deck, Masks, SpawnCounts
                });
                using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName));
                archive.Write(stream);
                Completed = true;
            }
        }

        internal static RelicEffectData Acquire(AllGameManagers managers)
        {
            SaveManager save = managers.GetSaveManager();
            RelicManager relics = managers.GetRelicManager();
            if (Application.version != "2.3.1" || typeof(RelicEffectMonstersAreAllSubtypes).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetMutators().Count != 0 || relics.GetRelicEffect<RelicEffectMonstersAreAllSubtypes>() != null ||
                save.GetAllGameData().FindMutatorData(OwnerId) is not MutatorData owner || owner.name != OwnerKey ||
                owner.DisallowInDeploymentPhase || owner.GetEffects() is not [var effect] ||
                effect.GetEffectClassName() != nameof(RelicEffectMonstersAreAllSubtypes) || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Missing or changed original AllUnitsAllSubtypes owner or nonempty baseline.");
            save.AddMutator(owner);
            if (save.GetMutators() is not [var native] || native.GetRelicDataID() != OwnerId ||
                native.GetEffects() is not [RelicEffectMonstersAreAllSubtypes loaded] ||
                !ReferenceEquals(relics.GetRelicEffect<RelicEffectMonstersAreAllSubtypes>(), loaded))
                throw new InvalidOperationException("The original all-subtypes effect did not activate.");
            return effect;
        }

        private static void Observe(string phase, CharacterState friendly, CharacterState foe, CharacterState pyre,
            CardState card, QueryMasks masks, AllGameManagers managers, FullBattleTrace trace)
        {
            ObserveActor(phase, "Friendly", friendly, trace);
            ObserveActor(phase, "Foe", foe, trace);
            ObserveActor(phase, "Pyre", pyre, trace);
            CardStatistics stats = managers.GetCardStatistics();
            RelicManager relics = managers.GetRelicManager();
            foreach (SubtypeData subtype in actorQueries!.Where(value => !value.IsNone))
            {
                var matched = (List<CardState>)CardsWithSubtype.Invoke(stats, new object?[]
                    { subtype, CardStatistics.CardTypeTarget.Any, relics, null, TargetMode.Room });
                Deck.Add(new { Phase = phase, subtype.Key, subtype.IsChampion, Matched = matched.Select(trace.CardId).OrderBy(id => id).ToArray() });
                foreach (var duration in new[] { CardStatistics.EntryDuration.ThisTurn, CardStatistics.EntryDuration.ThisBattle })
                {
                    int Count(CardStatistics.TrackedValueType type) => stats.GetStatValue(new CardStatistics.StatValueData
                        { trackedValue = type, entryDuration = duration, paramSubtype = subtype, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
                    SpawnCounts.Add(new { Phase = phase, Duration = duration.ToString(), subtype.Key,
                        SubtypeCount = Count(CardStatistics.TrackedValueType.MonsterSubtypePlayed), AllCount = Count(CardStatistics.TrackedValueType.AnyMonsterSpawned) });
                }
            }
            ObserveMask("Required", masks.Required);
            ObserveMask("Excluded", masks.Excluded);
            void ObserveMask(string mode, CardUpgradeMaskData mask)
            {
                var rule = CardUpgradeMaskProbe.Rule(mask);
                Masks.Add(new { Phase = phase, Kind = "Card", Mode = mode, SubjectId = trace.CardId(card), Mask = rule, Actual = mask.FilterCard(card, relics) });
                Masks.Add(new { Phase = phase, Kind = "Character", Mode = mode, SubjectId = trace.UnitId(friendly), Mask = rule, Actual = mask.FilterCharacter(friendly, relics) });
            }
        }

        private static void ObserveActor(string phase, string role, CharacterState actor, FullBattleTrace trace)
        {
            ICharacterManager? manager = actor.GetCharacterManager();
            Actors.Add(new { Phase = phase, Role = role, ActorId = trace.UnitId(actor), Alive = actor.IsAlive, IsPyre = actor.IsPyreHeart(),
                RawSubtypes = actor.GetSubtypes().Select(subtype => subtype.Key).ToArray(),
                Queried = actorQueries!.Select(subtype => new { subtype.Key, subtype.IsChampion, subtype.IsPyre,
                    Actual = manager!.DoesCharacterPassSubtypeCheck(actor, subtype) }).ToArray() });
        }

        private sealed class QueryMasks : IDisposable
        {
            internal readonly CardUpgradeMaskData Required = ScriptableObject.CreateInstance<CardUpgradeMaskData>();
            internal readonly CardUpgradeMaskData Excluded = ScriptableObject.CreateInstance<CardUpgradeMaskData>();
            internal QueryMasks(SubtypeData required, SubtypeData excluded)
            {
                RequiredSubtypes(Required) = new List<string> { required.Key };
                ExcludedSubtypes(Excluded) = new List<string> { excluded.Key };
            }
            public void Dispose()
            {
                UnityEngine.Object.Destroy(Required);
                UnityEngine.Object.Destroy(Excluded);
            }
        }

        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.CheckDeathCardTriggers))]
        private static class DyingPatch
        {
            private static void Prefix(CharacterState __instance)
            {
                if (dying == null || !ReferenceEquals(__instance, dying) || __instance.IsAlive ||
                    FullBattleTrace.Active is not FullBattleTrace trace) return;
                ObserveActor("Dying", "Friendly", __instance, trace);
                dying = null;
            }
        }
    }
}
#endif
