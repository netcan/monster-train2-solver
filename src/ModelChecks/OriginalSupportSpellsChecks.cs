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
            if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId is
                "2ed78ce2-13c3-4e82-963f-99c9ea12d0f0" or "175a7730-7a1c-4da1-97b8-d799e7570396" or "98276338-b18f-4ffa-83a2-9501c2741f03" &&
                ids.Any(id => source.Spawn.Train.Context.FindCard(id)?.DataId is "3cb0976c-7cf5-4470-bb5a-7eacfb404e9a" or "1362aa8c-d7b4-49b9-a4f7-268f47de4aa6" or "a5b05b40-45be-41ea-8c56-dd18f3feb1b4"))
                candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Player && unit.Modifiers?.RoomStatuses?.Count > 0));
            if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId is "2ed78ce2-13c3-4e82-963f-99c9ea12d0f0" or "175a7730-7a1c-4da1-97b8-d799e7570396" &&
                ids.Any(id => source.Spawn.Train.Context.FindCard(id)?.DataId == "acb9a1cb-f6fc-40df-b287-fc82b8df156b"))
                candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && source.Spawn.Train.Context.FindCard(unit.SpawnerCardId)?.DataId == "acb9a1cb-f6fc-40df-b287-fc82b8df156b"));
            if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "25787bff-d2fb-4d9a-a636-d96a5863d559" &&
                ids.Any(id => source.Spawn.Train.Context.FindCard(id)?.DataId == "f892b119-fa6a-4f69-9369-10e29bcfd96e"))
                candidates = candidates.OrderByDescending(action => action.RoomIndex == 1);
            if (replayKind != null) candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Id == action.TargetUnitId && unit.Triggers.Any(trigger => trigger.Kind == replayKind)));
            if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "f3406a6d-69f0-46e8-8db9-d829db017716")
                candidates = candidates.OrderByDescending(action => action.RoomIndex == 2 && action.PlayerPosition == 1);
            if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "ae114dc4-d08f-4909-a2ad-d99215661463")
                candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Enemy && unit.Health + (unit.Status("armor")?.Stacks ?? 0) <= 8));
            if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "f29d063b-cb41-44ee-8118-4db66b6f8195")
                candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Enemy && unit.Health <= 5));
            if (index >= 4 && source.Spawn.Train.Context!.FindCard(ids[index])!.DataId == "cc374cb3-1675-4deb-93bc-53e30b2045fe")
                candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && unit.Health + (unit.Status("armor")?.Stacks ?? 0) <= 10 &&
                        (ids.Any(id => source.Spawn.Train.Context!.FindCard(id)?.DataId == "0c8ff108-478c-4c1c-9c49-f6a297b4e0d8")
                            ? unit.Team == CombatTeam.Player && unit.Status("horde")?.Stacks > 0 : unit.Team == CombatTeam.Enemy)));
            if (index >= 4 && source.PlayRules!.Cards.Single(rule => rule.DataId == source.Spawn.Train.Context!.FindCard(ids[index])!.DataId).AssetKey == "ScrapForParts")
                candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && unit.EquipmentCards?.Count > 0));
            if (index >= 4 && ids.Any(id => source.Spawn.Train.Context!.FindCard(id)?.DataId == "75ce6859-dd29-4c88-a67a-5159a753f2db") &&
                source.PlayRules!.Cards.Single(rule => rule.DataId == source.Spawn.Train.Context!.FindCard(ids[index])!.DataId).AssetKey is "RitualOfBattle" or "ScrapForParts")
                candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && unit.AssetKey == "MonsterBalmabello"));
            if (index >= 4 && ids.Any(id => source.Spawn.Train.Context!.FindCard(id)?.DataId == "6a2bbbef-1282-4d28-8890-1daf0c1be50c") &&
                source.PlayRules!.Cards.Single(rule => rule.DataId == source.Spawn.Train.Context!.FindCard(ids[index])!.DataId).AssetKey == "HiddenPassage")
                candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && unit.AssetKey == "MonsterDeathsDancer"));
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
        bool returnedRecast = originals.FirstOrDefault() == "bae01a8c-25dd-4a08-b08a-5f17a5f3f52a" &&
            originals.Contains("6a3633b6-4ecc-43b1-bb56-548f56cf1d60");
        Require(originals.Length is > 0 and <= 30 && originals.Distinct().Count() == originals.Length &&
            originals.All(id => Guid.TryParseExact(id, "D", out _)) && ids.Length == originals.Length + (returnedRecast ? 5 : 4) &&
            (!returnedRecast || ids[^1] == ids[4]) && actions.Length == ids.Length,
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
            bool recast = returnedRecast && i == ids.Length - 1;
            Require(card.DataId == originals[recast ? 0 : i - 4] && rule.CardType is "Spell" or "Equipment" or "Monster" or "Junk" or "TrainRoomAttachment" && rule.ExternalInteractions.Count == 0 &&
                after.Spawn.Train.Context!.Statistics!.Value(ids[i], "TimesPlayed", "ThisBattle") == (recast ? 2 : 1) &&
                (before.Energy >= after.Energy || rule.Effects.Any(effect => effect.Type == "GainEnergy")),
                "Original spell identity, projection or positive native play differs: " + rule.AssetKey);
            CardGenerationRule[] generators = rule.Effects.Where(effect => effect.Type == "Generate").Select(effect => effect.Generation!).ToArray();
            if (originals.Contains("f892b119-fa6a-4f69-9369-10e29bcfd96e"))
            {
                if (rule.AssetKey == "EdgePrior")
                    Require(after.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.SpawnerCardId == card.InstanceId)
                        .Modifiers!.RoomResources?.Single() is { Type: "RoomStateHealCostModifier", Amount: -1, DisallowedInDeployment: false },
                        "Original Edge Prior must retain its exact native healing-cost room modifier.");
                if (rule.AssetKey is "Engraft" or "RegenPotion" or "StarterTorch" or "FocusedGrowth")
                {
                    bool discounted = rule.AssetKey is "Engraft" or "RegenPotion";
                    int gained = rule.Effects.Where(effect => effect.Type == "GainEnergy").Sum(effect => effect.Value);
                    int baseCost = rule.AssetKey == "FocusedGrowth" ? 2 : 1;
                    Require(rule.Cost == baseCost && (discounted ? action.RoomIndex == 0 : rule.AssetKey != "FocusedGrowth" || action.RoomIndex == 1) &&
                        before.Energy + gained - after.Energy == (discounted ? 0 : baseCost),
                        "Edge Prior must discount healing and Regen on its floor, retaining normal cost for damage and healing on another floor.");
                }
            }
            if (rule.AssetKey is "Pandemic" or "SpreadingSpores" or "Contagious")
            {
                CombatContext old = before.Spawn.Train.Context!, changed = after.Spawn.Train.Context!;
                CardGenerationRule generation = generators.Single();
                CardInstanceState copy = changed.CardInstances!.Single(item => item.InstanceId >= old.NextCardId);
                Require(generation.Destination == "DiscardPile" && generation.CopyModifiers && !generation.IgnoreTemporaryModifiers &&
                    copy.DataId == card.DataId && changed.NextCardId == old.NextCardId + 1 &&
                    changed.Cards.Discard.Any(token => token.InstanceId == copy.InstanceId) &&
                    generation.DiscardCopyUpgrades.Count == 3 && generation.DiscardCopyUpgrades.All(item =>
                        item.Upgrade.ExternalInteractions.Count == 0 && item.IgnoredCopyUpgradeIds?.Count == item.Upgrade.Lifecycle!.ReplacedAssets.Count),
                    "Original discarded copy must retain its identity and captured cleanup exclusion IDs.");
                foreach (DiscardGenerationUpgrade cleanup in generation.DiscardCopyUpgrades.Where(item =>
                    card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.DataId == item.RequiredUpgradeId)))
                    Require(copy.Temporary.Upgrades.Any(upgrade => upgrade.DataId == cleanup.Upgrade.DataId) &&
                        copy.Permanent.Upgrades.Concat(copy.Temporary.Upgrades).All(upgrade => !cleanup.IgnoredCopyUpgradeIds!.Contains(upgrade.DataId)) &&
                        !CardBranchMaskModel.Resolve(copy).Traits.Contains("CardTraitEphemeral"),
                        "Discarded copy must install cleanup before copying and exclude the source Ephemeral upgrade.");
                if (CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitEphemeral"))
                {
                    Require(after.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Count(token => token.InstanceId == card.InstanceId) == 2 &&
                        after.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.All(token => token.InstanceId != card.InstanceId) &&
                        Json(changed.FindCard(card.InstanceId)!.Temporary.Upgrades) == Json(card.Temporary.Upgrades),
                        "Ephemeral source must purge twice without changing its upgrades or leaving a pending exhausted copy.");
                    if (CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitExhaustState"))
                        Require(changed.Statistics!.Value(card.InstanceId, "TimesExhausted", "ThisBattle") -
                            old.Statistics!.Value(card.InstanceId, "TimesExhausted", "ThisBattle") == 1,
                            "Ephemeral plus Consume records exhaustion once before cancelling the standby return.");
                    RoomCombatState oldRoom = before.Spawn.Train.Rooms[action.RoomIndex], newRoom = after.Spawn.Train.Rooms[action.RoomIndex];
                    if (rule.AssetKey == "Pandemic" && oldRoom.Units.Any(unit => unit.AssetKey == "MonsterMorelMistress"))
                        Require(oldRoom.Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Id != action.TargetUnitId).Any(unit =>
                            (newRoom.Units.Single(item => item.Id == unit.Id).Status("decay")?.Stacks ?? 0) - (unit.Status("decay")?.Stacks ?? 0) == 8),
                            "Ephemeral must fire Morel Mistress's manual exhaustion callback exactly once before discard.");
                }
            }
            if (rule.AssetKey == "FunguyInASuit")
            {
                RoomCombatState oldRoom = before.Spawn.Train.Rooms[action.RoomIndex], newRoom = after.Spawn.Train.Rooms[action.RoomIndex];
                CombatUnit? oldHorde = oldRoom.Units.SingleOrDefault(unit => unit.Team == CombatTeam.Player && unit.Status("horde")?.Stacks > 0);
                CombatUnit horde = newRoom.Units.Single(unit => unit.Team == CombatTeam.Player && unit.Status("horde")?.Stacks > 0);
                Require(rule.Effects[0].SourceStatusMultiplier == "horde" && rule.Effects[0].Statuses.Single().Stacks == 3 &&
                    horde.Status("horde")!.Stacks == (oldHorde?.Status("horde")?.Stacks ?? 0) + 3 &&
                    horde.Status("armor")!.Stacks == (oldHorde?.Status("armor")?.Stacks ?? 0) + 4 * horde.Status("horde")!.Stacks &&
                    (oldHorde == null ? horde.Id >= before.Spawn.NextUnitId : horde.Id == oldHorde.Id) &&
                    oldRoom.Units.Where(unit => unit.Id != horde.Id).All(unit =>
                        (newRoom.Units.Single(changed => changed.Id == unit.Id).Status("armor")?.Stacks ?? 0) == (unit.Status("armor")?.Stacks ?? 0)),
                    "Funguy in a Suit must add three unmultiplied Horde with no source actor, then grant four Armor per resulting Horde only to its native subtype targets.");
            }
            if (rule.AssetKey == "MakingOfAMorsel")
            {
                CombatContext old = before.Spawn.Train.Context!, changed = after.Spawn.Train.Context!;
                CardGenerationRule generation = generators.Single();
                CardInstanceState generated = changed.CardInstances!.Single(card => card.InstanceId >= old.NextCardId);
                Require(rule.Effects.Single().Target == "Hand" && generation.Count == 0 && generation.Pool.Count == 1 &&
                    generated.DataId == generation.Pool.Single().DataId && generated.DataId == "536e6de0-7ccf-412a-9a4e-ec55ab3988e5" &&
                    changed.Cards.Hand.Any(token => token.InstanceId == generated.InstanceId) && changed.NextCardId == old.NextCardId + 1 &&
                    !old.BattleRng.Equals(changed.BattleRng),
                    "Making of a Morsel must use its native Hand target to generate exactly one Morsel Miner, retaining the one-entry pool RNG draw.");
            }
            bool corruptUpgradeBatch = originals.Contains("4e8b6d4c-ee6b-46e7-8804-b94489af371d") ||
                originals.Contains("6a3633b6-4ecc-43b1-bb56-548f56cf1d60");
            if (rule.AssetKey is "BoundingEchoes" or "ReturnedSoul")
            {
                CombatContext old = before.Spawn.Train.Context!, changed = after.Spawn.Train.Context!;
                CardInstanceState recipient = old.CardInstances!.Single(item => item.DataId == "bae01a8c-25dd-4a08-b08a-5f17a5f3f52a"),
                    upgraded = changed.FindCard(recipient.InstanceId)!;
                bool returned = rule.AssetKey == "ReturnedSoul";
                string upgradeKey = returned ? "WurmReturnEnhanceCorrupt" : "WurmEnhanceCorrupt";
                Require(upgraded.Temporary.Upgrades.Count(upgrade => upgrade.AssetKey == upgradeKey) == 1 &&
                    CardExhaustModel.RefreshTraits(upgraded).CombinedTraits!.Count(trait => trait.RuntimeType == CardCorruptStateModel.Trait) == 1 &&
                    Json(upgraded.Permanent) == Json(recipient.Permanent) &&
                    changed.Cards.Hand.Any(token => token.InstanceId == recipient.InstanceId) &&
                    (returned ? old.Cards.Discard.Any(token => token.InstanceId == recipient.InstanceId) &&
                        CardExhaustModel.HasConsume(upgraded) : old.Cards.Hand.Any(token => token.InstanceId == recipient.InstanceId)),
                    "Original Corrupt upgrade must reach the existing hand/discard recipient, with Returned Soul also installing temporary Consume.");
            }
            if (corruptUpgradeBatch && CardExhaustModel.RefreshTraits(card).CombinedTraits!.Any(trait => trait.RuntimeType == CardCorruptStateModel.Trait))
            {
                CombatContext old = before.Spawn.Train.Context!, changed = after.Spawn.Train.Context!;
                BattleResourceState resources = old.Resources!;
                Require(resources.CorruptionPerCorruptTrait == 1 &&
                    resources.WithVisibleDeckCount(resources.VisibleDeckCount).CorruptionPerCorruptTrait == 1 &&
                    resources.WithPyreModifiedBaseAttack(resources.PyreModifiedBaseAttack).CorruptionPerCorruptTrait == 1 &&
                    resources.WithBasePyreMaxHealth(resources.BasePyreMaxHealth).CorruptionPerCorruptTrait == 1 &&
                    CardExhaustModel.RefreshTraits(card).CombinedTraits!.Count(trait => trait.RuntimeType == CardCorruptStateModel.Trait) == 1 &&
                    changed.RoomCapacities!.All(room => room.Corruption == old.RoomCapacities!.Single(previous => previous.RoomIndex == room.RoomIndex).Corruption +
                        (room.RoomIndex == action.RoomIndex ? 1 : 0)) &&
                    Json(changed.FindCard(card.InstanceId)!.Permanent) == Json(card.Permanent) &&
                    (!originals.Contains("6a3633b6-4ecc-43b1-bb56-548f56cf1d60") ||
                        changed.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == card.InstanceId)),
                    "The upgraded original spell must add exactly the captured Corrupt amount on its played floor, preserving other rooms and permanent upgrades.");
            }
            if (rule.AssetKey is "SilverInk" or "MagicPowerHand")
            {
                CombatContext old = before.Spawn.Train.Context!, changed = after.Spawn.Train.Context!;
                foreach (string dataId in new[] { "bae01a8c-25dd-4a08-b08a-5f17a5f3f52a", "d81f1f43-f20e-4459-9fc4-19e5836a876f" })
                {
                    CardInstanceState control = old.CardInstances!.Single(item => item.DataId == dataId),
                        upgraded = changed.FindCard(control.InstanceId)!;
                    string key = rule.AssetKey == "SilverInk" ? "SilverInk_Upgrade" : "Magic";
                    CardUpgradeModifier applied = upgraded.Temporary.Upgrades.Single(item => item.AssetKey == key);
                    Require(old.Cards.Hand.Any(token => token.InstanceId == control.InstanceId) &&
                        applied.Stats.Damage == (rule.AssetKey == "SilverInk" ? 15 : 50) && applied.Stats.Heal == applied.Stats.Damage &&
                        upgraded.Permanent.Upgrades.Count == control.Permanent.Upgrades.Count,
                        "Filtered hand upgrade must positively enhance the native damage/heal controls with Attuned scaling.");
                }
                foreach (string excluded in new[] { "d14a50f3-728d-43e1-87f0-ef1b013f6678", "d861b6a6-d8ef-4d58-8ebd-9f0abc276456", "ac110052-0000-4000-8000-000000000099" })
                {
                    Require(old.Cards.Hand.Any(token => token.DataId == excluded), "Missing independent hand filter exclusion control.");
                    foreach (CardToken token in old.Cards.Hand.Where(token => token.DataId == excluded))
                        Require(Json(old.FindCard(token.InstanceId)!.Temporary) == Json(changed.FindCard(token.InstanceId)!.Temporary),
                            "Hand upgrade leaked to a monster, blacklisted heal or non-damage/heal spell.");
                }
            }
            if (rule.AssetKey == "HiddenPassage" && originals.Contains("6a2bbbef-1282-4d28-8890-1daf0c1be50c"))
            {
                CombatUnit dancer = after.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == action.TargetUnitId);
                Require(dancer.AssetKey == "MonsterDeathsDancer" && dancer.AttackBeforeStatuses == 11 &&
                    dancer.Triggers.Single(trigger => trigger.Kind == "OnShift").HasTriggered &&
                    after.Spawn.Train.Rooms[1].Units.Where(unit => unit.Team == CombatTeam.Player).All(unit =>
                        unit.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "DeathDancerUpgrade" && upgrade.Stats.Damage == 4)) &&
                    after.Spawn.Train.Rooms.Where(room => room.RoomIndex != 1).SelectMany(room => room.Units).All(unit =>
                        unit.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "DeathDancerUpgrade")),
                    "Death's Dancer must gain four Attack with its destination-floor allies on Shift, preserving other-floor controls.");
            }
            if (rule.AssetKey == "StarterDamageForMorsel")
            {
                CombatContext old = before.Spawn.Train.Context!, changed = after.Spawn.Train.Context!;
                CardActionEffect generator = card.MaskDescriptor!.KillTriggers!.Single().Actions.Single();
                CardInstanceState[] generated = changed.CardInstances!.Where(item => item.InstanceId >= old.NextCardId).ToArray();
                Require(generator.Target == "Room" && generator.Generation?.Count == 1 && generated.Length > 0 &&
                    generated.All(item => generator.Generation.Pool.Any(definition => definition.DataId == item.DataId)) &&
                    changed.NextCardId == old.NextCardId + generated.Length && !old.BattleRng.Equals(changed.BattleRng) &&
                    after.Spawn.Train.Rooms[action.RoomIndex].Units.Count(unit => unit.Team == CombatTeam.Enemy) <
                        before.Spawn.Train.Rooms[action.RoomIndex].Units.Count(unit => unit.Team == CombatTeam.Enemy),
                    "Shadesplitter must positively kill a native enemy and roll its original weighted Morsel pool through the Room-targeted OnKill effect.");
            }
            if (rule.AssetKey is "TFB_EntropyScourge" or "TFB_PyreAttack")
            {
                CardSourceTrait ephemeral = card.MaskDescriptor!.SourceTraits!.Single(trait => trait.Type == "CardTraitEphemeral");
                int expected = rule.AssetKey == "TFB_PyreAttack" ? 3 : 2;
                Require(ephemeral.Int == 0 &&
                    after.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Count(token => token.InstanceId == card.InstanceId) -
                    before.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Count(token => token.InstanceId == card.InstanceId) == expected &&
                    after.Spawn.Train.Context!.Cards.Hand.All(token => token.InstanceId != card.InstanceId) &&
                    after.Spawn.Train.Context.Statistics!.DeckCards!.All(id => id != card.InstanceId),
                    "Original Ephemeral must purge twice on play; the separate SelfPurge callback must create a third native reference.");
            }
            if (rule.AssetKey == "Boilerplate")
            {
                CombatContext oldContext = before.Spawn.Train.Context!, newContext = after.Spawn.Train.Context!;
                CardInstanceState changed = newContext.FindCard(card.InstanceId)!;
                int points = oldContext.QueryFrame!.ForgePoints!.Value;
                CombatUnit oldTarget = before.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == action.TargetUnitId),
                    newTarget = after.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == action.TargetUnitId);
                Require(rule.OnForgeEffects?.Count == 1 && rule.OnForgeEffects[0].Upgrade!.MaskMetadata!.Statuses.Single().FromPermanentUpgrade &&
                    changed.LastForgedAmount == points && newContext.QueryFrame!.ForgePoints == 0 &&
                    newTarget.EquipmentCards!.Contains(card.InstanceId), "Boilerplate must preserve its native forge callback and equip ownership.");
                if (points > 0)
                {
                    CardUpgradeModifier armor = changed.Permanent.Upgrades.Single(upgrade => upgrade.AssetKey == "Boilerplate_ArmorUpgrade");
                    int temporaryArmor = changed.Temporary.Upgrades.SelectMany(upgrade => upgrade.Statuses)
                        .Where(status => status.Id == "armor").Sum(status => status.Stacks);
                    Require(points == 2 && armor.Statuses.Single().Stacks == 2 * points &&
                        armor.MaskMetadata!.Statuses.Single().FromPermanentUpgrade &&
                        (newTarget.Status("armor")?.Stacks ?? 0) - (oldTarget.Status("armor")?.Stacks ?? 0) == 2 * points + temporaryArmor,
                        "Two native Forge Points must permanently add four armor, then apply it with the temporary forge armor on equip.");
                }
                else Require(changed.Permanent.Upgrades.Count == card.Permanent.Upgrades.Count &&
                    (newTarget.Status("armor")?.Stacks ?? 0) == (oldTarget.Status("armor")?.Stacks ?? 0),
                    "Zero-point Boilerplate must equip without firing its growth callback.");
            }
            if (rule.AssetKey == "Inhale")
            {
                RoomCombatState oldRoom = before.Spawn.Train.Rooms[action.RoomIndex], newRoom = after.Spawn.Train.Rooms[action.RoomIndex];
                int oldHorde = oldRoom.Units.Where(unit => unit.Team == CombatTeam.Player).Sum(unit => unit.Status("horde")?.Stacks ?? 0),
                    newHorde = newRoom.Units.Where(unit => unit.Team == CombatTeam.Player).Sum(unit => unit.Status("horde")?.Stacks ?? 0);
                CombatUnit victim = oldRoom.Units.Single(unit => unit.Id == action.TargetUnitId);
                bool friendly = victim.Team == CombatTeam.Player;
                int retainedHorde = oldHorde - (friendly ? victim.Status("horde")?.Stacks ?? 0 : 0);
                Require(card.MaskDescriptor!.KillTriggers!.Single().Effects.SequenceEqual(new[] { "CardEffectSpawnMonsterOrAddStatusEffect" }) &&
                    newRoom.Units.All(unit => unit.Id != action.TargetUnitId) && newHorde - retainedHorde == 2 &&
                    newRoom.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Status("horde")?.Stacks >= 2),
                    "Inhale must kill its native target and positively grant two Horde through its separate card kill callback.");
                Require(newRoom.Units.Count(unit => unit.Team == CombatTeam.Player) - oldRoom.Units.Count(unit => unit.Team == CombatTeam.Player) ==
                    (retainedHorde == 0 ? 1 : 0) - (friendly ? 1 : 0) && (retainedHorde != 0 || newRoom.Units.Any(unit =>
                        unit.Status("horde")?.Stacks == 2 && unit.Status("cardless")?.Stacks == 1)),
                    "Inhale must spawn one cardless unit only when no existing Horde target is available.");
            }
            if (rule.AssetKey is "SubsumingBlade" or "FanningTheFlame")
            {
                CardLifecycleTrigger trigger = card.MaskDescriptor!.KillTriggers!.Single();
                CardActionEffect growth = trigger.Actions.Single();
                CardInstanceState changed = after.Spawn.Train.Context!.FindCard(card.InstanceId)!;
                CardInstanceState control = before.Spawn.Train.Context!.CardInstances!.Single(item =>
                    item.DataId == card.DataId && item.InstanceId != card.InstanceId);
                Require(trigger.Effects.SequenceEqual(new[] { "CardTriggerEffectBuffSpellDamage" }) &&
                    growth.Type == "BuffSourceSpellDamage" && growth.Lifetime == "SingleRun" && growth.Value > 0 &&
                    changed.Permanent.Offsets.Damage - card.Permanent.Offsets.Damage == growth.Value &&
                    Json(changed.Temporary) == Json(card.Temporary) &&
                    Json(after.Spawn.Train.Context.FindCard(control.InstanceId)!.Permanent) == Json(control.Permanent) &&
                    after.Spawn.Train.Rooms.Sum(room => room.Units.Count(unit => unit.Team == CombatTeam.Enemy)) <
                        before.Spawn.Train.Rooms.Sum(room => room.Units.Count(unit => unit.Team == CombatTeam.Enemy)),
                    "Native spell kill must grow only its source's permanent damage, leaving a same-definition control unchanged.");
            }
            if (rule.AssetKey == "RevengeOfTheDamned")
            {
                int old = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex).Corruption!.Value,
                    changed = after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex).Corruption!.Value;
                Require(old > 0 && changed > old && card.MaskDescriptor!.KillTriggers!.Single().Actions.Single().Value == 2 &&
                    after.Spawn.Train.Rooms[action.RoomIndex].Units.All(unit => unit.Id != action.TargetUnitId),
                    "Revenge of the Damned must kill its target and positively add two Corruption through the native cap.");
            }
            if (rule.AssetKey == "DanteTheDependable")
            {
                CombatContext old = before.Spawn.Train.Context!, changed = after.Spawn.Train.Context!;
                CombatUnit dante = after.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.SpawnerCardId == ids[i]);
                CardToken candle = changed.Cards.Draw.Single();
                Require(old.Cards.Draw.Count == 0 && candle.InstanceId == old.NextCardId && changed.NextCardId == old.NextCardId + 1 &&
                    candle.DataId == "913694e9-bace-4480-8691-24f699ae583e" && changed.Statistics!.DeckCards!.Contains(candle.InstanceId) &&
                    !old.Statistics!.DeckCards!.Contains(candle.InstanceId) && dante.Status("multistrike")?.Stacks == 1 &&
                    dante.Triggers.Single(trigger => trigger.Kind == "OnSpawn").Effects.Single(effect => effect.Type == "CardEffectAddRunCard").Generation?.Permanent == true &&
                    !old.BattleRng.Equals(changed.BattleRng),
                    "Dante must add one permanent Candle to Draw before computing positive Multistrike from its new Blight count.");
            }
            if (rule.AssetKey == "IntentOnDeath")
            {
                CombatUnit oldTarget = before.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == action.TargetUnitId),
                    newTarget = after.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == action.TargetUnitId);
                Require(oldTarget.Health > 0 && newTarget.Health == oldTarget.Health &&
                    newTarget.Triggers.Any(trigger => trigger.Kind == "OnDeath" && trigger.HasTriggered) &&
                    Json(newTarget.DeathState) == Json(oldTarget.DeathState) &&
                    (oldTarget.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Type == "CardEffectPurgeUnitOnDeath")
                        ? after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Any(token => token.InstanceId == oldTarget.SpawnerCardId) &&
                            after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.All(token => token.InstanceId != oldTarget.SpawnerCardId)
                        : after.Spawn.Train.Rooms[action.RoomIndex].Units.Count(unit => unit.Team == CombatTeam.Enemy) <
                            before.Spawn.Train.Rooms[action.RoomIndex].Units.Count(unit => unit.Team == CombatTeam.Enemy)),
                    "Intent on Death must positively fire a living unit's death payload without changing its death lifecycle flags.");
            }
            if (rule.AssetKey == "Cannibalize")
            {
                CombatUnit victim = before.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == action.TargetUnitId);
                Require(after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != victim.Id) &&
                    after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == victim.SpawnerCardId) &&
                    after.Spawn.Train.Context.NextCardId - before.Spawn.Train.Context!.NextCardId == 3,
                    "Cannibalize must sacrifice its real friendly target into Exhausted and generate three native Morsels.");
            }
            if (rule.AssetKey == "ScrapForParts")
            {
                CombatUnit victim = before.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == action.TargetUnitId);
                CardToken drawn = before.Spawn.Train.Context!.Cards.Draw.Single();
                CardInstanceState upgraded = after.Spawn.Train.Context!.FindCard(drawn.InstanceId)!;
                Require((victim.EquipmentCards?.Count == 1 || victim.AssetKey == "MonsterBalmabello" && originals.Contains("75ce6859-dd29-4c88-a67a-5159a753f2db")) &&
                    after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != victim.Id) &&
                    after.Spawn.Train.Context.Cards.Hand.Any(token => token.InstanceId == drawn.InstanceId) && after.Spawn.Train.Context.Cards.Draw.Count == 0 &&
                    upgraded.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "ScrapForParts_Upgrade" && upgrade.Stats.Damage == 40 && upgrade.Stats.Cost == -99) &&
                    after.Spawn.Train.Context.Cards.NextDrawUpgrades?.Count == 0,
                    "Scrap for Parts must sacrifice its authored host, positively draw the waiting unit, apply forty attack/free cost, and clear its upgrade queue.");
                if (originals.Contains("fea8a847-88a1-4d77-b34d-83dcb3199d79"))
                    Require(after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Any(token => token.InstanceId == victim.SpawnerCardId) &&
                        after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.All(token => token.InstanceId != victim.SpawnerCardId) &&
                        after.Spawn.Train.Context.Statistics!.Value(victim.SpawnerCardId, "TimesExhausted") ==
                            before.Spawn.Train.Context.Statistics!.Value(victim.SpawnerCardId, "TimesExhausted"),
                        "Void Arms must purge its dying host's card before standby exhaustion and preserve the zero exhaustion count.");
            }
            if (rule.AssetKey == "ScrapForParts" && originals.Contains("75ce6859-dd29-4c88-a67a-5159a753f2db"))
            {
                CombatUnit victim = before.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Id == action.TargetUnitId);
                Require(victim.AssetKey == "MonsterBalmabello" && victim.Status("buff")?.Stacks == 12 &&
                    after.Spawn.Train.Rooms[action.RoomIndex].Units.All(unit => unit.Id != victim.Id) &&
                    before.Spawn.Train.Rooms[action.RoomIndex].Units.Where(unit => unit.Team == CombatTeam.Player && unit.Id != victim.Id)
                        .All(unit => after.Spawn.Train.Rooms[action.RoomIndex].Units.Single(changed => changed.Id == unit.Id).Status("buff")?.Stacks ==
                            (unit.Status("buff")?.Stacks ?? 0) + 12) &&
                    before.Spawn.Train.Rooms.Where(room => room.RoomIndex != action.RoomIndex).SelectMany(room => room.Units)
                        .Where(unit => unit.Team == CombatTeam.Player).All(unit => Json(unit) == Json(after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(changed => changed.Id == unit.Id))),
                    "Balmabello must positively copy all twelve Rage stacks to surviving floor allies on death and preserve other-floor controls.");
            }
            if (rule.AssetKey == "SacrificialRessurection")
            {
                CardToken[] victims = before.Spawn.Train.Context!.Cards.Hand.Where(token => token.InstanceId != ids[i]).ToArray();
                CardToken generated = after.Spawn.Train.Context!.Cards.Hand.Single();
                Require(victims.Length == 2 && victims.All(token => token.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678" &&
                        after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(exhausted => exhausted.InstanceId == token.InstanceId)) &&
                    generated.DataId == "3b74c955-fd58-4b60-81a7-e2d6a0aaa2d4" &&
                    after.Spawn.Train.Context.NextCardId - before.Spawn.Train.Context.NextCardId == 1 &&
                    after.Spawn.Train.Context.FindCard(generated.InstanceId)!.Temporary.Upgrades.Single().Stats.Damage == 20,
                    "Sacrificial Resurrection must consume both independent hand controls and generate one Draff with twenty temporary attack.");
                foreach (RoomCombatState room in before.Spawn.Train.Rooms)
                foreach (CombatUnit observer in room.Units.Where(unit => unit.Triggers.Any(trigger => trigger.Kind == "CardExhausted")))
                {
                    CombatUnit changed = after.Spawn.Train.Rooms[room.RoomIndex].Units.Single(unit => unit.Id == observer.Id);
                    if (room.RoomIndex != action.RoomIndex)
                    {
                        Require(Json(observer) == Json(changed), "Card exhaustion must not dispatch to a unit outside the selected room.");
                        continue;
                    }
                    if (observer.AssetKey == "MonsterFirstOfKin")
                        Require(observer.Attack == 45 && changed.Attack == 75 && changed.Health == observer.Health &&
                            changed.Triggers.Single(trigger => trigger.Kind == "CardExhausted").HasTriggered,
                            "First of Kin must gain fifteen attack for each consumed hand card without changing HP.");
                    if (observer.AssetKey == "MonsterMorelMistress")
                        Require(room.Units.Where(unit => unit.Team == CombatTeam.Enemy).All(unit =>
                            after.Spawn.Train.Rooms[room.RoomIndex].Units.Single(enemy => enemy.Id == unit.Id).Status("decay")?.Stacks ==
                                (unit.Status("decay")?.Stacks ?? 0) + 16) &&
                            changed.Triggers.Single(trigger => trigger.Kind == "CardExhausted").HasTriggered,
                            "Morel Mistress must apply eight Decay to each selected-room enemy for each consumed hand card.");
                }
            }
            if (rule.AssetKey == "GainXEnergy" && originals.Contains("ec5dbc45-0053-4dc3-84c3-528efcba6b26"))
            {
                RoomCombatState room = before.Spawn.Train.Rooms[action.RoomIndex];
                Require(room.Units.Any(unit => unit.AssetKey == "MonsterMorelMistress") &&
                    room.Units.Where(unit => unit.Team == CombatTeam.Enemy).All(unit =>
                        after.Spawn.Train.Rooms[action.RoomIndex].Units.Single(enemy => enemy.Id == unit.Id).Status("decay")?.Stacks == 8) &&
                    after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]),
                    "Natural Consume must dispatch one Morel Mistress callback after the original spell is played.");
            }
            if (rule.AssetKey == "HallowedHalls")
            {
                CombatUnit victim = before.Spawn.Train.Rooms[action.RoomIndex].Units.Single(unit => unit.Team == CombatTeam.Player);
                CombatUnit[] restoredUnits = after.Spawn.Train.Rooms[action.RoomIndex].Units.Where(unit => unit.Team == CombatTeam.Player).ToArray();
                CombatUnit? restored = restoredUnits.SingleOrDefault(unit => unit.SpawnerCardId == victim.SpawnerCardId);
                CardPileState standby = after.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Standby");
                int oldExhausted = before.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count;
                CardPileState exhausted = after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted");
                Require(oldExhausted is 0 or 6 && restoredUnits.Length == (oldExhausted == 0 ? 1 : 6) && exhausted.Cards.Count == (oldExhausted == 0 ? 0 : 1) &&
                    restoredUnits.All(unit => unit.Id != victim.Id && unit.Attack == 1005 && unit.Health == 10005 && unit.Status("ephemeral")?.Stacks == 1 &&
                        standby.UnitConditions!.Single(binding => binding.CardId == unit.SpawnerCardId).HostUnitId == unit.Id &&
                        exhausted.Cards.All(token => token.InstanceId != unit.SpawnerCardId)) &&
                    (oldExhausted != 0 || restored != null) &&
                    (oldExhausted == 0 || restoredUnits.Sum(unit => unit.Size) == 6 &&
                        !after.Spawn.Train.Context.BattleRng.Equals(before.Spawn.Train.Context.BattleRng)) &&
                    !BattleActionModel.PlayCard(before, new PlayCardAction(ids[i], before.PlayRules!.Rooms.Single(room => room.IsPyre).RoomIndex)).Supported,
                    "Hallowed Halls must shuffle candidates, restore fresh upgraded bodies with replaced Standby bindings, allow one capacity overflow, and reject the Pyre room.");
            }
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
        if (root.TryGetProperty("UnitSelfAttackQueries", out var attackQueries))
        {
            var positive = new HashSet<string>();
            int queries = 0;
            foreach (var record in attackQueries.EnumerateArray())
            {
                RoomCombatState room = record.GetProperty("Room").Deserialize<RoomCombatState>()!;
                foreach (var query in record.GetProperty("Attacks").EnumerateArray())
                {
                    CombatUnit unit = room.Units.Single(actor => actor.Id == query.GetProperty("UnitId").GetInt32());
                    Require(RelicResourceModel.AttackDamage(unit, room.Context) == query.GetProperty("Attack").GetInt32(),
                        "Native contextual attack getter differs: " + unit.AssetKey);
                    if (query.GetProperty("Attack").GetInt32() > unit.Attack)
                        foreach (UnitSelfAttackRule modifier in unit.Modifiers!.SelfAttackRules!) positive.Add(modifier.Type);
                    queries++;
                }
            }
            var expected = actions.SelectMany(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms)
                .SelectMany(room => room.Units).SelectMany(unit => unit.Modifiers?.SelfAttackRules ?? Array.Empty<UnitSelfAttackRule>())
                .Select(rule => rule.Type).Distinct().ToArray();
            Require(expected.All(positive.Contains), "Each contextual attack rule needs a positive independent native getter witness.");
            if (expected.Length > 0) Console.WriteLine($"UNIT-SELF-ATTACK PASS: {queries} exact native queries across {expected.Length} positive scaling rules.");
        }
        if (originals.Contains("e90fc0e0-b383-48a9-bb10-d8aaa1518531"))
        {
            var heals = root.GetProperty("TriggeredTrainHeals").EnumerateArray().ToArray();
            var tests = root.GetProperty("TriggeredTrainHealTests").EnumerateArray().ToArray();
            int stewardCardId = ids[4 + System.Array.IndexOf(originals, "e90fc0e0-b383-48a9-bb10-d8aaa1518531")];
            Require(heals.Any(heal => !heal.GetProperty("Preview").GetBoolean() && heal.GetProperty("After").GetInt32() > heal.GetProperty("Before").GetInt32()) &&
                tests.Any(test => test.GetProperty("Preview").GetBoolean() && !test.GetProperty("Result").GetBoolean()) &&
                tests.All(test => test.GetProperty("Result").GetBoolean() == (test.GetProperty("PyreTarget").GetBoolean() &&
                    (!test.GetProperty("Preview").GetBoolean() || test.GetProperty("SelectedPyreRoom").ValueKind == FixtureKind.Null ||
                        test.GetProperty("SelectedPyreRoom").GetBoolean()))) && heals.All(heal =>
                    heal.GetProperty("SourceCardId").GetInt32() == stewardCardId && heal.GetProperty("Amount").GetInt32() == 2 &&
                    heal.GetProperty("After").GetInt32() == (heal.GetProperty("Preview").GetBoolean() || heal.GetProperty("Disabled").GetBoolean()
                        ? heal.GetProperty("Before").GetInt32() : Math.Min(heal.GetProperty("Maximum").GetInt32(), heal.GetProperty("Before").GetInt32() + 2))),
                "Pyre Steward must heal the distant Pyre by two on native kills; native tests reject non-Pyre-room preview callbacks.");
        }
        if (originals.Contains("f3406a6d-69f0-46e8-8db9-d829db017716"))
        {
            var record = root.GetProperty("OriginalEquipmentGrowthEncounterComplete").EnumerateArray().Single();
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("CaptureError").ValueKind == FixtureKind.Null,
                "Bone Dog requires a completed native encounter callback.");
            TrainCombatState before = record.GetProperty("Before").Deserialize<TrainCombatState>()!,
                after = record.GetProperty("After").Deserialize<TrainCombatState>()!;
            int[] order = record.GetProperty("ActiveMonsterOrder").Deserialize<int[]>()!;
            string parent = Json(before);
            TrainCombatResult replay = EncounterCompleteModel.Apply(before, order);
            Require(replay.Supported, "Bone Dog encounter callback unsupported: " + replay.UnsupportedReason);
            CombatUnit oldPyre = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre),
                newPyre = after.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
            Require(order.Any(id => before.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == id &&
                    unit.SpawnerCardId == ids[4 + Array.IndexOf(originals, "f3406a6d-69f0-46e8-8db9-d829db017716")])) &&
                after.Context!.Resources!.BasePyreMaxHealth - before.Context!.Resources!.BasePyreMaxHealth == 10 &&
                newPyre.MaxHealth == oldPyre.MaxHealth && newPyre.Health - oldPyre.Health == 10,
                "Bone Dog must raise raw Pyre maximum and wounded Pyre health by ten; the combat character retains its initialized maximum.");
            Require(Json(replay.State) == Json(after) && Json(before) == parent,
                "Bone Dog full encounter-complete snapshot differs or mutated its parent: " + ModelJson.Difference(Json(replay.State), Json(after)));
        }
        foreach (string egg in originals.Where(id => id is "27b0fdda-5dc4-4280-a510-fe51e550e3fb" or "07448181-5b6f-404a-88ff-cb4d8ac8a3b1"))
        {
            string parent = egg == "27b0fdda-5dc4-4280-a510-fe51e550e3fb" ? "MonsterBogChrysalis" : "MonsterBogdeepCocoon";
            string child = parent == "MonsterBogChrysalis" ? "MonsterBogFly" : "MonsterBogWurmling";
            var birth = root.GetProperty("TriggeredSummons").EnumerateArray().Single(record =>
                record.GetProperty("Kind").GetString() == "OnHatched" && record.GetProperty("ActorBefore").GetProperty("AssetKey").GetString() == parent);
            CombatUnit actor = birth.GetProperty("ActorBefore").Deserialize<CombatUnit>()!;
            RoomCombatState before = birth.GetProperty("Before").Deserialize<RoomCombatState>()!,
                after = birth.GetProperty("After").Deserialize<RoomCombatState>()!;
            CombatUnit[] children = after.Units.Where(unit => before.Units.All(old => old.Id != unit.Id)).ToArray();
            Require(birth.GetProperty("Completed").GetBoolean() && birth.GetProperty("Error").ValueKind == FixtureKind.Null &&
                after.Units.All(unit => unit.Id != actor.Id) && children.Length == (child == "MonsterBogFly" ? 2 : 1) &&
                children.All(unit => unit.AssetKey == child && unit.Status("cardless")?.Stacks == 1) &&
                after.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == actor.SpawnerCardId) &&
                root.GetProperty("Turns").EnumerateArray().SelectMany(turn => turn.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms)
                    .SelectMany(room => room.Units).Any(unit => unit.Id == actor.Id && unit.Health > 0 && unit.Status("hatch")?.Stacks > 0 &&
                        unit.SacrificeCardId == unit.SpawnerCardId && unit.DeathState?.IsSacrifice == false),
                "Hatching needs positive original-parent exhaustion, exact cardless children and the preview's shared sacrifice-card reference.");
        }
        RecordedBattleChecks.Native(root);
        if (originals.Contains("3b603b7f-300c-4022-9fc2-275448446e58"))
        {
            var firstTurn = root.GetProperty("Turns").EnumerateArray().First();
            BattleTurnState before = firstTurn.GetProperty("Before").Deserialize<BattleTurnState>()!,
                after = firstTurn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CardInstanceState[] controls = before.Spawn.Train.Context!.Cards.Hand.Where(token => !ids.Contains(token.InstanceId))
                .Select(token => before.Spawn.Train.Context.FindCard(token.InstanceId)!)
                .Where(card => card.MaskDescriptor!.BaseTraits.Any(trait => trait.RuntimeType == "CardTraitEphemeral")).ToArray();
            Require(controls.Length == 2 && controls.All(card =>
                CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitFreeze") &&
                after.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Count(token => token.InstanceId == card.InstanceId) == 1 &&
                after.Spawn.Train.Context!.Cards.Hand.All(token => token.InstanceId != card.InstanceId) &&
                after.Spawn.Train.Context.Statistics!.Value(card.InstanceId, "TimesPlayed", "ThisBattle") == 0),
                "Unplayed Ephemeral must override Freeze and purge each control exactly once during natural hand discard.");
        }
        UnitRoomStatusChecks.Native(root, originals);
        ConditionalUnitUpgradeChecks.Native(root, originals);
        TriggeredRecursionChecks.Native(root, originals);
        RoomAttachmentTriggerChecks.Native(root);
        RoomStatusStackChecks.Native(root);
        RoomConsumedSpellDrawChecks.Native(root, originals);
        RoomSpawnCopyChecks.Native(root, originals);
        RoomSpellUpgradeChecks.Native(root, originals);
        if (originals.Contains("7018bb02-2ec2-4b4f-95d0-7bafce95d97d")) CrucibleStewardChecks.Native(root);
        Console.WriteLine($"ORIGINAL-ISOLATED-SPELLS PASS: {originals.Length} unchanged originals, independent control identities and full immutable chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
