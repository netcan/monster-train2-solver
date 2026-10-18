using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicEndCombatModel
    {
        internal const string CavendishId = "24c4ad26-f2b3-4a95-bddc-caab94bd4767";
        internal const string CavendishKey = "BalatroEventCavendish";
        internal const string GrosMichelId = "75abded0-b922-4413-84ca-f0095642c600";
        internal const string GrosMichelKey = "BalatroEventGrosMichel";
        internal const string GrosMichelDestroy = "GrosMichelRelicEffectChanceToDestroySelf";
        private const int CavendishDestroyChance = 1;

        internal static PlayCardAction? ChooseGrosMichelPlay(BattleTurnState source)
        {
            CombatContext context = source.Spawn.Train.Context!;
            int[] deployed = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId > 0).Select(unit => unit.SpawnerCardId).ToArray();
            int floor;
            if (source.Spawn.Turn == 0 && deployed.Length < 2) floor = deployed.Length;
            else if (source.Spawn.Turn > 0 && deployed.Length == 2 && context.Relics?.Any(owner => owner.DataId == GrosMichelId) != true) floor = 2;
            else return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action => action.RoomIndex == floor &&
                !deployed.Contains(action.CardInstanceId) && context.FindCard(action.CardInstanceId)?.MaskDescriptor?.Definition is CardMaskDefinition definition &&
                definition.CardType == "Monster" && definition.IsChampionCard == false);
        }

        internal static string? Validate(CombatRelicState relic)
        {
            bool grosMichel = relic.DataId == GrosMichelId;
            bool hasDestroy = relic.EffectTypes.Contains(RelicModel.ChanceToDestroySelf) || relic.EffectTypes.Contains(GrosMichelDestroy);
            bool hasAttempt = relic.EffectTypes.Contains(RelicModel.BalatroBananaShake);
            if (!hasDestroy && !hasAttempt) return null;

            string[] expected = { RelicModel.ModifyCardAttack, grosMichel ? GrosMichelDestroy : RelicModel.ChanceToDestroySelf, RelicModel.BalatroBananaShake };
            if ((grosMichel ? relic.AssetKey != GrosMichelKey : relic.DataId != CavendishId || relic.AssetKey != CavendishKey) ||
                !relic.EffectTypes.SequenceEqual(expected))
                return "End-of-combat relic effects require an unchanged Cavendish or Gros Michel owner.";
            if (grosMichel && (relic.IsCovenant != false || relic.DisallowedInPlacementPhase != false ||
                relic.ResourceEffects?.Count != 1 || relic.ResourceEffects[0].EffectIndex != 1 ||
                relic.ResourceEffects[0].Type != GrosMichelDestroy || relic.ResourceEffects[0].ParamInt != 10 ||
                relic.ResourceEffects[0].ParamInt2 != 2 || relic.ResourceEffects[0].Amount != 10 ||
                relic.ResourceEffects[0].Conditions.Count != 0 || relic.ResourceEffects[0].CanApplyInPreviewMode ||
                relic.ResourceEffects[0].RequiredMutatorId != null))
                return "Gros Michel requires its captured 10% chance, two waiting rounds and native callback restrictions.";
            if (relic.CardAttackModifiers?.Count != 1 || relic.CardAttackModifiers[0].EffectIndex != 0 ||
                relic.CardAttackModifiers[0].AttackModifier != (grosMichel ? 10 : 0) || relic.CardAttackModifiers[0].AttackMultiplier != 0f ||
                relic.CardAttackModifiers[0].UnmodifiedAttackMultiplier != (grosMichel ? 0 : 3) || relic.CardAttackModifiers[0].CardType != "Monster" ||
                relic.CardAttackModifiers[0].CharacterName.Length != 0 || relic.CardAttackModifiers[0].CharacterSubtypeKey.Length != 0 ||
                relic.CardAttackModifiers[0].MultiplyByUpgradeCount || relic.CardAttackModifiers[0].MultiplyByRelicCount ||
                !relic.CardAttackModifiers[0].ExcludeChampions || relic.CardAttackModifiers[0].CardFilter != null ||
                relic.CardAttackModifiers[0].AppliedCardAttacks == null)
                return "Banana end-of-combat undo requires its original Monster attack definition and captured contribution list.";

            RelicCardAttackContribution[] contributions = relic.CardAttackModifiers[0].AppliedCardAttacks!.ToArray();
            if (contributions.Any(item => item.CardInstanceId <= 0 || item.Amount <= 0))
                return "BalatroEventCavendish has invalid native tracked card contributions.";
            return null;
        }

        internal static string? Validate(CombatContext? context)
        {
            CombatRelicState[] owners = (context?.Relics ?? Array.Empty<CombatRelicState>())
                .Where(relic => relic.EffectTypes.Contains(RelicModel.ChanceToDestroySelf) || relic.EffectTypes.Contains(GrosMichelDestroy) ||
                    relic.EffectTypes.Contains(RelicModel.BalatroBananaShake)).ToArray();
            if (owners.Length == 0) return null;
            if (owners.Length != 1) return "Duplicate end-of-combat relic owners are not modeled.";
            CombatRelicState owner = owners[0];
            string? error = Validate(owner);
            if (error != null) return error;
            if (owner.DataId == GrosMichelId && context?.QueryFrame?.GrosMichelCombatRoundCount is not >= 0)
                return "Gros Michel requires the persistent native saved combat-round count.";
            if (context!.CardInstances == null && context.CardRegistry == null)
                return "Banana end-of-combat undo requires captured owned deck cards.";
            HashSet<int> cardIds = (context.CardInstances ?? Array.Empty<CardInstanceState>())
                .Concat(context.CardRegistry ?? Array.Empty<CardInstanceState>())
                .Select(card => card.InstanceId).ToHashSet();
            if (owner.CardAttackModifiers![0].AppliedCardAttacks!.Any(item => item.InDeckState && !cardIds.Contains(item.CardInstanceId)))
                return "Banana native tracked deck card is missing from the battle card state.";
            return null;
        }

        internal static CombatContext Apply(CombatContext context, out string? error,
            IReadOnlyCollection<int>? removedFromDeck = null)
        {
            context = RelicSpawnRetreatModel.Reset(context);
            error = Validate(context);
            if (error != null || context.IsolatedBattlePreview == true || context.Relics == null) return context;
            int ownerIndex = -1;
            for (int index = 0; index < context.Relics!.Count; index++)
            {
                if (context.Relics[index].DataId == CavendishId || context.Relics[index].DataId == GrosMichelId)
                { ownerIndex = index; break; }
            }
            if (ownerIndex < 0) return context;

            RngDraw draw = context.BattleRng.Range(0, 100);
            context = context.WithBattleRng(draw.State);
            bool grosMichel = context.Relics[ownerIndex].DataId == GrosMichelId;
            if (grosMichel && context.QueryFrame!.GrosMichelCombatRoundCount < 2)
                return context.WithQueryFrame(context.QueryFrame.With(
                    grosMichelCombatRoundCount: context.QueryFrame.GrosMichelCombatRoundCount + 1));
            if (draw.Value >= (grosMichel ? 10 : CavendishDestroyChance)) return context;

            CombatRelicState owner = context.Relics![ownerIndex];
            HashSet<int> purgedCardIds = (context.OtherPiles ?? Array.Empty<CardPileState>())
                .Where(pile => pile.Name == "Purged").SelectMany(pile => pile.Cards)
                .Select(card => card.InstanceId).ToHashSet();
            if (removedFromDeck != null) purgedCardIds.UnionWith(removedFromDeck);
            var contributions = owner.CardAttackModifiers![0].AppliedCardAttacks!
                .Where(item => item.InDeckState && !purgedCardIds.Contains(item.CardInstanceId))
                .GroupBy(item => item.CardInstanceId)
                .ToDictionary(group => group.Key, group => group.Aggregate(0,
                    (total, item) => unchecked(total + item.Amount)));
            CardInstanceState[] Undo(IEnumerable<CardInstanceState>? source) => source?.Select(card =>
            {
                if (!contributions.TryGetValue(card.InstanceId, out int amount)) return card;
                CardStatModifier offsets = card.Temporary.Offsets;
                return card.WithModifiers(card.Permanent, new CardModifiers(new CardStatModifier(
                    unchecked(offsets.Damage - amount), offsets.Health, offsets.Cost, offsets.Heal, offsets.Size,
                    offsets.XCost, offsets.EquipmentLimit, offsets.UpgradeSlotCount), card.Temporary.Upgrades,
                    card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions));
            }).ToArray()!;
            context = context.WithCardInstances(Undo(context.CardInstances));
            context = context.WithCardRegistry(Undo(context.CardRegistry));
            return context.WithRelics(context.Relics.Where((_, index) => index != ownerIndex).ToArray());
        }
    }
}
