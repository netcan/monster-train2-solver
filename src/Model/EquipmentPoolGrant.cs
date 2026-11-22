using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class EquipmentPoolGrantModel
    {
        internal const string NativeEffect = "CardEffectGrantEquipmentFromPool";
        internal const string CrownStatus = "redcrown";

        internal static bool CosmeticStatus(CombatStatus status) => status is
            { Id: CrownStatus, Stacks: 1, ParamInt: 1, Hidden: true, DisplayCategory: "Persistent", Stackable: false,
              Propagatable: false, ExcludeHeroPropagation: false, ExcludeMonsterPropagation: false,
              TriggerVfxEnemy: false, TriggerVfxPlayer: false, RemoveWhenTriggered: false, RemoveStackAtEnd: false,
              RemoveAllAtEnd: false, RemoveAfterPostCombat: false, PreventRemovalDuringRelentless: false,
              SkipDuringDeployment: false, RemoveDuringDeployment: false, RemoveWhenTriggeredAfterCardPlayed: false,
              RemoveAtEndOfTurnIfTriggered: false, MarkedForRemovalAfterCardPlayed: false };

        internal static bool Captured(CombatUnit unit, CombatTrigger trigger, CombatEffect effect, CombatContext? context) =>
            unit.Team == CombatTeam.Player && unit.Status(CrownStatus) is CombatStatus crown && CosmeticStatus(crown) &&
            trigger is { Kind: "OnSpawn", Once: true, FireCount: 1, Conditions: null, Effects.Count: 1 } &&
            effect.Action is { Type: NativeEffect, Target: "Self", Tests: { ShouldTest: true, DevotionRequirement: 0, DevotionRequirementMax: 0 } } &&
            effect.Generation is { Destination: "DiscardPile", Count: 1, Permanent: false, SelectedPoolIndex: null,
                DeterministicPoolSelection: false, CopyModifiers: false, Upgrade: null, Pool.Count: > 0 } generation &&
            generation.Pool.Select(card => card.DataId).SequenceEqual(effect.CardPool) &&
            generation.Pool.All(card => card.MaskDescriptor?.Definition.CardType == "Equipment") &&
            context?.SummonCatalog != null && context.OtherPiles?.Any(pile => pile.Name == "Standby") == true;
    }
}
