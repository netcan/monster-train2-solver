using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DamageCompositionCardPreviewChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static UnityRng Stream(FixtureValue capture, string name)
    {
        var words = capture.GetProperty("Streams").EnumerateArray().Single(stream => stream.GetProperty("Id").GetString() == name)
            .GetProperty("Words").EnumerateArray().Select(word => word.GetUInt32()).ToArray();
        Require(words.Length == 4, "Incomplete composition RNG stream.");
        return new UnityRng(words[0], words[1], words[2], words[3]);
    }
    internal static void Native(string path, bool boundaries = false)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == (boundaries ? "NativeDamageCompositionBoundaryCardHover" : "NativeDamageCompositionCardHover") &&
            root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("AuthoredControls").GetBoolean() &&
            root.GetProperty("SelfTargetId").GetInt32() == 0 &&
            root.GetProperty("OriginalSuppression").GetInt32() == root.GetProperty("FinalSuppression").GetInt32(),
            "Incomplete native damage-composition evidence.");
        string teamName = root.GetProperty("TargetTeam").GetString()!;
        Require(teamName is "Player" or "Enemy", "Unknown composition target team.");
        CombatTeam team = teamName == "Enemy" ? CombatTeam.Enemy : CombatTeam.Player;
        int baseHealth = root.GetProperty("BaseHealth").GetInt32();
        var rows = root.GetProperty("Samples").EnumerateArray().ToArray();
        int expectedRows = boundaries ? 192 : 384;
        Require(rows.Length == expectedRows && rows.Select(row => (row.GetProperty("Horde").GetBoolean(), row.GetProperty("Undying").GetBoolean(),
            row.GetProperty("ActiveHalfHealth").GetBoolean(), row.GetProperty("Lethal").GetBoolean(), row.GetProperty("Armor").GetInt32(),
            row.GetProperty("Composition").GetInt32(), row.GetProperty("RangeCase").GetInt32())).Distinct().Count() == expectedRows,
            "Missing damage-composition lifecycle, shape or range controls.");
        int tests = 0, applications = 0, roots = 0, roomCardExtraVictims = 0, retiredMainControls = 0;
        foreach (var row in rows)
        {
            string label = row.GetProperty("Label").GetString()!;
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var rule = row.GetProperty("Rule").Deserialize<CardPlayRule>()!;
            int id = row.GetProperty("TargetId").GetInt32(), mode = row.GetProperty("Composition").GetInt32();
            int rangeCase = row.GetProperty("RangeCase").GetInt32(), amount = row.GetProperty("Amount").GetInt32();
            var target = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            var actual = row.GetProperty("ActualTarget").Deserialize<CombatUnit>()!;
            var actors = row.GetProperty("ActualTargets").Deserialize<CombatUnit[]>()!;
            bool horde = row.GetProperty("Horde").GetBoolean();
            int armor = row.GetProperty("Armor").GetInt32();
            bool roomMain = mode is 5 or 7 or 9 or 12, onCast = mode is 4 or 5 or 8 or 9 or 11 or 12;
            bool cancelledMain = mode is 10 or 11;
            Require((boundaries ? mode is >= 10 and <= 12 : mode is >= 4 and <= 9) && rangeCase is 0 or 1 && target.Team == team && baseHealth >= 2 &&
                (target.Status("horde")?.Stacks ?? 0) == (horde ? 2 : 0) && armor is 0 or 3 && (target.Status("armor")?.Stacks ?? 0) == armor &&
                (target.Status("undying")?.Stacks ?? 0) == (row.GetProperty("Undying").GetBoolean() ? 1 : 0) &&
                target.Triggers.Count(HalfHealthDeathwishModel.IsTrigger) == 2 && target.Triggers.Where(HalfHealthDeathwishModel.IsTrigger)
                    .All(trigger => trigger.DeathwishActive == row.GetProperty("ActiveHalfHealth").GetBoolean()) &&
                amount == (row.GetProperty("Lethal").GetBoolean() ? baseHealth * (horde ? 3 : 1) + 3 : Math.Max(1, baseHealth / 3)) &&
                row.GetProperty("DropTargetId").GetInt32() == id && row.GetProperty("RoomEffect").GetBoolean() == roomMain &&
                row.GetProperty("OnCastDamage").GetBoolean() == onCast && rule.Effects.Count == (mode is >= 6 and <= 11 ? 2 : 1) &&
                rule.OnCastEffects.Count == (onCast ? 1 : 0) && actors.Length >= 3 && actors.Length == row.GetProperty("CopiedIds").GetArrayLength() &&
                row.GetProperty("NativeReturned").GetBoolean() && row.GetProperty("QueueEmpty").GetBoolean() &&
                row.GetProperty("TemporaryDisabled").GetBoolean() && row.GetProperty("EnableCount").GetInt32() == 1 &&
                row.GetProperty("DisableCount").GetInt32() == 1, "Invalid native composition control: " + label);
            var effects = rule.Effects.Concat(rule.OnCastEffects).ToArray();
            for (int index = 0; index < effects.Length; index++)
            {
                bool area = index == 0 ? roomMain : index < rule.Effects.Count ? !roomMain : mode is 4 or 9;
                int value = index == effects.Length - 1 ? amount : 1;
                var effect = effects[index];
                bool bothTeams = area || mode == 12 && index == effects.Length - 1;
                bool failedFirst = cancelledMain && index == 0;
                Require(effect.Type == "Damage" && effect.Target == (area ? "Room" : "DropTargetCharacter") &&
                    effect.AllowEnemy == (bothTeams || team == CombatTeam.Enemy) && effect.AllowPlayer == (bothTeams || team == CombatTeam.Player) &&
                    effect.Value == value && effect.Tests?.CancelSubsequent == failedFirst &&
                    (failedFirst ? effect.Range is { Min: 0, Max: 0, Multiplier: 1f } :
                        rangeCase == 0 ? effect.Range == null : effect.Range is { Min: 0, Multiplier: .75f } range && range.Max == value + 7),
                    "Authored composition effect differs: " + label + "/" + index);
            }
            UnityRng cursor = Stream(row.GetProperty("RngBefore"), "BattleTest");
            Require(cursor.Equals(before.Context!.Enchantments!.TestRng) &&
                Stream(row.GetProperty("RngBefore"), "Battle").Equals(before.Context.BattleRng), "Composition native RNG projection differs: " + label);
            var draws = row.GetProperty("RangeDraws").EnumerateArray().ToArray();
            var executed = cancelledMain ? effects.Take(1).Concat(rule.OnCastEffects) : effects.AsEnumerable();
            var ranged = executed.Where(effect => effect.Range != null).ToArray();
            int effectIndex = -1, rowApplications = 0;
            bool applied = false;
            foreach (var draw in draws)
            {
                bool test = draw.GetProperty("Test").GetBoolean();
                if (test) { effectIndex++; tests++; applied = false; }
                else { Require(!applied, "Duplicate composition application draw: " + label); applied = true; applications++; rowApplications++; }
                Require(effectIndex >= 0 && effectIndex < ranged.Length && draw.GetProperty("Preview").GetBoolean(),
                    "Unexpected composition draw phase: " + label);
                var range = ranged[effectIndex].Range!;
                Require(test || range.Max > 0, "Failed max-zero main effect unexpectedly applied: " + label);
                var sampled = range.Sample(cursor);
                Require(draw.GetProperty("Min").GetInt32() == range.Min && draw.GetProperty("Max").GetInt32() == range.Max &&
                    draw.GetProperty("Multiplier").GetSingle() == range.Multiplier && draw.GetProperty("Value").GetInt32() == sampled.Value &&
                    cursor.Equals(draw.GetProperty("Before").Deserialize<UnityRng>()) && sampled.State.Equals(draw.GetProperty("After").Deserialize<UnityRng>()),
                    "Composition native range draw differs: " + label);
                cursor = sampled.State;
            }
            Require(effectIndex + 1 == ranged.Length && cursor.Equals(Stream(row.GetProperty("RngAfter"), "BattleTest")) &&
                (rangeCase == 0 || mode == 10) == cursor.Equals(before.Context.Enchantments.TestRng), "Incomplete composition range trace: " + label);
            if (mode == 5 && rangeCase == 0 && actors.Any(actor => actor.Id != id && actor.Team == team &&
                before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actor.Id).Health - actor.Health > 1)) roomCardExtraVictims++;
            if (mode == 12 && rangeCase == 0)
            {
                var witnesses = actors.Where(actor => actor.Id != id).ToArray();
                Require(witnesses.Length == 2 && witnesses.All(actor =>
                    before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actor.Id).Health == 10 && actor.Health < 9),
                    "Room-card OnCast did not damage both living extra candidates: " + label);
                roomCardExtraVictims++;
            }
            if (mode == 10) Require(actual.Health == target.Health && (actual.Status("armor")?.Stacks ?? 0) == armor,
                "Cancelled main continuation applied damage: " + label);
            if (mode == 7 && rangeCase == 1 && !horde && !row.GetProperty("Undying").GetBoolean() && armor == 0 &&
                draws[1].GetProperty("Value").GetInt32() >= target.Health && actual.Health == 0 && rowApplications == 2) retiredMainControls++;
            foreach (string stage in new[] { "CopiedActual", "After", "Cleared" })
                Require(row.GetProperty(stage).GetProperty("Context").GetProperty("Enchantments").GetProperty("TestRng").Deserialize<UnityRng>().Equals(cursor) &&
                    row.GetProperty(stage).GetProperty("Context").GetProperty("Statistics").GetProperty("LastAttackDamageDealt").GetInt32() ==
                        before.Context.Statistics!.LastAttackDamageDealt, "Composition cancellation changed RNG or attack statistics: " + label);
            CardHoverFixtureChecks.Match(row, battleTestAdvances: rangeCase == 1 && mode != 10);
            if (++roots % 96 == 0) Console.WriteLine($"DAMAGE-COMPOSITION-CARD-PREVIEW progress: {teamName} {roots}/{expectedRows} roots matched.");
        }
        Require(tests == (boundaries ? 224 : 448) && applications > 0 &&
            (boundaries ? roomCardExtraVictims == 32 : team != CombatTeam.Enemy || retiredMainControls > 0),
            $"Missing composition boundary controls: tests={tests}, applications={applications}, wide={roomCardExtraVictims}, retired={retiredMainControls}.");
        Console.WriteLine($"DAMAGE-COMPOSITION-CARD-PREVIEW PASS: {expectedRows} native {teamName} hovers, " +
            (boundaries ? "cancelled main continuation with preserved OnCast and living room-wide drop candidates; " : "six mixed-target/multiple-main shapes; ") +
            $"{tests} tests and {applications} application draws; full temporary actors, copy/effect/cancel states and 16 immutable branches each.");
    }
}
