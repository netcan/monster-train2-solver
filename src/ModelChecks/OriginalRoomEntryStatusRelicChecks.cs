using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRoomEntryStatusRelicChecks
{
    private static readonly (string Key, string Id, string Status, int Stacks)[] Artifacts =
    [
        ("RootBeforePyre", "4778bb46-27e4-49ae-aa5d-750813ceb6d0", "rooted", 1),
        ("SpellWeaknessBelowPyre", "5a96d89f-d7c0-49fa-88d1-8ec75a8f3140", "spell weakness", 2),
        ("PyregelOnEnterTopFloor", "e4be072c-e389-4ae7-acc6-0c8aa4cf0078", "pyregel", 10)
    ];

    internal static void Native(FixtureValue root, bool required = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out var scenario) &&
            scenario.GetString() == "original-room-entry-status-relics";
        Require(eligible || !required, "Fixture lacks the original room-entry status relic battle.");
        if (!eligible) return;

        Require(root.GetProperty("Schema").GetInt32() == 132 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("Actions").GetArrayLength() > 0 && root.GetProperty("TrainPhases").GetArrayLength() > 0,
            "Native room-entry relic battle is incomplete or from an unexpected build.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Native room-entry relic battle has nonzero " + key + ".");

        CombatRelicState[] relics = root.GetProperty("Actions")[0].GetProperty("Before").GetProperty("Spawn")
            .GetProperty("Train").GetProperty("Context").GetProperty("Relics").Deserialize<CombatRelicState[]>()!;
        foreach (var artifact in Artifacts)
        {
            CombatRelicState[] owners = relics.Where(relic => relic.DataId == artifact.Id).ToArray();
            Require(owners.Length == 1, "Missing or duplicated original room-entry relic: " + artifact.Key);
            CombatRelicState owner = owners[0];
            RelicRoomEntryStatus? rule = owner.RoomEntryStatuses?.SingleOrDefault();
            Require(owner.AssetKey == artifact.Key && owner.EffectTypes.SequenceEqual(["RelicEffectAddStatusEffectOnEnterRoomIndex"]) &&
                rule?.EffectIndex == 0 && rule.TargetRoomIndex == 2 && !rule.TargetMonsters && rule.TargetHeroes && !rule.FirstWaveOnly &&
                rule.Statuses.Count == 1 && rule.Statuses[0].Id == artifact.Status && rule.Statuses[0].Stacks == artifact.Stacks,
                "Captured original room-entry relic payload changed: " + artifact.Key);
        }

        var witnesses = Artifacts.ToDictionary(item => item.Key, _ => new HashSet<int>());
        foreach (var sample in root.GetProperty("TrainPhases").EnumerateArray())
        {
            TrainCombatState actual = sample.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            foreach (RoomCombatState room in actual.Rooms.Where(room => room.RoomIndex == 2))
            foreach (CombatUnit hero in room.Units.Where(unit => unit.Team == CombatTeam.Enemy))
            foreach (var artifact in Artifacts)
                if (hero.Status(artifact.Status)?.Stacks == artifact.Stacks)
                    witnesses[artifact.Key].Add(hero.Id);
        }

        Require(witnesses.Values.All(units => units.Count > 0), "An original room-entry status has no native Heroes witness: " +
            string.Join(", ", witnesses.Select(item => item.Key + "=" + item.Value.Count)));
        Console.WriteLine("ORIGINAL-ROOM-ENTRY-STATUS-RELIC-WITNESSES " +
            string.Join(", ", witnesses.Select(item => item.Key + "=" + item.Value.Count)));
        Console.WriteLine("NATIVE-ORIGINAL-ROOM-ENTRY-STATUS-RELICS PASS: three unchanged owners, exact room/team/status payloads, actual room-2 Heroes witnesses, and zero battle differences.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
