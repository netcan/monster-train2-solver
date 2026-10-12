using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelicMagicPowerChecks
{
    internal static void Run()
    {
        CardMaskDescriptor Descriptor(bool? canScale, bool stronger = false)
        {
            var definition = new CardMaskDefinition("spell", "Spell", "Common", false, false, false,
                [], [], [], [], null, false, 0, 0, false, false, false, canScale);
            CardTraitValue[] traits = stronger
                ? [new("CardTraitStrongerMagicPower", "CardTraitStrongerMagicPower", 0, 0, true, 0)]
                : [];
            return new CardMaskDescriptor(definition, 1, traits, []);
        }
        CardInstanceState Card(int id, bool? canScale, bool stronger = false) => new(id, "spell",
            CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: Descriptor(canScale, stronger));
        var modifier = new RelicMagicPowerModifier(0, 15);
        var relic = new CombatRelicState("magic", "SyntheticMagicPower",
            [RelicModel.ModifyCardMagicalPower], magicPowerModifiers: [modifier]);
        Require(RelicModel.Validate([relic]) == null, "Unconditional native magic-power relic was rejected.");

        CardInstanceState source = Card(1, true, stronger: true);
        string parent = JsonSerializer.Serialize(source);
        RelicCardModifierResult applied = RelicCardModifierModel.Apply(source, [relic], resetTemporary: false);
        Require(applied.Supported && applied.Card!.Temporary.Offsets.Damage == 75 && applied.Card.Temporary.Offsets.Heal == 75,
            "Relic magic power did not apply its trait-scaled damage and healing offsets.");
        Require(JsonSerializer.Serialize(source) == parent, "Relic magic-power application mutated its source card.");

        RelicCardModifierResult skipped = RelicCardModifierModel.Apply(Card(2, false), [relic], resetTemporary: false);
        Require(skipped.Supported && skipped.Card!.Temporary.Offsets.Damage == 0 && skipped.Card.Temporary.Offsets.Heal == 0,
            "A card rejected by native CanScaleWithMagicPower received relic offsets.");
        RelicCardModifierResult unknown = RelicCardModifierModel.Apply(Card(3, null), [relic], resetTemporary: false);
        Require(!unknown.Supported && unknown.UnsupportedReason!.Contains("eligibility", StringComparison.Ordinal),
            "Missing native magic-power eligibility was silently accepted.");

        var conditional = new CombatRelicState("conditional", "ConditionalMagicPower",
            [RelicModel.ModifyCardMagicalPower], magicPowerModifiers: [new RelicMagicPowerModifier(0, 15,
                [new RelicConditionState(new("CardsPlayed", "ThisTurn"), false, 1, 1, true)])]);
        Require(RelicModel.Validate([conditional])?.Contains("branch-aware room-power refresh", StringComparison.Ordinal) == true,
            "Conditional room magic power was accepted without dynamic branch refresh.");

        UnityRng rng = UnityRng.Seed(551);
        CardInstanceState registryCard = Card(10, true);
        var cards = new CardCycleState([], [], [], rng, 0, []);
        var context = new CombatContext(cards, rng, 0, 11, 10, cardInstances: [registryCard],
            cardRegistry: [registryCard], relics: [relic]);
        var creation = new CardCreationRule("spell", CardModifiers.Empty(), null, [], maskDescriptor: Descriptor(true, true));
        CardGenerationResult generated = CardGenerationModel.Apply(context,
            new CardGenerationRule("HandPile", 1, [creation]));
        CardInstanceState? created = generated.Context?.FindCard(11);
        Require(generated.Supported && created != null && created.Temporary.Offsets.Damage == 75 && created.Temporary.Offsets.Heal == 75,
            "Generated cards did not receive native relic magic-power modifiers.");
        Require(context.FindCard(11) == null && context.Relics![0].MagicPowerModifiers!.Count == 1,
            "Generated magic-power application changed its parent context or lost its relic descriptor.");
        Console.WriteLine("RELIC-MAGIC-POWER-CHECKS PASS: native eligibility, trait multiplier, signed temporary offsets, unsupported conditions, generated cards and immutable input.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
