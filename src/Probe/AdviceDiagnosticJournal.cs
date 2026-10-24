using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;

namespace MonsterTrain2Poju.Probe
{
    // Only detached values enter this queue. No Unity/native objects, captures,
    // serialization or filesystem operations belong on the caller's thread.
    internal sealed class AdviceDiagnosticEntry
    {
        public string Kind { get; set; } = "";
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string? Battle { get; set; }
        public int Turn { get; set; }
        public long Generation { get; set; }
        public long PublishedGeneration { get; set; }
        public long Searches { get; set; }
        public int CurrentPyreHp { get; set; }
        public bool AdviceVisible { get; set; }
        public bool AdvicePending { get; set; }
        public bool FullPlan { get; set; }
        public string? Reason { get; set; }
        public string? DisplayText { get; set; }
        public bool DisplayTextTruncated { get; set; }
        public AdviceDiagnosticForecast? Forecast { get; set; }
        public AdviceDiagnosticInput? Input { get; set; }
    }

    internal sealed class AdviceDiagnosticForecast
    {
        public string Outcome { get; set; } = "";
        public bool Terminal { get; set; }
        public int ForecastPyreHp { get; set; }
        public int FutureLoss { get; set; }
        public int TurnsCompleted { get; set; }
        public bool TimedOut { get; set; }
        public string Source { get; set; } = "";
        public int PlanStepCount { get; set; }
        public AdviceDiagnosticStep[] Steps { get; set; } = Array.Empty<AdviceDiagnosticStep>();
    }

    internal sealed class AdviceDiagnosticStep
    {
        public int Turn { get; set; }
        public bool EndTurn { get; set; }
        // Immutable model values, including recursion/mix selections. These
        // never reference native cards or characters.
        public PlayCardAction? Action { get; set; }
        public string? Card { get; set; }
        public string? SourceUnit { get; set; }
        public string? TargetUnit { get; set; }
    }

    internal sealed class AdviceDiagnosticInput
    {
        public int? CardInstanceId { get; set; }
        public string? CardDataId { get; set; }
        public string? Card { get; set; }
        public string? CardIdentity { get; set; }
        public int HandIndex { get; set; } = -1;
        public int TargetRoom { get; set; } = -1;
        // A native physical spawn point, not the model's insertion position.
        public int DropSpawnPoint { get; set; } = -1;
        public int? TargetUnitId { get; set; }
        public int? SourceUnitId { get; set; }
        public string? SourceUnit { get; set; }
        public int SourceRoom { get; set; } = -1;
        public bool FromDirectPlay { get; set; }
    }

    internal sealed class AdviceDiagnosticJournal : IDisposable
    {
        private readonly BlockingCollection<AdviceDiagnosticEntry> queue;
        private readonly string directory;
        private readonly int maxFileBytes;
        private readonly string session = Guid.NewGuid().ToString("N");
        private long written, dropped;
        private long cardInputs;
        private int disposed, completed;
        private string? error;
        internal long Written => Interlocked.Read(ref written);
        internal long Dropped => Interlocked.Read(ref dropped);
        internal long CardInputs => Interlocked.Read(ref cardInputs);
        internal bool Completed => Volatile.Read(ref completed) != 0;
        internal string? Error => Volatile.Read(ref error);
        internal string DirectoryPath => directory;
        internal int WriterThreadId { get; private set; }

        internal AdviceDiagnosticJournal(string directory, int maxFileBytes = 2 * 1024 * 1024, int capacity = 64)
        {
            this.directory = directory;
            this.maxFileBytes = maxFileBytes;
            queue = new BlockingCollection<AdviceDiagnosticEntry>(capacity);
            new Thread(WriteLoop) { IsBackground = true, Priority = ThreadPriority.Lowest,
                Name = "Poju advice history" }.Start();
        }

        internal void Record(AdviceDiagnosticEntry entry)
        {
            if (Volatile.Read(ref disposed) == 0 && Error == null)
                try
                {
                    if (queue.TryAdd(entry))
                    {
                        if (entry.Kind == "card-accepted") Interlocked.Increment(ref cardInputs);
                        return;
                    }
                }
                catch (InvalidOperationException) { } // Concurrent shutdown.
            Interlocked.Increment(ref dropped);
        }

        private void WriteLoop()
        {
            WriterThreadId = Thread.CurrentThread.ManagedThreadId;
            try
            {
                // Delay directory creation until the first relevant event.
                foreach (AdviceDiagnosticEntry entry in queue.GetConsumingEnumerable())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                    {
                        Schema = 1, Version = typeof(AdviceDiagnosticJournal).Assembly.GetName().Version?.ToString(),
                        Session = session, Sequence = Written + 1,
                        DroppedRecords = Dropped, Entry = entry
                    }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }) + "\n");
                    if (bytes.Length > Math.Min(256 * 1024, maxFileBytes))
                    { Interlocked.Increment(ref dropped); continue; }
                    Directory.CreateDirectory(directory);
                    string current = Path.Combine(directory, "advice.jsonl");
                    string previous = Path.Combine(directory, "advice.previous.jsonl");
                    if (File.Exists(current) && new FileInfo(current).Length + bytes.Length > maxFileBytes)
                    {
                        if (File.Exists(previous)) File.Delete(previous);
                        File.Move(current, previous);
                    }
                    using (var stream = new FileStream(current, FileMode.Append, FileAccess.Write, FileShare.Read))
                        stream.Write(bytes, 0, bytes.Length);
                    Interlocked.Increment(ref written);
                }
            }
            catch (Exception failure)
            {
                Volatile.Write(ref error, failure.GetType().Name + ": " + failure.Message);
                queue.CompleteAdding();
                // Disable only diagnostics. Gameplay and advice keep working.
                Interlocked.Increment(ref dropped);
                while (queue.TryTake(out _)) Interlocked.Increment(ref dropped);
            }
            finally { Volatile.Write(ref completed, 1); }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0) queue.CompleteAdding();
            // No join or disk flush on the Unity thread; the worker drains it.
        }
    }
}
