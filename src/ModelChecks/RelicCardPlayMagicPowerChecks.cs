using MonsterTrain2Poju.Model;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;

internal static class RelicCardPlayMagicPowerChecks
{
    private const string RelicId = "a4c87eb7-1c4e-4f63-af08-79f910e5a2e8";
    private const string RelicKey = "MagicPowerWhenCardConsumed";
    private const string CarrierTrait = "CardTraitExhaustState";

    internal static void Run()
    {
        CardMaskDescriptor Descriptor(string id, string type, bool? canScale, params string[] traitNames)
        {
            var definition = new CardMaskDefinition(id, type, "Common", false, false, false,
                [], [], [], [], null, false, 0, 0, false, false, false, canScale);
            CardTraitValue[] traits = traitNames.Select(name => new CardTraitValue(name, name, 0, 0, true, 0)).ToArray();
            return new CardMaskDescriptor(definition, 1, traits, []);
        }

        CardInstanceState Card(int id, string key, string type, bool? canScale, params string[] traits) =>
            new(id, key, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
                maskDescriptor: Descriptor(key, type, canScale, traits));

        var relic = new CombatRelicState("original", "MagicPowerWhenCardConsumed",
            [RelicModel.ModifyCardMagicPowerOnCardPlayed], cardPlayMagicPowerModifiers: [
                new RelicCardPlayMagicPowerModifier(0, 2, ["CardTraitCarrier"])
            ]);
        Require(RelicModel.Validate([relic]) == null, "Unconditional native card-play magic-power relic was rejected.");

        CardInstanceState carrier = Card(1, "carrier", "Monster", null, "CardTraitCarrier");
        CardInstanceState strongerSpell = Card(2, "stronger", "Spell", true, "CardTraitStrongerMagicPower");
        CardInstanceState normalSpell = Card(3, "normal", "Spell", true);
        CardInstanceState nonScalingSpell = Card(4, "fixed", "Spell", false);
        CardInstanceState unownedSpell = Card(5, "unowned", "Spell", true);
        UnityRng rng = UnityRng.Seed(903);
        var cards = new CardCycleState([], [], [], rng, 0, []);
        var context = new CombatContext(cards, rng, 0, 6, 10,
            cardInstances: [carrier, strongerSpell, normalSpell, nonScalingSpell],
            cardRegistry: [carrier, strongerSpell, normalSpell, nonScalingSpell, unownedSpell], relics: [relic], otherPiles: []);

        RelicCardPlayMagicPowerResult first = RelicCardPlayMagicPowerModel.Apply(context, carrier.InstanceId);
        Require(first.Supported && first.Dispatches.Count == 1 && first.Dispatches[0].Applied,
            "A played carrier with the required trait did not trigger the native relic.");
        Require(first.Context.FindCard(2)!.Temporary.Offsets.Damage == 10 && first.Context.FindCard(2)!.Temporary.Offsets.Heal == 10 &&
            first.Context.FindCard(3)!.Temporary.Offsets.Damage == 2 && first.Context.FindCard(3)!.Temporary.Offsets.Heal == 2,
            "The relic did not add trait-scaled damage and healing to each owned scalable spell.");
        Require(first.Context.FindCard(4)!.Temporary.Offsets.Damage == 0 && first.Context.FindCard(5)!.Temporary.Offsets.Damage == 0,
            "The relic modified a non-scaling spell or a card outside the owned-card list.");

        RelicCardPlayMagicPowerResult second = RelicCardPlayMagicPowerModel.Apply(first.Context, carrier.InstanceId);
        Require(second.Supported && second.Context.FindCard(2)!.Temporary.Offsets.Damage == 20 &&
            second.Context.FindCard(3)!.Temporary.Offsets.Heal == 4,
            "Repeated card plays did not accumulate the temporary magic-power offsets.");
        Require(context.FindCard(2)!.Temporary.Offsets.Damage == 0 && first.Context.FindCard(2)!.Temporary.Offsets.Damage == 10,
            "Applying the relic mutated a parent branch.");

        CardInstanceState rejectedCarrier = Card(6, "rejected-carrier", "Monster", null);
        CombatContext rejectedContext = context.WithCardInstances([rejectedCarrier, strongerSpell, normalSpell, nonScalingSpell]);
        RelicCardPlayMagicPowerResult rejected = RelicCardPlayMagicPowerModel.Apply(rejectedContext, rejectedCarrier.InstanceId);
        Require(rejected.Supported && rejected.Dispatches.Count == 0 && rejected.Context.FindCard(2)!.Temporary.Offsets.Damage == 0,
            "A played card without the required native trait triggered the relic.");

        CardInstanceState missingMask = new(7, "missing", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, []);
        CombatContext incomplete = context.WithCardInstances([carrier, strongerSpell, normalSpell, missingMask]);
        RelicCardPlayMagicPowerResult unsupported = RelicCardPlayMagicPowerModel.Apply(incomplete, carrier.InstanceId);
        Require(!unsupported.Supported && unsupported.UnsupportedReason!.Contains("metadata", StringComparison.Ordinal),
            "Missing owned-card metadata was silently treated as a card-play relic no-op.");

        var malformed = new CombatRelicState("bad", "BadCardPlayMagicPower",
            [RelicModel.ModifyCardMagicPowerOnCardPlayed]);
        Require(RelicModel.Validate([malformed])?.Contains("ordered native card-play", StringComparison.Ordinal) == true,
            "An uncaptured native card-play magic-power payload passed validation.");
        var threshold = new CombatRelicState("threshold", "IncreaseMagicPowerOnSpellsPlayed",
            [RelicModel.ModifyCardMagicPowerOnCardPlayed], cardPlayMagicPowerModifiers: [
                new(0, 1, conditions: [new(new("AnyCardPlayed", typeMatchDataIds: ["normal"]), false, 6, 3, false)])]);
        var counted = context.WithRelics([threshold]).WithOtherPiles([]).WithStatistics(BattleStatistics.Empty(deckCards: [3])
            .Increment(3, "TimesPlayed").Increment(3, "TimesPlayed").Increment(3, "TimesPlayed"));
        var deployment = RelicCardPlayMagicPowerModel.Apply(counted, 3, currentTurn: 0);
        var combat = RelicCardPlayMagicPowerModel.Apply(counted, 3, currentTurn: 1);
        var repeated = RelicCardPlayMagicPowerModel.Apply(combat.Context, 3, currentTurn: 1);
        Require(deployment.Supported && deployment.Dispatches.Count == 1 && combat.Supported && combat.Dispatches.Count == 1 &&
            repeated.Supported && repeated.Dispatches.Count == 0 &&
            RelicCardPlayMagicPowerModel.Apply(RelicConditionModel.EndDuration(combat.Context, "ThisTurn"), 3, currentTurn: 2).Dispatches.Count == 1,
            "Spell threshold failed deployment, once-per-turn or condition-reset controls: " + deployment.UnsupportedReason + "/" + combat.UnsupportedReason +
            " dispatches=" + deployment.Dispatches.Count + "/" + combat.Dispatches.Count + "/" + repeated.Dispatches.Count);
        Console.WriteLine("RELIC-CARD-PLAY-MAGIC-POWER-CHECKS PASS: required traits, owned scalable spells, trait multipliers, accumulation, immutable branches and explicit missing metadata.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 128 &&
            root.GetProperty("ModifierScenario").GetString() == "original-card-play-magic-power" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("CaptureFailures").GetInt32() == 0 &&
            root.GetProperty("Mismatches").GetInt32() == 0 && root.GetProperty("Unsupported").GetInt32() == 0 &&
            root.GetProperty("Pending").GetInt32() == 0,
            "Original card-play magic-power battle is incomplete or has native/model failures.");

        int matched = 0;
        int triggeredPlays = 0;
        int changedSpells = 0;
        foreach (FixtureValue entry in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatContext beforeContext = before.Spawn.Train.Context!;
            CombatContext actualContext = actual.Spawn.Train.Context!;
            CombatRelicState relic = beforeContext.Relics!.Single(item => item.DataId == RelicId);
            Require(relic.AssetKey == RelicKey &&
                relic.EffectTypes.SequenceEqual([RelicModel.ModifyCardMagicPowerOnCardPlayed]) &&
                relic.CardPlayMagicPowerModifiers?.Count == 1 &&
                relic.CardPlayMagicPowerModifiers[0].PowerModifier == 1 &&
                relic.CardPlayMagicPowerModifiers[0].RequiredTraits.SequenceEqual([CarrierTrait]) &&
                relic.CardPlayMagicPowerModifiers[0].Conditions.Count == 0,
                "The battle did not retain the unchanged original relic effect payload.");

            CardInstanceState played = beforeContext.CardInstances!.Single(card => card.InstanceId ==
                entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32());
            IReadOnlyList<CardTraitValue> playedTraits = CardTraitRefreshModel.Ensure(CardBranchMaskModel.OwnedState(played).Traits)
                .Composition.CombinedTraits!;
            bool triggered = relic.CardPlayMagicPowerModifiers[0].RequiredTraits.All(required =>
                playedTraits.Any(trait => trait.RuntimeType == required));
            if (triggered) triggeredPlays++;
            CardInstanceState[] spells = beforeContext.CardInstances.Where(card =>
                card.MaskDescriptor?.Definition.CardType == "Spell" && card.MaskDescriptor.Definition.CanScaleWithMagicPower == true).ToArray();
            Require(spells.Length > 0, "Native card-play effect could not find an owned spell.");
            foreach (CardInstanceState spell in beforeContext.CardInstances!.Where(card =>
                card.MaskDescriptor?.Definition.CardType == "Spell" && card.MaskDescriptor.Definition.CanScaleWithMagicPower == true))
            {
                CardInstanceState? updated = actualContext.FindCard(spell.InstanceId);
                if (updated == null) continue;
                int amount = triggered ? unchecked(relic.CardPlayMagicPowerModifiers[0].PowerModifier * CardMagicPowerModel.Multiplier(spell)) : 0;
                int damageDelta = unchecked(updated.Temporary.Offsets.Damage - spell.Temporary.Offsets.Damage);
                int healDelta = unchecked(updated.Temporary.Offsets.Heal - spell.Temporary.Offsets.Heal);
                Require(damageDelta == amount && healDelta == amount,
                    "Native card-play relic changed spell " + spell.InstanceId + " by unexpected magic-power offsets.");
                changedSpells++;
            }
            matched++;
        }
        Require(matched > 0 && triggeredPlays > 0 && changedSpells > 0,
            "The full battle did not observe a played-card effect on an owned scalable spell.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"NATIVE-CARD-PLAY-MAGIC-POWER-CHECKS PASS: {matched} played-card boundaries, {triggeredPlays} matching carrier plays and {changedSpells} spell offset updates; unchanged owner {RelicKey}.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
