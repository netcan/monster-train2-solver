using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCorruptedAngelsChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 246 && root.GetProperty("OriginalCorruptedAngelsPrepared").GetBoolean() &&
            root.GetProperty("ModifierScenario").GetString() == "original-corrupted-angels" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete Corrupted Angels witness.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalCorruptedAngelsCaptureFailures",
            "OriginalCorruptedAngelsMismatches", "OriginalCorruptedAngelsUnsupported", "OriginalCorruptedAngelsPending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Nonzero " + field);
        var controls = new HashSet<string>(); var primaryTypes = new HashSet<string>();
        int total = 0, naturalPositive = 0, recursiveDepth = 0, attempts = 0;
        foreach (FixtureValue row in root.GetProperty("RelicCorruptedAngelsRecords").EnumerateArray())
        {
            total++;
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!, actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatContext source = before.Context!, target = actual.Context!;
            BattlePlayRules definitions = row.GetProperty("Definitions").Deserialize<BattlePlayRules>()!;
            int cardId = row.GetProperty("CardId").GetInt32(), depth = row.GetProperty("Depth").GetInt32();
            string role = row.GetProperty("Role").GetString()!;
            bool returned = row.GetProperty("Returned").GetBoolean();
            Require(row.GetProperty("Completed").GetBoolean(), "Pending native callback.");
            CombatRelicState owner = source.Relics!.Single(item => item.DataId == RelicCorruptedAngelsModel.DataId);
            RelicResourceEffect effect = owner.ResourceEffects!.Single();
            Require(RelicModel.Validate([owner]) == null && RelicCorruptedAngelsModel.Validate(owner, effect) == null, "Original owner or filters changed.");
            void Check()
            {
                var result = RelicCorruptedAngelsModel.Apply(source, cardId, out bool applied, definitions);
                Require(result.Supported && applied == returned, result.UnsupportedReason ?? "Callback bool differs.");
                Equal(CardSpellModel.WithContext(before, result.Context), actual, "complete train callback");
            }
            Check();
            if (depth == 0 && role is "draw-priority" or "recursive-candidate")
            {
                string parent = JsonSerializer.Serialize(before, ModelJson.Options);
                Parallel.For(0, 16, _ => Check());
                Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Callback mutated its parent branch.");
            }
            Require(source.Cards.Rng.Equals(target.Cards.Rng) && source.BattleRng.Equals(target.BattleRng) &&
                source.NextCardId == target.NextCardId && source.Resources!.VisibleDeckCount == target.Resources!.VisibleDeckCount,
                "Specific extra draws consumed RNG, allocated cards or changed permanent deck count.");
            Equal(source.Statistics!.DeckCards, target.Statistics!.DeckCards, "permanent deck membership");
            var calls = row.GetProperty("Attempts").EnumerateArray().ToArray(); attempts += calls.Length;
            Require(calls.All(call => call.GetProperty("Completed").GetBoolean()) && calls.Length <= 1,
                "Native one-card match count retried a rejected draw or left an attempt pending.");
            if (role == "natural" && returned)
            {
                naturalPositive++;
                primaryTypes.Add(source.FindCard(cardId)!.MaskDescriptor!.Definition.CardType);
            }
            if (role == "recursive-candidate") recursiveDepth = Math.Max(recursiveDepth, depth);
            if (depth != 0 || role == "natural") continue;
            controls.Add(role);
            if (role == "non-primary")
            {
                Require(!returned && calls.Length == 0, "Non-primary card applied the effect."); Equal(source, target, "non-primary control");
            }
            else
            {
                Require(returned && target.QueryFrame!.Energy == Math.Min(source.EnergyState!.Maximum,
                    source.QueryFrame!.Energy!.Value + (role == "recursive-candidate" ? 3 : 1)), "Energy follows all nested draw attempts, including refusals.");
                if (role == "draw-priority" || role == "discard-fallback")
                {
                    int id = calls.Single().GetProperty("CardId").GetInt32();
                    var pile = role == "draw-priority" ? source.Cards.Draw : source.Cards.Discard;
                    Require(id == pile.Last().InstanceId && calls[0].GetProperty("Returned").GetBoolean() &&
                        target.Cards.Hand.Count == source.Cards.Hand.Count + 1, "Wrong pile priority, reverse order or card movement.");
                }
                if (role == "no-eligible" || role == "full-hand" || role == "energy-cap" || role == "null-filter")
                {
                    Equal(source.Cards, target.Cards, "no-movement control"); Equal(source.Statistics, target.Statistics, "no-draw statistics");
                    Require(role == "no-eligible" ? calls.Length == 0 : calls.Length == 1 && !calls[0].GetProperty("Returned").GetBoolean(),
                        "Missing native refused-draw/no-candidate attempt evidence.");
                }
                if (role == "recursive-candidate") Require(target.Cards.Hand.Count == source.Cards.Hand.Count + 3 && calls.Length == 1,
                    "Overlapping original filters did not produce the three-card nested draw chain.");
            }
            CombatContext ablated = source.WithRelics(source.Relics.Where(item => item.DataId != owner.DataId).ToArray());
            var inert = RelicCorruptedAngelsModel.Apply(ablated, cardId, out bool ablatedApplied, definitions);
            Require(inert.Supported && !ablatedApplied && ReferenceEquals(inert.Context, ablated), "Absent owner still applied draw/energy.");
            var unknown = new CombatRelicState("changed-owner", owner.AssetKey, owner.EffectTypes, isCovenant: false,
                disallowedInPlacementPhase: false, resourceEffects: [effect]);
            Require(RelicCorruptedAngelsModel.Validate(unknown, effect) != null, "Unknown original owner identity was accepted.");
            var missing = new RelicResourceEffect(0, RelicCorruptedAngelsModel.Effect, 1, 1, 1, false, paramMinInt: 3,
                drawAndEnergy: new(null, effect.DrawAndEnergy!.DrawFilter, []));
            Require(RelicCorruptedAngelsModel.Validate(owner.WithResourceEffects([missing]), missing) != null, "Missing original filter was accepted.");
            var changedPool = new RelicResourceEffect(0, RelicCorruptedAngelsModel.Effect, 1, 1, 1, false, paramMinInt: 3,
                drawAndEnergy: new(effect.DrawAndEnergy.TriggerFilter, new(assetKey: "ExcludeScourgeAndBlight",
                    rarities: new(requiredOperator: 1)), []));
            Require(RelicCorruptedAngelsModel.Validate(owner.WithResourceEffects([changedPool]), changedPool) != null, "Changed original exclusion pool was accepted.");
        }
        Require(controls.SetEquals(new[] { "draw-priority", "discard-fallback", "no-eligible", "non-primary", "full-hand", "energy-cap",
            "null-filter", "recursive-candidate" }) && naturalPositive > 0 && primaryTypes.SetEquals(new[] { "Blight", "Junk" }) && recursiveDepth == 3,
            "Missing natural original Blight/Junk, nested filter overlap or isolated control roles.");
        CardCycleChecks.Native(root);
        Console.WriteLine($"ORIGINAL-CORRUPTED-ANGELS-NATIVE PASS: unchanged owner/two original masks, {total} complete callbacks/{naturalPositive} natural positives, {attempts} native draw attempts, eight controls, depth-three recursion, exact piles/statistics/energy/RNG and 16 immutable branches.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
