using System;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSummonWithAvariceChecks
{
    private const string RelicId = "a9670728-2ad2-4776-be80-e1a08864f18c";
    private const string RelicKey = "SummonWithAvarice";
    private const string Effect = "RelicEffectAddStatusEffectOnSpawn";

    internal static void Run()
    {
        CombatRelicState relic = Relic();
        Require(RelicModel.Validate([relic]) == null, "The captured original Avarice relic was rejected.");
        var rng = UnityRng.Seed(73);
        CombatUnit carrier = Unit(1, CombatTeam.Player, 4, 20, [new("avarice", 2)]);
        CombatUnit victim = Unit(2, CombatTeam.Enemy, 0, 30, []);
        RoomCombatState room = Room([carrier, victim], rng, 10);
        RoomCombatResult attack = RoomCombatModel.ApplyUnitTurn(room, carrier.Id);
        Require(attack.Supported && attack.State!.Context!.Gold == 12 &&
            attack.Events.Any(item => item.Kind == "Gold" && item.Actor == carrier.Id && item.Amount == 2),
            "A positive direct attack did not award one gold per Avarice stack: " +
            (attack.UnsupportedReason ?? $"gold={attack.State?.Context?.Gold}, events={string.Join(',', attack.Events.Select(item => item.Kind + ':' + item.Amount))}."));
        RoomCombatResult adjusted = RoomCombatModel.ApplyUnitTurn(Room([carrier, victim], rng, 10,
            goldRewardMultiplier: 1.5f), carrier.Id);
        Require(adjusted.Supported && adjusted.State!.Context!.Gold == 13 &&
            adjusted.Events.Any(item => item.Kind == "Gold" && item.Amount == 3),
            "Avarice gold did not use the captured campaign reward multiplier and unit rounding.");

        CombatUnit first = Unit(3, CombatTeam.Enemy, 0, 3, []);
        CombatUnit second = Unit(4, CombatTeam.Enemy, 0, 4, []);
        CombatUnit third = Unit(5, CombatTeam.Enemy, 0, 20, []);
        CombatUnit trampler = Unit(6, CombatTeam.Player, 10, 30, [new("avarice", 2), new("trample", 1)]);
        RoomCombatResult trample = RoomCombatModel.ApplyUnitTurn(Room([first, second, third, trampler], rng, 10), trampler.Id);
        Require(trample.Supported && trample.State!.Context!.Gold == 16 &&
            trample.Events.Count(item => item.Kind == "Gold" && item.Actor == trampler.Id && item.Amount == 2) == 3,
            "Avarice did not trigger independently for direct and each positive Trample hit.");

        CombatUnit armored = Unit(7, CombatTeam.Enemy, 0, 30, [new("armor", 10, 1)]);
        RoomCombatResult blocked = RoomCombatModel.ApplyUnitTurn(Room([carrier, armored], rng, 10), carrier.Id);
        Require(blocked.Supported && blocked.State!.Context!.Gold == 10,
            "A fully blocked attack triggered Avarice despite dealing no positive damage.");
        RoomCombatResult preview = RoomCombatModel.ApplyUnitTurn(new RoomCombatState(0, false,
            [carrier, victim], [], room.Context, preview: true), carrier.Id);
        Require(preview.Supported && preview.State!.Context!.Gold == 10,
            "Preview combat applied Avarice gold to the live campaign state.");

        CardInstanceState regularSpell = DamageCard("ordinary-spell", unitAbility: false);
        CardInstanceState ability = DamageCard("unit-ability", unitAbility: true);
        RoomCombatResult spellDamage = RoomCombatModel.ApplyCardDamage(
            Room([carrier, victim], rng, 10, [regularSpell]), victim.Id, 3, regularSpell.InstanceId, attackerUnitId: carrier.Id);
        RoomCombatResult abilityDamage = RoomCombatModel.ApplyCardDamage(
            Room([carrier, victim], rng, 10, [ability]), victim.Id, 3, ability.InstanceId, attackerUnitId: carrier.Id);
        Require(spellDamage.Supported && spellDamage.State!.Context!.Gold == 10 &&
            abilityDamage.Supported && abilityDamage.State!.Context!.Gold == 12,
            "Avarice failed to distinguish unit-ability damage from ordinary spell damage.");

        var spawnRule = new RelicSpawnStatus(0, true, false, [new CombatStatus("avarice", 2)]);
        CombatRelicState owner = Relic(spawnRule);
        CombatUnit fresh = Unit(9, CombatTeam.Player, 4, 20, []);
        RoomCombatState spawnBefore = Room([fresh, victim], rng, 10, relics: [owner]);
        RoomCombatResult born = RelicSpawnStatusModel.CharacterAdded(spawnBefore, fresh.Id, 0, false);
        Require(born.Supported && born.State!.Context!.BattleRng.Equals(rng.Next()) &&
            born.State.Units.Single(unit => unit.Id == fresh.Id).Status("avarice")?.Stacks == 2 &&
            RelicModel.Validate([owner]) == null,
            "The original spawn owner did not install its two captured Avarice stacks with the native birth RNG draw.");
        RoomCombatResult bornAttack = RoomCombatModel.ApplyUnitTurn(born.State, fresh.Id);
        Require(bornAttack.Supported && bornAttack.State!.Context!.Gold == 12,
            "Avarice installed by the original birth relic did not affect the subsequent direct attack.");

        CombatRelicState wrongOwner = new("00000000-0000-0000-0000-000000000001", "OtherOwner", [Effect],
            isCovenant: false, disallowedInPlacementPhase: false);
        Require(RelicModel.Validate([wrongOwner]) != null,
            "An unknown owner with the Avarice spawn effect was silently accepted.");
        Parallel.For(0, 16, _ => Require(RoomCombatModel.ApplyUnitTurn(room, carrier.Id).State!.Context!.Gold == 12,
            "Parallel Avarice attack branch diverged."));
        Require(JsonSerializer.Serialize(room.Context!.Gold) == "10", "Avarice mutated its parent context.");
        Console.WriteLine("ORIGINAL-SUMMON-WITH-AVARICE-CHECKS PASS: two-stack birth, direct attack, three-hit Trample, blocked/preview/ordinary-spell controls, UnitAbility damage, exact gold multiplier path and immutable branches.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 197 &&
            root.GetProperty("ModifierScenario").GetString() == "original-summon-with-avarice" &&
            root.GetProperty("OriginalSummonWithAvaricePrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "The original SummonWithAvarice battle is incomplete or has native/model failures.");

        FixtureValue definition = root.GetProperty("OriginalSummonWithAvariceDefinition");
        Require(definition.GetProperty("OwnerId").GetString() == RelicId &&
            definition.GetProperty("AssetKey").GetString() == RelicKey &&
            definition.GetProperty("Effect").GetString() == Effect &&
            definition.GetProperty("TargetTeam").GetString() == "Monsters" &&
            definition.GetProperty("RequiredSubtype").GetString() == "SubtypesData_None" &&
            definition.GetProperty("ExcludedSubtypeCount").GetInt32() == 0 &&
            !definition.GetProperty("RestrictToRoom").GetBoolean() &&
            definition.GetProperty("RestrictedRoom").GetInt32() == 0 &&
            definition.GetProperty("HpPercentAsStacks").GetInt32() == 0 &&
            definition.GetProperty("SourceKind").GetString() == string.Empty &&
            !definition.GetProperty("RequireGraft").GetBoolean() &&
            definition.GetProperty("ConditionCount").GetInt32() == 0 &&
            definition.GetProperty("CharacterFilterCount").GetInt32() == 0 &&
            definition.GetProperty("StatusId").GetString() == "avarice" &&
            definition.GetProperty("StatusStacks").GetInt32() == 2,
            "The native SummonWithAvarice owner or its spawn payload changed.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length > 0, "The native Avarice fixture has no captured battle decisions.");
        CombatContext context = actions[0].GetProperty("Before").GetProperty("Spawn").GetProperty("Train")
            .GetProperty("Context").Deserialize<CombatContext>()!;
        CombatRelicState relic = context.Relics!.Single(item => item.DataId == RelicId);
        RelicSpawnStatus rule = relic.SpawnStatuses!.Single();
        Require(relic.AssetKey == RelicKey && relic.EffectTypes.SequenceEqual([Effect]) &&
            rule.EffectIndex == 0 && rule.TargetPlayer && !rule.TargetEnemy && rule.SubtypeIsNone &&
            rule.Statuses.Count == 1 && rule.Statuses[0].Id == "avarice" && rule.Statuses[0].Stacks == 2 &&
            RelicModel.Validate([relic]) == null,
            "The immutable native owner payload did not retain its exact two-stack Monster-target rule.");

        int birthWitnesses = 0;
        foreach (FixtureValue record in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "A native Avarice birth observation was incomplete or mismatched.");
            RoomCombatState before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = record.GetProperty("After").Deserialize<RoomCombatState>()!;
            int unitId = record.GetProperty("UnitId").GetInt32();
            RoomCombatResult predicted = RelicSpawnStatusModel.CharacterAdded(before, unitId,
                record.GetProperty("FromCardId").GetInt32(), record.GetProperty("OnlyCovenants").GetBoolean());
            Require(predicted.Supported && Serialize(predicted.State) == Serialize(actual),
                "Independent replay of a native Avarice spawn did not match the completed birth.");
            CombatUnit old = before.Units.Single(unit => unit.Id == unitId);
            CombatUnit next = actual.Units.Single(unit => unit.Id == unitId);
            if (old.Team == CombatTeam.Enemy)
                Require(next.Status("avarice")?.Stacks == old.Status("avarice")?.Stacks,
                    "The Monster-target Avarice relic changed a native Heroes spawn.");
            if (old.Team == CombatTeam.Player && !old.IsPyre && old.Status("immune") == null && old.Status("purify") == null &&
                old.Status("untouchable") == null && !old.StatusImmunities.Contains("avarice") &&
                next.Status("avarice")?.Stacks == (old.Status("avarice")?.Stacks ?? 0) + 2)
                birthWitnesses++;
        }
        Require(birthWitnesses > 0, "Native battle has no eligible player birth receiving exactly two Avarice stacks.");

        int goldStages = 0;
        foreach (FixtureValue stage in root.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            RoomCombatResult predicted = stage.GetProperty("Kind").GetString() == "Exchange"
                ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before);
            Require(predicted.Supported && Serialize(predicted.State) == Serialize(actual),
                "Independent Avarice battle-stage replay differs: " + predicted.UnsupportedReason);
            bool carriesAvarice = before.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Status("avarice")?.Stacks > 0);
            if (!carriesAvarice) continue;
            RoomCombatState withoutAvarice = StripAvarice(before);
            RoomCombatResult ablated = stage.GetProperty("Kind").GetString() == "Exchange"
                ? RoomCombatModel.Exchange(withoutAvarice) : RoomCombatModel.Resolve(withoutAvarice);
            Require(ablated.Supported, "Avarice ablation replay is unsupported: " + ablated.UnsupportedReason);
            int contributedGold = predicted.State!.Context!.Gold - ablated.State!.Context!.Gold;
            if (contributedGold <= 0) continue;
            Require(actual.Context!.Gold - ablated.State.Context!.Gold == contributedGold,
                "Native gold movement does not contain the exact Avarice-only contribution.");
            goldStages++;
        }
        Require(goldStages > 0, "Native battle has no positive direct-attack or ability-damage Avarice gold witness.");
        BattleActionChecks.Native(root);
        Console.WriteLine($"NATIVE-SUMMON-WITH-AVARICE PASS: exact owner, {birthWitnesses} two-stack player births, {goldStages} native combat stages with isolated positive gold contribution and zero battle differences.");
    }

    private static CombatRelicState Relic(RelicSpawnStatus? rule = null) => new(RelicId, RelicKey, [Effect],
        [rule ?? new RelicSpawnStatus(0, true, false, [new CombatStatus("avarice", 2)])],
        isCovenant: false, disallowedInPlacementPhase: false);

    private static RoomCombatState Room(CombatUnit[] units, UnityRng rng, int gold,
        CardInstanceState[]? cards = null, CombatRelicState[]? relics = null, bool preview = false,
        float goldRewardMultiplier = 1f)
    {
        var resources = new BattleResourceState(3, 0, 5, 10, 10, "pyre", 20, 80, 0, 0,
            [], [], [], goldRewardMultiplier);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, gold, 20, 10,
            cardInstances: cards, cardRegistry: cards, relics: relics ?? [],
            energyState: new(10, 0, 0, "MonsterTurn", true), resources: resources);
        return new RoomCombatState(0, false, units, [], context, preview: preview);
    }

    private static CombatUnit Unit(int id, CombatTeam team, int attack, int health, CombatStatus[] statuses) =>
        new(id, "avarice-check-" + id, team, attack, health, health, true, false, false, statuses,
            modifiers: new(attack, 0, 0, 1, 1, true, false, []), statusRegistry: statuses,
            statusDictionary: new([], []));

    private static CardInstanceState DamageCard(string id, bool unitAbility)
    {
        var definition = new CardMaskDefinition(id, "Spell", "Common", false, false, false,
            [], [], [], [], null, false, 0, 0, unitAbility, false, false);
        var descriptor = new CardMaskDescriptor(definition, 0, [], []);
        return new CardInstanceState(unitAbility ? 12 : 11, id, CardModifiers.Empty(), CardModifiers.Empty(),
            0, 0, 0, [], maskDescriptor: descriptor);
    }

    private static RoomCombatState StripAvarice(RoomCombatState source) => new(source.RoomIndex, source.Deployment,
        source.Units.Select(unit =>
        {
            CombatStatus[] statuses = unit.Statuses.Where(status => status.Id != "avarice").ToArray();
            CombatStatus[]? registry = unit.StatusRegistry?.Where(status => status.Id != "avarice").ToArray();
            StatusDictionaryState? dictionary = unit.StatusDictionary;
            if (dictionary != null)
            {
                int slot = dictionary.Slots.ToList().IndexOf("avarice");
                if (slot >= 0)
                {
                    string?[] slots = dictionary.Slots.ToArray(); slots[slot] = null;
                    dictionary = new StatusDictionaryState(slots, new[] { slot }.Concat(dictionary.FreeSlots).ToArray());
                }
            }
            return new CombatUnit(unit.Id, unit.AssetKey, unit.Team, unit.BaseAttack, unit.Health, unit.MaxHealth,
                unit.CanAttack, unit.IsPyre, unit.EndsBattleOnDeath, statuses, unit.Triggers, unit.SpawnerCardId,
                unit.Size, unit.StatusImmunities, unit.Subtypes, unit.Modifiers, unit.IsBoss, unit.LastAttackerId,
                registry, unit.EquipmentCards, unit.NextTriggerId, unit.Ability, dictionary, unit.AbilityRules,
                unit.HordeDefinition, unit.IsSpawning, unit.SacrificeCardId, unit.DeathState, bumpRules: unit.BumpRules);
        }).ToArray(), source.ExternalInteractions, source.Context, source.Preview, source.ActiveCompanionBossIds);

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
