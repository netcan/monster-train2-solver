using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CardUpgradeLifecycleChecks
{
    private static CardLifecycleUpgrade Upgrade(string id, string asset, bool unique = false, bool discard = false,
        CardTraitValue[]? traits = null, CardLifecycleTrigger[]? triggers = null, string[]? replace = null, int instance = 0) =>
        new(new(id, new(), [], false, false, false, false), asset, unique, discard, traits ?? [], [], false, [], replace ?? [], triggers ?? [], instanceId: instance);
    private static CardUpgradeLifecycleState State(CardLifecycleUpgrade[]? permanent = null, CardLifecycleUpgrade[]? temporary = null)
    {
        permanent ??= []; temporary ??= [];
        var definition = new CardMaskDefinition("card", "Spell", "Common", false, false, false, [], [], [], ["main"], "", false, 0, 0, false, false, false);
        var composition = new CardTraitCompositionState([], [], temporary.Select(upgrade => upgrade.AddedTraits).ToArray(), [], [], []);
        var card = new CardOwnedMaskState(definition, 0, new(new(), permanent.Select(upgrade => upgrade.Values).ToArray()),
            new(new(), temporary.Select(upgrade => upgrade.Values).ToArray()), new(composition, false, 1, 1, 1, 1), [], false, false);
        return new(card, permanent, temporary, [], [], true, "DiscardPile");
    }
    internal static void Run()
    {
        var trigger = new CardLifecycleTrigger("OnCast", "key", ["cast"]);
        var trait = new CardTraitValue("CardTraitStrongerMagicPower", "CardTraitStrongerMagicPower", 0, 0, true, 0);
        var unique = Upgrade("same", "same", true, traits: [trait], triggers: [trigger]);
        var initial = State([unique]); var first = CardUpgradeLifecycleModel.ApplyTemporary(initial, unique, "a");
        var rejected = CardUpgradeLifecycleModel.ApplyTemporary(first.State, unique, "b");
        Require(first.Returned == true && rejected.Returned == false && ReferenceEquals(rejected.State, first.State), "Temporary uniqueness incorrectly searched permanent upgrades or altered rejected state.");
        var indexed = CardUpgradeLifecycleModel.RemoveByIndex(first.State, 0);
        Require(indexed.State.Card.Traits.TemporaryCount == first.State.Card.Traits.TemporaryCount &&
            CardOwnedMaskModel.Resolve(indexed.State.Card).Card.Traits.SequenceEqual(["CardTraitStrongerMagicPower"]), "Indexed removal did not preserve the native stale trait cache.");
        var removed = CardUpgradeLifecycleModel.RemoveById(first.State, "same", "a");
        Require(removed.Returned == true && removed.State.Temporary.Count == 0 && removed.State.UpgradeTriggers.Count == 0 &&
            removed.State.Card.Traits.TemporaryCount == first.State.Card.Traits.TemporaryCount + 1 &&
            CardOwnedMaskModel.Resolve(removed.State.Card).Card.Traits.Count == 0, "ID removal failed to refresh/remove traits, tagged triggers or modifier count.");
        var empty = Upgrade("", "", true, triggers: [trigger]);
        var twice = CardUpgradeLifecycleModel.ApplyTemporary(CardUpgradeLifecycleModel.ApplyTemporary(State(), empty, "a").State, empty, "b");
        Require(twice.State.Temporary.Count == 2, "Empty data IDs incorrectly participated in uniqueness.");
        var tagged = CardUpgradeLifecycleModel.RemoveById(twice.State, "", "a");
        Require(tagged.State.UpgradeTriggers.Single().Id == "b", "Tagged removal removed the other installed trigger scope.");
        var discarded = CardUpgradeLifecycleModel.Discard(CardUpgradeLifecycleModel.ApplyTemporary(State(), Upgrade("d", "d", discard: true, triggers: [trigger])).State);
        Require(discarded.State.Temporary.Count == 0 && discarded.State.UpgradeTriggers.Count == 1, "Discard incorrectly uninstalled trigger definitions.");
        var replaced = CardUpgradeLifecycleModel.ApplyTemporary(State(temporary: [Upgrade("x", "first"), Upgrade("x", "target")]), Upgrade("new", "new", replace: ["target"]));
        Require(replaced.State.Temporary.Select(upgrade => upgrade.AssetKey).SequenceEqual(["target", "new"]), "Replacement missed native asset selection followed by first-data-ID removal.");
        var scaled = new CardLifecycleUpgrade(new("p", new(damage: 99, heal: 99), [], false, false, false, false), "p", false, false, [], [], false, [], [], [], 2, 3);
        var reset = CardUpgradeLifecycleModel.Reset(State([scaled]));
        Require(reset.State.Permanent[0].Values.Stats.Damage == 2 && reset.State.Permanent[0].Values.Stats.Heal == 3 &&
            !reset.State.StandbyOverride && reset.State.StandbyPile == "None" && reset.State.Card.Traits.TemporaryCount == 1,
            "Reset lost original magic power statistics, standby override clearing or fresh temporary counter.");
        string parent = JsonSerializer.Serialize(initial);
        Parallel.For(0, 32, _ => Require(CardUpgradeLifecycleModel.ApplyTemporary(initial, unique).State.Temporary.Count == 1, "Parallel lifecycle branches differ."));
        Require(JsonSerializer.Serialize(initial) == parent, "Card upgrade lifecycle mutated its parent.");
        Console.WriteLine("CARD-UPGRADE-LIFECYCLE-CHECKS PASS: temporary uniqueness/empty IDs, ID/index removal cache differences, trigger scopes, discard retention, replacements, reset/recalculation and immutable parents.");
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) && root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")),
            "Native upgrade lifecycle calibration changed context/RNG/frame or game identity.");
        var originalRelics = root.GetProperty("OriginalRelicAssets").Deserialize<string[]>()!;
        Require(originalRelics.Length == 11 && originalRelics.Distinct().Count() == 11 &&
            root.GetProperty("MultiplierOverrides").Deserialize<string[]>()!.SequenceEqual(["CardTraitScalingMagicPowerOnMoonPhase", "CardTraitStrongerMagicPower"]), "Original relic coverage or native magic multiplier override inventory differs.");
        var rows = root.GetProperty("Rows").EnumerateArray().Select(row => (
            Group: row.GetProperty("Group").GetString()!, Operation: row.GetProperty("Operation").GetString()!,
            Payload: row.GetProperty("Payload").Deserialize<CardLifecycleUpgrade>(), Target: row.GetProperty("Target").GetString(),
            Index: row.GetProperty("Index").GetInt32(), TriggerId: row.GetProperty("TriggerId").GetString(),
            Before: row.GetProperty("Before").Deserialize<CardUpgradeLifecycleState>()!, Actual: row.GetProperty("Actual").Deserialize<CardUpgradeLifecycleState>()!,
            Returned: row.GetProperty("Returned").Deserialize<bool?>(), Card: row.GetProperty("Card").Deserialize<CardUpgradeMaskCard>()!,
            AfterQuery: row.GetProperty("AfterQuery").Deserialize<CardUpgradeLifecycleState>()!)).ToArray();
        Require(rows.Length == 212 && rows.Count(row => row.Group.StartsWith("original/")) == 88 && rows.Count(row => row.Group.StartsWith("moon/")) == 12 && rows.Select(row => row.Group).Distinct().Count() == 36 &&
            rows.Select(row => row.Operation).Distinct().Count() == 7 && rows.Any(row => row.Returned == false) &&
            rows.Any(row => row.Actual.Temporary.GroupBy(upgrade => upgrade.InstanceId).Any(group => group.Count() > 1)), "Native upgrade lifecycle operation/alias coverage differs.");
        string parent = JsonSerializer.Serialize(rows.Select(row => new { row.Before, row.Payload, row.Actual, row.AfterQuery }));
        void Check()
        {
            foreach (var group in rows.GroupBy(row => row.Group))
            {
                var state = group.First().Before;
                foreach (var row in group)
                {
                    Require(Equal(state, row.Before), $"Native carried upgrade input differs: {row.Group}/{row.Operation}.");
                    var step = row.Operation switch {
                        "Apply" => CardUpgradeLifecycleModel.ApplyTemporary(state, row.Payload!, row.TriggerId),
                        "RemoveId" => CardUpgradeLifecycleModel.RemoveById(state, row.Target!, row.TriggerId),
                        "RemoveIndex" => CardUpgradeLifecycleModel.RemoveByIndex(state, row.Index),
                        "Discard" => CardUpgradeLifecycleModel.Discard(state), "Reset" => CardUpgradeLifecycleModel.Reset(state),
                        "ResetTraits" => CardUpgradeLifecycleModel.ResetTraits(state), "Query" => new CardUpgradeLifecycleStep(state, null),
                        _ => throw new InvalidOperationException(row.Operation) };
                    Require(step.Returned == row.Returned && Equal(step.State, row.Actual),
                        $"Native upgrade lifecycle differs: {row.Group}/{row.Operation}, predicted={JsonSerializer.Serialize(step.State)}, actual={JsonSerializer.Serialize(row.Actual)}.");
                    var view = CardOwnedMaskModel.Resolve(step.State.Card);
                    state = new CardUpgradeLifecycleState(view.State, step.State.Permanent, step.State.Temporary, step.State.AuthoredTriggers,
                        step.State.UpgradeTriggers, step.State.StandbyOverride, step.State.StandbyPile);
                    Require(Equal(view.Card, row.Card) && Equal(state, row.AfterQuery), $"Native post-lifecycle query differs: {row.Group}/{row.Operation}.");
                }
            }
        }
        Check(); Parallel.For(0, 32, _ => Check());
        Require(JsonSerializer.Serialize(rows.Select(row => new { row.Before, row.Payload, row.Actual, row.AfterQuery })) == parent, "Upgrade lifecycle replay mutated its parent inputs.");
        Console.WriteLine("NATIVE-CARD-UPGRADE-LIFECYCLE-CHECKS PASS: 212 carried native operations/queries, 11 original relic payloads on units/spells, ID/index/discard/reset differences, aliases/tagged trigger definitions, both native magic multipliers with six signed rounding cases and 32 immutable branches.");
    }
    private static bool Equal<T>(T first, T second) => JsonSerializer.Serialize(first) == JsonSerializer.Serialize(second);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
