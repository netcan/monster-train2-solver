using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoonSpellThresholdCompositionChecks
{
    private const string DataId = "76166da7-d9aa-4c68-9a78-64914ec49f81";
    internal static RelicConditionState Owner(CombatContext context)
    {
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == DataId);
        Require(owner.AssetKey == "IncreaseMagicPowerOnSpellsPlayed" && owner.IsCovenant == false &&
            owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual([RelicModel.ModifyCardMagicPowerOnCardPlayed]) &&
            owner.CardPlayMagicPowerModifiers?.Count == 1 && RelicModel.Validate(context) == null,
            "Missing unchanged original spell-threshold owner.");
        RelicCardPlayMagicPowerModifier effect = owner.CardPlayMagicPowerModifiers![0];
        Require(effect.EffectIndex == 0 && effect.PowerModifier == 1 && effect.RequiredTraits.Count == 0 &&
            effect.ConditionCount == 1 && effect.Conditions.Count == 1, "Original third-spell payload changed.");
        RelicConditionState condition = effect.Conditions[0];
        Require(condition.Query.Type == "AnyCardPlayed" && condition.Query.Duration == "ThisTurn" &&
            !condition.TrackTriggerCount && condition.Comparator == 6 && condition.Value == 3 && !condition.AllowMultiple &&
            context.CardInstances!.All(card => condition.Query.TypeMatchDataIds!.Contains(card.DataId) ==
                (card.MaskDescriptor!.Definition.CardType == "Spell")), "Original spell threshold/type membership changed.");
        return condition;
    }

    internal static void Component(FixtureValue root)
    {
        Require(root.GetProperty("ThresholdRelicControls").GetBoolean() && root.GetProperty("SpellAdvanceControls").GetBoolean(),
            "Missing separate third-spell moon component controls.");
        RoomMoonSpellUpgradeChecks.Native(root);
        int preserved = 0;
        foreach (FixtureValue row in root.GetProperty("Rows").EnumerateArray())
        {
            CombatContext before = row.GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!.Context!;
            CombatContext after = row.GetProperty("Actual").GetProperty("Train").Deserialize<TrainCombatState>()!.Context!;
            Require(Json(Owner(before)) == Json(Owner(after)), "Moon refresh changed the spell-count condition history.");
            foreach (CardInstanceState card in before.CardInstances!)
            {
                Require(Json(card.Temporary.Offsets) == Json(after.FindCard(card.InstanceId)!.Temporary.Offsets),
                    "Moon phase/room refresh changed fixed third-spell offsets.");
                preserved++;
            }
        }
        Guards(root.GetProperty("Rows")[0].GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!);
        Console.WriteLine($"MOON-SPELL-THRESHOLD-COMPONENT PASS: {preserved} fixed offsets and complete condition histories preserved; malformed signatures rejected.");
    }

    private static void Guards(TrainCombatState source)
    {
        string parent = Json(source);
        Action<JsonNode, JsonNode, JsonNode>[] mutations = [
            (owner, effect, condition) => owner["DataId"] = "unknown",
            (owner, effect, condition) => owner["AssetKey"] = "unknown",
            (owner, effect, condition) => owner["IsCovenant"] = true,
            (owner, effect, condition) => owner["DisallowedInPlacementPhase"] = true,
            (owner, effect, condition) => owner["EffectTypes"]!.AsArray().Add("RelicEffectModifyCardMagicPowerOnCardPlayed"),
            (owner, effect, condition) => effect["EffectIndex"] = 1,
            (owner, effect, condition) => effect["PowerModifier"] = 2,
            (owner, effect, condition) => effect["RequiredTraits"]!.AsArray().Add("CardTraitExhaustState"),
            (owner, effect, condition) => effect["Conditions"]!.AsArray().Add(condition.DeepClone()),
            (owner, effect, condition) => condition["Value"] = 4,
            (owner, effect, condition) => condition["Comparator"] = 2,
            (owner, effect, condition) => condition["TrackTriggerCount"] = true,
            (owner, effect, condition) => condition["AllowMultiple"] = true,
            (owner, effect, condition) => condition["Query"]!["Type"] = "SubtypeInDeck",
            (owner, effect, condition) => condition["Query"]!["Duration"] = "ThisBattle",
            (owner, effect, condition) => condition["Query"]!["TargetTeam"] = "Heroes",
            (owner, effect, condition) => condition["Query"]!["SubtypeIsNone"] = false,
            (owner, effect, condition) => condition["Query"]!["SourceRawCost"] = 1,
            (owner, effect, condition) => condition["Query"]!["VariableCost"] = true,
            (owner, effect, condition) => condition["Query"]!["CurrentCostCardModifiersOnly"] = true,
            (owner, effect, condition) => condition["Query"]!["CurrentCostSourceIsSpell"] = true,
            (owner, effect, condition) => condition["Query"]!["SpecificCardDataId"] = "unknown",
            (owner, effect, condition) => condition["Query"]!["StatusIds"] = new JsonArray("armor"),
            (owner, effect, condition) => condition["Query"]!["TypeMatchDataIds"] = new JsonArray("unknown")
        ];
        foreach (var mutate in mutations)
        {
            JsonNode malformed = JsonNode.Parse(parent)!;
            JsonNode owner = malformed["Context"]!["Relics"]!.AsArray().Single(node => node!["DataId"]!.GetValue<string>() == DataId)!;
            JsonNode effect = owner["CardPlayMagicPowerModifiers"]![0]!, condition = effect["Conditions"]![0]!;
            mutate(owner, effect, condition);
            TrainCombatState unknown = JsonSerializer.Deserialize<TrainCombatState>(malformed.ToJsonString(), ModelJson.Options)!;
            Require(!RoomMoonPhaseModel.Refresh(unknown, false).Supported, "Malformed third-spell composition bypassed the moon guard.");
        }
        Require(Json(source) == parent, "Third-spell negative controls mutated their parent.");
    }

    internal static void CardPlays(FixtureValue root, bool requireFivefold = false)
    {
        Require(root.GetProperty("OriginalMoonCardUpgradesThresholdRelic").GetBoolean(), "Missing third-spell composition battle flag.");
        int fired = 0, suppressed = 0, increments = 0, fivefold = 0, excluded = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState prior = row.GetProperty("Before").Deserialize<BattleTurnState>()!, next = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext before = prior.Spawn.Train.Context!, after = next.Spawn.Train.Context!;
            RelicConditionState condition = Owner(before), actual = Owner(after);
            bool activated = !condition.Triggered && actual.Triggered;
            int spellCount = after.CardInstances!.Where(card => card.MaskDescriptor!.Definition.CardType == "Spell")
                .Sum(card => after.Statistics!.Value(card.InstanceId, "TimesPlayed", "ThisTurn"));
            Require(activated == (!condition.Triggered && spellCount >= 3), "Third-spell callback did not follow the native once-per-turn threshold.");
            if (activated)
            {
                fired++;
                Require(actual.ValueAtLastTrigger == spellCount && actual.DurationTriggerCount == 1, "Spell-count trigger history differs.");
            }
            else if (condition.Triggered) suppressed++;
            foreach (CardInstanceState card in before.CardInstances!)
            {
                CardInstanceState? updated = after.FindCard(card.InstanceId);
                if (updated == null) continue;
                bool scalable = updated.MaskDescriptor!.Definition.CardType == "Spell" && updated.MaskDescriptor.Definition.CanScaleWithMagicPower == true;
                int expected = activated && scalable ? CardMagicPowerModel.Multiplier(updated) : 0;
                Require(updated.Temporary.Offsets.Damage - card.Temporary.Offsets.Damage == expected &&
                    updated.Temporary.Offsets.Heal - card.Temporary.Offsets.Heal == expected,
                    "Third-spell fixed offsets were lost, reapplied or scaled at the wrong boundary.");
                if (expected > 0) { increments++; if (expected == 5) fivefold++; }
                if (activated && !scalable) excluded++;
            }
        }
        Require(fired > 0 && suppressed > 0 && increments > 0 && excluded > 0, "Missing positive, once-per-turn or excluded-card witnesses.");
        Require(!requireFivefold || fivefold > 0, "Missing actual fivefold native third-spell offsets.");
        Console.WriteLine($"MOON-SPELL-THRESHOLD-CARD-PLAYS PASS: triggers={fired} suppressed={suppressed} increments={increments} fivefold={fivefold} exclusions={excluded}.");
    }
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
