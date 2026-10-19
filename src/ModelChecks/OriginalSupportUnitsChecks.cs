using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSupportUnitsChecks
{
    private static readonly string[] Originals = ["4aa37169-c5cf-4a5b-b927-dc31b05f6e24", "82e21764-56cb-4e41-882f-ab3dd0c1dcbc",
        "c8b428ac-a1e9-4e52-96e0-5eed522195a2", "0c68b12a-0779-4c3c-bf64-6bc5bcf83e9f", "8c404be7-7179-461c-8e7d-b5fdb296174c",
        "d76be2d0-f851-4d43-bdc0-7c7a7779724f", "600ef4bf-59ec-45b6-a3e7-273cbb43c7bb", "85bddcfb-8e7a-4797-a209-c7d62f40ecc7",
        "61d9f4f1-80f5-416b-bf22-1836925c8ccd", "5a8399dd-a958-4317-b814-de0e2972ca3d", "a736e605-a4f4-4cc1-8eba-63cb0cf3515e",
        "64a3fd58-b7b3-466a-be8d-78bb2b9bdea4", "d653e83e-3f70-419d-b3f9-4b920d5c9e56"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int room = index < 3 ? index : index == 5 || index is >= 14 and <= 18 ? 1 : index is 6 or 19 or 20 or 28 ? 2 : 0;
        int position = index < 3 ? 0 : index is >= 8 and <= 13 ? index - 7 : index is >= 14 and <= 18 ? index - 13 : index is 19 or 20 ? index - 18 : -1;
        int host = index is 21 or 22 ? 11 : index is 23 or 24 ? 12 : index == 25 ? 9 : index == 26 ? 8 : index == 27 ? 10 : 19;
        int target = index >= 21 ? source.Spawn.Train.Rooms.SelectMany(floor => floor.Units).Single(unit => unit.SpawnerCardId == ids[host]).Id : 0;
        var action = new PlayCardAction(ids[index], room, playerPosition: position, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalSupportUnitsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original support unit witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalSupportUnitsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 29 && actions.Length == 29, "Missing thirteen original births or positive trigger controls.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Actor(BattleTurnState state, int index) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[index]);
        static int Stacks(CombatUnit unit, string id) => unit.Status(id)?.Stacks ?? 0;
        for (int i = 8; i <= 20; i++)
        {
            BattleTurnState before = Before(i), after = After(i);
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 8]);
            CombatUnit unit = Actor(after, i), template = rule.SpawnUnit!;
            Require(before.Spawn.Train.Context!.FindCard(ids[i])!.DataId == rule.DataId && rule.ExternalInteractions.Count == 0 &&
                before.Energy - after.Energy == rule.Cost - (i == 18 ? 4 : 0) && unit.Modifiers!.AttackDamage == template.Modifiers!.AttackDamage && unit.Size == template.Size,
                "Original support unit identity, paid cost, raw attack or size differs.");
            if (i is 11 or 12) Require(unit.Health == 25 && unit.MaxHealth == 85, "Hollow birth must add sixty maximum HP without healing current HP.");
        }
        Require(Stacks(Actor(After(16), 1), "armor") - Stacks(Actor(Before(16), 1), "armor") == 20,
            "Welder Helper must positively armor the existing front ally.");
        Require(Before(17).Spawn.Train.Rooms[1].Units.Where(unit => unit.Team == CombatTeam.Player).All(unit =>
            Stacks(After(17).Spawn.Train.Rooms[1].Units.Single(changed => changed.Id == unit.Id), "buff") - Stacks(unit, "buff") == 5),
            "Aggressive Imp must positively give the room five Rage.");
        foreach (int i in new[] { 16, 17, 18 })
            Require(Before(i).Spawn.Train.Rooms[1].Units.Where(unit => unit.Team == CombatTeam.Player).All(unit =>
                Stacks(After(i).Spawn.Train.Rooms[1].Units.Single(changed => changed.Id == unit.Id), "regen") - Stacks(unit, "regen") == 1 &&
                After(i).Spawn.Train.Rooms[1].Units.Single(changed => changed.Id == unit.Id).MaxHealth - unit.MaxHealth == 1),
                "Amorous Enoki must positively Rally with Regen and maximum HP on three later original units.");
        int starterImpDamage = Before(13).PlayRules!.Cards.Single(rule => rule.DataId == Originals[5]).SpawnUnit!.Triggers
            .Single(trigger => trigger.Kind == "OnSpawn").Effects.Single(effect => effect.Type == "CardEffectDamage").Value;
        CombatUnit starterImpVictimBefore = Before(13).Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
        CombatUnit starterImpVictimAfter = After(13).Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == starterImpVictimBefore.Id);
        Require(starterImpDamage > 0 && starterImpVictimBefore.Health == starterImpVictimAfter.Health &&
            Stacks(starterImpVictimBefore, "armor") - Stacks(starterImpVictimAfter, "armor") == starterImpDamage,
            "Starter Imp must positively route its captured spawn damage through front-unit armor.");
        CardCycleState drawBefore = Before(20).Spawn.Train.Context!.Cards, drawAfter = After(20).Spawn.Train.Context!.Cards;
        CombatEffect typedDraw = Before(20).PlayRules!.Cards.Single(rule => rule.DataId == Originals[12]).SpawnUnit!.Triggers
            .SelectMany(trigger => trigger.Effects).Single(effect => effect.Action?.Type == "DrawType");
        Require(typedDraw.Action!.TypedDraw?.CardType == "Equipment" && typedDraw.Action.Value == 1 &&
            drawBefore.Hand.Any(card => card.InstanceId == ids[28]) && !drawBefore.Draw.Concat(drawBefore.Discard).Any(card => card.InstanceId == ids[28]) &&
            drawAfter.Hand.Any(card => card.InstanceId == ids[28]) && !drawAfter.Draw.Concat(drawAfter.Discard).Any(card => card.InstanceId == ids[28]),
            "Swordmaiden's captured equipment draw must leave unmatched hand equipment untouched when no eligible card is in deck or discard.");
        Require(Actor(After(22), 11).Health - Actor(Before(22), 11).Health == 10 && Actor(After(22), 13).Modifiers!.AttackDamage - Actor(Before(22), 13).Modifiers!.AttackDamage == 3 &&
            Actor(After(22), 13).MaxHealth - Actor(Before(22), 13).MaxHealth == 3, "Awoken Hollow healing must buff the weakest current-HP ally by +3/+3.");
        Require(Actor(After(24), 12).Health - Actor(Before(24), 12).Health == 10 && Stacks(Actor(After(24), 12), "spikes") - Stacks(Actor(Before(24), 12), "spikes") == 4,
            "Thorned Hollow must positively gain four Spikes when healed.");
        Require(Actor(Before(25), 9).Health - Actor(After(25), 9).Health == 1 && Actor(After(25), 9).Modifiers!.AttackDamage - Actor(Before(25), 9).Modifiers!.AttackDamage == 3,
            "Husk Hermit must gain three raw attack from the selected hit.");
        Require(Actor(After(26), 8).Modifiers!.AttackDamage - Actor(Before(26), 8).Modifiers!.AttackDamage == 15,
            "Alpha Fiend must gain fifteen attack before its native forced attack.");
        CombatUnit railVictimBefore = Before(27).Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack"),
            railVictimAfter = After(27).Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == railVictimBefore.Id);
        Require(railVictimBefore.Health - railVictimAfter.Health == 10 && Stacks(railVictimBefore, "melee weakness") == 0 &&
            Stacks(railVictimAfter, "melee weakness") == 1,
            "Railbeater must land its native ten-damage hit and leave one Melee Weakness on the victim.");
        BattleTurnState equipmentBefore = Before(28), equipmentAfter = After(28);
        CardPlayRule equipment = equipmentBefore.PlayRules!.Cards.Single(rule => rule.DataId == "f1c2a098-e20d-4587-b8de-5bef315de42d");
        Require(Actor(equipmentAfter, 19).Modifiers!.AttackDamage - Actor(equipmentBefore, 19).Modifiers!.AttackDamage == equipment.Equipment!.Upgrades.Sum(upgrade => upgrade.Stats.Damage) + 3 &&
            Actor(equipmentAfter, 19).MaxHealth - Actor(equipmentBefore, 19).MaxHealth == 3 &&
            equipmentAfter.Spawn.Train.Context!.FindCard(ids[19])!.Permanent.Upgrades.Count - equipmentBefore.Spawn.Train.Context!.FindCard(ids[19])!.Permanent.Upgrades.Count == 1 &&
            Stacks(Actor(equipmentAfter, 20), "valor") - Stacks(Actor(equipmentBefore, 20), "valor") == 8,
            "Equipment must positively give Pincushion permanent +3/+3 and Swordmaiden eight Valor.");
        var phase = root.GetProperty("TrainPhases").EnumerateArray().First(item => item.GetProperty("Turn").GetInt32() == 1 && item.GetProperty("Kind").GetString() == "Combat");
        TrainCombatState combatBefore = phase.GetProperty("Before").Deserialize<TrainCombatState>()!, combatAfter = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        Require(combatAfter.Rooms[1].Units.Where(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId != ids[1]).All(unit =>
            Stacks(unit, "armor") - Stacks(combatBefore.Rooms[1].Units.Single(prior => prior.Id == unit.Id), "armor") == 6), "Steelworker needs positive postcombat room armor.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-SUPPORT-UNITS PASS: thirteen unchanged units, positive spawn/rally/heal/hit/attack/equipment/postcombat callbacks, native no-match typed-draw control and 16 immutable full chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
