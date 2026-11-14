using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HealingDamageCardPreviewChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static UnityRng Stream(FixtureValue capture, string name)
    {
        var words = capture.GetProperty("Streams").EnumerateArray().Single(stream => stream.GetProperty("Id").GetString() == name)
            .GetProperty("Words").EnumerateArray().Select(word => word.GetUInt32()).ToArray();
        Require(words.Length == 4, "Incomplete healing RNG stream.");
        return new UnityRng(words[0], words[1], words[2], words[3]);
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == "NativeHealingDamageCardHover" &&
            root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("AuthoredControls").GetBoolean() &&
            root.GetProperty("SelfTargetId").GetInt32() == 0 &&
            root.GetProperty("OriginalSuppression").GetInt32() == root.GetProperty("FinalSuppression").GetInt32(),
            "Incomplete native healing/damage evidence.");
        string teamName = root.GetProperty("TargetTeam").GetString()!;
        Require(teamName is "Player" or "Enemy", "Unknown healing target team.");
        CombatTeam team = teamName == "Enemy" ? CombatTeam.Enemy : CombatTeam.Player;
        int baseHealth = root.GetProperty("BaseHealth").GetInt32();
        var rows = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(rows.Length == 640 && rows.Select(row => (row.GetProperty("Horde").GetBoolean(), row.GetProperty("Undying").GetBoolean(),
            row.GetProperty("ActiveHalfHealth").GetBoolean(), row.GetProperty("Lethal").GetBoolean(), row.GetProperty("Armor").GetInt32(),
            row.GetProperty("Composition").GetInt32(), row.GetProperty("RangeCase").GetInt32())).Distinct().Count() == 640,
            "Missing healing lifecycle, shape or range controls.");
        int roots = 0, tests = 0, healingDraws = 0, damageDraws = 0, zeroHeals = 0, immuneHeals = 0, deadHeals = 0;
        foreach (var row in rows)
        {
            string label = row.GetProperty("Label").GetString()!;
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var rule = row.GetProperty("Rule").Deserialize<CardPlayRule>()!;
            int id = row.GetProperty("TargetId").GetInt32(), mode = row.GetProperty("Composition").GetInt32();
            int rangeCase = row.GetProperty("RangeCase").GetInt32(), amount = row.GetProperty("Amount").GetInt32(), armor = row.GetProperty("Armor").GetInt32();
            bool horde = row.GetProperty("Horde").GetBoolean(), half = row.GetProperty("ActiveHalfHealth").GetBoolean();
            bool undying = row.GetProperty("Undying").GetBoolean(), large = row.GetProperty("Lethal").GetBoolean();
            var target = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            var actual = row.GetProperty("ActualTarget").Deserialize<CombatUnit>()!;
            bool roomMain = mode is 14 or 18 or 19 or 22, onCast = mode is 17 or 18 or 20 or 21 or 22;
            Require(mode is >= 13 and <= 22 && rangeCase is 0 or 1 && target.Team == team && baseHealth >= 2 &&
                target.MaxHealth == baseHealth * (horde ? 3 : 1) && target.Health == target.MaxHealth / 2 + (half ? 0 : 1) &&
                target.Modifiers?.CanBeHealed == true && (target.Status("horde")?.Stacks ?? 0) == (horde ? 2 : 0) &&
                armor is 0 or 3 && (target.Status("armor")?.Stacks ?? 0) == armor &&
                (target.Status("undying")?.Stacks ?? 0) == (undying ? 1 : 0) &&
                (target.Status("heal immunity")?.Stacks ?? 0) == (mode == 22 ? 1 : 0) &&
                target.Triggers.Count(HalfHealthDeathwishModel.IsTrigger) == 2 && target.Triggers.Where(HalfHealthDeathwishModel.IsTrigger)
                    .All(trigger => trigger.DeathwishActive == half) &&
                target.Triggers.Single(trigger => trigger.Kind == "OnHeal").Effects.Single().Value == 17 &&
                amount == (large ? baseHealth * (horde ? 3 : 1) + 3 : Math.Max(1, baseHealth / 3)) &&
                row.GetProperty("DropTargetId").GetInt32() == id && row.GetProperty("RoomEffect").GetBoolean() == roomMain &&
                row.GetProperty("OnCastDamage").GetBoolean() == onCast && rule.Effects.Count == (mode is 15 or 16 or 19 ? 2 : 1) &&
                rule.OnCastEffects.Count == (onCast ? 1 : 0) && row.GetProperty("NativeReturned").GetBoolean() &&
                row.GetProperty("QueueEmpty").GetBoolean() && row.GetProperty("TemporaryDisabled").GetBoolean() &&
                row.GetProperty("EnableCount").GetInt32() == 1 && row.GetProperty("DisableCount").GetInt32() == 1,
                "Invalid native healing control: " + label);
            var effects = rule.Effects.Concat(rule.OnCastEffects).ToArray();
            for (int index = 0; index < effects.Length; index++)
            {
                bool healing = index == 0 ? mode is 13 or 14 or 16 or 18 or 21 or 22 : mode is 15 or 17 or 19 or 20 or 21;
                bool area = index == 0 ? roomMain : mode is 16 or 17;
                int value = index == 0 ? mode is 13 or 14 or 16 or 20 or 22 ? amount : mode == 21 ? 0 : 1 : mode is 16 or 22 ? 1 : amount;
                var effect = effects[index];
                Require(effect.Type == (healing ? "Heal" : "Damage") && effect.Target == (area ? "Room" : "DropTargetCharacter") &&
                    effect.Value == value && effect.AllowEnemy == (area || team == CombatTeam.Enemy) &&
                    effect.AllowPlayer == (area || team == CombatTeam.Player) && effect.Tests?.CancelSubsequent == false &&
                    (rangeCase == 0 ? effect.Range == null : effect.Range is { Min: 0, Multiplier: .75f } range && range.Max == value + 7),
                    "Authored healing effect differs: " + label + "/" + index);
            }
            UnityRng cursor = Stream(row.GetProperty("RngBefore"), "BattleTest");
            Require(cursor.Equals(before.Context!.Enchantments!.TestRng), "Healing native RNG projection differs: " + label);
            var draws = row.GetProperty("RangeDraws").EnumerateArray().ToArray();
            int previousIndex = -1;
            foreach (var draw in draws)
            {
                int index = draw.GetProperty("EffectIndex").GetInt32();
                Require(rangeCase == 1 && index >= previousIndex && index < effects.Length && index >= 0,
                    "Unexpected healing draw order: " + label);
                previousIndex = index;
                var effect = effects[index];
                bool test = draw.GetProperty("Test").GetBoolean();
                Require(draw.GetProperty("Preview").GetBoolean() && draw.GetProperty("EffectType").GetString() == "CardEffect" + effect.Type &&
                    (!test || effect.Type == "Damage"), "Healing unexpectedly sampled a test quantity: " + label);
                if (test) tests++; else if (effect.Type == "Heal") healingDraws++; else damageDraws++;
                var range = effect.Range!;
                var sampled = range.Sample(cursor);
                Require(draw.GetProperty("Min").GetInt32() == range.Min && draw.GetProperty("Max").GetInt32() == range.Max &&
                    draw.GetProperty("Multiplier").GetSingle() == range.Multiplier && draw.GetProperty("Value").GetInt32() == sampled.Value &&
                    cursor.Equals(draw.GetProperty("Before").Deserialize<UnityRng>()) && sampled.State.Equals(draw.GetProperty("After").Deserialize<UnityRng>()),
                    "Healing native range draw differs: " + label);
                cursor = sampled.State;
            }
            for (int index = 0; index < effects.Length; index++)
            {
                var effectDraws = draws.Where(draw => draw.GetProperty("EffectIndex").GetInt32() == index).ToArray();
                int expectedTests = rangeCase == 1 && effects[index].Type == "Damage" ? 1 : 0;
                Require(effectDraws.Count(draw => draw.GetProperty("Test").GetBoolean()) == expectedTests &&
                    effectDraws.Count(draw => !draw.GetProperty("Test").GetBoolean()) <= 1 &&
                    (effectDraws.Length < 2 || effectDraws[0].GetProperty("Test").GetBoolean()), "Invalid healing effect phases: " + label);
            }
            Require(cursor.Equals(Stream(row.GetProperty("RngAfter"), "BattleTest")) &&
                (rangeCase == 0) == cursor.Equals(before.Context.Enchantments.TestRng), "Incomplete healing range trace: " + label);
            var heals = row.GetProperty("Dispatches").EnumerateArray().Where(dispatch => dispatch.GetProperty("ActorId").GetInt32() == id &&
                dispatch.GetProperty("Kind").GetString() == "OnHeal").ToArray();
            if (rangeCase == 0 && mode is 13 or 14)
                Require(actual.Health == Math.Min(target.MaxHealth, target.Health + amount) && heals.Length == 1,
                    "Pure healing did not restore/cap health and notify: " + label);
            if (rangeCase == 0 && mode == 21)
            {
                Require(heals.Length == 2 && heals[0].GetProperty("ParamInt").GetInt32() == 0, "Zero healing skipped its callback: " + label);
                zeroHeals++;
            }
            if (rangeCase == 0 && mode == 22)
            {
                Require(heals.Length == 1 && heals[0].GetProperty("ParamInt").GetInt32() == 0 && actual.Health <= target.Health,
                    "Healing immunity restored health or skipped its callback: " + label);
                immuneHeals++;
            }
            if (rangeCase == 0 && mode == 20 && large && !undying && armor == 0)
            {
                Require(actual.Health == 0 && heals.Length == 0, "Post-lethal healing revived a dead actor: " + label);
                deadHeals++;
            }
            foreach (string stage in new[] { "CopiedActual", "After", "Cleared" })
                Require(row.GetProperty(stage).GetProperty("Context").GetProperty("Enchantments").GetProperty("TestRng").Deserialize<UnityRng>().Equals(cursor),
                    "Healing cancellation restored the wrong test RNG: " + label);
            CardHoverFixtureChecks.Match(row, battleTestAdvances: rangeCase == 1);
            if (++roots % 128 == 0) Console.WriteLine($"HEALING-DAMAGE-CARD-PREVIEW progress: {teamName} {roots}/640 roots matched.");
        }
        Require(tests == 224 && healingDraws > 0 && damageDraws > 0 && zeroHeals == 32 && immuneHeals == 32 && deadHeals == 4,
            "Missing healing RNG, zero/immunity or post-lethal controls.");
        Console.WriteLine($"HEALING-DAMAGE-CARD-PREVIEW PASS: 640 native {teamName} hovers, ten healing/damage shapes; {tests} damage tests, " +
            $"{healingDraws} healing and {damageDraws} damage application draws; full temporary actors, copy/effect/cancel states, exact RNG and 16 immutable branches each.");
    }
}
