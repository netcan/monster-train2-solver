using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OnCastGroupsChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static int[] Sizes(int mode) => mode switch { 23 => [2], 25 => [2, 1], 26 => [1, 2], 29 => [1, 2, 1], _ => [1, 1] };
    private static UnityRng Stream(FixtureValue capture)
    {
        var words = capture.GetProperty("Streams").EnumerateArray().Single(stream => stream.GetProperty("Id").GetString() == "BattleTest")
            .GetProperty("Words").EnumerateArray().Select(word => word.GetUInt32()).ToArray();
        Require(words.Length == 4, "Incomplete grouped RNG stream.");
        return new UnityRng(words[0], words[1], words[2], words[3]);
    }
    private static void Rule(CardPlayRule rule, int mode, int amount, int rangeCase, CombatTeam team)
    {
        Require(rule.OnCastEffectGroupSizes != null && rule.OnCastEffectGroupSizes.SequenceEqual(Sizes(mode)) &&
            rule.Effects.Count == (mode == 27 ? 2 : 1), "Native OnCast partition differs: " + mode);
        var expected = new List<(string Type, string Target, int Value, bool Fail, bool Both)>();
        void Add(string type, string target, int value, bool fail = false, bool both = false) => expected.Add((type, target, value, fail, both));
        Add("Damage", mode is 28 or 31 ? "Room" : "DropTargetCharacter", 1, mode == 27);
        if (mode == 27) Add("Damage", "Room", amount);
        switch (mode)
        {
            case 23: Add("Damage", "DropTargetCharacter", 1); Add("Heal", "DropTargetCharacter", amount); break;
            case 24: Add("Damage", "Room", 1); Add("Heal", "DropTargetCharacter", amount); break;
            case 25: Add("Damage", "DropTargetCharacter", 1, true); Add("Damage", "Room", amount); Add("Heal", "DropTargetCharacter", amount); break;
            case 26: Add("Damage", "DropTargetCharacter", 1, true); Add("Damage", "Room", 1); Add("Heal", "DropTargetCharacter", amount); break;
            case 27: Add("Heal", "DropTargetCharacter", amount); Add("Damage", "Room", 1); break;
            case 28: Add("Heal", "DropTargetCharacter", 1); Add("Damage", "Room", amount); break;
            case 29: Add("Damage", "Room", 1); Add("Damage", "DropTargetCharacter", 1, true); Add("Damage", "Room", amount); Add("Heal", "DropTargetCharacter", amount); break;
            case 30: Add("Heal", "Room", 1); Add("Damage", "LastTargetedCharacters", amount); break;
            case 31: Add("Heal", "DropTargetCharacter", 1); Add("Damage", "LastTargetedCharacters", amount, both: true); break;
        }
        var effects = rule.Effects.Concat(rule.OnCastEffects).ToArray();
        Require(effects.Length == expected.Count && rule.OnCastEffectGroupSizes.Sum() == rule.OnCastEffects.Count, "Missing grouped effects.");
        for (int index = 0; index < effects.Length; index++)
        {
            var a = effects[index]; var e = expected[index]; bool both = e.Both || e.Target == "Room";
            Require(a.Type == e.Type && a.Target == e.Target && a.Value == e.Value &&
                a.AllowEnemy == (both || team == CombatTeam.Enemy) && a.AllowPlayer == (both || team == CombatTeam.Player) &&
                a.Tests?.CancelSubsequent == e.Fail && (e.Fail ? a.Range is { Min: 0, Max: 0, Multiplier: 1f } :
                    rangeCase == 0 ? a.Range == null : a.Range is { Min: 0, Multiplier: .75f } range && range.Max == e.Value + 7),
                "Authored OnCast effect differs: " + mode + "/" + index);
        }
    }
    private static CardPlayRule WithGroups(CardPlayRule rule, IReadOnlyList<int>? sizes) => new(rule.DataId, rule.AssetKey, rule.Cost,
        rule.Effect, rule.Destination, rule.SpawnUnit, rule.ExternalInteractions, rule.Effects, rule.UpgradeInteractions,
        rule.HandDiscardInteractions, rule.HandConsumeInteractions, rule.CostType, rule.Equipment, rule.Ability, rule.Summon,
        rule.CardType, rule.IsAnyAbility, rule.CanPlayInEngineRoom, rule.OnCastEffects, rule.OnUnplayedNegativeEffects,
        rule.UnplayedNegativeOrder, rule.HasUnsupportedUnplayedTriggers, rule.ForgeEligible, rule.HasKillTriggers, rule.OnForgeEffects, sizes);
    internal static void Hover(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == "NativeOnCastGroupsCardHover" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("AuthoredControls").GetBoolean() &&
            root.GetProperty("SelfTargetId").GetInt32() == 0 &&
            root.GetProperty("OriginalSuppression").GetInt32() == root.GetProperty("FinalSuppression").GetInt32(), "Incomplete grouped hover evidence.");
        string teamName = root.GetProperty("TargetTeam").GetString()!;
        Require(teamName is "Player" or "Enemy", "Unknown grouped target team.");
        CombatTeam team = teamName == "Enemy" ? CombatTeam.Enemy : CombatTeam.Player;
        int baseHealth = root.GetProperty("BaseHealth").GetInt32();
        var rows = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(rows.Length == 576 && rows.Select(row => (row.GetProperty("Horde").GetBoolean(), row.GetProperty("Undying").GetBoolean(),
            row.GetProperty("ActiveHalfHealth").GetBoolean(), row.GetProperty("Lethal").GetBoolean(), row.GetProperty("Armor").GetInt32(),
            row.GetProperty("Composition").GetInt32(), row.GetProperty("RangeCase").GetInt32())).Distinct().Count() == 576,
            "Missing grouped hover matrix.");
        int roots = 0, tests = 0, applications = 0, history = 0, flattenDifferences = 0;
        foreach (var row in rows)
        {
            string label = row.GetProperty("Label").GetString()!;
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!; var rule = row.GetProperty("Rule").Deserialize<CardPlayRule>()!;
            int id = row.GetProperty("TargetId").GetInt32(), mode = row.GetProperty("Composition").GetInt32();
            int rangeCase = row.GetProperty("RangeCase").GetInt32(), amount = row.GetProperty("Amount").GetInt32(), armor = row.GetProperty("Armor").GetInt32();
            bool horde = row.GetProperty("Horde").GetBoolean(), half = row.GetProperty("ActiveHalfHealth").GetBoolean();
            var target = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            Require(mode is >= 23 and <= 31 && rangeCase is 0 or 1 && target.Team == team && baseHealth >= 2 &&
                target.Modifiers?.CanBeHealed == true && target.MaxHealth == baseHealth * (horde ? 3 : 1) &&
                target.Health == target.MaxHealth / 2 + (half ? 0 : 1) && (target.Status("horde")?.Stacks ?? 0) == (horde ? 2 : 0) &&
                (target.Status("undying")?.Stacks ?? 0) == (row.GetProperty("Undying").GetBoolean() ? 1 : 0) &&
                armor is 0 or 3 && (target.Status("armor")?.Stacks ?? 0) == armor &&
                target.Triggers.Count(HalfHealthDeathwishModel.IsTrigger) == 2 && target.Triggers.Where(HalfHealthDeathwishModel.IsTrigger).All(trigger => trigger.DeathwishActive == half) &&
                target.Triggers.Single(trigger => trigger.Kind == "OnHeal").Effects.Single().Value == 17 &&
                amount == (row.GetProperty("Lethal").GetBoolean() ? target.MaxHealth + 3 : Math.Max(1, baseHealth / 3)) &&
                row.GetProperty("DropTargetId").GetInt32() == id && row.GetProperty("NativeReturned").GetBoolean() &&
                row.GetProperty("QueueEmpty").GetBoolean() && row.GetProperty("TemporaryDisabled").GetBoolean() &&
                row.GetProperty("EnableCount").GetInt32() == 1 && row.GetProperty("DisableCount").GetInt32() == 1, "Invalid grouped lifecycle: " + label);
            Rule(rule, mode, amount, rangeCase, team);
            var effects = rule.Effects.Concat(rule.OnCastEffects).ToArray();
            int skipped = mode switch { 25 => 2, 27 => 1, 29 => 3, _ => -1 };
            UnityRng cursor = Stream(row.GetProperty("RngBefore")); var draws = row.GetProperty("RangeDraws").EnumerateArray().ToArray();
            Require(cursor.Equals(before.Context!.Enchantments!.TestRng), "Grouped test stream projection differs.");
            int previous = -1;
            foreach (var draw in draws)
            {
                int index = draw.GetProperty("EffectIndex").GetInt32();
                Require(index >= previous && index >= 0 && index < effects.Length && index != skipped, "Cancelled group drew a quantity: " + label);
                previous = index; var effect = effects[index]; bool test = draw.GetProperty("Test").GetBoolean(); var range = effect.Range;
                Require(range != null && draw.GetProperty("Preview").GetBoolean() && (!test || effect.Type == "Damage") &&
                    draw.GetProperty("EffectType").GetString() == "CardEffect" + effect.Type, "Invalid grouped draw phase: " + label);
                var sampled = range!.Sample(cursor);
                Require(draw.GetProperty("Min").GetInt32() == range.Min && draw.GetProperty("Max").GetInt32() == range.Max &&
                    draw.GetProperty("Multiplier").GetSingle() == range.Multiplier && draw.GetProperty("Value").GetInt32() == sampled.Value &&
                    cursor.Equals(draw.GetProperty("Before").Deserialize<UnityRng>()) && sampled.State.Equals(draw.GetProperty("After").Deserialize<UnityRng>()),
                    "Grouped native draw differs: " + label);
                cursor = sampled.State; if (test) tests++; else applications++;
            }
            for (int index = 0; index < effects.Length; index++)
            {
                var phases = draws.Where(draw => draw.GetProperty("EffectIndex").GetInt32() == index).ToArray();
                int expectedTests = index != skipped && effects[index].Range != null && effects[index].Type == "Damage" ? 1 : 0;
                Require(phases.Count(draw => draw.GetProperty("Test").GetBoolean()) == expectedTests &&
                    phases.Count(draw => !draw.GetProperty("Test").GetBoolean()) <= 1 &&
                    (phases.Length < 2 || phases[0].GetProperty("Test").GetBoolean()) &&
                    (effects[index].Range?.Max != 0 || phases.Length == expectedTests), "Invalid grouped per-effect sampling: " + label);
            }
            bool advances = rangeCase == 1;
            Require(cursor.Equals(Stream(row.GetProperty("RngAfter"))) && advances != cursor.Equals(before.Context.Enchantments.TestRng), "Grouped RNG successor differs: " + label);
            var actors = row.GetProperty("ActualTargets").Deserialize<CombatUnit[]>()!;
            if (rangeCase == 0 && mode is 30 or 31)
            {
                foreach (var witness in actors.Where(actor => actor.Id != id))
                {
                    Require(before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == witness.Id).Health == 10 &&
                        witness.Health == (mode == 30 ? 10 : Math.Max(0, (witness.Team == team ? 10 : 9) - amount)),
                        "OnCast target history was replaced by room/drop candidates: " + label);
                }
                history++;
            }
            var action = new PlayCardAction(row.GetProperty("CardId").GetInt32(), row.GetProperty("RoomIndex").GetInt32(), targetUnitId: id);
            if (rangeCase == 0 && mode == 25)
            {
                var flatRule = WithGroups(rule, [rule.OnCastEffects.Count]);
                using var keys = new BattleStateKeys();
                Require(keys.SnapshotKey(rule) != keys.SnapshotKey(flatRule), "Search key lost OnCast group boundaries.");
                var flat = CardHoverPreviewModel.Apply(before, flatRule, action);
                if (flat.Supported && Json(flat.AfterEffects!) != Json(row.GetProperty("CopiedActual").Deserialize<TrainCombatState>()!)) flattenDifferences++;
                foreach (IReadOnlyList<int>? invalid in new IReadOnlyList<int>?[] { null, new[] { 0, rule.OnCastEffects.Count }, new[] { rule.OnCastEffects.Count + 1 } })
                    Require(!CardHoverPreviewModel.Apply(before, WithGroups(rule, invalid), action).Supported, "Missing/invalid OnCast partition was accepted.");
            }
            CardHoverFixtureChecks.Match(row, battleTestAdvances: advances);
            if (++roots % 96 == 0) Console.WriteLine($"ONCAST-GROUPS-HOVER progress: {teamName} {roots}/576 roots matched.");
        }
        Require(tests == 768 && applications > 0 && history == 64 && flattenDifferences > 0, "Missing grouped cancellation/history witnesses.");
        Console.WriteLine($"ONCAST-GROUPS-HOVER PASS: 576 native {teamName} hovers, nine shapes, {tests} tests/{applications} application draws, " +
            $"{flattenDifferences} observable flattening counterexamples, full actors/cancel/RNG states and 16 immutable branches each.");
    }
    internal static void Battle(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OnCastGroupsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete grouped native battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OnCastGroupsIds").Deserialize<int[]>()!, modes = root.GetProperty("OnCastGroupsModes").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 10 && actions.Length == 10 && modes.SequenceEqual(new[] { 23, 24, 25, 26, 28, 29, 30, 31 }), "Missing grouped live plays.");
        for (int index = 2; index < actions.Length; index++)
        {
            var before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            Require(before.UiRngIsolated && action.CardInstanceId == ids[index] && before.Energy == after.Energy && rule.Cost == 0 &&
                after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == ids[index]), "Grouped live cost/discard/isolation differs.");
            Rule(rule, modes[index - 2], 12, (index - 2) % 2, CombatTeam.Player);
        }
        RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root);
        Console.WriteLine("ONCAST-GROUPS-BATTLE PASS: eight authored controls, ten native plays, full winning chain/16 branches, cancelled groups, room candidates, target history, exact card/resource/RNG states and serial/parallel search.");
    }
}
