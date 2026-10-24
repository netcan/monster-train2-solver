using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomAttachmentCorruption
    {
        public int Maximum { get; }
        public int Current { get; }
        public RoomAttachmentCorruption(int maximum, int current) { Maximum = maximum; Current = current; }
    }
    public sealed class RoomAbilityState
    {
        public string DataId { get; }
        public int Cooldown { get; }
        public int CooldownAfterActivated { get; }
        public CardCreationRule Creation { get; }
        public RoomAbilityState(string dataId, int cooldown, int cooldownAfterActivated, CardCreationRule creation)
        { DataId = dataId; Cooldown = cooldown; CooldownAfterActivated = cooldownAfterActivated; Creation = creation; }
        internal RoomAbilityState WithCooldown(int value) => new RoomAbilityState(DataId, value, CooldownAfterActivated, Creation);
    }
    public static class RoomAbilityModel
    {
        internal static CardActionEffect ResolveEffect(CardActionEffect effect, int ownerRoom, int selectedRoom) =>
            effect.Type == "RoomAbilityBumpToCurrentRoom" ? new CardActionEffect("Bump", effect.Target, ownerRoom - selectedRoom,
                effect.AllowEnemy, effect.AllowPlayer, effect.Statuses, tests: effect.Tests, filters: effect.Filters) : effect;
        internal static CombatContext ChangeCooldown(CombatContext source, int? activatedRoom = null)
        {
            if (source.RoomCapacities == null) return source;
            return RoomSpellUpgradeModel.Replace(source, source.RoomCapacities.Select(room =>
            {
                RoomCapacityAttachmentState? ability = room.Attachments?.FirstOrDefault(attachment => attachment.Ability != null);
                if (ability == null || activatedRoom.HasValue && activatedRoom != room.RoomIndex) return room;
                RoomAbilityState value = ability.Ability!;
                int cooldown = activatedRoom.HasValue ? value.CooldownAfterActivated : Math.Max(0, value.Cooldown - 1);
                return new RoomCapacityState(room.RoomIndex, room.PlayerMaximum, room.EnemyMaximum, room.IsPyre,
                    room.Corruption, room.Attachments!.Select(attachment => ReferenceEquals(attachment, ability)
                        ? attachment.WithAbility(value.WithCooldown(cooldown)) : attachment).ToArray(),
                    room.MaxCorruption, room.PermanentCorruption, room.AttachmentsAddedFromCards);
            }).ToArray());
        }
        internal static BattleActionEnumeration EnumerateActivations(BattleTurnState source,
            Func<BattleTurnState, PlayCardAction, BattleActionResult>? play = null)
        {
            var actions = new List<SupportedActionCandidate>();
            var unsupported = new List<UnsupportedActionAttempt>();
            CombatContext? context = source.Spawn.Train.Context;
            if (context?.RoomCapacities == null || source.PlayRules == null) return new BattleActionEnumeration(actions, unsupported);
            foreach (RoomCapacityState room in context.RoomCapacities)
            foreach (RoomCapacityAttachmentState attachment in room.Attachments ?? Array.Empty<RoomCapacityAttachmentState>())
            {
                if (attachment.Ability == null) continue;
                AbilityCardResult cache = AbilityCardModel.Get(context, attachment.Ability.Creation);
                if (!cache.Supported) { unsupported.Add(new UnsupportedActionAttempt(null, cache.UnsupportedReason!)); continue; }
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(rule => rule.DataId == attachment.Ability.DataId);
                if (rule == null) { unsupported.Add(new UnsupportedActionAttempt(null, "Missing room ability definition.")); continue; }
                foreach (RoomPlayRule target in source.PlayRules.Rooms)
                foreach (int id in CardSpellModel.RequiresUnitTarget(rule.Effects)
                    ? source.Spawn.Train.Rooms.Single(state => state.RoomIndex == target.RoomIndex).Units.Where(unit => unit.Health > 0).Select(unit => unit.Id)
                    : new[] { 0 })
                {
                    var action = new PlayCardAction(cache.Card!.InstanceId, target.RoomIndex, targetUnitId: id, activatorRoomIndex: room.RoomIndex);
                    BattleActionResult result = play?.Invoke(source, action) ?? BattleActionModel.PlayCard(source, action);
                    if (result.Supported) actions.Add(new SupportedActionCandidate(action, result));
                    else if (result.Rejection == ActionRejection.Unsupported) unsupported.Add(new UnsupportedActionAttempt(action, result.Reason!));
                }
            }
            return new BattleActionEnumeration(actions, unsupported);
        }
    }
}
