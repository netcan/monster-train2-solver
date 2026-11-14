using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DamageReactionBattleChecks
{
    private const string OwnerId = "5253721f-c51c-4148-9958-d45550f08316";
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "damage-reaction-battle" &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop" &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing versioned Bloodshed/Punish battle terminal.");
        foreach (string prefix in new[] { "", "OriginalEatenDamage" })
            foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
                Require(root.GetProperty(prefix + counter).GetInt32() == 0, "Incomplete damage battle: " + prefix + counter);
        FixtureValue evidence = root.GetProperty("DamageReactionBattle");
        int[] cards = evidence.GetProperty("ControlCardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("OwnerId").GetString() == OwnerId &&
            evidence.GetProperty("CompletedControlPlays").GetInt32() == 7 && cards.Length == 7 && cards.Distinct().Count() == 7 &&
            actions.Length >= 7 && turns.Length > 1 && actions.Take(7).Select(row => row.GetProperty("Action")
                .GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards), "Native damage controls were not played.");
        CombatRelicState owner = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Relics!
            .Single(relic => relic.DataId == OwnerId);
        RelicResourceEffect rule = owner.ResourceEffects!.Single();
        Require(evidence.GetProperty("OwnerRuntimeEffectTypes").EnumerateArray().Select(item => item.GetString())
            .SequenceEqual(new[] { RelicFriendlyTriggerDamageModel.Effect }) &&
            !evidence.GetProperty("PiercingEffectTypes").EnumerateArray().Any(item => item.GetString() == RelicFriendlyTriggerDamageModel.Effect),
            "Native runtime effect/balance piercing list differs.");
        Require(owner.AssetKey == "DamageFriendlyOnSpawn" && owner.EffectTypes.SequenceEqual(new[] { RelicFriendlyTriggerDamageModel.Effect }) &&
            rule.EffectIndex == 0 && rule.Amount == 5 && rule.ParamInt == 5 && rule.ParamInt2 == 0 && rule.Conditions.Count == 0 &&
            rule.CanApplyInPreviewMode && rule.UnitDamage?.TargetMode == "FrontInRoom" &&
            rule.UnitTrigger is { Trigger: "OnPunish", Team: "Monsters", Subtype: null } trigger && trigger.RequiredStatuses.Count == 0,
            "Original Punish retaliation owner definition differs.");
        int[] hp = { 12, 12, 5, 12, 11, 5, 12 };
        for (int index = 0; index < 7; index++)
        {
            int carrier = index == 1 || index >= 5 ? cards[1] : cards[0];
            CombatUnit actor = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms[0].Units
                .Single(unit => unit.SpawnerCardId == carrier);
            Require(actor.Health == hp[index] && actor.MaxHealth == 12, "Missing exact native wound/heal control at " + index);
        }
        FixtureValue[] transitions = evidence.GetProperty("Transitions").EnumerateArray().ToArray();
        foreach (FixtureValue row in transitions)
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("After").ValueKind == FixtureKind.Object &&
                row.GetProperty("Before").GetProperty("Id").GetInt32() == row.GetProperty("Dispatch").GetProperty("ActorId").GetInt32(),
                "Incomplete damage trigger observation.");
            if (row.GetProperty("Preview").GetBoolean()) Require(GoldDelta(row) == 0, "Preview callback changed gold.");
        }
        foreach (bool preview in new[] { false, true })
        {
            foreach (string kind in new[] { "OnBloodshed", "OnPunish", "OnHit" })
                Require(transitions.Any(row => row.GetProperty("Preview").GetBoolean() == preview && Kind(row) == kind &&
                    row.GetProperty("Before").Deserialize<CombatUnit>()!.Triggers.Any(trigger => trigger.Kind == kind && !trigger.HasTriggered) &&
                    row.GetProperty("After").Deserialize<CombatUnit>()!.Triggers.Any(trigger => trigger.Kind == kind && trigger.HasTriggered)),
                    "Missing first executed " + kind + " source transition in " + preview);
            FixtureValue[] rows = transitions.Where(row => row.GetProperty("Preview").GetBoolean() == preview &&
                cards.Take(2).Contains(row.GetProperty("Before").GetProperty("SpawnerCardId").GetInt32())).ToArray();
            foreach (var entry in new[] { ("OnBloodshed", 5), ("OnHit", 10), ("OnPunish", 15) })
                Require(rows.Any(row => Kind(row) == entry.Item1 && GoldDelta(row) == (preview ? 0 : entry.Item2)),
                    "Missing executed " + entry.Item1 + " in " + (preview ? "preview" : "real battle"));
            Require(rows.Any(row => Kind(row) == "OnBloodshed" && row.GetProperty("Dispatch").GetProperty("OverrideTargetId").GetInt32() !=
                row.GetProperty("Dispatch").GetProperty("ActorId").GetInt32()), "Missing same-room Bloodshed bystander in " + preview);
            Require(rows.Any(row => Kind(row) == "OnBloodshed" && row.GetProperty("Dispatch").GetProperty("ParamInt2").GetInt32() == 4),
                "Missing natural attack Bloodshed in " + preview);
            foreach (string kind in new[] { "OnBloodshed", "OnPunish" })
                Require(rows.Any(row => Kind(row) == kind && row.GetProperty("Before").GetProperty("Health").GetInt32() == 0),
                    "Missing retained zero-HP " + kind + " actor in " + preview);
        }
        Require(transitions.Any(row => !row.GetProperty("Preview").GetBoolean() && Kind(row) == "OnReanimated" &&
            row.GetProperty("After").GetProperty("Health").GetInt32() > 0), "Natural Undying revival missing.");
        Require(transitions.Any(row => !row.GetProperty("Preview").GetBoolean() && Kind(row) == "OnDeath" &&
            row.GetProperty("Before").GetProperty("Health").GetInt32() == 0), "Natural final death missing.");
        int positives = 0, wrongTriggers = 0, enemyControls = 0, emptyControls = 0;
        bool versionGuardChecked = false;
        foreach (FixtureValue row in root.GetProperty("RelicEatenDamageTests").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            string kind = row.GetProperty("Trigger").GetString()!;
            int ownerIndex = before.Context!.Relics!.Select((relic, index) => (relic, index)).Single(item => item.relic.DataId == OwnerId).index;
            RelicCardPlayDamageSelection selected = RelicFriendlyTriggerDamageModel.Select(before, actor, kind,
                row.GetProperty("RoomIndex").GetInt32(), ownerIndex, row.GetProperty("EffectIndex").GetInt32(), row.GetProperty("ParamInt").GetInt32());
            if (!versionGuardChecked)
            {
                TrainCombatState unversioned = CardSpellModel.WithContext(before, before.Context.WithStatistics(null));
                Require(!RelicFriendlyTriggerDamageModel.Select(unversioned, actor, kind,
                    row.GetProperty("RoomIndex").GetInt32(), ownerIndex, row.GetProperty("EffectIndex").GetInt32()).Supported,
                    "Unversioned Punish owner was accepted."); versionGuardChecked = true;
            }
            Require(selected.Supported && row.GetProperty("Result").GetBoolean() == (selected.Targets.Count > 0), "Relic trigger eligibility differs.");
            Equal(selected.State, row.GetProperty("Actual").Deserialize<TrainCombatState>()!, "relic test");
            if (selected.Targets.Count > 0)
            {
                positives++;
                Require(selected.Targets.SequenceEqual(row.GetProperty("CachedTargets").EnumerateArray().Select(item => item.GetInt32())),
                    "Original Punish front target differs.");
            }
            else if (kind != "OnPunish") wrongTriggers++;
            else if (actor.Team == CombatTeam.Enemy) enemyControls++;
            else emptyControls++;
        }
        Require(positives > 0 && wrongTriggers > 0 && enemyControls > 0 && emptyControls > 0, "Missing relic positive/trigger/team/empty controls.");
        int applications = 0, armorHits = 0, healthHits = 0;
        foreach (FixtureValue row in root.GetProperty("RelicEatenDamageApplications").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            int[] targets = row.GetProperty("Targets").EnumerateArray().Select(item => item.GetInt32()).ToArray();
            TrainCombatResult result = RelicFriendlyTriggerDamageModel.ApplySelected(before, targets, deferPhysicalRemoval: true);
            Require(result.Supported && targets.Length == 1 && row.GetProperty("Completed").GetBoolean(), "Incomplete native relic damage.");
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            Equal(result.State, actual, "relic damage"); applications++;
            CombatUnit victim = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == targets[0]);
            CombatUnit? after = actual.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == targets[0]);
            int incoming = NativeRetaliationAmount(victim);
            if (victim.Status("armor")?.Stacks >= incoming)
            {
                Require(after != null && after.Health == victim.Health && (after.Status("armor")?.Stacks ?? 0) == victim.Status("armor")!.Stacks - incoming,
                    "The 2.3.1 original relic must consume armor before health."); armorHits++;
            }
            if (after == null || after.Health < victim.Health) healthHits++;
        }
        Require(applications == positives && armorHits > 0 && healthHits > 0, "Missing relic positive/armor/health applications.");
        foreach (bool preview in new[] { false, true })
        {
            int observedArmor = 0, observedHealth = 0;
            foreach (FixtureValue row in evidence.GetProperty("Applications").EnumerateArray().Where(row => row.GetProperty("Preview").GetBoolean() == preview))
            {
                CombatUnit[] before = row.GetProperty("Before").EnumerateArray().Select(item => item.Deserialize<CombatUnit>()!).ToArray();
                CombatUnit[] after = row.GetProperty("After").EnumerateArray().Select(item => item.Deserialize<CombatUnit>()!).ToArray();
                Require(row.GetProperty("Completed").GetBoolean() && before.Length == 1 && after.Length == 1 && before[0].Id == after[0].Id,
                    "Missing direct native retaliation target observation.");
                int armorBefore = before[0].Status("armor")?.Stacks ?? 0, armorAfter = after[0].Status("armor")?.Stacks ?? 0;
                int incoming = NativeRetaliationAmount(before[0]), spent = Math.Min(incoming, armorBefore);
                Require(armorAfter == armorBefore - spent && after[0].Health == Math.Max(0, before[0].Health - (incoming - spent)),
                    "Native retaliation armor/health damage differs in " + preview);
                if (spent > 0) observedArmor++;
                if (after[0].Health < before[0].Health) observedHealth++;
            }
            Require(observedArmor > 0 && observedHealth > 0, "Missing direct retaliation armor/health effects in " + preview);
        }
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        Console.WriteLine($"DAMAGE-REACTION-BATTLE PASS: seven controls, {transitions.Length} callbacks ({transitions.Count(row => row.GetProperty("Preview").GetBoolean())} preview), natural Bloodshed/Punish, Undying/final death and unchanged Punish relic ({applications} applications), complete native phases/RNG and 16 immutable branches.");
    }
    private static string? Kind(FixtureValue row) => row.GetProperty("Dispatch").GetProperty("Kind").GetString();
    private static int GoldDelta(FixtureValue row) => row.GetProperty("GoldAfter").GetInt32() - row.GetProperty("GoldBefore").GetInt32();
    private static int NativeRetaliationAmount(CombatUnit target) => 5 +
        (target.Status("pyregel") is CombatStatus gel ? gel.Stacks * gel.ParamInt : 0);
    private static void Equal(object? expected, object actual, string label) => Require(
        JsonSerializer.Serialize(expected, ModelJson.Options) == JsonSerializer.Serialize(actual, ModelJson.Options), label + " complete successor differs.");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
