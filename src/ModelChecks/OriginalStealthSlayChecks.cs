using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStealthSlayChecks
{
    private const string Id = "4687feca-a053-48ed-b208-81dc6dfc6e72";

    internal static PlayCardAction? Choose(BattleTurnState state)
    {
        var carrier = BattleActionModel.EnumerateSupportedPlays(state).FirstOrDefault(play =>
        {
            var rule = state.PlayRules!.Cards.FirstOrDefault(item =>
                item.DataId == state.Spawn.Train.Context!.FindCard(play.CardInstanceId)?.DataId);
            return rule?.SpawnUnit != null && state.Spawn.Train.Rooms.Single(room => room.RoomIndex == play.RoomIndex)
                .Units.Any(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0);
        });
        return carrier ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(state);
    }

    internal static void Native(FixtureValue root)
    {
        string scenario = root.GetProperty("ModifierScenario").GetString()!;
        int attackSchema = root.GetProperty("RelicStatusAttackSchema").GetInt32();
        string version = root.GetProperty("GameVersion").GetString()!;
        string mvid = root.GetProperty("GameModuleMvid").GetString()!;
        string[] errors = new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
            .Where(name => root.GetProperty(name).GetInt32() != 0).ToArray();
        Require(scenario == "original-stealth-slay" && attackSchema == 1 && version == "2.2.1" &&
            mvid == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && errors.Length == 0,
            $"Stealth-on-slay native battle incomplete or from another build: scenario={scenario}, schema={attackSchema}, version={version}, mvid={mvid}, errors={string.Join(',', errors)}.");

        int births = 0, friendlyBirths = 0, enemyBirths = 0, spawnWitnesses = 0;
        foreach (FixtureValue row in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Incomplete or mismatched stealth-on-slay birth.");
            TrainCombatState before = row.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("AfterTrain").Deserialize<TrainCombatState>()!;
            Definitions(before.Context!);
            string parent = Serialize(before);
            RelicBirthResult replay = RelicBirthModel.CharacterAdded(before, row.GetProperty("UnitId").GetInt32(),
                row.GetProperty("FromCardId").GetInt32(), row.GetProperty("OnlyCovenants").GetBoolean(),
                row.GetProperty("DeferCallbacks").GetBoolean(), row.GetProperty("QueuedBefore").Deserialize<UnitCloneCallback[]>()!);
            Require(replay.Supported, "Stealth-on-slay birth unsupported: " + replay.UnsupportedReason);
            Equal(replay.State, actual, "complete native birth/train state");
            Equal(replay.Queued, row.GetProperty("QueuedAfter").Deserialize<UnitCloneCallback[]>(), "native birth queue");
            Equal(replay.Dispatched, row.GetProperty("Dispatched").Deserialize<UnitCloneCallback[]>(), "native birth dispatches");

            int id = row.GetProperty("UnitId").GetInt32();
            CombatUnit source = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            CombatUnit born = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            if (source.Team == CombatTeam.Player && !source.IsPyre)
            {
                var sourceIds = source.Triggers.Where(trigger => trigger.StateId.HasValue).Select(trigger => trigger.StateId).ToHashSet();
                CombatTrigger[] added = born.Triggers.Where(trigger => trigger.StateId.HasValue && !sourceIds.Contains(trigger.StateId)).ToArray();
                Require(added.Length == 2 && added.Select(trigger => trigger.Kind).OrderBy(kind => kind, System.StringComparer.Ordinal)
                    .SequenceEqual(["OnKill", "OnSpawn"]), "Friendly birth did not install both original stealth triggers.");
                friendlyBirths++;
            }
            else enemyBirths++;
            Require(parent == Serialize(before), "Stealth-on-slay birth replay mutated its parent.");
            births++;
        }

        int actions = 0, friendlyAttackObservations = 0, enemyControls = 0, stackedStealth = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Definitions(before.Spawn.Train.Context!);
            AttackObservations(before.Spawn.Train.Rooms.SelectMany(room => room.Units),
                ref friendlyAttackObservations, ref enemyControls, ref stackedStealth);
            AttackObservations(actual.Spawn.Train.Rooms.SelectMany(room => room.Units),
                ref friendlyAttackObservations, ref enemyControls, ref stackedStealth);
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleActionResult replay = BattleActionModel.PlayCard(before, action);
            Require(replay.Supported, "Stealth-on-slay native action unsupported: " + replay.Reason);
            Equal(replay.State, actual, "full native action");
            var existingIds = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            foreach (CombatUnit unit in actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => !existingIds.Contains(unit.Id)))
            {
                if (unit.Team != CombatTeam.Player || unit.IsPyre) continue;
                Require(unit.Triggers.Any(trigger => trigger.Kind == "OnSpawn") && unit.Status("stealth")?.Stacks == 1,
                    "New friendly carrier did not receive exactly one native OnSpawn Stealth stack.");
                spawnWitnesses++;
            }
            actions++;
        }

        int onKillAblations = 0;
        foreach (FixtureValue row in root.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Definitions(before.Context!);
            bool exchange = row.GetProperty("Kind").GetString() == "Exchange";
            RoomCombatResult replay = exchange ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before);
            Require(replay.Supported, "Stealth-on-slay native stage unsupported: " + replay.UnsupportedReason);
            Equal(replay.State, actual, "complete native combat stage");
            AttackObservations(actual.Units, ref friendlyAttackObservations, ref enemyControls, ref stackedStealth);
            if (!exchange) continue;

            CombatTrigger definition = OnKill(before.Context!);
            RoomCombatState withoutSlayTrigger = before.WithUnits(before.Units.Select(unit => unit.WithTriggers(
                unit.Triggers.Where(trigger => !Matches(trigger, definition)).ToArray())).ToArray());
            RoomCombatResult ablated = RoomCombatModel.Exchange(withoutSlayTrigger);
            Require(ablated.Supported, "Stealth-on-slay OnKill ablation unsupported: " + ablated.UnsupportedReason);
            var controls = ablated.State!.Units.ToDictionary(unit => unit.Id);
            onKillAblations += actual.Units.Count(unit => unit.Team == CombatTeam.Player && controls.TryGetValue(unit.Id, out CombatUnit? control) &&
                (unit.Status("stealth")?.Stacks ?? 0) - (control.Status("stealth")?.Stacks ?? 0) > 0);
        }

        Require(births > 0 && friendlyBirths > 0 && enemyBirths > 0 && spawnWitnesses > 0 && actions >= 8 &&
            friendlyAttackObservations > 0 && enemyControls > 0 && stackedStealth > 0 && onKillAblations > 0,
            $"Missing native trigger/status attack witnesses: births={births},friendly={friendlyBirths},enemy={enemyBirths},spawn={spawnWitnesses},actions={actions},attack={friendlyAttackObservations},enemyControl={enemyControls},stacked={stackedStealth},onKill={onKillAblations}.");
        Console.WriteLine($"ORIGINAL-STEALTH-SLAY-NATIVE PASS: {births} exact births ({friendlyBirths} friendly/{enemyBirths} enemy), {spawnWitnesses} OnSpawn stacks, {actions} exact actions, +3 attack per friendly Stealth, {onKillAblations} positive OnKill ablations and zero differential errors.");
    }

    private static void Definitions(CombatContext context)
    {
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == Id);
        Require(owner.AssetKey == "UnitsGainStealthOnSlayPlusAttack" && owner.IsCovenant == false &&
            owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual([RelicModel.ModifyAttackByStatus, RelicModel.AddTrigger]),
            "Unchanged stealth-on-slay owner or effect order changed.");
        RelicStatusAttackModifier attack = owner.StatusAttackModifiers!.Single();
        Require(attack.EffectIndex == 0 && attack.StatusId == "stealth" && attack.Team == "Monsters" && attack.Multiplier == 3,
            "Unchanged stealth attack query differs.");
        RelicCharacterTriggers addition = owner.TriggerAdditions!.Single();
        Require(addition.EffectIndex == 1 && addition.SourcePlayer && !addition.SourceEnemy && addition.NoCountModifiersAllowed &&
            addition.SubtypeIsNone && !addition.SubtypeIsPyre && addition.CharacterAssetKeys.Count == 0 && addition.Conditions.Count == 0 &&
            addition.ExternalInteractions.Count == 0 && addition.Triggers.Select(trigger => trigger.Kind).SequenceEqual(["OnKill", "OnSpawn"]),
            "Unchanged stealth trigger filters or order differ.");
        foreach (CombatTrigger trigger in addition.Triggers)
        {
            CombatEffect effect = trigger.Effects.Single();
            CardActionEffect action = effect.Action!;
            Require(!trigger.Once && trigger.IgnoreSilence && trigger.Origin?.OnlyIfEquipped == false &&
                effect.Type == "CardEffectAddStatusEffect" && action.Type == "AddStatus" && action.Target == "Self" &&
                action.AllowPlayer && !action.AllowEnemy && action.Statuses.Single().Id == "stealth" && action.Statuses.Single().Stacks == 1,
                "Unchanged stealth trigger action payload differs.");
        }
    }

    private static CombatTrigger OnKill(CombatContext context) => context.Relics!.Single(relic => relic.DataId == Id)
        .TriggerAdditions!.Single().Triggers.Single(trigger => trigger.Kind == "OnKill");

    private static void AttackObservations(System.Collections.Generic.IEnumerable<CombatUnit> units,
        ref int friendly, ref int enemy, ref int stacked)
    {
        foreach (CombatUnit unit in units)
        {
            RelicStatusAttackModifier rule = unit.Modifiers!.StatusAttackModifiers!.Single();
            int stacks = unit.Status("stealth")?.Stacks ?? 0;
            int expected = unit.Team == CombatTeam.Player ? stacks * 3 : 0;
            int actual = unit.Attack - unit.AttackBeforeStatuses - unit.AttackFromStatuses;
            Require(rule.EffectIndex == 0 && rule.StatusId == "stealth" && rule.Team == "Monsters" && rule.Multiplier == 3 && actual == expected,
                "Dynamic stealth attack differs for native unit " + unit.Id + ".");
            if (unit.Team == CombatTeam.Player)
            {
                if (stacks > 0) friendly++;
                if (stacks >= 2) stacked++;
            }
            else enemy++;
        }
    }

    private static bool Matches(CombatTrigger trigger, CombatTrigger definition) => trigger.Kind == "OnKill" &&
        trigger.Origin?.UpgradeId == "" && trigger.Origin.EquipmentCardId == 0 && Serialize(trigger.Effects) == Serialize(definition.Effects);
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal<T>(T actual, T expected, string label) => Require(Serialize(actual) == Serialize(expected), label + " differs.");
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}
