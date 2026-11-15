using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomHealthAttackProbe
    {
        // Use the same native room-target collector as GetRoomStateModifiedDamage;
        // a target's own modifier list does not describe the other aura sources.
        internal static int Additional(CharacterState target)
        {
            if (target.GetTeamType() != Team.Type.Monsters || target.GetCurrentRoom() is not RoomState room) return 0;
            var data = new TargetHelper.CollectTargetsData(); data.Reset(TargetMode.Room);
            data.targetTeamType = target.GetTeamType(); data.roomIndex = room.GetRoomIndex();
            data.targetModeHealthFilter = CardEffectData.HealthFilter.Both;
            data.targetModeStatusEffectsFilter = new List<string>(); data.targetModeStatusEffectsExcludedFilter = new List<string>();
            var sources = new List<CharacterState>();
            TargetHelper.CollectTargets(data, AllGameManagers.Instance!.GetCoreManagers(), ref sources);
            int result = 0;
            foreach (CharacterState source in sources)
            foreach (RoomStateModifierBase modifier in source.GetRoomStateModifiers<RoomStateModifierBase>()
                .Where(modifier => modifier.GetType().Name == RoomHealthDamageModel.FriendlyOwn))
                result = unchecked(result + modifier.GetDynamicInt(target));
            return result;
        }
    }
}
