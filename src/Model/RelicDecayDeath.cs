using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicDeathEffectRule
    {
        public string Team { get; }
        public string Subtype { get; }
        public CardActionEffect Action { get; }
        public RelicDeathEffectRule(string team, string subtype, CardActionEffect action)
        { Team = team; Subtype = subtype; Action = action; }
    }
    internal static class RelicDecayDeathModel
    {
        internal const string DataId = "d916bcc3-85ff-49ee-937d-32d7253a4e4d";
        internal const string AssetKey = "OnDecayDeathApplyStatusEffects";
        internal const string Effect = "RelicEffectAddEffectOnUnitDeath";
        internal static readonly string[] CardIds = Enumerable.Range(971, 6)
            .Select(index => "fc119000-0000-4000-8000-000000000" + index).ToArray();
        internal static PlayCardAction? ChoosePlay(BattleTurnState source)
        {
            if (source.Spawn.Turn > 3) return null;
            foreach (string dataId in CardIds)
            {
                int index = Array.IndexOf(CardIds, dataId), floor = index < 4 || index == 5 ? 0 : 1;
                if (index == 5 && source.Spawn.Turn != 3 || index != 5 && source.Spawn.Turn != 0) continue;
                PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                    action.RoomIndex == floor && source.Spawn.Train.Context!.FindCard(action.CardInstanceId)?.DataId == dataId);
                if (play != null) return play;
            }
            return null;
        }
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule)
        {
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual(new[] { Effect }) &&
                rule.EffectIndex == 0 && rule.ParamInt == 8 && rule.ParamInt2 == 0 && rule.Amount == 0 &&
                rule.CanApplyInPreviewMode && rule.Conditions.Count == 0 && rule.RequiredMutatorId == null &&
                rule.DeathEffect?.Team == "255" && rule.DeathEffect.Subtype == "SubtypesData_None" &&
                rule.DeathEffect.Action.Value == 11 && rule.DeathEffect.Action.Statuses.Count == 1 && rule.DeathEffect.Action.Statuses[0].Id == "decay" &&
                TriggeredStatusTransferModel.Validate(rule.DeathEffect.Action) == null
                ? null : "Missing or changed original Decay-death transfer definition.";
        }
        internal static bool Test(CombatUnit? actor, string trigger, int damageType) =>
            actor != null && trigger == "OnHit" && damageType == 8 && actor.Health <= 0;
        internal static RoomCombatResult Apply(RoomCombatState source, CombatUnit actor)
        {
            CombatRelicState? owner = source.Context?.Relics?.SingleOrDefault(item => item.DataId == DataId);
            RelicResourceEffect? rule = owner?.ResourceEffects?.SingleOrDefault(item => item.Type == Effect);
            string? error = rule == null ? "Missing original Decay-death transfer owner." : Validate(owner!, rule);
            if (error != null) return new(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
            bool retained = source.Units.All(unit => unit.Id != actor.Id);
            RoomCombatState scope = retained ? new(source.RoomIndex, source.Deployment, source.Units.Concat(new[] { actor }).ToArray(),
                source.ExternalInteractions, source.Context, source.Preview, source.ActiveCompanionBossIds) : source;
            RoomCombatResult transferred = TriggeredStatusTransferModel.Apply(scope, actor.Id, rule!.DeathEffect!.Action, 0);
            if (!transferred.Supported || !retained) return transferred;
            RoomCombatState result = transferred.State!;
            return new(new(result.RoomIndex, result.Deployment, result.Units.Where(unit => unit.Id != actor.Id).ToArray(),
                result.ExternalInteractions, result.Context, result.Preview, result.ActiveCompanionBossIds), transferred.Outcome,
                transferred.Rounds, transferred.Events.ToList(), pendingCallbacks: transferred.PendingCallbacks, retainedUnits: new[] { actor });
        }
    }
}
