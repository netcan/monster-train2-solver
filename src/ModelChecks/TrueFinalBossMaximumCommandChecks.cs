using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossMaximumCommandChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    internal static void Native(string path, HashSet<string>? searched = null)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement, control = root.GetProperty("TrueFinalBossMaximumBattle");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        int commandId = control.GetProperty("TerminalCommandCardId").GetInt32();
        int[] controls = control.GetProperty("ControlCardIds").Deserialize<int[]>()!;
        Require(control.GetProperty("TerminalCommand").GetBoolean() && control.GetProperty("Prepared").GetBoolean() &&
            control.GetProperty("CompletedControlPlays").GetInt32() == 11 && actions.Length == 11 && controls.Length == 11 &&
            root.GetProperty("Turns").GetArrayLength() == 3 && actions.Take(10).Select(row => row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32())
                .SequenceEqual(controls.Take(10)) && commandId != controls[10], "Missing separate original terminal-command input.");
        FixtureValue last = actions.Last();
        BattleTurnState before = last.GetProperty("Before").Deserialize<BattleTurnState>()!, actual = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction input = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardInstanceState card = before.Spawn.Train.Context!.FindCard(commandId)!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId);
        Require(input.CardInstanceId == commandId && card.DataId == "9f9afce5-44fe-40ff-aee5-1df851b30263" && rule.Cost == 1 && rule.Destination == "Purged" &&
            rule.Effects.Select(effect => effect.Type).SequenceEqual(new[] { "UnitUpgrade", "AttackWithUnit" }) &&
            rule.Effects.All(effect => effect.Target == "Pyre") && rule.Effects[0].Upgrade!.Stats.Damage == 10 &&
            CardTerminalPurgeModel.Supports(card) && CardBranchMaskModel.Resolve(card).Traits.SequenceEqual(new[] { "CardTraitSelfPurge", "CardTraitEphemeral" }) &&
            before.Spawn.Train.Context.LifeLinks!.Actors.All(actor => actor.Health == 1) && actual.Energy == before.Energy - 1 &&
            last.GetProperty("ActualOutcome").Deserialize<RoomOutcome>() == RoomOutcome.BattleWon && actual.Spawn.Train.Context!.LifeLinks == null &&
            actual.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Purged").Cards.Count(token => token.InstanceId == commandId) == 3 &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).BaseAttack == 55,
            "Original Pyre command failed its upgrade/attack, ember payment or three terminal purge callbacks.");
        // Added-trait/replacement combinations remain governed by their own evidence.
        Require(!CardTerminalPurgeModel.Supports(card.WithMaskDescriptor(card.MaskDescriptor!.WithBaseTraits(
            new[] { new CardTraitValue("CardTraitSelfPurge", "CardTraitSelfPurge", 0, 0, false, 0) }))),
            "Base Ephemeral evidence incorrectly enabled a different terminal purge lifecycle.");
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); BattleTurnChecks.Native(root);
        RecordedBattleChecks.Native(root, firstTurn: 1, completed: state => RecordedBattleChecks.Compare(state, actual, true, "original terminal Pyre command chain"));
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" && root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0) &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            control.GetProperty("TerminalSacrifices").GetArrayLength() == 0 && control.GetProperty("TerminalRemovals").GetArrayLength() == 5 &&
            control.GetProperty("TerminalHarvest").GetArrayLength() == 2, "Native terminal-command acceptance is incomplete.");
        TrueFinalBossMaximumSacrificeChecks.Search(before, searched);
        Console.WriteLine("TRUE-FINAL-BOSS-MAXIMUM-COMMAND PASS: original one-ember Pyre upgrade/attack victory, base SelfPurge+Ephemeral purges three times after terminal clearing; eleven plays/three EndTurns, full state/RNG replay, 16 immutable branches and 128-state serial/parallel search without unsupported attempts.");
    }
}
