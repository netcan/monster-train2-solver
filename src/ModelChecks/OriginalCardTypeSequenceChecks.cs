using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCardTypeSequenceChecks
{
    private static CombatRelicState Owner(int energy = 1) => new(RelicCardTypeSequenceModel.DataId, RelicCardTypeSequenceModel.AssetKey,
        [RelicCardTypeSequenceModel.Effect], isCovenant: false, disallowedInPlacementPhase: true,
        resourceEffects: [new(0, RelicCardTypeSequenceModel.Effect, energy, 1, energy, false,
            conditions: [new(new("SubtypeInDeck"), true, 3, 3, true)], cardTypeSequence: new([]))]);
    private static CombatContext Context()
    {
        CardMaskDefinition definition = new("bonus", "Spell", "Common", false, false, false, [], [], [], [], null, false, 0, 0, false, false, false);
        CardInstanceState card = new(1, "bonus", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: new(definition, 0, [], []));
        return new(new([], [new(1, "bonus")], [], UnityRng.Seed(171), 0, []), UnityRng.Seed(172), 0, 2, 10,
            cardInstances: [card], cardRegistry: [card], relics: [Owner()], queryFrame: new(energy: 0, runningCombat: true, turn: 1, selectedRoomIndex: 0),
            energyState: new(99, 0, 0, "MonsterTurn", true), resources: new(3, 0, 5, 99, 10, "pyre", 20, 80, 0, 20, [], [], []));
    }
    private static RelicResourceEffect Resource(CombatContext source) => source.Relics!.Single(owner => owner.DataId == RelicCardTypeSequenceModel.DataId).ResourceEffects!.Single();
    internal static void Run()
    {
        Require(RelicModel.Validate([Owner()]) == null && RelicModel.Validate([Owner(2)]) != null, "Sequence definition gate failed.");
        CombatContext root = Context();
        var first = RelicCardTypeSequenceModel.Test(root, "Spell", false, out bool accepted);
        Require(first.Supported && accepted && Resource(first.Context).CardTypeSequence!.PlayedTypes.SequenceEqual(["Spell"]), "First distinct type was not recorded.");
        var applied = RelicCardTypeSequenceModel.Apply(first.Context);
        Require(applied.Supported && applied.Context.QueryFrame!.Energy == 0 && applied.Context.Cards.Hand.Count == 0 &&
            Resource(applied.Context).Conditions.Single().DurationTriggerCount == 1, "First type must notify without granting resources.");
        var duplicate = RelicCardTypeSequenceModel.Test(applied.Context, "Spell", false, out bool repeated);
        Require(duplicate.Supported && !repeated, "Repeated type advanced the sequence."); Equal(duplicate.Context, applied.Context, "duplicate rejection");
        var ability = RelicCardTypeSequenceModel.Test(applied.Context, "Junk", true, out bool abilityAccepted);
        Require(ability.Supported && !abilityAccepted, "Ability advanced the sequence."); Equal(ability.Context, applied.Context, "ability rejection");
        var second = RelicCardTypeSequenceModel.Test(applied.Context, "Junk", false, out accepted);
        Require(second.Supported && accepted, "Second distinct type failed.");
        second = RelicCardTypeSequenceModel.Apply(second.Context);
        Require(second.Supported && second.Context.QueryFrame!.Energy == 0 && second.Context.Cards.Hand.Count == 0, "Second type granted resources early.");
        string parent = JsonSerializer.Serialize(second.Context, ModelJson.Options);
        var third = RelicCardTypeSequenceModel.Test(second.Context, "Blight", false, out accepted);
        var bonus = RelicCardTypeSequenceModel.Apply(third.Context);
        Require(accepted && bonus.Supported && bonus.Context.QueryFrame!.Energy == 1 && bonus.Context.Cards.Hand.Single().InstanceId == 1 &&
            Resource(bonus.Context).Conditions.Single().DurationTriggerCount == 3, "Third type must grant +1 energy and draw one card.");
        var capped = RelicCardTypeSequenceModel.Test(bonus.Context, null, null, out accepted);
        Require(capped.Supported && !accepted, "Three-type cap must precede card dereference.");
        var managed = RelicCardTypeSequenceModel.Played(bonus.Context, 0, null, null, false, false);
        Equal(managed.Context, bonus.Context, "manager three-type cap");
        var raw = RelicCardTypeSequenceModel.Apply(bonus.Context);
        Require(raw.Supported && raw.Context.QueryFrame!.Energy == 2 && Resource(raw.Context).Conditions.Single().DurationTriggerCount == 4,
            "Unconditional raw Apply was confused with the manager condition gate.");
        var reset = RelicCardTypeSequenceModel.StartTurn(raw.Context);
        Require(reset.Supported && Resource(reset.Context).CardTypeSequence!.PlayedTypes.Count == 0 &&
            Resource(reset.Context).Conditions.Single().DurationTriggerCount == 4, "Raw start callback must clear types independently of condition duration reset.");
        CombatContext next = RelicConditionModel.EndDuration(reset.Context, "ThisTurn");
        Require(Resource(next).Conditions.Single().DurationTriggerCount == 0, "Trigger-count duration did not reset.");
        Equal(RelicCardTypeSequenceModel.Played(root, 0, null, null, true, false).Context, root, "deployment gate");
        Equal(RelicCardTypeSequenceModel.Played(root, 0, null, null, false, true).Context, root, "preview manager gate");
        var removed = RelicCardTypeSequenceModel.Test(root.WithRelics([]), "Spell", false, out accepted);
        Require(removed.Supported && !accepted, "Removed owner still records types.");
        Parallel.For(0, 16, branch => Equal(RelicCardTypeSequenceModel.Test(second.Context, "Blight", false, out _).Context, third.Context, "parallel sequence"));
        Require(JsonSerializer.Serialize(second.Context, ModelJson.Options) == parent, "Sequence mutated its parent.");
        Console.WriteLine("CARD-TYPE-SEQUENCE-PURE PASS: ordered unique types, repeat/ability/cap gates, first/second notifications, third-type resources, raw/managed distinction, resets, owner ablation and 16 immutable branches.");
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 237 && root.GetProperty("OriginalCardTypeSequencePrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete sequence battle.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalCardTypeSequenceCaptureFailures",
            "OriginalCardTypeSequenceMismatches", "OriginalCardTypeSequenceUnsupported", "OriginalCardTypeSequencePending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Nonzero " + field);
        int tests = 0, applications = 0, starts = 0, duplicates = 0, abilities = 0, caps = 0, bonuses = 0, emptyBonuses = 0, early = 0, resets = 0;
        var bonusTurns = new HashSet<int>();
        foreach (var row in root.GetProperty("RelicCardTypeSequenceRecords").EnumerateArray())
        {
            string kind = row.GetProperty("Kind").GetString()!;
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int id = row.GetProperty("CardId").GetInt32(); bool control = row.GetProperty("Control").GetBoolean();
            string? type = row.GetProperty("CardType").GetString(); bool? ability = row.GetProperty("IsAbility").Deserialize<bool?>();
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            void Replay()
            {
                RelicFirstCardCostResult result;
                if (kind == "Test") { result = RelicCardTypeSequenceModel.Test(before.Context!, type, ability, out bool returned);
                    Require(returned == row.GetProperty("Returned").GetBoolean(), "Sequence Test bool differs."); }
                else if (kind == "Apply") result = RelicCardTypeSequenceModel.Apply(before.Context!, id);
                else if (kind == "Start") result = RelicCardTypeSequenceModel.StartTurn(before.Context!);
                else { result = new(before.Context!); Require(kind == "StartTest" && row.GetProperty("Returned").GetBoolean(), "Start Test differs."); }
                Require(result.Supported, result.UnsupportedReason!);
                Equal(CardSpellModel.WithContext(before, result.Context), actual, "native " + kind);
            }
            Replay(); Parallel.For(0, 16, _ => Replay());
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native sequence replay mutated its parent.");
            var prior = Resource(before.Context!).CardTypeSequence!.PlayedTypes;
            if (kind == "Test")
            {
                tests++; bool returned = row.GetProperty("Returned").GetBoolean();
                if (prior.Count == 3 && !returned) caps++;
                else if (ability == true && type != null && !prior.Contains(type) && !returned) abilities++;
                else if (type != null && prior.Contains(type) && !returned) duplicates++;
            }
            if (kind == "Start") { starts++; Require(Resource(actual.Context!).CardTypeSequence!.PlayedTypes.Count == 0, "Start did not clear types."); if (prior.Count > 0) resets++; }
            if (kind == "Apply")
            {
                applications++; Require(row.GetProperty("Notifications").GetInt32() == 1, "Apply must notify for every distinct type.");
                Require(Resource(actual.Context!).Conditions.Single().DurationTriggerCount == Resource(before.Context!).Conditions.Single().DurationTriggerCount + 1,
                    "Native notification did not advance condition count.");
                if (prior.Count < 3) { early++; Require(before.Context!.QueryFrame!.Energy == actual.Context!.QueryFrame!.Energy &&
                    before.Context.Cards.Hand.Count == actual.Context.Cards.Hand.Count, "Early sequence step granted resources."); }
                else
                {
                    Require(actual.Context!.QueryFrame!.Energy == before.Context!.QueryFrame!.Energy + 1, "Third-type energy bonus is absent.");
                    if (!control) { bonuses++; bonusTurns.Add(before.Context.QueryFrame.Turn!.Value);
                        Require(actual.Context.Cards.Hand.Count == before.Context.Cards.Hand.Count + 1, "Natural third-type bonus lacks draw evidence."); }
                    else if (before.Context.Cards.Draw.Count == 0 && before.Context.Cards.Discard.Count == 0 &&
                        actual.Context.Cards.Hand.Count == before.Context.Cards.Hand.Count) emptyBonuses++;
                }
            }
            if (!control && kind is "Test" or "Apply") Require(before.Context!.QueryFrame!.Turn > 0, "Native deployment manager gate failed.");
        }
        Require(tests >= 12 && applications >= 9 && starts >= 3 && duplicates > 0 && abilities > 0 && caps > 0 &&
            bonuses == 2 && bonusTurns.SetEquals([1, 2]) && emptyBonuses > 0 && early >= 6 && resets >= 2,
            "Missing sequence, reset, duplicate, independent ability, cap, empty-draw or two-turn bonus witnesses.");
        Require(root.GetProperty("Actions").GetArrayLength() == 10, "Missing natural sequence plays.");
        Console.WriteLine($"ORIGINAL-CARD-TYPE-SEQUENCE-NATIVE PASS: {tests} exact Tests, {applications} applications, {starts} starts, repeat/ability/cap controls, two natural energy/draw bonuses and 16 immutable branches.");
    }
    private static void Equal<T>(T expected, T actual, string label)
    { string a = JsonSerializer.Serialize(expected, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options); Require(a == b, label + ": " + ModelJson.Difference(a, b)); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
