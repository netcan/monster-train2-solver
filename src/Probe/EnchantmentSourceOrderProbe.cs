using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class EnchantmentSourceOrderProbe
    {
        private sealed class Sample
        {
            public string Label { get; set; } = "";
            public int RoomCount { get; set; }
            public EnchantmentSourcePosition[] Positions { get; set; } = Array.Empty<EnchantmentSourcePosition>();
            public EnchantmentSourcePosition[] Initial { get; set; } = Array.Empty<EnchantmentSourcePosition>();
            public int[] Actual { get; set; } = Array.Empty<int>();
        }
        private static readonly List<Sample> samples = new List<Sample>();
        private static bool stressed;
        private static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ENCHANTMENT_WORLD") == "1";
        private static EnchantmentSourcePosition Position(CharacterState actor) => new EnchantmentSourcePosition(actor.GetInstanceID(),
            actor.GetTeamType() == Team.Type.Monsters ? CombatTeam.Player : CombatTeam.Enemy,
            actor.GetCurrentRoomIndex(), actor.GetSpawnPoint().GetIndexInRoom());
        private static void Record()
        {
            var managers = AllGameManagers.Instance!;
            var rooms = managers.GetRoomManager()!;
            var live = new List<CharacterState>();
            for (int room = 0; room < rooms.GetNumRooms(); room++)
                rooms.GetRoom(room).AddCharactersToList(live, Team.Type.Heroes | Team.Type.Monsters);
            var positions = live.Select(Position).ToArray();
            Capture("ordinary-" + samples.Count, Array.Empty<CharacterState>());
            if (!stressed && positions.Any(item => item.Team == CombatTeam.Player) && positions.Any(item => item.Team == CombatTeam.Enemy))
            {
                stressed = true;
                // Read-only native manager calls on local lists exercise the Mono sort
                // with many equal physical indices and both sides of the size threshold.
                foreach (int count in new[] { 0, 1, 15, 16, 17, 31, 33, 64 })
                    for (int offset = 0; offset < 4; offset++)
                    {
                        var seed = Enumerable.Range(0, count).Select(index => live[(index * (offset + 1) + offset) % live.Count]).ToArray();
                        Capture("sort-stress-" + count + "-" + offset, seed);
                    }
            }
            void Capture(string label, CharacterState[] initial)
            {
                var actors = initial.ToList();
                managers.GetMonsterManager()!.AddCharactersInTowerToList(actors);
                managers.GetHeroManager()!.AddCharactersInTowerToList(actors);
                samples.Add(new Sample { Label = label, RoomCount = rooms.GetNumRooms(), Positions = positions,
                    Initial = initial.Select(Position).ToArray(), Actual = actors.Select(actor => actor.GetInstanceID()).ToArray() });
            }
        }
        internal static void Write()
        {
            if (!Enabled) return;
            if (!stressed) throw new InvalidOperationException("Both-team native enchantment source order was not observed.");
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "enchantment-source-order-calibration.mt2f");
            using (var archive = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "NativeManagerListCollection", GameplaySuppressed = false, Samples = samples }))
            using (var stream = File.Create(path)) archive.Write(stream);
        }
        [HarmonyPatch(typeof(RoomManager), nameof(RoomManager.UpdateEnchantments))]
        private static class UpdatePatch
        {
            private static void Prefix()
            {
                if (Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) Record();
            }
        }
    }
}
