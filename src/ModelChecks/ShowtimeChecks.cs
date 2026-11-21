using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ShowtimeChecks
{
    internal static void Native(string path)
    {
        using var archive = ModelJson.ReadFixture(path);
        var root = archive.RootElement;
        var definition = root.GetProperty("OriginalShowtimeDefinition");
        string? attachmentId = RelicShowtimeModel.AttachmentId(definition.GetProperty("Id").GetString()!);
        Require(root.GetProperty("OriginalShowtimePrepared").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            definition.ContentEquals(root.GetProperty("OriginalShowtimeDefinitionAfter")) &&
            attachmentId != null && definition.GetProperty("RoomUpgradeId").GetString() == attachmentId,
            "Missing unchanged original Showtime owner.");

        var witness = root.GetProperty("OriginalShowtimeWitness");
        int selected = Int(witness, "Room");
        int oldCapacity = Int(witness, "OriginalCapacity"), capacity = Int(witness, "ExpandedCapacity");
        int front = Int(witness, "FrontCardId"), rear = Int(witness, "RearCardId");
        int occupied = Int(witness, "FrontSize") + Int(witness, "RearSize");
        Require(occupied > oldCapacity && occupied < capacity && capacity - oldCapacity == RelicShowtimeModel.AttachmentCapacity(attachmentId!),
            "Deployment pair did not depend on the original hidden room capacity.");
        var opening = root.GetProperty("OriginalShowtimeOpeningCallbacks").EnumerateArray().ToArray();
        var apply = opening.Single(record => record.GetProperty("Kind").GetString() == "Apply");
        var before = apply.GetProperty("Before");
        var after = apply.GetProperty("After");
        Require(opening.All(record => record.GetProperty("Completed").GetBoolean()) && Int(before, "Turn") == 0 &&
            Int(after, "SelectedRoom") == selected && !before.GetProperty("BattleRng").ContentEquals(after.GetProperty("BattleRng")) &&
            after.GetProperty("Floors").EnumerateArray().Single(floor => Int(floor, "Index") == selected)
                .GetProperty("Attachments").EnumerateArray().Any(attachment => Int(attachment, "Type") == RelicShowtimeModel.HiddenAttachment),
            "Native shuffle and hidden attachment did not settle on the recorded floor.");

        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        var decisions = actions.Take(2).Select(row => row.GetProperty("Action")).ToArray();
        Require(decisions.Length == 2 && Int(decisions[0], "CardInstanceId") == front && Int(decisions[1], "CardInstanceId") == rear &&
            decisions.All(action => Int(action, "RoomIndex") == selected), "Recorded deployment did not use both Showtime controls.");
        int gains = 0;
        foreach (var callback in root.GetProperty("OriginalShowtimePostCombat").EnumerateArray())
        {
            Require(callback.GetProperty("Completed").GetBoolean(), "Native Showtime PostCombat is incomplete.");
            var prior = callback.GetProperty("Before");
            var next = callback.GetProperty("After");
            if (Int(prior, "Turn") <= 0 || Int(prior, "Room") != selected) continue;
            var priorActors = prior.GetProperty("Actors").EnumerateArray().ToArray();
            var nextActors = next.GetProperty("Actors").EnumerateArray().ToArray();
            if (!priorActors.Any(actor => Int(actor, "CardId") == front) || !priorActors.Any(actor => Int(actor, "CardId") == rear) ||
                !nextActors.Any(actor => Int(actor, "CardId") == front) || !nextActors.Any(actor => Int(actor, "CardId") == rear)) continue;
            int Upgrades(FixtureValue[] frame, int id) => Int(frame.Single(actor => Int(actor, "CardId") == id), "UpgradeCount");
            Require(Upgrades(priorActors, rear) == Upgrades(nextActors, rear), "Showtime incorrectly upgraded the rear control.");
            if (Upgrades(nextActors, front) == Upgrades(priorActors, front) + 1) gains++;
        }
        Require(gains >= 2, "Missing repeated native front-only Showtime upgrades.");
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path);
        Console.WriteLine($"SHOWTIME PASS tier={Int(definition, "Tier")} room={selected} capacity={oldCapacity}->{capacity} frontUpgrades={gains}.");
    }

    private static int Int(FixtureValue value, string key) => value.GetProperty(key).GetInt32();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}
