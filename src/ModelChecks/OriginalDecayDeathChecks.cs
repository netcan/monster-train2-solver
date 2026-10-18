using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDecayDeathChecks
{
    private static CardActionEffect Action() => new("TransferAllStatusesFromUnit", "Self", 11, true, true,
        [new("decay", 0, stackable: true, hidden: false, displayCategory: "Negative")],
        statusTransfer: new("EvenlyDistributedAcrossFriendlyTeam", "DebuffsOnly", "SourceRoomOnly", false, true, 0));
    private static CombatRelicState Owner() => new(RelicDecayDeathModel.DataId, RelicDecayDeathModel.AssetKey,
        [RelicDecayDeathModel.Effect], isCovenant: false, disallowedInPlacementPhase: false,
        resourceEffects: [new(0, RelicDecayDeathModel.Effect, 8, 0, 0, true,
            deathEffect: new("255", "SubtypesData_None", Action()))]);
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(31);
        CombatContext context = new(new([], [], [], rng, 0, []), rng, 0, 1, 10, relics: [Owner()]);
        Require(RelicModel.Validate(context.Relics) == null, "Original Decay-death definition rejected.");
        foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
        {
            CombatUnit Unit(int id, CombatTeam unitTeam, int hp, params CombatStatus[] statuses) =>
                new(id, "carrier", unitTeam, 0, hp, 100, false, false, false, statuses, statusRegistry: statuses);
            CombatUnit source = Unit(1, team, 0, new("decay", 5), new("regen", 2), new("spell weakness", 4));
            CombatUnit a = Unit(2, team, 100), b = Unit(3, team, 100, new CombatStatus("decay", 2));
            CombatUnit dead = Unit(4, team, 0), untouchable = Unit(5, team, 100, new CombatStatus("untouchable", 1));
            CombatUnit opposite = Unit(6, team == CombatTeam.Player ? CombatTeam.Enemy : CombatTeam.Player, 100);
            RoomCombatState room = new(0, false, [source, a, b, dead, untouchable, opposite], [], context);
            string parent = JsonSerializer.Serialize(room, ModelJson.Options);
            void Replay()
            {
                RoomCombatResult result = RelicDecayDeathModel.Apply(room, source);
                Require(result.Supported, result.UnsupportedReason ?? "Transfer unsupported.");
                CombatUnit Find(int id) => result.State!.Units.Single(unit => unit.Id == id);
                Require(Find(1).Status("decay")?.Stacks == 5 && Find(2).Status("decay")?.Stacks == 3 &&
                    Find(3).Status("decay")?.Stacks == 5 && Find(4).Status("decay") == null &&
                    Find(5).Status("decay") == null && Find(6).Status("decay") == null &&
                    Find(2).Status("regen") == null && Find(3).Status("spell weakness") == null &&
                    result.State!.Context!.BattleRng.Equals(rng), "Wrong ceil distribution, source retention, status/team/validity filter or RNG.");
                Require(result.PendingCallbacks.Where(row => row.Kind == "OnStatusEffectChanged").Select(row => row.Unit.Id)
                    .SequenceEqual([3, 2]), "Transfer did not visit recipients in reverse physical order.");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(room, ModelJson.Options) == parent, "Transfer mutated parent.");
            RoomCombatState noRecipients = new(0, false, [source, opposite, untouchable], [], context);
            Equal(RelicDecayDeathModel.Apply(noRecipients, source).State, noRecipients, "empty eligible team");
            RoomCombatState sourceRetainedOnly = new(0, false, [a, b], [], context);
            RoomCombatResult retained = RelicDecayDeathModel.Apply(sourceRetainedOnly, source);
            Require(retained.Supported && retained.State!.Units.Count == 2 && retained.RetainedUnits.Single().Id == source.Id,
                "Dead callback source was reintroduced into the live room.");
            Require(RelicDecayDeathModel.Test(source, "OnHit", 8) && !RelicDecayDeathModel.Test(a, "OnHit", 8) &&
                !RelicDecayDeathModel.Test(source, "OnDeath", 8) && !RelicDecayDeathModel.Test(source, "OnHit", 2) &&
                !RelicDecayDeathModel.Test(null, "OnHit", 8), "Wrong dead/trigger/damage/null gates.");
            Require(!RelicDecayDeathModel.Apply(new(0, false, room.Units, [], context.WithRelics([])), source).Supported,
                "Missing owner accepted.");
        }
        Require(RelicModel.Validate([new(RelicDecayDeathModel.DataId, RelicDecayDeathModel.AssetKey,
            [RelicDecayDeathModel.Effect])]) != null, "Missing exact native payload accepted.");
        Console.WriteLine("DECAY-DEATH-PURE PASS: both teams, ceil distribution, source retention, reverse callbacks, status/target/death gates, no recipients, retained dead actor, absent owner and 16 immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 242 && root.GetProperty("OriginalDecayDeathPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete Decay-death witness.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalDecayDeathCaptureFailures",
            "OriginalDecayDeathMismatches", "OriginalDecayDeathUnsupported", "OriginalDecayDeathPending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Nonzero " + field);
        var tests = root.GetProperty("RelicDecayDeathTests").EnumerateArray().ToArray();
        int eligible = 0, ineligible = 0; bool nullActor = false, wrongTrigger = false, wrongDamage = false, living = false;
        foreach (FixtureValue row in tests)
        {
            CombatUnit? actor = row.GetProperty("Actor").Deserialize<CombatUnit>();
            string trigger = row.GetProperty("Trigger").GetString()!; int damage = row.GetProperty("DamageType").GetInt32();
            bool result = RelicDecayDeathModel.Test(actor, trigger, damage);
            Require(result == row.GetProperty("Returned").GetBoolean(), "Native Test result differs.");
            Equal(row.GetProperty("Before").Deserialize<TrainCombatState>(), row.GetProperty("Actual").Deserialize<TrainCombatState>(), "Test side effects");
            if (result) eligible++; else ineligible++;
            nullActor |= actor == null; wrongTrigger |= actor?.Health <= 0 && trigger != "OnHit";
            wrongDamage |= actor?.Health <= 0 && trigger == "OnHit" && damage != 8; living |= actor?.Health > 0 && trigger == "OnHit";
        }
        Require(eligible >= 2 && ineligible > 0 && nullActor && wrongTrigger && wrongDamage && living, "Missing native Test controls.");
        var applications = root.GetProperty("RelicDecayDeathApplications").EnumerateArray().ToArray();
        var teams = new HashSet<CombatTeam>(); bool rounded = false, untouchable = false, unrelatedStatus = false, noRecipients = false;
        foreach (FixtureValue row in applications)
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!, afterActor = row.GetProperty("ActorAfter").Deserialize<CombatUnit>()!;
            int floor = row.GetProperty("RoomIndex").GetInt32(); RoomCombatState room = before.Rooms.Single(item => item.RoomIndex == floor);
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                RoomCombatResult result = RelicDecayDeathModel.Apply(room, actor); Require(result.Supported, result.UnsupportedReason ?? "Native transfer unsupported.");
                TrainCombatState predicted = CardSpellModel.WithContext(new(before.Rooms.Select(item => item.RoomIndex == floor ? result.State! : item).ToArray(),
                    before.Movement, before.EnemySlotsPerRoom, result.State!.Context), result.State.Context!);
                Equal(predicted, actual, "complete native transfer");
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native replay mutated parent.");
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Notifications").Deserialize<string[]>()!.SequenceEqual(["Triggered"]), "Missing completion or exactly one native notification.");
            Require(actor.Health <= 0 && (actor.Status("decay")?.Stacks ?? 0) == (afterActor.Status("decay")?.Stacks ?? 0), "Transfer removed source stacks.");
            CombatUnit[] targets = room.Units.Where(unit => unit.Team == actor.Team && unit.Health > 0 && unit.Status("untouchable")?.Stacks is not > 0 &&
                unit.DeathState?.IsDestroyed != true && unit.DeathState?.IsDespawned != true).ToArray();
            int stacks = actor.Status("decay")?.Stacks ?? 0;
            if (stacks > 0 && targets.Length > 0) teams.Add(actor.Team);
            rounded |= stacks == 5 && targets.Length == 2 && targets.All(unit =>
                actual.Rooms.Single(item => item.RoomIndex == floor).Units.Single(other => other.Id == unit.Id).Status("decay")?.Stacks == (unit.Status("decay")?.Stacks ?? 0) + 3);
            untouchable |= room.Units.Any(unit => unit.Team == actor.Team && unit.Health > 0 && unit.Status("untouchable")?.Stacks > 0);
            unrelatedStatus |= actor.Status("spell weakness")?.Stacks == 4 && actor.Status("regen")?.Stacks > 0;
            noRecipients |= stacks > 0 && targets.Length == 0;
        }
        Require(teams.Count == 2 && rounded && untouchable && unrelatedStatus && noRecipients,
            $"Missing native evidence: teams={teams.Count}, ceil={rounded}, untouchable={untouchable}, unrelated-status={unrelatedStatus}, no-recipients={noRecipients}.");
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 6 && actions.All(row => !row.GetProperty("ScenarioAction").GetBoolean()), "Expected six ordinary independently selected plays.");
        Console.WriteLine($"ORIGINAL-DECAY-DEATH-NATIVE PASS: {tests.Length} exact Test calls, {applications.Length} full transfers, both teams, ceil(5/2)=3, source retention, unrelated status/untouchable/other-room controls, no-recipient witness={noRecipients} and 16 immutable branches.");
    }
    private static void Equal<T>(T a, T b, string label)
    { string x = JsonSerializer.Serialize(a, ModelJson.Options), y = JsonSerializer.Serialize(b, ModelJson.Options); Require(x == y, label + ": " + ModelJson.Difference(x, y)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
