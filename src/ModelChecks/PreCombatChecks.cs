using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class PreCombatChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(85);
        var instances = new[] { CardInstanceState.Empty(1, "unit"), new CardInstanceState(2, "unit", CardModifiers.Empty(), CardModifiers.Empty(),
            0, 0, 0, [], unitUpgradeScalingTraits: [new(new("TurnCount"), "Damage", 1), new(new("AnyExhausted"), "Health", 1)]) };
        var context = new CombatContext(new([new(3, "junk")], [], [], rng, 0, []), rng, 0, 4, 10,
            statistics: BattleStatistics.Empty(deckCards: [1, 2, 3]).TrackCards([1, 2, 3]).WithEndTurnEnergy(7),
            cardInstances: instances.Append(CardInstanceState.Empty(3, "junk")).ToArray(),
            otherPiles: [new("Standby", [new(1, "unit"), new(2, "unit")], [0, 1], []), new("Exhausted", [])],
            queryFrame: new(energy: 3, runningCombat: true, turn: 3));
        CombatEffect Upgrade(string id, int damage = 1, int health = 1) => new("CardEffectAddTempCardUpgradeToUnits", 0, 0, "", 0, [], false,
            unitUpgrade: new("UnitUpgrade", "Self", 0, false, true, [], new(id, id, new(damage: damage, health: health), [], false,
                false, false, 0, 0, []), "TemporaryUntilUnitDeath"));
        CombatEffect Gold() => new("CardEffectRewardGold", 1, 0, "", 0, [], false);
        var generation = new CombatEffect("CardEffectAddBattleCard", 0, 0, "HandPile", 1, ["junk"], false,
            generation: new("HandPile", 1, [new("junk", CardModifiers.Empty(), null, [])]));
        CombatTrigger Trigger(CombatEffect effect, bool once = false, bool ignored = false, bool deployment = false) =>
            new("PreCombat", once, false, ignored, 1, [effect], deployment);
        CombatUnit Player(int id, CombatTrigger[] triggers, CombatStatus[]? statuses = null) => new(id, "unit", CombatTeam.Player,
            0, 20, 20, true, false, false, statuses ?? [], triggers, id - 1, 1, modifiers: new(0, 0, 0, 1, 1, true, false, []));
        var pyre = new CombatUnit(1, "pyre", CombatTeam.Player, 20, 80, 80, true, true, false, []);
        TrainCombatState Train(CombatUnit[] lower, CombatUnit[] upper, bool preview = false, bool deployment = false) => new([
            new(0, deployment, lower, [], context, preview), new(1, deployment, [], [], context, preview),
            new(2, deployment, upper, [], context, preview), new(3, deployment, [pyre], [], context, preview)], [], 5, context);
        var oldest = Player(2, [Trigger(new("CardEffectDespawnCharacter", 1, 1, "", 0, [], false))]);
        var younger = Player(3, [Trigger(Upgrade("scaled")), Trigger(generation), Trigger(Gold())], [new("dazed", 1)]);
        var root = Train([younger], [oldest]); string parent = JsonSerializer.Serialize(root);
        var result = TrainCombatModel.PreCombat(root, CombatTeam.Player);
        Require(result.Supported && result.RoomResults.Select(room => room.State!.RoomIndex).SequenceEqual([3, 2, 0]) &&
            result.State!.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != 2) &&
            result.State.Rooms[0].Units.Single().BaseAttack == 4 && result.State.Rooms[0].Units.Single().MaxHealth == 22 &&
            result.State.Context!.Gold == 5 && result.State.Context.Cards.Hand.Select(card => card.InstanceId).SequenceEqual([4, 3]) &&
            result.State.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Single().InstanceId == 1 &&
            result.State.Context.Statistics!.EnergyRemainingEndOfTurn == 7 && result.State.Context.QueryFrame!.Energy == 3,
            "Pre-combat creation order, new turn scaling, earlier exhaustion, daze or generation differs.");
        var combatOnly = TrainCombatModel.ResolveCombat(root);
        Require(combatOnly.Supported && combatOnly.State!.Context!.Gold == context.Gold &&
            combatOnly.State.Context.NextCardId == context.NextCardId &&
            combatOnly.State.Rooms.SelectMany(room => room.Units).Count() == root.Rooms.SelectMany(room => room.Units).Count(),
            "Room combat repeated the once-per-new-turn pre-combat phase.");
        var onceRoot = Train([Player(2, [Trigger(Gold(), once: true), Trigger(Upgrade("repeat"))])], []);
        var first = TrainCombatModel.PreCombat(onceRoot, CombatTeam.Player);
        var second = TrainCombatModel.PreCombat(first.State!, CombatTeam.Player);
        Require(second.State!.Context!.Gold == 5 && second.State.Rooms[0].Units.Single().BaseAttack == 2,
            "Pre-combat once/repeated counters differ.");
        var silenced = Train([Player(2, [Trigger(Gold()), Trigger(Upgrade("ignored"), ignored: true)], [new("silenced", 1)])], []);
        Require(TrainCombatModel.PreCombat(silenced, CombatTeam.Player).State!.Rooms[0].Units.Single().BaseAttack == 1 &&
            TrainCombatModel.PreCombat(silenced, CombatTeam.Player).State!.Context!.Gold == 0,
            "Pre-combat silence/ignored exception differs.");
        var deployed = Train([Player(2, [Trigger(Gold(), deployment: true)])], [], deployment: true);
        Require(TrainCombatModel.PreCombat(deployed, CombatTeam.Player).State!.Context!.Gold == 0 &&
            !TrainCombatModel.PreCombat(root, (CombatTeam)99).Supported &&
            !RoomCombatModel.ApplyPreCombat(root.Rooms[3], 99).Supported, "Pre-combat deployment/team/actor gates differ.");
        var pyreTrigger = new CombatUnit(1, "pyre", CombatTeam.Player, 20, 80, 80, true, true, false, [], [Trigger(Gold())]);
        Require(RoomCombatModel.ApplyPreCombat(new(3, false, [pyreTrigger], [], context), 1).State!.Context!.Gold == 5,
            "Pre-combat phase omitted the native active-list Pyre actor.");
        var enemy = new CombatUnit(4, "enemy", CombatTeam.Enemy, 0, 20, 20, false, false, false, [], [Trigger(Gold())]);
        var enemyRoot = Train([enemy], []);
        Require(TrainCombatModel.PreCombat(enemyRoot, CombatTeam.Player).State!.Context!.Gold == 0 &&
            TrainCombatModel.PreCombat(enemyRoot, CombatTeam.Enemy).State!.Context!.Gold == 5, "Pre-combat teams were combined.");
        var preview = Train([Player(2, [Trigger(Gold()), Trigger(generation), Trigger(Upgrade("preview"))])], [], preview: true);
        var previewResult = TrainCombatModel.PreCombat(preview, CombatTeam.Player);
        Require(previewResult.State!.Rooms[0].Units.Single().BaseAttack == 1 &&
            JsonSerializer.Serialize(previewResult.State.Context) == JsonSerializer.Serialize(context), "Pre-combat preview changed live context or lost its buff.");
        var lethalRoot = Train([Player(2, [Trigger(Upgrade("fatal", 0, -100))]), younger], []);
        var killed = TrainCombatModel.PreCombat(lethalRoot, CombatTeam.Player);
        Require(killed.State!.Rooms[0].Units.Count == 1 && killed.State.Context!.Statistics!.MonstersDeadThisTurn == 1 &&
            killed.State.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Single().InstanceId == 1,
            "Pre-combat nested death lost its spawner or later queued actor.");
        CombatEffect DeathCard(string dataId) => new("CardEffectAddBattleCard", 0, 0, "HandPile", 1, [dataId], false,
            generation: new("HandPile", 1, [new(dataId, CardModifiers.Empty(), null, [])]));
        CombatUnit Doomed(int id, string dataId) => Player(id, [Trigger(generation), Trigger(Gold()), Trigger(Upgrade("fatal", 0, -100)),
            new("OnDeath", false, false, true, 1, [Gold(), DeathCard(dataId)])]);
        var queuedRoot = Train([Doomed(3, "death-two"), Doomed(2, "death-one")], []);
        var queued = TrainCombatModel.PreCombat(queuedRoot, CombatTeam.Player);
        Require(queued.Supported && queued.State!.Context!.Cards.Hand.OrderBy(card => card.InstanceId).Select(card => card.DataId)
            .SequenceEqual(["junk", "junk", "junk", "death-one", "death-two"]) &&
            queued.RoomResults.SelectMany(room => room.Events).Where(item => item.Kind == "Gold").Select(item => item.Actor)
                .SequenceEqual([2, 3, 2, 3]), "Death effects ran inline before the remaining queued pre-combat actors.");
        var unknown = Train([Player(2, [new("PreCombat", false, false, false, 1, [new("Unknown", 0, 0, "", 0, [], false)], false)])], []);
        Require(!TrainCombatModel.PreCombat(unknown, CombatTeam.Player).Supported, "Unknown pre-combat effect produced a search child.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(TrainCombatModel.PreCombat(root, CombatTeam.Player)) ==
            JsonSerializer.Serialize(result) && JsonSerializer.Serialize(TrainCombatModel.PreCombat(queuedRoot, CombatTeam.Player)) ==
            JsonSerializer.Serialize(queued), "Parallel pre-combat phases differed."));
        Require(JsonSerializer.Serialize(root) == parent, "Pre-combat phase changed its parent.");
        Console.WriteLine("PRE-COMBAT-CHECKS PASS: creation order across floors, turn statistics/exhaustion, hand generation, daze/silence, once/repeat, deployment, teams/Pyre, queued death/routing/generation, preview and 32 parallel branches.");
    }

    private static string Comparable(TrainCombatState state) => JsonSerializer.Serialize(new
    { state.Rooms, Movement = state.Movement.OrderBy(rule => rule.UnitId), state.EnemySlotsPerRoom, state.Context });

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out FixtureValue scenario) || scenario.GetString() is not ("pre-combat" or "pre-combat-lethal")) return;
        bool lethal = scenario.GetString() == "pre-combat-lethal";
        int phases = 0, player = 0, enemy = 0, generated = 0, deaths = 0, dazed = 0, silent = 0;
        int callbacks = 0, turnSamples = 0, resetSamples = 0, deathOrder = 0, drawBoundaries = 0, initialEnemies = 0;
        TrainCombatState? preceding = null; int? precedingTurn = null; CombatTeam? precedingTeam = null;
        foreach (FixtureValue phase in fixture.GetProperty("PreCombats").EnumerateArray())
        {
            TrainCombatState before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState after = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatTeam team = (CombatTeam)phase.GetProperty("Team").GetInt32();
            int[] actors = phase.GetProperty("ActorIds").Deserialize<int[]>()!;
            var predicted = TrainCombatModel.PreCombat(before, team);
            Require(predicted.Supported && phase.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                ModelJson.Difference(Comparable(predicted.State!), Comparable(after)) == null, "Independent native pre-combat phase differs.");
            Require(actors.SequenceEqual(before.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == team)
                .OrderBy(unit => unit.Id).Select(unit => unit.Id)), "Native pre-combat actor order is not creation order.");
            int turn = before.Context!.QueryFrame!.Turn!.Value;
            Require(turn > 0 && before.Context.QueryFrame.Energy == 0 && after.Context!.QueryFrame!.Energy == 0 &&
                after.Context.Statistics!.EnergyRemainingEndOfTurn == before.Context.Statistics!.EnergyRemainingEndOfTurn,
                "Pre-combat ran before rollover or after replenishing energy.");
            if (team == CombatTeam.Enemy)
                Require(precedingTeam == CombatTeam.Player && precedingTurn == turn && Comparable(preceding!) == Comparable(before),
                    "Pre-combat team order or shared context propagation differs.");
            else Require(before.Context.Cards.Hand.Count == 0 && before.Context.Statistics!.Values.All(value =>
                value.Duration != "ThisTurn" || value.Type != "TimesDrawn"), "Pre-combat ran after the regular draw or before resetting draw statistics.");
            preceding = after; precedingTurn = turn; precedingTeam = team;
            phases++; player += team == CombatTeam.Player ? 1 : 0; enemy += team == CombatTeam.Enemy ? 1 : 0;
            generated += after.Context!.Cards.Hand.Count(card => card.InstanceId >= before.Context.NextCardId);
            deaths += after.Context.Statistics!.MonstersDeadThisTurn - before.Context.Statistics!.MonstersDeadThisTurn;
            if (turn == 1 && team == CombatTeam.Player)
                initialEnemies += before.Rooms.SelectMany(room => room.Units).Count(unit => unit.Team == CombatTeam.Enemy &&
                    unit.Triggers.Any(trigger => trigger.Kind == "PreCombat"));
            if (lethal && team == CombatTeam.Player)
            {
                CardInstanceState[] births = after.Context.CardInstances!.Where(card => card.InstanceId >= before.Context.NextCardId).ToArray();
                int[] marked = births.Where(card => card.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "PojuPreCombatDeathCard"))
                    .Select(card => card.InstanceId).ToArray();
                int[] ordinary = births.Where(card => card.Temporary.Upgrades.All(upgrade => upgrade.AssetKey != "PojuPreCombatDeathCard"))
                    .Select(card => card.InstanceId).ToArray();
                if (marked.Length > 0 && ordinary.Length > 0)
                {
                    Require(ordinary.Max() < marked.Min(), "Pre-combat death cards were allocated before remaining team generation.");
                    deathOrder++;
                }
            }
            dazed += before.Rooms.SelectMany(room => room.Units).Count(unit => unit.Statuses.Any(status => status.Id == "dazed") &&
                unit.Triggers.Any(trigger => trigger.Kind == "PreCombat" && trigger.Effects.Any(effect => effect.UnitUpgrade != null)));
            silent += before.Rooms.SelectMany(room => room.Units).Count(unit => unit.Statuses.Any(status => status.Id == "silenced") &&
                unit.Triggers.Any(trigger => trigger.Kind == "PreCombat" && trigger.IgnoreSilence));
            if (team != CombatTeam.Enemy) continue;
            FixtureValue draw = fixture.GetProperty("CardCycles").EnumerateArray().Single(item =>
                item.GetProperty("Kind").GetString() == "Draw" && item.GetProperty("Turn").GetInt32() == turn);
            CardCycleState drawBefore = draw.GetProperty("Before").Deserialize<CardCycleState>()!;
            CardCycleState drawAfter = draw.GetProperty("Actual").Deserialize<CardCycleState>()!;
            var drawn = CardCycleModel.DrawHand(drawBefore, draw.GetProperty("HandSize").GetInt32(), draw.GetProperty("MaxHandSize").GetInt32());
            Require(JsonSerializer.Serialize(drawBefore) == JsonSerializer.Serialize(after.Context.Cards) && drawn.Supported &&
                JsonSerializer.Serialize(drawn.State) == JsonSerializer.Serialize(drawAfter) &&
                after.Context.Cards.Hand.All(card => drawAfter.Hand.Any(item => item.InstanceId == card.InstanceId)),
                "Pre-combat hand generation did not precede/coexist with the regular draw.");
            drawBoundaries++;
        }
        foreach (FixtureValue sample in fixture.GetProperty("UnitUpgradeScaling").EnumerateArray())
        {
            CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext after = sample.GetProperty("After").Deserialize<CombatContext>()!;
            ScalingUnitUpgradeTrait trait = sample.GetProperty("Trait").Deserialize<ScalingUnitUpgradeTrait>()!;
            CardUpgradeModifier original = sample.GetProperty("BeforeUpgrade").Deserialize<CardUpgradeModifier>()!;
            CardUpgradeModifier actual = sample.GetProperty("AfterUpgrade").Deserialize<CardUpgradeModifier>()!;
            string kind = sample.GetProperty("TriggerKind").GetString()!;
            var predicted = UnitUpgradeScalingModel.ApplyTrait(before, trait, sample.GetProperty("OwnerCardId").GetInt32(), original, kind);
            Require(predicted.Supported && sample.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                sample.GetProperty("CaptureError").ValueKind == FixtureKind.Null && JsonSerializer.Serialize(predicted.Upgrade) == JsonSerializer.Serialize(actual) &&
                JsonSerializer.Serialize(predicted.Context) == JsonSerializer.Serialize(after), "Native pre-combat scaling callback differs.");
            if (kind != "PreCombat") continue;
            callbacks++;
            Require(before.QueryFrame!.Energy == 0 && before.QueryFrame.Turn > 0, "Pre-combat scaling used old turn or new energy.");
            if (trait.Query.Type == "TurnCount") turnSamples++;
            if (trait.Query.Type == "AnyCardDrawn")
            {
                Require(original.Stats.Health == actual.Stats.Health && before.Statistics!.Values.All(value =>
                    value.Duration != "ThisTurn" || value.Type != "TimesDrawn"), "Pre-combat draw scaling used the previous/new hand's draw counts.");
                resetSamples++;
            }
        }
        Require(phases > 0 && player == enemy && generated > 0 && dazed > 0 && silent > 0 && callbacks >= 4 &&
            turnSamples > 0 && resetSamples > 0 && drawBoundaries == enemy && initialEnemies > 0 && (!lethal || deaths > 0 && deathOrder > 0),
            "Native pre-combat fixture lacks required teams, timing, draw statistics, generated hand, gates or queued deaths.");
        Console.WriteLine($"NATIVE-PRE-COMBAT-CHECKS PASS: {phases} complete phases ({player} player/{enemy} enemy), {generated} generated hand cards, {callbacks} callbacks ({turnSamples} turn/{resetSamples} reset-draw), {drawBoundaries} draw boundaries, {initialEnemies} initial enemies, {deaths} nested deaths and {deathOrder} marked death-generation orders; complete states.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
