using System;
using System.Linq;
using System.Threading;

namespace MonsterTrain2Poju.Model
{
    // Shared only by searches of one immutable root. Reaching this objective
    // bound is success, not cancellation; the winning worker must still return
    // its complete plan for independent replay before publication.
    internal sealed class BattleEarlyStop
    {
        internal int PyreHealthUpperBound { get; }
        private int reached;
        internal bool Reached => Volatile.Read(ref reached) != 0;
        private BattleEarlyStop(int bound) { PyreHealthUpperBound = bound; }

        internal static BattleEarlyStop? Create(BattleTurnState source, CancellationToken token)
        {
            CombatUnit[] pyres = source.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.IsPyre).ToArray();
            CombatContext? context = source.Spawn.Train.Context;
            if (token.IsCancellationRequested || pyres.Length != 1) return null;
            CombatUnit pyre = pyres[0];
            // Ordinary healing/status upgrades must not offer another way to
            // increase this actor's health outside the native train-heal paths.
            if (pyre.Health <= 0 || pyre.Modifiers?.CanBeHealed != false || pyre.Triggers.Any(trigger => trigger.Effects.Any(effect =>
                effect.Type != RelicResourceModel.PermanentPyreAttackAdjustment && effect.Type != "CardEffectHealTrain" &&
                effect.Type != "CardEffectPyreDamageIfNoPlayerUnits"))) return null;
            bool canHeal = false;
            bool changesMaximum = BattleStateKeys.AnySnapshot(source, value =>
            {
                if (value is CombatEffect effect && effect.Type == "CardEffectPermanentlyAdjustMaxPyreHealth") return true;
                // Resurrection bypasses ordinary healing and uses the actor's
                // maximum, which can differ from the raw train-healing cap.
                if (value is CombatRelicState relic && relic.EffectTypes.Contains(RelicPyreResurrectionModel.Effect)) return true;
                if (value is CardActionEffect action)
                {
                    // Direct health replacement/stat swaps bypass healability;
                    // conservatively decline the bound even on other actors.
                    if (action.Type is "AdjustPyreMaxHealth" or "SetHealth" or "SwapStats") return true;
                    if (action.Type is "HealTrain" or "HealTrainPerEmber") canHeal = true;
                    // Unknown direct Pyre operations cannot justify a bound.
                    else if (action.Target == "Pyre") return true;
                }
                return false;
            }, token);
            if (changesMaximum) return null;
            // Unit healing cannot heal the captured native Pyre. With no train
            // healing anywhere in the reachable definitions, current HP is an
            // upper bound. Otherwise keep all potentially useful healing routes.
            if (!canHeal) return new BattleEarlyStop(pyre.Health);
            if (context?.Resources == null) return null;
            RelicResourceResult maximum = RelicResourceModel.GetPyreMaxHealth(context);
            return maximum.Supported && pyre.Health <= maximum.Value ? new BattleEarlyStop(maximum.Value) : null;
        }

        internal void Consider(BattleTurnState state, RoomOutcome outcome)
        {
            if (outcome != RoomOutcome.BattleWon) return;
            int hp = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.IsPyre).Select(unit => unit.Health).FirstOrDefault();
            if (hp == PyreHealthUpperBound) Interlocked.Exchange(ref reached, 1);
        }
    }
}
