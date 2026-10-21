using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class BossActionReference
    {
        public int Group { get; }
        public int Action { get; }
        public BossActionReference(int group, int action) { Group = group; Action = action; }
    }
    public sealed class BossActionGroupProgress
    {
        public int Repeats { get; }
        public int ActionIndex { get; }
        public int? CurrentAction { get; }
        public IReadOnlyList<bool> Executable { get; }
        public bool AlwaysValid { get; }
        public BossActionGroupProgress(int repeats, int actionIndex, int? currentAction,
            IReadOnlyList<bool> executable, bool alwaysValid = false)
        {
            Repeats = repeats; ActionIndex = actionIndex; CurrentAction = currentAction;
            Executable = Array.AsReadOnly(executable.ToArray()); AlwaysValid = alwaysValid;
        }
    }
    public sealed class BossActionControllerProgress
    {
        public IReadOnlyList<BossActionGroupProgress> Groups { get; }
        public BossActionGroupProgress DestroyGroup { get; }
        public int GroupIndex { get; }
        // Groups.Count identifies the separate native room-destruction group.
        public int? CurrentGroup { get; }
        public BossActionReference? InitialAction { get; }
        public BossActionReference? NextAction
        {
            get
            {
                if (Groups.Count == 0) return null;
                if (InitialAction != null) return InitialAction;
                if (!CurrentGroup.HasValue) return null;
                int index = CurrentGroup.Value;
                BossActionGroupProgress group = index == Groups.Count ? DestroyGroup : Groups[index];
                return group.CurrentAction.HasValue ? new BossActionReference(index, group.CurrentAction.Value) : null;
            }
        }
        public BossActionControllerProgress(IReadOnlyList<BossActionGroupProgress> groups,
            BossActionGroupProgress destroyGroup, int groupIndex, int? currentGroup, BossActionReference? initialAction)
        {
            Groups = Array.AsReadOnly(groups.ToArray()); DestroyGroup = destroyGroup;
            GroupIndex = groupIndex; CurrentGroup = currentGroup; InitialAction = initialAction;
        }
    }
    public sealed class BossActionControllerResult
    {
        public BossActionControllerProgress? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal BossActionControllerResult(BossActionControllerProgress? state, string? reason = null)
        { State = state; UnsupportedReason = reason; }
    }
    public static class BossActionControllerModel
    {
        public static BossActionControllerResult Apply(BossActionControllerProgress source, string operation, bool destroyRound = false)
        {
            BossActionControllerResult Fail(string reason) => new BossActionControllerResult(null, reason);
            int count = source.Groups.Count;
            if (source.GroupIndex < 0 || count > 0 && source.GroupIndex >= count ||
                source.CurrentGroup.HasValue && (source.CurrentGroup < 0 || source.CurrentGroup > count) ||
                source.Groups.Append(source.DestroyGroup).Any(group => group.Executable.Count == 0 || group.ActionIndex < 0 ||
                    group.ActionIndex >= group.Executable.Count || group.CurrentAction.HasValue &&
                    (group.CurrentAction < 0 || group.CurrentAction >= group.Executable.Count)))
                return Fail("Missing native boss action-group cursor or executable actions.");
            if (source.InitialAction != null && (source.InitialAction.Group < 0 || source.InitialAction.Group > count ||
                source.InitialAction.Action < 0 || source.InitialAction.Action >=
                    (source.InitialAction.Group == count ? source.DestroyGroup : source.Groups[source.InitialAction.Group]).Executable.Count))
                return Fail("Invalid initial boss action reference.");
            if (operation is not ("DecideInitial" or "DecideNext" or "Perform")) return Fail("Unknown boss controller operation.");
            var groups = source.Groups.ToArray(); BossActionGroupProgress destroy = source.DestroyGroup;
            int index = source.GroupIndex; int? selected = source.CurrentGroup;
            BossActionReference? initial = source.InitialAction;
            BossActionControllerProgress State() => new BossActionControllerProgress(groups, destroy, index, selected, initial);
            BossActionGroupProgress Get(int group) => group == count ? destroy : groups[group];
            void Set(int group, BossActionGroupProgress value) { if (group == count) destroy = value; else groups[group] = value; }
            BossActionGroupProgress Increment(BossActionGroupProgress group)
            {
                int action = group.ActionIndex + 1, repeats = group.Repeats;
                if (action >= group.Executable.Count) { action = 0; repeats = unchecked(repeats - 1); }
                return new BossActionGroupProgress(repeats, action, group.CurrentAction, group.Executable, group.AlwaysValid);
            }
            if (operation == "Perform")
            {
                if (!selected.HasValue) return new BossActionControllerResult(source);
                initial = null;
                BossActionGroupProgress group = Get(selected.Value);
                if (group.CurrentAction.HasValue) Set(selected.Value, Increment(group));
                return new BossActionControllerResult(State());
            }
            if (initial == null)
            {
                if (destroyRound && operation != "DecideInitial") selected = count;
                else if (count == 0) selected = null;
                else if (index >= count - 1) selected = count - 1;
                else
                {
                    for (int attempt = 0; attempt < count; attempt++)
                    {
                        if (groups[index].AlwaysValid || groups[index].Repeats >= 0) { selected = index; break; }
                        index = Math.Min(count - 1, index + 1);
                    }
                }
                if (selected.HasValue)
                {
                    BossActionGroupProgress group = Get(selected.Value);
                    int start = group.ActionIndex;
                    while (!group.Executable[group.ActionIndex])
                    {
                        group = Increment(group);
                        if (group.ActionIndex == start) break;
                    }
                    if (group.Executable[group.ActionIndex])
                        group = new BossActionGroupProgress(group.Repeats, group.ActionIndex, group.ActionIndex, group.Executable, group.AlwaysValid);
                    Set(selected.Value, group);
                }
                else if (count > 0) return Fail("Native boss controller cannot select an action group.");
            }
            if (operation == "DecideInitial") initial = State().NextAction;
            return new BossActionControllerResult(State());
        }
    }
}
