using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalUnitAbilityChecks
{
    private const string PyrebloodedAbilityId = "7eb9fd9a-0d9b-4509-80b9-991de5f217c0";
    private const string PyrebloodedId = "e962f77b-d5bc-441b-be79-8cb3207d8a42";
    private const string PyrecaskId = "28875721-10df-4427-b9ef-4d237e89608e";
    private const string LavaFlowId = "479ceb1b-fc8c-420f-8fa0-17683cee4511";

    internal static void Native(FixtureValue root)
    {
        if (!root.TryGetProperty("OriginalUnitAbilityId", out var value) || value.ValueKind != FixtureKind.String) return;
        string id = value.GetString()!;
        int count = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (action.ActivatorUnitId == 0) continue;
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == id);
            CardInstanceState card = before.Spawn.Train.Context!.CardRegistry.Single(card => card.InstanceId == action.CardInstanceId);
            Require(card.DataId == id && rule.Ability?.Kind == "Unit" && rule.ExternalInteractions.Count == 0, "Missing unchanged original unit skill.");
            if (id == PyrebloodedAbilityId)
            {
                Pyreblooded(root, before, after, action, rule, card);
                count++;
                continue;
            }
            if (id == "798142fe-fdf3-4f4f-8af7-dbc5dbc333c6")
            {
                CombatContext old = before.Spawn.Train.Context!, next = after.Spawn.Train.Context!;
                ElixirRecipe recipe = rule.Effects.Single().SpikeRecipe!;
                ElixirDraftOptions expected = SpikeCraftModel.Draft(recipe, old.BattleRng);
                CardToken token = next.Cards.Hand.Single(token => old.Cards.Hand.All(prior => prior.InstanceId != token.InstanceId));
                CardInstanceState spike = next.CardRegistry.Single(value => value.InstanceId == token.InstanceId);
                FixtureValue draft = root.GetProperty("OriginalUnitAbilityDrafts").EnumerateArray().Single();
                Require(draft.GetProperty("Cards").EnumerateArray().Select(value => value.GetString()).SequenceEqual(new[] { token.DataId, token.DataId }) &&
                    draft.GetProperty("Upgrades").EnumerateArray().Select(value => value.GetString()).SequenceEqual(expected.Offers.Select(offer => offer.Upgrade?.DataId ?? "")) &&
                    expected.Offers.Select(offer => offer.Upgrade!.DataId).Distinct().Count() == 2 &&
                    draft.GetProperty("Choice").GetInt32() == action.MixSelections.Single().ChoiceIndex &&
                    spike.Permanent.Upgrades.Any(upgrade => upgrade.DataId == expected.Offers[action.MixSelections.Single().ChoiceIndex].Upgrade!.DataId) &&
                    next.BattleRng.Equals(expected.Rng) && next.Cards.Rng.Equals(old.Cards.Rng),
                    "Factory must offer the original spike with two distinct upgrades, copy the selected upgrade and advance only Battle RNG.");
                count++; continue;
            }
            if (id == "f6fa19db-cad1-4104-91a7-48098daabe4f")
            {
                CombatContext old = before.Spawn.Train.Context!, next = after.Spawn.Train.Context!;
                CardActionEffect effect = rule.Effects.Single();
                var candidates = CardLastTurnCopyModel.EligibleCards(old, effect, out string? error);
                Require(error == null && before.Spawn.Turn == 2 && candidates.Count == 1 &&
                    old.CardRegistry.Single(value => value.InstanceId == candidates[0]).DataId == "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b",
                    "Lunar Echo must select the original Torch from previous-turn hand plays, excluding the Junk energy control.");
                CardToken token = next.Cards.Hand.Single(token => old.Cards.Hand.All(prior => prior.InstanceId != token.InstanceId));
                CardInstanceState copied = next.CardRegistry.Single(value => value.InstanceId == token.InstanceId);
                Require(copied.DataId == "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b" && effect.Upgrade != null &&
                    copied.Temporary.Upgrades.Any(upgrade => upgrade.DataId == effect.Upgrade.DataId) &&
                    next.Statistics!.HandCardsPlayedThisTurn!.SequenceEqual(old.Statistics!.HandCardsPlayedThisTurn!) &&
                    next.Statistics.CardsPlayedThisTurn.Contains(action.CardInstanceId) &&
                    !next.Statistics.HandCardsPlayedThisTurn.Contains(action.CardInstanceId) &&
                    next.Cards.Rng.Equals(old.Cards.Rng.Range(0, 1).State) && next.BattleRng.Equals(old.BattleRng),
                    "Lunar Echo must create an upgraded temporary copy using CardDraw RNG, without recording ability activation as a hand play.");
                BattleTurnState first = root.GetProperty("Actions").EnumerateArray().First().GetProperty("Before").Deserialize<BattleTurnState>()!;
                Require(CardLastTurnCopyModel.EligibleCards(first.Spawn.Train.Context!, effect, out error).Count == 0 && error == null,
                    "Empty prior-turn history must provide no copy candidate.");
                count++; continue;
            }
            if (id == "f7126e4a-ef22-4621-b1f7-31a53e2e5870")
            {
                CombatUnit actor = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == action.ActivatorUnitId);
                CombatUnit victim = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == action.TargetUnitId);
                CombatUnit changed = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == actor.Id);
                Require(victim.Id != actor.Id && victim.Team == CombatTeam.Player &&
                    after.Spawn.Train.Rooms[0].Units.All(unit => unit.Id != victim.Id) &&
                    after.Spawn.Train.Context!.Gold == before.Spawn.Train.Context!.Gold + 5 && changed.Attack > actor.Attack,
                    "Chomp must remove a distinct friendly target, fire its activator's positive OnFeed control and then upgrade itself.");
                bool food = victim.Triggers.Any(trigger => trigger.Kind == "OnEaten");
                string pile = food ? "Eaten" : "Exhausted";
                Require(after.Spawn.Train.Context!.OtherPiles!.Any(value => value.Name == pile && value.Cards.Any(token => token.InstanceId == victim.SpawnerCardId)),
                    "Chomp must preserve the distinct food/sacrifice destination: " + pile);
                count++; continue;
            }
            if (id == "4dbfc527-50b2-4068-aa36-9770d4444a57")
            {
                CombatContext old = before.Spawn.Train.Context!, next = after.Spawn.Train.Context!;
                int newlyFrozen = 0, alreadyFrozen = 0;
                foreach (CardToken token in old.Cards.Hand)
                {
                    CardInstanceState original = old.CardRegistry.Single(card => card.InstanceId == token.InstanceId);
                    CardInstanceState frozen = next.CardRegistry.Single(card => card.InstanceId == token.InstanceId);
                    Require(CardBranchMaskModel.Resolve(frozen).Traits.Contains("CardTraitFreeze"), "Time Freeze must freeze every hand card.");
                    Require(frozen.MaskDescriptor!.TemporaryTraits.Count(trait => trait.RuntimeType == "CardTraitFreeze") ==
                        original.MaskDescriptor!.TemporaryTraits.Count(trait => trait.RuntimeType == "CardTraitFreeze") + 1,
                        "Whole-hand freeze must append a new temporary trait even to an already frozen card.");
                    if (CardBranchMaskModel.Resolve(original).Traits.Contains("CardTraitFreeze")) alreadyFrozen++;
                    else newlyFrozen++;
                }
                Require(newlyFrozen >= 2 && alreadyFrozen > 0 &&
                    after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.ActivatorUnitId).Ability?.HasAbility != true,
                    "Time Freeze requires positive ordinary cards, an already frozen control and removal of its own skill.");
                count++; continue;
            }
            if (id is "4252e8ed-4f8f-474f-80be-622eb6dae7fa" or "2479c736-4463-4346-8464-d517dae23f40" or "be65a126-831e-43c1-b987-698c37edead2")
            {
                UnitStatusAbilityChecks.Native(before, after, action, id);
                count++; continue;
            }
            if (id == "8d54f68b-5660-4507-84c3-a0cf092bfbf1")
            {
                CardGenerationRule generation = rule.Effects.Single().Generation!;
                CardToken generated = after.Spawn.Train.Context!.Cards.Hand.Single(token => before.Spawn.Train.Context.Cards.Hand.All(old => old.InstanceId != token.InstanceId));
                CardInstanceState created = after.Spawn.Train.Context.CardRegistry.Single(card => card.InstanceId == generated.InstanceId);
                Require(generation.OptionalUpgradeFilters?.Count > 0 && generation.Pool.Any(choice => choice.DataId == created.DataId) &&
                    created.Temporary.Upgrades.Single().AssetKey == "FetchCostReduction" &&
                    created.Temporary.Upgrades.Single().Stats.Cost == -1 && CardBranchMaskModel.Resolve(created).Traits.Contains("CardTraitEphemeral"),
                    "Fetch must generate an original equipment pool member with its filtered temporary discount and Ephemeral trait.");
                count++; continue;
            }
            int sourceRoom = before.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == action.ActivatorUnitId)).RoomIndex;
            CardSourceTrait trait = card.MaskDescriptor!.SourceTraits!.Single(trait => trait.Type == CardCorruptRestrictedModel.Trait);
            int consumedRoom = trait.Bool ? sourceRoom : action.RoomIndex;
            foreach (RoomCapacityState old in before.Spawn.Train.Context.RoomCapacities!)
                Require(after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == old.RoomIndex).Corruption ==
                    old.Corruption - (old.RoomIndex == consumedRoom ? trait.Int : 0), "Corrupt Restricted consumed the wrong floor or amount.");
            if (id == "c59d1814-0e0e-414c-b82f-a71cead2951f")
                Require(sourceRoom == 0 && action.RoomIndex == 1 && trait.Bool && trait.Int == 1 &&
                    before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 0 &&
                    after.Spawn.Train.Rooms[1].Units.Any(unit => unit.Id == action.ActivatorUnitId), "Bog Fly must consume source corruption and move to a zero-corruption destination.");
            else if (id == "127b5582-1e59-4ce0-afdb-9ccc2d59f913")
                Require(trait.Int == 4 && !trait.Bool && before.Spawn.Train.Rooms[0].Units.Any(unit => unit.Id == action.TargetUnitId && unit.Health > 0) &&
                    after.Spawn.Train.Rooms[0].Units.All(unit => unit.Id != action.TargetUnitId), "Glugsider must kill a living non-boss target after consuming four corruption.");
            else if (id == "1b18c4ef-9df6-466d-b61e-c338800a14a6")
                Require(trait.Int == 3 && !trait.Bool && after.Spawn.Train.Context!.Cards.Hand.Count == before.Spawn.Train.Context.Cards.Hand.Count + 2 &&
                    after.Spawn.Train.Context.Cards.Draw.Count == before.Spawn.Train.Context.Cards.Draw.Count - 2,
                    "Extract Draw must draw two waiting cards after consuming three corruption.");
            else throw new InvalidOperationException("Missing original skill checker: " + id);
            int nextSourceRoom = after.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == action.ActivatorUnitId)).RoomIndex;
            Require(CardCorruptRestrictedModel.Test(after.Spawn.Train.Context!, card, action.RoomIndex, out bool allowed, nextSourceRoom) == null && !allowed,
                "The depleted skill must fail its modeled corruption gate.");
            var queries = root.GetProperty("OriginalUnitAbilityQueries").EnumerateArray().ToArray();
            Require(queries.Any(query => !query.GetProperty("Allowed").GetBoolean()) && queries.All(query =>
                query.GetProperty("Allowed").GetBoolean() == (query.GetProperty(trait.Bool ? "SourceCorruption" : "SelectedCorruption").GetInt32() >= trait.Int)),
                "Native positive and negative eligibility queries must use the correct floor.");
            count++;
        }
        Require(count == 1, "Expected one positive original unit skill activation.");
        if (id is "c59d1814-0e0e-414c-b82f-a71cead2951f" or "127b5582-1e59-4ce0-afdb-9ccc2d59f913" or "1b18c4ef-9df6-466d-b61e-c338800a14a6")
            Require(root.GetProperty("OriginalUnitAbilityQueries").EnumerateArray().Any(query => query.GetProperty("Allowed").GetBoolean()), "Missing native corruption eligibility query.");
        Console.WriteLine("ORIGINAL-UNIT-ABILITY PASS: " + id + "; unchanged native definition and positive skill effect.");
    }

    private static void Pyreblooded(FixtureValue root, BattleTurnState before, BattleTurnState after,
        PlayCardAction action, CardPlayRule rule, CardInstanceState card)
    {
        int[] ids = root.GetProperty("OriginalSupportSpellsIds").Deserialize<int[]>()!;
        FixtureValue[] rows = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 7 && rows.Length == 8 && root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Pyreblooded requires the pinned original owner and ordered status controls.");
        BattleTurnState zero = rows[5].GetProperty("Before").Deserialize<BattleTurnState>()!;
        PlayCardAction pyrecask = rows[5].GetProperty("Action").Deserialize<PlayCardAction>()!;
        PlayCardAction lavaFlow = rows[6].GetProperty("Action").Deserialize<PlayCardAction>()!;
        RoomCombatState room = before.Spawn.Train.Rooms[0], nextRoom = after.Spawn.Train.Rooms[0];
        CombatContext old = before.Spawn.Train.Context!, changed = after.Spawn.Train.Context!;
        CombatUnit actor = room.Units.Single(unit => unit.Id == action.ActivatorUnitId);
        CombatUnit next = nextRoom.Units.Single(unit => unit.Id == actor.Id);
        CombatUnit anchor = room.Units.Single(unit => unit.SpawnerCardId == ids[0]);
        ScalingUnitUpgradeTrait scaling = card.UnitUpgradeScalingTraits!.Single();
        CardUpgradeModifier upgrade = rule.Effects.Single().Upgrade!;
        int Pyregel(CombatUnit unit) => unit.Status("pyregel")?.Stacks ?? 0;
        int heroStacks = room.Units.Where(unit => unit.Health > 0 && unit.Team == CombatTeam.Player).Sum(Pyregel);
        CombatUnit[] enemies = room.Units.Where(unit => unit.Health > 0 && unit.Team == CombatTeam.Enemy).ToArray();
        int bonus = heroStacks + enemies.Sum(Pyregel);

        Require(pyrecask is { RoomIndex: 0 } && pyrecask.TargetUnitId == anchor.Id &&
            old.FindCard(pyrecask.CardInstanceId)?.DataId == PyrecaskId &&
            lavaFlow is { RoomIndex: 0, TargetUnitId: 0 } && old.FindCard(lavaFlow.CardInstanceId)?.DataId == LavaFlowId &&
            Pyregel(anchor) == 10 && heroStacks == 10 && enemies.Length > 0 && enemies.All(unit => Pyregel(unit) == 4) &&
            actor.AssetKey == "MonsterPyreblooded" && actor.SpawnerCardId == ids[4] &&
            old.FindCard(actor.SpawnerCardId)?.DataId == PyrebloodedId && actor.Ability?.DataId == PyrebloodedAbilityId &&
            action.RoomIndex == 0 && action.TargetUnitId == actor.Id && old.QueryFrame?.SelectedRoomIndex == 0 &&
            rule is { Cost: 0, CostType: "Default", Ability.CanTargetOtherFloors: false,
                Effects: [{ Type: "UnitUpgrade", Target: "DropTargetCharacter", Lifetime: "TemporaryUntilEndOfBattle" }] } &&
            scaling is { Stat: "Damage", AmountPerStat: 1, Restriction: 0,
                Query: { Type: "StatusEffectCountInTargetRoom", TargetTeam: "255", StatusIds: ["pyregel"] } } &&
            upgrade is { AssetKey: "PyrebloodedAbility_Upgrade", Stats.Damage: 0, Stats.Health: 0 },
            "Expected the original no-cost Pyreblooded skill with positive Pyregel on both teams in its selected room.");

        CardInstanceState used = changed.FindCard(action.CardInstanceId)!;
        Require(after.Energy == before.Energy && used.LastPlayedCost == 0 && used.PlayCount == card.PlayCount &&
            changed.Statistics!.CardsPlayedThisTurn.Contains(action.CardInstanceId) && next.Status("cooldown")?.Stacks == 3 &&
            next.BaseAttack == actor.BaseAttack + bonus && next.Health == actor.Health && next.MaxHealth == actor.MaxHealth &&
            next.Modifiers!.Upgrades.Count == actor.Modifiers!.Upgrades.Count + 1 &&
            next.Modifiers.Upgrades.Single(item => item.DataId == upgrade.DataId).Stats.Damage == bonus,
            "The native ability must record payment, start its cooldown and upgrade the authored owner by the complete selected-room sum.");

        RoomCombatState zeroRoom = zero.Spawn.Train.Rooms[0];
        BattleActionResult zeroCast = BattleActionModel.PlayCard(zero, action);
        Require(zeroRoom.Units.All(unit => Pyregel(unit) == 0) && zeroCast.Supported &&
            zeroCast.State!.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == actor.Id).BaseAttack == actor.BaseAttack &&
            !StatisticQueryModel.Evaluate(old, scaling.Query, action.CardInstanceId).Supported &&
            !StatisticQueryModel.Evaluate(old, scaling.Query, action.CardInstanceId, selectedRoom: before.Spawn.Train.Rooms[1]).Supported,
            "Pyreblooded must support zero stacks and reject missing or unrelated selected-room state.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
