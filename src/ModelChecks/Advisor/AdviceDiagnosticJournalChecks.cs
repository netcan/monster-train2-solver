using MonsterTrain2Poju.Probe;
using Newtonsoft.Json.Linq;

internal static class AdviceDiagnosticJournalChecks
{
    internal static void Run()
    {
        string root = Directory.CreateTempSubdirectory("poju-advice-history-").FullName;
        try
        {
            InputTargets();
            AcceptedOperations(Path.Combine(root, "accepted"));
            Rotation(Path.Combine(root, "rotation"));
            Overflow(Path.Combine(root, "overflow"));
            Failure(Path.Combine(root, "not-a-directory"));
            Console.WriteLine("Advice history: canonical input targets, bounded rotation, UTF-8 JSON, queue overflow, background drain and I/O failure passed.");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void AcceptedOperations(string path)
    {
        using var journal = new AdviceDiagnosticJournal(path);
        journal.Record(new AdviceDiagnosticEntry { Kind = "card-accepted" });
        journal.Record(new AdviceDiagnosticEntry
        {
            Kind = "devote-accepted",
            Input = new AdviceDiagnosticInput { Operation = "Devote", TargetRoom = 2 }
        });
        journal.Record(new AdviceDiagnosticEntry { Kind = "end-turn-accepted" });
        journal.Dispose();
        Wait(() => journal.Completed);
        Require(journal.CardInputs == 1 && journal.DevoteInputs == 1 && journal.Written == 3 && journal.Dropped == 0,
            "Devote changed the separate card count or was missed");
        var records = File.ReadAllLines(Path.Combine(path, "advice.jsonl")).Select(JObject.Parse).ToArray();
        var devote = records[1]["Entry"]!;
        Require((string?)devote["Kind"] == "devote-accepted" && (string?)devote["Input"]?["Operation"] == "Devote" &&
            (int?)devote["Input"]?["TargetRoom"] == 2 && devote["Input"]?["CardInstanceId"] == null,
            "Devote lost its original room or acquired a fabricated card identity");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("Advice history: " + message); }

    private static void Wait(Func<bool> predicate) => Require(SpinWait.SpinUntil(predicate, TimeSpan.FromSeconds(5)), "worker did not finish");

    private static void InputTargets()
    {
        object actor = new(), droppedUnit = new();
        Require(AdviceDiagnosticInput.TargetCharacter(true, false, null, actor) == null,
            "an implicit Self skill logged its caster as an explicit target");
        Require(AdviceDiagnosticInput.TargetCharacter(true, false, droppedUnit, actor) == null,
            "an occupied skill destination logged its occupant as a selected unit");
        Require(AdviceDiagnosticInput.TargetCharacter(true, true, droppedUnit, actor) == droppedUnit &&
            AdviceDiagnosticInput.TargetCharacter(true, true, actor, actor) == actor,
            "a unit skill lost its explicit target");
        Require(AdviceDiagnosticInput.TargetCharacter(true, true, droppedUnit, null) == droppedUnit &&
            AdviceDiagnosticInput.TargetCharacter(true, true, null, actor) == null,
            "a room skill lost its selected unit or inferred a target from the caster");
        Require(AdviceDiagnosticInput.TargetCharacter(false, false, droppedUnit, actor) == droppedUnit &&
            AdviceDiagnosticInput.TargetCharacter(false, false, null, actor) == actor,
            "a hand play lost its native drop or self target");
    }

    private static void Rotation(string path)
    {
        using var journal = new AdviceDiagnosticJournal(path, maxFileBytes: 1024);
        for (int index = 1; index <= 12; index++)
        {
            journal.Record(new AdviceDiagnosticEntry { Kind = "建议-" + index, Reason = new string('x', 300) });
            int expected = index;
            Wait(() => journal.Written == expected || journal.Error != null);
            Require(journal.Error == null, "rotation failed: " + journal.Error);
        }
        journal.Dispose(); Wait(() => journal.Completed);
        Require(journal.Dropped == 0 && journal.WriterThreadId != Environment.CurrentManagedThreadId, "writes were dropped or used the caller thread");
        var files = Directory.GetFiles(path);
        Require(files.Length == 2 && files.All(file => new FileInfo(file).Length <= 1024), "rotation exceeded two bounded files");
        var lines = File.ReadAllLines(Path.Combine(path, "advice.previous.jsonl"))
            .Concat(File.ReadAllLines(Path.Combine(path, "advice.jsonl"))).Select(JObject.Parse).ToArray();
        Require(lines.All(line => (int)line["Schema"]! == 1) && (string?)lines.Last()["Entry"]?["Kind"] == "建议-12", "UTF-8 record lost its envelope or text");
        Require(lines.Select(line => (long)line["Sequence"]!).SequenceEqual(Enumerable.Range(13 - lines.Length, lines.Length).Select(i => (long)i)), "rotation lost record ordering");
    }

    private static void Overflow(string path)
    {
        using var journal = new AdviceDiagnosticJournal(path, maxFileBytes: 1024, capacity: 1);
        const int count = 10000;
        for (int index = 0; index < count; index++)
            journal.Record(new AdviceDiagnosticEntry { Kind = "too-large", DisplayText = new string('a', 2048) });
        journal.Dispose(); Wait(() => journal.Completed);
        Require(journal.Error == null && journal.Written == 0 && journal.Dropped == count, "oversized/overflowing events were not accounted for");
        Require(!Directory.Exists(path), "oversized events reached the filesystem");
        journal.Record(new AdviceDiagnosticEntry());
        Require(journal.Dropped == count + 1, "shutdown accepted another event");
    }

    private static void Failure(string path)
    {
        File.WriteAllText(path, "An existing file prevents directory creation.");
        using var journal = new AdviceDiagnosticJournal(path);
        journal.Record(new AdviceDiagnosticEntry { Kind = "io-failure" });
        Wait(() => journal.Completed);
        Require(journal.Error != null && journal.Dropped == 1, "I/O failure escaped or was hidden");
        journal.Record(new AdviceDiagnosticEntry());
        Require(journal.Dropped == 2, "failed worker kept accepting events");
    }
}
