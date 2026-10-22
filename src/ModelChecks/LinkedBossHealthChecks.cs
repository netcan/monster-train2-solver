using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LinkedBossHealthChecks
{
    internal static void Bridge(FixtureValue root)
    {
        void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
        string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
        int[] rooms = root.GetProperty("WorldBefore").GetProperty("Characters").EnumerateArray().Select(actor => actor.GetProperty("Room").GetInt32()).ToArray();
        TrainCombatState Make(TrueFinalBossHealthState before)
        {
            var context = new CombatContext(new CardCycleState([], [], [], new UnityRng(1, 2, 3, 4), 0, []),
                new UnityRng(5, 6, 7, 8), 73, 91, 10, lifeLinks: before.LifeLink);
            var units = before.LifeLink.Actors.Select((actor, index) => new CombatUnit(actor.Id, TrueFinalBossChecks.Assets[index], CombatTeam.Enemy,
                0, actor.Health, before.Health.Actors[index].RawMaxHealth, true, false, actor.OuterBoss, [])).ToArray();
            return new TrainCombatState(Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
                units.Where((unit, actor) => rooms[actor] == index).ToArray(), [], context)).ToArray(), [], 7, context);
        }
        FixtureValue[] rows = root.GetProperty("Rows").EnumerateArray().ToArray();
        TrainCombatState? chain = null;
        int passed = 0, previews = 0;
        foreach (FixtureValue row in rows)
        {
            TrueFinalBossHealthState before = row.GetProperty("Before").Deserialize<TrueFinalBossHealthState>()!;
            TrueFinalBossHealthResult actual = row.GetProperty("Actual").Deserialize<TrueFinalBossHealthResult>()!;
            int target = row.GetProperty("TargetId").GetInt32(), value = row.GetProperty("Value").GetInt32(), mode = row.GetProperty("Mode").GetInt32();
            string operation = row.GetProperty("Operation").GetString()!, label = row.GetProperty("Label").GetString()!;
            bool chained = label != "matrix";
            TrainCombatState source = chained && chain != null && mode == 0 ? chain : Make(before);
            if (mode == 0 && (chain == null || !chained))
            {
                // Native non-preview reads use live HP even when stale UI preview
                // copies exist. The physical train projection canonicalizes them.
                LifeLinkContext live = new(before.LifeLink.Actors.Select(actor => new LifeLinkActor(actor.Id, actor.LinkedId,
                    actor.Health, actor.Health, actor.Initialized, actor.HasLifeLink, actor.TrueFinalBoss, actor.OuterBoss)).ToArray(), false, 0);
                var context = new CombatContext(source.Context!.Cards, source.Context.BattleRng, 73, 91, 10, lifeLinks: live);
                source = new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, false, room.Units, [], context)).ToArray(), [], 7, context);
            }
            string immutable = Json(source);
            LinkedBossHealthResult result = LinkedBossHealthModel.Apply(source, target, operation, value);
            if (mode == 1) { Require(!result.Supported, "UI calculation state was admitted to live train updates."); previews++; continue; }
            Require(result.Supported && Json(source) == immutable && Json(result.Events) == Json(actual.Events) &&
                Json(result.DamageStage!) == Json(actual.DamageStage!), "Native linked train update differs or parent mutated: " + label + ": " + result.UnsupportedReason);
            foreach (LifeLinkActor actor in actual.State!.LifeLink.Actors)
            {
                CombatUnit physical = result.State!.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actor.Id);
                Require(physical.Health == actor.Health && result.State.Rooms[rooms[actor.Id - 1]].Units.Any(unit => unit.Id == actor.Id) &&
                    result.State.Context!.LifeLinks!.Actors.Single(unit => unit.Id == actor.Id).Health == actor.Health,
                    "Native linked health did not propagate to every original physical room and shared context.");
            }
            Require(result.State!.Context!.Gold == 73 && result.State.Context.NextCardId == 91 &&
                result.State.Rooms.All(room => ReferenceEquals(room.Context, result.State.Context)) && result.State.Context.LifeLinks!.SharedPreviewHpChange == 0,
                "Linked train update lost unrelated shared state or leaked UI preview damage.");
            if (chained) chain = result.State;
            passed++;
        }
        Require(passed == 100 && previews == 100, "Native train bridge controls or explicit preview rejections are incomplete.");
        Console.WriteLine("LINKED-BOSS-TRAIN PASS: 100 native live health updates including dependent cross-room chain; single-target signals, room membership, canonical shared context and immutable parents; 100 UI-calculation contexts rejected. Controller/full combat remains guarded.");
    }

    internal static void Capture(FixtureValue root)
    {
        void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
        Require(root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Mismatches").GetInt32() == 0 &&
            root.GetProperty("Pending").GetInt32() == 0 && root.GetProperty("Unsupported").GetInt32() > 0 &&
            !root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("OriginalTrueFinalBossRoute").GetProperty("Kind").GetString() == "NativeCheatJumpToTFB" &&
            root.GetProperty("OriginalTrueFinalBossRoute").GetProperty("StartingDeckUnchanged").GetBoolean() &&
            root.GetProperty("OriginalTrueFinalBossRoute").GetProperty("PyreHealth").GetInt32() == 80,
            "Live-world observation omitted native route controls, failed capture or wrongly claimed full model acceptance.");
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 6, "Incomplete original native no-cards diagnostic.");
        int captures = 0;
        foreach (FixtureValue turn in turns)
        foreach (string boundary in new[] { "Before", "Actual" })
        {
            BattleTurnState state = turn.GetProperty(boundary).Deserialize<BattleTurnState>()!;
            TrainCombatState train = state.Spawn.Train;
            CombatUnit[] actors = train.Rooms.SelectMany(room => room.Units).Where(unit => TrueFinalBossChecks.Assets.Contains(unit.AssetKey)).ToArray();
            LifeLinkContext? world = train.Context?.LifeLinks;
            if (actors.Length == 0) { Require(world == null, "Global life-link roster appeared before original birth."); continue; }
            Require(actors.Length == 5 && world != null && LifeLinkModel.Validate(world) == null && world.Actors.Count == 5 &&
                !world.CalculatingPreview && world.SharedPreviewHpChange == 0 && world.Actors.All(actor => actor.Health == actor.PreviewHealth),
                "Missing canonical live roster or captured UI hover/calculation state.");
            int owner = actors.Single(actor => actor.AssetKey == TrueFinalBossChecks.Assets[2]).Id;
            foreach (LifeLinkActor actor in world!.Actors)
            {
                CombatUnit physical = actors.Single(unit => unit.Id == actor.Id);
                int index = Array.IndexOf(TrueFinalBossChecks.Assets, physical.AssetKey);
                Require(actor.Health == physical.Health && actor.Initialized && actor.TrueFinalBoss && actor.HasLifeLink &&
                    actor.LinkedId == (index >= 3 ? owner : 0) && actor.OuterBoss == (index >= 2),
                    "Native shared roster differs from physical character state, role or original owner.");
            }
            captures++;
        }
        Require(captures == 11 && TrueFinalBossChecks.Assets.All(asset => !OuterBossDefinitions.Supports(asset)),
            "Missing post-birth native boundaries or weakened full-battle guard.");
        Console.WriteLine("LINKED-BOSS-LIVE-CAPTURE PASS: original no-cards terminal loss, 11 post-birth decision boundaries, global owners/HP/roles match all physical rooms and omit UI preview state. Observation only; unsupported guards remain active.");
    }
}
