using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalPunishedSkillsChecks
{
    private const string Grim = "7d98ab2a-97a2-4e47-b836-dfc6deed50d8";
    private const string Chain = "a7e84160-5e7b-4fd7-9227-7bf8f766454c";
    private const string Berserker = "1c98fd78-22ee-4227-a550-de4398fe427f";
    private const string GrimAbility = "334705d9-ce34-40d5-bcc0-708edb246484";
    private const string ChainAbility = "5c8950a8-a409-4bcf-9c67-1c6c1e130e9b";
    private const string Flight = "dc3337da-96a6-469b-88fc-561c588dc3bb";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static int Status(CombatUnit unit, string id) => unit.Status(id)?.Stacks ?? 0;
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("OriginalPunishedSkillsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Punished skills native battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definitions = root.GetProperty("OriginalPunishedSkillsDefinitions").EnumerateArray().ToArray();
        string[] originals = { Grim, Chain, Berserker, GrimAbility, ChainAbility, Flight }; int[] costs = { 1, 1, 2, 0, 0, 0 };
        Require(definitions.Length == 6, "Missing three original units and three original skills.");
        var rules = definitions.Select(row => row.GetProperty("Rule").Deserialize<CardPlayRule>()!).ToArray();
        for (int i = 0; i < 6; i++) Require(definitions[i].GetProperty("Id").GetString() == originals[i] &&
            definitions[i].GetProperty("Cost").GetInt32() == costs[i] && rules[i].ExternalInteractions.Count == 0,
            "Original identity/cost/projection changed at " + i);
        int[] attack = { 60, 20, 20 }, health = { 25, 30, 20 };
        for (int i = 0; i < 3; i++) Require(rules[i].SpawnUnit is CombatUnit unit && unit.BaseAttack == attack[i] &&
            unit.Health == health[i] && unit.MaxHealth == health[i] && unit.Size == 2 && unit.AbilityRules?.ReplacementSkipsCooldown == true, "Original unit stats changed.");
        var birth = rules[2].SpawnUnit!.Triggers.Single();
        Require(birth.Kind == "OnSpawn" && birth.Effects.Single() is { Type: DevotionStatusModel.NativeEffect,
            Action: { Type: DevotionStatusModel.Effect, Target: "Self", DevotionLevelsPerGrant: 2 } } &&
            birth.Effects.Single().Action!.Statuses.Single() is { Id: "multistrike", Stacks: 1 }, "Original two-level Devotion birth rule changed.");
        Require(Status(rules[0].SpawnUnit!, "restricted") == 1 && Status(rules[0].SpawnUnit!, "sweep") == 1 &&
            rules[0].SpawnUnit!.Ability is { DataId: GrimAbility, Cooldown: 4 } &&
            rules[1].SpawnUnit!.Ability is { DataId: ChainAbility, Cooldown: 3 } &&
            rules[3].Effects.Count == 3 && rules[3].Effects[0] is { Type: "Heal", Target: "Self", Value: 9999 } &&
            rules[3].Effects[1] is { Type: "RemoveStatus", Target: "Self" } &&
            rules[3].Effects[2].AbilityChange!.Definition is { DataId: Flight, Cooldown: 2 }, "Original restriction, skill effects or cooldown changed.");
        int[] ids = root.GetProperty("OriginalPunishedSkillsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray(); int[] skills = { 5, 8, 12, 15, 25 };
        Require(actions.Length == 26 && ids.Length == 26 && ids.Count(id => id == 0) == 5 &&
            ids.Where(id => id > 0).Distinct().Count() == 21, "Missing ordered native inputs/five skill uses.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int card) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.SpawnerCardId == ids[card]);
        int Room(BattleTurnState state, int card) => state.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.SpawnerCardId == ids[card])).RoomIndex;
        for (int i = 0; i < 26; i++)
        {
            var action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (!skills.Contains(i)) Require(action.CardInstanceId == ids[i], "Control order differs at " + i);
            else Require(action.ActivatorUnitId == Unit(Before(i), i is 5 or 25 ? 2 : 1).Id &&
                Before(i).Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == (i is 5 or 25 ? ChainAbility : i == 8 ? GrimAbility : Flight),
                "Original skill cache or source differs at " + i);
        }
        Require(Before(7).Spawn.Turn == 2 && Unit(Before(7), 1).Health == 25 && Status(Unit(Before(7), 1), "restricted") == 1 &&
            Status(Unit(Before(7), 2), "cooldown") == 2, "Missing restricted natural combat and natural skill cooldown progression.");
        Require(Status(Unit(Before(7), 1), "cooldown") == 3 && Unit(After(7), 1).Health == 10 &&
            Status(Unit(After(7), 1), "cooldown") == 0 && Unit(After(8), 1).Health == 25 && Status(Unit(After(8), 1), "restricted") == 0 &&
            Status(Unit(After(8), 1), "cooldown") == 0 &&
            Unit(After(8), 1).Ability is { DataId: Flight, Cooldown: 2, ReplacedWhileResolving: false }, "Original first skill did not heal, clear restriction and replace itself.");
        foreach (int i in new[] { 9, 13, 14 }) Require(Unit(After(i), 1).Health == Unit(Before(i), 1).Health - 1 &&
            Status(Unit(After(i), 1), "cooldown") == Math.Max(0, Status(Unit(Before(i), 1), "cooldown") - 1), "OnHit cooldown reduction differs at " + i);
        Require(Unit(After(10), 1).Health == Unit(Before(10), 1).Health &&
            Status(Unit(After(10), 1), "cooldown") == Status(Unit(Before(10), 1), "cooldown"), "Zero damage incorrectly reduced cooldown.");
        Require(Room(Before(12), 1) == 0 && Room(After(12), 1) == 1 && Room(Before(15), 1) == 1 && Room(After(15), 1) == 0 &&
            Status(Unit(After(12), 1), "cooldown") == 2 && Status(Unit(After(13), 1), "cooldown") == 1 &&
            Status(Unit(After(14), 1), "cooldown") == 0 && Status(Unit(After(15), 1), "cooldown") == 2,
            "Replaced Flight must move both ways, reactivate after two hits and restart cooldown.");
        foreach (int i in new[] { 5, 25 })
        {
            var enemies = Before(i).Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
            Require(enemies.Length >= 2 && Status(Unit(After(i), 2), "cooldown") == 3, "Original chained skill needs live room targets and cooldown.");
            foreach (var enemy in enemies)
            {
                var next = After(i).Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == enemy.Id);
                Require(Status(next, "chained") == Status(enemy, "chained") + 2 && next.Health == enemy.Health,
                    "Original ChainBearer ability must add two Chained to every room enemy without direct damage.");
            }
        }
        var guardAfterSkill = After(5).Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
        var guardAfterCombat = Before(7).Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == guardAfterSkill.Id);
        Require(Status(guardAfterCombat, "chained") == Status(guardAfterSkill, "chained") + 1 &&
            guardAfterCombat.Health == guardAfterSkill.Health &&
            Unit(Before(7), 2).Triggers.Any(trigger => trigger.Kind == "OnAttacking" && trigger.HasTriggered),
            "Natural ChainBearer attack must add two Chained before its one-stack end-turn reduction.");
        var resets = root.GetProperty("AbilityCooldownEffects").EnumerateArray().ToArray();
        Require(resets.Length == 5 && resets.All(row => row.GetProperty("Completed").GetBoolean() &&
            row.GetProperty("TriggerKind").GetString() == "OnOwnAbilityActivated"), "Missing five native own-ability cooldown effects.");
        int replacements = 0;
        foreach (var row in resets)
        {
            var old = row.GetProperty("TargetActors").Deserialize<CombatUnit[]>()!.Single();
            var next = row.GetProperty("AfterTargetActors").Deserialize<CombatUnit[]>()!.Single();
            Require(!next.Ability!.ReplacedWhileResolving, "Native reset did not consume the replacement marker.");
            if (old.Ability!.ReplacedWhileResolving)
            {
                replacements++;
                Require(old.Ability.DataId == Flight && Status(old, "cooldown") == 0 && Status(next, "cooldown") == 0,
                    "Replacement must skip just its next after-activation cooldown reset.");
            }
            else Require(Status(next, "cooldown") == next.Ability.Cooldown, "Ordinary skill did not reset its cooldown.");
        }
        Require(replacements == 1, "Missing the unique in-flight original skill replacement.");
        AbilityCooldownChecks.Native(root);
        int[] births = { 4, 17, 19, 21, 23 };
        for (int level = 0; level < 5; level++)
        {
            int i = births[level]; Require(Before(i).Spawn.Train.Context!.QueryFrame!.Devotion!.Level == level &&
                Before(i).Energy - After(i).Energy == 2 && Status(Unit(After(i), i), "multistrike") == level / 2 &&
                Status(Unit(After(23), i), "multistrike") == level / 2, "Devotion birth divisor or nonretroactive multistrike differs at level " + level);
        }
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        Console.WriteLine("ORIGINAL-PUNISHED-SKILLS PASS: three unchanged units/three unchanged skills, restricted natural combat, full heal/replacement, two-way Flight and hit/zero cooldown controls, two chained room skills/natural attack, five native cooldown effects with one consumed replacement marker, five births at Devotion 0..4, full chain/16 branches and zero-unsupported serial/parallel search.");
    }
}
