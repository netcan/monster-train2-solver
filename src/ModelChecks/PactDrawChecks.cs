using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PactDrawChecks
{
    internal static void Native(FixtureValue root)
    {
        string primary = root.GetProperty("PrimaryClanId").GetString()!;
        string secondary = root.GetProperty("SecondaryClanId").GetString()!;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("Boundary").GetString() == "OriginalPactDrawAfterEachClan" &&
            root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            root.GetProperty("OwnerId").GetString() == RelicPactDrawModel.OwnerId &&
            root.GetProperty("OwnerKey").GetString() == RelicPactDrawModel.OwnerKey &&
            root.GetProperty("PyreId").GetString() == "707491d7-0dde-4670-b129-d0356c641829" &&
            root.GetProperty("DrawNextTurn").GetBoolean() && root.GetProperty("DrawCount").GetInt32() == 1 &&
            Guid.TryParse(primary, out _) && Guid.TryParse(secondary, out _) && primary != secondary,
            "Expected the original Pact Pyre and its two distinct clans and one-card draw reward.");

        FixtureValue[] starts = root.GetProperty("Starts").EnumerateArray().ToArray();
        FixtureValue[] plays = root.GetProperty("Plays").EnumerateArray().ToArray();
        string[] roles = ["Neutral", "Secondary", "Secondary", "Primary", "Primary", "Secondary", "AnyClan"];
        int[] turnEnds = [4, 7];
        Require(starts.Length == 3 && plays.Length == roles.Length, "Expected seven paid inputs across two Pact rounds.");
        FixtureValue[] rewardedResets = root.GetProperty("Resets").EnumerateArray().Where(row => ReadFlags(row.GetProperty("Before")).Rewarded).ToArray();
        Require(rewardedResets.Select(row => row.GetProperty("Turn").GetInt32()).SequenceEqual([1, 2]) &&
            rewardedResets.All(row => row.GetProperty("DrawModifier").GetInt32() == 1 &&
                ReadFlags(row.GetProperty("Before")) == new Flags(true, true, true) && ReadFlags(row.GetProperty("After")) == default),
            "The native round resets did not clear both clan flags after scheduling one card.");

        for (int start = 0; start < starts.Length; start++)
        {
            BattleTurnState state = Read(starts[start], start + 1, default, 0);
            Require(state.Spawn.Train.Context!.Cards.Hand.Count == state.DrawPerTurn + (start == 0 ? 0 : 1),
                "The native hand does not contain the one scheduled extra draw on turn " + (start + 1) + ".");
        }

        int index = 0;
        for (int round = 0; round < turnEnds.Length; round++)
        {
            Flags flags = default;
            while (index < turnEnds[round])
            {
                FixtureValue play = plays[index];
                PlayCardAction action = play.GetProperty("Action").Deserialize<PlayCardAction>()!;
                BattleTurnState before = Read(play.GetProperty("Before"), round + 1, flags, flags.Rewarded ? 1 : 0);
                CombatContext context = before.Spawn.Train.Context!;
                CardInstanceState card = context.FindCard(action.CardInstanceId)!;
                string? clan = card.MaskDescriptor?.Definition.LinkedClan;
                string role = clan == primary ? "Primary" : clan == secondary ? "Secondary" : "Neutral";
                Require(card.MaskDescriptor != null && card.DataId == play.GetProperty("DataId").GetString() &&
                    play.GetProperty("Role").GetString() == role &&
                    (roles[index] == "AnyClan" ? role != "Neutral" : role == roles[index]) &&
                    action.ActivatorUnitId == 0 && action.ActivatorRoomIndex == null &&
                    before.PlayRules!.Cards.Single(rule => rule.DataId == card.DataId).IsAnyAbility == false &&
                    context.Cards.Hand.Any(token => token.InstanceId == action.CardInstanceId),
                    "Expected the recorded ordinary paid card and its actual linked clan at play " + (index + 1) + ".");

                bool main = flags.Primary || role == "Primary", sub = flags.Secondary || role == "Secondary";
                Flags next = new(main, sub, main && sub);
                BattleTurnState after = Read(play.GetProperty("After"), round + 1, next, next.Rewarded ? 1 : 0);
                Require(!after.Spawn.Train.Context!.Cards.Hand.Any(token => token.InstanceId == action.CardInstanceId),
                    "The original paid card remained in hand.");
                FixtureValue[] callbacks = play.GetProperty("Callbacks").EnumerateArray().ToArray();
                Require(callbacks.Length == (flags.Rewarded ? 0 : 1) && callbacks.All(row =>
                    ReadFlags(row.GetProperty("Before")) == flags && ReadFlags(row.GetProperty("After")) == next &&
                    row.GetProperty("DrawBefore").GetInt32() == (flags.Rewarded ? 1 : 0) &&
                    row.GetProperty("DrawAfter").GetInt32() == (next.Rewarded ? 1 : 0)),
                    "The exact native Pact callback and absolute draw modifier differ at play " + (index + 1) + ".");
                flags = next;
                index++;
            }
        }

        BattleTurnState initial = Decision(starts[0]), stateToReplay = initial;
        string parent = JsonSerializer.Serialize(initial, ModelJson.Options);
        index = 0;
        for (int round = 0; round < turnEnds.Length; round++)
        {
            while (index < turnEnds[round])
            {
                FixtureValue play = plays[index];
                string step = "Pact play " + (index + 1);
                stateToReplay = RecordedBattleChecks.RecordedBoundary(stateToReplay, play.GetProperty("Before").GetProperty("Decision"),
                    terminal: false, "before " + step);
                BattleActionResult modeled = BattleActionModel.PlayCard(stateToReplay, play.GetProperty("Action").Deserialize<PlayCardAction>()!);
                Require(modeled.State != null && modeled.Outcome == RoomOutcome.Exchanged, step + ": " + modeled.Reason);
                stateToReplay = RecordedBattleChecks.RecordedBoundary(modeled.State!, play.GetProperty("After").GetProperty("Decision"),
                    terminal: false, "after " + step);
                index++;
            }
            BattleTurnResult next = BattleTurnModel.EndTurn(stateToReplay);
            Require(next.State != null && next.Outcome == RoomOutcome.Exchanged, "Pact EndTurn " + (round + 1) + ": " + next.UnsupportedReason);
            stateToReplay = RecordedBattleChecks.RecordedBoundary(next.State!, starts[round + 1].GetProperty("Decision"),
                terminal: false, "after Pact EndTurn " + (round + 1));
        }
        Require(parent == JsonSerializer.Serialize(initial, ModelJson.Options), "Pact transition mutated the recorded parent.");
        Console.WriteLine("NATIVE-PACT-DRAW PASS: seven original paid plays, two next-turn extra draws and flag resets, full continuous states and RNG.");

        BattleTurnState Read(FixtureValue snapshot, int turn, Flags flags, int draw)
        {
            BattleTurnState state = Decision(snapshot);
            CombatContext context = state.Spawn.Train.Context!;
            CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == RelicPactDrawModel.OwnerId);
            RelicResourceEffect effect = owner.ResourceEffects!.Single();
            Require(state.Spawn.Turn == turn && state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.IsPyre && unit.AssetKey == "PyreHeartPact") &&
                effect.Type == RelicPactDrawModel.Effect && RelicPactDrawModel.Validate(owner, effect) == null &&
                effect.PactDraw is { } pact && pact.MainClanId == primary && pact.SubClanId == secondary &&
                new Flags(pact.MainPlayed, pact.SubPlayed, pact.BonusScheduled) == flags &&
                ReadFlags(snapshot.GetProperty("Flags")) == flags && snapshot.GetProperty("DrawModifier").GetInt32() == draw &&
                context.Cards.DrawModifier == draw,
                "Native Pact flags or absolute draw modifier disagree at turn " + turn + ".");
            return state;
        }
    }

    private static BattleTurnState Decision(FixtureValue snapshot) =>
        snapshot.GetProperty("Decision").Deserialize<BattleTurnState>()!;

    private static Flags ReadFlags(FixtureValue value) => new(value.GetProperty("Primary").GetBoolean(),
        value.GetProperty("Secondary").GetBoolean(), value.GetProperty("Rewarded").GetBoolean());

    private readonly record struct Flags(bool Primary, bool Secondary, bool Rewarded);

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
