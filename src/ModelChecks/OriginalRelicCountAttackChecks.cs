using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRelicCountAttackChecks
{
    private const string Id = "79122f7a-6895-443a-830c-2db23027776c";
    private const string Noop = "815a25e7-2fed-4cb2-a568-79af12cd7116";
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-relic-count-attack" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => root.GetProperty(name).GetInt32() == 0),
            "Relic-count native battle incomplete or from another build.");
        var controls = root.GetProperty("RelicCountAttackControls").EnumerateArray().ToArray();
        Require(controls.Length == 45, "Missing count/classification controls.");
        foreach (var phase in controls.GroupBy(row => row.GetProperty("Phase").GetString()))
        {
            Require(phase.Count() == 9 && phase.Count(row => row.GetProperty("CardType").GetString() == "Spell") == 3 &&
                phase.Select(row => row.GetProperty("Card").GetString()).Distinct().Count() == 3, "Missing monster/champion/spell witnesses.");
            foreach (var row in phase)
            {
                var roles = row.GetProperty("Roles").Deserialize<RelicCountRole[]>()!;
                int count = roles.Count(role => role.Collected && !role.BossGiven && !role.SoulSavior && !role.ActiveStarter);
                bool foreign = phase.Key == "foreign-starter";
                Require(count == (foreign ? 2 : 1), "Native collection count differs.");
                if (phase.Key is "foreign-starter" or "active-starter" or "boss-excluded")
                    Require(roles.Single(role => role.Id == Noop).ActiveStarter == !foreign, "Active-clan starter identity was ignored.");
                if (phase.Key == "boss-excluded") Require(roles.Any(role => role.Collected && role.BossGiven), "Missing original boss exclusion.");
                int expected = row.GetProperty("CardType").GetString() == "Monster" ? (10 - count) * (row.GetProperty("Reset").GetBoolean() ? 1 : 2) : 0;
                Require(row.GetProperty("AdditionalAttack").GetInt32() == expected, "Native count attack/reset/repeat differs.");
            }
        }
        int calls = 0, removals = 0;
        foreach (var row in root.GetProperty("RelicCardModifiers").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            var owners = row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!;
            var context = row.GetProperty("ContextBefore").Deserialize<CombatContext>()!;
            var afterContext = row.GetProperty("ContextAfter").Deserialize<CombatContext>()!;
            var owner = owners.Single(item => item.DataId == Id);
            Require(owner.AssetKey == "MoreOrLessArtifactsRelic" && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual([RelicModel.ModifyCardAttack, RelicModel.ModifyCardAttack]) && owner.CardAttackModifiers!.Count == 2,
                "Original count owner identity changed.");
            foreach (var effect in owner.CardAttackModifiers!)
                Require(effect.AttackModifier == (effect.EffectIndex == 0 ? -1 : 10) && effect.AttackMultiplier == 1 && effect.UnmodifiedAttackMultiplier == 0 &&
                    effect.MultiplyByRelicCount == (effect.EffectIndex == 0) && effect.ExcludeStartRelics == (effect.EffectIndex == 0) &&
                    !effect.MultiplyByUpgradeCount && !effect.ExcludeChampions && effect.CardType == "Monster" && effect.CharacterName == "" && effect.CharacterSubtypeKey == "" && effect.CardFilter == null,
                    "Original count effect parameters changed.");
            bool reset = row.GetProperty("ResetTemporary").GetBoolean();
            string frozen = JsonSerializer.Serialize(new { before, owners, context });
            var actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
            var result = RelicCardModifierModel.Apply(before, owners, reset, context);
            Require(result.Supported, "Count manager unsupported: " + result.UnsupportedReason);
            Equal(result.Card, actual, "complete card modifiers");
            Equal(result.Dispatches, row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>(), "ordered native dispatches");
            Equal(result.Context!.Relics, afterContext.Relics, "relic state");
            Equal(result.Context.Statistics, afterContext.Statistics, "statistics");
            Equal(context.Cards.Rng, afterContext.Cards.Rng, "card-draw RNG");
            if (reset && before.MaskDescriptor!.Definition.CardType == "Monster")
            {
                int count = owner.CardAttackModifiers[0].CountRoles!.Count(role => owners.Any(item => item.DataId == role.Id) && role.Collected && !role.BossGiven && !role.SoulSavior && !role.ActiveStarter);
                var absent = RelicCardModifierModel.Apply(before, owners.Where(item => item.DataId != Id).ToArray(), true, context);
                Require(absent.Supported && actual.Temporary.Offsets.Damage - absent.Card!.Temporary.Offsets.Damage == 10 - count, "Positive owner ablation differs.");
                if (owners.Any(item => item.DataId == Noop) && !owner.CardAttackModifiers[0].CountRoles!.Single(role => role.Id == Noop).ActiveStarter)
                {
                    var removed = RelicCardModifierModel.Apply(before, owners.Where(item => item.DataId != Noop).ToArray(), true, context);
                    Require(removed.Supported && removed.Card!.Temporary.Offsets.Damage == actual.Temporary.Offsets.Damage + 1, "Removed branch owner used a stale count."); removals++;
                }
                var unknown = new CombatRelicState("unclassified-new-owner", "New", [RelicModel.Null]);
                Require(!RelicCardModifierModel.Apply(before, owners.Append(unknown).ToArray(), true, context).Supported, "Unclassified ownership silently counted.");
                Parallel.For(0, 16, _ => Equal(RelicCardModifierModel.Apply(before, owners, true, context).Card, actual, "parallel branch"));
            }
            Require(frozen == JsonSerializer.Serialize(new { before, owners, context }), "Count replay mutated its parent."); calls++;
        }
        Require(calls >= 45 && removals >= 2, "Missing exact calls or positive foreign-owner removal.");
        Console.WriteLine($"ORIGINAL-RELIC-COUNT-ATTACK PASS: {calls} exact native manager calls, 45 monster/champion/spell controls, active/foreign starter and boss exclusions, reset/repeat, positive owner removal, unknown ownership guard and 16 parallel immutable branches.");
    }
    private static void Equal<T>(T a, T b, string label) => Require(JsonSerializer.Serialize(a) == JsonSerializer.Serialize(b), "Native relic-count " + label + " differs.");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
