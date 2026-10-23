using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class AngelDuoIncantChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("AngelDuoIncantControl").GetProperty("Kind").GetString() == "LegalSpellAtOriginalAngelDuo" &&
            fixture.GetProperty("AngelDuoIncantControl").GetProperty("DeckAndCharacterAssetsUnchanged").GetBoolean(),
            "Angel Duo Incant evidence requires legal native spells and unchanged authored assets.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Angel Duo Incant native battle failed: " + counter);
        FixtureValue input = fixture.GetProperty("NativeRunInput");
        Require(input.GetProperty("AscensionLevel").GetInt32() == 2 && input.GetProperty("StartingLevel").GetInt32() == 2 &&
            input.GetProperty("Seed").GetInt32() == 424242 && input.GetProperty("Distance").GetInt32() == 5 &&
            input.GetProperty("Scenario").GetString() == "Level6AngelDuo_RallyAttackIncantHealth" &&
            input.GetProperty("BattlesWon").GetInt32() == 5 && !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            input.GetProperty("CompletedBattles").GetArrayLength() == 5 &&
            input.GetProperty("CompletedBattles").EnumerateArray().All(row => row.GetProperty("Won").GetBoolean()) &&
            fixture.GetProperty("StartingCovenantControl").ValueKind == FixtureKind.Null &&
            fixture.GetProperty("OriginalOuterBossRoute").ValueKind == FixtureKind.Null &&
            fixture.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            fixture.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop",
            "Angel Duo Incant requires the original cumulative rank-two five-win native input.");
        int casts = 0, remoteHeals = 0, excludedBosses = 0;
        foreach (FixtureValue row in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction play = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string? cardId = before.Spawn.Train.Context!.FindCard(play.CardInstanceId)?.DataId;
            CardPlayRule? card = before.PlayRules!.Cards.SingleOrDefault(rule => rule.DataId == cardId);
            if (card?.CardType != "Spell" || card.IsAnyAbility != false) continue;
            CombatUnit? outer = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == play.RoomIndex).Units
                .SingleOrDefault(unit => OuterBossDefinitions.IsAngelDuo(unit.AssetKey) && unit.Health > 0);
            if (outer == null) continue;
            RoomCombatState? remote = before.Spawn.Train.Rooms.FirstOrDefault(room => room.RoomIndex != play.RoomIndex &&
                room.Units.Any(unit => unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack" && unit.Health > 0));
            if (remote == null) continue;
            CombatUnit inner = remote.Units.Single(unit => unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack");
            CombatTrigger trigger = outer.Triggers.Single(value => value.Kind == "CardSpellPlayed");
            CombatEffect effect = trigger.Effects.Single();
            Require(effect.Type == "CardEffectBuffMaxHealth" && effect.Value == 30 && effect.Action is
                { Type: "BuffHealth", Target: "Tower", Lifetime: "TemporaryUntilEndOfBattle", UseCardTraits: false } &&
                effect.Action.Filters?.Subtype == "SubtypesData_SavageAngel",
                "Angel Duo Incant changed its original +30 Tower health effect or subtype selection.");
            CombatUnit healed = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == inner.Id);
            CombatUnit excluded = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == outer.Id);
            Require(healed.MaxHealth == inner.MaxHealth + 30 * trigger.FireCount &&
                excluded.MaxHealth == outer.MaxHealth,
                "Original Incant did not exclusively increase the remote Savage companion's maximum health.");
            if (healed.Health == inner.Health + 30 * trigger.FireCount) remoteHeals++;
            excludedBosses++;
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            string expected = BattleTurnChecks.Comparable(actual, compareRoomEnabled: BattleTurnChecks.ShouldCompareRoomEnabled(fixture));
            Require(BattleActionModel.EnumerateSupportedPlays(before, play.CardInstanceId).Any(candidate =>
                candidate.CardInstanceId == play.CardInstanceId && candidate.RoomIndex == play.RoomIndex &&
                JsonSerializer.Serialize(candidate, ModelJson.Options) == JsonSerializer.Serialize(play, ModelJson.Options)),
                "The native cross-floor Incant spell disappeared from the searchable action frontier.");
            Parallel.For(0, 16, _ => {
                BattleActionResult branch = BattleActionModel.PlayCard(before, play);
                Require(branch.Supported && BattleTurnChecks.Comparable(branch.State!,
                    compareRoomEnabled: BattleTurnChecks.ShouldCompareRoomEnabled(fixture)) == expected &&
                    JsonSerializer.Serialize(before, ModelJson.Options) == immutable,
                    "Angel Duo Incant branch changed complete native state/RNG or mutated its root.");
            });
            Require(before.Spawn.Train.Context!.Enchantments?.TemporaryPreviewUnitIds == null &&
                actual.Spawn.Train.Context!.Enchantments?.TemporaryPreviewUnitIds == null,
                "A temporary companion preview scope leaked into a stable native decision.");
            casts++;
        }
        Require(casts > 0 && remoteHeals > 0 && excludedBosses > 0,
            "Angel Duo Incant requires positive native remote casts, healing and excluded outer-boss controls.");
        int slayIncrements = 0;
        foreach (FixtureValue row in fixture.GetProperty("Stages").EnumerateArray())
        {
            if (row.GetProperty("Kind").GetString() != "Exchange") continue;
            RoomCombatState before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            CombatUnit? inner = before.Units.SingleOrDefault(unit =>
                unit.AssetKey == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack");
            if (inner == null) continue;
            CombatTrigger slay = inner.Triggers.Single(trigger => trigger.Kind == "OnKill");
            CombatEffect effect = slay.Effects.Single();
            Require(effect.Type == "CardEffectBuffDamage" && effect.Value == 3 && effect.Action is
                { Type: "BuffAttack", Target: "Tower", AllowPlayer: true, AllowEnemy: true, UseCardTraits: false } &&
                effect.Action.Filters?.Subtype == "SubtypesData_EntropicAngel",
                "Angel Duo Slay changed its original +3 Tower attack buff or subtype selection.");
            var prior = row.GetProperty("ExternalTargets").EnumerateArray().Select(target =>
                target.GetProperty("Unit").Deserialize<CombatUnit>()!).SingleOrDefault(unit => OuterBossDefinitions.IsAngelDuo(unit.AssetKey));
            if (prior == null) continue;
            CombatUnit after = row.GetProperty("ActualExternalTargets").EnumerateArray().Select(target =>
                target.GetProperty("Unit").Deserialize<CombatUnit>()!).Single(unit => unit.Id == prior.Id);
            int delta = after.Modifiers!.DamageBuff - prior.Modifiers!.DamageBuff;
            Require(delta >= 0 && delta % 3 == 0, "Original remote companion Slay attack increments changed.");
            slayIncrements += delta / 3;
        }
        Require(slayIncrements > 0, "Angel Duo battle requires a positive original remote Slay witness.");
        Console.WriteLine($"ANGEL-DUO-INCANT PASS: {casts} searchable original spells, {remoteHeals} remote +30 heals, {excludedBosses} excluded outer bosses and sixteen immutable full state/RNG branches per cast.");
        Console.WriteLine($"ANGEL-DUO-SLAY PASS: {slayIncrements} original remote +3 attack increments across independently checked native exchanges.");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
