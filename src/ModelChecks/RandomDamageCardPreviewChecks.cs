using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RandomDamageCardPreviewChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static UnityRng Stream(FixtureValue capture, string name)
    {
        var words = capture.GetProperty("Streams").EnumerateArray().Single(stream => stream.GetProperty("Id").GetString() == name)
            .GetProperty("Words").EnumerateArray().Select(word => word.GetUInt32()).ToArray();
        Require(words.Length == 4, "Incomplete native RNG words.");
        return new UnityRng(words[0], words[1], words[2], words[3]);
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeRandomDamageCardHover" &&
            root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("AuthoredControls").GetBoolean() &&
            root.GetProperty("SelfTargetId").GetInt32() == 0 &&
            root.GetProperty("OriginalSuppression").GetInt32() == root.GetProperty("FinalSuppression").GetInt32(),
            "Incomplete native random-damage hover evidence.");
        string teamName = root.GetProperty("TargetTeam").GetString()!;
        Require(teamName is "Player" or "Enemy", "Unknown random-damage target team.");
        CombatTeam team = teamName == "Enemy" ? CombatTeam.Enemy : CombatTeam.Player;
        int baseHealth = root.GetProperty("BaseHealth").GetInt32();
        var rows = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(rows.Length == 512 && rows.Select(row => (row.GetProperty("Horde").GetBoolean(), row.GetProperty("Undying").GetBoolean(),
            row.GetProperty("ActiveHalfHealth").GetBoolean(), row.GetProperty("Lethal").GetBoolean(), row.GetProperty("Armor").GetInt32(),
            row.GetProperty("RoomEffect").GetBoolean(), row.GetProperty("OnCastDamage").GetBoolean(), row.GetProperty("RangeCase").GetInt32()))
            .Distinct().Count() == 512, "Missing random-damage lifecycle, targeting or range controls.");
        int tests = 0, applications = 0, changed = 0, negative = 0, zero = 0, checkedRoots = 0;
        foreach (var row in rows)
        {
            string label = row.GetProperty("Label").GetString()!;
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var rule = row.GetProperty("Rule").Deserialize<CardPlayRule>()!;
            var target = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == row.GetProperty("TargetId").GetInt32());
            var actual = row.GetProperty("ActualTarget").Deserialize<CombatUnit>()!;
            bool room = row.GetProperty("RoomEffect").GetBoolean(), onCast = row.GetProperty("OnCastDamage").GetBoolean();
            int rangeCase = row.GetProperty("RangeCase").GetInt32(), amount = row.GetProperty("Amount").GetInt32();
            bool horde = row.GetProperty("Horde").GetBoolean(), lethal = row.GetProperty("Lethal").GetBoolean();
            int armor = row.GetProperty("Armor").GetInt32();
            Require(baseHealth >= 2 && target.Team == team && armor is 0 or 3 && (target.Status("armor")?.Stacks ?? 0) == armor &&
                (target.Status("horde")?.Stacks ?? 0) == (horde ? 2 : 0) &&
                (target.Status("undying")?.Stacks ?? 0) == (row.GetProperty("Undying").GetBoolean() ? 1 : 0) &&
                target.Triggers.Count(HalfHealthDeathwishModel.IsTrigger) == 2 && target.Triggers.Where(HalfHealthDeathwishModel.IsTrigger)
                    .All(trigger => trigger.DeathwishActive == row.GetProperty("ActiveHalfHealth").GetBoolean()) &&
                amount == (lethal ? baseHealth * (horde ? 3 : 1) + 3 : Math.Max(1, baseHealth / 3)) &&
                rule.Effects.Count == 1 && rule.OnCastEffects.Count == (onCast ? 1 : 0) && rangeCase is >= 1 and <= 4 &&
                row.GetProperty("DropTargetId").GetInt32() == (room ? 0 : target.Id) &&
                row.GetProperty("ActualTargets").GetArrayLength() == row.GetProperty("CopiedIds").GetArrayLength() &&
                row.GetProperty("ActualTargets").GetArrayLength() >= 3 &&
                row.GetProperty("NativeReturned").GetBoolean() && row.GetProperty("QueueEmpty").GetBoolean() &&
                row.GetProperty("TemporaryDisabled").GetBoolean() && row.GetProperty("EnableCount").GetInt32() == 1 &&
                row.GetProperty("DisableCount").GetInt32() == 1, "Invalid native random-damage control: " + label);
            var effects = rule.Effects.Concat(rule.OnCastEffects).ToArray();
            for (int index = 0; index < effects.Length; index++)
            {
                var effect = effects[index];
                int value = onCast ? index == 0 ? 1 : amount - 1 : amount;
                Require(effect.Type == "Damage" && effect.Target == (room ? "Room" : "DropTargetCharacter") &&
                    (!room || effect.AllowEnemy && effect.AllowPlayer) && effect.Range is CardEffectRange range &&
                    range.Min == (rangeCase == 1 ? 0 : rangeCase == 2 ? value : -value) &&
                    range.Max == (rangeCase == 1 ? value + 7 : rangeCase == 2 ? value : rangeCase == 3 ? 2 : 0) &&
                    range.Multiplier == (rangeCase == 1 ? .75f : rangeCase == 2 ? 1f : rangeCase == 3 ? -.5f : .5f),
                    "Random-damage authored range differs: " + label);
            }
            UnityRng cursor = Stream(row.GetProperty("RngBefore"), "BattleTest");
            Require(cursor.Equals(before.Context!.Enchantments!.TestRng) &&
                Stream(row.GetProperty("RngBefore"), "Battle").Equals(before.Context.BattleRng), "Native RNG projection differs: " + label);
            var draws = row.GetProperty("RangeDraws").EnumerateArray().ToArray();
            Require(draws.Length >= 1 && draws[0].GetProperty("Test").GetBoolean(), "Missing native range test: " + label);
            foreach (var draw in draws)
            {
                var range = new CardEffectRange(draw.GetProperty("Min").GetInt32(), draw.GetProperty("Max").GetInt32(),
                    draw.GetProperty("Multiplier").GetSingle());
                var sampled = range.Sample(cursor);
                Require(draw.GetProperty("Preview").GetBoolean() && draw.GetProperty("Before").Deserialize<UnityRng>().Equals(cursor) &&
                    sampled.Value == draw.GetProperty("Value").GetInt32() && sampled.State.Equals(draw.GetProperty("After").Deserialize<UnityRng>()) &&
                    effects.Any(effect => Math.Max(0, effect.Range!.Min) == range.Min && Math.Max(0, effect.Range.Max) == range.Max && effect.Range.Multiplier == range.Multiplier),
                    "Native random-damage draw differs: " + label);
                bool test = draw.GetProperty("Test").GetBoolean();
                if (test) tests++; else applications++;
                if (sampled.Value < 0) negative++;
                if (sampled.Value == 0) zero++;
                Require(rangeCase != 4 || test, "A max-zero damage effect unexpectedly applied: " + label);
                cursor = sampled.State;
            }
            Require(cursor.Equals(Stream(row.GetProperty("RngAfter"), "BattleTest")), "Native range trace missed a draw: " + label);
            if (!cursor.Equals(before.Context.Enchantments.TestRng)) changed++;
            if (rangeCase == 2) Require(cursor.Equals(before.Context.Enchantments.TestRng), "Equal endpoints consumed RNG: " + label);
            if (rangeCase == 4) Require(actual.Health == target.Health && (actual.Status("armor")?.Stacks ?? 0) == armor,
                "Rejected max-zero effect changed the target: " + label);
            foreach (string stage in new[] { "CopiedActual", "After", "Cleared" })
                Require(row.GetProperty(stage).GetProperty("Context").GetProperty("Enchantments").GetProperty("TestRng")
                    .Deserialize<UnityRng>().Equals(cursor) &&
                    row.GetProperty(stage).GetProperty("Context").GetProperty("Statistics").GetProperty("LastAttackDamageDealt").GetInt32() ==
                        before.Context.Statistics!.LastAttackDamageDealt, "Cancellation lost BattleTest or changed attack statistics: " + label + "/" + stage);
            CardHoverFixtureChecks.Match(row, battleTestAdvances: true);
            if (++checkedRoots % 128 == 0) Console.WriteLine($"RANDOM-DAMAGE-CARD-PREVIEW progress: {teamName} {checkedRoots}/512 roots matched.");
        }
        Require(tests >= 512 && applications > 0 && changed > 0 && negative > 0 && zero > 0, "Missing random-damage positive/rejected controls.");
        Console.WriteLine($"RANDOM-DAMAGE-CARD-PREVIEW PASS: 512 native {teamName} hovers; {tests} tests, {applications} applications, {changed} advanced BattleTest roots; signed/fractional/equal/max-zero ranges, full copy/effect/cancel states and 16 immutable branches; Battle and other gameplay streams preserved.");
    }
}
