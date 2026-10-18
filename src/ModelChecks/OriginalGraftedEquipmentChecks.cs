using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalGraftedEquipmentChecks
{
    private static readonly string[] Originals = ["c2689249-1886-423d-98d0-cd3857289b3e", "dc7ff295-83fd-47e4-93c7-a133e7058ac8",
        "42d238d8-7180-48b0-b0e8-51f2bbb7ffb4", "0a928329-666c-4ce6-a1c3-413977e37e46", "96f46995-23fe-4c0b-8325-54826e95c333"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Enumerable.Range(0, ids.Length).Where(i => i != 12 && source.Spawn.Train.Context!.Statistics!.Value(ids[i], "TimesPlayed", "ThisBattle") == 0).DefaultIfEmpty(-1).First();
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        int target = index >= 4 && index <= 11 || index == 13 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[index is 9 or 11 ? 2 : 0]).Id : 0;
        var action = new PlayCardAction(ids[index], index == 1 ? 1 : 0, playerPosition: index < 3 ? 0 : -1, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    private static bool Severed(CardInstanceState card) => card.MaskDescriptor!.BaseTraits.Single(trait => trait.RuntimeType == CardGraftedEquipmentModel.Trait).GraftSevered!.Value;
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalGraftedEquipmentPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() && root.GetProperty("OriginalGraftedEquipmentControlsUnchanged").GetBoolean(),
            "Missing completed grafted equipment witness or detached reset isolation.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalGraftedEquipmentIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray(), resets = root.GetProperty("OriginalGraftedEquipmentResets").EnumerateArray().ToArray();
        Require(ids.Length == 15 && actions.Length == 14 && resets.Length == 10, "Missing original equipment, replacement/removal/death/consumption or reset controls.");
        foreach (FixtureValue row in resets)
        {
            CardInstanceState before = row.GetProperty("Before").Deserialize<CardInstanceState>()!, actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
            string parent = Json(before); int repeat = row.GetProperty("Repeat").GetInt32();
            Require(Originals.Contains(before.DataId) && Severed(before) == (repeat == 0) && !Severed(actual) && CardGraftedEquipmentModel.Validate(before) == null,
                "Native trait reset did not clear the captured Severed flag or repeated reset was not idempotent.");
            CardInstanceState result = CardExhaustModel.ResetTraits(before);
            Require(Json(result) == Json(actual), "Full native graft reset card state differs.");
            Parallel.For(0, 16, _ => Require(Json(CardExhaustModel.ResetTraits(before)) == Json(actual), "Independent reset branch differs."));
            CardMaskDescriptor descriptor = before.MaskDescriptor!;
            CardInstanceState missing = before.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost, descriptor.BaseTraits,
                descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft, descriptor.TemporaryTraits, descriptor.PermanentReplacements, null));
            Require(CardGraftedEquipmentModel.Validate(missing) != null && Json(before) == parent, "Missing graft source was accepted or reset mutated its parent.");
        }
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (int i in Enumerable.Range(4, 6))
        {
            BattleTurnState before = Before(i), after = After(i); CombatContext old = before.Spawn.Train.Context!, context = after.Spawn.Train.Context!;
            CardInstanceState card = context.FindCard(ids[i])!;
            CardPlayRule rule = before.PlayRules!.Cards.Single(item => item.DataId == card.DataId);
            int hostSourceId = ids[i == 9 ? 2 : 0];
            CombatUnit host = after.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == hostSourceId);
            Require(card.DataId == Originals[i == 9 ? 4 : i - 4] && rule.Effect == "Equipment" && rule.ExternalInteractions.Count == 0 &&
                before.Energy - after.Energy == rule.Cost && !Severed(card) && card.EquippedUnitId == host.Id && host.EquipmentCards!.SequenceEqual([ids[i]]) &&
                after.OtherPiles.Single(pile => pile.Name == "Standby").EquipmentConditions!.Any(condition => condition.CardId == ids[i] && condition.HostUnitId == host.Id && !condition.ReturnToHand),
                "Original equipment, exact cost, attached identity or standby binding differs.");
            CardStatModifier stats = rule.Equipment!.Upgrades.Single().Stats;
            Require(host.BaseAttack == (i == 9 ? 1 : 60) + stats.Damage && host.MaxHealth == (i == 9 ? 1 : 500) + stats.Health,
                "Original equipment's attack/health upgrade was lost or previous equipment compounded.");
            if (i is >= 5 and <= 8)
            {
                Require(!Severed(old.FindCard(ids[i - 1])!) && Severed(context.FindCard(ids[i - 1])!) &&
                    context.FindCard(ids[i - 1])!.EquippedUnitId == 0 && after.OtherPiles.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == ids[i - 1]),
                    "Replacement must sever/detach the previous graft while retaining its deferred standby entry.");
            }
            Require(Json(before.Spawn.Train.Rooms[1].Units) == Json(after.Spawn.Train.Rooms[1].Units), "Equipment changed a neighbor floor.");
        }
        BattleTurnState removed = After(10), dead = After(11);
        CombatContext copySource = removed.Spawn.Train.Context!;
        string copyParent = Json(copySource);
        CardInstanceState graft = copySource.FindCard(ids[4])!;
        var creation = new CardCreationRule(graft.DataId, CardModifiers.Empty(), graft.EffectCounters,
            graft.ExternalInteractions, graft.DamageScalingTraits, graft.StatusScalingTraits,
            graft.UnitUpgradeScalingTraits, graft.CapacityScalingTraits, graft.EquippedUnitId,
            graft.MaskDescriptor, graft.ForgeCountScalingTraits, graft.CanBeCopied, graft.TrainHealScalingTraits);
        Require(!CardGenerationModel.CloneDetached(copySource, creation, graft.InstanceId).Supported,
            "Detached cloning must reject uncalibrated graft private-state copying.");
        foreach (bool requireCopyable in new[] { false, true })
            Require(!CardGenerationModel.Apply(copySource, new CardGenerationRule("HandPile", 1, [creation],
                copyModifiers: true, deterministicPoolSelection: true, requireCopyableSource: requireCopyable), graft.InstanceId).Supported,
                "AddCard copying must reject uncalibrated graft private-state copying.");
        Require(Json(copySource) == copyParent, "Rejected graft copies changed their parent.");
        Require(ids.Skip(4).Take(5).All(id => Severed(removed.Spawn.Train.Context!.FindCard(id)!) &&
            removed.Spawn.Train.Context.FindCard(id)!.EquippedUnitId == 0 && removed.OtherPiles.Single(pile => pile.Name == "Standby").Cards.Any(card => card.InstanceId == id)) &&
            removed.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).EquipmentCards!.Count == 0,
            "Explicit removal must sever/detach the fifth equipment while standby return remains deferred.");
        BattleTurnState settled = After(12);
        Require(ids.Skip(4).Take(5).All(id => settled.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == id) &&
            settled.OtherPiles.Single(pile => pile.Name == "Standby").Cards.All(card => card.InstanceId != id)) &&
            settled.Spawn.Train.Rooms[0].Units.All(unit => unit.SpawnerCardId != ids[0]), "The original host's death must exhaust its five detached severed grafts.");
        Require(dead.Spawn.Train.Rooms[0].Units.All(unit => unit.SpawnerCardId != ids[2]) &&
            dead.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == ids[9]) &&
            Severed(dead.Spawn.Train.Context.FindCard(ids[9])!) && dead.Spawn.Train.Context.FindCard(ids[9])!.EquippedUnitId == 0,
            "Host death must select hand using the intact graft, then sever and detach it as it leaves Standby.");
        BattleTurnState consumed = After(13);
        Require(!Severed(dead.Spawn.Train.Context.FindCard(ids[12])!) && Severed(consumed.Spawn.Train.Context!.FindCard(ids[12])!) &&
            new[] { ids[9], ids[12] }.All(id => consumed.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == id)),
            "Forced consumption must exhaust intact and severed graft cards and sever them on standby exit.");
        EquipmentChecks.Native(root);
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-GRAFTED-EQUIPMENT PASS: five unchanged equipment definitions, exact upgrades/costs/standby identities, replacements/removal sever with deferred exhaustion, host death selects hand before severing/detaching on standby exit, intact/severed hand consumption, ten detached native reset controls and 16 immutable full chains. Automatic graft birth remains guarded.");
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
