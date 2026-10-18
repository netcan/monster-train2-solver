using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalReactiveUnitsChecks
{
    private static readonly string[] Originals = ["fec68cb5-1d4f-4ae5-b01a-288a06cc9502", "107ecf70-99f8-47d1-b1e1-8627fff3f574",
        "49cfd4db-f1f1-4c3d-ab08-380e3fc0f6a6", "3d20e7fd-0c1f-478e-8845-4a11e27baca5", "545a9444-3773-4867-bfa1-e6032646552f",
        "4b7a0257-6f1b-4dde-8c97-b5ca5bd06fc7", "5259f045-a38e-4ff8-b6aa-c4efd942058f", "d4262a26-c3d6-4792-90f7-9ed5a1b93eb7",
        "b5b205da-0178-4859-b7d8-3a8d75cad180", "ecb7cd49-a997-43b6-9820-bf3c46db29d2"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int room = index < 3 ? index : index == 5 || index is >= 11 and <= 15 || index >= 22 ? 1 : 0;
        int position = index < 3 ? 0 : index == 6 ? 0 : index is >= 7 and <= 10 ? index - 5 : index is >= 11 and <= 15 ? index - 10 : -1;
        int target = index == 16 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[6]).Id :
            index is >= 17 and <= 20 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[index - 10]).Id : 0;
        var action = new PlayCardAction(ids[index], room, playerPosition: position, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalReactiveUnitsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original reactive unit witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalReactiveUnitsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 24 && actions.Length == 24, "Missing ten original unit births or positive trigger controls.");
        CombatUnit Actor(BattleTurnState state, int cardIndex) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[cardIndex]);
        static int Stacks(CombatUnit unit, string id) => unit.Status(id)?.Stacks ?? 0;
        for (int i = 6; i < 16; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 6]);
            CombatUnit template = rule.SpawnUnit!, unit = Actor(after, i);
            Require(before.Spawn.Train.Context!.FindCard(ids[i])!.DataId == rule.DataId && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == rule.Cost &&
                unit.AssetKey == template.AssetKey && unit.BaseAttack == template.BaseAttack && unit.Health == template.Health && unit.MaxHealth == template.MaxHealth && unit.Size == template.Size,
                "Original reactive unit definition, paid cost or birth stats differ.");
        }
        for (int i = 16; i < 24; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            if (i == 16)
            {
                CombatUnit enemyBefore = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack"), enemyAfter = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == enemyBefore.Id);
                Require(action.TargetUnitId == Actor(before, 6).Id && Actor(before, 6).Health - Actor(after, 6).Health == 2 && Actor(after, 6).LastAttackerId == enemyAfter.Id && Stacks(enemyAfter, "decay") - Stacks(enemyBefore, "decay") == 5,
                    "Prickly Puffball must receive enemy-attributed Spikes and apply five Reap to that exact last attacker.");
            }
            if (i is >= 17 and <= 20)
            {
                CombatUnit a = Actor(before, i - 10), b = Actor(after, i - 10);
                Require(a.Health - b.Health == 1, "A selected native OnHit control did not deal one damage.");
                if (i == 17) Require(after.Energy - before.Energy == 1, "Wilting Sapwood must positively restore one energy.");
                if (i == 18) Require(Stacks(b, "burst") - Stacks(a, "burst") == 1, "Knuckler Steward must gain one Burst.");
                if (i == 19) Require(Stacks(b, "valor") - Stacks(a, "valor") == 2, "Firebrand must gain two Valor.");
                if (i == 20) Require(before.Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).All(unit =>
                    Stacks(after.Spawn.Train.Rooms[0].Units.Single(changed => changed.Id == unit.Id), "poison") - Stacks(unit, "poison") == 3), "Runebound Protector must apply three Frostbite to both native enemies.");
            }
            if (i >= 22)
            {
                CombatUnit a = Actor(before, 11), b = Actor(after, 11);
                Require(b.Modifiers!.AttackDamage - a.Modifiers!.AttackDamage == 3 && b.Health - a.Health == 3 && b.MaxHealth - a.MaxHealth == 3 &&
                    after.Spawn.Train.Context!.FindCard(ids[11])!.Temporary.Upgrades.Count - before.Spawn.Train.Context!.FindCard(ids[11])!.Temporary.Upgrades.Count == 1,
                    "Combative Siren must gain and record +3/+3 on each Incant.");
                Require(Stacks(Actor(after, 12), "buff") - Stacks(Actor(before, 12), "buff") == 3, "Enlightened Siren must gain three Rage per Incant.");
                Require(before.Spawn.Train.Rooms[1].Units.Where(unit => unit.Team == CombatTeam.Enemy).Any() &&
                    before.Spawn.Train.Rooms[1].Units.Where(unit => unit.Team == CombatTeam.Enemy).All(unit =>
                        Stacks(after.Spawn.Train.Rooms[1].Units.Single(changed => changed.Id == unit.Id), "pyregel") - Stacks(unit, "pyregel") == 1), "Magma Cultist needs positive Pyregel on native enemies.");
                Require(before.Spawn.Train.Rooms[1].Units.Where(unit => unit.Team == CombatTeam.Player).All(unit =>
                    Stacks(after.Spawn.Train.Rooms[1].Units.Single(changed => changed.Id == unit.Id), "armor") - Stacks(unit, "armor") == (unit.SpawnerCardId == ids[14] ? 4 : 1)),
                    "Runebound Guardian and Healing Totem must add exact self/room armor per Incant.");
                Require(Json(before.Spawn.Train.Rooms[0].Units) == Json(after.Spawn.Train.Rooms[0].Units), "Incant leaked into the neighboring floor.");
            }
        }
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-REACTIVE-UNITS PASS: ten unchanged units, positive last-attacker Reap, OnHit energy/Burst/Valor/Frostbite, repeated enemy-present Incant upgrades/Rage/Pyregel/armor and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
