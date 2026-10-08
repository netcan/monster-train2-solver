using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AdditionalSummonChecks
{
    internal static void Native(FixtureValue fixture)
    {
        bool fresh = fixture.GetProperty("ModifierScenario").GetString() == "multi-summon-additional-fresh";
        var births = fixture.GetProperty("UnitBirths").EnumerateArray().ToArray();
        var clones = fixture.GetProperty("DetachedCardClones").EnumerateArray().ToArray();
        var upgrades = fixture.GetProperty("SpawnUpgrades").EnumerateArray().ToArray();
        var phases = fixture.GetProperty("RallyPhases").EnumerateArray().ToArray();
        var triggers = fixture.GetProperty("RallyTriggers").EnumerateArray().ToArray();
        bool oddSplit = false, bothKinds = false;
        foreach (var sample in fixture.GetProperty("Actions").EnumerateArray())
        {
            var before = sample.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = sample.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string dataId = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId;
            var rule = before.PlayRules!.Cards.Single(item => item.DataId == dataId);
            if (rule.Summon?.Additional == null) continue;
            var after = sample.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var oldIds = before.Spawn.Train.Rooms[action.RoomIndex].Units.Select(unit => unit.Id).ToHashSet();
            var born = after.Spawn.Train.Rooms[action.RoomIndex].Units.Where(unit => !oldIds.Contains(unit.Id))
                .OrderBy(unit => unit.Id).ToArray();
            int slots = before.PlayRules.Rooms[action.RoomIndex].PlayerSlots;
            int oldCount = before.Spawn.Train.Rooms[action.RoomIndex].Units.Count(unit => unit.Team == CombatTeam.Player);
            int capped = Math.Min(Math.Max(rule.Summon.Count, 1) * 2, slots - oldCount);
            int position = action.PlayerPosition < 0 ? oldCount : action.PlayerPosition;
            Require(born.Length == Math.Min(capped, slots - position), "Native mixed summon count or insertion limit differs.");
            for (int index = 0; index < born.Length; index++)
            {
                bool additional = index >= capped / 2;
                Require(born[index].AssetKey == (additional ? rule.Summon.Additional.Unit.AssetKey : rule.SpawnUnit!.AssetKey),
                    "Additional selection did not use the capped total's halfway point.");
                Require(born[index].Modifiers!.SpawnerMatchesDefinition == (fresh || !additional), "Source-character identity differs.");
                Require((born[index].StatusImmunities.Contains("endless")) == (index > 0), "Cardless marker depends on selected kind.");
                if (fresh)
                {
                    string selectedId = additional ? rule.Summon.Additional.FallbackCreation!.DataId : rule.Summon.FallbackCreation!.DataId;
                    Require(after.Spawn.Train.Context!.FindCard(born[index].SpawnerCardId)!.DataId == selectedId,
                        "Fresh fallback source did not match the selected character.");
                }
                else Require(after.Spawn.Train.Context!.FindCard(born[index].SpawnerCardId)!.DataId == dataId,
                    "A copied source changed definition when the selected character changed.");
            }
            bothKinds |= born.Any(unit => unit.AssetKey == rule.SpawnUnit!.AssetKey) &&
                born.Any(unit => unit.AssetKey == rule.Summon.Additional.Unit.AssetKey);
            oddSplit |= capped % 2 == 1 && born.Count(unit => unit.AssetKey == rule.Summon.Additional.Unit.AssetKey) >
                born.Count(unit => unit.AssetKey == rule.SpawnUnit!.AssetKey);
        }
        Require(bothKinds && oddSplit && upgrades.Length >= 7, "Two kinds, odd capped split and extra upgrades were not exercised.");
        if (fresh) FreshSummonChecks.Native(fixture);
        else
        {
            Require(clones.Length >= 5 && upgrades.Any(sample =>
            {
                var state = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
                return !state.Units.Single(unit => unit.Id == sample.GetProperty("UnitId").GetInt32()).Modifiers!.SpawnerMatchesDefinition &&
                    sample.GetProperty("SourceAdded").GetBoolean();
            }), "Mismatched source still receiving the explicit extra upgrade was not exercised.");
            Require(triggers.Any(sample =>
            {
                var actor = sample.GetProperty("Actor").Deserialize<CombatUnit>()!;
                var changed = sample.GetProperty("AfterActor").Deserialize<CombatUnit>()!;
                if (actor.Modifiers?.SpawnerMatchesDefinition != false || changed.BaseAttack <= actor.BaseAttack) return false;
                var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!.Context!;
                var after = sample.GetProperty("After").Deserialize<RoomCombatState>()!.Context!;
                return Serialize(before.FindCard(actor.SpawnerCardId)!.Temporary) == Serialize(after.FindCard(actor.SpawnerCardId)!.Temporary);
            }), "Mismatched self growth incorrectly wrote back to its source, or the case was not recorded.");
            Verify();
            Parallel.For(0, 32, _ => Verify());
        }
        void Verify()
        {
            foreach (var sample in births) UnitSummonChecks.VerifyBirth(sample);
            foreach (var sample in clones) UnitSummonChecks.VerifyClone(sample);
            foreach (var sample in upgrades) UnitSummonChecks.VerifyExtra(sample);
            foreach (var sample in phases) RallyChecks.VerifyPhase(sample);
            foreach (var sample in triggers) RallyChecks.VerifyTrigger(sample);
        }
        Console.WriteLine($"NATIVE-ADDITIONAL-SUMMON-CHECKS PASS: {births.Length} complete births, two kinds, odd capped split, source matching, fresh={fresh}, extra upgrade writes and 32 branches.");
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}
