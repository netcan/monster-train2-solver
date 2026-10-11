using System.Linq;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDeployableRelicChecks
{
    private const string RelicKey = "NonBannerUnitsHaveDeployable";
    private const string UpgradeKey = "NonBannerUnitsHaveDeployable_Upgrade";
    private const string DeploymentTrait = "CardTraitDrawInDeploymentPhase";

    internal static PlayCardAction? Choose(BattleTurnState source)
    {
        if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
        CombatContext context = source.Spawn.Train.Context;
        foreach (PlayCardAction play in BattleActionModel.EnumerateSupportedPlays(source))
        {
            CardInstanceState? card = context.FindCard(play.CardInstanceId);
            CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
            if (card?.Temporary.Upgrades.Any(item => item.AssetKey == UpgradeKey) == true &&
                rule?.Effect == "SpawnMonster") return play;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    internal static void Native(FixtureValue root, bool required = false)
    {
        if (!root.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "original-deployable-relic")
        { Require(!required, "Fixture lacks original deployable relic battle coverage."); return; }
        RelicCardModifierChecks.Native(root, required: true);
        Require(root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean(), "Deployable relic battle is not a winning native capture from the current game build.");

        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length > 0, "Deployable relic battle contains no modeled player actions.");
        BattleTurnState first = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatContext context = first.Spawn.Train.Context!;
        CombatRelicState owner = context.Relics!.Single(relic => relic.AssetKey == RelicKey);
        Require(owner.EffectTypes.SequenceEqual([RelicModel.AddTempUpgrade]) &&
            owner.CardModifiers?.Single().Upgrade?.AssetKey == UpgradeKey,
            "The original deployable relic or its unchanged temporary-upgrade payload is absent from the decision state.");

        CardInstanceState[] initialHandUnits = context.Cards.Hand.Select(token => context.FindCard(token.InstanceId))
            .Where(card => card != null && card.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == UpgradeKey))
            .Select(card => card!).ToArray();
        Require(initialHandUnits.Length > 0 && initialHandUnits.All(card => card.MaskDescriptor != null &&
            CardBranchMaskModel.Resolve(card).Traits.Contains(DeploymentTrait, System.StringComparer.Ordinal)),
            "Native deployment phase did not put an eligible relic-upgraded unit with its draw trait in the first decision hand.");

        int played = 0;
        foreach (FixtureValue entry in actions)
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext beforeContext = before.Spawn.Train.Context!;
            CardInstanceState? card = beforeContext.FindCard(action.CardInstanceId);
            if (card?.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == UpgradeKey) != true) continue;
            CardPlayRule? rule = before.PlayRules?.Cards.FirstOrDefault(item => item.DataId == card.DataId);
            if (rule?.Effect != "SpawnMonster") continue;
            Require(card.MaskDescriptor != null && CardBranchMaskModel.Resolve(card).Traits.Contains(DeploymentTrait, System.StringComparer.Ordinal),
                "A played deployable relic unit lost its branch-owned deployment trait.");
            BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(after.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == card.InstanceId),
                "The played deployable relic unit did not enter a native room.");
            played++;
        }
        Require(played > 0, "The independent battle policy never played a relic-upgraded deployment unit.");
        Console.WriteLine($"NATIVE-ORIGINAL-DEPLOYABLE-RELIC-CHECKS PASS: initial hand has {initialHandUnits.Length} naturally deployed upgraded units; {played} are independently played in the complete battle.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
