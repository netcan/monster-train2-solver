using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomSpellUpgradeChecks
{
    private const string Chamber = "00f55f64-c5c1-4d5e-9f59-f50413341c1e";
    internal static void Native(FixtureValue root, IReadOnlyCollection<string> originals)
    {
        if (!originals.Contains(Chamber)) return;
        int selected = 0, outside = 0, replacements = 0, attuned = 0, damage = 0, heal = 0;
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!,
                after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            CombatContext prior = before.Spawn.Train.Context!, next = after.Spawn.Train.Context!;
            string played = prior.FindCard(action.CardInstanceId)!.DataId;
            RoomCapacityAttachmentState? attachment = next.RoomCapacities!.SelectMany(room => room.Attachments!).SingleOrDefault(value => value.DataId == Chamber);
            if (attachment?.SpellUpgrade is RoomSpellUpgradeState tracker)
            {
                Require(tracker.TrackerId == "room-spell:" + attachment.AttachmentId,
                    "Enchanted Chamber requires a detached, canonical native tracker identity.");
                bool active = next.QueryFrame!.SelectedRoomIndex == 0;
                int[] expected = active ? next.Cards.Hand.Select(token => next.FindCard(token.InstanceId)!)
                    .Where(card => card.MaskDescriptor!.Definition.CanScaleWithMagicPower == true).Select(card => card.InstanceId).ToArray() : [];
                Require(tracker.Cards.Order().SequenceEqual(expected.Order()), "The native spell tracker must contain exactly the eligible selected-room hand.");
                foreach (CardInstanceState card in next.CardInstances!)
                {
                    CardUpgradeModifier[] upgrades = card.Temporary.Upgrades.Where(upgrade => upgrade.DataId == tracker.TrackerId).ToArray();
                    Require(upgrades.Length == (expected.Contains(card.InstanceId) ? 1 : 0), "Discarded or off-floor cards must lose the room's temporary upgrade.");
                    if (upgrades.Length == 0) continue;
                    int amount = 15 * CardMagicPowerModel.Multiplier(card);
                    Require(upgrades[0].Stats.Damage == amount && upgrades[0].Stats.Heal == amount,
                        "Native room magic must scale damage and healing exactly once, including Attuned.");
                    if (amount == 75) attuned++;
                }
                Require(next.MagicPower!.Where(power => power.RoomIndex == 0).All(power => power.Value == 15) &&
                    next.MagicPower.Where(power => power.RoomIndex != 0).All(power => power.Value == 0), "Room magic must affect only the attachment's floor, for both teams.");
                if (active) selected++; else outside++;
            }
            if (played is "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b" or "d81f1f43-f20e-4459-9fc4-19e5836a876f")
            {
                CombatUnit old = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId),
                    current = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == old.Id);
                if (played == "f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b")
                { Require(old.Health - current.Health == 18, "The original Torch must deal 3 + 15 native damage."); damage++; }
                else { Require(current.Health - old.Health == Math.Min(old.MaxHealth - old.Health, 19), "The original Restore must heal 4 + 15, capped by missing health."); heal++; }
            }
            if (played == "6bc6afc8-a113-4f03-b090-d4436f41d822")
            {
                Require(attachment == null && next.CardInstances!.All(card => card.Temporary.Upgrades.All(upgrade => !upgrade.DataId.StartsWith("room-spell:"))) &&
                    next.MagicPower!.All(power => power.Value == 0), "Replacement must clear tracker upgrades and magic power, including cards outside the hand.");
                replacements++;
            }
        }
        Require(selected >= 3 && outside >= 1 && attuned >= 2 && damage == 1 && heal == 1 &&
            replacements == (originals.Contains("6bc6afc8-a113-4f03-b090-d4436f41d822") ? 1 : 0),
            "Enchanted Chamber needs positive damage/heal, Attuned, off-floor and optional replacement witnesses.");
        Console.WriteLine($"ORIGINAL-ROOM-SPELL-UPGRADES PASS: {selected} selected, {outside} off-floor, {attuned} Attuned upgrades, {replacements} replacements; native damage/healing and complete tracker cleanup.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
