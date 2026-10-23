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
        bool advance = root.TryGetProperty("SpellAdvanceControls", out FixtureValue flag) && flag.GetBoolean();
        bool costRelic = root.TryGetProperty("CostRelicControls", out FixtureValue costFlag) && costFlag.GetBoolean();
        int[] costIds = costRelic ? root.GetProperty("CostControlIds").Deserialize<int[]>()! : [];
        Require(!costRelic || advance && costIds.Length == 2, "Missing cost-relic spell composition controls.");
        bool attackRelic = root.TryGetProperty("AttackRelicControls", out FixtureValue attackFlag) && attackFlag.GetBoolean();
        int[] attackIds = attackRelic ? root.GetProperty("AttackControlIds").Deserialize<int[]>()! : [];
        Require(!attackRelic || advance && !costRelic && attackIds.Length == 1, "Missing separate monster attack relic controls.");
        bool healingRelic = root.TryGetProperty("HealingRelicControls", out FixtureValue healingFlag) && healingFlag.GetBoolean();
        Require(!healingRelic || advance && !attackRelic && !costRelic, "Missing separate healing relic controls.");
        Require(ids.Length == (advance ? 6 : 5) && root.GetProperty("Rows").GetArrayLength() == 15, "Missing original spells or controls.");
        int witnesses = 0, changes = 0, exclusive = 0, additive = 0, selections = 0;
        bool guardsChecked = false, advanceGuardChecked = false;
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
            if (costRelic) CostRelic(after, costIds);
            if (attackRelic) AttackRelic(after, ids, attackIds);
            if (healingRelic)
            {
                HealingRelic(after);
                foreach (int id in ids)
                    Require(after.FindCard(id)!.Temporary.Offsets.Heal == 10 &&
                        before.Context!.FindCard(id)!.Temporary.Offsets.Heal == 10,
                        "Moon refresh must preserve the native healing offset without reapplying or rescaling it.");
                if (operation == "Refresh" && !guardsChecked)
                    foreach (string field in new[] { "PowerModifier", "ConditionCount" })
                    {
                        var malformed = System.Text.Json.Nodes.JsonNode.Parse(parent)!;
                        var modifier = malformed["Context"]!["Relics"]!.AsArray().Single(node => node!["DataId"]!.GetValue<string>() ==
                            "03613302-e55a-4e6c-99a1-c198c8d3175c")!["MagicPowerModifiers"]![0]!;
                        modifier[field] = field == "PowerModifier" ? 11 : 1;
                        if (field == "ConditionCount") modifier["Conditions"] = null;
                        TrainCombatState unknown = JsonSerializer.Deserialize<TrainCombatState>(malformed.ToJsonString(), ModelJson.Options)!;
                        Require(!RoomMoonPhaseModel.Refresh(unknown, false).Supported,
                            "Uncalibrated healing amount/conditions bypassed the moon guard.");
                    }
            }
            if (attackRelic && operation == "Refresh" && !guardsChecked)
            {
                var malformed = System.Text.Json.Nodes.JsonNode.Parse(parent)!;
                malformed["Context"]!["Relics"]!.AsArray().Single(node => node!["DataId"]!.GetValue<string>() ==
                    "79122f7a-6895-443a-830c-2db23027776c")!["CardAttackModifiers"]![0]!["CardType"] = "Spell";
                TrainCombatState unknown = JsonSerializer.Deserialize<TrainCombatState>(malformed.ToJsonString(), ModelJson.Options)!;
                Require(!RoomMoonPhaseModel.Refresh(unknown, false).Supported, "Uncalibrated spell-matching attack relic bypassed the moon guard.");
            }
            if (costRelic && operation == "Refresh" && !guardsChecked)
            {
                var malformed = System.Text.Json.Nodes.JsonNode.Parse(parent)!;
                malformed["Context"]!["Relics"]!.AsArray().Single(node => node!["DataId"]!.GetValue<string>() ==
                    "a19cac7c-b2e0-42bd-bc1e-d180e17a1c15")!["CardTypeCosts"]![0]!["ConditionCount"] = 1;
                TrainCombatState unknown = JsonSerializer.Deserialize<TrainCombatState>(malformed.ToJsonString(), ModelJson.Options)!;
                Require(!RoomMoonPhaseModel.Refresh(unknown, false).Supported, "Uncalibrated conditional cost relic bypassed the moon guard.");
            }
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
            if (advance)
            {
                CardInstanceState lunacy = after.FindCard(ids[5])!, priorLunacy = before.Context!.FindCard(ids[5])!;
                Require(lunacy.DataId == "e9fa09e5-13bd-4826-a8e5-969332cbfbbf" &&
                    lunacy.MaskDescriptor!.SourceTraits!.Count(trait => trait.Type == CardMoonAdvanceScalingModel.Trait) == 1 &&
                    CardMoonAdvanceScalingModel.Adjust(after, ids[5], 0).Supported &&
                    JsonSerializer.Serialize(priorLunacy, ModelJson.Options) == JsonSerializer.Serialize(lunacy, ModelJson.Options),
                    "Original Lunacy resource refresh changed its card state or scaling signature.");
                if (!advanceGuardChecked)
                {
                    var malformed = System.Text.Json.Nodes.JsonNode.Parse(parent)!;
                    var source = malformed["Context"]!["CardInstances"]!.AsArray()
                        .Single(node => node!["InstanceId"]!.GetValue<int>() == ids[5])!["MaskDescriptor"]!["SourceTraits"]!.AsArray()
                        .Single(node => node!["Type"]!.GetValue<string>() == CardMoonAdvanceScalingModel.Trait)!;
                    source["Float"] = 2;
                    TrainCombatState unknown = JsonSerializer.Deserialize<TrainCombatState>(malformed.ToJsonString(), ModelJson.Options)!;
                    Require(!RoomMoonPhaseModel.Refresh(unknown, false).Supported,
                        "Uncalibrated Lunacy parameters bypassed the resource refresh guard.");
                    advanceGuardChecked = true;
                }
            }
            foreach (int id in ids.Take(5))
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
        Require(changes > 0 && exclusive > 0 && additive > 0 && selections == 6 && guardsChecked && (!advance || advanceGuardChecked), "Missing changed upgrades, both room modes, guard or selection controls.");
        Console.WriteLine($"ROOM-MOON-SPELL-UPGRADES PASS boundaries=15 originalSpells={ids.Length} lunacyResourceControls={advance} costRelicControls={costRelic} attackRelicControls={attackRelic} healingRelicControls={healingRelic} traitSnapshots={witnesses} activeChanges={changes} exclusiveWitnesses={exclusive} additiveWitnesses={additive} selections={selections} immutableBranchesPerBoundary=8");
    }
    internal static void HealingRelic(CombatContext context)
    {
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == "03613302-e55a-4e6c-99a1-c198c8d3175c");
        Require(owner.AssetKey == "IncreaseSpellHealing" && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual([RelicModel.ModifyCardHealingPower]) && RelicModel.Validate(context) == null &&
            owner.MagicPowerModifiers!.Select(rule => (rule.EffectIndex, rule.PowerModifier, rule.ConditionCount))
                .SequenceEqual([(0, 10, 0)]), "Original healing relic signature changed.");
    }
    internal static void AttackRelic(CombatContext context, int[] spells, int[] monsters)
    {
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == "79122f7a-6895-443a-830c-2db23027776c");
        Require(owner.AssetKey == "MoreOrLessArtifactsRelic" && owner.EffectTypes.SequenceEqual(
            [RelicModel.ModifyCardAttack, RelicModel.ModifyCardAttack]) && RelicModel.Validate(context) == null,
            "Original monster attack relic definitions differ.");
        Require(owner.CardAttackModifiers!.Select(rule => (rule.EffectIndex, rule.CardType, rule.CharacterName, rule.CharacterSubtypeKey,
            rule.AttackModifier, rule.AttackMultiplier, rule.UnmodifiedAttackMultiplier, rule.MultiplyByUpgradeCount,
            rule.MultiplyByRelicCount, rule.ExcludeChampions, rule.ExcludeStartRelics, rule.CardFilter))
            .SequenceEqual([(0, "Monster", "", "", -1, 1f, 0, false, true, false, true, null),
                (1, "Monster", "", "", 10, 1f, 0, false, false, false, false, null)]), "Original count/flat attack signatures changed.");
        int counted = context.Relics.Count(relic => owner.CardAttackModifiers[0].CountRoles!
            .Any(role => role.Id == relic.DataId && role.Collected && !role.BossGiven && !role.SoulSavior && !role.ActiveStarter));
        Require(counted is > 0 and < 10 && owner.CardAttackModifiers.All(rule => rule.AppliedCardAttacks != null) &&
            monsters.All(id => context.FindCard(id)!.Temporary.Offsets.Damage == 10 - counted &&
                owner.CardAttackModifiers[0].AppliedCardAttacks!.Any(entry => entry.CardInstanceId == id && entry.Amount == -counted) &&
                owner.CardAttackModifiers[1].AppliedCardAttacks!.Any(entry => entry.CardInstanceId == id && entry.Amount == 10)) &&
            spells.All(id => owner.CardAttackModifiers.All(rule => rule.AppliedCardAttacks!.All(entry => entry.CardInstanceId != id))),
            "Monster attack contribution or untouched spell tracking differs.");
    }
    internal static void CostRelic(CombatContext context, int[] ids)
    {
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == "a19cac7c-b2e0-42bd-bc1e-d180e17a1c15");
        Require(owner.AssetKey == "CheaperBlightScourge" && owner.EffectTypes.SequenceEqual(
            [RelicModel.ModifyCardTypeCost, RelicModel.ModifyCardTypeCost]) && RelicModel.Validate(context) == null &&
            owner.CardTypeCosts!.Select(rule => (rule.EffectIndex, rule.CardType, rule.Amount, rule.IgnoreRestrictions, rule.ConditionCount))
                .SequenceEqual([(0, "Blight", -99, false, 0), (1, "Junk", -99, false, 0)]), "Original cost relic signature changed.");
        for (int index = 0; index < 2; index++)
        {
            CardInstanceState card = context.FindCard(ids[index])!;
            Require(card.DataId == (index == 0 ? "21038774-c0a4-413c-ab7e-27e458228e5f" : "62cc5532-5fa0-4e66-9fec-85be573af359") &&
                card.Temporary.Offsets.Cost == -99 && card.Temporary.Offsets.XCost == 99,
                "Moon refresh changed the original Blight/Junk relic discount.");
        }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
