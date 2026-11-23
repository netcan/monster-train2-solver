using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyreAbilityCooldownChecks
{
    private const string PyreId = "0ff61363-ab58-4873-9a26-18db51eacd46";
    private const string AbilityId = "4dbfc527-50b2-4068-aa36-9770d4444a57";

    internal static void Native(FixtureValue positive, FixtureValue negative)
    {
        (TrainCombatState train, RoomCombatState room, CombatUnit pyre) = Read(positive, true);
        (TrainCombatState control, _, CombatUnit baseline) = Read(negative, false);
        CombatUnit beforeBirth = AbilityCooldownModel.Copy(pyre, pyre.Ability!.WithCooldown(baseline.Ability!.Cooldown));
        RoomCombatState beforeRoom = room.WithUnits(room.Units.Select(unit => unit.Id == pyre.Id ? beforeBirth : unit).ToArray());
        var before = new TrainCombatState(train.Rooms.Select(frame => frame.RoomIndex == room.RoomIndex ? beforeRoom : frame).ToArray(),
            train.Movement, train.EnemySlotsPerRoom, train.Context);
        using var keys = new BattleStateKeys();
        string key = keys.SnapshotKey(before);
        RelicBirthResult modeled = RelicBirthModel.CharacterAdded(before, pyre.Id, 0, false, true, []);
        Require(modeled.Supported && modeled.Queued.Count == 0 && modeled.Dispatched.Count == 0,
            "Original Pyre birth did not settle: " + modeled.UnsupportedReason);
        CombatUnit result = modeled.State!.Rooms.Single(frame => frame.RoomIndex == room.RoomIndex).Units.Single(unit => unit.Id == pyre.Id);
        Require(JsonSerializer.Serialize(result, ModelJson.Options) == JsonSerializer.Serialize(pyre, ModelJson.Options) &&
            modeled.State.Context!.BattleRng.Equals(before.Context!.BattleRng) && keys.SnapshotKey(before) == key,
            "The cooldown mutator must change only the original Pyre's after-activation interval.");

        RelicBirthResult unchanged = RelicBirthModel.CharacterAdded(control, baseline.Id, 0, false, true, []);
        Require(unchanged.Supported && unchanged.State!.Rooms.SelectMany(frame => frame.Units).Single(unit => unit.Id == baseline.Id)
            .Ability?.Cooldown == baseline.Ability.Cooldown, "Control birth changed cooldown without the native owner.");
        Console.WriteLine("NATIVE-PYRE-ABILITY-COOLDOWN PASS: original callback 99 to 2, control stays 99, initial and remaining cooldown stay zero.");
    }

    internal static void NativeReuse(FixtureValue positive, FixtureValue negative)
    {
        var differences = new List<string>();
        CheckReuse(positive, withOwner: true, differences);
        CheckReuse(negative, withOwner: false, differences);
        Require(differences.Count == 0, string.Join(Environment.NewLine, differences));
        Console.WriteLine("NATIVE-PYRE-ABILITY-COOLDOWN-REUSE PASS: two zero-cost casts with cooldown 2 to 1 to 0 to 2, " +
            "one zero-cost control cast with ability removal, complete cast and EndTurn successors.");
    }

    private static void CheckReuse(FixtureValue root, bool withOwner, List<string> differences)
    {
        string label = withOwner ? "Time Pyre with owner" : "Time Pyre control";
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == "OriginalPyreAbilityCooldownReuse" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("OwnerId").GetString() == RelicPyreAbilityCooldownModel.OwnerId &&
            root.GetProperty("HasOwner").GetBoolean() == withOwner &&
            root.GetProperty("PyreId").GetString() == PyreId && root.GetProperty("AbilityId").GetString() == AbilityId,
            label + " did not use the pinned original configuration.");
        FixtureValue nativeBefore = root.GetProperty("BirthCallbackBefore"), nativeAfter = root.GetProperty("BirthCallbackAfter");
        Require(withOwner ? IsSample(nativeBefore, 99) && IsSample(nativeAfter, 2) :
            nativeBefore.ValueKind == FixtureKind.Null && nativeAfter.ValueKind == FixtureKind.Null,
            label + " did not preserve the appropriate original birth callback.");

        int unitId = root.GetProperty("UnitId").GetInt32(), interval = withOwner ? 2 : 99;
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue[] casts = root.GetProperty("Casts").EnumerateArray().ToArray();
        Require(turns.Length == (withOwner ? 2 : 1) && casts.Length == turns.Length,
            label + " recorded the wrong number of turns or casts.");
        BattleTurnState next = Snapshot(turns[0], 0, interval, 0, "DeploymentPhase");
        for (int index = 0; index < casts.Length; index++)
        {
            FixtureValue cast = casts[index];
            FixtureValue before = cast.GetProperty("Before"), after = cast.GetProperty("After");
            int turn = index * 2 + 1;
            BattleTurnState original = Snapshot(before, turn, interval, 0, "CanActivate");
            BattleTurnState actual = Snapshot(after, turn, withOwner ? interval : 0, withOwner ? 2 : 0,
                withOwner ? "Cooldown" : "NoAbility");
            PlayCardAction action = cast.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardPlayRule definition = original.PlayRules!.Cards.Single(rule => rule.DataId == AbilityId);
            Require(action.ActivatorUnitId == unitId && action.TargetUnitId == 0 && action.RoomIndex == original.SelectedRoom &&
                original.Spawn.Train.Context!.AbilityCardCache!.Any(card => card.InstanceId == action.CardInstanceId && card.DataId == AbilityId) &&
                definition.Cost == 0 && actual.Energy == original.Energy,
                label + " did not record the original zero-cost paid input.");
            FixtureValue callback = cast.GetProperty("RemoveAbility");
            CardActionEffect effect = callback.GetProperty("Effect").Deserialize<CardActionEffect>()!;
            Require(callback.GetProperty("Completed").GetBoolean() && callback.GetProperty("HasRelicGate").GetBoolean() &&
                callback.GetProperty("SourceCardId").GetInt32() == action.CardInstanceId &&
                callback.GetProperty("Targets").EnumerateArray().Single().GetInt32() == unitId &&
                effect.Type == "RemoveAbility" && effect.AbilityChange is { Permanent: false } change && change.BlockedByRelic == withOwner,
                label + " recorded the wrong RemoveAbility callback or owner gate.");
            int[] priorHand = HandIds(before, original), playedHand = HandIds(after, actual);
            Require(priorHand.Order().SequenceEqual(playedHand.Order()) && after.GetProperty("Hand").EnumerateArray()
                .Any(card => card.GetProperty("FreezeTraits").GetInt32() > 0), label + " did not retain a hand with frozen cards.");

            EndTurn(next, before, "before cast " + (index + 1));
            BattleActionResult result = BattleActionModel.PlayCard(original, action);
            Compare(result.State, result.Outcome, result.Reason, after, "after cast " + (index + 1));
            next = actual;
            if (index + 1 < casts.Length)
            {
                EndTurn(next, turns[index + 1], "first cooldown decrement");
                next = Snapshot(turns[index + 1], turn + 1, interval, 1, "Cooldown");
            }
        }

        BattleTurnState Snapshot(FixtureValue snapshot, int turn, int cooldown, int remaining, string availability)
        {
            BattleTurnState state = snapshot.GetProperty("Decision").Deserialize<BattleTurnState>()!;
            CombatUnit unit = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(actor => actor.Id == unitId);
            FixtureValue sample = snapshot.GetProperty("Cooldown");
            string? abilityId = cooldown == 0 ? null : AbilityId;
            Require(state.Spawn.Turn == turn && state.CanonicalDecisionReferences && state.CanonicalPhysicalReferences &&
                unit.IsPyre && unit.SpawnerCardId == 0 && unit.Ability?.DataId == abilityId &&
                (unit.Ability?.Cooldown ?? 0) == cooldown && (unit.Ability?.CooldownAtSpawn ?? 0) == 0 &&
                (unit.Status("cooldown")?.Stacks ?? 0) == remaining && sample.GetProperty("AfterActivation").GetInt32() == cooldown &&
                sample.GetProperty("AtSpawn").GetInt32() == 0 && sample.GetProperty("Remaining").GetInt32() == remaining &&
                snapshot.GetProperty("AbilityId").GetString() == abilityId && snapshot.GetProperty("Availability").GetString() == availability &&
                !snapshot.GetProperty("DisabledPermanently").GetBoolean() &&
                !(state.Spawn.Train.Context!.PermanentlyDisabledAbilities?.Contains(AbilityId) ?? false) &&
                (state.Spawn.Train.Context.Relics?.Any(RelicPyreAbilityCooldownModel.IsOriginal) ?? false) == withOwner,
                label + " recorded the wrong full state at turn " + turn + " (" + availability + ").");
            return state;
        }

        int[] HandIds(FixtureValue snapshot, BattleTurnState decision)
        {
            int[] ids = snapshot.GetProperty("Hand").EnumerateArray().Select(card => card.GetProperty("Id").GetInt32()).ToArray();
            Require(ids.SequenceEqual(decision.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId)),
                label + " hand metadata disagrees with the captured decision.");
            return ids;
        }

        void EndTurn(BattleTurnState before, FixtureValue after, string step)
        {
            BattleTurnResult outcome = BattleTurnModel.EndTurn(before);
            Compare(outcome.State, outcome.Outcome, outcome.UnsupportedReason, after, step);
        }

        void Compare(BattleTurnState? state, RoomOutcome outcome, string? reason, FixtureValue after, string step)
        {
            string boundary = label + " " + step;
            if (state == null || outcome != RoomOutcome.Exchanged)
            {
                differences.Add(boundary + ": " + (reason ?? outcome.ToString()));
                return;
            }
            try { RecordedBattleChecks.RecordedBoundary(state, after.GetProperty("Decision"), terminal: false, boundary); }
            catch (InvalidOperationException error) { differences.Add(error.Message); }
        }
    }

    private static (TrainCombatState Train, RoomCombatState Room, CombatUnit Pyre) Read(FixtureValue root, bool withOwner)
    {
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == "OriginalPyreAbilityCooldownBirth" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version && root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("OwnerId").GetString() == RelicPyreAbilityCooldownModel.OwnerId && root.GetProperty("OwnerKey").GetString() == RelicPyreAbilityCooldownModel.OwnerKey &&
            root.GetProperty("Effect").GetString() == RelicPyreAbilityCooldownModel.Effect && root.GetProperty("ParamInt").GetInt32() == 2 &&
            root.GetProperty("ParamInt2").GetInt32() == 2 && root.GetProperty("HasOwner").GetBoolean() == withOwner &&
            root.GetProperty("PyreId").GetString() == PyreId && root.GetProperty("AbilityId").GetString() == AbilityId,
            "Expected original Time Pyre, cooldown owner and callback parameters.");
        FixtureValue before = root.GetProperty("CallbackBefore"), after = root.GetProperty("CallbackAfter");
        Require(withOwner ? IsSample(before, 99) && IsSample(after, 2) : before.ValueKind == FixtureKind.Null && after.ValueKind == FixtureKind.Null,
            "Expected the original birth callback only with the owner.");

        int cooldown = withOwner ? 2 : 99;
        var decision = root.GetProperty("Decision").Deserialize<BattleTurnState>()!;
        TrainCombatState train = decision.Spawn.Train;
        int id = root.GetProperty("UnitId").GetInt32();
        RoomCombatState room = train.Rooms.Single(frame => frame.Units.Any(unit => unit.Id == id));
        CombatUnit pyre = room.Units.Single(unit => unit.Id == id);
        Require(IsSample(root.GetProperty("Settled"), cooldown) && decision.Spawn.Turn == 0 && pyre.IsPyre && pyre.SpawnerCardId == 0 &&
            pyre.Ability is { CooldownAtSpawn: 0 } ability && ability.DataId == AbilityId && ability.Cooldown == cooldown &&
            (pyre.Status("cooldown")?.Stacks ?? 0) == 0 &&
            train.Context!.Relics?.Any(owner => owner.EffectTypes.Contains(RelicPyreAbilityCooldownModel.Effect) &&
                RelicPyreAbilityCooldownModel.IsOriginal(owner)) == withOwner,
            "Settled deployment did not preserve the original Pyre ability boundary.");
        return (train, room, pyre);
    }

    private static bool IsSample(FixtureValue value, int cooldown) => value.ValueKind == FixtureKind.Object &&
        value.GetProperty("AfterActivation").GetInt32() == cooldown && value.GetProperty("AtSpawn").GetInt32() == 0 &&
        value.GetProperty("Remaining").GetInt32() == 0;

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
