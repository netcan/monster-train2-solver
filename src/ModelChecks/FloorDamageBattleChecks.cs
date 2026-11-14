using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FloorDamageBattleChecks
{
    private const string Divine = "83e4e7bd-e285-4239-8e99-6987b49db839", Dante = "6314e127-6a79-4b81-9d0e-9e95b6dab168",
        Covenant = "97c99633-b02b-4f84-a0c1-a9dd37ac1858", Candle = "913694e9-bace-4480-8691-24f699ae583e";
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "floor-damage-battle" && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Incomplete original floor-damage native victory.");
        FixtureValue evidence = root.GetProperty("FloorDamageBattle");
        int[] ids = evidence.GetProperty("ControlCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(),
            sweeps = evidence.GetProperty("Sweeps").EnumerateArray().ToArray(),
            timeline = evidence.GetProperty("Timeline").EnumerateArray().ToArray(),
            definitions = evidence.GetProperty("OriginalCards").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 12 &&
            ids.Length == 12 && ids.Distinct().Count() == 12 && actions.Length == 12 && actions.Select(row =>
                row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing twelve scheduled native inputs.");
        Require(definitions.Length == 3, "Missing original definitions.");
        for (int index = 0; index < 3; index++)
        {
            FixtureValue definition = definitions[index], effect = definition.GetProperty("Effects").EnumerateArray().Single();
            Require(definition.GetProperty("Id").GetString() == new[] { Divine, Dante, Covenant }[index] &&
                definition.GetProperty("Key").GetString() == new[] { "DivinePunishment", "DantesInferno", "ForbiddenCovenant" }[index] &&
                definition.GetProperty("Cost").GetInt32() == (index == 0 ? 3 : 2) &&
                effect.GetProperty("Type").GetString() == (index == 2 ? "CardEffectDamage" : "CardEffectDamageFloorByFloor") &&
                effect.GetProperty("Int").GetInt32() == (index == 2 ? 30 : 0) &&
                effect.GetProperty("Target").GetString() == (index == 2 ? "Room" : "Tower") &&
                effect.GetProperty("Team").GetString() == "Heroes", "Changed original spell definition.");
        }
        FixtureValue scaling = definitions[0].GetProperty("Traits").EnumerateArray().Single(trait =>
            trait.GetProperty("Type").GetString() == "CardTraitScalingAddDamage");
        Require(scaling.GetProperty("Int").GetInt32() == 20 && scaling.GetProperty("Tracked").GetString() == "MonolithLevel" &&
            definitions[1].GetProperty("Traits").EnumerateArray().Any(trait => trait.GetProperty("Type").GetString() == "CardTraitIgnoreArmor"),
            "Changed original level scaling or piercing.");
        FixtureValue reserve = definitions[2].GetProperty("Triggers").EnumerateArray().Single();
        Require(reserve.GetProperty("Kind").GetString() == "OnUnplayedNegative" &&
            reserve.GetProperty("Effects").EnumerateArray().Single() is var reserveHit &&
            reserveHit.GetProperty("Type").GetString() == "CardEffectDamageFloorByFloor" && reserveHit.GetProperty("Int").GetInt32() == 5 &&
            reserveHit.GetProperty("Target").GetString() == "Tower" && reserveHit.GetProperty("Team").GetString() == "Monsters",
            "Changed original reserve sweep.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext initial = Before(0).Spawn.Train.Context!;
        int[] candles = evidence.GetProperty("CandleIds").Deserialize<int[]>()!;
        Require(candles.Length == 3 && candles.All(id => initial.Statistics!.DeckCards!.Contains(id) && initial.FindCard(id)!.DataId == Candle),
            "Dante requires three actual permanent deck candles.");
        for (int index = 4; index < 12; index++)
        {
            BattleTurnState before = Before(index), after = After(index); string dataId = before.Spawn.Train.Context!.FindCard(ids[index])!.DataId;
            int energyDelta = index == 8 ? 1 : dataId == Dante || dataId == Covenant ? -2 : -3;
            Require(before.Spawn.Turn == new[] { 1, 2, 3, 4, 4, 4, 5, 6 }[index - 4] && after.Energy == before.Energy + energyDelta &&
                (index is 8 or 9 ? after.Spawn.Train.Context!.Cards.Discard : after.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards)
                    .Any(card => card.InstanceId == ids[index]),
                "Scheduled original cost, turn or exhaust routing differs.");
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            BattleActionResult played = BattleActionModel.PlayCard(before, actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!);
            Require(played.Supported, played.Reason ?? "Unsupported original floor spell.");
            RecordedBattleChecks.Compare(played.State!, after, false, "original floor spell successor");
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Original spell mutated its input.");
        }
        CombatUnit bossBefore = Before(9).Spawn.Train.Rooms[0].Units.Single(unit => unit.IsBoss == true),
            bossAfter = After(9).Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == bossBefore.Id);
        Require(bossAfter.Health == bossBefore.Health - 30, "Original Covenant's played room damage differs.");
        Require(sweeps.Count(row => row.GetProperty("DataId").GetString() == Divine) == 3 &&
            sweeps.Count(row => row.GetProperty("DataId").GetString() == Dante) == 1 &&
            sweeps.Count(row => row.GetProperty("DataId").GetString() == Covenant) >= 1 &&
            sweeps.All(row => row.GetProperty("Completed").GetBoolean()), "Missing complete original-card sweeps.");
        int reordered = 0, multiFloor = 0, armoredPiercing = 0, deathGold = 0;
        foreach (FixtureValue sweep in sweeps)
        {
            int number = sweep.GetProperty("Index").GetInt32(); string dataId = sweep.GetProperty("DataId").GetString()!;
            FixtureValue[] events = timeline.Where(row => row.GetProperty("Sweep").GetInt32() == number).ToArray(),
                hits = events.Where(row => row.GetProperty("Phase").GetString() == "Hit").ToArray();
            int[] actual = hits.Select(row => row.GetProperty("UnitId").GetInt32()).ToArray(),
                ordered = hits.OrderByDescending(row => row.GetProperty("Room").GetInt32()).ThenBy(row => row.GetProperty("Position").GetInt32())
                    .Select(row => row.GetProperty("UnitId").GetInt32()).ToArray();
            Require(actual.SequenceEqual(ordered) && actual.Distinct().Count() == actual.Length &&
                actual.Order().SequenceEqual(sweep.GetProperty("Collected").Deserialize<int[]>()!.Order()), "Native sweep target snapshot/order differs.");
            if (!actual.SequenceEqual(sweep.GetProperty("Collected").Deserialize<int[]>()!)) reordered++;
            if (hits.Select(row => row.GetProperty("Room").GetInt32()).Distinct().Count() >= 3) multiFloor++;
            Require(events[0].GetProperty("Phase").GetString() == "Remove" && events[^1].GetProperty("Phase").GetString() == "RemoveDone" &&
                sweep.GetProperty("AfterRoom").GetInt32() == hits[^1].GetProperty("Room").GetInt32(), "Missing boundary cleanup or final native floor focus.");
            int lastRoom = -1; bool removed = false; var removalGold = new Stack<int>();
            foreach (FixtureValue row in events)
            {
                string phase = row.GetProperty("Phase").GetString()!;
                if (phase == "Remove") removalGold.Push(row.GetProperty("Gold").GetInt32());
                if (phase == "RemoveDone")
                {
                    Require(removalGold.Count > 0, "Unpaired native death cleanup.");
                    if (row.GetProperty("Gold").GetInt32() > removalGold.Pop()) deathGold++;
                    if (removalGold.Count == 0) removed = true;
                }
                if (phase != "Hit") continue;
                int room = row.GetProperty("Room").GetInt32();
                Require(room == lastRoom || removed, "A new floor begins before native death cleanup completes.");
                Require(row.GetProperty("Focus").GetInt32() == room, "Damage did not follow native UI room focus.");
                int damage = dataId == Divine ? 20 * sweep.GetProperty("Level").GetInt32() : dataId == Dante ? 30 : 5;
                Require(row.GetProperty("Damage").GetInt32() == (dataId == Covenant ? 5 : 0), "Effect base damage differs.");
                FixtureValue traitDamage = events.Single(item => item.GetProperty("Phase").GetString() == "TraitDamage" &&
                    item.GetProperty("UnitId").GetInt32() == row.GetProperty("UnitId").GetInt32());
                Require(traitDamage.GetProperty("Damage").GetInt32() == damage && traitDamage.GetProperty("Focus").GetInt32() == room,
                    "Per-target level/candle/reserve trait damage differs.");
                CombatUnit before = row.GetProperty("Before").Deserialize<CombatUnit>()!,
                    after = events.Single(done => done.GetProperty("Phase").GetString() == "HitDone" &&
                        done.GetProperty("UnitId").GetInt32() == before.Id).GetProperty("After").Deserialize<CombatUnit>()!;
                Require(before.Team == (dataId == Covenant ? CombatTeam.Player : CombatTeam.Enemy) && !before.IsPyre,
                    "Original sweep hit the wrong team or engine.");
                if (dataId == Dante && before.Status("armor")?.Stacks > 0)
                {
                    Require(after.Health == Math.Max(0, before.Health - 30) && after.Status("armor")?.Stacks == before.Status("armor")?.Stacks,
                        "Original Dante did not bypass armor."); armoredPiercing++;
                }
                lastRoom = room; removed = false;
            }
            Require(removalGold.Count == 0, "Incomplete nested native death cleanup.");
        }
        Require(reordered > 0 && multiFloor > 0 && armoredPiercing > 0 && deathGold > 0,
            $"Missing native witnesses: reordered={reordered}, threeFloor={multiFloor}, piercing={armoredPiercing}, deathGold={deathGold}.");
        Require(sweeps.Where(row => row.GetProperty("DataId").GetString() == Divine).Select(row => row.GetProperty("Level").GetInt32())
            .SequenceEqual([0, 1, 2]), "Missing original zero/one/two-level controls.");
        FixtureValue[] refreshes = evidence.GetProperty("StatisticRefreshes").EnumerateArray().ToArray();
        Require(refreshes.Length > 0 && refreshes.All(row => row.GetProperty("After").GetInt32() != row.GetProperty("Before").GetInt32() &&
            row.GetProperty("Stack").GetString()!.Contains("CardUI.LateUpdate")) && root.GetProperty("TrainPhases").EnumerateArray()
                .Any(row => row.GetProperty("StoredCardCacheDiffers").GetBoolean()),
            "Missing observed asynchronous card-text cache refreshes.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root);
        Console.WriteLine($"FLOOR-DAMAGE-BATTLE PASS: three unchanged originals, Divine levels 0/1/2, three permanent Dante candles and armor bypass, reserve friendly damage, top-down physical order, floor-boundary death gold, full successors/RNG and 16 immutable complete branches; {sweeps.Length} sweeps.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
