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
            var data = managers.GetSaveManager().GetAllGameData();
            var relics = managers.GetRelicManager();
            var cards = data.GetAllCardData().OrderBy(card => card.GetID(), StringComparer.Ordinal).ToArray();
            if (cards.Length != 668 || cards.Select(card => card.GetID()).Distinct().Count() != cards.Length)
                throw new InvalidOperationException("Expected the complete 668-definition original card catalog.");
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
                    Traits = card.GetTraits().Select(trait => new
                    {
                        Type = trait.GetTraitStateName(), Int = trait.GetParamInt(), Int2 = trait.GetParamInt2(),
                        Int3 = trait.GetParamInt3(), Bool = trait.GetParamBool(), Str = trait.GetParamStr(),
                        Float = trait.GetParamFloat(), Scaling = trait.GetUseScalingParams(),
                        TrackedValue = trait.GetParamTrackedValue().ToString(), EntryDuration = trait.GetParamEntryDuration().ToString()
                    }).ToArray(),
                    Triggers = card.GetCardTriggers().Select(trigger => new
                    {
                        Kind = trigger.GetTrigger().ToString(), Effects = trigger.GetCardEffects().Select(RuleCatalogProbe.Effect).ToArray()
                    }).ToArray(),
                    Mask = CardUpgradeMaskCalibration.Card(card, relics), Rule = rule, ProjectionError = error
                };
            }).ToArray();
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
                DecisionUnchanged = unchanged, Cards = entries
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
