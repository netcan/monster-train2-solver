using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomAttachmentTriggerChecks
{
    internal static void Native(FixtureValue root)
    {
        BattleTurnState[] turns = root.GetProperty("Turns").EnumerateArray().Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            PlayCardAction action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string id = before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId;
            RoomCapacityAttachmentState? old = before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex).Attachments!.SingleOrDefault();
            if (!RoomAttachmentTriggerIdentity.Known(id))
            {
                if (old != null && RoomAttachmentTriggerIdentity.Known(old.DataId) &&
                    before.PlayRules!.Cards.Single(rule => rule.DataId == id).Effects.Any(effect => effect.Type == "AttachRoomCapacity"))
                    Require(after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Triggers.All(trigger => trigger.Origin?.UpgradeId != old.AttachmentId)),
                        "Replacing a room attachment must remove its keyed callbacks from all impacted units.");
                continue;
            }
            RoomCapacityState capacity = after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == action.RoomIndex);
            RoomCapacityAttachmentState attachment = capacity.Attachments!.Single();
            CardUpgradeModifier upgrade = attachment.CharacterUpgrades!.Single();
            Require(attachment.AttachmentId == RoomAttachmentTriggerIdentity.Key(id, action.RoomIndex, capacity.AttachmentsAddedFromCards!.Value) && upgrade.TriggerUpgrades!.Count > 0,
                "Native card-created room callbacks require their canonical attachment creation key.");
            foreach (RoomCombatState room in after.Spawn.Train.Rooms)
                foreach (CombatUnit unit in room.Units)
                {
                    bool affected = room.RoomIndex == action.RoomIndex && (unit.Team == CombatTeam.Player ? attachment.AffectsMonsters : attachment.AffectsHeroes);
                    Require(unit.Triggers.Count(trigger => trigger.Origin?.UpgradeId == attachment.AttachmentId) == (affected ? upgrade.TriggerUpgrades.Count : 0),
                        "Room callbacks must bind only to affected units on the selected floor.");
                }
            CombatUnit[] later = turns.SelectMany(turn => turn.Spawn.Train.Rooms).Where(room => room.RoomIndex == action.RoomIndex).SelectMany(room => room.Units)
                .Where(unit => unit.Triggers.Any(trigger => trigger.Origin?.UpgradeId == attachment.AttachmentId && trigger.HasTriggered)).ToArray();
            bool positive = id switch
            {
                "93759003-bb97-44e4-9ef6-472c55bee2d9" => later.Any(unit => (unit.Status("buff")?.Stacks ?? 0) > 0),
                "6ee4f735-fc54-4f85-aa13-fd0d169af3b2" => later.Any(unit => (unit.Status("valor")?.Stacks ?? 0) >= 6),
                "57921ae8-ca98-4a3b-a659-5e741d2e904a" => later.Any(unit => unit.MaxHealth == 10020 && unit.Health > 10000),
                "db494633-3dda-4410-b9bf-c7e4d2d47d1f" => LaterBirth(root, attachment, upgrade, spikes: true),
                "9e203beb-65a2-4c3d-9f16-83724d98b002" => LaterBirth(root, attachment, upgrade, spikes: false),
                "627f12a8-081e-437d-8a54-a20d719163b7" => turns.Any(turn => turn.Spawn.Train.Context!.Gold > after.Spawn.Train.Context.Gold),
                "08001aa4-2e9d-42da-9ba7-ff4b0fd5ce80" => turns.Any(turn => turn.Energy > 3),
                "e221f8ac-7b5f-4485-a4d1-dedd22f61466" => NextDraw(root, attachment),
                _ => false
            };
            Require(positive, "Room attachment needs a positive native callback: " + id);
            Console.WriteLine($"ORIGINAL-ROOM-TRIGGERS PASS: {attachment.AssetKey}, keyed callback installation, floor/team controls and positive native effect.");
        }
    }

    private static bool NextDraw(FixtureValue root, RoomCapacityAttachmentState attachment)
    {
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!, after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int source = row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            CombatUnit? born = after.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.SpawnerCardId == source);
            if (born?.Triggers.Any(trigger => trigger.Kind == "OnSpawn" && trigger.Origin?.UpgradeId == attachment.AttachmentId && trigger.HasTriggered) != true) continue;
            CombatContext prior = before.Spawn.Train.Context!, next = after.Spawn.Train.Context!;
            CardToken[] equipment = prior.Cards.Draw.Concat(prior.Cards.Discard).Where(token => prior.FindCard(token.InstanceId)!.MaskDescriptor!.Definition.CardType == "Equipment").ToArray();
            int[] drawn = next.Cards.Hand.Where(token => prior.Cards.Hand.All(old => old.InstanceId != token.InstanceId)).Select(token => token.InstanceId).ToArray();
            Require(next.Cards.NextUpgradeReferenceId == prior.Cards.NextUpgradeReferenceId + 1 && next.Cards.NextDrawUpgrades!.Count == 0,
                "Weapons Cache must allocate one native upgrade object and clear its queue at the surrounding card-effect boundary.");
            if (equipment.Length == 0)
                Require(drawn.Length == 0, "Weapons Cache must draw nothing when neither deck nor discard has equipment.");
            else
                Require(equipment.Length == 1 && prior.Cards.Hand.Count(token => prior.FindCard(token.InstanceId)!.MaskDescriptor!.Definition.CardType == "Equipment") == 1 &&
                    drawn.Length == 1 && equipment.Any(token => token.InstanceId == drawn[0]) &&
                    next.FindCard(drawn[0])!.Temporary.Upgrades.Count(upgrade => upgrade.DataId == "b4449765-226a-4c58-9b47-d9dc29d34415" && upgrade.Stats.Cost == -2) == 1 &&
                    prior.Cards.Hand.Concat(prior.Cards.Draw).Concat(prior.Cards.Discard).Where(token => token.InstanceId != drawn[0]).All(token =>
                        next.FindCard(token.InstanceId)!.Temporary.Upgrades.All(upgrade => upgrade.DataId != "b4449765-226a-4c58-9b47-d9dc29d34415")),
                    "Weapons Cache must draw and discount exactly one equipment while excluding spells and the other equipment.");
            return true;
        }
        return false;
    }

    private static bool LaterBirth(FixtureValue root, RoomCapacityAttachmentState attachment, CardUpgradeModifier upgrade, bool spikes)
    {
        foreach (FixtureValue row in root.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            int spawner = row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            CombatUnit? unit = after.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.SpawnerCardId == spawner);
            if (unit == null || after.Spawn.Train.Context!.FindCard(spawner)?.DataId != "63a48bad-07da-4687-bfef-2f7c6653af5e") continue;
            CombatTrigger callback = unit.Triggers.Single(trigger => trigger.Kind == "OnSpawn" && trigger.Origin?.UpgradeId == attachment.AttachmentId);
            return callback.HasTriggered && (spikes ? unit.Status("spikes")?.Stacks == 8 :
                unit.Modifiers!.Upgrades.Any(applied => applied.DataId == upgrade.TriggerUpgrades!.Single(trigger => trigger.Kind == "OnSpawn").Effects.Single().UnitUpgrade!.Upgrade!.DataId));
        }
        return false;
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
