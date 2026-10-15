using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalOpeningDrawChecks
{
    private static readonly (string Id, string Key, string Type, string Filter, bool Expand, string Upgrade, int Cost, int XCost)[] Owners = [
        ("7a1d5b5d-cd08-4935-b629-d3d441905100", "RandomEquipmentCardFirstTurn", RelicOpeningDrawModel.Cascade, "Equipment", true, "96560d71-8b2d-490a-8429-21e79cfcccde", -99, 0),
        ("d245f9d5-e966-4a8e-a125-4aa5b48f1e7c", "RandomTrainRoomCardFirstTurn", RelicOpeningDrawModel.Cascade, "TrainRoomAttachment", true, "96560d71-8b2d-490a-8429-21e79cfcccde", -99, 0),
        ("69d88251-cfed-4fd0-a355-cba2e10e09d4", "DrawSpellOnDeployment", RelicOpeningDrawModel.Draw, "Spell", false, "8763bf5e-cbac-41a8-b04f-8522f4b3136e", -1, 1) ];
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-opening-draw-relics" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" && root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0), "Opening draw battle failed acceptance.");
        var natural = Owners.ToDictionary(owner => owner.Id, _ => 0); int fallback = 0, full = 0, count = 0;
        foreach (var row in root.GetProperty("RelicOpeningDraw").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean(), "Unfinished native pre-opening callback.");
            string ownerId = row.GetProperty("OwnerId").GetString()!, phase = row.GetProperty("Phase").GetString()!;
            var before = row.GetProperty("Before").Deserialize<CombatContext>()!; var actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            var expected = Owners.Single(item => item.Id == ownerId); var owner = before.Relics!.Single(item => item.DataId == ownerId);
            var effect = owner.ResourceEffects!.Single(); var rule = effect.OpeningDraw!;
            Require(owner.AssetKey == expected.Key && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual([expected.Type]) && effect.EffectIndex == 0 && effect.ParamInt == 1 && effect.ParamInt2 == 0 &&
                effect.Conditions.Count == 0 && !effect.CanApplyInPreviewMode && rule.ExpandDraw == expected.Expand &&
                rule.FirstMask?.CardType == expected.Filter && rule.Upgrade?.DataId == expected.Upgrade &&
                rule.Upgrade.Stats.Cost == expected.Cost && rule.Upgrade.Stats.XCost == expected.XCost,
                "Original opening draw filter/count/temporary upgrade differs: " + expected.Key);
            if (expected.Type == RelicOpeningDrawModel.Cascade)
                Require(rule.FirstMask!.Traits.Excluded.SequenceEqual(["CardTraitDrawInDeploymentPhase"]) &&
                    rule.SecondMask?.CardType == expected.Filter && rule.SecondMask.Traits.Excluded.Count == 0,
                    "Original deployment-excluding primary or deployment-permitting fallback changed.");
            else Require(rule.FirstMask!.Effects.Excluded.Count == 8 && rule.FirstMask.Effects.Excluded.Contains("CardEffectDamage"), "Original non-damage spell filter changed.");
            bool Eligible(CardToken token) => CardUpgradeMaskModel.FilterCard(rule.FirstMask!, CardBranchMaskModel.Resolve(before.FindCard(token.InstanceId)!));
            var primary = before.Cards.Draw.Where(Eligible).ToArray();
            var result = RelicOpeningDrawModel.ApplyOne(before, ownerId, 0);
            Require(result.Supported, "Opening draw consumer unsupported: " + result.UnsupportedReason);
            Equal(result.Context, actual, "native callback");
            Require(result.Value == 1 && actual.Cards.Hand.Count == before.Cards.Hand.Count + 1 && !actual.Cards.Rng.Equals(before.Cards.Rng) &&
                actual.Cards.DrawModifier == before.Cards.DrawModifier + (expected.Expand ? 1 : 0), "Missing positive native draw/RNG/modifier witness.");
            var added = actual.Cards.Hand.Where(card => before.Cards.Hand.All(previous => previous.InstanceId != card.InstanceId)).Single();
            Require(actual.FindCard(added.InstanceId)!.Temporary.Upgrades.Any(upgrade => upgrade.DataId == expected.Upgrade) &&
                actual.FindCard(added.InstanceId)!.Permanent.Upgrades.All(upgrade => upgrade.DataId != expected.Upgrade), "Draw upgrade was lost or made permanent.");
            if (phase == "natural")
            {
                Require(primary.Length > 0 && primary.Any(token => token.InstanceId == added.InstanceId), "Natural pre-opening path did not use the primary filter."); natural[ownerId]++;
            }
            else
            {
                Require(phase == "fallback" && expected.Type == RelicOpeningDrawModel.Cascade && primary.Length == 0 &&
                    (rule.SecondMask == null || CardUpgradeMaskModel.FilterCard(rule.SecondMask, CardBranchMaskModel.Resolve(before.FindCard(added.InstanceId)!))),
                    "Controlled native fallback did not use the original secondary filter."); fallback++;
            }
            var skipped = RelicOpeningDrawModel.ApplyOne(before, ownerId, 0, preview: true);
            Require(skipped.Supported && ReferenceEquals(skipped.Context, before), "Preview consumed opening draw state.");
            var laterTurn = RelicOpeningDrawModel.ApplyOne(before, ownerId, 0, placement: false);
            Require(laterTurn.Supported && ReferenceEquals(laterTurn.Context, before), "Manager's non-deployment gate consumed pre-opening state.");
            var absent = before.WithRelics([]); var removed = RelicOpeningDrawModel.Apply(absent);
            Require(removed.Supported && ReferenceEquals(removed.Context, absent) && removed.Value == 0, "Removed owners still draw.");
            if (phase == "natural")
            {
                var keep = primary[0]; var tokens = before.Cards.Hand.Concat(before.Cards.Draw).Concat(before.Cards.Discard)
                    .Where(token => token.InstanceId != keep.InstanceId).Take(before.MaxHandSize).ToArray();
                if (tokens.Length == before.MaxHandSize)
                {
                    var ids = tokens.Select(token => token.InstanceId).ToHashSet(); var cycle = before.Cards;
                    var capped = before.WithCards(new(tokens, cycle.Draw.Where(token => !ids.Contains(token.InstanceId)).ToArray(),
                        cycle.Discard.Where(token => !ids.Contains(token.InstanceId)).ToArray(), cycle.Rng, cycle.DrawModifier, cycle.ExternalInteractions,
                        cycle.BonusDraw, cycle.NextDrawUpgrades, cycle.NextUpgradeReferenceId));
                    var blocked = RelicOpeningDrawModel.ApplyOne(capped, ownerId, 0);
                    var remaining = capped.Cards.Draw.Where(Eligible).ToArray();
                    var chosen = remaining[capped.Cards.Rng.Range(0, remaining.Length).Value];
                    Require(blocked.Supported && blocked.Value == 1 && blocked.Context!.Cards.Hand.Count == before.MaxHandSize &&
                        blocked.Context.Cards.Draw.Any(token => token.InstanceId == chosen.InstanceId) && !blocked.Context.Cards.Rng.Equals(cycle.Rng) &&
                        blocked.Context.FindCard(chosen.InstanceId)!.Temporary.Upgrades.Any(upgrade => upgrade.DataId == expected.Upgrade),
                        "Full-hand control skipped native selection/upgrade or moved the selected card."); full++;
                }
            }
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 16, _ => { var branch = RelicOpeningDrawModel.ApplyOne(before, ownerId, 0); Require(branch.Supported, "Parallel opening draw unsupported."); Equal(branch.Context, actual, "parallel callback"); });
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Opening draw mutated parent state."); count++;
        }
        Require(natural.Values.All(value => value == 1) && fallback == 2 && full == 3 && count == 5, "Missing primary/fallback/full-hand original opening evidence.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-OPENING-DRAW-NATIVE PASS: three unchanged owners, {count} exact consumers ({fallback} controlled native fallbacks), numeric temporary upgrades, {full} full-hand controls, CardDraw RNG, modifier/preview/removed-owner controls and 16 parallel branches; complete independent policy replay.");
    }
    private static void Equal<T>(T predicted, T actual, string label) => Require(JsonSerializer.Serialize(predicted, ModelJson.Options) == JsonSerializer.Serialize(actual, ModelJson.Options),
        "Opening draw " + label + " differs: " + ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options)));
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
