using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSpecificCardDamageChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 2 && source.Spawn.Turn < 1) return null;
        if (index >= 3 && !source.Spawn.Train.Rooms.Any(room => room.Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0))) return null;
        return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == (index == 1 ? 1 : 0));
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalSpecificCardDamagePrepared").GetBoolean() && root.GetProperty("OriginalSpecificCardDamageQueriesUnchanged").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Dante's Inferno witness or deck restoration.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalSpecificCardDamageIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), queries = root.GetProperty("OriginalSpecificCardDamageQueries").EnumerateArray().ToArray(),
            samples = root.GetProperty("OriginalSpecificCardDamageSamples").EnumerateArray().ToArray();
        Require(ids.Length == 5 && actions.Length == 5 && queries.Length == 12 && samples.Length >= 2 &&
            queries.Select(query => $"{query.GetProperty("CardId").GetInt32()}/{query.GetProperty("DeckCount").GetInt32()}/{query.GetProperty("InputDamage").GetInt32()}").Distinct().Count() == 12,
            "Missing count/input/upgrade controls or positive tower casts.");
        foreach (FixtureValue query in queries)
        {
            CombatContext context = query.GetProperty("Context").Deserialize<CombatContext>()!;
            int id = query.GetProperty("CardId").GetInt32(), count = query.GetProperty("DeckCount").GetInt32(), input = query.GetProperty("InputDamage").GetInt32();
            string counted = query.GetProperty("CountedCardDataId").GetString()!;
            CardInstanceState card = context.FindCard(id)!; CardMaskDescriptor descriptor = card.MaskDescriptor!;
            CardSourceTrait source = descriptor.SourceTraits!.Single(trait => trait.Type == CardSpecificDamageModel.Trait);
            string parent = JsonSerializer.Serialize(context, ModelJson.Options);
            DamageScalingResult scaled = DamageScalingModel.Apply(context, id, id, input);
            Require(query.GetProperty("StateUnchanged").GetBoolean() && count is 0 or 1 or 3 && input is 0 or 99 &&
                card.DataId == "6314e127-6a79-4b81-9d0e-9e95b6dab168" && (id == ids[3] || id == ids[4]) &&
                source.SpecificCardDataId == counted && context.Statistics!.DeckCards!.Count(cardId => context.FindCard(cardId)!.DataId == counted) == count &&
                scaled.Supported && scaled.Damage == query.GetProperty("ActualDamage").GetInt32() && scaled.Damage == count * 10 + (id == ids[4] ? 5 : 0),
                "Native specific-card permanent-deck count, ignored input or source damage upgrade differs.");
            CardInstanceState WithSource(IReadOnlyList<CardSourceTrait>? sources) => card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost,
                descriptor.BaseTraits, descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, sources));
            CardSourceTrait missing = new CardSourceTrait(source.Type, source.Int, source.Int2, source.Int3, source.Bool, source.Str, source.Float, source.Scaling, source.TrackedValue, source.EntryDuration);
            CardSourceTrait[] replaced = descriptor.SourceTraits.Select(trait => trait.Type == CardSpecificDamageModel.Trait ? missing : trait).ToArray();
            Require(!DamageScalingModel.Apply(context.WithCard(WithSource(null)), id, id, input).Supported &&
                !DamageScalingModel.Apply(context.WithCard(WithSource(replaced)), id, id, input).Supported &&
                !DamageScalingModel.Apply(context, id, id, input, attackerUnitId: 1).Supported,
                "Missing source/specific-card metadata or attacker mutation must remain guarded.");
            Require(parent == JsonSerializer.Serialize(context, ModelJson.Options), "Specific-card evaluation or guards mutated their parent.");
        }
        foreach (FixtureValue sample in samples)
        {
            CombatContext context = sample.GetProperty("Context").Deserialize<CombatContext>()!;
            int id = sample.GetProperty("CardId").GetInt32();
            Require(sample.GetProperty("AttackerId").GetInt32() == 0 && sample.GetProperty("Target").Deserialize<CombatUnit>()!.Team == CombatTeam.Enemy &&
                DamageScalingModel.Apply(context, id, id, sample.GetProperty("InputDamage").GetInt32()).Damage == sample.GetProperty("ActualDamage").GetInt32() &&
                sample.GetProperty("ActualDamage").GetInt32() == (id == ids[3] ? 30 : 35), "Native direct-spell damage callback differs.");
        }
        foreach (int index in new[] { 3, 4 })
        {
            BattleTurnState before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            CardSourceTrait[] traits = card.MaskDescriptor!.SourceTraits!.ToArray();
            Require(traits.Select(trait => trait.Type).SequenceEqual(["CardTraitIgnoreArmor", CardSpecificDamageModel.Trait, "CardTraitExhaustState"]) &&
                traits[0].Int == 5 && traits[1].Int == 10 && traits[2].Int == 10 && traits[1].SpecificCardDataId != null &&
                rule.Cost == 2 && rule.Effects.Single().Target == "Tower" && rule.Effects[0].Value == 0 && rule.ExternalInteractions.Count == 0 &&
                rule.UpgradeInteractions.Count == 0 && before.Energy - after.Energy == 2 && after.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[index]) &&
                after.Spawn.Train.Context!.Statistics!.Value(ids[index], "TimesExhausted", "ThisBattle") == 2,
                "Original paired piercing/Consume parameters, tower targeting, cost or positive exhaustion differs.");
            CombatUnit[] enemies = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).ToArray();
            Require(enemies.Length > 0 && enemies.Any(unit => unit.Status("armor")?.Stacks > 0), "Positive tower cast needs an armored enemy control.");
            foreach (CombatUnit enemy in enemies)
            {
                CombatUnit hit = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == enemy.Id);
                Require(enemy.Health - hit.Health == (index == 3 ? 30 : 35) && enemy.Status("armor")?.Stacks == hit.Status("armor")?.Stacks,
                    "Native specific-card tower damage must pierce armor on every enemy.");
            }
            Require(JsonSerializer.Serialize(before.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Player), ModelJson.Options) ==
                JsonSerializer.Serialize(after.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Player), ModelJson.Options), "Enemy tower spell changed a friendly unit.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-SPECIFIC-CARD-DAMAGE PASS: original Dante's Inferno, twelve restored zero/one/three permanent-deck and 0/99 input queries, +5 damage upgrade, 30/35 armor-piercing tower damage, Consume and 16 immutable full chains. Attacker mutation and added/replaced/mixed traits remain guarded.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
