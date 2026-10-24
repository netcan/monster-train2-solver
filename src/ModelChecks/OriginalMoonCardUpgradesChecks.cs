using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMoonCardUpgradesChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Enumerable.Range(0, ids.Length).Where(i => i != 2 && source.Spawn.Train.Context!.Statistics!.Value(ids[i], "TimesPlayed", "ThisBattle") == 0).DefaultIfEmpty(-1).First();
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        CombatUnit? target = index is 5 or 11 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[1]) :
            index is 6 or 12 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]) : null;
        int roomIndex = index == 1 ? 1 : target == null ? 0 : source.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == target.Id)).RoomIndex;
        var action = new PlayCardAction(ids[index], roomIndex, playerPosition: index < 2 ? 0 : -1, targetUnitId: target?.Id ?? 0);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        bool roomMask = root.TryGetProperty("OriginalMoonCardUpgradesRoomMask", out FixtureValue maskFlag) && maskFlag.GetBoolean();
        bool lunacy = root.TryGetProperty("OriginalMoonCardUpgradesLunacy", out FixtureValue lunacyFlag) && lunacyFlag.GetBoolean();
        bool costRelic = root.TryGetProperty("OriginalMoonCardUpgradesCostRelic", out FixtureValue costFlag) && costFlag.GetBoolean();
        bool attackRelic = root.TryGetProperty("OriginalMoonCardUpgradesAttackRelic", out FixtureValue attackFlag) && attackFlag.GetBoolean();
        bool healingRelic = root.TryGetProperty("OriginalMoonCardUpgradesHealingRelic", out FixtureValue healingFlag) && healingFlag.GetBoolean();
        bool phaseHealRelic = root.TryGetProperty("OriginalMoonCardUpgradesPhaseHealRelic", out FixtureValue phaseFlag) && phaseFlag.GetBoolean();
        bool phaseEnergyRelic = root.TryGetProperty("OriginalMoonCardUpgradesPhaseEnergyRelic", out FixtureValue energyFlag) && energyFlag.GetBoolean();
        Require(root.GetProperty("OriginalMoonCardUpgradesPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing settled original moon upgrade battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalMoonCardUpgradesIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), hands = root.GetProperty("OriginalMoonCardUpgradesHands").EnumerateArray().ToArray();
        if (actions.Length < 15)
        {
            BattleTurnState last = actions[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit target = last.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[1]);
            BattleActionResult attempted = BattleActionModel.PlayCard(last, new PlayCardAction(ids[5], 1, targetUnitId: target.Id));
            Console.WriteLine("MOON-UPGRADE-NEXT-ACTION: " + attempted.Reason);
        }
        Require(ids.Length == (costRelic ? 23 : lunacy ? 21 : roomMask ? 17 : 16) && actions.Length == (costRelic ? 22 : lunacy ? 20 : roomMask ? 16 : 15) && hands.Length >= 2, "Missing original moon plays/hand callback boundaries.");
        if (phaseEnergyRelic)
        {
            Require(lunacy && !costRelic && !attackRelic && !healingRelic && !phaseHealRelic, "Phase-energy composition requires separate Lunacy controls.");
            RoomMoonSpellUpgradeChecks.PhaseEnergyRelic(actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!);
            RoomMoonSpellUpgradeChecks.PhaseEnergyCallbacks(root);
        }
        if (phaseHealRelic)
        {
            Require(lunacy && !costRelic && !attackRelic && !healingRelic, "Phase-healing composition requires separate Lunacy controls.");
            RoomMoonSpellUpgradeChecks.PhaseHealRelic(actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!);
            RoomMoonSpellUpgradeChecks.PhaseHealCallbacks(root);
        }
        if (healingRelic)
        {
            Require(lunacy && !costRelic && !attackRelic, "Healing relic composition requires separate Lunacy controls.");
            CombatContext context = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
            RoomMoonSpellUpgradeChecks.HealingRelic(context);
            Require(ids.All(id => context.FindCard(id)!.MaskDescriptor!.Definition.CardType == "Spell"
                    ? context.FindCard(id)!.Temporary.Offsets.Heal >= 10 : context.FindCard(id)!.Temporary.Offsets.Heal == 0),
                "Missing positive original healing contributions or excluded Monster/room-attachment controls.");
        }
        if (attackRelic)
        {
            Require(lunacy && !costRelic, "Attack relic composition requires separate Lunacy controls.");
            RoomMoonSpellUpgradeChecks.AttackRelic(actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!, ids.Skip(2).ToArray(), ids.Take(2).ToArray());
        }
        if (costRelic)
        {
            Require(lunacy, "Cost relic composition requires the Lunacy controls.");
            foreach (int index in new[] { 21, 22 })
            {
                FixtureValue row = actions.Single(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[index]);
                BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
                BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                RoomMoonSpellUpgradeChecks.CostRelic(before.Spawn.Train.Context!, ids.Skip(21).ToArray());
                Require(before.Energy == 0 && after.Energy == 0 && after.Spawn.Train.Context!.OtherPiles!
                    .Single(pile => pile.Name == "Purged").Cards.Any(token => token.InstanceId == ids[index]),
                    "Discounted original Blight/Junk must play free and purge after the moon/Conduit chain.");
            }
            Console.WriteLine("MOON-COST-RELIC PASS originalCheaperBlightScourge=true twoFreeOriginalPurges=true completeNativeCallbacks=true");
        }
        if (lunacy)
        {
            Require(roomMask, "Lunacy composition requires the original room mask.");
            foreach ((int index, int paid) in new[] { (17, phaseEnergyRelic ? 4 : 2), (19, 3), (20, 0) })
            {
                FixtureValue row = actions.Single(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[index]);
                BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
                BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                Require(before.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "e9fa09e5-13bd-4826-a8e5-969332cbfbbf" &&
                    before.Energy == paid && after.Energy == RoomMoonSpellUpgradeChecks.LunacyEnergyAfter(before, paid, phaseEnergyRelic) &&
                    after.MoonPhase == (paid % 2 == 0 ? before.MoonPhase : before.MoonPhase == "New" ? "Full" : "New"),
                    "Original masked Lunacy lost its actual paid cost, zero/even/odd phase progression or native definition.");
                int target = row.GetProperty("Action").GetProperty("TargetUnitId").GetInt32();
                CombatUnit prior = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target);
                CombatUnit next = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target);
                Require((next.Status("conduit")?.Stacks ?? 0) - (prior.Status("conduit")?.Stacks ?? 0) == paid &&
                    after.Spawn.Train.Context.Cards.Discard.Any(token => token.InstanceId == ids[index]),
                    "Original Lunacy must grant the actually paid Conduit amount and discard after the phase callbacks.");
            }
            Console.WriteLine($"LUNACY-ROOM-MASK PASS originalPaidCosts={(phaseEnergyRelic ? 4 : 2)},3,0 fullNativeStateAndRng=true");
        }
        foreach (FixtureValue row in hands)
        {
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!, actual = row.GetProperty("After").Deserialize<CombatContext>()!;
            string parent = Json(before); CombatContext predicted = CardMoonUpgradeModel.RefreshHand(before, out string? error);
            string? difference = error ?? ModelJson.Difference(Json(predicted), Json(actual));
            Require(row.GetProperty("Completed").GetBoolean() && difference == null && Json(before) == parent, "Complete native moon hand callback differs: " + difference);
        }
        foreach (int index in Enumerable.Range(5, 5).Concat(Enumerable.Range(11, 5)))
        {
            FixtureValue row = actions.Single(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[index]);
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CardInstanceState source = before.Spawn.Train.Context!.FindCard(ids[index])!, discarded = after.Spawn.Train.Context!.FindCard(ids[index])!;
            int phase = index < 10 ? 1 : 2;
            CardSourceTrait[] definitions = source.MaskDescriptor!.SourceTraits!.Where(trait => trait.Type == CardMoonUpgradeModel.Trait).ToArray();
            CardTraitValue[] traits = source.MaskDescriptor.BaseTraits.Where(trait => trait.RuntimeType == CardMoonUpgradeModel.Trait).ToArray();
            Require(before.Spawn.Train.Context.QueryFrame!.MoonPhase == phase && definitions.Length == traits.Length &&
                traits.Select((trait, position) => trait.MoonPhaseUpgradeState != null && trait.MoonPhaseUpgradeState.Active ==
                    ((trait.ParamInt & (roomMask && index != 5 ? definitions[position].Int2 == 1 ? 1 : phase | 1 : phase)) != 0)).All(active => active),
                "Original spell selected-room/private upgrade state differs.");
            Require(discarded.MaskDescriptor!.BaseTraits.Where(trait => trait.RuntimeType == CardMoonUpgradeModel.Trait)
                .All(trait => trait.MoonPhaseUpgradeState is { Active: false, Cached: true }) && after.Spawn.Train.Context.Cards.Discard.Any(token => token.InstanceId == source.InstanceId), "Played discard must remove/reset moon upgrades.");
            if (index is 6 or 12)
            {
                int target = row.GetProperty("Action").GetProperty("TargetUnitId").GetInt32();
                CombatUnit unit = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target);
                Require((unit.Status("dazed")?.Stacks ?? 0) == (phase == 2 ? 1 : 0), "Starstruck's upgraded dazed callback must depend on phase.");
            }
            if (index is 5 or 11)
            {
                int target = row.GetProperty("Action").GetProperty("TargetUnitId").GetInt32();
                Require(after.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == target)).RoomIndex == (phase == 1 || roomMask ? 0 : 1),
                    "Gravity must use Exclusive New in Eclipse and global phase outside the mask.");
            }
            if (index is 7 or 13)
            {
                CardUpgradeModifier numeric = source.Permanent.Upgrades.Single(upgrade => upgrade.CloneDamageBase > 0);
                Require(numeric.Stats.Damage == numeric.CloneDamageBase * (phase == 2 ? 5 : 1), "Celestial Storm magic power must refresh from authored base.");
            }
        }
        RecordedBattleChecks.Native(root);
        if (roomMask)
        {
            FixtureValue cast = actions.Single(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[16]);
            BattleTurnState installed = cast.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(installed.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single().MoonPhase != null,
                "Missing original paid Eclipse installation.");
        }
        Console.WriteLine($"ORIGINAL-MOON-CARD-UPGRADES PASS: five unchanged spells at both phases, full hand contexts, stronger magic-power refresh, upgraded OnCast, played/unplayed reset/redraw, 16 immutable chains; selectedRoomMask={roomMask} costRelic={costRelic} attackRelic={attackRelic} healingRelic={healingRelic}. Other unit, recast-copy and relic combinations remain separate boundaries.");
    }
    private static string Json(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
}
