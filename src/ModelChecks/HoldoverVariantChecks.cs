using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HoldoverVariantChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("FrameBefore").GetInt32() == root.GetProperty("FrameAfter").GetInt32() &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")), "Native calibration changed live state/RNG/frame.");
        int count = 0, positive = 0, cached = 0, rebuilt = 0, clones = 0, removed = 0;
        var groups = new Dictionary<string, int>();
        foreach (var row in root.GetProperty("Rows").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            var actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
            string parent = Json(before), operation = row.GetProperty("Operation").GetString()!;
            string variant = row.GetProperty("Variant").GetString()!;
            string key = variant + "/" + row.GetProperty("RemoveOnDiscard").GetBoolean();
            groups[key] = groups.GetValueOrDefault(key) + 1;
            CardInstanceState predicted;
            switch (operation)
            {
                case "Permanent":
                    var permanent = row.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!;
                    Require(CardExhaustModel.PermanentUpgradeError(before, permanent) == null, "Permanent Holdover remains rejected.");
                    predicted = before.WithPermanentUpgrade(permanent, false); break;
                case "TemporaryUpgrade":
                    predicted = before.WithModifiers(before.Permanent, UnitModifierModel.Add(before.Temporary,
                        row.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!)); break;
                case "TemporaryTrait":
                    var d = before.MaskDescriptor!;
                    predicted = before.WithMaskDescriptor(d.WithHoldover(d.BaseTraits,
                        d.TemporaryTraits.Concat(new[] { new CardTraitValue(CardRetainModel.Trait, CardRetainModel.Trait, 0, 0,
                            true, 0, holdoverPending: false) }).ToArray(), d.HoldoverUpgradePending)); break;
                case "Replacement":
                    var descriptor = before.MaskDescriptor!;
                    predicted = before.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                        descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft,
                        descriptor.TemporaryTraits, descriptor.PermanentReplacements.Concat(new[] {
                            new CardTraitReplacement("CardTraitIgnoreArmor", CardRetainModel.Trait, CardRetainModel.Trait) }).ToArray(),
                        descriptor.SourceTraits, descriptor.KillTriggers, descriptor.HoldoverUpgradePending));
                    predicted = CardRetainModel.RefreshUpgradeTrait(predicted); break;
                case "DiscardCallback":
                    predicted = CardRetainModel.Discarded(before, row.GetProperty("WasPlayed").GetBoolean(), false,
                        row.GetProperty("Recast").GetBoolean());
                    if (Pending(actual)) positive++; break;
                case "Query": predicted = before; Require(Pending(actual), "Query lost the cached pending state."); cached++; break;
                case "Refresh":
                    predicted = CardRetainModel.RefreshUpgradeTrait(before);
                    Require(Pending(actual) == (variant is "Permanent" or "TemporaryTrait" or "BaseReplacement"),
                        "Native rebuild did not distinguish persistent from upgrade-owned trait state."); rebuilt++; break;
                case "Reset": predicted = CardRetainModel.Reset(before); break;
                case "DiscardCleanup":
                    predicted = before.WithModifiers(before.Permanent, before.Temporary.OnDiscard());
                    if (row.GetProperty("RemoveOnDiscard").GetBoolean() && variant is "TemporaryUpgrade" or "TemporaryReplacement")
                    { Require(!CardBranchMaskModel.Resolve(actual).Traits.Contains(CardRetainModel.Trait), "Remove-on-discard left an active Holdover trait."); removed++; } break;
                case "Clone":
                    var creation = row.GetProperty("Creation").Deserialize<CardCreationRule>()!;
                    var context = new CombatContext(new CardCycleState([], [], [], UnityRng.Seed(1), 0, []), UnityRng.Seed(2),
                        0, 78, 10, cardInstances: [before], cardRegistry: [before]);
                    string original = Json(context);
                    var clone = CardGenerationModel.CloneDetached(context, creation, before.InstanceId);
                    Require(clone.Supported, clone.UnsupportedReason ?? "Holdover clone rejected.");
                    predicted = clone.Context!.FindCard(78)!;
                    Equal(clone.Context.FindCard(before.InstanceId), row.GetProperty("SourceAfter").Deserialize<CardInstanceState>()!, key + " native clone source");
                    Require(original == Json(context) && !Pending(actual), "Clone inherited pending state or mutated its model parent.");
                    clones++; break;
                default: throw new InvalidOperationException("Unknown native Holdover operation: " + operation);
            }
            Equal(predicted, actual, key + " " + operation);
            Require(parent == Json(before), "Branch operation mutated its source."); count++;
        }
        Require(count == 100 && groups.Count == 10 && groups.Values.All(value => value == 10) &&
            positive == 10 && cached == 10 && rebuilt == 10 && clones == 10 && removed == 2,
            "Incomplete native Holdover variant matrix.");
        Console.WriteLine($"HOLDOVER-VARIANTS PASS: {count} native operations, five variants, both discard lifetimes, " +
            "private-state/cache/rebuild checks, ten independent clones, unchanged complete cards and live decision/RNG/frame.");
    }
    private static bool Pending(CardInstanceState card) => card.MaskDescriptor!.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits)
        .Any(trait => trait.RuntimeType == CardRetainModel.Trait && trait.HoldoverPending == true) || card.MaskDescriptor.HoldoverUpgradePending == true;
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal<T>(T left, T right, string label) => Require(ModelJson.Difference(Json(left), Json(right)) is not string error, label + ": " + ModelJson.Difference(Json(left), Json(right)));
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
