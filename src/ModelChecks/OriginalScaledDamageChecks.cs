using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalScaledDamageChecks
{
    private static readonly int[] Indices = [9, 10, 11, 12, 13, 15, 17];
    private static readonly string[] Originals = ["97d18bbe-6fa9-45a4-90a9-5a1d0ea24ca3", "f6434819-3b62-44e4-a82c-e344dc4c1aa0",
        "e8e593f4-7e2d-4791-a7d4-8a6155f1a50f", "f2bed1d3-6b52-43d5-8b97-bfe5c90dce53", "acc7bdd1-8f5b-4738-8c4a-4542792e7388", "97d18bbe-6fa9-45a4-90a9-5a1d0ea24ca3",
        "f50ecee3-8bde-4100-a315-a362ff23a561"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 6 && source.Spawn.Turn < 1) return null;
        int target = index == 10 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[3]).Id : 0;
        var action = new PlayCardAction(ids[index], index is 1 or 2 ? index : 0,
            playerPosition: index < 3 ? 0 : index < 6 ? index - 2 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalScaledDamagePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original scaled damage witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalScaledDamageIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 22 && actions.Length == 22, "Missing original scaling spells or authored positive/zero controls.");
        int consumed = 0;
        foreach (int i in Indices)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!; var changed = after.Spawn.Train.Context!;
            var card = context.FindCard(ids[i])!; string original = Originals[Array.IndexOf(Indices, i)];
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == original);
            Require(card.DataId == original && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == rule.Cost,
                "Original scaling spell identity, cost or projection differs.");
            CombatUnit[] a = before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray(),
                b = after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
            Require(a.Length == 2 && a.Select(unit => unit.Id).SequenceEqual(b.Select(unit => unit.Id)) && b.All(unit => unit.Health > 0), "Scaling controls require two surviving enemies.");
            int expected = i switch { 9 => 0, 10 => 255, 11 => 120, 12 => 48, 13 => 90, 15 => 12, 17 => 20, _ => throw new InvalidOperationException() };
            Require(a.Sum(Effective) - b.Sum(Effective) == expected, "Exact resource/HP/attack/death/deck-type scaled damage differs at action " + i);
            if (i == 10)
            {
                CombatUnit support = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[3]);
                Require(support.Health == 80 && support.Status("armor")?.Stacks == 5 && support.Status("horde")?.Stacks == 1 &&
                    support.Subtypes.Contains("SubtypesData_FunGuy") && rule.Effects![0].DamagePerHpIncludeArmor,
                    "Deadly Plunge must use three times 80 HP plus five armor while retaining a positive FunGuy Horde target.");
            }
            if (i == 12) Require(context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 3, "Ancient Resonance requires positive three-Corruption scaling.");
            if (i == 13)
            {
                int registeredSpells = context.CardRegistry!.Count(state => state.MaskDescriptor?.Definition.CardType == "Spell" && ids.Contains(state.InstanceId));
                Require(card.MaskDescriptor!.SourceTraits!.Single().QueryCardType == "Spell" && rule.Effects!.Count == 3 && registeredSpells == 16,
                    "Ancient Synergy requires captured Spell query metadata and sixteen created spells; the resolving source leaves the native deck query. Actual=" + registeredSpells);
            }
            if (i == 15) Require(before.Spawn.Train.Rooms[0].Units.All(unit => unit.SpawnerCardId != ids[4] && unit.SpawnerCardId != ids[5]),
                "Positive death-count damage must follow both authored allied deaths.");
            Require(Json(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player)) ==
                Json(after.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player)) &&
                Json(before.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)) == Json(after.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)),
                "Original scaled spell changed a friendly or neighboring floor.");
            bool consume = CardExhaustModel.HasConsume(card); if (consume) consumed++;
            Require(consume ? changed.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]) :
                changed.Cards.Discard.Any(token => token.InstanceId == ids[i]), "Original scaled spell Consume/discard route differs.");
            Require(context.BattleRng.Equals(changed.BattleRng), "Fixed scaled spells must not consume Battle RNG.");
        }
        BattleTurnState mementoBefore = actions[16].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState mementoAfter = actions[16].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext mementoContext = mementoBefore.Spawn.Train.Context!;
        CombatContext mementoChanged = mementoAfter.Spawn.Train.Context!;
        CardInstanceState memento = mementoContext.FindCard(ids[16])!;
        CardPlayRule mementoRule = mementoBefore.PlayRules!.Cards.Single(rule => rule.DataId == memento.DataId);
        ScalingDamageTrait mementoScaling = memento.DamageScalingTraits!.Single();
        CardSourceTrait mementoSource = memento.MaskDescriptor!.SourceTraits!.Single();
        CombatUnit[] mementoEnemies = mementoBefore.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        CombatUnit frontBefore = mementoEnemies.First();
        CombatUnit frontAfter = mementoAfter.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == frontBefore.Id);
        DamageScalingResult mementoExpected = DamageScalingModel.Apply(mementoContext, ids[16], ids[16], mementoRule.Effects!.Single().Value);
        Require(memento.DataId == "c04c359c-9991-42bc-9040-bff7d3441248" && mementoRule.AssetKey == "MementoMori" &&
            mementoRule.Cost == 3 && mementoRule.CostType == "Default" && mementoRule.Destination == "Discard" &&
            mementoRule.Effects.Single().Type == "Damage" && mementoRule.Effects[0].Target == "FrontInRoom" &&
            mementoRule.Effects[0].Value == 0 && mementoRule.Effects[0].AllowEnemy && !mementoRule.Effects[0].AllowPlayer &&
            mementoRule.ExternalInteractions.Count == 0 && mementoRule.UpgradeInteractions!.Count == 0 &&
            mementoSource.Type == "CardTraitScalingAddDamage" && mementoSource.Int == 10 && mementoSource.Int2 == 0 && mementoSource.Int3 == 0 &&
            !mementoSource.Bool && mementoSource.Str == "" && mementoSource.Float == 1 && mementoSource.Scaling &&
            mementoSource.TrackedValue == "AnyMonsterDeath" && mementoSource.EntryDuration == "ThisBattle" &&
            mementoScaling.Query.Type == "AnyMonsterDeath" && mementoScaling.Query.Duration == "ThisBattle" &&
            mementoScaling.DamagePerStat == 10 && mementoScaling.Multiplier == 1 && !mementoScaling.AddToDamage &&
            mementoContext.Statistics!.MonstersDeadThisBattle == 2 && mementoContext.Statistics.Value(ids[16], "AnyMonsterDeath", "ThisBattle") == 2 &&
            mementoExpected.Supported && mementoExpected.Damage == 20 && frontBefore.Health - frontAfter.Health == 20 &&
            mementoBefore.Energy - mementoAfter.Energy == 3 && mementoChanged.Cards.Discard.Any(card => card.InstanceId == ids[16]) &&
            mementoContext.BattleRng.Equals(mementoChanged.BattleRng) &&
            Json(mementoBefore.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != frontBefore.Id)) ==
                Json(mementoAfter.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != frontBefore.Id)) &&
            Json(mementoBefore.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)) == Json(mementoAfter.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)),
            "Original Memento Mori AnyMonsterDeath scaling, target damage, payment or discard differs.");
        BattleTurnState knowledgeBefore = actions[17].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState knowledgeAfter = actions[17].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext knowledgeContext = knowledgeBefore.Spawn.Train.Context!;
        CardInstanceState knowledge = knowledgeContext.FindCard(ids[17])!;
        CardPlayRule knowledgeRule = knowledgeBefore.PlayRules!.Cards.Single(rule => rule.DataId == knowledge.DataId);
        CardSourceTrait knowledgeSource = knowledge.MaskDescriptor!.SourceTraits!.Single();
        ScalingDamageTrait knowledgeScaling = knowledge.DamageScalingTraits!.Single();
        string exhaustedSpellDataId = knowledgeContext.FindCard(ids[10])!.DataId;
        string friendlyMonsterDataId = knowledgeContext.FindCard(ids[0])!.DataId;
        CombatUnit knowledgeTarget = knowledgeBefore.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy);
        CombatUnit knowledgeTargetAfter = knowledgeAfter.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == knowledgeTarget.Id);
        DamageScalingResult knowledgeExpected = DamageScalingModel.Apply(knowledgeContext, ids[17], ids[17], knowledgeRule.Effects!.Single().Value);
        Require(knowledge.DataId == "f50ecee3-8bde-4100-a315-a362ff23a561" && knowledgeRule.AssetKey == "PowerOfKnowledge" &&
            knowledgeRule.Cost == 2 && knowledgeRule.CostType == "Default" && knowledgeRule.Destination == "Discard" &&
            knowledgeRule.Effects.Single().Type == "Damage" && knowledgeRule.Effects[0].Target == "DropTargetCharacter" &&
            knowledgeRule.Effects[0].Value == 0 && knowledgeRule.Effects[0].AllowEnemy && knowledgeRule.Effects[0].AllowPlayer &&
            knowledgeRule.ExternalInteractions.Count == 0 && knowledgeRule.UpgradeInteractions!.Count == 0,
            "Original Power of Knowledge definition, target or cost differs.");
        Require(knowledgeSource.Type == "CardTraitScalingAddDamage" && knowledgeSource.Int == 20 && knowledgeSource.Int2 == 0 && knowledgeSource.Int3 == 0 &&
            !knowledgeSource.Bool && knowledgeSource.Str == "" && knowledgeSource.Float == 1 && knowledgeSource.Scaling &&
            knowledgeSource.TrackedValue == "TypeInExhaustPile" && knowledgeSource.EntryDuration == "ThisBattle" &&
            knowledgeScaling.Query.Type == "TypeInExhaustPile" && knowledgeScaling.Query.Duration == "ThisBattle" &&
            knowledgeScaling.Query.TypeMatchDataIds!.Contains(exhaustedSpellDataId) &&
            !knowledgeScaling.Query.TypeMatchDataIds.Contains(friendlyMonsterDataId) && knowledgeScaling.DamagePerStat == 20 &&
            knowledgeScaling.Multiplier == 1 && !knowledgeScaling.AddToDamage,
            "Original Power of Knowledge source trait or captured Spell-only scaling query differs.");
        Require(
            knowledgeContext.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[10]) &&
            StatisticQueryModel.Evaluate(knowledgeContext, knowledgeScaling.Query, ids[17]).Value == 1 &&
            knowledgeExpected.Supported && knowledgeExpected.Damage == 20,
            "Original Power of Knowledge TypeInExhaustPile query did not count exactly one exhausted Spell.");
        Require(knowledgeTarget.Health - knowledgeTargetAfter.Health == 20 && knowledgeBefore.Energy - knowledgeAfter.Energy == 2 &&
            knowledgeAfter.Spawn.Train.Context!.Cards.Discard.Any(card => card.InstanceId == ids[17]) &&
            knowledgeBefore.Spawn.Train.Context!.BattleRng.Equals(knowledgeAfter.Spawn.Train.Context!.BattleRng),
            "Original Power of Knowledge target damage, payment, discard or RNG differs.");
        BattleTurnState poxBefore = actions[18].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState poxAfter = actions[18].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext poxContext = poxBefore.Spawn.Train.Context!;
        CombatContext poxChanged = poxAfter.Spawn.Train.Context!;
        CardInstanceState pox = poxContext.FindCard(ids[18])!;
        CardPlayRule poxRule = poxBefore.PlayRules!.Cards.Single(rule => rule.DataId == pox.DataId);
        CardActionEffect poxDamage = poxRule.Effects!.Single(effect => effect.Type == "Damage");
        CardActionEffect poxDecay = poxRule.Effects.Single(effect => effect.Type == "AddStatus");
        Require(pox.DataId == "eb866e99-d16b-427a-a762-69d6b0f320f8" && poxRule.AssetKey == "Pox" &&
            poxRule.Cost == 2 && poxRule.CostType == "Default" && poxRule.Effects.Count == 2 &&
            poxDamage.Target == "Room" && poxDamage.Value == 20 && poxDamage.AllowEnemy && poxDamage.AllowPlayer &&
            poxDecay.Target == "Room" && poxDecay.AllowEnemy && poxDecay.AllowPlayer && poxDecay.Statuses.Single().Id == "decay" &&
            poxDecay.Statuses[0].Stacks == 10 && poxDecay.Statuses[0].ParamInt == 3 && poxDecay.Statuses[0].RemoveStackAtEnd,
            "Original Pox definition, room targets, base damage or Decay metadata differs.");
        CombatUnit[] poxUnits = poxBefore.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team is CombatTeam.Player or CombatTeam.Enemy).ToArray();
        CombatUnit[] poxUnitsAfter = poxAfter.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team is CombatTeam.Player or CombatTeam.Enemy).ToArray();
        Require(poxUnits.Select(unit => unit.Id).SequenceEqual(poxUnitsAfter.Select(unit => unit.Id)) &&
            poxUnits.All(unit => Effective(unit) - Effective(poxUnitsAfter.Single(changed => changed.Id == unit.Id)) == 20 &&
                (poxUnitsAfter.Single(changed => changed.Id == unit.Id).Status("decay")?.Stacks ?? 0) - (unit.Status("decay")?.Stacks ?? 0) == 10),
            "Original Pox must deal 20 and add 10 Decay to every living unit on both teams.");
        bool poxConsumes = CardExhaustModel.HasConsume(pox);
        Require(poxBefore.Energy - poxAfter.Energy == 2 &&
            (poxConsumes ? poxChanged.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[18]) :
                poxChanged.Cards.Discard.Any(card => card.InstanceId == ids[18])) && poxBefore.Spawn.Train.Context!.BattleRng.Equals(poxChanged.BattleRng) &&
            Json(poxBefore.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)) == Json(poxAfter.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)),
            "Original Pox payment, Consume/discard route, RNG or neighboring floor differs.");
        BattleTurnState rotBefore = actions[19].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState rotAfter = actions[19].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext rotContext = rotBefore.Spawn.Train.Context!;
        CardInstanceState rot = rotContext.FindCard(ids[19])!;
        CardPlayRule rotRule = rotBefore.PlayRules!.Cards.Single(rule => rule.DataId == rot.DataId);
        CardActionEffect rotDamage = rotRule.Effects!.Single(effect => effect.Type == "Damage");
        CardActionEffect rotDecay = rotRule.Effects.Single(effect => effect.Type == "AddStatus");
        CombatUnit rotTarget = rotBefore.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy);
        CombatUnit rotTargetAfter = rotAfter.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == rotTarget.Id);
        Require(rot.DataId == "d27baa97-0afa-4bb4-a8e0-2af43a507e67" && rotRule.AssetKey == "Rot" &&
            rotRule.Cost == 3 && rotRule.CostType == "Default" && rotRule.Destination == "Discard" && rotRule.Effects.Count == 2 &&
            rotDamage.Target == "DropTargetCharacter" && rotDamage.Value == 15 && rotDamage.AllowEnemy && rotDamage.AllowPlayer &&
            rotDecay.Target == "LastTargetedCharacters" && rotDecay.AllowEnemy && rotDecay.AllowPlayer &&
            rotDecay.Statuses.Single().Id == "decay" && rotDecay.Statuses[0].Stacks == 15 && rotDecay.Statuses[0].ParamInt == 3 &&
            rotDecay.Statuses[0].RemoveStackAtEnd,
            "Original Rot definition, targeted damage or LastTargetedCharacters Decay metadata differs.");
        Require(Effective(rotTarget) - Effective(rotTargetAfter) == 15 &&
            (rotTargetAfter.Status("decay")?.Stacks ?? 0) - (rotTarget.Status("decay")?.Stacks ?? 0) == 15 &&
            Json(rotBefore.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != rotTarget.Id)) ==
                Json(rotAfter.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != rotTarget.Id)) &&
            rotBefore.Energy - rotAfter.Energy == 3 && rotAfter.Spawn.Train.Context!.Cards.Discard.Any(card => card.InstanceId == ids[19]) &&
            rotContext.BattleRng.Equals(rotAfter.Spawn.Train.Context!.BattleRng) &&
            Json(rotBefore.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)) == Json(rotAfter.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)),
            "Original Rot must deal 15 and add 15 Decay only to its selected character, then discard without RNG or neighbor changes.");
        BattleTurnState reactionBefore = actions[20].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState reactionAfter = actions[20].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext reactionContext = reactionBefore.Spawn.Train.Context!;
        CardInstanceState reaction = reactionContext.FindCard(ids[20])!;
        CardPlayRule reactionRule = reactionBefore.PlayRules!.Cards.Single(rule => rule.DataId == reaction.DataId);
        CardActionEffect unstable = reactionRule.Effects![0];
        CardActionEffect reactionDamage = reactionRule.Effects[1];
        CombatUnit reactionTarget = reactionBefore.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy);
        CombatUnit reactionTargetAfter = reactionAfter.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == reactionTarget.Id);
        Require(reaction.DataId == "d7f93607-5e85-45fe-b764-d716c0776727" && reactionRule.AssetKey == "ExplosiveReaction" &&
            reactionRule.Cost == 2 && reactionRule.CostType == "Default" && reactionRule.Destination == "Discard" && reactionRule.Effects.Count == 2 &&
            unstable.Type == "AddStatus" && unstable.Target == "DropTargetCharacter" && unstable.AllowEnemy && unstable.AllowPlayer &&
            unstable.Statuses.Single().Id == "unstable" && unstable.Statuses[0].Stacks == 25 && unstable.Statuses[0].ParamInt == 1 &&
            reactionDamage.Type == "Damage" && reactionDamage.Target == "LastTargetedCharacters" && reactionDamage.Value == 5 &&
            reactionDamage.AllowEnemy && reactionDamage.AllowPlayer,
            "Original Explosive Reaction definition or ordered status/damage target metadata differs.");
        Require(Effective(reactionTarget) - Effective(reactionTargetAfter) == 5 &&
            (reactionTargetAfter.Status("unstable")?.Stacks ?? 0) - (reactionTarget.Status("unstable")?.Stacks ?? 0) == 25 &&
            reactionTargetAfter.Health > 0 &&
            Json(reactionBefore.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != reactionTarget.Id)) ==
                Json(reactionAfter.Spawn.Train.Rooms[0].Units.Where(unit => unit.Id != reactionTarget.Id)) &&
            reactionBefore.Energy - reactionAfter.Energy == 2 && reactionAfter.Spawn.Train.Context!.Cards.Discard.Any(card => card.InstanceId == ids[20]) &&
            reactionContext.BattleRng.Equals(reactionAfter.Spawn.Train.Context!.BattleRng) &&
            Json(reactionBefore.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)) == Json(reactionAfter.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)),
            "Original Explosive Reaction must add Unstable, then deal 5 to that same target only, and discard without RNG or neighbor changes.");
        BattleTurnState mushroomBefore = actions[21].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState mushroomAfter = actions[21].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext mushroomContext = mushroomBefore.Spawn.Train.Context!;
        CombatContext mushroomChanged = mushroomAfter.Spawn.Train.Context!;
        CardInstanceState mushroom = mushroomContext.FindCard(ids[21])!;
        CardPlayRule mushroomRule = mushroomBefore.PlayRules!.Cards.Single(rule => rule.DataId == mushroom.DataId);
        CardActionEffect removeHorde = mushroomRule.Effects![0];
        CardActionEffect mushroomDecay = mushroomRule.Effects[1];
        CardActionEffect mushroomDamage = mushroomRule.Effects[2];
        CombatUnit funGuy = mushroomBefore.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[3]);
        CombatUnit? funGuyAfter = mushroomAfter.Spawn.Train.Rooms[0].Units.SingleOrDefault(unit => unit.Id == funGuy.Id);
        Require(mushroom.DataId == "55251be0-b711-4398-bd3f-d26a0976fcfc" && mushroomRule.AssetKey == "MushroomCloud" &&
            mushroomRule.Cost == 1 && mushroomRule.CostType == "Default" && mushroomRule.Effects.Count == 3 &&
            removeHorde.Type == "RemoveStatus" && removeHorde.Target == "Room" && !removeHorde.AllowEnemy && removeHorde.AllowPlayer &&
            removeHorde.Filters!.Subtype == "SubtypesData_FunGuy" && removeHorde.Statuses.Single().Id == "horde" && removeHorde.Statuses[0].Stacks == 1 &&
            mushroomDecay.Type == "AddStatus" && mushroomDecay.Target == "Room" && mushroomDecay.AllowEnemy && !mushroomDecay.AllowPlayer &&
            mushroomDecay.Statuses.Single().Id == "decay" && mushroomDecay.Statuses[0].Stacks == 10 &&
            mushroomDamage.Type == "Damage" && mushroomDamage.Target == "Room" && mushroomDamage.Value == 10 && mushroomDamage.AllowEnemy &&
            !mushroomDamage.AllowPlayer && funGuy.Subtypes.Contains("SubtypesData_FunGuy") && funGuy.Status("horde")?.Stacks == 1,
            "Original Mushroom Cloud definition, FunGuy Horde filter, enemy Decay or room damage metadata differs.");
        CombatUnit[] mushroomEnemies = mushroomBefore.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        CombatUnit[] mushroomEnemiesAfter = mushroomAfter.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        bool mushroomEnemyEffectsExact = mushroomEnemiesAfter.All(after =>
        {
            CombatUnit before = mushroomEnemies.Single(unit => unit.Id == after.Id);
            return Effective(before) - Effective(after) == 10 &&
                (after.Status("decay")?.Stacks ?? 0) - (before.Status("decay")?.Stacks ?? 0) == 10;
        }) && mushroomEnemies.All(before =>
        {
            CombatUnit? after = mushroomEnemiesAfter.SingleOrDefault(unit => unit.Id == before.Id);
            return after == null ? Effective(before) <= 10 : true;
        });
        Require(funGuyAfter == null &&
            mushroomEnemyEffectsExact &&
            Json(mushroomBefore.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player && unit.Id != funGuy.Id)) ==
                Json(mushroomAfter.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player && unit.Id != funGuy.Id)),
            "Original Mushroom Cloud must remove one FunGuy Horde and deal 10/add 10 Decay to all enemies only.");
        Require(CardExhaustModel.HasConsume(mushroom) && mushroomBefore.Energy - mushroomAfter.Energy == 1 &&
            mushroomChanged.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[21]) &&
            mushroomContext.BattleRng.Equals(mushroomChanged.BattleRng) &&
            Json(mushroomBefore.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)) == Json(mushroomAfter.Spawn.Train.Rooms.Skip(1).Select(room => room.Units)),
            "Original Mushroom Cloud payment, Consume route, RNG or neighboring floor differs.");
        Require(consumed == 1, "Deadly Plunge must be the only Consume spell among the scaling-damage witness cards.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-SCALED-DAMAGE PASS: eleven unchanged originals/twelve plays, exact scaled damage, Pox/Rot/Explosive Reaction/Mushroom Cloud effect chains with positive FunGuy Horde removal, three Corruption, fifteen TypeInDeck Spell members while Ancient Synergy resolves, zero/two allied deaths, Memento Mori +20 from AnyMonsterDeath, Power of Knowledge +20 from one exhausted Spell and 22 immutable full chains.");
    }
    private static int Effective(CombatUnit unit) => unit.Health + (unit.Status("armor")?.Stacks ?? 0);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
