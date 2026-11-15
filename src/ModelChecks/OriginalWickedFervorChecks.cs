using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class OriginalWickedFervorChecks
{
    private const string OriginalId = "2107d396-9427-4bfd-9889-eef36bd5c7b0";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalWickedFervorPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Wicked Fervor battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        int[] ids = root.GetProperty("OriginalWickedFervorIds").Deserialize<int[]>()!;
        int[] discards = root.GetProperty("OriginalWickedFervorDiscardIds").Deserialize<int[]>()!;
        int[] protectedIds = root.GetProperty("OriginalWickedFervorProtectedIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length == 4 && discards.Length == 3 && protectedIds.Length == 2,
            "Missing positive/zero discard controls.");
        foreach (int index in new[] { 2, 3 })
        {
            var before = actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            var rule = before.PlayRules!.Cards.Single(rule => rule.DataId == OriginalId);
            var context = before.Spawn.Train.Context!; var actual = after.Spawn.Train.Context!;
            Require(action.CardInstanceId == ids[index] && before.UiRngIsolated && card.DataId == OriginalId && rule.AssetKey == "WickedFervor" &&
                rule.Cost == 1 && before.Energy - after.Energy == 1 && card.MaskDescriptor!.SourceTraits!.Count == 0 &&
                rule.ExternalInteractions.Count == 0 && rule.OnCastEffects.Count == 0 && rule.Effects.Count == 1 &&
                rule.Effects[0] is { Type: DiscardHandStatusModel.Effect, Target: "DropTargetCharacter", AllowEnemy: false, AllowPlayer: true,
                    Range: null, Value: 0 } effect && effect.Statuses.Count == 1 && effect.Statuses[0].Id == "zeal" && effect.Statuses[0].Stacks == 5,
                "Original identity, definition, cost or five-Zeal-per-card effect differs.");
            int[] eligible = DiscardHandStatusModel.EligibleCards(context, ids[index], out string? error)!;
            Require(error == null && eligible.Order().SequenceEqual(index == 2 ? discards.Order() : Array.Empty<int>()), "Eligible native discard list differs.");
            Require(protectedIds.All(id => context.Cards.Hand.Any(token => token.InstanceId == id) && actual.Cards.Hand.Any(token => token.InstanceId == id) &&
                context.Statistics!.Value(id, "TimesDiscarded", "ThisBattle") == actual.Statistics!.Value(id, "TimesDiscarded", "ThisBattle")),
                "Persistent or Infusion was discarded or counted.");
            Require(CardBranchMaskModel.Resolve(context.FindCard(protectedIds[0])!).Traits.Contains("CardTraitPersistent") &&
                CardBranchMaskModel.Resolve(context.FindCard(protectedIds[1])!).Traits.Contains("CardTraitInfusion"), "Missing protected native trait controls.");
            var target = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == action.TargetUnitId);
            var changed = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == action.TargetUnitId);
            if (index == 2)
            {
                Require(discards.All(id => CardBranchMaskModel.Resolve(context.FindCard(id)!).Traits.Contains("CardTraitFreeze") &&
                    actual.Cards.Hand.All(token => token.InstanceId != id) && actual.Cards.Discard.Any(token => token.InstanceId == id) &&
                    actual.Statistics!.Value(id, "TimesDiscarded", "ThisBattle") - context.Statistics!.Value(id, "TimesDiscarded", "ThisBattle") == 1),
                    "Frozen ordinary cards were not all discarded exactly once.");
                Require(actual.Cards.Discard.Where(token => discards.Contains(token.InstanceId)).Select(token => token.InstanceId).SequenceEqual(eligible),
                    "Native sequential discard order differs from the original hand snapshot.");
                Require((changed.Status("zeal")?.Stacks ?? 0) - (target.Status("zeal")?.Stacks ?? 0) == 15 && changed.Health == target.Health &&
                    changed.MaxHealth > target.MaxHealth && changed.Attack > target.Attack &&
                    actual.Cards.Hand.Any(token => token.InstanceId == ids[3]), "Three discards must grant fifteen Zeal without healing or discarding the protected second spell.");
            }
            else Require(Json(changed) == Json(target), "Zero eligible discards must leave the entire target unchanged.");
            Require(Json(context.BattleRng) == Json(actual.BattleRng), "Fixed single-status discard spell consumed Battle RNG.");
            Require(after.Spawn.Train.Rooms[1].Units.Select(Json).SequenceEqual(before.Spawn.Train.Rooms[1].Units.Select(Json)), "Other floor was affected.");
        }
        RecordedBattleChecks.Native(root);
        BattleSearchChecks.Native(root);
        Console.WriteLine("ORIGINAL-WICKED-FERVOR PASS: unchanged original, three frozen discards -> fifteen Zeal, Persistent/Infusion/source exclusions, zero eligible discard control, complete winning chain/16 branches, exact piles/stats/RNG and serial/parallel search.");
    }
}
