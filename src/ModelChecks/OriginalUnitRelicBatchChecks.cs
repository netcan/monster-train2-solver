using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalUnitRelicBatchChecks
{
    private const string Demon = "SubtypesData_Demon_fcebd195-f262-4d7a-ac9f-01ffa2007a12";
    private const string Snack = "SubtypesData_Snack";
    private static readonly (string Key, string Id, string? Upgrade, string? Subtype)[] Artifacts =
    [
        ("ImpDamageBuff", "94595f3a-4624-4408-977d-b166a98f23d2", "d4d2f3a0-a191-45f4-850a-98feb26f86b5", Demon),
        ("StewardSacrificeDamage", "f6a4e405-98b9-4eeb-bae1-50cb204d12ed", "33228cd3-3d43-4116-a8ff-2eb149e919ae", "SubtypesData_TrainSteward"),
        ("MorselsGetDamageShield", "d1daea7e-3346-47df-a756-c839b044e5f3", "cd9f3315-56f6-4400-bef7-a77c16ed8c83", Snack),
        ("ValorTransferOnAngelDeath", "8c493cd7-8048-4598-bdd5-6cc2801cd842", null, null)
    ];

    internal static void Native(FixtureValue root, bool required = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out var scenario) && scenario.GetString() == "original-unit-relic-batch";
        Require(eligible || !required, "Fixture is missing the original unit relic batch.");
        if (!eligible) return;
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("RelicCardModifiersEnabled").GetBoolean(), "Unit relic batch lacks native modifier capture.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Unit relic batch has nonzero " + field + ".");

        var rows = root.GetProperty("RelicCardModifiers").EnumerateArray().ToArray();
        var applications = new HashSet<string>();
        int resets = 0, rejects = 0, uniqueRejects = 0;
        foreach (var row in rows)
        {
            var before = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            var context = row.GetProperty("ContextBefore").Deserialize<CombatContext>()!;
            var afterContext = row.GetProperty("ContextAfter").Deserialize<CombatContext>()!;
            var relics = row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!;
            var actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
            var dispatches = row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>()!;
            bool reset = row.GetProperty("ResetTemporary").GetBoolean();
            CheckIdentities(relics, context);
            string parent = Serialize(before) + Serialize(context) + Serialize(relics);
            void Check()
            {
                var result = RelicCardModifierModel.Apply(before, relics, reset, context);
                Require(result.Supported, "Unit relic manager unsupported: " + result.UnsupportedReason);
                Equal(result.Card, actual, "unit relic complete card state");
                Equal(result.Dispatches, dispatches, "unit relic exact filter/unique dispatch");
                Equal(result.Context!.Relics, afterContext.Relics, "unit relic condition state");
                Equal(result.Context.Statistics, afterContext.Statistics, "unit relic statistics");
                Require(result.Dispatches.Where(effect => effect.Returned).Select(effect => (effect.RelicIndex, effect.EffectIndex)).SequenceEqual(
                    row.GetProperty("Notifications").EnumerateArray().Select(item =>
                        (item.GetProperty("RelicIndex").GetInt32(), item.GetProperty("EffectIndex").GetInt32()))), "Unit relic notification order differs.");
            }
            Check(); Parallel.For(0, 32, _ => Check());
            Require(Serialize(before) + Serialize(context) + Serialize(relics) == parent, "Unit relic modifier branches changed their parent.");
            if (reset && dispatches.Any(effect => effect.UpgradeAdded)) resets++;
            rejects += dispatches.Count(effect => !effect.Returned);
            uniqueRejects += dispatches.Count(effect => effect.Returned && !effect.UpgradeAdded);
            foreach (var effect in dispatches.Where(effect => effect.UpgradeAdded)) applications.Add(relics[effect.RelicIndex].AssetKey);
        }
        Require(rows.Length > 0 && resets > 0 && rejects > 0 && uniqueRejects > 0 &&
            Artifacts.Where(item => item.Upgrade != null).All(item => applications.Contains(item.Key)),
            "Some original unit relics lack positive application, reset, unique rejection or failed filtering.");

        int paidSnacks = 0, cardlessSnacks = 0;
        foreach (var row in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Unit relic birth capture is incomplete or differs.");
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("After").Deserialize<RoomCombatState>()!;
            int id = row.GetProperty("UnitId").GetInt32();
            string parent = Serialize(before);
            void Check()
            {
                var result = RelicSpawnStatusModel.CharacterAdded(before, id, row.GetProperty("FromCardId").GetInt32(),
                    row.GetProperty("OnlyCovenants").GetBoolean());
                Require(result.Supported, "Unit relic birth unsupported: " + result.UnsupportedReason);
                Equal(result.State, actual, "unit relic complete birth state");
            }
            Check(); Parallel.For(0, 32, _ => Check());
            Require(Serialize(before) == parent, "Unit relic birth branches changed their parent.");
            var prior = before.Units.Single(unit => unit.Id == id);
            var actor = actual.Units.Single(unit => unit.Id == id);
            if (actor.Team != CombatTeam.Player || !actor.Subtypes.Contains(Snack)) continue;
            if (actor.Status("cardless") == null)
            {
                Require(actor.Status("damage shield")?.Stacks == 1 && prior.Status("damage shield")?.Stacks == 1,
                    "Paid Morsel birth repeated the original cardless-only shield effect.");
                paidSnacks++;
            }
            else
            {
                Require(actor.Status("damage shield")?.Stacks == (prior.Status("damage shield")?.Stacks ?? 0) + 1,
                    "Cardless Morsel birth missed its original extra shield.");
                cardlessSnacks++;
            }
        }
        Require(paidSnacks > 0 && cardlessSnacks > 0, "Original Morsel relic lacks paid and cardless native births.");

        var gates = new HashSet<string>(); int queries = 0;
        foreach (var row in root.GetProperty("RelicShiftQueries").EnumerateArray())
        {
            var room = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var unit = room.Units.Single(unit => unit.Id == row.GetProperty("UnitId").GetInt32());
            bool excluded = row.GetProperty("NoCountModifiersAllowed").GetBoolean();
            var trigger = new CombatTrigger("OnShift", false, false, true, 1, [], noCountModifiersAllowed: excluded);
            int actual = row.GetProperty("ActualCount").GetInt32();
            int expected = TriggerCountModel.Query(room.Context, unit, trigger, out string? error);
            Require(error == null && expected == actual, "Original Shift relic native query differs: " + error);
            string gate = excluded ? "excluded" : unit.Team == CombatTeam.Player ? "player" : unit.EquipmentCards?.Count > 0 ? "enemy-gear" : "enemy-body";
            Require(actual == (gate is "player" or "enemy-gear" ? 2 : 1), "Original Shift admission/count differs.");
            if (excluded) Require(row.GetProperty("OriginalNoCountFlag").GetBoolean() == row.GetProperty("RestoredNoCountFlag").GetBoolean() &&
                row.GetProperty("NormalCountAfter").GetInt32() == TriggerCountModel.Query(room.Context, unit,
                    new("OnShift", false, false, true, 1, [], noCountModifiersAllowed: false), out _), "Excluded query changed the live native trigger.");
            gates.Add(gate); queries++;
        }
        Require(new[] { "player", "enemy-gear", "enemy-body", "excluded" }.All(gates.Contains), "Original Shift relic admission gates lack native witnesses.");

        int sacrifices = 0, damagingSacrifices = 0, playerShifts = 0, enemyShifts = 0, repeatedAttacks = 0, shieldBlocks = 0;
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var context = before.Spawn.Train.Context!;
            var after = actual.Spawn.Train.Context!;
            if (action.ActivatorUnitId > 0)
            {
                var actor = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.ActivatorUnitId);
                if (actor.Ability?.DataId != "5ea5b8fc-806f-4d25-ba1d-be02d8a774ad") continue;
                Require(actual.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != actor.Id),
                    "Original Steward ability did not sacrifice itself.");
                if (actor.Status("cardless") == null)
                    Require(actual.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == actor.SpawnerCardId),
                        "Paid Steward sacrifice did not exhaust its source.");
                else Require(after.CardInstances!.All(card => card.InstanceId != actor.SpawnerCardId) &&
                    actual.OtherPiles.All(pile => pile.Cards.All(card => card.InstanceId != actor.SpawnerCardId)) &&
                    after.CardRegistry!.Any(card => card.InstanceId == actor.SpawnerCardId), "Cardless Steward sacrifice did not retire its copied source.");
                Require(after.LiveStatistics!.CardsPlayedThisTurn.SequenceEqual(context.LiveStatistics!.CardsPlayedThisTurn.Append(action.CardInstanceId)),
                    "Original Steward ability lacked actual played history.");
                var enemies = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
                if (enemies.Any(enemy => actual.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != enemy.Id) ||
                    actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == enemy.Id).Health < enemy.Health)) damagingSacrifices++;
                sacrifices++; continue;
            }
            var card = context.FindCard(action.CardInstanceId)!;
            var rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            if (!rule.Effects.Any(effect => effect.Type == "Bump")) continue;
            var shifted = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId);
            var destination = actual.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == shifted.Id));
            // Other policy targets include the champion without our reward
            // trigger, and successful casts can be blocked at a train boundary.
            if (destination.RoomIndex == action.RoomIndex || !shifted.Triggers.Any(trigger => trigger.Kind == "OnShift" &&
                trigger.Effects.Any(effect => effect.Type == "CardEffectRewardGold" && effect.Value == 5))) continue;
            var bumpEffect = rule.Effects.Single(effect => effect.Type == "Bump");
            var bumped = BumpModel.Apply(before.Spawn.Train, [shifted.Id], bumpEffect.Value, before.PlayRules.Rooms, card.InstanceId);
            Require(bumped.Supported && bumped.Events.Count(effect => effect.Kind == "Gold" && effect.Actor == shifted.Id && effect.Amount == 5) == 2 &&
                after.Gold - context.Gold == bumped.Events.Where(effect => effect.Kind == "Gold").Sum(effect => effect.Amount),
                "Original Shift relic did not double the moved unit's reward and reproduce all same-team shift rewards.");
            if (shifted.Team == CombatTeam.Player) playerShifts++; else enemyShifts++;
        }
        foreach (var row in root.GetProperty("Stages").EnumerateArray())
        {
            if (row.GetProperty("Kind").GetString() != "Exchange") continue;
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var result = RoomCombatModel.Exchange(before);
            Require(result.Supported, "Unit relic combat unsupported: " + result.UnsupportedReason);
            Equal(result.State!.Units, actual.Units, "unit relic native combat units");
            Equal(result.State.Context, actual.Context, "unit relic native combat context");
            foreach (var actor in before.Units.Where(unit => unit.Team == CombatTeam.Player))
            {
                if (actor.Subtypes.Contains(Demon) && actor.Status("multistrike")?.Stacks == 1 &&
                    result.Events.Count(effect => effect.Kind == "Attack" && effect.Actor == actor.Id) >= 2) repeatedAttacks++;
                var survivor = actual.Units.SingleOrDefault(unit => unit.Id == actor.Id);
                if (!actor.Subtypes.Contains(Snack) || actor.Status("damage shield") == null || survivor == null) continue;
                if ((survivor.Status("damage shield")?.Stacks ?? 0) < actor.Status("damage shield")!.Stacks &&
                    survivor.Health == actor.Health && result.Events.Any(effect => effect.Kind == "Attack" && effect.Target == actor.Id && effect.Amount == 0)) shieldBlocks++;
            }
        }
        Require(sacrifices > 0 && damagingSacrifices > 0 && playerShifts > 0 && enemyShifts > 0 && repeatedAttacks > 0 && shieldBlocks > 0,
            $"Missing actual relic effects: sacrifice={sacrifices}/{damagingSacrifices} damaging, playerShift={playerShifts}, enemyShift={enemyShifts}, multistrike={repeatedAttacks}, shield={shieldBlocks}.");
        Console.WriteLine($"NATIVE-ORIGINAL-UNIT-RELICS-CHECKS PASS: four originals, {rows.Length} manager calls, {paidSnacks}/{cardlessSnacks} paid/cardless Morsel births, " +
            $"{queries} Shift queries/four gates, {sacrifices}/{damagingSacrifices} sacrifices/damaging, {playerShifts}/{enemyShifts} real shifts, {repeatedAttacks} multistrikes, {shieldBlocks} shield blocks and 32 branches.");
    }

    private static void CheckIdentities(IReadOnlyList<CombatRelicState> relics, CombatContext context)
    {
        foreach (var item in Artifacts)
        {
            var relic = relics.Single(candidate => candidate.AssetKey == item.Key && candidate.DataId == item.Id);
            if (item.Upgrade == null)
            {
                Require(relic.EffectTypes.SequenceEqual([RelicModel.ModifyTriggerCount]) && context.TriggerCounts!.Modifiers.Any(modifier =>
                    modifier.Kind == "OnShift" && modifier.Value == 1) && context.TriggerCounts.EnemyAllowedKinds.Contains("OnShift"), "Original Shift cache differs.");
                continue;
            }
            var modifier = relic.CardModifiers!.Single();
            var upgrade = modifier.Upgrade ?? throw new InvalidDataException("Missing original relic upgrade.");
            Require(relic.EffectTypes[0] == RelicModel.AddTempUpgrade && upgrade.DataId == item.Upgrade && upgrade.Unique &&
                modifier.Rule.Filters.Count == 1 && modifier.Rule.Filters[0].Subtypes.Required.SequenceEqual([item.Subtype!]) && modifier.Conditions.Count == 0,
                "Original unit relic payload/filter differs: " + item.Key);
            if (item.Key == "ImpDamageBuff") Require(upgrade.Statuses.Single().Id == "multistrike" && upgrade.Statuses[0].Stacks == 1,
                "Original Demon multistrike differs.");
            if (item.Key == "StewardSacrificeDamage") Require(modifier.ApplyToCardlessSpawns &&
                upgrade.AbilityUpgrade?.Definition?.DataId == "5ea5b8fc-806f-4d25-ba1d-be02d8a774ad", "Original Steward ability differs.");
            if (item.Key == "MorselsGetDamageShield") Require(upgrade.Statuses.Single().Id == "damage shield" && upgrade.Statuses[0].Stacks == 1 &&
                relic.EffectTypes.SequenceEqual([RelicModel.AddTempUpgrade, RelicModel.AddStatusOnSpawn]) && relic.SpawnStatuses!.Single().SourceKind == "NoCard" &&
                relic.SpawnStatuses![0].Statuses.Single().Id == "damage shield" && relic.SpawnStatuses[0].Statuses[0].Stacks == 1, "Original Morsel shield effects differ.");
        }
    }
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        var plays = BattleActionModel.EnumerateSupportedPlays(source);
        var rooms = source.Spawn.Train.Rooms;
        var context = source.Spawn.Train.Context!;
        if (source.Spawn.Turn >= 2)
            foreach (var action in UnitAbilityModel.EnumerateSupportedActivations(source))
                if (!rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.ActivatorUnitId).Subtypes.Contains(Demon)) return action;
        foreach (string subtype in new[] { Demon, Snack })
        foreach (var card in context.Cards.Hand.Where(card => source.PlayRules!.Cards.Any(rule => rule.DataId == card.DataId && rule.SpawnUnit?.Subtypes.Contains(subtype) == true)))
        {
            var action = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.RoomIndex == 0);
            if (action != null) return action;
        }
        foreach (var card in context.Cards.Hand.Where(card => source.PlayRules!.Cards.Any(rule => rule.DataId == card.DataId && rule.Equipment != null)))
        {
            var action = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.RoomIndex == 0 &&
                rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Enemy && unit.IsBoss != true));
            if (action != null) return action;
        }
        foreach (var card in context.Cards.Hand.Where(card => source.PlayRules!.Cards.Any(rule => rule.DataId == card.DataId && rule.Effects.Any(effect => effect.Type == "Bump"))))
        {
            var action = plays.Where(action => action.CardInstanceId == card.InstanceId).OrderBy(action =>
                rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId).Team == CombatTeam.Enemy ? 0 : 1)
                .FirstOrDefault(action => action.RoomIndex == 0 && rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.Id == action.TargetUnitId &&
                    (unit.Team == CombatTeam.Player || unit.EquipmentCards?.Count > 0)));
            if (action != null) return action;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal<T>(T expected, T actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(expected), Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
