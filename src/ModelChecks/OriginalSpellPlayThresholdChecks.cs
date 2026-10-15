using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSpellPlayThresholdChecks
{
    private const string MagicId = "76166da7-d9aa-4c68-9a78-64914ec49f81";
    private const string DrawId = "4fbb0cca-26cd-4510-af02-66694c7edfd3";
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-spell-play-threshold-relics" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Spell threshold battle failed acceptance.");
        int magicTriggers = 0, drawTriggers = 0, negativeTraits = 0, changedSpells = 0, laterSuppressed = 0;
        bool deploymentMagic = false;
        var spellCounts = new Dictionary<int, int>();
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!; var actual = after.Spawn.Train.Context!;
            var played = context.FindCard(row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32())!;
            bool spell = played.MaskDescriptor!.Definition.CardType == "Spell";
            int turn = before.Spawn.Turn;
            spellCounts.TryGetValue(turn, out int count); if (spell) spellCounts[turn] = ++count;
            var magic = context.Relics!.Single(owner => owner.DataId == MagicId);
            var draw = context.Relics!.Single(owner => owner.DataId == DrawId);
            Require(magic.AssetKey == "IncreaseMagicPowerOnSpellsPlayed" && magic.DisallowedInPlacementPhase == false &&
                magic.EffectTypes.SequenceEqual([RelicModel.ModifyCardMagicPowerOnCardPlayed]) &&
                draw.AssetKey == "DrawCardWhenConsumePlayed" && draw.DisallowedInPlacementPhase == false &&
                draw.EffectTypes.SequenceEqual([RelicModel.CardDrawOnCardPlay]), "Original owner identity changed.");
            var m = magic.CardPlayMagicPowerModifiers!.Single(); var d = draw.CardDrawOnCardPlayModifiers!.Single();
            var mc = m.Conditions.Single(); var dc = d.Conditions.Single();
            Require(m.EffectIndex == 0 && m.PowerModifier == 1 && m.RequiredTraits.Count == 0 &&
                mc.Query.Type == "AnyCardPlayed" && mc.Query.Duration == "ThisTurn" && !mc.TrackTriggerCount &&
                mc.Comparator == 6 && mc.Value == 3 && !mc.AllowMultiple &&
                d.EffectIndex == 0 && d.Count == 1 && d.CardType == "Spell" && d.RequiredSubtype == null &&
                d.RequiredTraits.SequenceEqual(["CardTraitExhaustState"]) && dc.Query.Type == "SubtypeInDeck" &&
                dc.Query.Duration == "ThisTurn" && dc.TrackTriggerCount && dc.Comparator == 6 && dc.Value == 1 && !dc.AllowMultiple,
                "Original threshold/trait payload changed.");
            Require(context.CardInstances!.All(card => mc.Query.TypeMatchDataIds!.Contains(card.DataId) ==
                (card.MaskDescriptor!.Definition.CardType == "Spell")), "Threshold card-type query mask differs.");
            var ma = actual.Relics!.Single(owner => owner.DataId == MagicId).CardPlayMagicPowerModifiers!.Single().Conditions.Single();
            var da = actual.Relics!.Single(owner => owner.DataId == DrawId).CardDrawOnCardPlayModifiers!.Single().Conditions.Single();
            bool magicFired = !mc.Triggered && ma.Triggered;
            Require(magicFired == (!mc.Triggered && count >= 3), "Magic did not fire exactly at the third spell.");
            if (magicFired)
            {
                magicTriggers++; deploymentMagic |= turn == 0;
                Require(ma.ValueAtLastTrigger == count && ma.DurationTriggerCount == 1, "Magic condition did not record its spell count.");
                foreach (var card in context.CardInstances!.Where(card => card.MaskDescriptor!.Definition.CardType == "Spell" &&
                    card.MaskDescriptor.Definition.CanScaleWithMagicPower == true))
                {
                    var updated = actual.FindCard(card.InstanceId);
                    if (updated != null && updated.Temporary.Offsets.Damage - card.Temporary.Offsets.Damage == CardMagicPowerModel.Multiplier(card) &&
                        updated.Temporary.Offsets.Heal - card.Temporary.Offsets.Heal == CardMagicPowerModel.Multiplier(card)) changedSpells++;
                }
            }
            else if (mc.Triggered) laterSuppressed++;
            bool exhaust = CardTraitRefreshModel.Ensure(CardBranchMaskModel.OwnedState(played).Traits)
                .Composition.CombinedTraits!.Any(trait => trait.RuntimeType == "CardTraitExhaustState");
            bool drawFired = !dc.Triggered && da.Triggered;
            Require(drawFired == (!dc.Triggered && spell && exhaust), "Consume draw ignored its type, trait or once-per-turn condition.");
            if (drawFired)
            {
                drawTriggers++;
                int Drawn(CombatContext state) => state.Statistics!.Values.Where(value => value.Type == "TimesDrawn" && value.Duration == "ThisTurn").Sum(value => value.Value);
                Require(Drawn(actual) == Drawn(context) + 1 && da.DurationTriggerCount == 1,
                    "Consume callback did not draw exactly one real card.");
            }
            if (spell && !exhaust && !dc.Triggered) negativeTraits++;
        }
        Require(magicTriggers >= 2 && deploymentMagic && drawTriggers > 0 && negativeTraits > 0 && changedSpells > 0 && laterSuppressed > 0,
            "Missing actual threshold, deployment, Consume, non-Consume or once-per-turn witnesses.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-SPELL-PLAY-THRESHOLDS-NATIVE PASS: {magicTriggers} third-spell activations including deployment, {drawTriggers} Consume draws, {negativeTraits} non-Consume controls, {changedSpells} spell increments and exact independent policy replay.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
