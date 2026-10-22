using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class PyreBaneCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_PYREBANE_TARGETS") == "1";
        internal static void Capture(IReadOnlyList<CharacterState> actors, Func<object> snapshot)
        {
            if (!Enabled) return;
            CharacterState savagery = actors[1]; CharacterState bottom = actors[2];
            FullBattleTrace trace = FullBattleTrace.Active!;
            if (savagery.GetSourceCharacterData().GetAssetKey() != "Boss_Level9_Savagery_V2" ||
                savagery.GetHP() != 2750 || savagery.GetCurrentRoomIndex() != 0 || bottom.GetHP() != 3500)
                throw new InvalidOperationException("Pyrebane queries require the original live Savagery and Entropy bottom room.");
            object Rng() => new { Streams = Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
                new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray(),
                Unity = RngCalibration.Words(UnityEngine.Random.state) };
            var primary = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation");
            object original = primary.GetValue(savagery);
            Type stateType = original.GetType();
            var hp = AccessTools.Property(stateType, "Hp");
            var statuses = AccessTools.Field(stateType, "statusEffects");
            MethodInfo clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var nativeRng = (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { RngId.Battle });
            var savedRng = new HadesRNG(nativeRng);
            object before = snapshot(), beforeRng = Rng(); int frame = UnityEngine.Time.frameCount;
            var rows = new List<object>();
            try
            {
                foreach (int health in new[] { 1, 2, 2750 })
                foreach (bool lifeLink in new[] { false, true })
                foreach (bool pyreBane in new[] { false, true })
                foreach (bool inCombat in new[] { false, true })
                foreach (int seed in new[] { 1, 2, 73, int.MaxValue })
                foreach (TargetMode mode in new[] { TargetMode.Room, TargetMode.FrontInRoom, TargetMode.BackInRoom,
                    TargetMode.Weakest, TargetMode.RandomInRoom })
                {
                    object shadow = clone.Invoke(original, null);
                    var map = new Dictionary<string, CharacterState.StatusEffectStack>(
                        (Dictionary<string, CharacterState.StatusEffectStack>)statuses.GetValue(original));
                    if (!lifeLink) map.Remove("lifelink");
                    if (!pyreBane) map.Remove("pyrebane");
                    statuses.SetValue(shadow, map); hp.SetValue(shadow, health); primary.SetValue(savagery, shadow);
                    nativeRng.Init(seed);
                    uint[] rngBefore = RngCalibration.Words(nativeRng.GetState());
                    var input = new TargetHelper.CollectTargetsData(); input.Reset(mode);
                    input.targetTeamType = Team.Type.Heroes; input.roomIndex = 0; input.ignorePyre = true;
                    input.inCombat = inCombat;
                    var targets = new List<CharacterState>();
                    TargetHelper.CollectTargets(input, AllGameManagers.Instance.GetCoreManagers(), ref targets);
                    bool targetable = !pyreBane || map["pyrebane"].State.GetUnitIsTargetable(inCombat);
                    rows.Add(new { Health = health, LifeLink = lifeLink, PyreBane = pyreBane, InCombat = inCombat,
                        Seed = seed, Mode = mode.ToString(), Targetable = targetable, Targets = targets.Select(trace.UnitId).ToArray(),
                        RngBefore = rngBefore, RngAfter = RngCalibration.Words(nativeRng.GetState()) });
                    primary.SetValue(savagery, original); nativeRng.Init(savedRng);
                }
            }
            finally { primary.SetValue(savagery, original); nativeRng.Init(savedRng); }
            bool unchanged = frame == UnityEngine.Time.frameCount && JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(snapshot())) &&
                JToken.DeepEquals(JToken.FromObject(beforeRng), JToken.FromObject(Rng()));
            if (!unchanged) throw new InvalidOperationException("Pyrebane target calibration changed live actors, frame or RNG.");
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "OriginalSavageryPyreBaneTargetQueries", Route = TrueFinalBossBirthProbe.Control,
                DetachedHealthAndStatusControls = true, OriginalUnitsAndRooms = true, LiveWorldAndFrameAndRngUnchanged = unchanged,
                SavageryId = trace.UnitId(savagery), BottomId = trace.UnitId(bottom), NativeCompleted = true, Rows = rows
            }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "true-final-boss-pyrebane-targets-calibration.mt2f"))) document.Write(output);
        }
    }
}
