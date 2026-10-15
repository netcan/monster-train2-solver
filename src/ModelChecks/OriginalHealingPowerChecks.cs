using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalHealingPowerChecks
{
    private const string DataId = "03613302-e55a-4e6c-99a1-c198c8d3175c";
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "original-healing-power-relic" &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Healing-power battle failed acceptance.");
        int count = 0; bool scalable = false, nonScalable = false, stronger = false, monster = false, blight = false;
        foreach (FixtureValue row in root.GetProperty("RelicCardModifiers").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<CardInstanceState>()!;
            var context = row.GetProperty("ContextBefore").Deserialize<CombatContext>()!;
            var afterContext = row.GetProperty("ContextAfter").Deserialize<CombatContext>()!;
            var actual = row.GetProperty("Actual").Deserialize<CardInstanceState>()!;
            var relics = row.GetProperty("Relics").Deserialize<CombatRelicState[]>()!;
            var owner = relics.Single(relic => relic.DataId == DataId);
            Equal(owner, new CombatRelicState(DataId, "IncreaseSpellHealing", [RelicModel.ModifyCardHealingPower],
                isCovenant: false, disallowedInPlacementPhase: false, magicPowerModifiers: [new(0, 10)]), "unchanged original owner");
            bool reset = row.GetProperty("ResetTemporary").GetBoolean();
            string frozen = JsonSerializer.Serialize(before);
            var predicted = RelicCardModifierModel.Apply(before, relics, reset, context);
            Require(predicted.Supported, "Healing manager replay unsupported: " + predicted.UnsupportedReason);
            Equal(predicted.Card, actual, "card state");
            Equal(predicted.Dispatches, row.GetProperty("Dispatches").Deserialize<RelicCardModifierDispatch[]>(), "dispatches");
            Equal(predicted.Context!.Relics, afterContext.Relics, "relic conditions");
            Equal(predicted.Context.Statistics, afterContext.Statistics, "statistics");
            Require(frozen == JsonSerializer.Serialize(before), "Manager replay mutated its input.");
            var ablated = RelicCardModifierModel.Apply(before, relics.Where(relic => relic.DataId != DataId).ToArray(), reset, context);
            Require(ablated.Supported, "Healing owner ablation unsupported.");
            bool spell = before.MaskDescriptor!.Definition.CardType == "Spell";
            int bonus = spell ? 10 * CardMagicPowerModel.Multiplier(before) : 0;
            Require(actual.Temporary.Offsets.Heal - ablated.Card!.Temporary.Offsets.Heal == bonus &&
                actual.Temporary.Offsets.Damage == ablated.Card.Temporary.Offsets.Damage,
                "Healing owner ablation did not isolate healing only.");
            if (spell && reset)
            {
                scalable |= before.MaskDescriptor.Definition.CanScaleWithMagicPower == true;
                nonScalable |= before.MaskDescriptor.Definition.CanScaleWithMagicPower == false;
                stronger |= bonus == 50;
            }
            monster |= before.MaskDescriptor.Definition.CardType == "Monster";
            blight |= before.MaskDescriptor.Definition.CardType == "Blight";
            count++;
        }
        Require(count >= 16 && scalable && nonScalable && stronger && monster && blight,
            "Missing native eligibility/trait/control witnesses.");
        int played = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!;
            if (card.DataId != "fc119000-0000-4000-8000-000000000401") continue;
            Require(card.Temporary.Offsets.Heal == 10 && card.Temporary.Offsets.Damage == 0,
                "Played healing carrier lost its +10 healing-only modifier.");
            var replay = BattleActionModel.PlayCard(before, action);
            Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(actual),
                "Native healing play differs: " + replay.Reason);
            CombatUnit? target = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex)
                .Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
            if (target == null) continue; // Later policy plays can target an empty room.
            CombatUnit? healed = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == target.Id);
            if (target.Health <= 6)
            {
                Require(healed == null, "Damage-killed healing target was unexpectedly revived.");
                continue;
            }
            Require(healed != null && healed.Health == Math.Min(target.MaxHealth, target.Health - 6 + 11),
                "Healing carrier did not actually restore native damage with the original relic bonus.");
            played++;
        }
        Require(played > 0, "Original healing relic lacks an actual healing play.");
        Console.WriteLine($"ORIGINAL-HEALING-POWER-NATIVE PASS: {count} exact manager refreshes, independent eligibility, trait scaling, resets, ablations and {played} actual healing plays.");
    }
    private static void Equal<T>(T left, T right, string label) => Require(JsonSerializer.Serialize(left) == JsonSerializer.Serialize(right), "Native healing " + label + " differs.");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
