using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OuterBossPyreAttachmentChecks
{
    private const string Reason = "Native Pyre trigger registry differs after rolling input52";
    private const string Upgrade = "05e215c6-600c-4ecb-a75e-c806dd596bcf";
    private const string Attachment = "08001aa4-2e9d-42da-9ba7-ff490ed5ce80";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source, true);
        if (replay != null)
        {
            Check(replay.Value, false);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("OUTER-BOSS-PYRE-ATTACHMENT PASS:52 complete native inputs/forecasts/RNG; direct tower movement retains BoilerRoom upgrade and death trigger through defeat;" +
            (replay == null ? " independent replay pending." : " corrected independent native pair."));
    }

    private static void Check(FixtureValue fixture, bool historical)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, historical ? HistoricalMismatch : null,
            boundaryOutcome: RoomOutcome.PlayerDefeated);
        Require(rows.Length == 52 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing original52-input EndTurn prefix.");
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 10 && after.Spawn.Turn == 11 &&
            Room(before) == 2 && Room(after) == 3 && Boss(before).Health == 513 && Boss(after).Health == 368 &&
            Boss(before).AssetKey == "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth" &&
            HasAttachment(Boss(before)) && HasAttachment(Boss(after)) &&
            after.PlayRules!.Rooms.Count(room => !room.IsPyre && !room.Enabled) == 3 &&
            after.Spawn.Train.Context!.EnergyState!.PyreAlive == false,
            "Missing original direct boss movement, retained upgrade/callback, or terminal defeat.");
        foreach (string field in new[] { "RoomAttachmentsBefore", "RoomAttachmentsAfter" })
        {
            FixtureValue floor = rows[^1].GetProperty(field).EnumerateArray().Single(room => room.GetProperty("RoomIndex").GetInt32() == 2);
            Require(!floor.GetProperty("Updating").GetBoolean() && !floor.GetProperty("PreviewMode").GetBoolean() &&
                floor.GetProperty("Attachments").EnumerateArray().Single().GetProperty("Impacted").EnumerateArray().Any(id => id.GetInt32() == 8),
                "Native departure attachment lost the moved boss registration.");
        }
        FixtureValue[] updates = fixture.GetProperty("RoomAttachmentMovement").EnumerateArray().ToArray();
        Require(updates.Length > 0 && updates.Any(update => update.GetProperty("State").EnumerateArray().Any(room =>
                room.GetProperty("PreviewMode").GetBoolean() && room.GetProperty("Heroes").EnumerateArray().Any(unit =>
                    unit.GetProperty("Id").GetInt32() == 8 && unit.GetProperty("RoomIndex").GetInt32() == 3))) &&
            updates.All(update => update.GetProperty("State").EnumerateArray().All(room =>
                room.GetProperty("PreviewMode").GetBoolean() || room.GetProperty("Heroes").EnumerateArray().All(unit =>
                    unit.GetProperty("Id").GetInt32() != 8 || unit.GetProperty("RoomIndex").GetInt32() != 3))),
            "Expected attachment updates before real boss movement, followed only by tower previews.");
    }

    private static bool HasAttachment(CombatUnit unit) => unit.Modifiers!.Upgrades.Any(upgrade => upgrade.DataId == Upgrade) &&
        unit.Triggers.Count(trigger => trigger.Kind == "OnDeath" && trigger.StateId == 2 && trigger.IgnoreSilence &&
            trigger.Origin?.UpgradeId == Attachment) == 1;
    private static void HistoricalMismatch(BattleTurnState historical, BattleTurnState actual) =>
        Require(!Boss(historical).Modifiers!.Upgrades.Any(upgrade => upgrade.DataId == Upgrade) &&
            !Boss(historical).Triggers.Any(trigger => trigger.Origin?.UpgradeId == Attachment) && HasAttachment(Boss(actual)),
            "Historical early attachment removal was replaced.");
    private static CombatUnit Boss(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 8);
    private static int Room(BattleTurnState state) => state.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == 8)).RoomIndex;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
