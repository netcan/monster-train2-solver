using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSelfAttackChecks
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        int pending = Enumerable.Range(0, 3).Where(i => context.Statistics!.Value(ids[i], "TimesPlayed", "ThisBattle") == 0).DefaultIfEmpty(-1).First();
        PlayCardAction action;
        if (pending >= 0) action = new PlayCardAction(ids[pending], pending == 1 ? 1 : 0, playerPosition: pending == 2 ? 1 : 0);
        else
        {
            if (source.Spawn.Turn != 1) return null;
            CombatUnit? actor = source.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.SpawnerCardId == ids[2]);
            if (actor == null) return null;
            AbilityCardResult cached = AbilityCardModel.Get(context, actor.Ability!.CardCreation!);
            if (!cached.Supported) return null;
            int played = context.Statistics!.CardsPlayedThisTurn.Count(id => id == cached.Card!.InstanceId);
            if (played >= 4) return null;
            int control = played > 0 && context.Statistics.Value(ids[played + 2], "TimesPlayed", "ThisBattle") == 0 ? played + 2 :
                played == 3 && context.Statistics.Value(ids[6], "TimesPlayed", "ThisBattle") == 0 ? 6 : -1;
            CombatUnit back = source.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "MageT1_Junker");
            action = control >= 0 ? new PlayCardAction(ids[control], 0, targetUnitId: control == 6 ? back.Id : actor.Id) :
                new PlayCardAction(cached.Card.InstanceId, 0, targetUnitId: back.Id, activatorUnitId: actor.Id);
        }
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalSelfAttackPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original self-attack witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalSelfAttackIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 7 && actions.Length == 11, "Missing original unit, four ability activations or status controls.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Actor(BattleTurnState state) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[2]);
        CombatUnit Back(BattleTurnState state) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "MageT1_Junker");
        CombatUnit Front(BattleTurnState state) => state.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
        CardPlayRule unit = Before(2).PlayRules!.Cards.Single(rule => rule.DataId == "d632e3f2-c5cf-49d0-aa05-0c79331f6c85");
        Require(unit.Cost == 1 && unit.ExternalInteractions.Count == 0 && unit.SpawnUnit!.BaseAttack == 35 && unit.SpawnUnit.MaxHealth == 8 &&
            unit.SpawnUnit.Ability!.Cooldown == 2 && unit.SpawnUnit.Ability.CooldownAtSpawn == 0, "Original Animus unit or ability cooldown differs.");
        foreach (int i in new[] { 3, 5, 7, 10 })
        {
            BattleTurnState before = Before(i), after = After(i); PlayCardAction action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext context = before.Spawn.Train.Context!, actual = after.Spawn.Train.Context!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == "c2055882-a6f3-4b74-81f4-53f9350e51a0");
            Require(rule.Cost == 0 && rule.Effect == "Spell" && rule.ExternalInteractions.Count == 0 && rule.Effects.Count == 1 &&
                rule.Effects[0].Type == "AttackTargetsWithSelfUnit" && rule.Effects[0].Target == "DropTargetCharacter" &&
                !rule.Effects[0].AllowPlayer && rule.Effects[0].AllowEnemy && !rule.Effects[0].RequireEquipment &&
                action.ActivatorUnitId == Actor(before).Id && action.TargetUnitId == Back(before).Id,
                "Original self-attack definition, actor or selected back target differs.");
            Require(after.Energy == before.Energy && Actor(after).Status("cooldown")?.Stacks == 2 &&
                actual.Statistics!.CardsPlayedThisTurn.Count(id => id == action.CardInstanceId) == context.Statistics!.CardsPlayedThisTurn.Count(id => id == action.CardInstanceId) + 1 &&
                actual.Statistics.Value(action.CardInstanceId, "TimesPlayed", "ThisBattle") == context.Statistics.Value(action.CardInstanceId, "TimesPlayed", "ThisBattle") &&
                actual.FindCard(action.CardInstanceId)!.PlayCount == 0 && Json(context.Cards.Hand) == Json(actual.Cards.Hand),
                "Cached targeted ability cost, cooldown, played history or ownership differs.");
            Require(Json(before.Spawn.Train.Rooms[1].Units) == Json(after.Spawn.Train.Rooms[1].Units), "Self attack changed a neighbor floor.");
            string parent = Json(before);
            foreach (int invalid in new[] { 0, Actor(before).Id, before.Spawn.Train.Rooms[1].Units.Single().Id })
                Require(BattleActionModel.PlayCard(before, new PlayCardAction(action.CardInstanceId, 0, targetUnitId: invalid,
                    activatorUnitId: action.ActivatorUnitId)).Rejection == ActionRejection.Illegal, "Invalid targeted ability was accepted.");
            Require(UnitAbilityModel.EnumerateSupportedActivations(before).Any(candidate => candidate.TargetUnitId == action.TargetUnitId &&
                candidate.ActivatorUnitId == action.ActivatorUnitId), "Targeted ability is absent from search enumeration.");
            Require(Json(before) == parent, "Targeted ability validation/enumeration mutated the parent.");
        }
        Require(Back(After(3)).Health == Back(Before(3)).Health - 35 && Json(Front(After(3))) == Json(Front(Before(3))),
            "Selected back attack must bypass the front enemy.");
        Require(Actor(Before(5)).Status("dazed")?.Stacks == 1 && Actor(After(5)).Status("dazed")?.Stacks == 1 &&
            Json(Before(5).Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy)) ==
            Json(After(5).Spawn.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy)), "Dazed must prevent this ability's attack and remain until the turn's removal stage.");
        Require(Actor(Before(7)).Status("dazed") == null && Back(After(7)).Health == Back(Before(7)).Health - 35 &&
            Front(Before(7)).Health + (Front(Before(7)).Status("armor")?.Stacks ?? 0) - Front(After(7)).Health - (Front(After(7)).Status("armor")?.Stacks ?? 0) == 35 &&
            Actor(After(7)).Health == Actor(Before(7)).Health - 2, "Sweep must attack both enemies and take front-target Spikes retaliation.");
        Require(Actor(Before(10)).Status("sweep") == null && Actor(Before(10)).Attacks == 2 && Back(Before(10)).Health == 5 &&
            After(10).Spawn.Train.Rooms[0].Units.All(unit => unit.Id != Back(Before(10)).Id) && Front(After(10)).Health == Front(Before(10)).Health - 35,
            "Lethal first strike must prune the selected target and retarget the remaining front enemy.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-SELF-ATTACK PASS: unchanged Animus/ability, selected back enemy, Dazed prevention, Sweep, lethal Multistrike fallback, cost/cooldown/history, target legality/enumeration and 16 immutable full chains.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
