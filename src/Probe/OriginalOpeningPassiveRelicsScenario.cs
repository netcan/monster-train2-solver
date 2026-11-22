#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalOpeningPassiveRelicsScenario
    {
        internal const string Name = "original-opening-passive-relics";
        internal const string Boundary = "DeploymentOriginalPassiveRelicComparison";
        private const string TypeCost = nameof(RelicEffectModifyCardTypeCost);
        private const string NoUpgrade = nameof(RelicEffectModifyCardCostIfNoUpgrades);
        private const string Attack = nameof(RelicEffectModifyCardAttack);
        private const string Health = nameof(RelicEffectModifyCardHealth);
        private const string WhelpOwnerId = "72706202-526e-4377-9745-7df40a26ba7d";
        private const string TorchId = "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b";
        private static readonly (string Key, string Id, string[] Effects)[] Owners =
        {
            ("CheaperBlightScourge", "a19cac7c-b2e0-42bd-bc1e-d180e17a1c15", new[] { TypeCost, TypeCost }),
            ("ReduceCostUnitIfNoUpgrade", "f03c7664-0485-4bcf-8bb9-f76321e090c3", new[] { NoUpgrade }),
            ("WhelpDragonAttack_DH", WhelpOwnerId, new[] { Attack, Attack }),
            ("GiveSpellStatsForUpgrades_DH", "4200307a-616d-4cbb-a54e-527c5dc0f492", new[] { Attack }),
            ("GiveUnitStatsForUpgrades_DH", "fc1f6104-b3c1-4714-ac0f-9c76a6fb1a77", new[] { Health, Attack }),
            ("MoreOrLessArtifactsRelic", "79122f7a-6895-443a-830c-2db23027776c", new[] { Attack, Attack })
        };
        internal static string[] OwnerIds { get; } = Owners.Select(owner => owner.Id).Where(id => id != WhelpOwnerId).ToArray();
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool CaptureReady { get; private set; }

        internal static bool MatchesOriginal(CollectableRelicData relic)
        {
            var owner = Owners.Single(item => item.Id == relic.GetID());
            return relic.name == owner.Key && !relic.DisallowInDeploymentPhase &&
                relic.GetEffects().Select(effect => effect.GetEffectClassName()).SequenceEqual(owner.Effects) &&
                relic.GetEffects().All(effect => effect.GetEffectConditions().Count == 0);
        }

        internal static void Run(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (!OriginalOpeningRelicsScenario.Acquired || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Expected original passive relics at settled deployment.");

            var all = save.GetAllGameData();
            RelicManager relics = managers.GetRelicManager();
            FullBattleTrace trace = FullBattleTrace.Active!;
            var cases = new List<object>();
            var subtypeCases = new List<object>();
            string[] expectedOwners = OwnerIds;

            (string? UpgradeId, RelicCardModifierProbe.Record Witness) Observe(string name, string id, bool upgraded)
            {
                CardData definition = all.FindCardData(id);
                if (definition == null || definition.GetAssetKey() != name)
                    throw new InvalidOperationException("Missing original passive relic carrier " + name);
                var card = new CardState(definition, save, setupStartingUpgrades: false);
                string? upgradeId = null;
                if (upgraded)
                {
                    CardUpgradeData original = all.GetAllCardUpgradeData().OrderBy(item => item.GetID(), StringComparer.Ordinal)
                        .First(item => item.GetUpgradeIcon() != null && !item.GetHideUpgradeIconOnCard() && !item.GetIsRegionRunUpgrade() &&
                            item.GetBonusDamage() > 0 &&
                            item.GetTraitDataUpgrades().Count == 0 && item.GetRemoveTraitUpgrades().Count == 0 &&
                            item.GetFilters().All(filter => filter.FilterCard(card, relics)));
                    var upgrade = new CardUpgradeState();
                    upgrade.Setup(original);
                    card.GetCardStateModifiers().AddUpgrade(upgrade);
                    upgradeId = original.GetID();
                }

                int index = RelicCardModifierProbe.Records.Count;
                CaptureReady = true;
                try { relics.ApplyCardStateModifiers(card); }
                finally { CaptureReady = false; }
                var record = RelicCardModifierProbe.Records.Skip(index).Single();
                if (trace.CaptureFailures != 0 || record.Actual == null || record.ContextAfter == null ||
                    !record.Relics.Where(owner => Owners.Any(item => item.Id == owner.DataId)).Select(owner => owner.DataId).SequenceEqual(expectedOwners))
                    throw new InvalidOperationException("Incomplete native passive relic observation for " + name);
                return (upgradeId, record);
            }

            string? Check(List<object> output, string name, string id, bool upgraded, params (int Owner, int Effect, bool Returned)[] expected)
            {
                var (upgradeId, record) = Observe(name, id, upgraded);
                if (record.Difference != null)
                    throw new InvalidOperationException("Original passive relic modifiers differ for " + name + ": " + record.Difference);

                foreach (var (owner, effect, returned) in expected)
                {
                    var dispatch = record.Dispatches.Single(item => record.Relics[item.RelicIndex].DataId == Owners[owner].Id && item.EffectIndex == effect);
                    if (dispatch.Returned != returned)
                        throw new InvalidOperationException("Unexpected native modifier callback for " + name + "/" + Owners[owner].Key);
                    if (owner == 1)
                    {
                        // An upgraded Monster still returns true from ApplyCardStateModifiers.
                        var discounts = record.Actual!.Temporary.Upgrades.Where(item => item.DataId == NoUpgrade).ToArray();
                        bool validDiscount = upgraded ? discounts.Length == 0 :
                            discounts is [{ Unique: true, Stats: { Cost: -1, XCost: 1 }, MaskMetadata: { HasIcon: false } }];
                        bool originalSlot = record.Before.Permanent.Upgrades.Any(item => item.DataId == upgradeId &&
                            item.MaskMetadata is { HasIcon: true, HideIcon: false, RegionRun: false });
                        if (!validDiscount || dispatch.UpgradeAdded != !upgraded || originalSlot != upgraded ||
                            record.Before.Temporary.Upgrades.Any(item => item.DataId == NoUpgrade))
                            throw new InvalidOperationException("Unexpected native no-upgrade discount for " + name + " (upgraded=" + upgraded + ").");
                    }
                }
                output.Add(new { AssetKey = name, OriginalUpgradeId = upgradeId, Witness = record });
                return upgradeId;
            }

            Check(cases, "SelfPurgeBlight", "21038774-c0a4-413c-ab7e-27e458228e5f", false, (0, 0, true), (0, 1, false));
            Check(cases, "ScourgeT1", "62cc5532-5fa0-4e66-9fec-85be573af359", false, (0, 0, false), (0, 1, true));
            Check(cases, "TrainStewardShield", "d14a50f3-728d-43e1-87f0-ef1b013f6678", false, (1, 0, true), (5, 0, true), (5, 1, true));
            Check(cases, "TrainStewardShield", "d14a50f3-728d-43e1-87f0-ef1b013f6678", true, (1, 0, true), (4, 0, true), (4, 1, true));
            string? initialTorchUpgradeId = Check(cases, "StarterTorch", TorchId, true, (3, 0, true));
            Check(cases, "DevilishDetails", "10f64bfb-81cd-4b7c-bf46-6fc1fa832008", true, (3, 0, false));

            CollectableRelicData whelpOwner = all.GetAllCollectableRelicData().Single(item => item.GetID() == WhelpOwnerId);
            if (!MatchesOriginal(whelpOwner) || save.GetCollectedRelics().Any(item => item.GetRelicDataID() == WhelpOwnerId))
                throw new InvalidOperationException("Unexpected original Whelp/Dragon relic before subtype observations.");
            save.AddRelic(whelpOwner);
            expectedOwners = OwnerIds.Append(WhelpOwnerId).ToArray();
            Check(subtypeCases, "RoyalWhelp", "198fb647-d5fc-4fb4-912c-cb8e64789412", false, (2, 0, true), (2, 1, false));
            Check(subtypeCases, "GreedDragon", "0060b286-4716-49e0-9714-3fd7811b9f72", false, (2, 0, false), (2, 1, true));

            var (torchUpgradeId, torch) = Observe("StarterTorch", TorchId, upgraded: true);
            var predicted = RelicCardModifierModel.Apply(torch.Before, torch.Relics, torch.ResetTemporary, torch.ContextBefore);
            if (torchUpgradeId != initialTorchUpgradeId || !predicted.Supported || predicted.Card == null)
                throw new InvalidOperationException("Unable to compare the native StarterTorch cross-card observation.");
            string? crossCardDifference = UnitPlayModelProbe.FirstDifference(JToken.FromObject(predicted.Card), JToken.FromObject(torch.Actual!), "$.Card");
            if (crossCardDifference != "$.Card.Temporary.Offsets.Damage")
                throw new InvalidOperationException("Unexpected StarterTorch cross-card result: " + (crossCardDifference ?? "matched"));

            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 2, Boundary, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId, OwnerIds, Cases = cases, SubtypeOwnerId = WhelpOwnerId,
                SubtypeCases = subtypeCases, UnsupportedCrossCard = new
                {
                    AssetKey = "StarterTorch", OriginalUpgradeId = torchUpgradeId, Witness = torch, PredictedCard = predicted.Card, Difference = crossCardDifference
                }
            });
            using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "opening-passive-relics.mt2f"));
            document.Write(stream);
            log.LogInfo("ORIGINAL-OPENING-PASSIVE-RELICS five ordered owners, two exact Whelp/Dragon subtype witnesses; unsupported StarterTorch cross-card difference " + crossCardDifference);
        }
    }
}
#endif
