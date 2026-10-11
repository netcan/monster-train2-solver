using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalTriggerRelicBatchChecks
{
    private const string Angel = "SubtypesData_Angel", Dragon = "SubtypesData_Dragon", Whelp = "SubtypesData_Whelp";
    private static readonly (string Key, string Id, string Kind, string? Subtype, bool Enemy, string Target, string[] Statuses, int[] Stacks)[] Artifacts =
    [
        ("EquippedUnitsGainArmor", "9af0364b-8e05-45d9-be46-7b7dd1e721e7", "OnEquipmentAdded", null, false, "Self", ["armor"], [10]),
        ("DamageShieldOnEquip", "0c50313f-1f07-40c4-8a3e-54748b57981f", "OnEquipmentAdded", null, false, "Self", ["damage shield"], [1]),
        ("ManyBuffonAngelEquip", "c97500a9-b0a4-4c18-b203-b9f4447c84c3", "OnEquipmentAdded", Angel, true, "Self", ["valor", "buff", "regen"], [3, 3, 3]),
        ("SurgeonsStitcher", "8f22145d-5c41-45da-8abf-95bc5f369648", "OnEquipmentAdded", null, false, "Self", [], []),
        ("AngelsSpawnWithShiftyValor", "3d4705ec-ee6b-433b-88c8-1b4ce0ed7cc9", "OnShift", Angel, false, "Self", ["valor"], [2]),
        ("RageOnHymnistDeath", "ec46f782-87ad-4cbd-aacb-023701417283", "OnDeath", null, false, "Room", ["buff"], [7]),
        ("WhelpsGainExtinguishHealPyre", "e6a167d5-f57e-4a02-a0f0-b8cf9373c838", "OnDeath", Whelp, true, "Room", ["pyregel"], [5]),
        ("DragonsGainStrikeAddPyregel", "1fdf2fde-a5e7-4eae-86eb-d432145c4621", "OnAttacking", Dragon, false, "LastAttackedCharacter", ["pyregel"], [5]),
        ("WhelpStrikeAddPyregel_DH", "588825ab-8517-48db-8447-31b48e931aaa", "OnAttacking", Whelp, false, "LastAttackedCharacter", ["pyregel"], [3])
    ];
    internal static IReadOnlyDictionary<string, int>? Native(FixtureValue root, bool required = false)
    {
        bool eligible = root.TryGetProperty("ModifierScenario", out var scenario) && scenario.GetString() == "original-trigger-relic-batch";
        Require(eligible || !required, "Fixture lacks the original added-trigger relic batch.");
        if (!eligible) return null;
        int schema = root.GetProperty("Schema").GetInt32();
        bool deathCapture = schema == 103;
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4", "Native build changed.");
        Require(schema is 103 or 106 && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Native trigger relic battle is incomplete.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Native trigger relic capture has nonzero " + key);
        var installs = Artifacts.ToDictionary(item => item.Key, _ => 0);
        var rejections = Artifacts.ToDictionary(item => item.Key, _ => 0);
        var witnesses = Artifacts.ToDictionary(item => item.Key, _ => 0);
        var definitions = new Dictionary<string, CombatTrigger>();
        int births = 0, enemyAngels = 0, admitted = 0, excluded = 0;
        foreach (var row in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "Native relic birth is incomplete or differs.");
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = row.GetProperty("After").Deserialize<RoomCombatState>()!;
            CheckDefinitions(before.Context!.Relics!, definitions);
            int id = row.GetProperty("UnitId").GetInt32(), fromCard = row.GetProperty("FromCardId").GetInt32();
            bool covenant = row.GetProperty("OnlyCovenants").GetBoolean();
            string parent = Serialize(before);
            void Check()
            {
                var result = RelicSpawnStatusModel.CharacterAdded(before, id, fromCard, covenant);
                Require(result.Supported, "Native added-trigger birth unsupported: " + result.UnsupportedReason);
                Equal(result.State, after, "complete native added-trigger birth");
                Require(result.Events.Count == 0 && result.PendingCallbacks.Count == 0 &&
                    result.State!.Context!.BattleRng.Equals(before.Context!.BattleRng), "Trigger installation consumed RNG or emitted callbacks.");
            }
            Check(); Parallel.For(0, 32, _ => Check());
            Require(Serialize(before) == parent, "Installation branches changed their parent.");
            CombatUnit prior = before.Units.Single(unit => unit.Id == id), actor = after.Units.Single(unit => unit.Id == id);
            if (actor.Team == CombatTeam.Enemy && actor.Subtypes.Contains(Angel)) enemyAngels++;
            foreach (var item in Artifacts)
            {
                var added = actor.Triggers.Where(trigger => Matches(trigger, definitions[item.Key]) && !prior.Triggers.Any(old => old.StateId == trigger.StateId)).ToArray();
                bool expected = (actor.Team == CombatTeam.Player || item.Enemy) &&
                    (item.Subtype == null ? !actor.IsPyre : actor.Subtypes.Contains(item.Subtype));
                Require(added.Length == (expected ? 1 : 0), "Original installation/filter differs: " + item.Key);
                if (!expected) { rejections[item.Key]++; continue; }
                installs[item.Key]++;
                Require(added[0].NoCountModifiersAllowed == (item.Key != "EquippedUnitsGainArmor"), "Original count flag differs.");
                int expectedCount = item.Key == "EquippedUnitsGainArmor" ? 2 : 1;
                Require(added[0].FireCount == expectedCount, "Original admission count differs: " + item.Key);
                if (expectedCount == 2) admitted++; else if (item.Kind is "OnEquipmentAdded" or "OnShift" or "OnDeath") excluded++;
            }
            births++;
        }
        Require(births > 0 && enemyAngels > 0 && admitted > 0 && excluded > 0 &&
            installs.Values.All(count => count > 0) && rejections.Values.All(count => count > 0), "Missing original installs, rejection or count gates.");

        int equipmentPlays = 0, enemyEquipment = 0, woundedHeals = 0, shifts = 0;
        foreach (var row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported, "Trigger relic action unsupported: " + result.Reason);
            // Moving a unit can move the native UI's selected floor. Compare the
            // same complete battle projection used by all recorded card actions.
            Require(ModelJson.Difference(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual)) == null,
                "Complete native trigger relic action differs.");
            var card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            var playRule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
            CombatUnit? actor = before.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == action.TargetUnitId);
            if (playRule.Equipment != null)
            {
                equipmentPlays++; if (actor?.Team == CombatTeam.Enemy) enemyEquipment++;
                if (actor?.Team == CombatTeam.Player && actor.Health < actor.MaxHealth &&
                    actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == actor.Id).Health == actor.MaxHealth) woundedHeals++;
            }
            if (playRule.Effects.Any(effect => effect.Type == "Bump")) shifts++;
            foreach (var item in Artifacts)
            {
                if (!before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Triggers.Any(trigger => Matches(trigger, definitions[item.Key])))) continue;
                var ablated = BattleActionModel.PlayCard(Remove(before, definitions[item.Key]), action);
                Require(ablated.Supported, "Relic action ablation unsupported: " + item.Key + " " + ablated.Reason);
                if (Facts(ablated.State!.Spawn.Train.Rooms.SelectMany(room => room.Units)) != Facts(actual.Spawn.Train.Rooms.SelectMany(room => room.Units))) witnesses[item.Key]++;
            }
        }
        foreach (var row in root.GetProperty("Stages").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            bool exchange = row.GetProperty("Kind").GetString() == "Exchange";
            var result = exchange ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before);
            Require(result.Supported, "Trigger relic native exchange unsupported.");
            Equal(result.State, actual, "complete native trigger relic exchange");
            foreach (var item in Artifacts.Where(item => item.Kind is "OnAttacking" or "OnDeath"))
            {
                if (!before.Units.Any(unit => unit.Triggers.Any(trigger => Matches(trigger, definitions[item.Key])))) continue;
                var ablated = exchange ? RoomCombatModel.Exchange(Remove(before, definitions[item.Key])) :
                    RoomCombatModel.Resolve(Remove(before, definitions[item.Key]));
                Require(ablated.Supported, "Relic combat ablation unsupported: " + item.Key + " " + ablated.UnsupportedReason);
                if (Facts(ablated.State!.Units) != Facts(actual.Units)) witnesses[item.Key]++;
            }
        }
        Console.WriteLine("ORIGINAL-TRIGGER-RELIC-WITNESSES " + string.Join(", ", witnesses.Select(item => item.Key + "=" + item.Value)));
        // The earlier stationary battle has the natural Angel death witness;
        // the later physical battle covers Shift and enemy equipment. Require
        // each archive's observed scope, and validate their union separately.
        string absentWitness = deathCapture ? "AngelsSpawnWithShiftyValor" : "RageOnHymnistDeath";
        Require(equipmentPlays > 0 && woundedHeals > 0 && (deathCapture || enemyEquipment > 0 && shifts > 0) &&
            witnesses.Where(item => item.Key != absentWitness).All(item => item.Value > 0),
            "Some original relics lack a positive effect beyond trigger metadata.");
        Console.WriteLine($"NATIVE-ORIGINAL-TRIGGER-RELICS PASS: nine unchanged originals installed, eight required effect witnesses ({(deathCapture ? "stationary death" : "physical Shift")}), {births} complete births, {enemyAngels} enemy Angels, {admitted} admitted/{excluded} excluded counts, {equipmentPlays} equips/{enemyEquipment} enemy equips/{woundedHeals} wounded heals, {shifts} shifts; per-relic effect ablations and 32 immutable installation branches.");
        return witnesses;
    }
    internal static void Combined(FixtureValue deaths, FixtureValue shifts)
    {
        Require(deaths.GetProperty("Schema").GetInt32() == 103 && shifts.GetProperty("Schema").GetInt32() == 106,
            "Combined acceptance requires both stationary and physical native archives.");
        var a = Native(deaths, required: true)!; var b = Native(shifts, required: true)!;
        Require(Artifacts.All(item => a[item.Key] > 0 || b[item.Key] > 0), "The two native archives do not cover every original relic effect.");
        Console.WriteLine("NATIVE-ORIGINAL-TRIGGER-RELICS-COMBINED PASS: all nine unchanged originals have actual effect witnesses across two complete native battles.");
    }
    private static void CheckDefinitions(IReadOnlyList<CombatRelicState> relics, Dictionary<string, CombatTrigger> definitions)
    {
        foreach (var item in Artifacts)
        {
            CombatRelicState relic = relics.Single(relic => relic.DataId == item.Id);
            var rule = relic.TriggerAdditions!.Single(); var trigger = rule.Triggers.Single();
            Require(relic.AssetKey == item.Key && relic.EffectTypes.SequenceEqual([RelicModel.AddTrigger]) && relic.IsCovenant == false &&
                relic.DisallowedInPlacementPhase == false && rule.EffectIndex == 0 && rule.SourcePlayer && rule.SourceEnemy == item.Enemy &&
                rule.SubtypeIsNone == (item.Subtype == null) && (item.Subtype == null || rule.RequiredSubtype == item.Subtype) && !rule.SubtypeIsPyre &&
                rule.CharacterAssetKeys.Count == 0 && rule.Conditions.Count == 0 && rule.ExternalInteractions.Count == 0 &&
                rule.NoCountModifiersAllowed == (item.Key != "EquippedUnitsGainArmor") && trigger.Kind == item.Kind && !trigger.Once &&
                trigger.IgnoreSilence && trigger.TriggerAtThreshold == 0 && trigger.Conditions == null && !trigger.RemoveOnRelentlessChange &&
                trigger.Origin?.OnlyIfEquipped == false,
                "Original trigger relic identity/rules changed: " + item.Key);
            if (item.Key == "SurgeonsStitcher") Require(trigger.Effects.Single().Type == "CardEffectHeal" &&
                trigger.Effects[0].Action?.Value == 999999 && trigger.Effects[0].Action!.Target == "Self", "Original surgeon heal changed.");
            else Require(trigger.Effects.Count == item.Statuses.Length && trigger.Effects.SelectMany(effect => effect.Action!.Statuses)
                .Select(status => (status.Id, status.Stacks)).SequenceEqual(item.Statuses.Zip(item.Stacks)) &&
                trigger.Effects.All(effect => effect.Type == "CardEffectAddStatusEffect" && effect.Action?.Target == item.Target),
                "Original trigger payload changed: " + item.Key);
            Require(item.Key != "RageOnHymnistDeath" || trigger.Effects[0].Action!.Filters!.Subtype == Angel,
                "Original Hymnist death target subtype changed.");
            if (definitions.TryGetValue(item.Key, out var old)) Equal(old, trigger, "stable original trigger descriptor");
            else definitions.Add(item.Key, trigger);
        }
    }
    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        var plays = BattleActionModel.EnumerateSupportedPlays(source); var rooms = source.Spawn.Train.Rooms;
        var context = source.Spawn.Train.Context!;
        bool Has(string dataId, Func<CardPlayRule, bool> predicate) => source.PlayRules!.Cards.Any(rule => rule.DataId == dataId && predicate(rule));
        bool Wound(string dataId) => Has(dataId, rule => rule.Effects.Any(effect => effect.Type == "Damage" && effect.AllowPlayer && !effect.AllowEnemy));
        foreach (string subtype in new[] { Angel, Whelp })
        foreach (var card in context.Cards.Hand.Where(card => Has(card.DataId, rule => rule.SpawnUnit?.Subtypes.Contains(subtype) == true)))
        {
            var summon = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.RoomIndex == 0);
            if (summon != null) return summon;
        }
        var carrier = rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Team == CombatTeam.Player &&
            unit.Subtypes.Contains(Angel) && unit.EquipmentCards?.Count == 0 && unit.Health == unit.MaxHealth);
        if (carrier != null)
        foreach (var card in context.Cards.Hand.Where(card => Wound(card.DataId)))
        {
            var wound = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.TargetUnitId == carrier.Id);
            if (wound != null) return wound;
        }
        var fragile = context.SpawnPoints == null ? null : rooms.Where(room => room.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(Angel)) &&
            room.Units.Any(unit => unit.Team == CombatTeam.Enemy)).SelectMany(room => room.Units)
            .FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(Whelp) && unit.Health <= 12);
        if (fragile != null)
        foreach (var card in context.Cards.Hand.Where(card => Wound(card.DataId)))
        {
            var death = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.TargetUnitId == fragile.Id);
            if (death != null) return death;
        }
        foreach (var card in context.Cards.Hand.Where(card => Has(card.DataId, rule => rule.Equipment != null)))
        {
            var equip = plays.Where(action => action.CardInstanceId == card.InstanceId).OrderBy(action =>
                rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId).EquipmentCards?.Count ?? 0)
                .ThenBy(action => rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId).Team == CombatTeam.Player ? 0 : 1)
                .FirstOrDefault(action => rooms.SelectMany(room => room.Units).Any(unit => unit.Id == action.TargetUnitId &&
                    unit.Subtypes.Contains(Angel) && unit.IsBoss != true && (unit.Team == CombatTeam.Player ||
                        unit.Triggers.Any(trigger => trigger.Kind == "OnEquipmentAdded" && trigger.Effects.Any(effect => effect.Action?.Statuses.Any(status => status.Id == "valor") == true)))));
            if (equip != null) return equip;
        }
        foreach (var card in context.Cards.Hand.Where(card => Has(card.DataId, rule => rule.Effects.Any(effect => effect.Type == "Bump"))))
        {
            var shift = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Player && unit.Subtypes.Contains(Angel)));
            if (shift != null) return shift;
        }
        return plays.FirstOrDefault(action => !Wound(context.FindCard(action.CardInstanceId)!.DataId));
    }
    private static bool Matches(CombatTrigger trigger, CombatTrigger definition) => trigger.Kind == definition.Kind &&
        trigger.Origin?.UpgradeId == "" && trigger.Origin.EquipmentCardId == 0 && Serialize(trigger.Effects) == Serialize(definition.Effects);
    private static string Facts(IEnumerable<CombatUnit> units) => Serialize(units.Select(unit => new
    {
        unit.Id, unit.Team, unit.BaseAttack, unit.Health, unit.MaxHealth,
        Statuses = unit.Statuses.Select(status => new { status.Id, status.Stacks }).OrderBy(status => status.Id)
    }).OrderBy(unit => unit.Id));
    private static RoomCombatState Remove(RoomCombatState state, CombatTrigger definition) => new(state.RoomIndex, state.Deployment,
        state.Units.Select(unit => unit.WithTriggers(unit.Triggers.Where(trigger => !Matches(trigger, definition)).ToArray())).ToArray(),
        state.ExternalInteractions, state.Context, state.Preview);
    private static BattleTurnState Remove(BattleTurnState state, CombatTrigger definition)
    {
        var train = new TrainCombatState(state.Spawn.Train.Rooms.Select(room => Remove(room, definition)).ToArray(),
            state.Spawn.Train.Movement, state.Spawn.Train.EnemySlotsPerRoom, state.Spawn.Train.Context);
        var spawn = state.Spawn;
        var copiedSpawn = new EnemySpawnState(train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, spawn.Rng,
            spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
            spawn.FirstTreasureRoom, spawn.Turn, spawn.ExternalInteractions, spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds);
        return new(copiedSpawn, state.Energy, state.EnergyPerTurn, state.DrawPerTurn, state.ForgePoints, state.DragonsHoard,
            state.MoonPhase, state.RngStreams, state.OtherPiles, state.ExternalInteractions, state.PlayRules, state.BattlePreviewEnabled,
            state.UiRngIsolated, state.CanonicalDecisionReferences, state.CanonicalPhysicalReferences, state.SelectedRoom);
    }
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal<T>(T expected, T actual, string label)
    {
        string? difference = ModelJson.Difference(Serialize(expected), Serialize(actual));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool value, string error) { if (!value) throw new InvalidDataException(error); }
}
