using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitCardCopyProbe
    {
        internal const string Effect = "CardEffectAddUnitCard";
        internal static bool Enabled => OriginalEquipmentCardsScenario.IsolatedIds?.Contains("c71b2d2b-a2c5-449f-8cf5-2abef684ff33") == true;
        internal static CardGenerationRule? Definition(CardEffectData effect, List<string> interactions)
        {
            if (effect.GetEffectStateName() != Effect) return null;
            CardUpgradeModifier? upgrade = null;
            if (effect.GetParamCardUpgradeData() != null)
            {
                var state = new CardUpgradeState(); state.Setup(effect.GetParamCardUpgradeData());
                upgrade = CardModifierProbe.Upgrade(state);
                interactions.AddRange(upgrade.ExternalInteractions);
            }
            return new CardGenerationRule(((CardPile)effect.GetParamInt()).ToString(), Math.Max(1, effect.GetAdditionalParamInt()),
                Array.Empty<CardCreationRule>(), copyModifiers: effect.GetCopyModifiersFromSource(),
                ignoreTemporaryModifiers: effect.GetIgnoreTempModifiersFromSource(), upgrade: upgrade,
                discardCopyUpgrades: CardGenerationProbe.Definition(effect).DiscardCopyUpgrades,
                deterministicPoolSelection: true, permanent: effect.GetParamBool());
        }
    }
}
