using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRearrangeSpellsChecks
{
    private static readonly string[] Originals = ["4a9b228e-d3d2-4de8-8cc5-f66fe20af14b", "1450e56b-ca09-453a-937a-97bd44bcb3de", "5ae81798-157e-4fcb-b4e6-822ff40843b0",
        "ac87d3d4-c542-4950-878a-50cbc9a45919", "aaaddead-a999-40ae-8327-a6f977645c04", "1185bf27-1e2a-478a-af93-500a864ebad4",
        "37f4e51b-3771-4a9a-aaae-910c3062ba64", "1737fa0b-7f37-41e8-9183-ed9e6a391556", "392dce9e-ad56-4834-82cc-053705610145",
        "d35bfe1d-d05e-403a-b6ab-38ce30d3e2ea", "11c9415e-3316-4e86-8a38-fe61f7e43cbd", "11c9415e-3316-4e86-8a38-fe61f7e43cbd"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int target = index is >= 6 and <= 10 || index == 16 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id :
            index == 17 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "MageT1_Junker").Id :
            index is 11 or 13 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[1]).Id :
            index is 12 or 14 or 15 ? source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
        var action = new PlayCardAction(ids[index], index == 2 || index == 15 ? 1 : 0, playerPosition: index < 3 ? (index == 1 ? 1 : 0) : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalRearrangeSpellsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original rearrange spell witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalRearrangeSpellsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 19 && actions.Length == 19, "Missing eleven originals, repeated X and positive health/resource/first-target controls.");
        for (int i = 6; i < 18; i++)
        {
            BattleTurnState before = actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!, after = actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var context = before.Spawn.Train.Context!; var changed = after.Spawn.Train.Context!;
            var card = context.FindCard(ids[i])!; var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 6]);
            Require(card.DataId == rule.DataId && rule.ExternalInteractions.Count == 0 && before.Energy - after.Energy == (i >= 16 ? before.Energy : rule.Cost),
                "Original rearrange spell identity, cost or projection differs.");
            CombatUnit[] a = before.Spawn.Train.Rooms.SelectMany(room => room.Units).OrderBy(unit => unit.Id).ToArray(),
                b = after.Spawn.Train.Rooms.SelectMany(room => room.Units).OrderBy(unit => unit.Id).ToArray();
            Require(a.Select(unit => unit.Id).SequenceEqual(b.Select(unit => unit.Id)), "Positive rearrange controls must preserve every live actor.");
            var action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatUnit actor = b.Single(unit => unit.Id == action.TargetUnitId), prior = a.Single(unit => unit.Id == actor.Id);
            int[] Order(int floor, CombatTeam team) => changed.SpawnPoints!.Group(floor, team)!.Occupants.Where(id => id != 0).ToArray();
            int floor = changed.SpawnPoints!.Units.Single(unit => unit.UnitId == actor.Id).Current!.RoomIndex;
            bool back = i is 6 or 8 or 10;
            Require((back ? Order(floor, actor.Team).Last() : Order(floor, actor.Team).First()) == actor.Id, "Original selected front/back physical ordering differs at " + i);
            int Effective(CombatUnit unit) => unit.Health + (unit.Status("armor")?.Stacks ?? 0);
            int enemyLoss = a.Where(unit => unit.Team == CombatTeam.Enemy).Sum(Effective) - b.Where(unit => unit.Team == CombatTeam.Enemy).Sum(Effective);
            Require(enemyLoss == (i switch { 7 => 3, 8 => 13, 9 => -12, 13 => 65, 15 => 1000, _ => 0 }), "Exact damage/armor contribution differs at " + i);
            if (i == 6) Require(actor.Status("dazed")?.Stacks == 1, "Horrify must add one Dazed after pushing.");
            if (i == 9) Require((actor.Status("armor")?.Stacks ?? 0) - (prior.Status("armor")?.Stacks ?? 0) == 12, "March of Shields armor differs.");
            if (i == 10) Require(actor.Status("dazed")?.Stacks == 3 && context.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 3 &&
                changed.RoomCapacities!.Single(room => room.RoomIndex == 0).Corruption == 0, "Soulcrushing Guilt must spend three Corruption and add two Dazed.");
            if (i == 11) Require(actor.Status("damage shield")?.Stacks == 2, "Selfless Sacrifice must add two shields after pulling.");
            if (i == 12) Require(actor.Attack == 67 && actor.Status("lifesteal")?.Stacks == 1, "It's Alive must pull, grant Lifesteal and upgrade attack by seven.");
            if (i == 13) Require(actor.Attack == 65 && actor.Status("valor")?.Stacks == 5, "Eagleheart must pull, grant five Valor and attack immediately.");
            if (i == 14) Require(floor == 1 && Order(1, CombatTeam.Player).Length == 2, "Rising Rage must ascend then pull ahead of the existing ally.");
            if (i == 15) Require(floor == 0 && actor.Health == 500 && rule.Effects![2].DamagePerHpIncludeArmor, "Heroic Dive must descend, pull then deal twice 500 HP plus armor.");
            if (i >= 16) Require((actor.Status("melee weakness")?.Stacks ?? 0) - (prior.Status("melee weakness")?.Stacks ?? 0) == before.Energy &&
                (i == 16 ? before.Energy > 0 : before.Energy == 0), "Positive/zero X melee weakness must scale from spent energy.");
            foreach (CombatUnit ally in a.Where(unit => unit.Team == CombatTeam.Player))
                Require(b.Single(unit => unit.Id == ally.Id).Health == ally.Health, "Rearrange chain damaged an allied control.");
            if (i == 9) Require(context.BattleRng.Next().Equals(changed.BattleRng), "March of Shields chance-100 must consume one RNG draw.");
            else Require(context.BattleRng.Equals(changed.BattleRng), "Fixed rearrange chain unexpectedly consumed Battle RNG.");
            bool consume = CardExhaustModel.HasConsume(card);
            Require(consume == (i is 8 or 11 or 13 or 15) && (consume ? changed.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == ids[i]) :
                changed.Cards.Discard.Any(token => token.InstanceId == ids[i])), "Original rearrange Consume/discard route differs.");
        }
        BattleTurnState firstBefore = actions[18].GetProperty("Before").Deserialize<BattleTurnState>()!, firstAfter = actions[18].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var firstPoints = firstBefore.Spawn.Train.Context!.SpawnPoints!; var lastPoints = firstAfter.Spawn.Train.Context!.SpawnPoints!;
        int firstEnemy = firstPoints.Group(0, CombatTeam.Enemy)!.Occupants.First(id => id != 0);
        Require(lastPoints.Group(0, CombatTeam.Enemy)!.Occupants.Last(id => id != 0) == firstEnemy &&
            firstPoints.Group(0, CombatTeam.Player)!.Occupants.SequenceEqual(lastPoints.Group(0, CombatTeam.Player)!.Occupants),
            "Both-team front collection must rearrange only its first enemy; the collected friendly remains in place.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-REARRANGE-SPELLS PASS: eleven unchanged originals/twelve plays, damage then last-target reorder, armor RNG, Corruption, shield/lifesteal/upgrade, immediate Valor attack, ascension/HP-damage descent and positive/zero X; 16 full immutable chains.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
