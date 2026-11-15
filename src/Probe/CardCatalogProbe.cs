using System;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Definition inspection is not a playability or whole-card acceptance test.
    internal static class CardCatalogProbe
    {
        internal static void Capture(FullBattleTrace trace, AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CARD_CATALOG") != "1") return;
            if (!CardBranchMaskProbe.Enabled)
                throw new InvalidOperationException("Card catalog projections require branch card metadata.");
            MoonPhaseCardUpgradeCalibration.Capture(trace, managers);
            var data = managers.GetSaveManager().GetAllGameData();
            var relics = managers.GetRelicManager();
            var cards = data.GetAllCardData().OrderBy(card => card.GetID(), StringComparer.Ordinal).ToArray();
            if (cards.Length == 0 || cards.Any(card => string.IsNullOrEmpty(card.GetID())) ||
                cards.Select(card => card.GetID()).Distinct().Count() != cards.Length)
                throw new InvalidOperationException("Native card catalog is empty or has missing/duplicate identities.");
            JToken before = JToken.FromObject(trace.CaptureDecision());
            JObject beforeRng = RngStates();
            int frame = UnityEngine.Time.frameCount;
            var entries = cards.Select(card =>
            {
                CardPlayRule? rule = null;
                string? error = null;
                try { rule = BattleActionProbe.Definition(card); }
                catch (Exception exception) { error = exception.GetType().Name + ": " + exception.Message; }
                return new
                {
                    Id = card.GetID(), AssetKey = card.name, Type = card.GetCardType().ToString(),
                    Rarity = card.GetRarity().ToString(), LinkedClan = card.GetLinkedClassID(),
                    AppearsInLogbook = card.ShouldAppearInLogbook(), CountsTowardsMastery = card.ShouldCountTowardsMastery(),
                    IsUnitAbility = card.IsUnitAbility(), IsRoomAbility = card.IsRoomAbility(), Archived = card.IsArchived,
                    SpawnCharacter = card.GetSpawnCharacterData()?.GetAssetKey(),
                    Effects = card.GetEffects().Select(RuleCatalogProbe.Effect).ToArray(),
                    Traits = card.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(),
                    Triggers = card.GetCardTriggers().Select(trigger => new
                    {
                        Kind = trigger.GetTrigger().ToString(), Effects = trigger.GetCardEffects().Select(RuleCatalogProbe.Effect).ToArray()
                    }).ToArray(),
                    Mask = CardUpgradeMaskCalibration.Card(card, relics), Rule = rule, ProjectionError = error
                };
            }).ToArray();
            var moonUpgrades = cards.SelectMany(card => card.GetTraits().Select((trait, index) => new { card, trait, index }))
                .Where(item => item.trait.GetTraitStateName() == "CardTraitAddCardUpgradeOnMoonPhase")
                .Select(item =>
                {
                    var upgrade = new CardUpgradeState(); upgrade.Setup(item.trait.GetCardUpgradeDataParam());
                    upgrade.SetAvoidClobberingExistingTraits(true);
                    return new { CardDataId = item.card.GetID(), TraitIndex = item.index,
                        Upgrade = CardModifierProbe.Upgrade(upgrade) };
                }).ToArray();
            var characterRoomModifiers = cards.Where(card => card.GetSpawnCharacterData()?.GetRoomModifiersData().Count > 0)
                .Select(card => new { CardDataId = card.GetID(), Character = card.GetSpawnCharacterData().GetAssetKey(),
                    Modifiers = card.GetSpawnCharacterData().GetRoomModifiersData().Select(RuleCatalogProbe.RoomModifier).ToArray() }).ToArray();
            var equipmentRoomModifiers = cards.SelectMany(card => card.GetEffects().Select((effect, index) => new { card, effect, index }))
                .Where(item => item.effect.GetEffectStateName() == "CardEffectAttachEquipment" &&
                    item.effect.GetParamCardUpgradeData()?.GetRoomModifierUpgrades().Count > 0)
                .Select(item => new { CardDataId = item.card.GetID(), EffectIndex = item.index,
                    UpgradeDataId = item.effect.GetParamCardUpgradeData().GetID(),
                    UpgradeAssetKey = item.effect.GetParamCardUpgradeData().name,
                    Modifiers = item.effect.GetParamCardUpgradeData().GetRoomModifierUpgrades().Select(RuleCatalogProbe.RoomModifier).ToArray() }).ToArray();
            var nativeCardTriggers = cards.Where(card => card.GetCardTriggers().Any(trigger => trigger.GetTriggerEffects().Count > 0))
                .Select(card => new { CardDataId = card.GetID(), Triggers = card.GetCardTriggers().Select(trigger => new
                {
                    Kind = trigger.GetTrigger().ToString(), Effects = trigger.GetTriggerEffects().Select(effect => new
                    { Type = ShinyShoe.TypeNameCache.GetType(effect.cardTriggerEffect).Name, Amount = effect.paramInt,
                        Persistence = effect.persistenceMode.ToString(), BuffEffectType = effect.buffEffectType,
                        UpgradeDataId = effect.paramUpgrade?.GetID() }).ToArray()
                }).ToArray() }).ToArray();
            var killTriggers = cards.Where(card => card.GetCardTriggers().Any(trigger => trigger.GetTrigger() == CardTriggerType.OnKill))
                .Select(card => new { CardDataId = card.GetID(), Triggers = card.GetCardTriggers()
                    .Where(trigger => trigger.GetTrigger() == CardTriggerType.OnKill).Select(CardUpgradeLifecycleProbe.TriggerDefinition).ToArray() }).ToArray();
            object[] punishedChampions = PunishedChampionDefinitionProbe.Capture(data);
            JObject afterRng = RngStates();
            bool unchanged = JToken.DeepEquals(before, JToken.FromObject(trace.CaptureDecision()));
            if (!unchanged || UnityEngine.Time.frameCount != frame || !JToken.DeepEquals(beforeRng, afterRng))
                throw new InvalidOperationException("Card catalog inspection changed native decision state, RNG or frame.");
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Scope = "AllGameData", GameVersion = UnityEngine.Application.version,
                BranchCardMasksEnabled = CardBranchMaskProbe.Enabled,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = beforeRng, RngAfter = afterRng,
                DecisionUnchanged = unchanged, NativeDefinitionCount = cards.Length,
                Cards = entries, MoonPhaseUpgradeDefinitions = moonUpgrades,
                CharacterRoomModifierDefinitions = characterRoomModifiers, EquipmentRoomModifierDefinitions = equipmentRoomModifiers,
                CardTriggerEffectDefinitions = nativeCardTriggers,
                CardKillTriggerDefinitions = killTriggers,
                PunishedChampionUpgradeDefinitions = punishedChampions
            }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "card-catalog.mt2f")))
                document.Write(output);
        }

        private static JObject RngStates()
        {
            var states = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
                if (id != RngId.NonDeterministic && id != RngId.Chatter)
                    states[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            return states;
        }
    }
}
