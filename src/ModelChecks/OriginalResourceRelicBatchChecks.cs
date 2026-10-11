using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalResourceRelicBatchChecks
{
    private sealed record Artifact(string Key, string Id);
    private static readonly Artifact[] Artifacts =
    [
        new("SilverIncreasedDraw", "4a51a457-9c50-4623-b8f5-e1bd9aac561a"),
        new("SilverIncreasedEmber", "1a69fbff-9f1d-4c98-92ff-a63e0ac520c5"),
        new("Inkbound_LeakyPen", "19c5ec66-4dde-4161-8fcf-68caf38a293c"),
        new("LessDeployEmberMoreOtherEmber", "7bc61ac1-f295-4bf5-a978-1c538c8f2e51"),
        new("MoreDeploymentPhaseEmber", "83116c76-f5cc-4912-8a56-872731393975"),
        new("BossEmber", "780f977e-4007-45ae-900f-c8929b2bf000"),
        new("BossEmber2", "6eef1e2d-3bd5-4f04-bd88-d0c24d5eb691"),
        new("BossEmberDH", "50e516a6-f27a-4878-9586-c0efce046206")
    ];
    private sealed record Snapshot(BattleResourceState Resources, CombatRelicState[] Relics, int Energy, int Draw,
        int PyreHP, int MaxHP, int PyreAttack, int EnergyModification, int DeploymentModification,
        string Phase, int Turn, bool Preview, int Gold);
    private sealed record Operation(string Kind, string OwnerDataId, int EffectIndex, bool Deployment,
        int Notifications, Snapshot Before, Snapshot After);

    internal static IReadOnlyDictionary<string, int>? Native(FixtureValue root, bool required = false,
        bool replayBattle = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out var scenario) &&
            scenario.GetString() == "original-resource-relic-batch";
        Require(eligible || !required, "Fixture lacks the original resource relic batch.");
        if (!eligible) return null;
        Require(root.GetProperty("Schema").GetInt32() == 117 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Native resource relic schema or game build changed.");
        Require(root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("Policy").GetString() == "units-spells-and-junk", "Native resource battle is incomplete.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Resource capture has nonzero " + field + ".");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] stages = root.GetProperty("Stages").EnumerateArray().ToArray();
        Require(actions.Length > 0 && turns.Length >= 2 && stages.Length > 0 &&
            turns[^1].GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon,
            "Resource battle lacks actual card plays, two EndTurns, complete combat or its terminal win.");
        Operation[] operations = root.GetProperty("RelicResourceOperations").EnumerateArray().Select(ReadOperation).ToArray();
        Operation[] acquisitions = operations.Where(row => row.Kind == "Acquire").ToArray();
        Require(acquisitions.Length == Artifacts.Length && acquisitions.Select(row => row.OwnerDataId)
            .SequenceEqual(Artifacts.Select(item => item.Id)), "Resource owners were not acquired once in the native scene order.");
        Require(operations.All(row => row.Kind is "Acquire" or "Energy"), "Unknown native resource operation.");
        var proofs = new Dictionary<(string Id, int Index), int>();
        foreach (Artifact item in Artifacts)
            for (int index = 0; index < Definition(item.Key).Length; index++) proofs[(item.Id, index)] = 0;
        void Witness(string id, int index) => proofs[(id, index)]++;
        string operationParent = Serialize(operations);

        for (int index = 0; index < acquisitions.Length; index++)
        {
            Operation row = acquisitions[index];
            Require(row.EffectIndex == -1 && !row.Deployment && !row.Before.Preview && !row.After.Preview &&
                row.Before.Turn == 0 && row.After.Turn == 0, "Resource acquisition was not the pre-battle native add boundary.");
            CheckDefinitions(row.Before.Resources, row.Before.Relics, false);
            CheckDefinitions(row.After.Resources, row.After.Relics, false);
            Require(!row.Before.Relics.Any(owner => owner.DataId == row.OwnerDataId), "Acquired resource owner already existed.");
            Equal(row.Before.Relics, row.After.Relics.Where(owner => owner.DataId != row.OwnerDataId).ToArray(),
                "native acquisition changed another installed owner");
            Equal(row.Before.Resources, row.After.Resources, "native acquisition rewrote raw balance/save resource inputs");
            if (index > 0) Equal(acquisitions[index - 1].After, row.Before, "native acquisition chain");
            CheckSnapshot(row.Before); CheckSnapshot(row.After);
            CombatContext afterContext = Context(row.After);
            int ownerIndex = Array.FindIndex(row.After.Relics, owner => owner.DataId == row.OwnerDataId);
            RelicPyreResourceResult acquired = RelicResourceModel.Acquire(afterContext, ownerIndex, row.Before.PyreHP);
            Require(acquired.Supported && acquired.Health == row.After.PyreHP && acquired.MaxHealth == row.After.MaxHP,
                "Native resource acquisition HP/max differs: " + row.OwnerDataId + " " + acquired.UnsupportedReason);
            CombatContext removed = afterContext.WithRelics(row.Before.Relics);
            RelicResourceResult oldDraw = RelicResourceModel.GetDrawCount(removed);
            RelicResourceResult oldMax = RelicResourceModel.GetPyreMaxHealth(removed);
            RelicResourceResult oldAttack = RelicResourceModel.GetPyreAttack(removed);
            Require(oldDraw.Supported && oldDraw.Value == row.Before.Draw && oldMax.Supported && oldMax.Value == row.Before.MaxHP &&
                oldAttack.Supported && oldAttack.Value == row.Before.PyreAttack, "Resource acquisition owner ablation differs.");
            foreach (RelicResourceEffect rule in row.After.Relics[ownerIndex].ResourceEffects!)
            {
                if (rule.Type == RelicResourceModel.Draw)
                    Require(row.After.Draw - row.Before.Draw == rule.Amount && rule.Amount > 0,
                        "Original draw owner lacks a positive native registered mutation: " + row.OwnerDataId);
                if (rule.Type == RelicResourceModel.PyreHealth)
                {
                    Require(rule.Amount > 0 && row.After.MaxHP - row.Before.MaxHP == rule.Amount &&
                        row.After.PyreHP - row.Before.PyreHP == rule.Amount,
                        "Original Boss health owner lacks its actual acquisition heal/max increment: " + row.OwnerDataId);
                    Witness(row.OwnerDataId, rule.EffectIndex);
                }
                if (rule.Type == RelicResourceModel.PyreAttack)
                    Require(rule.Amount > 0 && row.After.PyreAttack - row.Before.PyreAttack == rule.Amount,
                        "Original Boss attack owner lacks its positive native initialized attack increment: " + row.OwnerDataId);
            }
        }
        Snapshot installed = acquisitions[^1].After;
        CheckDefinitions(installed.Resources, installed.Relics, true);
        Require(installed.EnergyModification == 4 && installed.DeploymentModification == 4 && installed.Draw == 8,
            "Eight unchanged original resource owners lack their native +4/+4/draw8 aggregate.");
        Operation[] energy = operations.Where(row => row.Kind == "Energy").ToArray();
        Require(energy.Length > 0, "No actual native resource timing callback was captured.");
        foreach (Operation row in energy)
        {
            Require(!row.Before.Preview && !row.After.Preview && row.Before.Phase == "MonsterTurn" &&
                row.After.Phase == row.Before.Phase && row.After.Turn == row.Before.Turn,
                "Resource timing capture is outside an actual native MonsterTurn body.");
            CheckDefinitions(row.Before.Resources, row.Before.Relics, true);
            CheckDefinitions(row.After.Resources, row.After.Relics, true);
            CheckSnapshot(row.Before); CheckSnapshot(row.After);
            CombatRelicState owner = row.Before.Relics.Single(item => item.DataId == row.OwnerDataId);
            RelicResourceEffect rule = owner.ResourceEffects!.Single(item => item.EffectIndex == row.EffectIndex);
            Require(rule.Type == (row.Deployment ? RelicResourceModel.DeploymentEnergy : RelicResourceModel.Energy) &&
                (!row.Deployment || row.Before.Turn == 0), "Native energy callback owner/type/deployment turn differs.");
            Equal(row.Before.Resources, row.After.Resources, "energy callback changed raw resource inputs");
            Require(row.Before.Draw == row.After.Draw && row.Before.PyreHP == row.After.PyreHP &&
                row.Before.MaxHP == row.After.MaxHP && row.Before.PyreAttack == row.After.PyreAttack &&
                row.Before.Gold == row.After.Gold, "Energy timing callback changed a different live resource.");
            int expected = rule.Amount > 0 ? Math.Min(row.Before.Resources.MaxEnergy, unchecked(row.Before.Energy + rule.Amount)) :
                Math.Max(0, unchecked(row.Before.Energy - Math.Abs(rule.Amount)));
            Require(row.After.Energy == expected && row.Notifications == (row.Deployment ? 0 : 1),
                "Native signed energy body, one-sided clamp or E1/D0 notification differs.");
            RelicResourceEffect isolatedRule = new(0, rule.Type, rule.ParamInt, rule.ParamInt2, rule.Amount,
                rule.CanApplyInPreviewMode, rule.RequiredMutatorId, rule.Conditions);
            CombatRelicState isolatedOwner = new(owner.DataId, owner.AssetKey, [rule.Type],
                isCovenant: owner.IsCovenant, disallowedInPlacementPhase: owner.DisallowedInPlacementPhase,
                resourceEffects: [isolatedRule]);
            CombatContext isolated = Context(row.Before).WithRelics([isolatedOwner]);
            string parent = Serialize(isolated);
            RelicResourceResult applied = RelicResourceModel.ApplyEnergyTiming(isolated, row.Deployment,
                row.Before.Preview, placement: row.Before.Turn == 0);
            Require(applied.Supported && applied.Value == row.After.Energy && applied.Events.Count == 1 &&
                applied.Events[0].RelicId == owner.DataId && applied.Events[0].EffectIndex == 0 &&
                applied.Events[0].Before == row.Before.Energy && applied.Events[0].After == row.After.Energy &&
                applied.Events[0].Notified == !row.Deployment, "Independent single-effect energy body differs: " + applied.UnsupportedReason);
            Equal(applied.Context!.Relics![0].ResourceEffects![0].Conditions,
                row.After.Relics.Single(item => item.DataId == owner.DataId).ResourceEffects!
                    .Single(item => item.EffectIndex == row.EffectIndex).Conditions, "energy callback native conditions");
            RelicResourceResult without = RelicResourceModel.ApplyEnergyTiming(isolated.WithRelics([]), row.Deployment,
                row.Before.Preview, placement: row.Before.Turn == 0);
            Require(without.Supported && without.Value == row.Before.Energy && without.Events.Count == 0 && Serialize(isolated) == parent,
                "Energy owner ablation guessed a contribution or mutated its parent.");
            if (row.After.Energy != without.Value && (row.Deployment || row.Before.Turn > 0)) Witness(owner.DataId, row.EffectIndex);
        }
        CheckTimingBridge(energy, installed);

        void CheckQueries()
        {
            foreach (Operation row in operations) { CheckSnapshot(row.Before); CheckSnapshot(row.After); }
        }
        Parallel.For(0, 32, _ => CheckQueries());
        Require(Serialize(operations) == operationParent, "32 parallel resource queries mutated native snapshots or condition state.");

        int drawWitnesses = CheckDraws(root, Witness);
        BattleTurnState first = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState terminal = turns[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CheckBattle(first); CheckBattle(terminal);
        CombatUnit initialPyre = Units(first).Single(unit => unit.IsPyre);
        RelicPyreResourceResult birth = RelicResourceModel.InitializePyre(first.Spawn.Train.Context!, installed.PyreHP, placement: true);
        Require(birth.Supported && initialPyre.Modifiers != null && initialPyre.Modifiers.AttackDamage == birth.Attack &&
            initialPyre.BaseAttack == birth.Attack && initialPyre.MaxHealth == birth.MaxHealth && initialPyre.Health == birth.Health,
            "First native live Pyre raw attack/HP/max do not reproduce initialized original resources: " + birth.UnsupportedReason);
        foreach (Artifact artifact in Artifacts.Where(item => item.Key.StartsWith("BossEmber", StringComparison.Ordinal)))
        {
            CombatContext before = first.Spawn.Train.Context!;
            RelicResourceEffect attack = before.Relics!.Single(owner => owner.DataId == artifact.Id).ResourceEffects!
                .Single(rule => rule.Type == RelicResourceModel.PyreAttack);
            RelicResourceEffect health = before.Relics.Single(owner => owner.DataId == artifact.Id).ResourceEffects!
                .Single(rule => rule.Type == RelicResourceModel.PyreHealth);
            RelicPyreResourceResult without = RelicResourceModel.InitializePyre(Remove(before, artifact.Id), installed.PyreHP, placement: true);
            Require(without.Supported && birth.Attack - without.Attack == attack.Amount && birth.MaxHealth - without.MaxHealth == health.Amount,
                "Original Boss birth ablation lost an initialized table/index contribution.");
        }
        foreach (FixtureValue row in actions)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CheckBattle(before); CheckBattle(actual);
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            Require(IsEmptySpell(before, action.CardInstanceId) && action.ActivatorUnitId == 0 && action.TargetUnitId == 0,
                "Resource battle used an authored resource setter, summon or nonempty spell action.");
            string parent = Serialize(before);
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported, "Original resource action unsupported: " + result.Reason);
            Equal(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual), "complete native resource action");
            if (row.TryGetProperty("ActualOutcome", out var outcome)) Require(result.Outcome == (RoomOutcome)outcome.GetInt32(), "Resource action outcome differs.");
            Require(Serialize(before) == parent, "Resource card replay mutated its source.");
        }
        foreach (FixtureValue row in turns)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CheckBattle(before); CheckBattle(actual);
            string parent = Serialize(before);
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32(),
                "Original resource EndTurn unsupported or outcome differs: " + result.UnsupportedReason);
            Equal(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual), "complete native resource EndTurn");
            Require(Serialize(before) == parent, "Resource EndTurn mutated its source.");
        }
        int pyreCombats = 0;
        foreach (FixtureValue row in stages)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CheckRoom(before); CheckRoom(actual);
            string parent = Serialize(before);
            string kind = row.GetProperty("Kind").GetString()!;
            Require(kind is "Exchange" or "Room", "Unknown native resource combat stage.");
            RoomCombatResult Run(RoomCombatState value) => kind == "Exchange" ? RoomCombatModel.Exchange(value) : RoomCombatModel.Resolve(value);
            RoomCombatResult result = Run(before);
            Require(result.Supported, "Original resource combat unsupported: " + result.UnsupportedReason);
            Equal(result.State, actual, "complete native original resource combat");
            CombatUnit? pyre = before.Units.SingleOrDefault(unit => unit.IsPyre);
            if (pyre != null && result.Events.Any(effect => effect.Kind == "Attack" && effect.Actor == pyre.Id && effect.Amount > 0) &&
                before.Units.Where(unit => unit.Team == CombatTeam.Enemy).Any(unit =>
                    (actual.Units.FirstOrDefault(after => after.Id == unit.Id)?.Health ?? 0) < unit.Health))
            {
                pyreCombats++;
                foreach (Artifact artifact in Artifacts.Where(item => item.Key.StartsWith("BossEmber", StringComparison.Ordinal)))
                {
                    RelicResourceEffect rule = before.Context!.Relics!.Single(owner => owner.DataId == artifact.Id).ResourceEffects!
                        .Single(effect => effect.Type == RelicResourceModel.PyreAttack);
                    CombatUnit weaker = WeakenPyre(pyre, rule.Amount);
                    RoomCombatState ablated = new(before.RoomIndex, before.Deployment,
                        before.Units.Select(unit => unit.Id == pyre.Id ? weaker : unit).ToArray(), before.ExternalInteractions,
                        Remove(before.Context!, artifact.Id), before.Preview);
                    RoomCombatResult without = Run(ablated);
                    Require(without.Supported, "Original Boss attack combat ablation unsupported: " + without.UnsupportedReason);
                    if (result.Events.Any(effect => effect.Kind == "Attack" && effect.Actor == pyre.Id && effect.Amount > 0 &&
                        without.Events.Any(other => other.Kind == effect.Kind && other.Actor == effect.Actor && other.Target == effect.Target &&
                            other.Round == effect.Round && other.Amount < effect.Amount))) Witness(artifact.Id, rule.EffectIndex);
                }
            }
            Require(Serialize(before) == parent, "Pyre combat or its birth-attack ablations mutated native starting state.");
        }
        Require(pyreCombats > 0, "The original resource batch never exercised a positive native Pyre attack against enemies.");
        foreach (var proof in proofs) Require(proof.Value > 0,
            "An unchanged resource original lacks actual effect proof: " + Artifacts.Single(item => item.Id == proof.Key.Id).Key + "/" + proof.Key.Index);
        if (replayBattle)
        {
            CardCycleChecks.Native(root); BattleTurnChecks.Native(root); BattleActionChecks.Native(root);
            ReplayBranches(first, actions, turns, terminal, "root");
            int midIndex = Array.FindIndex(actions, row => row.GetProperty("Actual").GetProperty("Spawn").GetProperty("Turn").GetInt32() > 0);
            Require(midIndex >= 0, "Resource native battle lacks a mid-battle card-action root.");
            BattleTurnState mid = actions[midIndex].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            ReplayBranches(mid, actions.Skip(midIndex + 1).ToArray(), turns.Where(row =>
                row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() >= mid.Spawn.Turn).ToArray(), terminal, "mid");
        }
        var witnesses = Artifacts.ToDictionary(item => item.Key, item => proofs.Where(proof => proof.Key.Id == item.Id).Sum(proof => proof.Value));
        Console.WriteLine("ORIGINAL-RESOURCE-RELIC-WITNESSES " + string.Join(", ", witnesses.Select(item => item.Key + "=" + item.Value)));
        Console.WriteLine($"NATIVE-ORIGINAL-RESOURCE-RELICS PASS: eight unchanged originals/all20 effects, {acquisitions.Length} real acquisitions, {energy.Length} signed/clamped native energy bodies, {drawWitnesses} real draw8 applications, {pyreCombats} Pyre combat stages, {actions.Length} actions/{turns.Length} EndTurns; table-indexed acquisition/birth/combat, owner ablations and 32 immutable getter branches.");
        return witnesses;
    }

    private static void CheckSnapshot(Snapshot row)
    {
        CheckDefinitions(row.Resources, row.Relics, false);
        CombatContext context = Context(row);
        string parent = Serialize(context);
        RelicResourceResult ordinary = RelicResourceModel.GetEnergyModification(context, preview: row.Preview);
        RelicResourceResult deployment = RelicResourceModel.GetEnergyModification(context, deployment: true, preview: row.Preview);
        RelicResourceResult draw = RelicResourceModel.GetDrawCount(context);
        RelicResourceResult health = RelicResourceModel.GetPyreMaxHealth(context);
        RelicResourceResult attack = RelicResourceModel.GetPyreAttack(context, placement: row.Turn == 0);
        int nativeOrdinary = row.Preview ? 0 : Math.Max(0, row.Relics.SelectMany(owner => owner.ResourceEffects ?? [])
            .Where(rule => rule.Type == RelicResourceModel.Energy).Sum(rule => rule.ParamInt));
        int nativeDeployment = row.Preview ? 0 : row.Relics.SelectMany(owner => owner.ResourceEffects ?? [])
            .Where(rule => rule.Type == RelicResourceModel.DeploymentEnergy).Sum(rule => rule.ParamInt);
        int nativeDraw = Math.Clamp(row.Resources.BaseDraw + row.Relics.SelectMany(owner => owner.ResourceEffects ?? [])
            .Where(rule => rule.Type == RelicResourceModel.Draw && rule.DrawMutationOrder.HasValue).Sum(rule => rule.ParamInt), 1, row.Resources.MaxHandSize);
        int nativeHealth = row.Resources.BasePyreMaxHealth + row.Relics.SelectMany(owner => owner.ResourceEffects ?? [])
            .Where(rule => rule.Type == RelicResourceModel.PyreHealth).Sum(rule => row.Resources.UpgradeHealth[rule.ParamInt2]);
        int nativeAttack = row.Resources.PyreBaseAttack + row.Resources.PyrePersistentDamage + row.Relics.SelectMany(owner => owner.ResourceEffects ?? [])
            .Where(rule => rule.Type == RelicResourceModel.PyreAttack).Sum(rule => row.Resources.UpgradeAttack[rule.ParamInt2]);
        Require(ordinary.Supported && ordinary.Value == row.EnergyModification && row.EnergyModification == nativeOrdinary &&
            deployment.Supported && deployment.Value == row.DeploymentModification && row.DeploymentModification == nativeDeployment &&
            draw.Supported && draw.Value == row.Draw && row.Draw == nativeDraw && health.Supported && health.Value == row.MaxHP &&
            row.MaxHP == nativeHealth && attack.Supported && attack.Value == row.PyreAttack && row.PyreAttack == nativeAttack,
            "Native resource getter/raw table result differs from independent unchanged original parameters.");
        foreach (RelicResourceResult result in new[] { ordinary, deployment, draw, health, attack })
            Require(Serialize(result.Context) == parent && result.Context!.BattleRng.Equals(context.BattleRng) &&
                result.Context.Cards.Rng.Equals(context.Cards.Rng), "Resource getter mutated conditions, cache or RNG.");
        Require(Serialize(context) == parent, "Resource getter mutated its full immutable source.");
    }

    private static void CheckTimingBridge(Operation[] energy, Snapshot installed)
    {
        Operation[] deployed = energy.Where(row => row.Deployment).ToArray();
        var expectedDeployment = installed.Relics.SelectMany(owner => (owner.ResourceEffects ?? [])
            .Where(rule => rule.Type == RelicResourceModel.DeploymentEnergy).Select(rule => (owner.DataId, rule.EffectIndex))).ToArray();
        Require(deployed.Select(row => (row.OwnerDataId, row.EffectIndex)).SequenceEqual(expectedDeployment) &&
            deployed[0].Before.Energy == installed.Resources.BaseEnergy + installed.Resources.DeploymentExtraEnergy &&
            deployed[^1].After.Energy == 8, "Native deployment callbacks lack their exact owner order or raw4-to8 bridge.");
        for (int index = 1; index < deployed.Length; index++) Equal(deployed[index - 1].After, deployed[index].Before, "deployment callback chain");
        Operation[] opening = energy.Where(row => !row.Deployment && row.Before.Turn == 0).ToArray();
        Require(opening.Length > 0 && opening[0].Before.Energy == deployed[^1].After.Energy && opening[^1].After.Energy == 12,
            "Native deployment-to-before-draw energy8-to12 ordered callback bridge is missing.");
        var expectedOrdinary = installed.Relics.SelectMany(owner => (owner.ResourceEffects ?? [])
            .Where(rule => rule.Type == RelicResourceModel.Energy).Select(rule => (owner.DataId, rule.EffectIndex))).ToArray();
        foreach (var group in energy.Where(row => !row.Deployment).GroupBy(row => row.Before.Turn))
        {
            Operation[] rows = group.ToArray();
            Require(rows.Select(row => (row.OwnerDataId, row.EffectIndex)).SequenceEqual(expectedOrdinary), "Native ordinary energy effect order differs.");
            for (int index = 1; index < rows.Length; index++) Equal(rows[index - 1].After, rows[index].Before, "ordinary callback chain");
            if (group.Key > 0) Require(rows[0].Before.Energy == installed.Resources.BaseEnergy && rows[^1].After.Energy == 7,
                "Native ordinary turn did not execute base3-to7 ordered signed resource callbacks.");
        }
        Require(energy.Any(row => !row.Deployment && row.Before.Turn > 0), "Native resource batch has no ordinary post-deployment timing effects.");
    }

    private static int CheckDraws(FixtureValue root, Action<string, int> witness)
    {
        int count = 0;
        foreach (FixtureValue row in root.GetProperty("CardCycles").EnumerateArray().Where(row => row.GetProperty("Kind").GetString() == "Draw"))
        {
            CombatContext context = row.GetProperty("StandbyContext").Deserialize<CombatContext>()!;
            Require(context.Resources != null, "Native resource hand draw omitted raw resource context.");
            CheckDefinitions(context.Resources!, context.Relics!, true);
            CardCycleState before = row.GetProperty("Before").Deserialize<CardCycleState>()!;
            CardCycleState actual = row.GetProperty("Actual").Deserialize<CardCycleState>()!;
            int handSize = row.GetProperty("HandSize").GetInt32();
            int max = row.GetProperty("MaxHandSize").GetInt32();
            RelicResourceResult draw = RelicResourceModel.GetDrawCount(context);
            Require(draw.Supported && handSize == 8 && draw.Value == handSize && max == context.MaxHandSize,
                "Native ordinary hand draw lacks registered original draw8 metadata.");
            Equal(Cycle(context.Cards), Cycle(before), "native resource pre-draw context/input");
            CardCycleState prepared = EquipmentModel.ReturnStandby(context, row.GetProperty("LivingUnitIds").Deserialize<int[]>()!).Cards;
            prepared = new(prepared.Hand, prepared.Draw, prepared.Discard, prepared.Rng, prepared.DrawModifier,
                before.ExternalInteractions, prepared.BonusDraw, prepared.NextDrawUpgrades, prepared.NextUpgradeReferenceId);
            string parent = Serialize(context);
            CardCycleResult result = CardCycleModel.DrawHand(prepared, handSize, max);
            Require(result.Supported, "Native resource card-cycle replay unsupported: " + result.UnsupportedReason);
            Equal(Cycle(result.State!), Cycle(actual), "complete resource native draw piles/RNG");
            HashSet<int> heldIds = prepared.Hand.Select(card => card.InstanceId).ToHashSet();
            int added = actual.Hand.Count(card => !heldIds.Contains(card.InstanceId));
            bool positive = max - prepared.Hand.Count >= 8 && prepared.DrawModifier == 0 &&
                prepared.Draw.Count + prepared.Discard.Count >= 8 && added == 8;
            if (positive)
            {
                count++;
                foreach (CombatRelicState owner in context.Relics!.Where(owner => owner.ResourceEffects?.Any(rule => rule.Type == RelicResourceModel.Draw) == true))
                foreach (RelicResourceEffect rule in owner.ResourceEffects!.Where(rule => rule.Type == RelicResourceModel.Draw))
                {
                    RelicResourceResult fewer = RelicResourceModel.GetDrawCount(Remove(context, owner.DataId));
                    CardCycleResult without = CardCycleModel.DrawHand(prepared, fewer.Value, max);
                    Require(fewer.Supported && without.Supported && draw.Value - fewer.Value == rule.Amount &&
                        added - without.State!.Hand.Count(card => !heldIds.Contains(card.InstanceId)) == rule.Amount && rule.Amount > 0,
                        "Native original draw8 lacks a positive real-card owner ablation: " + owner.AssetKey);
                    witness(owner.DataId, rule.EffectIndex);
                }
            }
            Require(Serialize(context) == parent, "Native resource draw or ablation mutated starting context.");
        }
        Require(count > 0, "No native ordinary draw actually added eight new card IDs with sufficient deck and hand capacity.");
        return count;
    }

    private static void CheckDefinitions(BattleResourceState raw, IReadOnlyList<CombatRelicState> relics, bool complete)
    {
        Require(raw.BaseEnergy == 3 && raw.DeploymentExtraEnergy == 1 && raw.BaseDraw == 5 && raw.MaxEnergy == 99 &&
            raw.MaxHandSize == 10 && raw.BasePyreMaxHealth > 0 && raw.UpgradeAttack.Count >= 2 && raw.UpgradeHealth.Count >= 2 &&
            raw.UpgradeAttack.Take(2).All(value => value > 0) && raw.UpgradeHealth.Take(2).All(value => value > 0) &&
            !string.IsNullOrEmpty(raw.PyreDataId), "Original resource batch lacks unchanged native raw balance/Pyre upgrade inputs.");
        foreach (Artifact artifact in Artifacts)
        {
            CombatRelicState[] matches = relics.Where(owner => owner.DataId == artifact.Id).ToArray();
            Require(matches.Length <= 1 && (!complete || matches.Length == 1), "Missing or duplicated original resource owner: " + artifact.Key);
            if (matches.Length == 0) continue;
            CombatRelicState owner = matches[0];
            var expected = Definition(artifact.Key);
            Require(owner.AssetKey == artifact.Key && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(expected.Select(rule => rule.Type)) && owner.ResourceEffects?.Count == expected.Length,
                "Original resource owner identity/runtime types changed: " + artifact.Key);
            for (int index = 0; index < expected.Length; index++)
            {
                RelicResourceEffect rule = owner.ResourceEffects![index];
                var definition = expected[index];
                int amount = definition.Type == RelicResourceModel.PyreAttack ? raw.UpgradeAttack[definition.Int2] :
                    definition.Type == RelicResourceModel.PyreHealth ? raw.UpgradeHealth[definition.Int2] : definition.Int;
                Require(rule.EffectIndex == index && rule.Type == definition.Type && rule.ParamInt == definition.Int &&
                    rule.ParamInt2 == definition.Int2 && rule.Amount == amount && !rule.CanApplyInPreviewMode &&
                    rule.RequiredMutatorId == null && rule.Conditions.Count == 0,
                    "Original resource effect parameter/runtime table/index/gates changed: " + artifact.Key + "/" + index);
                if (rule.Type == RelicResourceModel.Draw)
                    Require(rule.DrawValue == (float)definition.Int && rule.DrawMutationOrder == (artifact.Key == "SilverIncreasedDraw" ? 0 : 1),
                        "Original draw mutation lacks exact native float value/ref registration order.");
                else Require(rule.DrawValue == null && rule.DrawMutationOrder == null, "Non-draw original gained mutable draw metadata.");
            }
        }
        Require(relics.Where(owner => owner.ResourceEffects?.Count > 0).All(owner => Artifacts.Any(artifact => artifact.Id == owner.DataId)),
            "An additional resource owner confounds the eight original effects.");
    }
    private static (string Type, int Int, int Int2)[] Definition(string key) => key switch
    {
        "SilverIncreasedDraw" => [(RelicResourceModel.Draw, 1, 0)],
        "SilverIncreasedEmber" => [(RelicResourceModel.Energy, 1, 0), (RelicResourceModel.DeploymentEnergy, 1, 0)],
        "Inkbound_LeakyPen" => [(RelicResourceModel.Energy, -1, 0), (RelicResourceModel.Draw, 2, 0)],
        "LessDeployEmberMoreOtherEmber" => [(RelicResourceModel.DeploymentEnergy, -3, 0), (RelicResourceModel.Energy, 1, 0)],
        "MoreDeploymentPhaseEmber" => [(RelicResourceModel.DeploymentEnergy, 3, 0)],
        "BossEmber" => Boss(0),
        "BossEmber2" or "BossEmberDH" => Boss(1),
        _ => throw new InvalidDataException("Unexpected resource original: " + key)
    };
    private static (string Type, int Int, int Int2)[] Boss(int index) =>
        [(RelicResourceModel.Energy, 1, 0), (RelicResourceModel.PyreHealth, 30, index),
            (RelicResourceModel.PyreAttack, 10, index), (RelicResourceModel.DeploymentEnergy, 1, index)];
    private static Snapshot ReadSnapshot(FixtureValue value) => new(value.GetProperty("Resources").Deserialize<BattleResourceState>()!,
        value.GetProperty("Relics").Deserialize<CombatRelicState[]>()!, value.GetProperty("Energy").GetInt32(), value.GetProperty("Draw").GetInt32(),
        value.GetProperty("PyreHP").GetInt32(), value.GetProperty("MaxHP").GetInt32(), value.GetProperty("PyreAttack").GetInt32(),
        value.GetProperty("EnergyModification").GetInt32(), value.GetProperty("DeploymentModification").GetInt32(),
        value.GetProperty("Phase").GetString()!, value.GetProperty("Turn").GetInt32(), value.GetProperty("Preview").GetBoolean(), value.GetProperty("Gold").GetInt32());
    private static Operation ReadOperation(FixtureValue value)
    {
        Require(value.GetProperty("After").ValueKind != FixtureKind.Null, "Unfinished native resource operation.");
        return new(value.GetProperty("Kind").GetString()!, value.GetProperty("OwnerDataId").GetString()!, value.GetProperty("EffectIndex").GetInt32(),
            value.GetProperty("Deployment").GetBoolean(), value.GetProperty("Notifications").GetInt32(),
            ReadSnapshot(value.GetProperty("Before")), ReadSnapshot(value.GetProperty("After")));
    }
    private static CombatContext Context(Snapshot row) => new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(17),
        row.Gold, 1, row.Resources.MaxHandSize, statistics: BattleStatistics.Empty(deckCards: []), otherPiles: [],
        queryFrame: new(energy: row.Energy, runningCombat: true, turn: row.Turn),
        energyState: new(row.Resources.MaxEnergy, 0, 0, row.Phase, row.PyreHP > 0), relics: row.Relics, resources: row.Resources);
    private static CombatContext Remove(CombatContext source, string id) => source.WithRelics(source.Relics!.Where(owner => owner.DataId != id).ToArray());
    private static UnitModifiers PyreModifiers(UnitModifiers source, int amount) => new(unchecked(source.AttackDamage - amount),
        source.AttackDamageAdded, source.DamageBuff, source.RawSize, source.EquipmentLimit, source.CanBeHealed, source.IsClone,
        source.Upgrades, source.HealthFromUpgrades, source.SpawnerMatchesDefinition, source.DamageBuffFromStatusEffects);
    private static CombatUnit WeakenPyre(CombatUnit source, int amount) => new(source.Id, source.AssetKey, source.Team,
        unchecked(source.BaseAttack - amount), source.Health, source.MaxHealth, source.CanAttack, source.IsPyre, source.EndsBattleOnDeath,
        source.Statuses, source.Triggers, source.SpawnerCardId, source.Size, source.StatusImmunities, source.Subtypes,
        PyreModifiers(source.Modifiers!, amount), source.IsBoss, source.LastAttackerId, source.StatusRegistry, source.EquipmentCards,
        source.NextTriggerId, source.Ability, source.StatusDictionary, source.AbilityRules, source.HordeDefinition,
        source.IsSpawning, source.SacrificeCardId, source.DeathState, bumpRules: source.BumpRules);
    private static CombatUnit[] Units(BattleTurnState source) => source.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
    private static void CheckBattle(BattleTurnState source)
    {
        Require(source.Spawn.Train.Context?.Resources != null, "Resource battle omitted raw resource projection.");
        CheckDefinitions(source.Spawn.Train.Context!.Resources!, source.Spawn.Train.Context.Relics!, true);
        foreach (RoomCombatState room in source.Spawn.Train.Rooms) CheckRoom(room);
    }
    private static void CheckRoom(RoomCombatState source)
    {
        Require(source.Context?.Resources != null, "Resource combat stage omitted raw resource projection.");
        CheckDefinitions(source.Context!.Resources!, source.Context.Relics!, true);
        Require(source.Units.All(unit => unit.Team != CombatTeam.Player || unit.IsPyre), "Resource scene deployed a friendly carrier.");
    }
    private static string Cycle(CardCycleState source) => Serialize(new
        { source.Hand, source.Draw, source.Discard, source.Rng, source.DrawModifier, source.BonusDraw, source.NextDrawUpgrades, source.NextUpgradeReferenceId });
    private static void ReplayBranches(BattleTurnState source, FixtureValue[] actions, FixtureValue[] turns, BattleTurnState terminal, string label)
    {
        string parent = Serialize(source);
        string expected = BattleTurnChecks.Comparable(terminal);
        void Run()
        {
            BattleSimulationResult result = BattleSimulator.Resolve(source, Choose);
            Require(result.Supported && result.Outcome == RoomOutcome.BattleWon && result.Actions.Count == actions.Length && result.Turns.Count == turns.Length,
                "Independent resource " + label + " policy did not reproduce native action/turn counts: " + result.UnsupportedReason);
            Equal(expected, BattleTurnChecks.Comparable(result.State!), "independent resource " + label + " terminal");
            for (int index = 0; index < actions.Length; index++)
            {
                Equal(result.Actions[index].Action, actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!, label + " native policy action");
                Equal(BattleTurnChecks.Comparable(result.Actions[index].Result.State!),
                    BattleTurnChecks.Comparable(actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!), label + " native action decision");
            }
            for (int index = 0; index < turns.Length; index++)
            {
                Require(result.Turns[index].Outcome == (RoomOutcome)turns[index].GetProperty("ActualOutcome").GetInt32(), label + " EndTurn outcome differs.");
                Equal(BattleTurnChecks.Comparable(result.Turns[index].State!),
                    BattleTurnChecks.Comparable(turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!), label + " native EndTurn decision");
            }
        }
        Run(); Parallel.For(0, 16, _ => Run());
        Require(Serialize(source) == parent, "Independent resource " + label + " policy mutated its source.");
    }

    // Independent chooser: only captured definitions/current state determine a play.
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
        IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
        foreach (CardToken card in source.Spawn.Train.Context.Cards.Hand)
        {
            CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
            if (rule?.CardType != "Spell" || rule.IsAnyAbility != false || rule.Effect != "Null" ||
                rule.Effects.Count != 0 || rule.SpawnUnit != null || rule.Summon != null || rule.Ability != null ||
                rule.Equipment != null || rule.Cost != 0 || rule.CostType != "Default" || rule.ExternalInteractions.Count != 0)
                continue;
            foreach (RoomPlayRule room in source.PlayRules.Rooms.Where(room => !room.IsPyre).OrderBy(room => room.RoomIndex))
            {
                PlayCardAction? play = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                    action.RoomIndex == room.RoomIndex && action.ActivatorUnitId == 0 && action.TargetUnitId == 0);
                if (play != null) return play;
            }
        }
        return null;
    }
    private static bool IsEmptySpell(BattleTurnState source, int id)
    {
        CardInstanceState? card = source.Spawn.Train.Context!.FindCard(id);
        CardPlayRule? rule = source.PlayRules!.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
        return rule?.CardType == "Spell" && rule.Effect == "Null" && rule.Effects.Count == 0 && rule.Cost == 0 &&
            rule.CostType == "Default" && rule.IsAnyAbility == false && rule.SpawnUnit == null && rule.Summon == null &&
            rule.Ability == null && rule.Equipment == null && rule.ExternalInteractions.Count == 0;
    }
    private static string Serialize<T>(T source) => JsonSerializer.Serialize(source, ModelJson.Options);
    private static void Equal<T>(T expected, T actual, string label) => Equal(Serialize(expected), Serialize(actual), label);
    private static void Equal(string expected, string actual, string label)
    { string? difference = ModelJson.Difference(expected, actual); Require(difference == null, label + ": " + difference); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
