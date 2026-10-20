using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class SentryAttackProbe
    {
        internal const string Type = "RoomStateSentryAttackModifier";
        internal static SentryState? Capture(CharacterState actor)
        {
            RoomStateSentryAttackModifier? modifier = actor.GetRoomStateModifiers<RoomStateSentryAttackModifier>().SingleOrDefault();
            return modifier == null ? null : new SentryState(((List<CharacterState>)AccessTools.Field(modifier.GetType(), "cachedSentryTargets")
                .GetValue(modifier)).Select(target => FullBattleTrace.Active!.UnitId(target)).ToArray(),
                (bool)AccessTools.Field(modifier.GetType(), "isAttacking").GetValue(modifier));
        }
        internal static SentryState? Definition(CharacterData data) => data.GetRoomModifiersData().Any(modifier =>
            modifier.GetRoomStateModifierClassName() == Type) ? new SentryState(Array.Empty<int>()) : null;
    }
}
