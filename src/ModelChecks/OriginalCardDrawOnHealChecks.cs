using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalCardDrawOnHealChecks
{
    private const string RelicId = "9a38370b-150c-45bd-9e3e-93a688a04850";
    private const string RelicKey = "CardDrawOnHeal";
    private const string Effect = "RelicEffectCardDrawOnHeal";

    internal static void Run()
    {
        RelicConditionState Condition() => new(new CardStatisticQuery("SubtypeInDeck", "ThisTurn"), true, 6, 1, false);
        RelicResourceEffect Resource() => new(0, Effect, 1, 0, 1, false, conditions: [Condition()],
            unitTrigger: new RelicUnitResourceRule("OnHeal", "Monsters", null, []));
        CombatRelicState Owner() => new(RelicId, RelicKey, [Effect], isCovenant: false,
            disallowedInPlacementPhase: true, resourceEffects: [Resource()]);
        CombatContext Context() => new(new CardCycleState([], [new(101, "spell-a"), new(102, "spell-b"), new(103, "spell-c")],
            [], UnityRng.Seed(903), 0, []), UnityRng.Seed(117), 0, 104, 10,
            relics: [Owner()]);
        CombatUnit Player() => new(1, "Steward", CombatTeam.Player, 5, 7, 10, true, false, false, []);
        CombatUnit Enemy() => new(2, "Sentry", CombatTeam.Enemy, 5, 7, 10, true, false, false, []);

        Require(RelicModel.Validate([Owner()]) == null, "Valid original CardDrawOnHeal definition was rejected.");
        Require(RelicModel.Validate([new CombatRelicState(RelicId, RelicKey, [Effect], isCovenant: false,
            disallowedInPlacementPhase: true, resourceEffects: [new RelicResourceEffect(0, Effect, 1, 0, 1, false)])]) != null,
            "Missing CardDrawOnHeal callback metadata passed validation.");

        CombatContext source = Context();
        string parent = Serialize(source);
        var first = RelicCardDrawOnHealModel.Apply(source, Player(), "OnHeal");
        RelicConditionState firstCondition = first.Context!.Relics![0].ResourceEffects![0].Conditions[0];
        Require(first.Supported && first.DrawnCardIds.SequenceEqual([103]) && first.Context.Cards.Hand.Count == 1 &&
            firstCondition.Triggered && firstCondition.DurationTriggerCount == 2,
            "The first allied heal did not draw once and record both native notifications.");
        Require(parent == Serialize(source), "CardDrawOnHeal mutated its parent context.");

        var repeated = RelicCardDrawOnHealModel.Apply(first.Context, Player(), "OnHeal");
        Require(repeated.Supported && repeated.DrawnCardIds.Count == 0 && repeated.Context!.Cards.Hand.Count == 1 &&
            repeated.Context.Relics![0].ResourceEffects![0].Conditions[0].DurationTriggerCount == 2,
            "A second allied heal was not suppressed for the current turn.");

        CombatContext reset = RelicConditionModel.EndDuration(repeated.Context!, "ThisTurn");
        var nextTurn = RelicCardDrawOnHealModel.Apply(reset, Player(), "OnHeal");
        Require(nextTurn.Supported && nextTurn.DrawnCardIds.SequenceEqual([102]) &&
            nextTurn.Context!.Relics![0].ResourceEffects![0].Conditions[0].DurationTriggerCount == 2,
            "The original heal-draw condition did not reset at the end of ThisTurn.");

        var enemy = RelicCardDrawOnHealModel.Apply(source, Enemy(), "OnHeal");
        var wrongTrigger = RelicCardDrawOnHealModel.Apply(source, Player(), "OnDeath");
        var placement = RelicCardDrawOnHealModel.Apply(source, Player(), "OnHeal", placement: true);
        var preview = RelicCardDrawOnHealModel.Apply(source, Player(), "OnHeal", preview: true);
        Require(enemy.Supported && enemy.DrawnCardIds.Count == 0 && !enemy.Context!.Relics![0].ResourceEffects![0].Conditions[0].Triggered &&
            wrongTrigger.Supported && wrongTrigger.DrawnCardIds.Count == 0 &&
            placement.Supported && placement.DrawnCardIds.Count == 0 &&
            preview.Supported && preview.DrawnCardIds.Count == 0,
            "The native team, trigger, deployment, or preview gate was ignored.");

        var fullHand = new CombatContext(new CardCycleState([new(201, "held-a"), new(202, "held-b")],
            [new(203, "spell")], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(17), 0, 204, 2, relics: [Owner()]);
        var fullHandResult = RelicCardDrawOnHealModel.Apply(fullHand, Player(), "OnHeal");
        Require(fullHandResult.Supported && fullHandResult.DrawnCardIds.Count == 0 &&
            fullHandResult.Context!.Relics![0].ResourceEffects![0].Conditions[0].DurationTriggerCount == 2,
            "A full hand prevented the native relic notification after DrawCards.");
        Console.WriteLine("ORIGINAL-CARD-DRAW-ON-HEAL-PURE PASS: allied-only OnHeal, once per ThisTurn, double notification count, reset, preview/deployment gates and full-hand notification.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 175 &&
            root.GetProperty("ModifierScenario").GetString() == "original-card-draw-on-heal" &&
            root.GetProperty("OriginalCardDrawOnHealPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "Original CardDrawOnHeal battle is incomplete or has native/model failures.");

        FixtureValue[] rows = root.GetProperty("RelicUnitResources").EnumerateArray().ToArray();
        Require(rows.Length > 0, "The native manager callback probe did not capture relic callbacks.");
        int positiveDraws = 0, suppressed = 0, enemyControls = 0, turnResets = 0;
        int? firstPlayerHealTurn = null;
        foreach (FixtureValue row in rows)
        {
            Require(row.GetProperty("Completed").GetBoolean() &&
                row.GetProperty("CaptureError").ValueKind == FixtureKind.Null &&
                row.GetProperty("UnsupportedReason").ValueKind == FixtureKind.Null &&
                row.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "A native CardDrawOnHeal callback capture failed or diverged.");
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext actual = row.GetProperty("Actual").Deserialize<CombatContext>()!;
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            string trigger = row.GetProperty("Trigger").GetString()!;
            CombatRelicState owner = before.Relics!.Single(relic => relic.DataId == RelicId);
            RelicResourceEffect effect = owner.ResourceEffects!.Single();
            RelicUnitResourceRule rule = effect.UnitTrigger!;
            RelicConditionState condition = effect.Conditions.Single();
            Require(owner.AssetKey == RelicKey && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == true &&
                owner.EffectTypes.SequenceEqual([Effect]) && effect.EffectIndex == 0 && effect.Type == Effect &&
                effect.Amount == 1 && effect.ParamInt == 1 && effect.ParamInt2 == 0 && !effect.CanApplyInPreviewMode &&
                effect.RequiredMutatorId == null && rule.Trigger == "OnHeal" && rule.Team == "Monsters" &&
                rule.Subtype == null && rule.RequiredStatuses.Count == 0 && condition.TrackTriggerCount &&
                condition.Query.Type == "SubtypeInDeck" && condition.Query.Duration == "ThisTurn" &&
                condition.Comparator == 6 && condition.Value == 1 && !condition.AllowMultiple && RelicModel.Validate(before) == null,
                "Captured original CardDrawOnHeal owner, parameter, team or condition changed.");

            var predicted = RelicCardDrawOnHealModel.Apply(before, actor, trigger, row.GetProperty("Placement").GetBoolean());
            Require(predicted.Supported && Serialize(predicted.Context) == Serialize(actual),
                "The original CardDrawOnHeal callback does not reproduce its complete native context.");
            int addedToHand = actual.Cards.Hand.Select(card => card.InstanceId).Except(before.Cards.Hand.Select(card => card.InstanceId)).Count();
            Require(addedToHand == predicted.DrawnCardIds.Count,
                "Native hand movement differs from the predicted CardDrawOnHeal result.");

            if (trigger != "OnHeal") continue;
            if (row.GetProperty("Placement").GetBoolean())
            {
                Require(predicted.DrawnCardIds.Count == 0 && addedToHand == 0 &&
                    actual.Relics!.Single(relic => relic.DataId == RelicId).ResourceEffects![0].Conditions[0].DurationTriggerCount ==
                    condition.DurationTriggerCount,
                    "The original CardDrawOnHeal owner ignored its deployment-phase restriction.");
                continue;
            }
            if (actor.Team == CombatTeam.Enemy)
            {
                Require(predicted.DrawnCardIds.Count == 0 && addedToHand == 0 &&
                    actual.Relics!.Single(relic => relic.DataId == RelicId).ResourceEffects![0].Conditions[0].DurationTriggerCount ==
                    condition.DurationTriggerCount,
                    "An enemy heal triggered the original Monsters-only relic.");
                enemyControls++;
                continue;
            }
            if (actor.Team != CombatTeam.Player) continue;
            int turn = before.QueryFrame?.Turn ?? -1;
            if (!condition.Triggered)
            {
                if (firstPlayerHealTurn.HasValue && turn > firstPlayerHealTurn.Value && condition.DurationTriggerCount == 0)
                    turnResets++;
                firstPlayerHealTurn ??= turn;
                Require(predicted.DrawnCardIds.Count == 1 && addedToHand == 1,
                    "The first allied heal in a turn did not draw exactly one card.");
                positiveDraws++;
            }
            else
            {
                Require(predicted.DrawnCardIds.Count == 0 && addedToHand == 0 &&
                    condition.DurationTriggerCount == 2,
                    "A repeated same-turn allied heal was not suppressed after the first double notification.");
                suppressed++;
            }
        }
        Require(positiveDraws >= 2 && suppressed > 0 && enemyControls > 0 && turnResets > 0,
            $"Missing native callback controls: positive={positiveDraws}, suppressed={suppressed}, enemy={enemyControls}, reset={turnResets}.");
        FixtureValue[] authoredActions = root.GetProperty("Actions").EnumerateArray()
            .Where(entry => entry.GetProperty("ScenarioAction").GetBoolean()).ToArray();
        int AuthoredPlays(string dataId) => authoredActions.Count(entry =>
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            return before.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == dataId;
        });
        Require(AuthoredPlays("fc119000-0000-4000-8000-000000000851") >= 2 &&
            AuthoredPlays("fc119000-0000-4000-8000-000000000852") >= 3 &&
            AuthoredPlays("fc119000-0000-4000-8000-000000000853") > 0,
            "The native scenario did not play its repeated allied heals and enemy-heal control cards.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"ORIGINAL-CARD-DRAW-ON-HEAL NATIVE PASS: {rows.Length} exact manager callbacks; {positiveDraws} allied draws, {suppressed} same-turn suppressions, {enemyControls} enemy controls, {turnResets} ThisTurn resets; all recorded actions and turn transitions matched.");
    }

    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool pass, string message)
    { if (!pass) throw new InvalidOperationException(message); }
}
