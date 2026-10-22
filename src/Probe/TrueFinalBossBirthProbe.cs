using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // An observational component boundary. It does not opt Level9 into battle
    // simulation, alter character data, or claim a naturally unlocked run.
    internal static class TrueFinalBossBirthProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_BIRTH") == "1";
        internal static bool RouteEnabled => Enabled || Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_TRUE_FINAL_BOSS") == "1";
        internal static object? Control => route;
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static object? route;
        private static bool observing;
        private static readonly List<CharacterState> created = new List<CharacterState>();
        private static readonly List<object> creation = new List<object>();
        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Select(id => new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
        private static int Id(CharacterState? character) => character == null ? 0 : created.IndexOf(character) + 1;
        private static int[] Queue(HeroManager heroes) => ((List<int>)AccessTools.Field(typeof(HeroManager), "queuedTrueFinalBosses").GetValue(heroes)).ToArray();

        internal static void Navigate(SaveManager save, MapScreen map, ManualLogSource log)
        {
            if (route != null) throw new InvalidOperationException("True-final-boss route was already selected.");
            bool unlockedBefore = save.Cheats.ForceTFBUnlocked;
            int distanceBefore = save.GetCurrentDistance();
            object before = Rng();
            map.Cheat_JumpToTFB();
            object after = Rng();
            route = new { Kind = "NativeCheatJumpToTFB", UnlockOverrideBefore = unlockedBefore,
                UnlockOverrideAfter = save.Cheats.ForceTFBUnlocked, DistanceBefore = distanceBefore,
                DistanceAfter = save.GetCurrentDistance(), Scenario = save.GetCurrentScenarioData()?.name,
                RngBefore = before, RngAfter = after, ScenarioAssetsUnchanged = true,
                CharacterAssetsUnchanged = true, StartingDeckUnchanged = true, PyreHealth = save.GetTowerHP() };
            log.LogInfo("TRUE-FINAL-BOSS-ROUTE native jump; unlock override=" + save.Cheats.ForceTFBUnlocked +
                " distance=" + save.GetCurrentDistance() + " scenario=" + save.GetCurrentScenarioData()?.name);
        }

        private static object Snapshot(HeroManager heroes)
        {
            RoomManager rooms = AllGameManagers.Instance.GetRoomManager();
            return new
            {
                QueuedBosses = Queue(heroes), OuterBossId = Id(heroes.GetOuterTrainBossCharacter()),
                ActiveBossIds = heroes.GetCharactersReadOnly().Where(character => character.IsTrueFinalBoss()).Select(Id).ToArray(),
                Characters = created.Select(character => new
                {
                    Id = Id(character), AssetKey = character.GetSourceCharacterData().GetAssetKey(),
                    DataId = character.GetSourceCharacterData().GetID(), LinkedId = Id(character.GetLinkedCharacterState()),
                    Room = character.GetCurrentRoomIndex(), Health = character.GetHP(), MaxHealth = character.GetMaxHP(),
                    Attack = character.GetAttackDamage(), CanAttack = character.GetCanAttack(),
                    IsTrueFinalBoss = character.IsTrueFinalBoss(), IsOuterTrainBoss = character.IsOuterTrainBoss(),
                    Phase = character.GetBossState().GetCurrentAttackPhase().ToString(),
                    PhysicalRooms = Enumerable.Range(0, rooms.GetNumRooms()).Where(index =>
                    {
                        var characters = new List<CharacterState>();
                        rooms.GetRoom(index).AddCharactersToList(characters, Team.Type.Heroes);
                        return characters.Contains(character);
                    }).ToArray()
                }).ToArray(),
                Rooms = Enumerable.Range(0, rooms.GetNumRooms()).Select(index => new
                {
                    Index = index, Enabled = rooms.GetRoom(index).IsRoomEnabled(),
                    OuterBossId = Id(rooms.GetRoom(index).GetOuterTrainBoss())
                }).ToArray(),
                Rng = Rng()
            };
        }

        private static IEnumerator Observe(IEnumerator native, HeroManager heroes)
        {
            if (observing) throw new InvalidOperationException("Nested true-final-boss birth observation.");
            observing = true;
            int[] before = Queue(heroes);
            bool returned = false;
            try { while (native.MoveNext()) yield return native.Current; returned = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                observing = false;
                try
                {
                    object rngBefore = Rng();
                    object actual = Snapshot(heroes);
                    object repeat = Snapshot(heroes);
                    object rngAfter = Rng();
                    bool pure = JToken.DeepEquals(JToken.FromObject(actual), JToken.FromObject(repeat)) &&
                        JToken.DeepEquals(JToken.FromObject(rngBefore), JToken.FromObject(rngAfter));
                    using (var document = NativeFixtureCapture.Capture(new
                    {
                        Schema = 1, GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                        EvidenceKind = "NativeObservationOnly", Boundary = "AfterDoTrueFinalBossSpawning",
                        Route = route, QueuedBefore = before, Creation = creation,
                        NativeReturned = returned, ReadOnlyCapture = pure, Actual = actual,
                        RngBeforeCapture = rngBefore, RngAfterCapture = rngAfter
                    }))
                    using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "true-final-boss-birth-calibration.mt2f"))) document.Write(output);
                    if (!returned || !pure || route == null || created.Count != 5)
                        throw new InvalidOperationException("Incomplete or impure true-final-boss birth observation.");
                    LinkedHealthCalibration.Capture(created, () => Snapshot(heroes));
                    LifeLinkCalibration.Capture(created, () => Snapshot(heroes));
                    TrueFinalBossHealthCalibration.Capture(created, () => Snapshot(heroes));
                    TrueFinalBossDeathCalibration.Arm(created);
                    Completed = !TrueFinalBossDamageCalibration.Enabled;
                }
                catch (Exception error) { Error = error.ToString(); }
            }
            if (Error == null && TrueFinalBossDamageCalibration.Enabled)
            {
                IEnumerator paid = TrueFinalBossDamageCalibration.Capture(created);
                while (true)
                {
                    bool moved;
                    try { moved = paid.MoveNext(); }
                    catch (Exception error) { Error = error.ToString(); break; }
                    if (!moved) { Completed = true; break; }
                    yield return paid.Current;
                }
                (paid as IDisposable)?.Dispose();
            }
        }

        [HarmonyPatch(typeof(HeroManager), "DoTrueFinalBossSpawning")]
        private static class BirthPatch
        {
            private static void Postfix(ref IEnumerator __result, HeroManager __instance)
            { if (Enabled && !AllGameManagers.Instance.GetSaveManager().PreviewMode) __result = Observe(__result, __instance); }
        }
        [HarmonyPatch(typeof(HeroManager), "CreateHeroState")]
        private static class CreationPatch
        {
            private static void Prefix(CharacterData heroData, ref Action<CharacterState, CharacterData> afterCharacterCreated)
            {
                if (!Enabled || !observing || AllGameManagers.Instance.GetSaveManager().PreviewMode) return;
                Action<CharacterState, CharacterData> callback = afterCharacterCreated;
                afterCharacterCreated = (character, usedData) =>
                {
                    created.Add(character);
                    creation.Add(new { Id = Id(character), Requested = heroData.GetAssetKey(), Used = usedData.GetAssetKey(),
                        DataId = usedData.GetID(), OriginalAttack = usedData.GetAttackDamage(), OriginalHealth = usedData.GetHealth(),
                        Health = character.GetHP(), MaxHealth = character.GetMaxHP(), Attack = character.GetAttackDamage(),
                        Room = character.GetCurrentRoomIndex(), LinkedId = Id(character.GetLinkedCharacterState()) });
                    callback?.Invoke(character, usedData);
                };
            }
        }
    }
}
