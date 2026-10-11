using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelicStatusStackChecks
{
    internal static void Run()
    {
        var spent = new RelicConditionState(new("SubtypeInDeck"), true, 6, 1, false, triggered: true);
        var limit = new RelicConditionState(new("SubtypeInDeck"), true, 6, 1, false);
        CombatRelicState[] relics =
        [
            Relic("unconditional", new(0, "Monsters", "valor", 1, 0, [spent])),
            Relic("positive", new(0, "Monsters", "armor", 4, 1, [limit])),
            Relic("first", new(0, "Heroes", "pyregel", 6, 2)),
            Relic("either", new(0, "None", "pyregel", 1, 1))
        ];
        int Get(string id, int count, CombatTeam? team = CombatTeam.Player, bool has = false,
            bool permanent = false, bool deferred = false, string? type = null) =>
            RelicStatusStackModel.GetStacks(relics, id, count, team, has, permanent, deferred, type);
        Require(Get("valor", 0) == 1 && Get("valor", -2) == -1 && Get("armor", 0) == 0 && Get("armor", -1) == -1 &&
            Get("armor", 1) == 5 && Get("armor", 1, CombatTeam.Enemy) == 1, "Unconditional, positive or exact-team gates differ.");
        Require(Get("pyregel", 1, CombatTeam.Enemy) == 8 && Get("pyregel", 1, CombatTeam.Enemy, true) == 2 &&
            Get("pyregel", 0, CombatTeam.Enemy) == 7 && Get("pyregel", 1, null) == 2 &&
            Get("armor", 1, null) == 5, "First application or null-character gates differ.");
        Require(Get("armor", 1, permanent: true) == 1 && Get("armor", 0, permanent: true, deferred: true) == 4 &&
            Get("pyregel", 0, CombatTeam.Enemy, true, true, true) == 7 && Get("armor", 1, deferred: true) == 1,
            "Permanent/deferred additions incorrectly use ordinary optional conditions.");
        foreach (string type in new[] { "CardEffectMultiplyStatusEffect", "CardEffectTransferAllStatusEffects", "CardEffectTransferAllStatusEffectsFromUnit" })
            Require(Get("armor", 1, type: type) == 1, "An excluded transfer/multiply operation received extra stacks.");
        Require(Get("armor", int.MaxValue) == unchecked(int.MaxValue + 4), "Native signed overflow was clamped before addition.");
        Require(RelicStatusStackModel.GetStacks([Relic("combined", new(0, "255", "armor", 9, 0))],
            "armor", 1, CombatTeam.Player, false) == 1, "A combined source team was treated as flags rather than exact equality.");
        var firstA = Relic("firstA", new(0, "Heroes", "pyregel", 6, 2));
        var firstB = Relic("firstB", new(0, "Heroes", "pyregel", 7, 2));
        Require(RelicStatusStackModel.GetStacks([firstA, firstB], "pyregel", 1, CombatTeam.Enemy, false) == 14,
            "A preceding adder changed the first-application input of a later adder.");
        var unconditionalArmor = Relic("zeroSeeder", new(0, "Monsters", "armor", 1, 0));
        Require(RelicStatusStackModel.GetStacks([unconditionalArmor, relics[1]], "armor", 0, CombatTeam.Player, false) == 5 &&
            RelicStatusStackModel.GetStacks([relics[1], unconditionalArmor], "armor", 0, CombatTeam.Player, false) == 1,
            "A later positive-stack adder did not use the running amount in native relic order.");

        RoomCombatState root = State(relics);
        string parent = Serialize(root);
        var applied = StatusApplicationModel.Apply(root, 1, new("armor", 1));
        Require(applied.Supported && applied.State!.Units[0].Status("armor")?.Stacks == 5, applied.UnsupportedReason ?? "Status addition differs.");
        Require(applied.State!.Context!.Relics![1].StatusStackAdditions![0].Conditions[0].DurationTriggerCount == 1,
            "The adder callback did not record its condition before status application.");
        var repeated = StatusApplicationModel.Apply(applied.State, 1, new("armor", 1));
        Require(repeated.State!.Units[0].Status("armor")!.Stacks == 10 &&
            repeated.State.Context!.Relics![1].StatusStackAdditions![0].Conditions[0].DurationTriggerCount == 1,
            "A spent outer condition disabled the value query or was recorded twice.");
        var unrelated = StatusApplicationModel.Apply(root, 1, new("regen", 1));
        Require(unrelated.State!.Context!.Relics![1].StatusStackAdditions![0].Conditions[0].Triggered,
            "Native notification should record even when the added status ID differs.");
        var unmodified = StatusApplicationModel.Apply(root, 1, new("armor", 1), allowModification: false);
        Require(unmodified.State!.Units[0].Status("armor")!.Stacks == 1 &&
            unmodified.State.Context!.Relics![1].StatusStackAdditions![0].Conditions[0].Triggered,
            "Skipping count modification incorrectly skipped the notification phase.");
        var deferredResult = StatusApplicationModel.Apply(root, 1, new("armor", 1), fromPermanentUpgrade: true);
        Require(deferredResult.State!.Units[0].Status("armor")!.Stacks == 5 &&
            StatusApplicationModel.Apply(root, 1, new("armor", 1), fromPermanentUpgrade: true, spawnEffect: true)
                .State!.Units[0].Status("armor")!.Stacks == 1, "Permanent upgrade deferred/spawn paths differ.");
        var preview = new RoomCombatState(0, false, root.Units, [], root.Context, preview: true);
        var previewResult = StatusApplicationModel.Apply(preview, 1, new("armor", 1));
        Require(previewResult.State!.Units[0].Status("armor")!.Stacks == 5 &&
            !previewResult.State.Context!.Relics![1].StatusStackAdditions![0].Conditions[0].Triggered, "Preview count/condition behavior differs.");
        var excluded = StatusApplicationModel.Apply(root, 1, new("armor", 1), fromEffectType: "CardEffectMultiplyStatusEffect");
        Require(excluded.State!.Units[0].Status("armor")!.Stacks == 1 &&
            !excluded.State.Context!.Relics![1].StatusStackAdditions![0].Conditions[0].Triggered, "An excluded source recorded a notification.");
        var reset = RelicConditionModel.EndDuration(applied.State.Context!, "ThisBattle");
        // The synthetic query defaults to ThisTurn; only its matching boundary resets it.
        Require(reset.Relics![1].StatusStackAdditions![0].Conditions[0].Triggered &&
            !RelicConditionModel.EndDuration(applied.State.Context!, limit.Query.Duration).Relics![1].StatusStackAdditions![0].Conditions[0].Triggered,
            "Relic status condition duration was not preserved/reset correctly.");
        Require(!StatusApplicationModel.Apply(State([new("bad", "bad", [RelicModel.AddStatusOnApplied])]), 1, new("armor", 1)).Supported,
            "Missing status-adder payload was guessed.");
        CombatUnit template = CardSpellModel.Copy(root.Units[0], 20, [new("armor", 2), new("valor", 1)]);
        RoomCombatState starting = new(0, false, [template], [], root.Context);
        CombatUnit? initialized = null; RoomCombatState? initializedRoom = null;
        string? initializationError = StatusCallbackModel.Initialize(starting, template, template.Statuses,
            new List<RoomCombatModel.QueuedCharacterTrigger>(), value => initialized = value, value => initializedRoom = value);
        Require(initializationError == null && initialized?.Status("armor")?.Stacks == 6 && initialized.Status("valor")?.Stacks == 2 &&
            initializedRoom?.Context?.Relics![1].StatusStackAdditions![0].Conditions[0].Triggered == true,
            initializationError ?? "Starting status additions or their condition state were discarded.");
        Parallel.For(0, 32, _ => Require(Serialize(StatusApplicationModel.Apply(root, 1, new("armor", 1)).State) == Serialize(applied.State),
            "Parallel status branches differ."));
        Require(Serialize(root) == parent && applied.State.Context!.BattleRng.Equals(root.Context!.BattleRng), "Status additions mutated the parent or consumed RNG.");
        Console.WriteLine("RELIC-STATUS-STACK-CHECKS PASS: native count gates, independent first-application adders, overflow, deferred upgrades, unconditional notifications, preview and 32 immutable branches.");
    }
    private static CombatRelicState Relic(string id, RelicStatusStacks rule) => new(id, id, [RelicModel.AddStatusOnApplied], statusStackAdditions: [rule]);
    private static RoomCombatState State(IReadOnlyList<CombatRelicState> relics) => new(0, false,
        [new(1, "carrier", CombatTeam.Player, 2, 20, 20, true, false, false, [], [])], [],
        new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10, relics: relics));
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
