using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalNoUpgradeCardCostChecks
{
    private const int Schema = 212;
    private const string OwnerId = "f03c7664-0485-4bcf-8bb9-f76321e090c3";
    private const string AssetKey = "ReduceCostUnitIfNoUpgrade";
    private const string Effect = "RelicEffectModifyCardCostIfNoUpgrades";
    private const string MonsterId = "fc119000-0000-4000-8000-00000000052001";
    private const string SpellId = "fc119000-0000-4000-8000-00000000052002";
    private const string EquipmentId = "fc119000-0000-4000-8000-00000000052003";

    public sealed class Callback(string kind, CardInstanceState before, CardInstanceState? after, bool? returned,
        bool? modelReturned, bool upgradeAdded, bool? modelUpgradeAdded, bool modelSupported,
        string? difference, string? error)
    {
        public string Kind { get; } = kind;
        public CardInstanceState Before { get; } = before;
        public CardInstanceState? After { get; } = after;
        public bool? Returned { get; } = returned;
        public bool? ModelReturned { get; } = modelReturned;
        public bool UpgradeAdded { get; } = upgradeAdded;
        public bool? ModelUpgradeAdded { get; } = modelUpgradeAdded;
        public bool ModelSupported { get; } = modelSupported;
        public string? Difference { get; } = difference;
        public string? Error { get; } = error;
    }

    public sealed class Dispatch(int relicIndex, int effectIndex, bool returned, bool upgradeAdded)
    {
        public int RelicIndex { get; } = relicIndex;
        public int EffectIndex { get; } = effectIndex;
        public bool Returned { get; } = returned;
        public bool UpgradeAdded { get; } = upgradeAdded;
    }

    public sealed class ManagerRecord(CardInstanceState before, CombatContext contextBefore,
        CombatRelicState[] relics, bool resetTemporary, CardInstanceState? actual,
        Dispatch[] dispatches, string? difference)
    {
        public CardInstanceState Before { get; } = before;
        public CombatContext ContextBefore { get; } = contextBefore;
        public CombatRelicState[] Relics { get; } = relics;
        public bool ResetTemporary { get; } = resetTemporary;
        public CardInstanceState? Actual { get; } = actual;
        public Dispatch[] Dispatches { get; } = dispatches;
        public string? Difference { get; } = difference;
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == Schema &&
            root.GetProperty("OriginalNoUpgradeCardCostSchema").GetInt32() == Schema &&
            root.GetProperty("ModifierScenario").GetString() == "original-reduce-cost-unit-no-upgrade" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean(),
            "Unupgraded-card cost battle is incomplete or from an unexpected schema.");
        foreach (string field in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(field).GetInt32() == 0, "Unupgraded-card cost battle has nonzero " + field + ".");

        FixtureValue evidence = root.GetProperty("OriginalNoUpgradeCardCost");
        FixtureValue definition = evidence.GetProperty("Definition");
        Require(definition.GetProperty("OwnerId").GetString() == OwnerId &&
            definition.GetProperty("AssetKey").GetString() == AssetKey &&
            definition.GetProperty("EffectType").GetString() == Effect &&
            !definition.GetProperty("DisallowedInPlacementPhase").GetBoolean() &&
            definition.GetProperty("ParamInt").GetInt32() == -1 &&
            definition.GetProperty("ParamCardType").GetString() == "Monster" &&
            definition.GetProperty("ConditionCount").GetInt32() == 0,
            "Native ReduceCostUnitIfNoUpgrade owner parameters changed.");

        ManagerRecord[] managerRecords = root.GetProperty("RelicCardModifiers").EnumerateArray()
            .Select(item => item.Deserialize<ManagerRecord>()!)
            .Where(item => item.Before.DataId is MonsterId or SpellId or EquipmentId).ToArray();
        Require(managerRecords.Length == 6 && managerRecords.All(item => item.Actual != null && item.Difference == null),
            "RelicManager reset/no-reset card-modifier observations are incomplete or divergent.");
        CombatRelicState[] capturedOwners = managerRecords.SelectMany(item => item.Relics)
            .Where(relic => relic.DataId == OwnerId).ToArray();
        Require(capturedOwners.Length == managerRecords.Length && capturedOwners.All(item =>
            ModelJson.Difference(JsonSerializer.Serialize(capturedOwners[0], ModelJson.Options),
                JsonSerializer.Serialize(item, ModelJson.Options)) == null),
            "ReduceCostUnitIfNoUpgrade payload is missing or changed between manager observations.");
        CombatRelicState owner = capturedOwners[0];
        RelicCardModifier modifier = owner.CardModifiers!.Single();
        RelicNoUpgradeCardCostModifier rule = modifier.NoUpgradeCost!;
        Require(owner.DataId == OwnerId && owner.AssetKey == AssetKey && owner.IsCovenant == false &&
            owner.DisallowedInPlacementPhase == false && rule.EffectIndex == 0 &&
            rule.ExclusiveType == "Monster" && rule.CostChangeAmount == -1 &&
            rule.Upgrade.DataId == Effect && rule.Upgrade.Unique && rule.Upgrade.Stats.Cost == -1 &&
            rule.Upgrade.Stats.XCost == 1 && rule.Upgrade.MaskMetadata is { HasIcon: false, HideIcon: false, RegionRun: false } &&
            RelicCardModifierModel.Apply(new CardInstanceState(1, "validation", CardModifiers.Empty(), CardModifiers.Empty(),
                0, 0, 0, [], maskDescriptor: new CardMaskDescriptor(
                    new CardMaskDefinition("validation", "Monster", "Common", false, false, false,
                        [], [], [], [], null, false, 1, 0, false, false, false), 1, [], [])),
                [owner], resetTemporary: false).Supported,
            "Captured owner state or its hidden -1 Cost/+1 XCost upgrade is not modeled.");

        FixtureValue cards = evidence.GetProperty("Cards");
        Require(cards.GetProperty("MonsterId").GetString() == MonsterId &&
            cards.GetProperty("SpellId").GetString() == SpellId &&
            cards.GetProperty("EquipmentId").GetString() == EquipmentId &&
            !string.IsNullOrEmpty(cards.GetProperty("VisibleUpgradeId").GetString()) &&
            cards.GetProperty("VisibleUpgradeShowsIcon").GetBoolean() &&
            !cards.GetProperty("VisibleUpgradeRegionRun").GetBoolean(),
            "Synthetic card IDs or visible permanent-upgrade control changed.");

        Callback[] callbacks = evidence.GetProperty("Callbacks").EnumerateArray()
            .Select(item => item.Deserialize<Callback>()!).ToArray();
        Require(callbacks.Length >= 5 && callbacks.All(item => item.After != null && item.ModelSupported &&
            item.Error == null && item.Difference == null),
            "Native hand-added/permanent-upgrade callback captures are missing, unsupported or divergent.");
        foreach (Callback callback in callbacks)
        {
            RelicCardModifierResult predicted = RelicCardModifierModel.Apply(callback.Before, [owner], resetTemporary: false);
            Require(predicted.Supported && Same(predicted.Card!, callback.After!),
                "Independent callback replay differs for " + callback.Kind + " on card " + callback.Before.DataId + ".");
            RelicCardModifierDispatch dispatch = predicted.Dispatches.Single();
            Require(callback.ModelReturned == dispatch.Returned && callback.ModelUpgradeAdded == dispatch.UpgradeAdded &&
                (!callback.Returned.HasValue || callback.Returned.Value == dispatch.Returned) &&
                callback.UpgradeAdded == dispatch.UpgradeAdded,
                "Callback return or temporary-upgrade transition differs from the model.");
        }

        Require(callbacks.Any(item => item.Kind == "OnCardAdded" && item.Before.DataId == MonsterId &&
                TemporaryCount(item.Before) == 0 && TemporaryCount(item.After!) == 1 &&
                item.Returned == true && item.UpgradeAdded && item.ModelUpgradeAdded == true),
            "OnCardAdded did not reinstall the missing Monster cost modifier exactly once.");
        Require(callbacks.Any(item => item.Kind == "OnCardAdded" && item.Before.DataId == MonsterId &&
                TemporaryCount(item.Before) == 1 && TemporaryCount(item.After!) == 1 &&
                item.Returned == true && !item.UpgradeAdded),
            "An already-installed Monster cost modifier was not idempotent.");
        Require(callbacks.Any(item => item.Kind == "OnCardAdded" && item.Before.MaskDescriptor!.Definition.CardType == "Spell" &&
                item.Returned == false && TemporaryCount(item.Before) == TemporaryCount(item.After!)) &&
            callbacks.Any(item => item.Kind == "OnCardAdded" && item.Before.MaskDescriptor!.Definition.CardType == "Equipment" &&
                item.Returned == false && TemporaryCount(item.Before) == TemporaryCount(item.After!)),
            "Spell or Equipment negative controls received the Monster-only cost modifier.");
        Require(callbacks.Any(item => item.Kind == "OnPermanentCardUpgradeApplied" && item.Before.DataId == MonsterId &&
                item.Before.Permanent.Upgrades.Any(upgrade => upgrade.MaskMetadata is { HasIcon: true, HideIcon: false, RegionRun: false }) &&
                TemporaryCount(item.Before) == 1 && TemporaryCount(item.After!) == 0 && item.Returned == null),
            "A visible permanent upgrade did not remove the temporary unupgraded-card cost modifier.");

        CardInstanceState handBefore = cards.GetProperty("HandAddedBefore").Deserialize<CardInstanceState>()!;
        CardInstanceState handAfter = cards.GetProperty("HandAddedAfter").Deserialize<CardInstanceState>()!;
        CardInstanceState permanentBefore = cards.GetProperty("PermanentCallbackBefore").Deserialize<CardInstanceState>()!;
        CardInstanceState permanentAfter = cards.GetProperty("PermanentCallbackAfter").Deserialize<CardInstanceState>()!;
        Require(handBefore.DataId == MonsterId && TemporaryCount(handBefore) == 0 && TemporaryCount(handAfter) == 1 &&
            callbacks.Any(item => item.Kind == "OnCardAdded" && item.Before.DataId == MonsterId &&
                TemporaryCount(item.Before) == 0 && Same(item.Before, handBefore)) &&
            permanentBefore.DataId == MonsterId && TemporaryCount(permanentBefore) == 1 && TemporaryCount(permanentAfter) == 0,
            "Scenario-level native callback snapshots disagree with the callback probe.");

        foreach (ManagerRecord managerRecord in managerRecords)
        {
            RelicCardModifierResult predicted = RelicCardModifierModel.Apply(managerRecord.Before,
                managerRecord.Relics, managerRecord.ResetTemporary, managerRecord.ContextBefore);
            Require(predicted.Supported && Same(predicted.Card!, managerRecord.Actual!),
                "Independent RelicManager card-modifier replay differs for " + managerRecord.Before.DataId + ".");
            int ownerIndex = Array.FindIndex(managerRecord.Relics, relic => relic.DataId == OwnerId);
            Require(ownerIndex >= 0, "Manager observation omitted ReduceCostUnitIfNoUpgrade.");
            RelicCardModifierDispatch modeledDispatch = predicted.Dispatches.Single(item =>
                item.RelicIndex == ownerIndex && item.EffectIndex == 0);
            Dispatch nativeDispatch = managerRecord.Dispatches.Single(item =>
                item.RelicIndex == ownerIndex && item.EffectIndex == 0);
            Require(nativeDispatch.Returned == modeledDispatch.Returned &&
                nativeDispatch.UpgradeAdded == modeledDispatch.UpgradeAdded &&
                TemporaryCount(managerRecord.Actual!) <= 1,
                "Manager native return/unique-upgrade state differs from the model.");
        }
        Require(managerRecords.Any(item => item.Before.DataId == MonsterId && item.ResetTemporary &&
                TemporaryCount(item.Actual!) == 1) &&
            managerRecords.Any(item => item.Before.DataId == MonsterId && !item.ResetTemporary &&
                TemporaryCount(item.Actual!) == 1) &&
            managerRecords.Where(item => item.Before.DataId == SpellId || item.Before.DataId == EquipmentId)
                .All(item => TemporaryCount(item.Actual!) == 0),
            "Manager reset/retain behavior or non-Monster controls changed.");

        Require(RelicNoUpgradeCardCostModel.Validate(owner, rule) == null,
            "Native owner payload no longer satisfies the exact model boundary.");
        BattleTurnChecks.Native(root);
        BattleActionChecks.Native(root);
        Console.WriteLine("ORIGINAL-NO-UPGRADE-CARD-COST-NATIVE PASS: Monster-only discount, hand callback reinstall, visible-upgrade removal, and manager reset parity.");
    }

    private static int TemporaryCount(CardInstanceState card) =>
        card.Temporary.Upgrades.Count(upgrade => upgrade.DataId == Effect);

    private static bool Same(CardInstanceState expected, CardInstanceState actual) => ModelJson.Difference(
        JsonSerializer.Serialize(expected, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options)) == null;

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
