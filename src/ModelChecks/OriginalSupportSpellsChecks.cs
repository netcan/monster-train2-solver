using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSupportSpellsChecks
{
    private static readonly string[] Originals = ["8ccdb317-e3e3-47b1-95c0-662ffe3fb7c6", "2ff4ef73-00f4-4bd5-b868-607320188980", "acc303db-30ea-4ff1-8e46-97344210cb6b",
        "6534ff5b-d036-4b2f-ac21-70ee4d68700e", "24e4729c-f256-4524-84c8-99564affb42c", "f25d8b79-056a-4db3-940d-c6cfc540bc4d",
        "4ca4ed47-9867-4004-acf0-171c3282751f", "ac110052-0000-4000-8000-000000000008", "297c2618-1a10-4367-8b7c-d068a6c5b3f4",
        "3c96e9b7-bdfb-4784-88f7-4b01bd58b4b7", "1754dece-cf0b-455e-b9ca-d238134d7762", "c354dd2c-0087-4eae-82d9-d4ab17b15f19",
        "996a0953-168a-44b9-bb90-5e46cbbb3e09", "7f3dcb62-e418-4120-861c-02f8372173ce", "7f3dcb62-e418-4120-861c-02f8372173ce"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids, bool isolated = false)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        if (isolated)
        {
            string? replayKind = source.PlayRules!.Cards.Single(rule => rule.DataId == source.Spawn.Train.Context!.FindCard(ids[index])!.DataId)
                .Effects.FirstOrDefault(effect => effect.Type == "PlayUnitTrigger")?.UnitTriggerKind;
            IEnumerable<PlayCardAction> candidates = index < 4 ? Array.Empty<PlayCardAction>() : BattleActionModel.EnumerateSupportedPlays(source, ids[index]);
            if (replayKind != null) candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Id == action.TargetUnitId && unit.Triggers.Any(trigger => trigger.Kind == replayKind)));
            PlayCardAction? chosen = index < 4 ? new PlayCardAction(ids[index], index < 3 ? index : 0,
                playerPosition: index < 3 ? 0 : -1) : candidates.FirstOrDefault();
            return chosen != null && BattleActionModel.PlayCard(source, chosen).Supported ? chosen : null;
        }
        int target = index is >= 8 and <= 16 or >= 23 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
        var action = new PlayCardAction(ids[index], index is 2 or 22 ? 1 : 0, playerPosition: index < 3 ? (index == 1 ? 1 : 0) : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Steelguard()
    {
        foreach (int armor in new[] { 14, 15, 16, 30 })
        {
            var unit = new CombatUnit(1, "steelguard-edge", CombatTeam.Player, 0, 100, 100, false, false, false,
                [new CombatStatus("armor", armor, 1, removeWhenTriggered: true), new CombatStatus("steelguard", 1, 15)]);
            var parent = new RoomCombatState(0, false, [unit], []);
            string original = Json(parent);
            RoomCombatResult hit = RoomCombatModel.ApplyCardDamage(parent, 1, 50);
            Require(hit.Supported && hit.State!.Units.Single().Health == (armor > 15 ? 100 : 50 + armor) &&
                (hit.State.Units.Single().Status("armor")?.Stacks ?? 0) == Math.Max(0, armor - 15) && Json(parent) == original,
                "Steelguard exact-cap and over-cap boundaries or parent isolation differ.");
        }
        Console.WriteLine("STEELGUARD PASS: armor fourteen/fifteen/sixteen/thirty, exact cap versus full-block branch and immutable parent.");
    }
    internal static void Native(FixtureValue root)
    {
        if (root.GetProperty("ModifierScenario").GetString()?.StartsWith("original-isolated-spells:", StringComparison.Ordinal) == true)
        { NativeIsolated(root); return; }
        Steelguard();
        Require(root.GetProperty("OriginalSupportSpellsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original support spell witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalSupportSpellsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 27 && actions.Length == 27, "Missing thirteen originals, repeated X, healing and four Steelguard damage controls.");
        for (int i = 8; i < 23; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!; var changed = after.Spawn.Train.Context!;
            var card = context.FindCard(ids[i])!; var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 8]);
            Require(card.DataId == rule.DataId && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == (i >= 21 ? before.Energy : rule.Cost - (i == 14 ? 3 : 0)), "Original support identity, cost or energy gain differs.");
            CombatUnit Actor(BattleTurnState state) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]);
            CombatUnit a = Actor(before), b = Actor(after);
            int Armor(CombatUnit unit) => unit.Status("armor")?.Stacks ?? 0;
            if (i == 8) Require(b.MaxHealth - a.MaxHealth == -2 && b.Health - a.Health == -2 && b.Attack - a.Attack == 12, "Razorsharp Edge must add twelve attack and remove two current/max HP.");
            if (i == 9) Require(b.Attack - a.Attack == 6 && b.Status("spikes")?.Stacks == 6, "Sharpen must add six attack then six Spikes.");
            if (i == 10) Require(b.MaxHealth - a.MaxHealth == 4 && b.Health - a.Health == 4 && b.Status("regen")?.Stacks == 4, "Regen Potion must add four max/current HP and four Regen.");
            if (i == 11) Require(b.Status("endless")?.Stacks == 1, "Remnant Pact must attach Endless as an upgrade.");
            if (i == 12) Require(b.Status("steelguard")?.Stacks == 1, "Machinist's Tome must attach Steelguard as an upgrade.");
            if (i == 13) Require(b.Attack - a.Attack == 3 && changed.Cards.DrawModifier - context.Cards.DrawModifier == 1,
                "Rootseeds must add three attack and schedule one next-turn draw.");
            if (i == 14) Require(b.Status("buff")?.Stacks == 3 && b.Status("scorch")?.Stacks == 2, "Perils of Production must apply Rage/Emberdrain before three energy.");
            if (i == 15)
            {
                Require(b.Health - a.Health == 15 && Json(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy)) ==
                    Json(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy)), "Authored healing control must restore fifteen without changing enemies.");
            }
            if (i == 16) Require(b.Health == b.MaxHealth && b.Health > a.Health && b.Status("conduit")?.Stacks == 4 && changed.MagicPower!.Any(room => room.Conduit?.Units.Any(unit => unit.UnitId == b.Id && unit.Power == 4) == true), "Glittering Draught must positively heal and install four captured Conduit.");
            if (i == 17)
            {
                CombatUnit oldControl = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[1]), newControl = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[1]);
                Require(a.Health == a.MaxHealth && b.Health == a.Health && Armor(b) - Armor(a) == 14 && newControl.Health - oldControl.Health == 14 && Armor(newControl) == Armor(oldControl), "Tidal Turning must heal fourteen with Conduit and convert full fourteen overheal to armor only on the full ally.");
            }
            if (i == 18) Require(context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 3 &&
                before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player).All(unit => Armor(after.Spawn.Train.Rooms[0].Units.Single(other => other.Id == unit.Id)) - Armor(unit) == 6), "Shelter must use three Corruption for six armor on both allies.");
            if (i == 19) Require(changed.RoomCapacities!.Single(room => room.RoomIndex == 0).PermanentCorruption - context.RoomCapacities!.Single(room => room.RoomIndex == 0).PermanentCorruption == 1 &&
                changed.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 4, "Eternal Kinstone must add one permanent and immediate Corruption.");
            if (i == 20) Require(after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health - before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 6,
                "Pay Off must positively heal the damaged Pyre from three Hoard.");
            if (i >= 21)
            {
                int floor = i == 21 ? 0 : 1;
                var oldRoom = context.RoomCapacities!.Single(room => room.RoomIndex == floor); var newRoom = changed.RoomCapacities!.Single(room => room.RoomIndex == floor);
                Require(newRoom.PlayerMaximum - oldRoom.PlayerMaximum == before.Energy && newRoom.EnemyMaximum == oldRoom.EnemyMaximum &&
                    (i == 21 ? before.Energy == 21 : before.Energy == 0), "Renovate's Both team mask must select friendly capacity, adding twenty-one/zero from spent X energy while preserving enemy capacity.");
            }
            Require(Json(before.Spawn.Train.Rooms[1].Units) == Json(after.Spawn.Train.Rooms[1].Units), "Support spell changed the neighboring floor control.");
            if (i == 9) Require(context.BattleRng.Next().Equals(changed.BattleRng), "Sharpen chance-100 must consume one RNG draw.");
            else Require(context.BattleRng.Equals(changed.BattleRng), "Fixed support spell consumed Battle RNG.");
            bool consume = CardExhaustModel.HasConsume(card);
            Require(consume == (i is 10 or 11 or 12 or 16 or 17 or 18 or 20) && (consume ? changed.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]) : changed.Cards.Discard.Any(token => token.InstanceId == ids[i])), "Original support Consume/discard route differs.");
        }
        for (int i = 23; i < 27; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit a = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]), b = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]);
            int oldArmor = a.Status("armor")?.Stacks ?? 0, newArmor = b.Status("armor")?.Stacks ?? 0;
            Require(a.Status("steelguard")?.ParamInt == 15 && (i switch {
                23 => oldArmor > 15 && newArmor == oldArmor && a.Health - b.Health == 54,
                24 => oldArmor > 15 && oldArmor - newArmor == 15 && a.Health == b.Health,
                25 => oldArmor == 5 && newArmor == 0 && a.Health - b.Health == 49,
                26 => oldArmor == 0 && newArmor == 0 && a.Health - b.Health == 54,
                _ => false }), "Steelguard must cap armor loss while fully blocking above-cap hits, fall back below the cap and honor piercing/no-armor controls.");
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-SUPPORT-SPELLS PASS: thirteen unchanged originals/fourteen plays, exact unit stats/status upgrades, next-turn draw/energy, positive healing, Conduit overheal, corruption armor/permanence, Hoard Pyre healing, positive/zero X capacity and Steelguard damage controls; 16 full immutable chains.");
    }
    private static void NativeIsolated(FixtureValue root)
    {
        Require(root.GetProperty("OriginalSupportSpellsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed isolated spell witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        string[] originals = root.GetProperty("ModifierScenario").GetString()!["original-isolated-spells:".Length..].Split(',');
        int[] ids = root.GetProperty("OriginalSupportSpellsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(originals.Length is > 0 and <= 30 && originals.Distinct().Count() == originals.Length &&
            originals.All(id => Guid.TryParseExact(id, "D", out _)) && ids.Length == originals.Length + 4 && actions.Length == ids.Length,
            "Isolated spell batch must contain its exact controls and distinct original identities.");
        for (int i = 0; i < actions.Length; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!,
                after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[i])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            Require(action.CardInstanceId == ids[i], "Isolated spell action order differs.");
            if (i < 3)
            {
                CombatUnit host = after.Spawn.Train.Rooms[i].Units.Single(unit => unit.SpawnerCardId == ids[i]);
                Require(action.RoomIndex == i && action.PlayerPosition == 0 && rule.Cost == 0 &&
                    card.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678" && host.Health == 10000 &&
                    host.Size == 1 && host.Modifiers!.AttackDamage == 1000, "Isolated spell host differs.");
                continue;
            }
            if (i == 3)
            {
                Require(card.DataId == "ac110052-0000-4000-8000-000000000099" && rule.Cost == 0 &&
                    rule.Effects.Single().Type == "GainEnergy" && after.Energy - before.Energy == rule.Effects.Single().Value,
                    "Isolated spell energy control differs.");
                continue;
            }
            Require(card.DataId == originals[i - 4] && rule.CardType is "Spell" or "Equipment" or "Monster" && rule.ExternalInteractions.Count == 0 &&
                after.Spawn.Train.Context!.Statistics!.Value(ids[i], "TimesPlayed", "ThisBattle") == 1 &&
                before.Energy >= after.Energy, "Original spell identity, projection or positive native play differs: " + rule.AssetKey);
            CardGenerationRule[] generators = rule.Effects.Where(effect => effect.Type == "Generate").Select(effect => effect.Generation!).ToArray();
            if (rule.AssetKey == "ReplicatorRay")
            {
                CombatUnit oldHost = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId),
                    newHost = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
                CombatUnit clone = after.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit =>
                    unit.Modifiers?.IsClone == true && !before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(old => old.Id == unit.Id));
                Require(oldHost.EquipmentCards?.Count == 1 && newHost.EquipmentCards?.Count == 0 && clone.EquipmentCards?.Count == 0 &&
                    clone.Attack == newHost.Attack && clone.Health == newHost.Health && clone.SpawnerCardId != newHost.SpawnerCardId &&
                    after.Spawn.Train.Context!.SpawnPoints != null,
                    "Replicator Ray must remove the original equipment before making a fresh-spawner, unequipped physical clone.");
            }
            if (rule.AssetKey == "CarCollapse")
            {
                RoomCapacityState oldRoom = before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex),
                    newRoom = after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex);
                Require(oldRoom.PlayerMaximum == 5 && newRoom.PlayerMaximum == 1 && newRoom.EnemyMaximum == oldRoom.EnemyMaximum,
                    "Car Collapse must clamp the friendly capacity to one and preserve enemy capacity.");
            }
            if (rule.AssetKey == "Hammerfall")
            {
                CombatUnit oldHost = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId),
                    newHost = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
                Require(oldHost.Team == CombatTeam.Player && newHost.Attack - oldHost.Attack == 20 && newHost.Health == oldHost.Health &&
                    newHost.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "Hammerfall_Upgrade" && upgrade.Stats.Damage == 20),
                    "Hammerfall must skip enemy damage on the friendly target and apply twenty attack through its native scaling upgrade.");
            }
            if (generators.Length > 0)
            {
                CombatContext changed = after.Spawn.Train.Context!;
                Require(changed.NextCardId > before.Spawn.Train.Context.NextCardId &&
                    changed.CardRegistry!.Any(added => added.InstanceId >= before.Spawn.Train.Context.NextCardId &&
                        generators.Any(generator => generator.Pool.Any(creation => creation.DataId == added.DataId))) &&
                    generators.All(generator => generator.CopyModifiers && generator.Destination == "DiscardPile" || generator.DiscardCopyUpgrades.Count == 0),
                    "Original generation must positively allocate a native pool card and omit unreachable discard-copy upgrade branches.");
            }
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine($"ORIGINAL-ISOLATED-SPELLS PASS: {originals.Length} unchanged originals, independent control identities and full immutable chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
