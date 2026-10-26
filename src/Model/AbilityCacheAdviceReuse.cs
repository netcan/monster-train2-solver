using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // A native tooltip may lazily create a skill card. Reproduce that exact
    // known creation; never normalize the allocator or ignore cache contents.
    internal static class AbilityCacheAdviceReuse
    {
        internal static bool TryWarm(BattleTurnState source, BattleTurnState target, out BattleTurnState warmed)
        {
            warmed = source;
            CombatContext? before = source.Spawn.Train.Context, after = target.Spawn.Train.Context;
            if (source.PlayRules == null || before?.AbilityCardCache == null || after?.AbilityCardCache == null ||
                after.AbilityCardCache.Count <= before.AbilityCardCache.Count ||
                after.AbilityCardCache.Count - before.AbilityCardCache.Count > 8) return false;
            for (int index = 0; index < before.AbilityCardCache.Count; index++)
                if (before.AbilityCardCache[index].DataId != after.AbilityCardCache[index].DataId ||
                    before.AbilityCardCache[index].InstanceId != after.AbilityCardCache[index].InstanceId) return false;
            var definitions = source.PlayRules.Cards.Select(rule => rule.SpawnUnit?.Ability?.CardCreation)
                .Concat(source.Spawn.Train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Ability?.CardCreation))
                .Where(rule => rule != null).ToArray();
            CombatContext context = before;
            foreach (AbilityCardCacheEntry entry in after.AbilityCardCache.Skip(before.AbilityCardCache.Count))
            {
                CardCreationRule? definition = definitions.FirstOrDefault(rule => rule!.DataId == entry.DataId);
                if (definition == null) return false;
                AbilityCardResult result = AbilityCardModel.Get(context, definition);
                if (!result.Supported || !result.Created || result.Card?.InstanceId != entry.InstanceId) return false;
                context = result.Context!;
            }
            TrainCombatState train = CardSpellModel.WithContext(source.Spawn.Train, context);
            EnemySpawnState old = source.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
                old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled, old.FirstTreasureTurn,
                old.FirstTreasureRoom, old.Turn, old.ExternalInteractions, old.CanonicalDecisionReferences,
                old.PendingDestroyedUnitIds, old.OuterBoss, old.QueuedOuterBoss, old.QueuedTrueFinalBoss, old.TrueFinalBoss);
            warmed = new BattleTurnState(spawn, source.Energy, source.EnergyPerTurn, source.DrawPerTurn, source.ForgePoints,
                source.DragonsHoard, source.MoonPhase, source.RngStreams, source.OtherPiles, source.ExternalInteractions,
                source.PlayRules, source.BattlePreviewEnabled, source.UiRngIsolated, source.CanonicalDecisionReferences,
                source.CanonicalPhysicalReferences, source.SelectedRoom, source.MoonPhaseTrackingActive);
            return true;
        }

        internal static bool Matches(BattleTurnState left, BattleTurnState right, BattleStateKeys keys) =>
            TryWarm(left, right, out BattleTurnState warmed) && keys.Key(warmed) == keys.Key(right) ||
            TryWarm(right, left, out warmed) && keys.Key(warmed) == keys.Key(left);
    }
}
