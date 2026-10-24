using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerMoonRoomSpellChecks
{
    internal static void Pair(FixtureValue source, FixtureValue replay, bool requireHealingRelic = false, bool requirePhaseHealRelic = false,
        bool requirePhaseEnergyRelic = false)
    {
        if (requireHealingRelic)
            foreach (FixtureValue fixture in new[] { source, replay })
                Require(fixture.TryGetProperty("OriginalMoonCardUpgradesHealingRelic", out FixtureValue flag) && flag.GetBoolean(),
                    "Healing composition acceptance requires its explicit original-relic witnesses in both native processes.");
        if (requirePhaseHealRelic)
            foreach (FixtureValue fixture in new[] { source, replay })
                Require(fixture.TryGetProperty("OriginalMoonCardUpgradesPhaseHealRelic", out FixtureValue flag) && flag.GetBoolean(),
                    "Phase-healing acceptance requires explicit original-relic witnesses in both native processes.");
        if (requirePhaseEnergyRelic)
            foreach (FixtureValue fixture in new[] { source, replay })
                Require(fixture.TryGetProperty("OriginalMoonCardUpgradesPhaseEnergyRelic", out FixtureValue flag) && flag.GetBoolean(),
                    "Phase-energy acceptance requires explicit original-relic witnesses in both native processes.");
        RollingAdviceChecks.FrozenPair(source, replay, calibration: "original-moon-card-upgrades");
        BattleAdviceChecks.Native(source, requireBoss: false, calibration: "original-moon-card-upgrades");
        bool lunacy = source.TryGetProperty("OriginalMoonCardUpgradesLunacy", out FixtureValue advance) && advance.GetBoolean();
        bool costRelic = source.TryGetProperty("OriginalMoonCardUpgradesCostRelic", out FixtureValue costFlag) && costFlag.GetBoolean();
        bool attackRelic = source.TryGetProperty("OriginalMoonCardUpgradesAttackRelic", out FixtureValue attackFlag) && attackFlag.GetBoolean();
        bool healingRelic = source.TryGetProperty("OriginalMoonCardUpgradesHealingRelic", out FixtureValue healingFlag) && healingFlag.GetBoolean();
        bool phaseHealRelic = source.TryGetProperty("OriginalMoonCardUpgradesPhaseHealRelic", out FixtureValue phaseFlag) && phaseFlag.GetBoolean();
        bool phaseEnergyRelic = source.TryGetProperty("OriginalMoonCardUpgradesPhaseEnergyRelic", out FixtureValue energyFlag) && energyFlag.GetBoolean();
        Require(!costRelic || lunacy, "Cost relic player scene requires Lunacy controls.");
        Require(!attackRelic || lunacy && !costRelic, "Attack relic player scene requires separate Lunacy controls.");
        Require(!healingRelic || lunacy && !costRelic && !attackRelic, "Healing relic player scene requires separate Lunacy controls.");
        Require(!phaseHealRelic || lunacy && !costRelic && !attackRelic && !healingRelic,
            "Phase-healing player scene requires separate Lunacy controls.");
        Require(!phaseEnergyRelic || lunacy && !costRelic && !attackRelic && !healingRelic && !phaseHealRelic,
            "Phase-energy player scene requires separate Lunacy controls.");
        int traits = 0, exclusive = 0, additive = 0, maskedCasts = 0, eclipseCasts = 0, waits = 0, lunacyCasts = 0, paidAdvances = 0, freePurges = 0;
        var spells = new HashSet<string>();
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            Require(fixture.GetProperty("OriginalMoonCardUpgradesRoomMask").GetBoolean() &&
                fixture.GetProperty("NativeWon").GetBoolean(), "Missing winning original moon spell/room player scene.");
            Require((fixture.TryGetProperty("OriginalMoonCardUpgradesLunacy", out FixtureValue flag) && flag.GetBoolean()) == lunacy,
                "Lunacy player scene flags differ between processes.");
            Require((fixture.TryGetProperty("OriginalMoonCardUpgradesCostRelic", out FixtureValue cost) && cost.GetBoolean()) == costRelic,
                "Cost relic player scene flags differ between processes.");
            Require((fixture.TryGetProperty("OriginalMoonCardUpgradesAttackRelic", out FixtureValue attack) && attack.GetBoolean()) == attackRelic,
                "Attack relic player scene flags differ between processes.");
            Require((fixture.TryGetProperty("OriginalMoonCardUpgradesHealingRelic", out FixtureValue healing) && healing.GetBoolean()) == healingRelic,
                "Healing relic player scene flags differ between processes.");
            Require((fixture.TryGetProperty("OriginalMoonCardUpgradesPhaseHealRelic", out FixtureValue phaseRelicFlag) && phaseRelicFlag.GetBoolean()) == phaseHealRelic,
                "Phase-healing player scene flags differ between processes.");
            if (phaseHealRelic) RoomMoonSpellUpgradeChecks.PhaseHealCallbacks(fixture);
            Require((fixture.TryGetProperty("OriginalMoonCardUpgradesPhaseEnergyRelic", out FixtureValue energyRelicFlag) && energyRelicFlag.GetBoolean()) == phaseEnergyRelic,
                "Phase-energy player scene flags differ between processes.");
            if (phaseEnergyRelic) RoomMoonSpellUpgradeChecks.PhaseEnergyCallbacks(fixture);
            // The player capture owns a different instance registry from the
            // isolation trace. Resolve unchanged control identities in its root.
            CombatContext initialContext = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root")
                .Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            if (healingRelic)
            {
                RoomMoonSpellUpgradeChecks.HealingRelic(initialContext);
                CardInstanceState[] moonSpells = initialContext.Cards.Hand.Select(token => initialContext.FindCard(token.InstanceId)!)
                    .Where(card => card.MaskDescriptor?.SourceTraits?.Any(trait => trait.Type == CardMoonUpgradeModel.Trait) == true).ToArray();
                Require(moonSpells.Select(card => card.DataId).Distinct().Count() == 5 &&
                    moonSpells.All(card => card.Temporary.Offsets.Heal >= 10),
                    "Player hand lacks positive original healing contributions on all five moon spells.");
            }
            int[] costIds = costRelic ? new[] { "21038774-c0a4-413c-ab7e-27e458228e5f", "62cc5532-5fa0-4e66-9fec-85be573af359" }
                .Select(id => initialContext.CardInstances!.Single(card => card.DataId == id).InstanceId).ToArray() : [];
            Require(!costRelic || costIds.All(id => initialContext.Cards.Hand.Any(token => token.InstanceId == id)),
                "Missing original cost control cards in the player's initial hand.");
            BattleTurnState initialState = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
            int[] attackIds = attackRelic ? initialContext.Cards.Hand.Where(token =>
                initialState.PlayRules!.Cards.Single(rule => rule.DataId == token.DataId).CardType == "Monster")
                .Select(token => token.InstanceId).ToArray() : [];
            int[] spellIds = attackRelic ? initialContext.Cards.Hand.Where(token =>
                initialContext.FindCard(token.InstanceId)!.MaskDescriptor?.SourceTraits?.Any(trait =>
                    trait.Type == CardMoonUpgradeModel.Trait || trait.Type == "CardTraitScalingAdvanceMoonPhase") == true)
                .Select(token => token.InstanceId).ToArray() : [];
            Require(!attackRelic || attackIds.Length == 2 && spellIds.Length >= 6 &&
                spellIds.Select(id => initialContext.FindCard(id)!.DataId).Distinct().Count() == 6,
                "Missing original attack contributions and untouched moon spell controls in the player registry.");
            var purged = new HashSet<int>();
            EnemySpawningChecks.Native(fixture);
            waits += fixture.GetProperty("NativeEffectSettlement").GetProperty("PendingEffectWaits").GetInt32();
            foreach (FixtureValue row in fixture.GetProperty("RollingDecisions").EnumerateArray())
            {
                BattleTurnState before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
                foreach (string boundary in new[] { "Root", "Actual" })
                {
                    CombatContext context = row.GetProperty(boundary).Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
                    if (costRelic) RoomMoonSpellUpgradeChecks.CostRelic(context, costIds);
                    if (attackRelic) RoomMoonSpellUpgradeChecks.AttackRelic(context, spellIds, attackIds);
                    if (healingRelic) RoomMoonSpellUpgradeChecks.HealingRelic(context);
                    if (phaseHealRelic) RoomMoonSpellUpgradeChecks.PhaseHealRelic(context);
                    if (phaseEnergyRelic) RoomMoonSpellUpgradeChecks.PhaseEnergyRelic(context);
                    int phase = context.QueryFrame!.MoonPhase!.Value;
                    int mask = Mask(context, context.QueryFrame.SelectedRoomIndex!.Value);
                    foreach (CardToken token in context.Cards.Hand)
                    {
                        CardInstanceState card = context.FindCard(token.InstanceId)!;
                        var definitions = card.MaskDescriptor?.SourceTraits?.Where(trait => trait.Type == CardMoonUpgradeModel.Trait).ToArray();
                        if (definitions == null || definitions.Length == 0) continue;
                        var runtime = card.MaskDescriptor!.BaseTraits.Where(trait => trait.RuntimeType == CardMoonUpgradeModel.Trait).ToArray();
                        Require(runtime.Length == definitions.Length, "Player moon spell source/runtime traits differ.");
                        for (int index = 0; index < definitions.Length; index++)
                        {
                            CardSourceTrait definition = definitions[index];
                            int effective = definition.Int2 == 1 && mask != 0 ? mask : phase | mask;
                            Require(runtime[index].MoonPhaseUpgradeState!.Active == ((definition.Int & effective) != 0),
                                "Settled player hand retained the wrong room/global moon upgrade.");
                            traits++;
                            if (phase == 2 && mask == 1) { if (definition.Int2 == 1) exclusive++; else additive++; }
                        }
                    }
                }
                if (row.GetProperty("IsEndTurn").GetBoolean()) continue;
                PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
                CardInstanceState played = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
                CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == played.DataId);
                if (costIds.Contains(action.CardInstanceId))
                {
                    BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                    Require(before.Energy == after.Energy && after.Spawn.Train.Context!.OtherPiles!
                        .Single(pile => pile.Name == "Purged").Cards.Any(token => token.InstanceId == action.CardInstanceId),
                        "Player cost-control card did not play free and purge.");
                    purged.Add(action.CardInstanceId); freePurges++;
                }
                if (played.DataId == "e9fa09e5-13bd-4826-a8e5-969332cbfbbf")
                {
                    Require(lunacy, "Unexpected Lunacy in the original five-spell player scene.");
                    BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                    int paid = before.Energy, targetId = action.TargetUnitId;
                    CombatUnit prior = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targetId);
                    CombatUnit next = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targetId);
                    Require(paid > 0 && after.Energy == RoomMoonSpellUpgradeChecks.LunacyEnergyAfter(before, paid, phaseEnergyRelic) &&
                        after.MoonPhase == (paid % 2 == 0 ? before.MoonPhase : before.MoonPhase == "New" ? "Full" : "New") &&
                        (next.Status("conduit")?.Stacks ?? 0) - (prior.Status("conduit")?.Stacks ?? 0) == paid,
                        "Player Lunacy differs in actual paid X cost, phase advancement or Conduit grant.");
                    lunacyCasts++; paidAdvances += paid;
                }
                if (rule.AssetKey == "EclipseChamber") eclipseCasts++;
                if (played.MaskDescriptor?.SourceTraits?.Any(trait => trait.Type == CardMoonUpgradeModel.Trait) != true) continue;
                spells.Add(played.DataId);
                if (Mask(before.Spawn.Train.Context, action.RoomIndex) != 0) maskedCasts++;
            }
            Require(!costRelic || purged.SetEquals(costIds), "Player scene did not publish both original discounted card plays.");
        }
        Require(spells.Count == 5 && eclipseCasts > 0 && maskedCasts > 0 && exclusive > 0 && additive > 0 && waits > 0,
            "Missing original spell casts, both mask modes or pending native effects settlement witnesses.");
        Require(!lunacy || lunacyCasts >= 2 && paidAdvances > 0, "Missing paid original Lunacy casts in the player pair.");
        var records = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        BattleTurnState initial = records[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        string parent = JsonSerializer.Serialize(initial, ModelJson.Options);
        Parallel.For(0, 16, _ =>
        {
            BattleTurnState state = initial;
            foreach (FixtureValue row in records)
            {
                bool end = row.GetProperty("IsEndTurn").GetBoolean();
                var action = end ? null : row.GetProperty("Action").Deserialize<PlayCardAction>();
                state = BattlePlannerChecks.Replay(state, [new BattleSearchStep(action, end)], out RoomOutcome outcome);
                bool terminal = outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
                Require(outcome == row.GetProperty("Outcome").Deserialize<RoomOutcome>() &&
                    BattleTurnChecks.Comparable(state, terminal) == BattleTurnChecks.Comparable(
                        row.GetProperty("Actual").Deserialize<BattleTurnState>()!, terminal), "Immutable player spell/room chain differs.");
            }
        });
        Require(JsonSerializer.Serialize(initial, ModelJson.Options) == parent, "Player spell/room branch mutated its parent.");
        Console.WriteLine($"PLAYER-MOON-ROOM-SPELLS PASS originalSpells={spells.Count} eclipseCasts={eclipseCasts} maskedSpellCasts={maskedCasts} settledTraitSnapshots={traits} exclusiveWitnesses={exclusive} additiveWitnesses={additive} pendingEffectWaits={waits} lunacyCasts={lunacyCasts} paidAdvances={paidAdvances} costRelic={costRelic} attackRelic={attackRelic} healingRelic={healingRelic} freeOriginalPurges={freePurges} immutableBattleBranches=16 exactFrozenReplay=true");
    }
    private static int Mask(CombatContext context, int roomIndex) => context.RoomCapacities!
        .Single(room => room.RoomIndex == roomIndex).Attachments?
        .Aggregate(0, (mask, attachment) => mask | (attachment.MoonPhase?.AdditionalMask ?? 0)) ?? 0;

    internal static void TurnBoundary(FixtureValue root)
    {
        Require(root.GetProperty("Boundary").GetString() == "PlayerPublishedEndTurnAfterDrawAndTriggerQueue" &&
            root.GetProperty("SourceRunCompleted").GetBoolean() == false, "Missing retained failed-run boundary provenance.");
        BattleTurnState before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = root.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 0 && actual.Spawn.Turn == 1 && before.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == 2 &&
            actual.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == 0 && before.MoonPhase == "New" && actual.MoonPhase == "Full",
            "Missing original deployment, phase advance and native enemy focus controls.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        void Replay()
        {
            BattleTurnResult next = BattleTurnModel.EndTurn(before);
            Require(next.Supported && next.Outcome == RoomOutcome.Exchanged, next.UnsupportedReason ?? "Player turn boundary failed.");
            string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(next.State!), BattleTurnChecks.Comparable(actual));
            Require(difference == null, "Complete player turn successor/RNG differs: " + difference);
        }
        Replay(); Parallel.For(0, 16, _ => Replay());
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Player turn boundary mutated its parent.");
        CombatContext handBefore = root.GetProperty("HandBefore").Deserialize<CombatContext>()!;
        CombatContext handAfter = root.GetProperty("HandAfter").Deserialize<CombatContext>()!;
        CombatContext refreshed = CardMoonUpgradeModel.RefreshHand(handBefore, out string? error);
        Require(error == null && ModelJson.Difference(JsonSerializer.Serialize(refreshed, ModelJson.Options),
            JsonSerializer.Serialize(handAfter, ModelJson.Options)) == null, "Native queued post-draw hand callback differs: " + error);
        CardInstanceState[] gravity = handAfter.CardInstances!.Where(card => card.DataId == "87e45427-921e-45d3-b4c4-9c156a83cedf" &&
            handAfter.Cards.Hand.Any(token => token.InstanceId == card.InstanceId)).ToArray();
        Require(gravity.Length > 0 && gravity.All(card => card.Permanent.Upgrades.Any(upgrade => upgrade.AssetKey == "GravityAscend_Upgrade") &&
            card.Permanent.Upgrades.All(upgrade => upgrade.AssetKey != "GravityDescend_Upgrade")), "Retained Gravity did not restore unmasked Full.");
        Console.WriteLine("PLAYER-MOON-ROOM-TURN PASS completeEndTurnAndRng=true retainedGravity=newToFull nativeQueuedHandContext=true immutableBranches=16 sourceBattleAccepted=false");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
