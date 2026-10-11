using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalFloorCapacityRelicChecks
{
    private sealed record Artifact(string Key, string Id, int PyreIndex = -1);
    private static readonly Artifact[] Artifacts =
    [
        new("PurgeChampionReward", "be25af24-9516-4ed2-9cf1-c756a381a928"),
        new("BossCapacity", "9291142a-dc6f-4834-89f1-ceda778e06ec", 0),
        new("BossCapacity2", "2d313e11-aa80-4116-b359-7560d3307544", 1),
        new("BossCapacityDH", "f7b0c251-7f96-4115-897a-d5b4a40a36ef", 1),
        new("SilverIncreasedCapacity", "aab4971f-05c6-430a-8a6a-dfb654a41b0c")
    ];
    private sealed record Snapshot(BattleResourceState Resources, CombatRelicState[] Relics, int Energy,
        int Draw, int PyreHP, int MaxHP, int PyreAttack, int EnergyModification, int DeploymentModification,
        string Phase, int Turn, bool Preview, int Gold, int Capacity);
    private sealed record Operation(string Kind, string OwnerDataId, int EffectIndex, bool Deployment,
        int Notifications, Snapshot Before, Snapshot After);

    internal static IReadOnlyDictionary<string, int>? Native(FixtureValue root, bool required = false,
        bool replayBattle = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out var scenario) &&
            scenario.GetString() == "original-floor-capacity-relics";
        Require(eligible || !required, "Fixture lacks the original floor capacity relic batch.");
        if (!eligible) return null;
        Require(root.GetProperty("Schema").GetInt32() == 119 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Native floor capacity schema or original game build changed.");
        Require(root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("Policy").GetString() == "units-spells-and-junk", "Floor capacity native battle is incomplete.");
        foreach (string name in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(name).GetInt32() == 0, "Floor capacity capture has nonzero " + name + ".");
        CheckOriginalDefinitions(root.GetProperty("FloorCapacityOriginalDefinitions"));
        CheckDeploymentCards(root.GetProperty("FloorCapacityDeploymentCards"));
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] stages = root.GetProperty("Stages").EnumerateArray().ToArray();
        Require(actions.Length > 0 && turns.Length >= 2 && stages.Length > 0 &&
            turns[^1].GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon,
            "Original capacity battle lacks card plays, multiple EndTurns or complete winning combat.");
        var proofs = Artifacts.SelectMany(owner => Enumerable.Range(0, Definition(owner).Length)
            .Select(index => (owner.Id, Index: index))).ToDictionary(key => key, _ => 0);
        void Witness(string id, int index) => proofs[(id, index)]++;
        Operation[] operations = root.GetProperty("RelicResourceOperations").EnumerateArray().Select(ReadOperation).ToArray();
        Operation[] acquisitions = operations.Where(row => row.Kind == "Acquire").ToArray();
        Require(acquisitions.Length == 5 && acquisitions.Select(row => row.OwnerDataId).SequenceEqual(Artifacts.Select(owner => owner.Id)) &&
            operations.All(row => row.Kind is "Acquire" or "Energy"), "Original capacity acquisition order or resource operations changed.");
        CheckAcquisitions(acquisitions, Witness);
        Operation[] energy = operations.Where(row => row.Kind == "Energy").ToArray();
        CheckEnergy(energy, acquisitions[^1].After, Witness);
        int queries = CheckCapacityQueries(root.GetProperty("FloorCapacityQueries"));
        CheckMatrixRestorations(root.GetProperty("FloorCapacityMatrixRestorations"));
        int initializations = CheckInitializations(root.GetProperty("FloorCapacityInitializations"));
        int draws = CheckDraws(root, Witness);
        BattleTurnState first = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState terminal = turns[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CheckBattle(first); CheckBattle(terminal);
        Snapshot installed = acquisitions[^1].After;
        CombatUnit initialPyre = Units(first).Single(unit => unit.IsPyre);
        RelicPyreResourceResult birth = RelicResourceModel.InitializePyre(first.Spawn.Train.Context!, installed.PyreHP, placement: true);
        Require(birth.Supported && initialPyre.Modifiers != null && initialPyre.Modifiers.AttackDamage == birth.Attack &&
            initialPyre.BaseAttack == birth.Attack && initialPyre.Health == birth.Health && initialPyre.MaxHealth == birth.MaxHealth,
            "Original Boss capacity Pyre birth differs from the independently captured raw definition tables: " + birth.UnsupportedReason);
        foreach (Artifact owner in Artifacts.Where(owner => owner.PyreIndex >= 0))
        {
            RelicPyreResourceResult without = RelicResourceModel.InitializePyre(Remove(first.Spawn.Train.Context!, owner.Id), installed.PyreHP, placement: true);
            Require(without.Supported && birth.Attack - without.Attack == installed.Resources.UpgradeAttack[owner.PyreIndex] &&
                birth.MaxHealth - without.MaxHealth == installed.Resources.UpgradeHealth[owner.PyreIndex],
                "Original Boss capacity birth lacks its actual table-indexed attack/health contribution.");
        }
        foreach (FixtureValue row in actions)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CheckBattle(before); CheckBattle(actual);
            CardPlayRule rule = Rule(before, action.CardInstanceId);
            Require((IsCarrier(rule) || IsEmptySpell(rule)) && action.ActivatorUnitId == 0 && action.TargetUnitId == 0,
                "Original capacity battle played a resource setter, capacity adjustment or unexpected card.");
            string parent = Serialize(before);
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported, "Original capacity action unsupported: " + result.Reason);
            Equal(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual), "complete native capacity action");
            Require(result.Outcome == (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32(), "Original capacity card outcome differs.");
            Require(Serialize(before) == parent, "Original capacity action mutated its parent.");
        }
        CheckCapacityConsumers(actions, Witness);
        foreach (FixtureValue row in turns)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CheckBattle(before); CheckBattle(actual);
            string parent = Serialize(before);
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32(),
                "Original capacity EndTurn unsupported or outcome differs: " + result.UnsupportedReason);
            Equal(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual), "complete native capacity EndTurn");
            Require(Serialize(before) == parent, "Original capacity EndTurn mutated its source.");
        }
        int pyreCombats = CheckCombat(stages, Witness);
        foreach (var proof in proofs)
            Require(proof.Value > 0, "Original floor capacity owner lacks an actual effect witness: " +
                Artifacts.Single(owner => owner.Id == proof.Key.Id).Key + "/" + proof.Key.Index);
        if (replayBattle)
        {
            CardCycleChecks.Native(root); BattleTurnChecks.Native(root); BattleActionChecks.Native(root);
            ReplayBranches(first, actions, turns, terminal, "root");
            int midIndex = Array.FindIndex(actions, row => row.GetProperty("Actual").GetProperty("Spawn").GetProperty("Turn").GetInt32() > 0);
            Require(midIndex >= 0, "Original floor capacity battle lacks a genuine mid-battle policy action.");
            BattleTurnState mid = actions[midIndex].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            ReplayBranches(mid, actions.Skip(midIndex + 1).ToArray(), turns.Where(row =>
                row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() >= mid.Spawn.Turn).ToArray(), terminal, "mid");
        }
        var witnesses = Artifacts.ToDictionary(owner => owner.Key, owner => proofs.Where(proof => proof.Key.Id == owner.Id).Sum(proof => proof.Value));
        Console.WriteLine("ORIGINAL-FLOOR-CAPACITY-WITNESSES " + string.Join(", ", witnesses.Select(row => row.Key + "=" + row.Value)));
        Console.WriteLine($"NATIVE-ORIGINAL-FLOOR-CAPACITY PASS: five unchanged originals/all14 effects, {acquisitions.Length} acquisitions, {energy.Length} energy bodies, {draws} actual draw6 applications, {queries} capacity queries, {initializations} real room initializations, legal size6/6/2/1 capacity15 versus freshly initialized13, {pyreCombats} native Pyre attack exchanges, full battle and immutable branches.");
        return witnesses;
    }

    private static void CheckAcquisitions(Operation[] acquisitions, Action<string, int> witness)
    {
        for (int index = 0; index < acquisitions.Length; index++)
        {
            Operation row = acquisitions[index];
            Require(row.EffectIndex == -1 && !row.Deployment && !row.Before.Preview && !row.After.Preview &&
                row.Before.Turn == 0 && row.After.Turn == 0, "Original capacity acquisition is not a real pre-battle add.");
            CheckSnapshot(row.Before); CheckSnapshot(row.After);
            Require(!row.Before.Relics.Any(owner => owner.DataId == row.OwnerDataId), "Capacity owner was already installed before its native acquisition.");
            Equal(row.Before.Relics, row.After.Relics.Where(owner => owner.DataId != row.OwnerDataId).ToArray(), "capacity acquisition other owners");
            Equal(row.Before.Resources, row.After.Resources, "capacity acquisition raw resources");
            if (index > 0) Equal(acquisitions[index - 1].After, row.Before, "native capacity acquisition chain");
            CombatContext context = Context(row.After);
            int ownerIndex = Array.FindIndex(row.After.Relics, owner => owner.DataId == row.OwnerDataId);
            RelicPyreResourceResult acquired = RelicResourceModel.Acquire(context, ownerIndex, row.Before.PyreHP);
            Require(acquired.Supported && acquired.Health == row.After.PyreHP && acquired.MaxHealth == row.After.MaxHP,
                "Original capacity acquisition HP/max differ: " + acquired.UnsupportedReason);
            foreach (RelicResourceEffect rule in row.After.Relics[ownerIndex].ResourceEffects!)
            {
                if (rule.Type == RelicResourceModel.FloorCapacity)
                    Require(row.After.Capacity - row.Before.Capacity == 2,
                        "Original capacity add did not register its real +2 mutable rule.");
                else if (rule.Type == RelicResourceModel.Draw)
                    Require(row.After.Draw - row.Before.Draw == 1, "Purge champion draw add lacks its positive native registered mutation.");
                else if (rule.Type == RelicResourceModel.PyreHealth)
                {
                    Require(rule.Amount > 0 && row.After.MaxHP - row.Before.MaxHP == rule.Amount && row.After.PyreHP - row.Before.PyreHP == rule.Amount,
                        "Original Boss capacity HP effect did not actually increase both current/max Pyre health.");
                    witness(row.OwnerDataId, rule.EffectIndex);
                }
                else if (rule.Type == RelicResourceModel.PyreAttack)
                {
                    Require(rule.Amount > 0 && row.After.PyreAttack - row.Before.PyreAttack == rule.Amount,
                        "Original Boss capacity attack add lacks its table-indexed native contribution.");
                    witness(row.OwnerDataId, rule.EffectIndex);
                }
            }
        }
        CheckDefinitions(acquisitions[^1].After.Resources, acquisitions[^1].After.Relics, true);
    }

    private static void CheckEnergy(Operation[] rows, Snapshot installed, Action<string, int> witness)
    {
        Require(rows.Length > 0, "Purge champion reward energy effects never ran native callback bodies.");
        foreach (Operation row in rows)
        {
            CheckSnapshot(row.Before); CheckSnapshot(row.After);
            Require(row.OwnerDataId == Artifacts[0].Id && !row.Before.Preview && !row.After.Preview &&
                row.Before.Phase == "MonsterTurn" && row.Before.Phase == row.After.Phase && row.Before.Turn == row.After.Turn &&
                row.EffectIndex == (row.Deployment ? 1 : 0) && (!row.Deployment || row.Before.Turn == 0),
                "Purge champion energy body has the wrong owner, original effect or timing.");
            RelicResourceEffect rule = row.Before.Relics.Single(owner => owner.DataId == row.OwnerDataId).ResourceEffects!
                .Single(rule => rule.EffectIndex == row.EffectIndex);
            Require(row.After.Energy == Math.Min(row.Before.Resources.MaxEnergy, unchecked(row.Before.Energy + 1)) &&
                row.After.Energy > row.Before.Energy && row.Notifications == (row.Deployment ? 0 : 1),
                "Original energy +1 was capped away or native callback notification differs.");
            RelicResourceEffect isolatedRule = new(0, rule.Type, rule.ParamInt, rule.ParamInt2, rule.Amount,
                rule.CanApplyInPreviewMode, rule.RequiredMutatorId, rule.Conditions);
            CombatRelicState isolatedOwner = new(row.OwnerDataId, Artifacts[0].Key, [rule.Type], isCovenant: false,
                disallowedInPlacementPhase: false, resourceEffects: [isolatedRule]);
            CombatContext context = Context(row.Before).WithRelics([isolatedOwner]);
            string parent = Serialize(context);
            RelicResourceResult applied = RelicResourceModel.ApplyEnergyTiming(context, row.Deployment, placement: row.Before.Turn == 0);
            Require(applied.Supported && applied.Value == row.After.Energy && applied.Events.Count == 1 &&
                applied.Events[0].EffectIndex == 0 && applied.Events[0].RelicId == row.OwnerDataId &&
                applied.Events[0].Notified == !row.Deployment, "Independent single-effect original energy body differs: " + applied.UnsupportedReason);
            RelicResourceResult without = RelicResourceModel.ApplyEnergyTiming(context.WithRelics([]), row.Deployment, placement: row.Before.Turn == 0);
            Require(without.Supported && without.Value == row.Before.Energy && without.Events.Count == 0 && Serialize(context) == parent,
                "Original energy ablation changed a source or guessed a contribution.");
            Equal(row.Before.Resources, row.After.Resources, "native energy raw inputs");
            Equal(row.Before.Relics, row.After.Relics, "unconditional native energy owner state");
            Require(row.Before.Draw == row.After.Draw && row.Before.PyreHP == row.After.PyreHP && row.Before.MaxHP == row.After.MaxHP &&
                row.Before.PyreAttack == row.After.PyreAttack && row.Before.Gold == row.After.Gold && row.Before.Capacity == row.After.Capacity,
                "Energy body unexpectedly changed another live resource.");
            witness(row.OwnerDataId, row.EffectIndex);
        }
        Operation[] deploy = rows.Where(row => row.Deployment).ToArray();
        Operation[] opening = rows.Where(row => !row.Deployment && row.Before.Turn == 0).ToArray();
        Require(deploy.Length == 1 && opening.Length == 1 && deploy[0].Before.Energy == installed.Resources.BaseEnergy + installed.Resources.DeploymentExtraEnergy &&
            deploy[0].After.Energy == opening[0].Before.Energy && opening[0].After.Energy == installed.Resources.BaseEnergy + installed.Resources.DeploymentExtraEnergy + 2,
            "Original +1 deployment then +1 ordinary callbacks lack their real ordered bridge.");
        Require(rows.Any(row => !row.Deployment && row.Before.Turn > 0), "Original normal energy has no post-deployment callback.");
        foreach (Operation row in rows.Where(row => !row.Deployment && row.Before.Turn > 0))
            Require(row.Before.Energy == installed.Resources.BaseEnergy && row.After.Energy == installed.Resources.BaseEnergy + 1,
                "Purge champion normal energy lacks the actual base-to-base+1 turn body.");
    }

    private static int CheckCapacityQueries(FixtureValue records)
    {
        FixtureValue[] rows = records.EnumerateArray().ToArray();
        Require(rows.Length > 0, "Native floor capacity getter was never captured.");
        int live = 0, previews = 0;
        var removals = new HashSet<string>();
        void Check(FixtureValue row)
        {
            BattleResourceState resources = row.GetProperty("Resources").Deserialize<BattleResourceState>()!;
            CombatRelicState[] relics = row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!;
            string? removed = NullableString(row.GetProperty("OwnerRemovalDataId"));
            CheckDefinitions(resources, relics, false, removed);
            RoomCapacityState[] before = row.GetProperty("RoomCapacitiesBefore").Deserialize<RoomCapacityState[]>()!;
            RoomCapacityState[] after = row.GetProperty("RoomCapacitiesAfter").Deserialize<RoomCapacityState[]>()!;
            CombatContext context = QueryContext(resources, relics, before);
            string parent = Serialize(context);
            RelicResourceResult result = RelicResourceModel.GetFloorCapacity(context);
            int actual = row.GetProperty("Actual").GetInt32();
            Require(result.Supported && result.Value == actual && actual == resources.BaseFloorCapacity!.Value +
                relics.SelectMany(owner => owner.ResourceEffects ?? []).Where(rule => rule.Type == RelicResourceModel.FloorCapacity && rule.CapacityMutationOrder.HasValue).Sum(rule => rule.Amount),
                "Native capacity getter differs from independently captured base/registered original values: " + result.UnsupportedReason);
            Require(ReferenceEquals(result.Context, context) && result.Events.Count == 0 && Serialize(context) == parent &&
                row.GetProperty("Notifications").GetInt32() == 0, "Native capacity getter notified or changed conditions/source/RNG.");
            Equal(before, after, "capacity getter retained already initialized room maxima");
        }
        foreach (FixtureValue row in rows)
        {
            Check(row);
            string? removed = NullableString(row.GetProperty("OwnerRemovalDataId"));
            bool matrix = row.GetProperty("Matrix").GetBoolean();
            CombatRelicState[] relics = row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!;
            bool complete = Artifacts.All(owner => relics.Any(relic => relic.DataId == owner.Id));
            if (complete && removed == null)
            {
                Require(row.GetProperty("Actual").GetInt32() == 15 && removed == null, "Native full capacity getter lacks five +2 originals and real base5.");
                if (row.GetProperty("Preview").GetBoolean()) previews++;
                else if (!matrix) live++;
            }
            if (removed != null)
            {
                Require(matrix && Artifacts.Any(owner => owner.Id == removed) && Artifacts.All(owner => relics.Any(relic => relic.DataId == owner.Id)) &&
                    relics.Single(owner => owner.DataId == removed).ResourceEffects!.Single(rule => rule.Type == RelicResourceModel.FloorCapacity).CapacityMutationOrder == null &&
                    row.GetProperty("Actual").GetInt32() == 13,
                    "Native scratch-registration capacity ablation lacks one exact unregistered capacity mutation or actual13.");
                RoomCapacityState[] capacities = row.GetProperty("RoomCapacitiesBefore").Deserialize<RoomCapacityState[]>()!;
                Require(capacities.Length > 0 && capacities.All(room => room.PlayerMaximum == 15 && room.EnemyMaximum == 15),
                    "Capacity getter ablation recomputed a live room instead of preserving its initialized15.");
                removals.Add(removed);
            }
        }
        Require(live > 0 && previews > 0 && removals.Count == 5, "Native capacity evidence lacks actual/preview full getter or all five scratch owner removals.");
        string source = Serialize(rows.Select(row => row.ToObjectGraph()).ToArray());
        Parallel.For(0, 32, _ => { foreach (FixtureValue row in rows) Check(row); });
        Require(Serialize(rows.Select(row => row.ToObjectGraph()).ToArray()) == source, "32 parallel capacity queries mutated their fixture/source state.");
        return rows.Length;
    }

    private static void CheckMatrixRestorations(FixtureValue records)
    {
        FixtureValue[] rows = records.EnumerateArray().ToArray();
        Require(rows.Length == 1, "Native capacity matrix lacks its single restoration proof.");
        foreach (FixtureValue row in rows)
        {
            Require(row.GetProperty("MutableRulesObjectRestored").GetBoolean() && row.GetProperty("MutationReferencesAndOrderRestored").GetBoolean() &&
                !row.GetProperty("PreviewBefore").GetBoolean() && !row.GetProperty("PreviewAfter").GetBoolean(),
                "Native capacity matrix failed to restore the original rules object, registered references/order or preview save.");
            foreach (string name in new[] { "RoomCapacities", "Rng", "UnityRng" })
                Equal(row.GetProperty(name + "Before").ToObjectGraph(), row.GetProperty(name + "After").ToObjectGraph(), "native capacity matrix restored " + name);
            Require(row.GetProperty("RoomCapacitiesBefore").GetArrayLength() > 0 && row.GetProperty("RngBefore").GetArrayLength() > 0 &&
                row.GetProperty("UnityRngBefore").GetArrayLength() == 4, "Native capacity restoration omitted rooms or RNG words.");
        }
    }

    private static int CheckInitializations(FixtureValue records)
    {
        FixtureValue[] rows = records.EnumerateArray().ToArray();
        Require(rows.Length > 0, "Native room setup initialized no actual team capacity.");
        var seen = new HashSet<int>();
        foreach (FixtureValue row in rows)
        {
            Snapshot raw = ReadSnapshot(row.GetProperty("Before"));
            CheckSnapshot(raw); CheckDefinitions(raw.Resources, raw.Relics, true);
            RoomCapacityState actual = row.GetProperty("Actual").Deserialize<RoomCapacityState>()!;
            int room = row.GetProperty("RoomIndex").GetInt32();
            bool pyre = row.GetProperty("IsPyre").GetBoolean();
            Require(actual.RoomIndex == room && actual.IsPyre == pyre && seen.Add(room) &&
                row.GetProperty("PlayerSlots").GetInt32() > 0 && row.GetProperty("EnemySlots").GetInt32() > 0,
                "Native room setup lacks unique indices, team spawn slots or the real Pyre flag.");
            CombatContext source = Context(raw).WithRoomCapacities([new(room, 0, 0, pyre)]);
            string parent = Serialize(source);
            RoomCapacityResult initialized = RelicResourceModel.InitializeRoomCapacities(source);
            Require(initialized.Supported && initialized.Amount == 15, "Original room initialization unsupported: " + initialized.UnsupportedReason);
            Equal(initialized.Context!.RoomCapacities!.Single(), actual, "native two-team initialized room maxima");
            Require(Serialize(source) == parent, "Room initialization changed its raw input/source.");
        }
        Require(seen.Count >= 2 && rows.Any(row => row.GetProperty("IsPyre").GetBoolean()) && rows.Any(row => !row.GetProperty("IsPyre").GetBoolean()),
            "Original capacity setup lacks real playable and Pyre room initialization.");
        return rows.Length;
    }

    private static void CheckCapacityConsumers(FixtureValue[] actions, Action<string, int> witness)
    {
        FixtureValue[] summons = actions.Where(row => IsCarrier(Rule(row.GetProperty("Before").Deserialize<BattleTurnState>()!,
            row.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId))).ToArray();
        Require(summons.Length == 4, "Capacity scene did not naturally play its four carrier cards exactly once.");
        BattleTurnState empty = summons[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        PlayCardAction[] plays = summons.Select(row => row.GetProperty("Action").Deserialize<PlayCardAction>()!).ToArray();
        int floor = plays[0].RoomIndex;
        Require(plays.All(play => play.RoomIndex == floor) &&
            summons.All(row => row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() == empty.Spawn.Turn) &&
            empty.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units.All(unit => unit.Team != CombatTeam.Player) &&
            plays.Select(play => Rule(empty, play.CardInstanceId).SpawnUnit!.Size).SequenceEqual([6, 6, 2, 1]),
            "Capacity evidence needs a legal empty-floor native size6/6/2/1 prefix in one deployment turn.");
        BattleTurnState full = summons[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit[] players = full.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
        Require(players.Select(unit => unit.Size).SequenceEqual([6, 6, 2, 1]) && players.Sum(unit => unit.Size) == 15 &&
            RoomCapacityModel.Maximum(full.Spawn.Train.Context, floor, CombatTeam.Player) == 15,
            "The recorded floor never actually used all15 registered capacity with four living carriers.");
        foreach (Artifact artifact in Artifacts)
        {
            CombatContext original = empty.Spawn.Train.Context!;
            CombatContext removed = Remove(original, artifact.Id);
            string parent = Serialize(original);
            RelicResourceResult getter = RelicResourceModel.GetFloorCapacity(removed);
            Require(getter.Supported && getter.Value == 13, "Original capacity owner ablation lacks real getter contribution2.");
            Equal(original.RoomCapacities, getter.Context!.RoomCapacities, "getter removal retained born room capacity15");
            // New initialization starts before any player enters this floor. The native
            // populated15 floor above is never shrunk into an invalid counterfactual.
            CombatContext fresh = removed.WithRoomCapacities(original.RoomCapacities!.Select(room => new RoomCapacityState(room.RoomIndex, 0, 0, room.IsPyre)).ToArray());
            RoomCapacityResult initialized = RelicResourceModel.InitializeRoomCapacities(fresh);
            Require(initialized.Supported && initialized.Amount == 13 && initialized.Context!.RoomCapacities!.All(room =>
                room.PlayerMaximum == 13 && room.EnemyMaximum == 13), "Owner removal failed fresh two-team room initialization13.");
            BattleTurnState branch = WithContext(empty, initialized.Context!);
            Require(branch.Spawn.Train.Rooms.All(room => Enum.GetValues<CombatTeam>().All(team =>
                room.Units.Where(unit => unit.Team == team).Sum(unit => (long)unit.Size) <= (RoomCapacityModel.Maximum(branch.Spawn.Train.Context, room.RoomIndex, team) ?? 0))),
                "Capacity owner ablation constructed an already overfull counterfactual root.");
            for (int index = 0; index < 2; index++)
            {
                BattleActionResult prefix = BattleActionModel.PlayCard(branch, plays[index]);
                Require(prefix.Supported, "Legal size6/6 prefix failed at new capacity13: " + prefix.Reason);
                branch = prefix.State!;
            }
            Require(branch.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units.Where(unit => unit.Team == CombatTeam.Player).Sum(unit => unit.Size) == 12,
                "Legal capacity13 branch did not really deploy size12 before rejection.");
            BattleActionResult blocked = BattleActionModel.PlayCard(branch, plays[2]);
            Require(!blocked.Supported && blocked.Rejection == ActionRejection.Illegal &&
                blocked.Reason?.Contains("size", StringComparison.Ordinal) == true,
                "Removing one original did not reject the native size2 summon from legal12 at freshly initialized13.");
            Require(Serialize(original) == parent, "Capacity owner removal or fresh summon branch changed its native source.");
            RelicResourceEffect rule = original.Relics!.Single(owner => owner.DataId == artifact.Id).ResourceEffects!
                .Single(rule => rule.Type == RelicResourceModel.FloorCapacity);
            witness(artifact.Id, rule.EffectIndex);
        }
    }

    private static int CheckDraws(FixtureValue root, Action<string, int> witness)
    {
        int positive = 0;
        foreach (FixtureValue row in root.GetProperty("CardCycles").EnumerateArray().Where(row => row.GetProperty("Kind").GetString() == "Draw"))
        {
            CombatContext context = row.GetProperty("StandbyContext").Deserialize<CombatContext>()!;
            CheckDefinitions(context.Resources!, context.Relics!, true);
            CardCycleState before = row.GetProperty("Before").Deserialize<CardCycleState>()!;
            CardCycleState actual = row.GetProperty("Actual").Deserialize<CardCycleState>()!;
            RelicResourceResult count = RelicResourceModel.GetDrawCount(context);
            Require(count.Supported && count.Value == 6 && row.GetProperty("HandSize").GetInt32() == 6 &&
                row.GetProperty("MaxHandSize").GetInt32() == context.MaxHandSize, "Original Purge champion lacks native ordinary draw6.");
            Equal(Cycle(context.Cards), Cycle(before), "native capacity draw input/context");
            CardCycleState prepared = EquipmentModel.ReturnStandby(context, row.GetProperty("LivingUnitIds").Deserialize<int[]>()!).Cards;
            prepared = new(prepared.Hand, prepared.Draw, prepared.Discard, prepared.Rng, prepared.DrawModifier,
                before.ExternalInteractions, prepared.BonusDraw, prepared.NextDrawUpgrades, prepared.NextUpgradeReferenceId);
            string parent = Serialize(context);
            CardCycleResult replay = CardCycleModel.DrawHand(prepared, 6, context.MaxHandSize);
            Require(replay.Supported, "Original capacity draw unsupported: " + replay.UnsupportedReason);
            Equal(Cycle(replay.State!), Cycle(actual), "complete native ordinary draw6 piles/RNG");
            var held = prepared.Hand.Select(card => card.InstanceId).ToHashSet();
            int added = actual.Hand.Count(card => !held.Contains(card.InstanceId));
            if (prepared.Hand.Count == 0 && prepared.DrawModifier == 0 && prepared.Draw.Count + prepared.Discard.Count >= 6)
            {
                RelicResourceResult fewer = RelicResourceModel.GetDrawCount(Remove(context, Artifacts[0].Id));
                CardCycleResult ablated = CardCycleModel.DrawHand(prepared, fewer.Value, context.MaxHandSize);
                Require(fewer.Supported && fewer.Value == 5 && ablated.Supported && added == 6 &&
                    ablated.State!.Hand.Count(card => !held.Contains(card.InstanceId)) == 5,
                    "Original +1 draw did not actually add a sixth unique card under uncapped owner removal.");
                witness(Artifacts[0].Id, 3); positive++;
            }
            Require(Serialize(context) == parent, "Draw replay or owner ablation mutated its native source.");
        }
        Require(positive > 0, "Purge champion draw lacks actual draw6 versus5 with sufficient deck and empty hand.");
        return positive;
    }

    private static int CheckCombat(FixtureValue[] stages, Action<string, int> witness)
    {
        int pyreCombats = 0, carrierCombats = 0;
        foreach (FixtureValue row in stages)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CheckRoom(before); CheckRoom(actual);
            string parent = Serialize(before);
            string kind = row.GetProperty("Kind").GetString()!;
            Require(kind is "Exchange" or "Room", "Unknown original capacity combat kind.");
            RoomCombatResult Run(RoomCombatState state) => kind == "Exchange" ? RoomCombatModel.Exchange(state) : RoomCombatModel.Resolve(state);
            RoomCombatResult result = Run(before);
            Require(result.Supported, "Original capacity combat unsupported: " + result.UnsupportedReason);
            Equal(result.State, actual, "complete native original capacity combat");
            CombatUnit[] carriers = before.Units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).ToArray();
            if (carriers.Any(unit => actual.Units.FirstOrDefault(after => after.Id == unit.Id) is CombatUnit after && after.Health < unit.Health) ||
                result.Events.Any(effect => effect.Kind == "Attack" && effect.Amount > 0 && carriers.Any(unit => unit.Id == effect.Actor)))
                carrierCombats++;
            CombatUnit? pyre = before.Units.SingleOrDefault(unit => unit.IsPyre);
            if (pyre != null && result.Events.Any(effect => effect.Kind == "Attack" && effect.Actor == pyre.Id && effect.Amount > 0) &&
                before.Units.Any(unit => unit.Team == CombatTeam.Enemy && (actual.Units.FirstOrDefault(after => after.Id == unit.Id)?.Health ?? 0) < unit.Health))
            {
                pyreCombats++;
                foreach (Artifact artifact in Artifacts.Where(owner => owner.PyreIndex >= 0))
                {
                    RelicResourceEffect rule = before.Context!.Relics!.Single(owner => owner.DataId == artifact.Id).ResourceEffects!
                        .Single(rule => rule.Type == RelicResourceModel.PyreAttack);
                    RoomCombatState weaker = new(before.RoomIndex, before.Deployment,
                        before.Units.Select(unit => unit.Id == pyre.Id ? WeakenPyre(unit, rule.Amount) : unit).ToArray(),
                        before.ExternalInteractions, Remove(before.Context, artifact.Id), before.Preview);
                    RoomCombatResult ablated = Run(weaker);
                    Require(ablated.Supported, "Original capacity Pyre attack ablation unsupported: " + ablated.UnsupportedReason);
                    if (result.Events.Any(effect => effect.Kind == "Attack" && effect.Actor == pyre.Id && effect.Amount > 0 &&
                        ablated.Events.Any(other => other.Kind == effect.Kind && other.Actor == effect.Actor && other.Target == effect.Target &&
                            other.Round == effect.Round && other.Amount < effect.Amount))) witness(artifact.Id, rule.EffectIndex);
                }
            }
            Require(Serialize(before) == parent, "Native capacity combat or owner ablation changed its parent.");
        }
        Require(carrierCombats > 0, "Original capacity batch lacks actual carrier combat.");
        return pyreCombats;
    }

    private static void CheckSnapshot(Snapshot row)
    {
        CheckDefinitions(row.Resources, row.Relics, false);
        CombatContext context = Context(row);
        string parent = Serialize(context);
        RelicResourceResult energy = RelicResourceModel.GetEnergyModification(context, preview: row.Preview);
        RelicResourceResult deployment = RelicResourceModel.GetEnergyModification(context, deployment: true, preview: row.Preview);
        RelicResourceResult draw = RelicResourceModel.GetDrawCount(context);
        RelicResourceResult health = RelicResourceModel.GetPyreMaxHealth(context);
        RelicResourceResult attack = RelicResourceModel.GetPyreAttack(context, placement: row.Turn == 0);
        RelicResourceResult capacity = RelicResourceModel.GetFloorCapacity(context);
        var rules = row.Relics.SelectMany(owner => owner.ResourceEffects ?? []).ToArray();
        Require(energy.Supported && energy.Value == row.EnergyModification && energy.Value == (row.Preview ? 0 : rules.Where(rule => rule.Type == RelicResourceModel.Energy).Sum(rule => rule.Amount)) &&
            deployment.Supported && deployment.Value == row.DeploymentModification && deployment.Value == (row.Preview ? 0 : rules.Where(rule => rule.Type == RelicResourceModel.DeploymentEnergy).Sum(rule => rule.Amount)) &&
            draw.Supported && draw.Value == row.Draw && draw.Value == Math.Clamp(row.Resources.BaseDraw + rules.Where(rule => rule.Type == RelicResourceModel.Draw && rule.DrawMutationOrder.HasValue).Sum(rule => rule.Amount), 1, row.Resources.MaxHandSize) &&
            health.Supported && health.Value == row.MaxHP && health.Value == row.Resources.BasePyreMaxHealth + rules.Where(rule => rule.Type == RelicResourceModel.PyreHealth).Sum(rule => row.Resources.UpgradeHealth[rule.ParamInt2]) &&
            attack.Supported && attack.Value == row.PyreAttack && attack.Value == row.Resources.PyreBaseAttack + row.Resources.PyrePersistentDamage + rules.Where(rule => rule.Type == RelicResourceModel.PyreAttack).Sum(rule => row.Resources.UpgradeAttack[rule.ParamInt2]) &&
            capacity.Supported && capacity.Value == row.Capacity && row.Capacity == row.Resources.BaseFloorCapacity!.Value + rules.Where(rule => rule.Type == RelicResourceModel.FloorCapacity && rule.CapacityMutationOrder.HasValue).Sum(rule => rule.Amount),
            "Native capacity resource snapshot differs from captured original parameters, registrations and selected Pyre tables.");
        foreach (RelicResourceResult result in new[] { energy, deployment, draw, health, attack, capacity })
            Require(Serialize(result.Context) == parent && result.Events.Count == 0, "Original resource getter changed its source/conditions/RNG or notified.");
        Require(Serialize(context) == parent, "Original resource snapshot queries changed a parent.");
    }

    private static void CheckDefinitions(BattleResourceState raw, IReadOnlyList<CombatRelicState> relics, bool complete,
        string? unregisteredCapacityOwner = null)
    {
        Require(raw.BaseFloorCapacity == 5 && raw.BaseDraw == 5 && raw.MaxHandSize >= 6 && raw.BaseEnergy > 0 && raw.DeploymentExtraEnergy >= 0 &&
            raw.MaxEnergy > raw.BaseEnergy + raw.DeploymentExtraEnergy + 2 && raw.BasePyreMaxHealth > 0 && raw.UpgradeAttack.Count >= 2 &&
            raw.UpgradeHealth.Count >= 2 && raw.UpgradeAttack.Take(2).All(value => value > 0) && raw.UpgradeHealth.Take(2).All(value => value > 0),
            "Original capacity batch lacks captured unchanged Balance and selected Pyre definition inputs.");
        foreach (Artifact artifact in Artifacts)
        {
            CombatRelicState[] owners = relics.Where(owner => owner.DataId == artifact.Id).ToArray();
            Require(owners.Length <= 1 && (!complete || owners.Length == 1), "Missing or duplicate capacity original: " + artifact.Key);
            if (owners.Length == 0) continue;
            CombatRelicState owner = owners[0];
            var expected = Definition(artifact);
            Require(owner.AssetKey == artifact.Key && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(expected.Select(rule => rule.Type)) && owner.ResourceEffects?.Count == expected.Length,
                "Original capacity owner or full ordered effect list changed: " + artifact.Key);
            int capacityOrder = Artifacts.TakeWhile(other => other.Id != artifact.Id).Count(other => other.Id != unregisteredCapacityOwner);
            for (int index = 0; index < expected.Length; index++)
            {
                RelicResourceEffect rule = owner.ResourceEffects![index];
                var definition = expected[index];
                int amount = definition.Type == RelicResourceModel.PyreHealth ? raw.UpgradeHealth[definition.Int2] :
                    definition.Type == RelicResourceModel.PyreAttack ? raw.UpgradeAttack[definition.Int2] : definition.Int;
                Require(rule.EffectIndex == index && rule.Type == definition.Type && rule.ParamInt == definition.Int && rule.ParamInt2 == definition.Int2 &&
                    rule.Amount == amount && !rule.CanApplyInPreviewMode && rule.RequiredMutatorId == null && rule.Conditions.Count == 0,
                    "Original capacity effect initialized parameter/conditions/preview gate changed: " + artifact.Key + "/" + index);
                Require(rule.Type == RelicResourceModel.Draw ? rule.DrawValue == 1f && rule.DrawMutationOrder == 0 :
                    rule.DrawValue == null && rule.DrawMutationOrder == null, "Original capacity draw registration metadata differs.");
                Require(rule.Type == RelicResourceModel.FloorCapacity ? rule.CapacityValue == 2f &&
                    rule.CapacityMutationOrder == (artifact.Id == unregisteredCapacityOwner ? null : (int?)capacityOrder) :
                    rule.CapacityValue == null && rule.CapacityMutationOrder == null, "Original floor capacity float/ref registration order differs.");
            }
        }
        Require(relics.Where(owner => owner.ResourceEffects?.Count > 0).All(owner => Artifacts.Any(artifact => artifact.Id == owner.DataId)),
            "Additional resource owners confound the five exact original capacity definitions.");
    }

    private static void CheckOriginalDefinitions(FixtureValue records)
    {
        FixtureValue[] owners = records.EnumerateArray().ToArray();
        Require(owners.Length == 5 && owners.Select(row => row.GetProperty("OwnerDataId").GetString()).SequenceEqual(Artifacts.Select(owner => owner.Id)),
            "Original raw capacity definitions lack the exact five catalog owners in scene order.");
        for (int index = 0; index < owners.Length; index++)
        {
            FixtureValue owner = owners[index]; Artifact artifact = Artifacts[index];
            Require(owner.GetProperty("AssetKey").GetString() == artifact.Key && !owner.GetProperty("DisallowedInPlacementPhase").GetBoolean(),
                "Original capacity raw owner identity/placement restriction changed.");
            FixtureValue[] effects = owner.GetProperty("Effects").EnumerateArray().ToArray();
            var definition = Definition(artifact);
            Require(effects.Length == definition.Length, "Original capacity raw effect count changed.");
            for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
            {
                FixtureValue effect = effects[effectIndex]; var expected = definition[effectIndex];
                Require(effect.GetProperty("EffectIndex").GetInt32() == effectIndex && effect.GetProperty("Type").GetString() == expected.Type &&
                    effect.GetProperty("ParamInt").GetInt32() == expected.Int && effect.GetProperty("ParamInt2").GetInt32() == expected.Int2 &&
                    effect.GetProperty("SourceTeam").GetInt32() == 0 && !effect.GetProperty("ParamBool").GetBoolean() &&
                    !effect.GetProperty("ParamBool2").GetBoolean() && effect.GetProperty("ParamString").GetString() == "" &&
                    effect.GetProperty("ConditionCount").GetInt32() == 0 && effect.GetProperty("CharacterFilterCount").GetInt32() == 0 &&
                    effect.GetProperty("CharacterCount").GetInt32() == 0 && NullableString(effect.GetProperty("RequiredMutatorId")) == null &&
                    NullableString(effect.GetProperty("CardUpgradeId")) == null && NullableString(effect.GetProperty("CharacterSubtypeKey")) == null &&
                    effect.GetProperty("ExcludedSubtypeCount").GetInt32() == 0 && effect.GetProperty("StatusEffectCount").GetInt32() == 0 &&
                    effect.GetProperty("CardEffectCount").GetInt32() == 0,
                    "Original capacity raw full parameters/filters/conditions differ from the untouched catalog: " + artifact.Key + "/" + effectIndex);
            }
        }
    }

    private static void CheckDeploymentCards(FixtureValue records)
    {
        FixtureValue[] cards = records.EnumerateArray().ToArray();
        int[] sizes = cards.Select(card => card.GetProperty("Size").GetInt32()).Order().ToArray();
        Require(cards.Length == 4 && sizes.SequenceEqual([1, 2, 6, 6]) &&
            cards.Select(card => card.GetProperty("CardDataId").GetString()).Distinct().Count() == 4 &&
            cards.All(card => card.GetProperty("ActualDeployable").GetBoolean() &&
                card.GetProperty("DeploymentUpgradeIds").EnumerateArray().Select(value => value.GetString())
                    .SequenceEqual(["fc119000-0000-4000-8000-000000000100"]) &&
                card.GetProperty("DeploymentTraitNames").EnumerateArray().Select(value => value.GetString())
                    .SequenceEqual(["CardTraitIgnoreArmor"]) &&
                card.GetProperty("DeploymentTraitFlags").EnumerateArray().Select(value => value.GetBoolean()).SequenceEqual([true])),
            "The four size6/6/2/1 carriers are not natively deployable through the recorded deployment upgrade trait.");
    }

    private static (string Type, int Int, int Int2)[] Definition(Artifact artifact) => artifact.Key == "PurgeChampionReward"
        ? [(RelicResourceModel.Energy, 1, 0), (RelicResourceModel.DeploymentEnergy, 1, 0), (RelicResourceModel.FloorCapacity, 2, 0), (RelicResourceModel.Draw, 1, 0)]
        : artifact.PyreIndex >= 0 ? [(RelicResourceModel.FloorCapacity, 2, 0), (RelicResourceModel.PyreHealth, 30, artifact.PyreIndex), (RelicResourceModel.PyreAttack, 10, artifact.PyreIndex)]
        : [(RelicResourceModel.FloorCapacity, 2, 0)];
    private static Snapshot ReadSnapshot(FixtureValue value) => new(value.GetProperty("Resources").Deserialize<BattleResourceState>()!,
        value.GetProperty("Relics").Deserialize<CombatRelicState[]>()!, value.GetProperty("Energy").GetInt32(), value.GetProperty("Draw").GetInt32(),
        value.GetProperty("PyreHP").GetInt32(), value.GetProperty("MaxHP").GetInt32(), value.GetProperty("PyreAttack").GetInt32(),
        value.GetProperty("EnergyModification").GetInt32(), value.GetProperty("DeploymentModification").GetInt32(),
        value.GetProperty("Phase").GetString()!, value.GetProperty("Turn").GetInt32(), value.GetProperty("Preview").GetBoolean(), value.GetProperty("Gold").GetInt32(),
        value.GetProperty("Capacity").GetInt32());
    private static Operation ReadOperation(FixtureValue row)
    {
        Require(row.GetProperty("After").ValueKind != FixtureKind.Null, "Unfinished native capacity resource operation.");
        return new(row.GetProperty("Kind").GetString()!, row.GetProperty("OwnerDataId").GetString()!, row.GetProperty("EffectIndex").GetInt32(),
            row.GetProperty("Deployment").GetBoolean(), row.GetProperty("Notifications").GetInt32(), ReadSnapshot(row.GetProperty("Before")), ReadSnapshot(row.GetProperty("After")));
    }
    private static CombatContext Context(Snapshot row) => new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(17), row.Gold, 1,
        row.Resources.MaxHandSize, statistics: BattleStatistics.Empty(deckCards: []), otherPiles: [],
        queryFrame: new(energy: row.Energy, runningCombat: true, turn: row.Turn),
        energyState: new(row.Resources.MaxEnergy, 0, 0, row.Phase, row.PyreHP > 0), relics: row.Relics, resources: row.Resources);
    private static CombatContext QueryContext(BattleResourceState raw, CombatRelicState[] relics, RoomCapacityState[] capacities) =>
        new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(17), 0, 1, raw.MaxHandSize, statistics: BattleStatistics.Empty(deckCards: []),
            otherPiles: [], queryFrame: new(energy: 0, runningCombat: true), energyState: new(raw.MaxEnergy, 0, 0, "MonsterTurn", true),
            roomCapacities: capacities, relics: relics, resources: raw);
    private static CombatContext Remove(CombatContext source, string id) => source.WithRelics(source.Relics!.Where(owner => owner.DataId != id).ToArray());
    private static BattleTurnState WithContext(BattleTurnState source, CombatContext context)
    {
        EnemySpawnState spawn = source.Spawn;
        TrainCombatState train = CardSpellModel.WithContext(spawn.Train, context);
        EnemySpawnState changed = new(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, spawn.Rng, spawn.NextUnitId,
            spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn, spawn.FirstTreasureRoom, spawn.Turn,
            spawn.ExternalInteractions, spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds);
        return new(changed, source.Energy, source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints, source.DragonsHoard, source.MoonPhase,
            source.RngStreams, source.OtherPiles, source.ExternalInteractions, source.PlayRules, source.BattlePreviewEnabled, source.UiRngIsolated,
            source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences, source.SelectedRoom);
    }
    private static CombatUnit WeakenPyre(CombatUnit unit, int amount)
    {
        UnitModifiers raw = unit.Modifiers!;
        UnitModifiers changed = new(unchecked(raw.AttackDamage - amount), raw.AttackDamageAdded, raw.DamageBuff, raw.RawSize,
            raw.EquipmentLimit, raw.CanBeHealed, raw.IsClone, raw.Upgrades, raw.HealthFromUpgrades, raw.SpawnerMatchesDefinition, raw.DamageBuffFromStatusEffects);
        return new(unit.Id, unit.AssetKey, unit.Team, unchecked(unit.BaseAttack - amount), unit.Health, unit.MaxHealth, unit.CanAttack, unit.IsPyre,
            unit.EndsBattleOnDeath, unit.Statuses, unit.Triggers, unit.SpawnerCardId, unit.Size, unit.StatusImmunities, unit.Subtypes, changed,
            unit.IsBoss, unit.LastAttackerId, unit.StatusRegistry, unit.EquipmentCards, unit.NextTriggerId, unit.Ability, unit.StatusDictionary,
            unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning, unit.SacrificeCardId, unit.DeathState, bumpRules: unit.BumpRules);
    }
    private static CombatUnit[] Units(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
    private static void CheckBattle(BattleTurnState state)
    {
        Require(state.Spawn.Train.Context?.Resources != null, "Original capacity battle omitted native raw resources.");
        CheckDefinitions(state.Spawn.Train.Context!.Resources!, state.Spawn.Train.Context.Relics!, true);
        foreach (RoomCombatState room in state.Spawn.Train.Rooms) CheckRoom(room);
    }
    private static void CheckRoom(RoomCombatState room)
    {
        Require(room.Context?.Resources != null && room.Context.RoomCapacities != null, "Original capacity room omitted actual initialized maxima/raw resources.");
        CheckDefinitions(room.Context!.Resources!, room.Context.Relics!, true);
        Require(room.Context.RoomCapacities!.All(capacity => capacity.PlayerMaximum == 15 && capacity.EnemyMaximum == 15),
            "Original capacity room lost the actual independently initialized15 maxima.");
    }
    private static string Cycle(CardCycleState state) => Serialize(new
        { state.Hand, state.Draw, state.Discard, state.Rng, state.DrawModifier, state.BonusDraw, state.NextDrawUpgrades, state.NextUpgradeReferenceId });
    private static void ReplayBranches(BattleTurnState source, FixtureValue[] actions, FixtureValue[] turns, BattleTurnState terminal, string label)
    {
        string parent = Serialize(source), expected = BattleTurnChecks.Comparable(terminal);
        void Run()
        {
            BattleSimulationResult result = BattleSimulator.Resolve(source, Choose);
            Require(result.Supported && result.Outcome == RoomOutcome.BattleWon && result.Actions.Count == actions.Length && result.Turns.Count == turns.Length,
                "Independent capacity " + label + " policy differs from native action/turn counts: " + result.UnsupportedReason);
            Equal(expected, BattleTurnChecks.Comparable(result.State!), "independent capacity " + label + " terminal");
            for (int index = 0; index < actions.Length; index++)
            {
                Equal(result.Actions[index].Action, actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!, label + " native capacity policy action");
                Equal(BattleTurnChecks.Comparable(result.Actions[index].Result.State!), BattleTurnChecks.Comparable(actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!), label + " native capacity action decision");
            }
            for (int index = 0; index < turns.Length; index++)
            {
                Require(result.Turns[index].Outcome == (RoomOutcome)turns[index].GetProperty("ActualOutcome").GetInt32(), label + " native capacity turn outcome differs.");
                Equal(BattleTurnChecks.Comparable(result.Turns[index].State!), BattleTurnChecks.Comparable(turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!), label + " native capacity turn decision");
            }
        }
        Run(); Parallel.For(0, 16, _ => Run());
        Require(Serialize(source) == parent, "Independent capacity " + label + " policy mutated its complete source/RNG.");
    }

    // Independent captured-state policy; no recorded action or future state picks a play.
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
        IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
        RoomCombatState? floor = source.Spawn.Train.Rooms.FirstOrDefault(room => room.RoomIndex == 0);
        if (floor == null) return null;
        CombatContext context = source.Spawn.Train.Context;
        CombatUnit[] players = floor.Units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).ToArray();
        int[] sizes = [6, 6, 2, 1];
        if (players.Length < sizes.Length)
        {
            int desired = sizes[players.Length];
            foreach (CardToken card in context.Cards.Hand)
            {
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule == null || !IsCarrier(rule) || rule.SpawnUnit!.Size != desired) continue;
                PlayCardAction? summon = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                    action.RoomIndex == 0 && action.PlayerPosition == players.Length && action.TargetUnitId == 0 && action.ActivatorUnitId == 0);
                if (summon != null) return summon;
            }
        }
        foreach (CardToken card in context.Cards.Hand)
        {
            CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
            if (rule?.CardType != "Spell" || rule.IsAnyAbility != false || rule.Effect != "Null" || rule.Cost != 0 ||
                rule.CostType != "Default" || rule.Effects.Count != 0 || rule.SpawnUnit != null || rule.ExternalInteractions.Count != 0) continue;
            PlayCardAction? play = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.RoomIndex == 0 &&
                action.TargetUnitId == 0 && action.ActivatorUnitId == 0);
            if (play != null) return play;
        }
        return null;
    }
    private static CardPlayRule Rule(BattleTurnState source, int id) => source.PlayRules!.Cards.Single(rule =>
        rule.DataId == source.Spawn.Train.Context!.FindCard(id)!.DataId);
    private static bool IsCarrier(CardPlayRule? rule) => rule?.CardType == "Monster" && rule.IsAnyAbility == false &&
        rule.Effect == "SpawnMonster" && rule.Cost == 0 && rule.CostType == "Default" && rule.Destination == "Standby" &&
        rule.ExternalInteractions.Count == 0 && rule.SpawnUnit?.Team == CombatTeam.Player &&
        rule.SpawnUnit.MaxHealth == 80 && rule.SpawnUnit.BaseAttack == 2 && !rule.SpawnUnit.IsPyre &&
        rule.SpawnUnit.Triggers.Count == 0 && rule.SpawnUnit.Statuses.Count == 0 && rule.SpawnUnit.Size is 6 or 2 or 1;
    private static bool IsEmptySpell(CardPlayRule? rule) => rule?.CardType == "Spell" && rule.IsAnyAbility == false && rule.Effect == "Null" &&
        rule.Effects.Count == 0 && rule.SpawnUnit == null && rule.Summon == null && rule.Ability == null && rule.Equipment == null &&
        rule.Cost == 0 && rule.CostType == "Default" && rule.ExternalInteractions.Count == 0;
    private static string? NullableString(FixtureValue value) => value.ValueKind == FixtureKind.Null ? null : value.GetString();
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal<T>(T expected, T actual, string label) => Equal(Serialize(expected), Serialize(actual), label);
    private static void Equal(string expected, string actual, string label)
    { string? difference = ModelJson.Difference(expected, actual); Require(difference == null, label + ": " + difference); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
