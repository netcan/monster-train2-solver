#if DEVELOPMENT_PROBE
using System;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFullCostReductionWitness
    {
        internal const string Name = "original-full-cost-reduction";
        private const string FecundityOwner = "d968e210-0d44-47c9-b783-f0c90dfe757b";
        private const string FecundityUpgrade = "16e75365-ef56-4525-95c1-3937844aad49";
        private const string FreeOwner = "128dd519-611c-4999-b7ac-e63ea027b1fa";
        private const string FreeUpgrade = "988193e9-2933-4c84-a957-71b97809170e";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;

        internal static void Capture(AllGameManagers managers, ManualLogSource log)
        {
            var trace = FullBattleTrace.Active!;
            SaveManager save = managers.GetSaveManager();
            if (UnityEngine.Application.version != "2.3.1" ||
                typeof(CardState).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Expected original 2.3.1 deployment.");
            var all = save.GetAllGameData();
            CardUpgradeData OriginalUpgrade(string ownerId, string ownerKey, string effectName, string upgradeId)
            {
                RelicData owner = RelicCatalogProbe.OriginalOwners(all).Select(entry => entry.Data)
                    .Distinct().Single(candidate => candidate.GetID() == ownerId);
                CardUpgradeData? upgrade = owner.GetEffects().Single(effect => effect.GetEffectClassName() == effectName).GetParamCardUpgradeData();
                if (owner.name != ownerKey || upgrade?.GetID() != upgradeId)
                    throw new InvalidOperationException("Original upgrade identity changed.");
                return upgrade;
            }
            CardUpgradeData fecundity = OriginalUpgrade(FecundityOwner, "InscryptionEventFecundityCardUpgrade",
                nameof(RelicEffectCardUpgrade), FecundityUpgrade);
            CardUpgradeData zero = OriginalUpgrade(FreeOwner, "ReduceRandomCardCostOnUnitPlay",
                nameof(RelicEffectAddTempUpgradeToRandomCardInHand), FreeUpgrade);
            CardData steward = all.FindCardData(StewardId)!;
            if (steward.GetCardType() != CardType.Monster || steward.GetCostType() != CardData.CostType.Default ||
                steward.GetCost() is <= 0 or >= 99 || zero.GetCostReduction() != 99 ||
                fecundity.GetTraitDataUpgrades() is not [var trait] ||
                trait.GetTraitStateName() != nameof(CardTraitResetFullCostReductionUpgrades))
                throw new InvalidOperationException("Original cost-reduction definitions changed.");

            object Definitions()
            {
                var originalFecundity = new CardUpgradeState();
                originalFecundity.Setup(fecundity);
                var originalZero = new CardUpgradeState();
                originalZero.Setup(zero);
                return new { FecundityOwner, FreeOwner, Fecundity = CardModifierProbe.Upgrade(originalFecundity, rejectFilters: false),
                    FullReduction = CardModifierProbe.Upgrade(originalZero, rejectFilters: false) };
            }

            CardManager cards = managers.GetCardManager();
            ICoreGameManagers core = managers.GetCoreManagers();
            var targets = cards.GetHand().Where(card => card.GetCardDataID() == StewardId &&
                card.GetCardStateModifiers().GetCardUpgrades().Count == 0 &&
                card.GetTemporaryCardStateModifiers().GetCardUpgrades().Count == 0).Take(4).ToList();
            while (targets.Count < 4)
                targets.Add(cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false) ??
                    throw new InvalidOperationException("Could not add original Train Steward to this isolated hand."));
            CardState plain = targets[0], targetOwner = targets[1], sourceOwner = targets[2], sourceTarget = targets[3];
            if (!CardUpgradeHelper.TryApplyUpgradeToCard(targetOwner, null, fecundity, false, false, core, out _) ||
                !CardUpgradeHelper.TryApplyUpgradeToCard(sourceOwner, null, fecundity, false, false, core, out _) ||
                !new[] { plain, targetOwner, sourceOwner, sourceTarget }.All(cards.GetHand().Contains))
                throw new InvalidOperationException("Original Fecundity trait setup failed.");

            JObject definition = JObject.FromObject(Definitions());
            int Cost(CardState card) => card.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(), managers.GetRelicManager());
            object Sample(string role, CardState target, CardState? source)
            {
                int nativeCost = Cost(target);
                var before = trace.CaptureDecision();
                if (nativeCost is <= 0 or >= 99 || !CardUpgradeHelper.TryApplyUpgradeToCard(target, source, zero,
                    isTemporaryUpgrade: true, isFromHiddenTrigger: false, core, out CardUpgradeState applied))
                    throw new InvalidOperationException("Native full-reduction upgrade was rejected.");
                var actual = trace.CaptureDecision();
                return new { Role = role, TargetId = trace.CardId(target), SourceId = source == null ? 0 : trace.CardId(source),
                    NativeCostBefore = nativeCost, NativeCostAfter = Cost(target), Applied = CardModifierProbe.Upgrade(applied, rejectFilters: false),
                    Before = before, Actual = actual };
            }

            int frame = UnityEngine.Time.frameCount;
            object[] cases = { Sample("Neither", plain, null), Sample("Target", targetOwner, null),
                Sample("ExplicitSource", sourceTarget, sourceOwner) };
            JObject definitionAfter = JObject.FromObject(Definitions());
            if (frame != UnityEngine.Time.frameCount || !JToken.DeepEquals(definition, definitionAfter))
                throw new InvalidOperationException("Native upgrade callback changed its authored definitions or crossed a frame.");
            using var document = NativeFixtureCapture.Capture(new { Schema = 1, Boundary = "OriginalFullCostReduction",
                GameVersion = UnityEngine.Application.version, GameModuleMvid = typeof(CardState).Module.ModuleVersionId,
                Definition = definition, DefinitionAfter = definitionAfter, Cases = cases });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "full-cost-reduction.mt2f"));
            document.Write(stream);
            log.LogInfo("ORIGINAL-FULL-COST-REDUCTION sampled native paid costs and applied reductions.");
        }
    }
}
#endif
