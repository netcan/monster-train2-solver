#if DEVELOPMENT_PROBE
using System;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBigMonstersCreationWitness
    {
        internal const string ArchiveName = "big-monsters-creation.mt2f";
        private const string OwnerId = "af623737-a208-4b85-bce3-e1066b4f4bcd";
        private const string ShadowId = "4dd2be98-c73c-4f96-865f-6c24cc42e91d";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_BIG_MONSTERS_CREATION") == "1";

        internal static void Write(AllGameManagers managers, FullBattleTrace trace)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager();
            if (Application.version != "2.3.1" ||
                typeof(SaveManager).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || save.InTestScenario || save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0 ||
                save.GetMutators().Count != 0 || combat.GetTurnCount() != 1 || combat.IsPlacementPhase)
                throw new InvalidOperationException("Expected a settled stock 2.3.1 Ring 1 first ordinary turn.");

            var all = save.GetAllGameData();
            MutatorData owner = all.FindMutatorData(OwnerId) ?? throw new InvalidOperationException("Original BigMonsters is missing.");
            RelicEffectData rule = owner.GetEffects().Single();
            if (owner.name != "BigMonsters" || owner.DisallowInDeploymentPhase || !save.IsDlcAvailableWhenStartingRun(owner.GetRequiredDLC()) ||
                rule.GetEffectClassName() != nameof(RelicEffectModifyMonsterCapacity) || rule.GetEffectConditions().Count != 0 ||
                rule.GetParamInt() != 1 || !rule.GetParamBool() || !string.IsNullOrEmpty(rule.GetParamString()) ||
                rule.GetParamCharacterSubtype()?.IsNone != true)
                throw new InvalidOperationException("Original BigMonsters definition changed.");

            CardData large = all.FindCardData(ShadowId) ?? throw new InvalidOperationException("Original Shadowsiege is missing.");
            CardData ordinary = all.FindCardData(MagneticRelayScenario.StewardId) ?? throw new InvalidOperationException("Original Train Steward is missing.");
            if (large.name != "Shadowsiege" || large.IsArchived || large.GetSpawnCharacterData()?.GetSize() != 6 ||
                ordinary.GetSpawnCharacterData()?.GetSize() != 3 ||
                !new[] { large, ordinary }.All(card => card.GetCardType() == CardType.Monster &&
                    card.GetEffects().Count(effect => effect.GetEffectStateName() == nameof(CardEffectSpawnMonster)) == 1))
                throw new InvalidOperationException("Original unit card controls changed.");

            EnhancerData enhancer = all.FindEnhancerDataByName("UnitUpgradeMakeTiny")
                ?? throw new InvalidOperationException("Original Tiny Stone enhancer is missing.");
            ClassData? clan = enhancer.GetLinkedClass();
            if (clan != null && !save.IsDlcAvailableWhenStartingRun(clan.GetRequiredDlc()))
                throw new InvalidOperationException("Original Tiny Stone enhancer requires owned content.");
            CardUpgradeData upgrade = enhancer.GetEffects().Single().GetParamCardUpgradeData()
                ?? throw new InvalidOperationException("Original Tiny Stone card upgrade is missing.");
            if (upgrade.GetBonusSize() >= 0)
                throw new InvalidOperationException("Expected a negative size modifier from original Tiny Stone.");

            object rngBefore = HalfHealthLifecycleCalibration.Rng();
            CardState[] originalDeck = save.GetDeckState().ToArray();
            var control = new[] { Create(large, null), Create(large, upgrade), Create(ordinary, null) };
            save.AddMutator(owner);
            if (save.GetMutators().Single().GetRelicDataID() != OwnerId ||
                save.GetMutators().Single().GetEffects() is not [RelicEffectModifyMonsterCapacity])
                throw new InvalidOperationException("Original BigMonsters runtime effect did not attach.");
            var active = new[] { Create(large, null), Create(large, upgrade), Create(ordinary, null) };
            object rngAfter = HalfHealthLifecycleCalibration.Rng();
            if (!originalDeck.SequenceEqual(save.GetDeckState()) ||
                !JToken.DeepEquals(HalfHealthLifecycleCalibration.GameplayRng(rngBefore), HalfHealthLifecycleCalibration.GameplayRng(rngAfter)))
                throw new InvalidOperationException("Controlled native card creation changed the permanent deck or gameplay RNG.");
            if (control[0].Size != 6 || active[0].Size != 6 ||
                control[1].Size >= 6 || active[1].Size != control[1].Size ||
                control[2].Size != 3 || active[2].Size != 4 ||
                control.Concat(active).Select(row => row.RegistryId).Distinct().Count() != 6)
                throw new InvalidOperationException("Native original creation sizes or upgrade order differed: " +
                    string.Join(", ", control.Concat(active).Select(row => row.Size)));

            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 2, Boundary = "NativeBigMonstersCardCreation", GameVersion = Application.version,
                GameModuleMvid = typeof(SaveManager).Module.ModuleVersionId, Turn = combat.GetTurnCount(),
                Owner = new { Id = owner.GetID(), Key = owner.name, Type = rule.GetEffectClassName(), Int = rule.GetParamInt(), All = rule.GetParamBool() },
                Enhancer = new { Id = enhancer.GetID(), Key = enhancer.name, UpgradeId = upgrade.GetID(), UpgradeKey = upgrade.name, Size = upgrade.GetBonusSize() },
                Control = control, Active = active, RngBefore = rngBefore, RngAfter = rngAfter
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, ArchiveName));
            archive.Write(output);

            Sample Create(CardData definition, CardUpgradeData? applied)
            {
                CardManager cards = managers.GetCardManager();
                var info = applied == null ? null : new CardManager.AddCardUpgradingInfo { upgradeDatas = { applied } };
                CardUpgradeModifier? upgradeModifier = null;
                if (applied != null)
                {
                    var state = new CardUpgradeState();
                    state.Setup(applied);
                    upgradeModifier = CardModifierProbe.Upgrade(state, rejectFilters: false, filtersAlreadyApplied: true);
                }
                var creation = new CardGenerationRule("DiscardPile", 1, new[] { CardGenerationProbe.Creation(definition) },
                    upgrade: upgradeModifier, upgradeIsPermanent: true, permanent: false, selectedPoolIndex: 0);
                CombatContext before = trace.CaptureDecision().Spawn.Train.Context!;
                CardState created = cards.AddNewCard(definition, CardPile.DiscardPile, fromRelic: false, permanent: false,
                    addCardUpgradingInfo: info!, animate: false);
                if (created == null || created.GetCardDataID() != definition.GetID())
                    throw new InvalidOperationException("Native CardManager did not create the controlled original unit card.");
                var permanent = created.GetCardStateModifiers();
                var temporary = created.GetTemporaryCardStateModifiers();
                string[] permanentUpgrades = permanent.GetCardUpgrades().Select(value => value.GetCardUpgradeDataId()).ToArray();
                if (applied != null && permanentUpgrades.Count(id => id == applied.GetID()) != 1)
                    throw new InvalidOperationException("Native CardManager did not apply the original size upgrade.");
                int id = trace.CardId(created);
                CombatContext after = trace.CaptureDecision().Spawn.Train.Context!;
                return new Sample(id, definition.GetID(), created.GetSize(), created.GetSize(ignoreTempUpgrade: true),
                    permanent.GetAdditionalSize().ToArray(), temporary.GetAdditionalSize().ToArray(), permanentUpgrades,
                    creation, before, after);
            }
        }

        private sealed class Sample
        {
            public int RegistryId { get; }
            public string DataId { get; }
            public int Size { get; }
            public int SizeWithoutTemporaryModifiers { get; }
            public int[] PermanentSizeModifiers { get; }
            public int[] TemporarySizeModifiers { get; }
            public string[] PermanentUpgrades { get; }
            public CardGenerationRule Rule { get; }
            public CombatContext Before { get; }
            public CombatContext After { get; }

            public Sample(int registryId, string dataId, int size, int sizeWithoutTemporaryModifiers,
                int[] permanentSizeModifiers, int[] temporarySizeModifiers, string[] permanentUpgrades,
                CardGenerationRule rule, CombatContext before, CombatContext after)
            {
                RegistryId = registryId;
                DataId = dataId;
                Size = size;
                SizeWithoutTemporaryModifiers = sizeWithoutTemporaryModifiers;
                PermanentSizeModifiers = permanentSizeModifiers;
                TemporarySizeModifiers = temporarySizeModifiers;
                PermanentUpgrades = permanentUpgrades;
                Rule = rule;
                Before = before;
                After = after;
            }
        }
    }
}
#endif
