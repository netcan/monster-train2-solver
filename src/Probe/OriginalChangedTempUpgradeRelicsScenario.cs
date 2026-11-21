using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalChangedTempUpgradeRelicsScenario
    {
        internal const string Name = "original-changed-temp-upgrade-relics";
        internal const string Boundary = "DeploymentOriginalCardModifierComparison";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }

        internal static void Run(AllGameManagers managers, ManualLogSource log)
        {
            try
            {
                SaveManager save = managers.GetSaveManager();
                if (!Enabled || Completed || save.PreviewMode || managers.GetCombatManager()!.GetTurnCount() != 0 ||
                    typeof(CardState).Assembly.ManifestModule.ModuleVersionId.ToString() != "bf93f060-beda-46b1-b808-95ebf1e9a99d")
                    throw new InvalidOperationException("Expected settled 2.3.1 deployment for original temporary relic upgrades.");
                AllGameData data = save.GetAllGameData();
                RelicManager relics = managers.GetRelicManager();
                var results = new List<object>();
                var originalOwners = new[]
                {
                    ("fb6332bb-3e1c-44ba-951b-39fbc8d43601", "NonBannerUnitsHaveDeployable", new[]
                    {
                        ("TrainStewardSpear", "38e70e6a-20f3-4e36-8d31-c5110be07f87"),
                        ("ChainBearer", "a7e84160-5e7b-4fd7-9227-7bf8f766454c")
                    }),
                    ("d892106c-dbbc-40ac-bf0b-902882cdc69d", "ReduceStarterCost", new[]
                    {
                        ("CruelLesson", "0a63cd82-9aee-4991-98af-4cc4233ed77e"),
                        ("StarterSharedSentence", "de1ef23c-c6bc-4d84-9df5-dac93a4b6a4f"),
                        ("HornBreak", "5a88d0e1-1c31-40f0-9e8d-8068dad832e9")
                    }),
                    ("f141d334-b503-48ff-ac21-5f2ad27105dd", "MoltenManifold", new[]
                    {
                        ("Vent", "a54a1fa9-8cf3-4be2-a9c6-d0e50bb82c7d"),
                        ("StarterTorch", "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b")
                    }),
                    ("374995fd-7619-4244-9d1f-269429258584", "MoreRares_Relic", new[]
                    {
                        ("TrainStewardSpear", "38e70e6a-20f3-4e36-8d31-c5110be07f87"),
                        ("MorselMiner", "536e6de0-7ccf-412a-9a4e-ec55ab3988e5")
                    })
                };
                foreach (var (ownerId, ownerKey, choices) in originalOwners)
                {
                    CollectableRelicData original = data.GetAllCollectableRelicData().Single(item => item.GetID() == ownerId);
                    RelicEffectData effectData = original.GetEffects().Single();
                    if (original.name != ownerKey || effectData.GetEffectClassName() != RelicModel.AddTempUpgrade ||
                        effectData.GetParamSourceTeam() != Team.Type.Monsters || effectData.GetEffectConditions().Count != 0)
                        throw new InvalidOperationException("Unexpected original temporary upgrade owner " + ownerKey);
                    var effect = new RelicEffectAddTempUpgrade();
                    effect.Initialize(new RelicState(), original, effectData);
                    CardUpgradeMaskData[] filters = effectData.GetParamCardUpgradeData().GetFilters().ToArray();
                    var owner = new CombatRelicState(ownerId, ownerKey, new[] { effectData.GetEffectClassName() },
                        isCovenant: false, disallowedInPlacementPhase: original.DisallowInDeploymentPhase,
                        cardModifiers: new[] { RelicCardModifierProbe.Definition(effect, 0) });
                    foreach (var (assetKey, dataId) in choices)
                    {
                        CardData definition = data.FindCardData(dataId);
                        if (definition == null || definition.GetAssetKey() != assetKey)
                            throw new InvalidOperationException("Missing original modifier control " + assetKey);
                        var card = new CardState(definition, save);
                        bool[] definitionFilters = filters.Select(filter => filter.FilterCard(definition, relics)).ToArray();
                        bool[] ownedFilters = filters.Select(filter => filter.FilterCard(card, relics)).ToArray();
                        CardInstanceState before = CardModifierProbe.Capture(new[] { card }, _ => 1).Single();
                        bool returned = effect.ApplyCardStateModifiers(card, relics, save, null);
                        CardInstanceState actual = CardModifierProbe.Capture(new[] { card }, _ => 1).Single();
                        var ignored = new List<string>();
                        card.GetTemporaryCardStateModifiers().TryGetIgnoredUpgrades(ignored);
                        results.Add(new { AssetKey = assetKey, DataId = dataId, Owner = owner,
                            DefinitionFilters = definitionFilters, OwnedFilters = ownedFilters, Returned = returned,
                            Before = before, Actual = actual, ExcludedCopyUpgradeIds = ignored });
                    }
                }
                using var document = NativeFixtureCapture.Capture(new
                {
                    Schema = 1, Boundary, GameVersion = UnityEngine.Application.version,
                    GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, Cases = results
                });
                using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                    "changed-temp-upgrade-relics.mt2f"));
                document.Write(stream);
                Completed = true;
                log.LogInfo($"ORIGINAL-CHANGED-TEMP-UPGRADE-RELICS PASS cases={results.Count}");
            }
            catch (Exception error)
            {
                Error = error.ToString();
                log.LogError(Error);
            }
        }
    }
}
