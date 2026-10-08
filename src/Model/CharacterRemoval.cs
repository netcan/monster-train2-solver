using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RemovalReferences
    {
        public string Name { get; }
        public IReadOnlyList<int> Ids { get; }
        public RemovalReferences(string name, IReadOnlyList<int> ids)
        { Name = name; Ids = Array.AsReadOnly(ids.ToArray()); }
    }

    public sealed class RemovalNumber
    {
        public string Name { get; }
        public int Value { get; }
        public RemovalNumber(string name, int value) { Name = name; Value = value; }
    }

    public sealed class RemovalInformation
    {
        public string Kind { get; }
        public IReadOnlyList<RemovalReferences> References { get; }
        public IReadOnlyList<RemovalNumber> UpgradeMaximums { get; }
        public IReadOnlyList<RemovalNumber> Numbers { get; }
        public IReadOnlyList<RemovalNumber> Statuses { get; }
        public RemovalInformation(string kind, IReadOnlyList<RemovalReferences> references,
            IReadOnlyList<RemovalNumber> upgradeMaximums, IReadOnlyList<RemovalNumber> numbers, IReadOnlyList<RemovalNumber> statuses)
        {
            Kind = kind; References = Array.AsReadOnly(references.ToArray());
            UpgradeMaximums = Array.AsReadOnly(upgradeMaximums.ToArray());
            Numbers = Array.AsReadOnly(numbers.ToArray()); Statuses = Array.AsReadOnly(statuses.ToArray());
        }
        internal RemovalInformation Destroy() => new RemovalInformation(Kind,
            References.Select(item => new RemovalReferences(item.Name, Array.Empty<int>())).ToArray(),
            Array.Empty<RemovalNumber>(), Numbers, Statuses);
    }

    // BeginDestroy and InRemoveList both satisfy native IsDestroyed. Neither clears
    // managed references. Even Destroyed only schedules Unity destruction; OnDestroy
    // performs the reference/listener cleanup at a later frame boundary.
    public sealed class CharacterRemovalState
    {
        public int Id { get; }
        public int Stage { get; }
        public int WeakTargetId { get; }
        public IReadOnlyList<RemovalReferences> Managers { get; }
        public IReadOnlyList<RemovalInformation> Information { get; }
        public CharacterRemovalState(int id, int stage, int weakTargetId, IReadOnlyList<RemovalReferences> managers,
            IReadOnlyList<RemovalInformation> information)
        {
            Id = id; Stage = stage; WeakTargetId = weakTargetId;
            Managers = Array.AsReadOnly(managers.ToArray()); Information = Array.AsReadOnly(information.ToArray());
        }
    }

    public static class CharacterRemovalModel
    {
        public static CharacterRemovalState SetStage(CharacterRemovalState source, int stage)
        {
            if (stage < 1 || stage > 3 || stage <= source.Stage)
                throw new ArgumentException("Destruction stages must advance from None through Destroyed.", nameof(stage));
            return new CharacterRemovalState(source.Id, stage, source.WeakTargetId, source.Managers, source.Information);
        }

        public static IReadOnlyList<CharacterRemovalState> ProcessQueue(IReadOnlyList<CharacterRemovalState> source,
            IReadOnlyList<int> queuedIds)
        {
            var queued = new HashSet<int>(queuedIds);
            if (queued.Count != queuedIds.Count || queued.Any(id => !source.Any(actor => actor.Id == id && actor.Stage == 2)))
                throw new ArgumentException("Only distinct actors already in the manager removal queue can be scheduled.", nameof(queuedIds));
            return Array.AsReadOnly(source.Select(actor => queued.Contains(actor.Id) ? SetStage(actor, 3) : actor).ToArray());
        }

        public static CharacterRemovalState CompleteDestruction(CharacterRemovalState source)
            => new CharacterRemovalState(source.Id, source.Stage, 0,
                source.Managers.Select(item => new RemovalReferences(item.Name, Array.Empty<int>())).ToArray(),
                source.Information.Select(info => info.Destroy()).ToArray());
    }
}
