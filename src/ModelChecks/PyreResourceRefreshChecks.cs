using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyreResourceRefreshChecks
{
    private sealed record Artifact(string Key, string Id, int PyreIndex);
    private static readonly Artifact[] Artifacts =
    [
        new("BossIncreaseTurnDraw", "f9cc17ee-b7e5-45de-a02f-bd271e2dcd1a", 0),
        new("BossIncreaseTurnDrawDH", "401f30f6-6206-4c1c-b014-a8047c01597e", 1),
        new("BossIncreaseTurnDraw2", "341001b3-3fa9-47a5-9d88-eb04390b82fa", 1)
    ];
    private const string PositiveUpgradeId = "51db335c-1a47-4dce-9353-f9acb1e48001";
    private const string NegativeUpgradeId = "51db335c-1a47-4dce-9353-f9acb1e48002";
    private const string SecondNegativeUpgradeId = "51db335c-1a47-4dce-9353-f9acb1e48003";
    private sealed record Snapshot(BattleResourceState Resources, CombatRelicState[] Relics, int Energy,
        int Draw, int PyreHP, int MaxHP, int PyreAttack, int EnergyModification, int DeploymentModification,
        string Phase, int Turn, bool Preview, int Gold);
    private sealed record Acquisition(string OwnerId, Snapshot Before, Snapshot After);
    private sealed record Operation(int Id, int? ParentId, string Kind, int? Amount, bool IsReward,
        bool Preview, bool Placement, int Notifications, int? CachedDamageBefore, int? CachedDamageAfter,
        TrainCombatState Before, TrainCombatState After);

    internal static IReadOnlyDictionary<string, int>? Native(FixtureValue root, bool required = false,
        bool replayBattle = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out var scenario) &&
            scenario.GetString() == "pyre-resource-refresh";
        Require(eligible || !required, "Fixture lacks the Pyre resource refresh scene.");
        if (!eligible) return null;
        bool compareRoomEnabled = BattleTurnChecks.ShouldCompareRoomEnabled(root);
        Require(root.GetProperty("Schema").GetInt32() == 118 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4",
            "Native Pyre refresh schema or game build changed.");
        Require(root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("Policy").GetString() == "units-spells-and-junk", "Native Pyre refresh battle is incomplete.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Pyre refresh capture has nonzero " + field + ".");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] stages = root.GetProperty("Stages").EnumerateArray().ToArray();
        Require(actions.Length > 0 && turns.Length >= 2 && stages.Length > 0 &&
            turns[^1].GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.BattleWon,
            "Pyre refresh native archive lacks real card plays, two EndTurns, complete combat or its terminal win.");
        var proofs = Artifacts.SelectMany(owner => Enumerable.Range(0, 3).Select(index => (owner.Id, Index: index)))
            .ToDictionary(key => key, _ => 0);
        void Witness(string id, int index) => proofs[(id, index)]++;
        Acquisition[] acquisitions = root.GetProperty("RelicResourceOperations").EnumerateArray()
            .Where(row => row.GetProperty("Kind").GetString() == "Acquire").Select(ReadAcquisition).ToArray();
        Require(acquisitions.Length == 3 && acquisitions.Select(row => row.OwnerId).SequenceEqual(Artifacts.Select(owner => owner.Id)),
            "Three original draw/Pyre owners were not acquired once in native scene order.");
        CheckAcquisitions(acquisitions, Witness);
        Operation[] operations = root.GetProperty("PyreResourceOperations").EnumerateArray().Select(ReadOperation).ToArray();
        CheckOperations(operations);
        string operationsParent = Serialize(operations);
        Parallel.For(0, 32, _ => ReplayOperations(operations));
        Require(Serialize(operations) == operationsParent, "32 parallel native Pyre refresh replays mutated their parents.");
        int draws = CheckDraws(root, Witness);

        BattleTurnState first = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState terminal = turns[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CheckBattle(first); CheckBattle(terminal);
        CombatUnit initial = Pyre(first.Spawn.Train);
        Snapshot installed = acquisitions[^1].After;
        RelicPyreResourceResult birth = RelicResourceModel.InitializePyre(first.Spawn.Train.Context!, installed.PyreHP, placement: true);
        Require(birth.Supported && initial.Modifiers != null && initial.Modifiers.AttackDamage == birth.Attack &&
            initial.BaseAttack == birth.Attack && initial.Modifiers.AttackDamageAdded == 0 &&
            initial.Health == birth.Health && initial.MaxHealth == birth.MaxHealth,
            "Native Pyre birth does not reproduce the original initialized raw resource inputs: " + birth.UnsupportedReason);
        foreach (Artifact artifact in Artifacts)
        {
            RelicPyreResourceResult without = RelicResourceModel.InitializePyre(Remove(first.Spawn.Train.Context!, artifact.Id), installed.PyreHP, placement: true);
            Require(without.Supported && birth.Attack - without.Attack == installed.Resources.UpgradeAttack[artifact.PyreIndex] &&
                birth.MaxHealth - without.MaxHealth == installed.Resources.UpgradeHealth[artifact.PyreIndex],
                "Native initialized Pyre owner ablation lost its actual lookup-table contribution.");
        }

        int compoundActions = 0;
        foreach (FixtureValue row in actions)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CheckBattle(before); CheckBattle(actual);
            CardPlayRule rule = Rule(before, action.CardInstanceId);
            Require(IsCompound(rule) && before.PlayRules!.Rooms.Single(room => room.RoomIndex == action.RoomIndex).IsPyre &&
                action.ActivatorUnitId == 0 && action.TargetUnitId == 0, "Pyre refresh action is not its exact compound Pyre spell.");
            string parent = Serialize(before);
            BattleActionResult replayed = BattleActionModel.PlayCard(before, action);
            Require(replayed.Supported, "Pyre refresh card action unsupported: " + replayed.Reason);
            Equal(BattleTurnChecks.Comparable(replayed.State!, compareRoomEnabled: compareRoomEnabled),
                BattleTurnChecks.Comparable(actual, compareRoomEnabled: compareRoomEnabled), "complete native Pyre refresh action");
            if (row.TryGetProperty("ActualOutcome", out var outcome)) Require(replayed.Outcome == (RoomOutcome)outcome.GetInt32(), "Pyre refresh action outcome differs.");
            CombatUnit oldPyre = Pyre(before.Spawn.Train), newPyre = Pyre(actual.Spawn.Train);
            Require(oldPyre.Modifiers != null && newPyre.Modifiers != null &&
                newPyre.Modifiers.Upgrades.Count - oldPyre.Modifiers.Upgrades.Count == 3 &&
                newPyre.Modifiers.DamageBuff - oldPyre.Modifiers.DamageBuff == 11 &&
                newPyre.Statuses.Single(status => status.Id == "buff").Stacks == 2 &&
                newPyre.Modifiers.DamageBuffFromStatusEffects > oldPyre.Modifiers.DamageBuffFromStatusEffects &&
                newPyre.Modifiers.AttackDamageAdded == 0 && actual.Spawn.Train.Context!.Gold == before.Spawn.Train.Context!.Gold,
                "Native compound play lacks three real Pyre upgrades, Buff11, Rage2/cache, zero added-attack or balanced gold changes.");
            Require(Serialize(before) == parent, "Native Pyre refresh card replay mutated its source.");
            compoundActions++;
        }
        Require(compoundActions == 1, "Unique +7 marker did not limit the independent native policy to one compound play.");
        foreach (FixtureValue row in turns)
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CheckBattle(before); CheckBattle(actual);
            string parent = Serialize(before);
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == (RoomOutcome)row.GetProperty("ActualOutcome").GetInt32(),
                "Pyre refresh EndTurn unsupported or outcome differs: " + result.UnsupportedReason);
            Equal(BattleTurnChecks.Comparable(result.State!, compareRoomEnabled: compareRoomEnabled),
                BattleTurnChecks.Comparable(actual, compareRoomEnabled: compareRoomEnabled), "complete native Pyre refresh EndTurn");
            Require(Serialize(before) == parent, "Pyre refresh EndTurn mutated its source.");
        }
        int pyreCombats = CheckStages(stages, Witness);
        foreach (var proof in proofs) Require(proof.Value > 0,
            "Original draw/Pyre owner lacks actual effect proof: " + Artifacts.Single(owner => owner.Id == proof.Key.Id).Key + "/" + proof.Key.Index);
        if (replayBattle)
        {
            CardCycleChecks.Native(root); BattleActionChecks.Native(root); BattleTurnChecks.Native(root);
            if (!compareRoomEnabled)
                Console.WriteLine("LEGACY-PYRE-RESOURCE-POLICY-LIMITATION: stale room Enabled snapshots are excluded; complete root and mid-battle policies remain checked.");
            ReplayBranches(first, actions, turns, terminal, compareRoomEnabled, "root");
            BattleTurnState mid = actions[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            ReplayBranches(mid, actions.Skip(1).ToArray(), turns.Where(row =>
                row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() >= mid.Spawn.Turn).ToArray(),
                terminal, compareRoomEnabled, "mid");
        }
        var witnesses = Artifacts.ToDictionary(owner => owner.Key, owner => proofs.Where(proof => proof.Key.Id == owner.Id).Sum(proof => proof.Value));
        Console.WriteLine("PYRE-RESOURCE-REFRESH-WITNESSES " + string.Join(", ", witnesses.Select(row => row.Key + "=" + row.Value)));
        Console.WriteLine($"NATIVE-PYRE-RESOURCE-REFRESH PASS: three unchanged originals/all9 effects, {acquisitions.Length} acquisitions, {draws} real draw8 applications, {operations.Length} native gold/refresh operations, {pyreCombats} positive Pyre combat stages; full raw/status/upgrade cache replay, zero/nonzero gold boundaries and 32 immutable operation branches.");
        return witnesses;
    }

    private static void CheckAcquisitions(Acquisition[] acquisitions, Action<string, int> witness)
    {
        for (int index = 0; index < acquisitions.Length; index++)
        {
            Acquisition row = acquisitions[index];
            Require(!row.Before.Preview && !row.After.Preview && row.Before.Turn == 0 && row.After.Turn == 0,
                "Pyre draw originals were not acquired at the native pre-battle boundary.");
            CheckSnapshot(row.Before); CheckSnapshot(row.After);
            Equal(row.Before.Resources, row.After.Resources, "acquisition raw resource inputs");
            Equal(row.Before.Relics, row.After.Relics.Where(owner => owner.DataId != row.OwnerId).ToArray(), "acquisition other owners");
            Require(!row.Before.Relics.Any(owner => owner.DataId == row.OwnerId), "Original Pyre draw owner existed before its acquisition.");
            if (index > 0) Equal(acquisitions[index - 1].After, row.Before, "native Pyre draw acquisition chain");
            CombatContext context = Context(row.After);
            int ownerIndex = Array.FindIndex(row.After.Relics, owner => owner.DataId == row.OwnerId);
            RelicPyreResourceResult result = RelicResourceModel.Acquire(context, ownerIndex, row.Before.PyreHP);
            Artifact original = Artifacts.Single(owner => owner.Id == row.OwnerId);
            int amount = row.After.Resources.UpgradeHealth[original.PyreIndex];
            Require(result.Supported && result.Health == row.After.PyreHP && result.MaxHealth == row.After.MaxHP && amount > 0 &&
                row.After.PyreHP - row.Before.PyreHP == amount && row.After.MaxHP - row.Before.MaxHP == amount &&
                row.After.PyreAttack - row.Before.PyreAttack == row.After.Resources.UpgradeAttack[original.PyreIndex] &&
                row.After.Draw - row.Before.Draw == 1,
                "Original draw/Pyre acquisition lacks its actual heal/max/draw/table-attack contribution: " + result.UnsupportedReason);
            CombatContext without = Remove(context, row.OwnerId);
            Require(RelicResourceModel.GetDrawCount(without).Value == row.Before.Draw &&
                RelicResourceModel.GetPyreMaxHealth(without).Value == row.Before.MaxHP &&
                RelicResourceModel.GetPyreAttack(without).Value == row.Before.PyreAttack,
                "Pyre draw acquisition owner ablation differs from the actual before boundary.");
            witness(row.OwnerId, 1);
        }
        Require(acquisitions[^1].After.Draw == 8, "Three original draw owners lack a real registered draw8 aggregate.");
    }

    private static void CheckOperations(Operation[] operations)
    {
        Require(operations.Length > 0 && operations.Select(row => row.Id).Distinct().Count() == operations.Length &&
            operations.All(row => row.Kind is "Gold" or "PyreRefresh"), "Missing, duplicated or unknown native gold/Pyre operation.");
        ReplayOperations(operations);
        Operation[] gold = operations.Where(row => row.Kind == "Gold" && row.ParentId == null).ToArray();
        Require(gold.Where(row => row.Amount is 0 or 5 or -5).Select(row => row.Amount).SequenceEqual(new int?[] { 0, 5, -5 }),
            "The compound card did not run its real ordered gold0/+5/-5 bodies.");
        foreach (Operation row in gold)
        {
            Require(row.Amount.HasValue && !row.Preview && !row.Placement && row.Notifications == 0,
                "Compound gold body has a preview, placement, direct relic notification or missing-amount mismatch.");
            Operation[] children = operations.Where(child => child.ParentId == row.Id).ToArray();
            Require(children.All(child => child.Kind == "PyreRefresh") && children.Length == (row.Amount == 0 ? 0 : 1),
                "Gold0 dispatched a refresh or a nonzero native gold body lacks exactly one synchronous Pyre refresh.");
            CombatUnit before = Pyre(row.Before), after = Pyre(row.After);
            UnitModifiers old = before.Modifiers!, next = after.Modifiers!;
            Require(old.AttackDamageAdded == 0 && next.AttackDamageAdded == 0 && old.DamageBuff == 11 && next.DamageBuff == 11 &&
                before.Statuses.Single(status => status.Id == "buff").Stacks == 2 &&
                after.Statuses.Single(status => status.Id == "buff").Stacks == 2 &&
                old.DamageBuffFromStatusEffects.HasValue && old.DamageBuffFromStatusEffects.Value > 0,
                "Native gold body lacks real zero upgrade-accumulator, Buff11 and Rage2 attack history.");
            int baseAttack = NativeBase(row.Before.Context!);
            Require(old.AttackDamage == unchecked(baseAttack - 3), "Native pre-gold Pyre lacks the actual temporary -3 raw attack delta.");
            if (row.Amount == 0)
            {
                Equal(row.Before, row.After, "Gold0 must retain the whole native train and its stale raw attack cache");
                Require(row.CachedDamageBefore.HasValue && row.CachedDamageAfter == row.CachedDamageBefore,
                    "Gold0 dispatched a native cached Pyre display update.");
                Require(old.Upgrades.Any(upgrade => upgrade.DataId == PositiveUpgradeId && upgrade.Stats.Damage == 7 && upgrade.Unique) &&
                    old.Upgrades.Any(upgrade => upgrade.DataId == NegativeUpgradeId && upgrade.Stats.Damage == -10 && upgrade.Unique),
                    "Gold0 lacks its real +7/-10 unique upgrade history.");
            }
            else
            {
                Require(next.AttackDamage == baseAttack && next.AttackDamage - old.AttackDamage == 3 &&
                    row.After.Context!.Gold == Math.Max(0, unchecked(row.Before.Context!.Gold + GoldRewardModel.Adjust(row.Amount.Value, row.IsReward))) &&
                    row.After.Context.Gold != row.Before.Context.Gold,
                    "Native nonzero gold body did not refresh raw Pyre attack while changing captured gold.");
                Require(row.CachedDamageBefore.HasValue && row.CachedDamageAfter == RelicResourceModel.AttackDamage(after, row.After.Context),
                    "SetGold did not finish its real displayed Pyre attack update with preserved buffs/statuses.");
                Equal(children[0].After, row.After, "synchronous gold/refresh completed train boundary");
                Require(children[0].Before.Context!.Gold == row.After.Context.Gold,
                    "Native nested refresh ran before SetGold updated the shared balance.");
                if (row.Amount == -5) Require(old.Upgrades.Any(upgrade => upgrade.DataId == SecondNegativeUpgradeId && upgrade.Stats.Damage == -3 && upgrade.Unique),
                    "Gold-5 did not follow its real second temporary -3 upgrade.");
            }
            Equal(Preserved(before), Preserved(after), "gold refresh retained status/upgrade/health/native unit metadata");
        }
        foreach (Operation row in operations.Where(row => row.Kind == "PyreRefresh"))
            Require(row.ParentId.HasValue && gold.Any(parent => parent.Id == row.ParentId) && row.Notifications == 3,
                "Native Pyre refresh has no synchronous gold parent or lacks three original attack-effect notifications.");
    }

    private static void ReplayOperations(Operation[] operations)
    {
        foreach (Operation row in operations)
        {
            CheckTrain(row.Before); CheckTrain(row.After);
            string parent = Serialize(row.Before);
            TrainCombatResult result = row.Kind == "Gold"
                ? GoldModel.Adjust(row.Before, row.Amount!.Value, row.IsReward, row.Preview)
                : RelicResourceModel.RefreshPyreAttack(row.Before, row.Placement);
            Require(result.Supported, "Native gold/Pyre operation unsupported: " + row.Kind + "/" + row.Id + " " + result.UnsupportedReason);
            Equal(result.State!, row.After, "complete native " + row.Kind + "/" + row.Id);
            Require(Serialize(row.Before) == parent && result.State!.Context!.BattleRng.Equals(row.Before.Context!.BattleRng) &&
                result.State.Context.Cards.Rng.Equals(row.Before.Context.Cards.Rng),
                "Native gold/Pyre operation mutated its parent or consumed RNG.");
            if (row.Kind == "PyreRefresh")
            {
                CombatUnit before = Pyre(row.Before), after = Pyre(row.After);
                Require(after.Modifiers!.AttackDamage == unchecked(NativeBase(row.Before.Context!) + before.Modifiers!.AttackDamageAdded),
                    "Native Pyre refresh raw attack differs from captured base plus the native cached upgrade accumulator.");
                Require(row.CachedDamageBefore.HasValue && row.CachedDamageAfter == after.Modifiers.AttackDamage,
                    "Native gold handler's cached Pyre attack did not retain its independently recorded raw refresh value.");
                Equal(Preserved(before), Preserved(after), "Pyre refresh other native unit fields");
                Equal(row.Before.Context!.Resources, row.After.Context!.Resources, "Pyre refresh raw save/balance/definition inputs");
                Require(row.Before.Context.Gold == row.After.Context.Gold, "A Pyre refresh rewrote captured gold.");
            }
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
            int handSize = row.GetProperty("HandSize").GetInt32(), maximum = row.GetProperty("MaxHandSize").GetInt32();
            Require(handSize == 8 && RelicResourceModel.GetDrawCount(context).Value == handSize && maximum == context.MaxHandSize,
                "Native hand draw omitted its actual registered three-owner draw8 inputs.");
            Equal(Cycle(context.Cards), Cycle(before), "native Pyre resource pre-draw context/input");
            CardCycleState returned = EquipmentModel.ReturnStandby(context, row.GetProperty("LivingUnitIds").Deserialize<int[]>()!).Cards;
            CardCycleState prepared = new(returned.Hand, returned.Draw, returned.Discard, returned.Rng, returned.DrawModifier,
                before.ExternalInteractions, returned.BonusDraw, returned.NextDrawUpgrades, returned.NextUpgradeReferenceId);
            string parent = Serialize(context);
            CardCycleResult result = CardCycleModel.DrawHand(prepared, handSize, maximum);
            Require(result.Supported, "Pyre resource native card draw unsupported: " + result.UnsupportedReason);
            Equal(Cycle(result.State!), Cycle(actual), "native Pyre resource full draw piles/RNG");
            HashSet<int> held = prepared.Hand.Select(card => card.InstanceId).ToHashSet();
            int added = actual.Hand.Count(card => !held.Contains(card.InstanceId));
            if (prepared.DrawModifier == 0 && maximum - prepared.Hand.Count >= 8 &&
                prepared.Draw.Count + prepared.Discard.Count >= 8 && added == 8)
            {
                positive++;
                foreach (Artifact owner in Artifacts)
                {
                    RelicResourceResult fewer = RelicResourceModel.GetDrawCount(Remove(context, owner.Id));
                    CardCycleResult without = CardCycleModel.DrawHand(prepared, fewer.Value, maximum);
                    Require(fewer.Supported && fewer.Value == 7 && without.Supported &&
                        added - without.State!.Hand.Count(card => !held.Contains(card.InstanceId)) == 1,
                        "Original +1 draw owner lacks a real added-card counterfactual: " + owner.Key);
                    witness(owner.Id, 0);
                }
            }
            Require(Serialize(context) == parent, "Native Pyre resource draw mutated its immutable context.");
        }
        Require(positive > 0, "Three original draw owners never added eight real cards with positive one-owner ablations.");
        return positive;
    }

    private static int CheckStages(FixtureValue[] stages, Action<string, int> witness)
    {
        int positive = 0;
        foreach (FixtureValue row in stages)
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CheckRoom(before); CheckRoom(actual);
            string parent = Serialize(before);
            string kind = row.GetProperty("Kind").GetString()!;
            Require(kind is "Exchange" or "Room", "Unknown native Pyre resource combat stage.");
            RoomCombatResult Run(RoomCombatState source) => kind == "Exchange" ? RoomCombatModel.Exchange(source) : RoomCombatModel.Resolve(source);
            RoomCombatResult result = Run(before);
            Require(result.Supported, "Native Pyre resource combat unsupported: " + result.UnsupportedReason);
            Equal(result.State!, actual, "complete native Pyre refresh combat stage");
            CombatUnit? pyre = before.Units.SingleOrDefault(unit => unit.IsPyre);
            if (pyre != null && result.Events.Any(effect => effect.Kind == "Attack" && effect.Actor == pyre.Id && effect.Amount > 0) &&
                before.Units.Where(unit => unit.Team == CombatTeam.Enemy).Any(unit =>
                    (actual.Units.FirstOrDefault(after => after.Id == unit.Id)?.Health ?? 0) < unit.Health))
            {
                positive++;
                foreach (Artifact owner in Artifacts)
                {
                    int contribution = before.Context!.Resources!.UpgradeAttack[owner.PyreIndex];
                    RoomCombatState removed = new(before.RoomIndex, before.Deployment,
                        before.Units.Select(unit => unit.Id == pyre.Id ? WeakenPyre(unit, contribution) : unit).ToArray(),
                        before.ExternalInteractions, Remove(before.Context, owner.Id), before.Preview);
                    RoomCombatResult without = Run(removed);
                    Require(without.Supported, "Pyre resource attack owner ablation unsupported: " + without.UnsupportedReason);
                    if (result.Events.Any(effect => effect.Kind == "Attack" && effect.Actor == pyre.Id && effect.Amount > 0 &&
                        without.Events.Any(other => other.Kind == effect.Kind && other.Actor == effect.Actor && other.Target == effect.Target &&
                            other.Round == effect.Round && other.Amount < effect.Amount))) witness(owner.Id, 2);
                }
            }
            Require(Serialize(before) == parent, "Native Pyre combat or its cache-preserving owner ablations mutated the source.");
        }
        Require(positive > 0, "The original Pyre owners never exercised a real positive Pyre attack against enemies.");
        return positive;
    }

    private static void CheckSnapshot(Snapshot row)
    {
        CheckDefinitions(row.Resources, row.Relics, false);
        CombatContext context = Context(row);
        string parent = Serialize(context);
        RelicResourceResult draw = RelicResourceModel.GetDrawCount(context), health = RelicResourceModel.GetPyreMaxHealth(context),
            attack = RelicResourceModel.GetPyreAttack(context, placement: row.Turn == 0),
            energy = RelicResourceModel.GetEnergyModification(context), deployment = RelicResourceModel.GetEnergyModification(context, deployment: true);
        int rawDraw = Math.Clamp(row.Resources.BaseDraw + row.Relics.SelectMany(owner => owner.ResourceEffects ?? [])
            .Where(rule => rule.Type == RelicResourceModel.Draw && rule.DrawMutationOrder.HasValue).Sum(rule => rule.ParamInt), 1, row.Resources.MaxHandSize);
        int rawHealth = unchecked(row.Resources.BasePyreMaxHealth + row.Relics.SelectMany(owner => owner.ResourceEffects ?? [])
            .Where(rule => rule.Type == RelicResourceModel.PyreHealth).Sum(rule => row.Resources.UpgradeHealth[rule.ParamInt2]));
        Require(draw.Supported && draw.Value == row.Draw && row.Draw == rawDraw && health.Supported && health.Value == row.MaxHP &&
            row.MaxHP == rawHealth && attack.Supported && attack.Value == row.PyreAttack && row.PyreAttack == NativeBase(context) &&
            energy.Supported && energy.Value == row.EnergyModification && row.EnergyModification == 0 &&
            deployment.Supported && deployment.Value == row.DeploymentModification && row.DeploymentModification == 0,
            "Captured native Pyre resource getters differ from unchanged original parameters and selected definition tables.");
        foreach (RelicResourceResult result in new[] { draw, health, attack, energy, deployment })
            Require(Serialize(result.Context) == parent, "Pyre resource getter mutated native caches or condition state.");
        Require(Serialize(context) == parent, "Pyre resource getter mutated its complete source.");
    }

    private static void CheckDefinitions(BattleResourceState raw, IReadOnlyList<CombatRelicState> relics, bool complete)
    {
        Require(raw.BaseEnergy >= 0 && raw.DeploymentExtraEnergy >= 0 && raw.BaseDraw > 0 && raw.MaxEnergy > 0 && raw.MaxHandSize >= 8 &&
            raw.BasePyreMaxHealth > 0 && raw.UpgradeAttack.Count >= 2 && raw.UpgradeHealth.Count >= 2 &&
            raw.UpgradeAttack.Take(2).All(value => value > 0) && raw.UpgradeHealth.Take(2).All(value => value > 0) &&
            !string.IsNullOrEmpty(raw.PyreDataId), "Native Pyre refresh capture omitted actual balance/save/selected-definition inputs.");
        foreach (Artifact artifact in Artifacts)
        {
            CombatRelicState[] owners = relics.Where(owner => owner.DataId == artifact.Id).ToArray();
            Require(owners.Length <= 1 && (!complete || owners.Length == 1), "Missing or duplicate original Pyre draw owner: " + artifact.Key);
            if (owners.Length == 0) continue;
            CombatRelicState owner = owners[0];
            string[] types = [RelicResourceModel.Draw, RelicResourceModel.PyreHealth, RelicResourceModel.PyreAttack];
            Require(owner.AssetKey == artifact.Key && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(types) && owner.ResourceEffects?.Count == 3,
                "Original Pyre draw owner identity/effect sequence changed: " + artifact.Key);
            int drawOrder = Array.FindIndex(Artifacts, item => item.Id == artifact.Id);
            for (int index = 0; index < 3; index++)
            {
                RelicResourceEffect rule = owner.ResourceEffects![index];
                int parameter = index == 0 ? 1 : index == 1 ? 30 : 10;
                int pyreIndex = index == 0 ? 0 : artifact.PyreIndex;
                int amount = index == 0 ? 1 : index == 1 ? raw.UpgradeHealth[pyreIndex] : raw.UpgradeAttack[pyreIndex];
                Require(rule.EffectIndex == index && rule.Type == types[index] && rule.ParamInt == parameter && rule.ParamInt2 == pyreIndex &&
                    rule.Amount == amount && !rule.CanApplyInPreviewMode && rule.RequiredMutatorId == null && rule.Conditions.Count == 0,
                    "Original Pyre draw effect runtime parameters/table amount/gates changed: " + artifact.Key + "/" + index);
                Require(index == 0 ? rule.DrawValue == 1f && rule.DrawMutationOrder == drawOrder :
                    rule.DrawValue == null && rule.DrawMutationOrder == null, "Original Pyre draw mutation registration changed.");
            }
        }
        Require(relics.Where(owner => owner.ResourceEffects?.Count > 0).All(owner => Artifacts.Any(artifact => artifact.Id == owner.DataId)),
            "An additional resource owner confounds the three new original Pyre draw owners.");
    }

    private static int NativeBase(CombatContext source) => unchecked(source.Resources!.PyreBaseAttack + source.Resources.PyrePersistentDamage +
        source.Relics!.SelectMany(owner => owner.ResourceEffects ?? []).Where(rule => rule.Type == RelicResourceModel.PyreAttack)
            .Sum(rule => source.Resources.UpgradeAttack[rule.ParamInt2]));
    private static object Preserved(CombatUnit unit) => new
    {
        unit.Id, unit.AssetKey, unit.Team, unit.Health, unit.MaxHealth, unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath,
        unit.Statuses, unit.Triggers, unit.SpawnerCardId, unit.Size, unit.StatusImmunities, unit.Subtypes, unit.IsBoss,
        unit.LastAttackerId, unit.StatusRegistry, unit.EquipmentCards, unit.NextTriggerId, unit.Ability, unit.StatusDictionary,
        unit.AbilityRules, unit.HordeDefinition, unit.IsSpawning, unit.SacrificeCardId, unit.DeathState, unit.BumpRules,
        Modifiers = new { unit.Modifiers!.AttackDamageAdded, unit.Modifiers.DamageBuff, unit.Modifiers.RawSize,
            unit.Modifiers.EquipmentLimit, unit.Modifiers.CanBeHealed, unit.Modifiers.IsClone, unit.Modifiers.Upgrades,
            unit.Modifiers.HealthFromUpgrades, unit.Modifiers.SpawnerMatchesDefinition, unit.Modifiers.DamageBuffFromStatusEffects }
    };
    private static Acquisition ReadAcquisition(FixtureValue value)
    {
        Require(value.GetProperty("EffectIndex").GetInt32() == -1 && !value.GetProperty("Deployment").GetBoolean() &&
            value.GetProperty("After").ValueKind != FixtureKind.Null, "Unfinished or invalid original Pyre resource acquisition.");
        return new(value.GetProperty("OwnerDataId").GetString()!, ReadSnapshot(value.GetProperty("Before")), ReadSnapshot(value.GetProperty("After")));
    }
    private static Snapshot ReadSnapshot(FixtureValue value) => new(value.GetProperty("Resources").Deserialize<BattleResourceState>()!,
        value.GetProperty("Relics").Deserialize<CombatRelicState[]>()!, value.GetProperty("Energy").GetInt32(), value.GetProperty("Draw").GetInt32(),
        value.GetProperty("PyreHP").GetInt32(), value.GetProperty("MaxHP").GetInt32(), value.GetProperty("PyreAttack").GetInt32(),
        value.GetProperty("EnergyModification").GetInt32(), value.GetProperty("DeploymentModification").GetInt32(),
        value.GetProperty("Phase").GetString()!, value.GetProperty("Turn").GetInt32(), value.GetProperty("Preview").GetBoolean(), value.GetProperty("Gold").GetInt32());
    private static Operation ReadOperation(FixtureValue value)
    {
        Require(value.GetProperty("After").ValueKind != FixtureKind.Null, "Native gold/Pyre operation is unfinished.");
        return new(value.GetProperty("Id").GetInt32(), value.GetProperty("ParentId").Deserialize<int?>(), value.GetProperty("Kind").GetString()!,
            value.GetProperty("Amount").Deserialize<int?>(), value.GetProperty("IsReward").GetBoolean(), value.GetProperty("Preview").GetBoolean(),
            value.GetProperty("Placement").GetBoolean(), value.GetProperty("Notifications").GetInt32(),
            value.GetProperty("CachedDamageBefore").Deserialize<int?>(), value.GetProperty("CachedDamageAfter").Deserialize<int?>(),
            value.GetProperty("Before").Deserialize<TrainCombatState>()!, value.GetProperty("After").Deserialize<TrainCombatState>()!);
    }
    private static CombatContext Context(Snapshot row) => new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(17),
        row.Gold, 1, row.Resources.MaxHandSize, statistics: BattleStatistics.Empty(deckCards: []), otherPiles: [],
        queryFrame: new(energy: row.Energy, runningCombat: true, turn: row.Turn),
        energyState: new(row.Resources.MaxEnergy, 0, 0, row.Phase, row.PyreHP > 0), relics: row.Relics, resources: row.Resources);
    private static CombatContext Remove(CombatContext source, string id) => source.WithRelics(source.Relics!.Where(owner => owner.DataId != id).ToArray());
    private static CombatUnit Pyre(TrainCombatState source) => source.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
    private static void CheckBattle(BattleTurnState source) => CheckTrain(source.Spawn.Train);
    private static void CheckTrain(TrainCombatState source)
    {
        Require(source.Context?.Resources != null, "Native Pyre refresh train omitted raw resources.");
        CheckDefinitions(source.Context!.Resources!, source.Context.Relics!, true);
        foreach (RoomCombatState room in source.Rooms) CheckRoom(room);
    }
    private static void CheckRoom(RoomCombatState source)
    {
        Require(source.Context?.Resources != null, "Native Pyre refresh room omitted raw resources.");
        CheckDefinitions(source.Context!.Resources!, source.Context.Relics!, true);
        Require(source.Units.All(unit => unit.Team != CombatTeam.Player || unit.IsPyre), "Pyre refresh scene deployed an unintended friendly carrier.");
        foreach (CombatUnit unit in source.Units.Where(unit => unit.IsPyre)) Require(unit.Modifiers?.DamageBuffFromStatusEffects != null,
            "Native Pyre refresh omitted its real status attack accumulator.");
    }
    private static string Cycle(CardCycleState source) => Serialize(new
        { source.Hand, source.Draw, source.Discard, source.Rng, source.DrawModifier, source.BonusDraw, source.NextDrawUpgrades, source.NextUpgradeReferenceId });
    private static CombatUnit WeakenPyre(CombatUnit source, int amount)
    {
        UnitModifiers old = source.Modifiers!;
        UnitModifiers modifiers = new(unchecked(old.AttackDamage - amount), old.AttackDamageAdded, old.DamageBuff,
            old.RawSize, old.EquipmentLimit, old.CanBeHealed, old.IsClone, old.Upgrades, old.HealthFromUpgrades,
            old.SpawnerMatchesDefinition, old.DamageBuffFromStatusEffects);
        return new(source.Id, source.AssetKey, source.Team, unchecked(source.BaseAttack - amount), source.Health, source.MaxHealth,
            source.CanAttack, source.IsPyre, source.EndsBattleOnDeath, source.Statuses, source.Triggers, source.SpawnerCardId, source.Size,
            source.StatusImmunities, source.Subtypes, modifiers, source.IsBoss, source.LastAttackerId, source.StatusRegistry,
            source.EquipmentCards, source.NextTriggerId, source.Ability, source.StatusDictionary, source.AbilityRules,
            source.HordeDefinition, source.IsSpawning, source.SacrificeCardId, source.DeathState, source.BumpRules);
    }
    private static void ReplayBranches(BattleTurnState source, FixtureValue[] actions, FixtureValue[] turns,
        BattleTurnState terminal, bool compareRoomEnabled, string label)
    {
        string parent = Serialize(source), expected = BattleTurnChecks.Comparable(terminal, compareRoomEnabled: compareRoomEnabled);
        void Run()
        {
            BattleSimulationResult result = BattleSimulator.Resolve(source, Choose);
            Require(result.Supported && result.Outcome == RoomOutcome.BattleWon && result.Actions.Count == actions.Length && result.Turns.Count == turns.Length,
                "Independent Pyre refresh " + label + " policy differs from native counts/outcome: " + result.UnsupportedReason);
            Equal(expected, BattleTurnChecks.Comparable(result.State!, compareRoomEnabled: compareRoomEnabled),
                "independent Pyre refresh " + label + " terminal");
            for (int index = 0; index < actions.Length; index++)
            {
                Equal(result.Actions[index].Action, actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!, label + " native policy action");
                Equal(BattleTurnChecks.Comparable(result.Actions[index].Result.State!, compareRoomEnabled: compareRoomEnabled),
                    BattleTurnChecks.Comparable(actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!,
                        compareRoomEnabled: compareRoomEnabled), label + " native action state");
            }
            for (int index = 0; index < turns.Length; index++)
            {
                Require(result.Turns[index].Outcome == (RoomOutcome)turns[index].GetProperty("ActualOutcome").GetInt32(), label + " native EndTurn outcome differs.");
                Equal(BattleTurnChecks.Comparable(result.Turns[index].State!, compareRoomEnabled: compareRoomEnabled),
                    BattleTurnChecks.Comparable(turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!,
                        compareRoomEnabled: compareRoomEnabled), label + " native EndTurn state");
            }
        }
        Run(); Parallel.For(0, 16, _ => Run());
        Require(Serialize(source) == parent, "Independent Pyre refresh " + label + " replay mutated its source.");
    }
    private static CardPlayRule Rule(BattleTurnState source, int cardId)
    {
        CardInstanceState? card = source.Spawn.Train.Context!.FindCard(cardId);
        return source.PlayRules!.Cards.Single(rule => rule.DataId == card?.DataId);
    }
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
        CombatUnit? pyre = source.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre);
        if (pyre?.Modifiers == null || pyre.Modifiers.Upgrades.Any(upgrade => upgrade.DataId == PositiveUpgradeId)) return null;
        RoomPlayRule? room = source.PlayRules.Rooms.SingleOrDefault(item => item.IsPyre);
        if (room == null) return null;
        IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
        foreach (CardToken card in source.Spawn.Train.Context.Cards.Hand)
        {
            CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
            if (rule == null || !IsCompound(rule)) continue;
            PlayCardAction? play = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                action.RoomIndex == room.RoomIndex && action.ActivatorUnitId == 0 && action.TargetUnitId == 0);
            if (play != null) return play;
        }
        return null;
    }
    private static bool IsCompound(CardPlayRule rule)
    {
        if (rule.CardType != "Spell" || rule.IsAnyAbility != false || rule.Effect != "Spell" || rule.Cost != 0 ||
            rule.CostType != "Default" || rule.SpawnUnit != null || rule.Summon != null || rule.Ability != null ||
            rule.Equipment != null || rule.ExternalInteractions.Count != 0 || rule.Effects.Count != 8) return false;
        string[] types = { "UnitUpgrade", "UnitUpgrade", "BuffAttack", "AddStatus", "RewardGold", "RewardGold", "UnitUpgrade", "RewardGold" };
        int[] values = { 0, 0, 11, 0, 0, 5, 0, -5 };
        for (int index = 0; index < types.Length; index++)
        {
            CardActionEffect effect = rule.Effects[index];
            bool unitEffect = index < 4 || index == 6;
            if (effect.Type != types[index] || effect.Target != "Pyre" || effect.Value != values[index] ||
                effect.AllowEnemy || effect.AllowPlayer != unitEffect || effect.RequiredGold != 0) return false;
            if (index == 3)
            {
                if (effect.Statuses.Count != 1 || effect.Statuses[0].Id != "buff" || effect.Statuses[0].Stacks != 2) return false;
            }
            else if (effect.Statuses.Count != 0) return false;
            if (index == 0 || index == 1 || index == 6)
            {
                string id = index == 0 ? PositiveUpgradeId : index == 1 ? NegativeUpgradeId : SecondNegativeUpgradeId;
                int damage = index == 0 ? 7 : index == 1 ? -10 : -3;
                if (effect.Lifetime != "TemporaryUntilUnitDeath" || effect.Upgrade?.DataId != id ||
                    !effect.Upgrade.Unique || effect.Upgrade.Stats.Damage != damage || effect.Upgrade.Statuses.Count != 0) return false;
            }
            else if (effect.Upgrade != null) return false;
        }
        return true;
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal<T>(T expected, T actual, string label) => Equal(Serialize(expected), Serialize(actual), label);
    private static void Equal(string expected, string actual, string label)
    { string? difference = ModelJson.Difference(expected, actual); Require(difference == null, label + ": " + difference); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
