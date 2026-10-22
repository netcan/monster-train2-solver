using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomMoonSpellUpgradeChecks
{
    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("RngUnchanged").GetBoolean() &&
            root.GetProperty("SpellUpgradeControls").GetBoolean() && root.GetProperty("PreviewUpdatesSuppressed").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "AfterOriginalCoroutineAndTriggerQueue", "Missing native room spell upgrade controls.");
        int[] ids = root.GetProperty("CardIds").Deserialize<int[]>()!;
        Require(ids.Length == 5 && root.GetProperty("Rows").GetArrayLength() == 15, "Missing five original spells or controls.");
        int witnesses = 0, changes = 0, exclusive = 0, additive = 0, selections = 0;
        bool guardsChecked = false;
        foreach (FixtureValue row in root.GetProperty("Rows").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").GetProperty("Train").Deserialize<TrainCombatState>()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options), operation = row.GetProperty("Operation").GetString()!;
            int value = row.GetProperty("Value").GetInt32();
            MoonPhaseTransitionResult Replay()
            {
                if (operation == "Refresh") return RoomMoonPhaseModel.Refresh(before, false);
                if (operation == "SetMoonPhase") return MoonPhaseTransitionModel.Apply(before,
                    new CardActionEffect("SetMoonPhase", "Room", value, false, false, []), playerActionOnly: false);
                if (operation == "Select")
                {
                    var selected = CardSpellModel.WithContext(before, before.Context!.WithQueryFrame(before.Context.QueryFrame!.With(selectedRoomIndex: value)));
                    return RoomMoonPhaseModel.Refresh(selected, false);
                }
                if (operation == "Attach")
                {
                    TrainSpellResult cast = CardSpellModel.Apply(before, 0, [root.GetProperty("AttachmentEffect").Deserialize<CardActionEffect>()!], 0);
                    return new MoonPhaseTransitionResult(cast.State, cast.UnsupportedReason);
                }
                Require(operation == "Detach", "Unknown spell room control.");
                CombatContext context = before.Context!;
                RoomCapacityState room = context.RoomCapacities!.Single(item => item.RoomIndex == 0);
                var removed = new RoomCapacityState(0, room.PlayerMaximum, room.EnemyMaximum, room.IsPyre,
                    room.Corruption, [], room.MaxCorruption, room.PermanentCorruption, room.AttachmentsAddedFromCards);
                var capacities = context.RoomCapacities!.Select(item => item.RoomIndex == 0 ? removed : item).ToArray();
                var detached = CardSpellModel.WithContext(before, context.WithRoomCapacities(capacities));
                RoomAttachmentUnitSyncResult synced = RoomAttachmentUnitModel.Synchronize(detached, context.RoomCapacities, capacities);
                return new MoonPhaseTransitionResult(synced.State, synced.UnsupportedReason);
            }
            void Check()
            {
                MoonPhaseTransitionResult result = Replay();
                Require(result.Supported, row.GetProperty("Label").GetString() + ": " + result.UnsupportedReason);
                string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
                Require(difference == null, row.GetProperty("Label").GetString() + ": " + difference);
            }
            Check(); Parallel.For(0, 8, _ => Check());
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Spell room branch mutated its parent.");
            Require(row.GetProperty("Before").GetProperty("Rng").ContentEquals(row.GetProperty("Actual").GetProperty("Rng")), "Spell room upgrade advanced gameplay RNG.");
            CombatContext after = actual.Context!;
            int selectedRoom = after.QueryFrame!.SelectedRoomIndex!.Value, phase = after.QueryFrame.MoonPhase!.Value;
            int roomMask = after.RoomCapacities!.Single(room => room.RoomIndex == selectedRoom).Attachments!
                .Aggregate(0, (mask, attachment) => mask | (attachment.MoonPhase?.AdditionalMask ?? 0));
            if (!guardsChecked && roomMask != 0)
            {
                // Rebuild through JSON so the malformed payload is an independent
                // child of the native witness, never a mutation of its source.
                var malformed = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(after, ModelJson.Options))!;
                var capacities = malformed["RoomCapacities"]!.AsArray();
                capacities.Single(node => node!["RoomIndex"]!.GetValue<int>() == selectedRoom)!["Attachments"]![0]!["MoonPhase"]!["AdditionalMask"] = 3;
                CombatContext unknown = JsonSerializer.Deserialize<CombatContext>(malformed.ToJsonString(), ModelJson.Options)!;
                CardMoonUpgradeModel.RefreshHand(unknown, out string? error);
                Require(error != null && !CardMoonUpgradeModel.Update(after.FindCard(ids[0])!, phase, roomMask: 3).Supported,
                    "Unknown room masks bypassed the spell upgrade guard.");
                guardsChecked = true;
            }
            foreach (int id in ids)
            {
                CardInstanceState card = after.FindCard(id)!;
                CardSourceTrait[] sources = card.MaskDescriptor!.SourceTraits!.Where(trait => trait.Type == CardMoonUpgradeModel.Trait).ToArray();
                CardTraitValue[] traits = card.MaskDescriptor.BaseTraits.Where(trait => trait.RuntimeType == CardMoonUpgradeModel.Trait).ToArray();
                Require(sources.Length == traits.Length && sources.Length > 0, "Missing original source/private trait definitions.");
                for (int index = 0; index < sources.Length; index++)
                {
                    CardSourceTrait definition = sources[index];
                    int effective = definition.Int2 == 1 && roomMask != 0 ? roomMask : phase | roomMask;
                    bool active = (effective & definition.Int) != 0;
                    Require(traits[index].MoonPhaseUpgradeState!.Active == active &&
                        card.Permanent.Upgrades.Any(upgrade => upgrade.DataId == definition.MoonPhaseUpgrade!.DataId) == active,
                        "Native additive/exclusive spell upgrade does not match its selected room.");
                    if (roomMask != 0 && phase == 2)
                    { if (definition.Int2 == 1) exclusive++; else additive++; }
                    CardTraitValue prior = before.Context!.FindCard(id)!.MaskDescriptor!.BaseTraits
                        .Where(trait => trait.RuntimeType == CardMoonUpgradeModel.Trait).ElementAt(index);
                    if (prior.MoonPhaseUpgradeState!.Active != active) changes++;
                    witnesses++;
                }
            }
            CardInstanceState storm = after.FindCard(ids[0])!;
            CardUpgradeModifier numeric = storm.Permanent.Upgrades.Single(upgrade => upgrade.CloneDamageBase > 0);
            Require(numeric.Stats.Damage == numeric.CloneDamageBase * ((phase & 2) != 0 ? 5 : 1), "Selected-room refresh lost stronger magic-power rescaling.");
            if (operation == "Select") selections++;
        }
        Require(changes > 0 && exclusive > 0 && additive > 0 && selections == 6 && guardsChecked, "Missing changed upgrades, both room modes, guard or selection controls.");
        Console.WriteLine($"ROOM-MOON-SPELL-UPGRADES PASS boundaries=15 originalSpells=5 traitSnapshots={witnesses} activeChanges={changes} exclusiveWitnesses={exclusive} additiveWitnesses={additive} selections={selections} immutableBranchesPerBoundary=8");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
