using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalBothSidesUnitsChecks
{
    internal static void Native(FixtureValue witness)
    {
        FixtureValue owner = witness.GetProperty("Owner");
        string? boundary = Text(witness, "Boundary");
        bool cardless = boundary == RelicBothSidesUnitsModel.AssetKey + "Cardless";
        Require(witness.GetProperty("Schema").GetInt32() == 1 &&
            (cardless || boundary == RelicBothSidesUnitsModel.AssetKey) &&
            Text(witness, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(witness, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(owner, "Id") == RelicBothSidesUnitsModel.DataId && Text(owner, "AssetKey") == RelicBothSidesUnitsModel.AssetKey &&
            !owner.GetProperty("DisallowInDeploymentPhase").GetBoolean() && owner.GetProperty("Effects").GetArrayLength() == 7,
            "Expected the pinned original BothSidesUnitsAreStronger capture.");

        var before = witness.GetProperty("BeforePlay").Deserialize<BattleTurnState>()!;
        var action = witness.GetProperty("Play").Deserialize<PlayCardAction>()!;
        CombatContext context = before.Spawn.Train.Context!;
        CombatRelicState original = context.Relics!.Single(RelicBothSidesUnitsModel.Applies);
        Require(RelicBothSidesUnitsModel.Validate(original) == null && context.Resources!.MutatorIds.Contains(original.DataId) &&
            RelicBothSidesUnitsModel.Validate(original.WithResourceEffects([])) != null,
            "Expected the complete original payload and rejection of incomplete projection.");
        CardInstanceState? card = context.FindCard(action.CardInstanceId);
        if (cardless)
        {
            VerifyCardless(witness, before, action, card);
            return;
        }
        Require(before.Spawn.Turn == 0 && card?.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678" &&
            card.Temporary.Offsets.Damage == 5 && card.Temporary.Offsets.Health == 10,
            "Original Train Steward card modifier inputs differ.");

        var played = BattleActionModel.PlayCard(before, action);
        Require(played.Supported, played.Reason ?? "Expected a supported deployment summon.");
        BattleTurnState placed = RecordedBattleChecks.RecordedBoundary(played.State!, witness.GetProperty("BeforeEndTurn"),
            terminal: false, "Original stronger units paid deployment", trackPreviewPosition: true);
        CombatUnit steward = placed.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == action.CardInstanceId);
        Require(steward.Attack == 13 && steward.Health == 35 && steward.MaxHealth == 35,
            "Original Train Steward did not inherit the native temporary card modifiers.");

        BattleTurnResult advanced = BattleTurnModel.EndTurn(placed);
        Require(advanced.Supported, advanced.UnsupportedReason ?? "Expected a supported deployment EndTurn.");
        BattleTurnState wave = RecordedBattleChecks.RecordedBoundary(advanced.State!, witness.GetProperty("AfterWave"),
            terminal: false, "Original stronger units first wave", trackPreviewPosition: true);
        var enemies = wave.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Enemy).ToDictionary(unit => unit.Id);
        FixtureValue[] callbacks = witness.GetProperty("Births").EnumerateArray().ToArray();
        Require(wave.Spawn.Turn == 1 && callbacks.Length == 4 && enemies.Count == 2,
            "Expected two original enemies and four native stat callbacks.");
        foreach (var pair in callbacks.Chunk(2))
        {
            FixtureValue health = pair[0], attack = pair[1];
            int id = Number(health, "UnitId");
            CombatUnit enemy = enemies[id];
            FixtureValue start = health.GetProperty("Before"), grown = health.GetProperty("After"), buffed = attack.GetProperty("After");
            Require(Number(health, "EffectIndex") == 0 && Text(health, "Trigger") == "OnCharacterAdded" &&
                Number(attack, "EffectIndex") == 2 && Text(attack, "Trigger") == "OnSpawn" &&
                Number(attack, "UnitId") == id && Text(health, "AssetKey") == enemy.AssetKey &&
                Text(attack, "AssetKey") == enemy.AssetKey && !health.GetProperty("FromCard").GetBoolean() &&
                !attack.GetProperty("FromCard").GetBoolean() && grown.ContentEquals(attack.GetProperty("Before")) &&
                Number(grown, "Health") == Number(start, "Health") + 10 && Number(grown, "MaxHealth") == Number(start, "MaxHealth") + 10 &&
                Number(grown, "DamageBuff") == Number(start, "DamageBuff") &&
                Number(buffed, "Health") == Number(grown, "Health") && Number(buffed, "MaxHealth") == Number(grown, "MaxHealth") &&
                Number(buffed, "DamageBuff") == Number(grown, "DamageBuff") + 5 &&
                enemy.Health == Number(buffed, "Health") && enemy.MaxHealth == Number(buffed, "MaxHealth") &&
                enemy.Modifiers?.DamageBuff == Number(buffed, "DamageBuff"),
                "Original enemy stat callbacks differ for " + enemy.AssetKey);
        }

        VerifyPyre(before, witness.GetProperty("PyreBeforePlay"));
        VerifyPyre(wave, witness.GetProperty("PyreAfterWave"));
        Console.WriteLine("NATIVE-BOTH-SIDES-UNITS PASS: exact paid placement and first wave, four native stat callbacks, and Pyre base query and refresh.");
    }

    private static void VerifyCardless(FixtureValue witness, BattleTurnState before, PlayCardAction action, CardInstanceState? card)
    {
        const string eagerConscript = "0c8ff108-478c-4c1c-9c49-f6a297b4e0d8";
        RoomCombatState selected = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex);
        Require(before.Spawn.Turn == 0 && selected.Deployment &&
            selected.Units.All(unit => unit.AssetKey != "MonsterFunguy") &&
            Text(witness, "DonorCardDataId") == eagerConscript && card?.DataId == eagerConscript,
            "Expected the original Eager Conscript and an empty Funguy placement.");
        var played = BattleActionModel.PlayCard(before, action);
        Require(played.Supported, played.Reason ?? "Expected a supported original cardless summon.");
        BattleTurnState after = RecordedBattleChecks.RecordedBoundary(played.State!, witness.GetProperty("AfterPlay"),
            terminal: false, "Original stronger units paid cardless birth", trackPreviewPosition: true);
        RoomCombatState room = after.Spawn.Train.Rooms.Single(item => item.RoomIndex == action.RoomIndex);
        CombatUnit fungus = room.Units.Single(unit => unit.AssetKey == "MonsterFunguy");
        Require(before.Energy - after.Energy == 1 &&
            after.Spawn.Train.Context!.Cards.Discard.Any(token => token.InstanceId == action.CardInstanceId) &&
            fungus.Team == CombatTeam.Player && fungus.SpawnerCardId == 0 &&
            fungus.Status("cardless")?.Stacks == 1 && fungus.Status("horde")?.Stacks == 1 &&
            fungus.Health == 14 && fungus.MaxHealth == 14 && fungus.Attack == 13 &&
            fungus.Modifiers is { IsClone: false, AttackDamage: 3, DamageBuff: 10 },
            "Original Eager Conscript payment or settled cardless Funguy state differs.");

        FixtureValue[] callbacks = witness.GetProperty("Births").EnumerateArray().ToArray();
        Require(callbacks.Select(row => Number(row, "EffectIndex")).SequenceEqual(new[] { 1, 4, 3 }) &&
            callbacks.Select(row => Text(row, "Trigger")).SequenceEqual(new[] { "OnCharacterAdded", "OnCharacterAdded", "OnSpawnNotFromCard" }) &&
            callbacks.All(row => Number(row, "UnitId") == fungus.Id && Text(row, "AssetKey") == fungus.AssetKey &&
                !row.GetProperty("FromCard").GetBoolean()), "Original cardless character callback identity or order differs.");
        FixtureValue initial = callbacks[0].GetProperty("Before");
        FixtureValue grown = callbacks[0].GetProperty("After");
        FixtureValue cardAttack = callbacks[1].GetProperty("After");
        FixtureValue final = callbacks[2].GetProperty("After");
        Require(grown.ContentEquals(callbacks[1].GetProperty("Before")) &&
            cardAttack.ContentEquals(callbacks[2].GetProperty("Before")) &&
            Stats(initial) == (4, 4, 0) && Stats(grown) == (14, 14, 0) &&
            Stats(cardAttack) == (14, 14, 5) && Stats(final) == (14, 14, 10),
            "Expected +10 current/maximum health and two separate +5 damage buffs.");
        Console.WriteLine("NATIVE-BOTH-SIDES-UNITS-CARDLESS PASS: exact paid successor and all three native birth callbacks.");

        static (int Health, int MaxHealth, int DamageBuff) Stats(FixtureValue value) =>
            (Number(value, "Health"), Number(value, "MaxHealth"), Number(value, "DamageBuff"));
    }

    private static void VerifyPyre(BattleTurnState state, FixtureValue sample)
    {
        CombatContext context = state.Spawn.Train.Context!;
        CombatUnit pyre = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre);
        int authored = Number(sample, "AuthoredBase"), queried = Number(sample, "BaseAttack");
        int persistent = Number(sample, "PersistentBonus"), upgrade = Number(sample, "UpgradeAttack");
        int rawAttack = Number(sample, "RawAttack"), saved = Number(sample, "SavedAttackOffset");
        RelicResourceResult query = RelicResourceModel.GetPyreAttack(context, placement: state.Spawn.Turn == 0);
        RelicPyreAttackRefreshResult refreshed = RelicResourceModel.RefreshPyreAttack(context, pyre.WithRawAttack(authored),
            placement: state.Spawn.Turn == 0);
        Require(sample.GetProperty("RngBefore").ContentEquals(sample.GetProperty("RngAfter")) &&
            context.Resources!.PyreBaseAttack == authored && context.Resources.PyrePersistentDamage == persistent &&
            context.Resources.PyreModifiedBaseAttack == saved && queried == authored + persistent + 10 &&
            rawAttack == queried + upgrade && rawAttack == Number(sample, "CombatAttack") &&
            pyre.Modifiers?.AttackDamage == rawAttack && pyre.Modifiers.AttackDamageAdded == upgrade &&
            query.Supported && query.Value == queried && refreshed.Supported &&
            refreshed.Pyre?.Modifiers?.AttackDamage == rawAttack && refreshed.Pyre.Modifiers.AttackDamageAdded == upgrade,
            query.UnsupportedReason ?? refreshed.UnsupportedReason ?? "Original +10 fell out of the Pyre query or refresh.");
    }

    private static string? Text(FixtureValue value, string property) => value.GetProperty(property).GetString();
    private static int Number(FixtureValue value, string property) => value.GetProperty(property).GetInt32();
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
