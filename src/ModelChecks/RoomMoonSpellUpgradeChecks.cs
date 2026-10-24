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
        int covenantLevel = root.TryGetProperty("StartingCovenantLevel", out FixtureValue covenant) ? covenant.GetInt32() : 0;
        if (covenantLevel > 0)
        {
            Require(advance, "Covenant spell controls require original Lunacy.");
            MoonCovenantCompositionChecks.Startup(root,
                root.GetProperty("Rows")[0].GetProperty("Before").GetProperty("Train").Deserialize<TrainCombatState>()!.Context!, covenantLevel, guards: true);
        }
        bool costRelic = root.TryGetProperty("CostRelicControls", out FixtureValue costFlag) && costFlag.GetBoolean();
        int[] costIds = costRelic ? root.GetProperty("CostControlIds").Deserialize<int[]>()! : [];
        Require(!costRelic || advance && costIds.Length == 2, "Missing cost-relic spell composition controls.");
        bool attackRelic = root.TryGetProperty("AttackRelicControls", out FixtureValue attackFlag) && attackFlag.GetBoolean();
        int[] attackIds = attackRelic ? root.GetProperty("AttackControlIds").Deserialize<int[]>()! : [];
        Require(!attackRelic || advance && !costRelic && attackIds.Length == 1, "Missing separate monster attack relic controls.");
        bool healingRelic = root.TryGetProperty("HealingRelicControls", out FixtureValue healingFlag) && healingFlag.GetBoolean();
        Require(!healingRelic || advance && !attackRelic && !costRelic, "Missing separate healing relic controls.");
        bool phaseHeal = root.TryGetProperty("PhaseHealRelicControls", out FixtureValue phaseFlag) && phaseFlag.GetBoolean();
        int anchor = phaseHeal ? root.GetProperty("ActorIds")[0].GetInt32() : 0;
        Require(!phaseHeal || advance && !healingRelic && !attackRelic && !costRelic &&
            root.GetProperty("AnchorWoundedBeforeEachRow").GetBoolean() && root.GetProperty("ActorIds").GetArrayLength() == 1,
            "Missing separate phase-healing anchor controls.");
        bool phaseEnergy = root.TryGetProperty("PhaseEnergyRelicControls", out FixtureValue energyFlag) && energyFlag.GetBoolean();
        Require(!phaseEnergy || advance && !phaseHeal && !healingRelic && !attackRelic && !costRelic &&
            root.GetProperty("EnergyConditionsResetBeforeRows").GetBoolean(), "Missing separate once-per-turn energy controls.");
        bool handPower = root.TryGetProperty("HandPowerRelicControls", out FixtureValue handPowerFlag) && handPowerFlag.GetBoolean();
        bool statusRelic = root.TryGetProperty("StatusRelicControls", out FixtureValue statusFlag) && statusFlag.GetBoolean();
        int[] armorActors = statusRelic ? root.GetProperty("ActorIds").Deserialize<int[]>()! : [];
        Require(!statusRelic || advance && !handPower && !phaseEnergy && !phaseHeal && !healingRelic && !attackRelic && !costRelic &&
            armorActors.Length == 2, "Missing separate Silent/non-Silent armor controls.");
        Require(!handPower || advance && !phaseEnergy && !phaseHeal && !healingRelic && !attackRelic && !costRelic,
            "Missing separate hand magic-power controls.");
        int boundaries = handPower ? 19 : 15;
        Require(ids.Length == (advance ? 6 : 5) && root.GetProperty("Rows").GetArrayLength() == boundaries, "Missing original spells or controls.");
        int witnesses = 0, changes = 0, exclusive = 0, additive = 0, selections = 0, heals = 0, energyGains = 0, blockedFull = 0;
        int powerApplications = 0, powerControls = 0, armorGains = 0;
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
                if (operation == "PreDiscardHand")
                {
                    RelicMoonPhaseHandUpgradeResult upgraded = RelicMoonPhaseHandUpgradeModel.Apply(before.Context!,
                        before.Context!.QueryFrame!.MoonPhase!.Value, deployment: false, preview: false);
                    return new MoonPhaseTransitionResult(upgraded.Supported ? CardSpellModel.WithContext(before, upgraded.Context) : null,
                        upgraded.UnsupportedReason);
                }
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
            if (statusRelic)
            {
                MoonStatusCompositionChecks.Owner(before.Context!); MoonStatusCompositionChecks.Owner(after);
                for (int index = 0; index < armorActors.Length; index++)
                {
                    CombatUnit prior = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == armorActors[index]);
                    CombatUnit next = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == prior.Id);
                    bool fires = index == 0 && operation == "SetMoonPhase" && value == 1;
                    Require(prior.Subtypes.Contains("SubtypesData_Silent") == (index == 0) &&
                        (next.Status("armor")?.Stacks ?? 0) - (prior.Status("armor")?.Stacks ?? 0) == (fires ? 10 : 0),
                        "New must add10 only to Silent; Full, room/resource refresh and selections must not add armor.");
                    if (fires) armorGains++;
                }
                if (operation == "Refresh" && !guardsChecked)
                    foreach (string field in new[] { "MoonPhaseMask", "RequiredSubtype", "Stacks" })
                    {
                        var malformed = System.Text.Json.Nodes.JsonNode.Parse(parent)!;
                        var rule = malformed["Context"]!["Relics"]!.AsArray().Single(node => node!["DataId"]!.GetValue<string>() ==
                            RelicMoonPhaseStatusModel.DataId)!["ResourceEffects"]![0]!["MoonPhaseStatus"]!;
                        if (field == "Stacks") rule["Statuses"]![0]![field] = 11;
                        else if (field == "MoonPhaseMask") rule[field] = 3;
                        else rule[field] = "SubtypesData_Fae";
                        var unknown = JsonSerializer.Deserialize<TrainCombatState>(malformed.ToJsonString(), ModelJson.Options)!;
                        Require(!RoomMoonPhaseModel.Refresh(unknown, false).Supported, "Uncalibrated moon armor payload bypassed the guard.");
                    }
            }
            if (handPower)
            {
                HandPowerRelic(before.Context!); HandPowerRelic(after);
                if (operation == "Refresh" && !guardsChecked) HandPowerGuards(parent);
                foreach (int id in ids)
                {
                    CardInstanceState prior = before.Context!.FindCard(id)!, next = after.FindCard(id)!;
                    int priorCount = prior.Temporary.Upgrades.Count(item => item.DataId == "ba427938-c3ad-480b-aa27-44a6426a059c");
                    CardUpgradeModifier[] upgrades = next.Temporary.Upgrades.Where(item => item.DataId == "ba427938-c3ad-480b-aa27-44a6426a059c").ToArray();
                    bool matches = id != ids[5]; // all five unchanged offensive spells match; Lunacy does not.
                    bool fires = operation == "PreDiscardHand" && before.Context.QueryFrame!.MoonPhase == 2 && matches;
                    Require(upgrades.Length == priorCount + (fires ? 1 : 0), "Full hand power must stack only on matching spells at pre-discard.");
                    int scale = CardMagicPowerModel.Multiplier(next);
                    Require(upgrades.All(item => item.Stats.Damage == 10 * scale && item.Stats.Heal == 10 * scale &&
                        !item.Unique && !item.RemoveOnDiscard), "Native hand power must rescale with the current moon trait and retain its lifetime.");
                    if (fires) powerApplications++;
                    else if (operation == "PreDiscardHand") powerControls++;
                }
            }
            if (phaseEnergy)
            {
                RelicConditionState prior = PhaseEnergyRelic(before.Context!), next = PhaseEnergyRelic(after);
                bool fires = operation == "SetMoonPhase" && value == 2 && prior.DurationTriggerCount == 0;
                int expected = fires ? Math.Min(before.Context!.Resources!.MaxEnergy, before.Context.QueryFrame!.Energy!.Value + 1)
                    : before.Context!.QueryFrame!.Energy!.Value;
                Require(after.QueryFrame!.Energy == expected && next.DurationTriggerCount == prior.DurationTriggerCount + (fires ? 1 : 0),
                    "Original Full must add one energy once per duration; selection/resource refresh must not retrigger.");
                if (fires) energyGains++;
                if (operation == "SetMoonPhase" && value == 2 && !fires) blockedFull++;
                if (operation == "Refresh" && !guardsChecked)
                    foreach (string field in new[] { "Amount", "ParamInt2", "Value" })
                    {
                        var malformed = System.Text.Json.Nodes.JsonNode.Parse(parent)!;
                        var effect = malformed["Context"]!["Relics"]!.AsArray().Single(node => node!["DataId"]!.GetValue<string>() ==
                            "70096a91-220a-4ab3-b8d0-b8fe8f3a819c")!["ResourceEffects"]![0]!;
                        if (field == "Value") effect["Conditions"]![0]![field] = 2;
                        else effect[field] = field == "Amount" ? 2 : 3;
                        var unknown = JsonSerializer.Deserialize<TrainCombatState>(malformed.ToJsonString(), ModelJson.Options)!;
                        Require(!RoomMoonPhaseModel.Refresh(unknown, false).Supported, "Uncalibrated moon energy payload bypassed the guard.");
                    }
            }
            if (phaseHeal)
            {
                PhaseHealRelic(before.Context!); PhaseHealRelic(after);
                CombatUnit prior = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == anchor);
                CombatUnit next = actual.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == anchor);
                Require(prior.Team == CombatTeam.Player && prior.Health == 1 && prior.MaxHealth >= 11 &&
                    next.Health == (operation == "SetMoonPhase" ? 11 : 1),
                    "Phase changes must heal the wounded original anchor by10; room/resource refresh must not heal.");
                if (next.Health > prior.Health) heals++;
                if (operation == "Refresh" && !guardsChecked)
                    foreach (string field in new[] { "HealthAmount", "MoonPhaseMask", "ConditionCount" })
                    {
                        var malformed = System.Text.Json.Nodes.JsonNode.Parse(parent)!;
                        var rule = malformed["Context"]!["Relics"]!.AsArray().Single(node => node!["DataId"]!.GetValue<string>() ==
                            "6d977136-5aac-44fd-9843-54cf2a0bd01d")!["MoonPhaseHealRules"]![0]!;
                        rule[field] = field == "HealthAmount" ? 11 : field == "MoonPhaseMask" ? 3 : 1;
                        var unknown = JsonSerializer.Deserialize<TrainCombatState>(malformed.ToJsonString(), ModelJson.Options)!;
                        Require(!RoomMoonPhaseModel.Refresh(unknown, false).Supported,
                            "Uncalibrated phase-heal amount/mask/conditions bypassed the moon guard.");
                    }
            }
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
        Require(!phaseHeal || heals == 4, "Missing four positive native phase-healing boundaries.");
        Require(!statusRelic || armorGains == 2, "Missing two positive original New armor boundaries.");
        if (statusRelic) Console.WriteLine("ROOM-MOON-STATUS PASS: two New +10 armor witnesses, 13 nontriggering boundaries, non-Silent exclusion and exact guards.");
        Require(!phaseEnergy || energyGains == 1 && blockedFull == 1, "Missing positive Full and repeated-Full once-per-turn controls.");
        if (phaseEnergy) Console.WriteLine("ROOM-MOON-PHASE-ENERGY PASS: original +1 Full, repeated-Full blocked, 11 room/resource/selection controls and exact guards.");
        if (phaseHeal) Console.WriteLine("ROOM-MOON-PHASE-HEAL PASS: 4 native +10 heals, 11 nonhealing controls and exact original relic guards.");
        Require(!handPower || powerApplications == 15 && powerControls == 9, "Missing three Full stack witnesses, New controls or Lunacy exclusions.");
        if (handPower) Console.WriteLine("ROOM-MOON-HAND-POWER PASS: 15 spell applications, 9 phase/filter controls, repeated Full stacking and subsequent 5x/1x rescaling.");
        Console.WriteLine($"ROOM-MOON-SPELL-UPGRADES PASS boundaries={boundaries} originalSpells={ids.Length} lunacyResourceControls={advance} costRelicControls={costRelic} attackRelicControls={attackRelic} healingRelicControls={healingRelic} traitSnapshots={witnesses} activeChanges={changes} exclusiveWitnesses={exclusive} additiveWitnesses={additive} selections={selections} immutableBranchesPerBoundary=8");
    }
    internal static int LunacyEnergyAfter(BattleTurnState before, int paid, bool phaseEnergy)
    {
        if (!phaseEnergy) return 0;
        RelicConditionState condition = PhaseEnergyRelic(before.Spawn.Train.Context!);
        bool reachesFull = paid >= (before.MoonPhase == "New" ? 1 : 2);
        return condition.DurationTriggerCount == 0 && reachesFull ? Math.Min(1, before.Spawn.Train.Context!.Resources!.MaxEnergy) : 0;
    }
    internal static void HandPowerRelic(CombatContext context)
    {
        Require(RelicModel.Validate(context) == null && RelicMoonPhaseHandUpgradeModel.CompatibleMoonSpells(
            context.Relics!.Single(owner => owner.DataId == RelicMoonPhaseHandUpgradeModel.OwnerId)),
            "Original hand magic-power composition signature changed.");
    }
    private static void HandPowerGuards(string parent)
    {
        foreach (string field in new[] { "Damage", "MoonPhaseMask", "Unique", "Filter", "ExcludedPool" })
        {
            var malformed = System.Text.Json.Nodes.JsonNode.Parse(parent)!;
            var rule = malformed["Context"]!["Relics"]!.AsArray().Single(node => node!["DataId"]!.GetValue<string>() ==
                RelicMoonPhaseHandUpgradeModel.OwnerId)!["CardModifiers"]![0]!["MoonPhaseHandUpgrade"]!;
            if (field == "Damage") rule["Upgrade"]!["Stats"]!["Damage"] = 11;
            else if (field == "MoonPhaseMask") rule[field] = 3;
            else if (field == "Unique") rule["Upgrade"]![field] = true;
            else if (field == "Filter") rule["Filters"]![0]!["Effects"]!["Required"]![0] = "CardEffectAddStatusEffect";
            else rule["Filters"]![1]!["DisallowedPools"]![0]![0] = "unknown";
            var unknown = JsonSerializer.Deserialize<TrainCombatState>(malformed.ToJsonString(), ModelJson.Options)!;
            Require(!RoomMoonPhaseModel.Refresh(unknown, false).Supported, "Uncalibrated moon hand-power payload bypassed the guard: " + field);
        }
    }
    internal static void HandPowerCallbacks(FixtureValue fixture)
    {
        int full = 0, newMoon = 0, applications = 0, moonApplications = 0;
        foreach (FixtureValue row in fixture.GetProperty("MoonHandPowerCallbacks").EnumerateArray())
        {
            CombatContext before = row.GetProperty("Before").Deserialize<CombatContext>()!;
            CombatContext actual = row.GetProperty("After").Deserialize<CombatContext>()!;
            HandPowerRelic(before); HandPowerRelic(actual);
            int phase = before.QueryFrame!.MoonPhase!.Value;
            Require(phase is 1 or 2 && row.GetProperty("Completed").GetBoolean(), "Native hand-power callback did not settle.");
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            RelicMoonPhaseHandUpgradeResult replay = RelicMoonPhaseHandUpgradeModel.Apply(before, phase, deployment: false, preview: false);
            string? difference = replay.UnsupportedReason ?? ModelJson.Difference(JsonSerializer.Serialize(replay.Context, ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options));
            Require(difference == null && JsonSerializer.Serialize(before, ModelJson.Options) == immutable,
                "Original native hand-power callback full context differs: " + difference);
            foreach (CardToken token in before.Cards.Hand)
            {
                CardInstanceState prior = before.FindCard(token.InstanceId)!, next = actual.FindCard(token.InstanceId)!;
                int delta = next.Temporary.Upgrades.Count(upgrade => upgrade.DataId == "ba427938-c3ad-480b-aa27-44a6426a059c") -
                    prior.Temporary.Upgrades.Count(upgrade => upgrade.DataId == "ba427938-c3ad-480b-aa27-44a6426a059c");
                Require(delta is 0 or 1 && (phase == 2 || delta == 0), "Hand power applied outside Full or duplicated within one callback.");
                applications += delta;
                if (prior.MaskDescriptor?.SourceTraits?.Any(trait => trait.Type == CardMoonUpgradeModel.Trait) == true)
                    moonApplications += delta;
            }
            if (phase == 2) full++; else newMoon++;
        }
        Require(full >= 1 && newMoon >= 1 && moonApplications >= 1, "Missing actual Full/New hand-power callbacks and positive original moon-spell upgrades.");
        Console.WriteLine($"MOON-HAND-POWER-CALLBACKS PASS Full={full} New={newMoon} applications={applications} moonSpellApplications={moonApplications} completeNativeContexts=true");
    }
    internal static RelicConditionState PhaseEnergyRelic(CombatContext context)
    {
        Require(RelicModel.Validate(context) == null, "Invalid phase-energy relic context.");
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == "70096a91-220a-4ab3-b8d0-b8fe8f3a819c");
        return PhaseEnergyOwner(owner);
    }
    private static RelicConditionState PhaseEnergyOwner(CombatRelicState owner)
    {
        RelicResourceEffect effect = owner.ResourceEffects!.Single();
        RelicConditionState condition = effect.Conditions.Single();
        Require(owner.AssetKey == "MoonPhaseModifyEmber" && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual([RelicResourceModel.MoonPhaseEnergy]) && effect.EffectIndex == 0 &&
            effect.Type == RelicResourceModel.MoonPhaseEnergy && effect.ParamInt == 1 && effect.ParamInt2 == 2 && effect.Amount == 1 &&
            !effect.CanApplyInPreviewMode && effect.RequiredMutatorId == null && condition.Query.Type == "MoonPhase" &&
            condition.Query.Duration == "ThisTurn" && condition.TrackTriggerCount && condition.Comparator == 6 &&
            condition.Value == 1 && !condition.AllowMultiple,
            "Original phase-energy relic signature changed.");
        return condition;
    }
    internal static void PhaseEnergyCallbacks(FixtureValue fixture)
    {
        FixtureValue[] rows = fixture.GetProperty("RelicResourceOperations").EnumerateArray()
            .Where(row => row.GetProperty("Kind").GetString() == "MoonPhaseEnergy").ToArray();
        int full = 0, newMoon = 0, gains = 0;
        foreach (FixtureValue row in rows)
        {
            Require(row.GetProperty("OwnerDataId").GetString() == "70096a91-220a-4ab3-b8d0-b8fe8f3a819c" &&
                row.GetProperty("EffectIndex").GetInt32() == 0 && row.GetProperty("After").ValueKind == FixtureKind.Object,
                "Missing settled original phase-energy callback.");
            FixtureValue before = row.GetProperty("Before"), after = row.GetProperty("After");
            Require(!before.GetProperty("Preview").GetBoolean() && !after.GetProperty("Preview").GetBoolean() &&
                before.GetProperty("Turn").GetInt32() == after.GetProperty("Turn").GetInt32(),
                "Original phase-energy callback crossed preview or turn boundaries.");
            RelicConditionState prior = PhaseEnergyOwner(before.GetProperty("Relics").Deserialize<CombatRelicState[]>()!
                .Single(owner => owner.DataId == "70096a91-220a-4ab3-b8d0-b8fe8f3a819c"));
            RelicConditionState next = PhaseEnergyOwner(after.GetProperty("Relics").Deserialize<CombatRelicState[]>()!
                .Single(owner => owner.DataId == "70096a91-220a-4ab3-b8d0-b8fe8f3a819c"));
            int energy = before.GetProperty("Energy").GetInt32(), actual = after.GetProperty("Energy").GetInt32();
            if (before.GetProperty("MoonPhase").GetString() == "Full")
            {
                Require(actual == Math.Min(before.GetProperty("Resources").GetProperty("MaxEnergy").GetInt32(), energy + 1) &&
                    prior.DurationTriggerCount == 0 && next.Triggered && next.DurationTriggerCount == 1 && next.ValueAtLastTrigger == 0,
                    "Full did not apply the original +1/once-per-turn condition.");
                full++; if (actual > energy) gains++;
            }
            else
            {
                Require(before.GetProperty("MoonPhase").GetString() == "New" && actual == energy &&
                    JsonSerializer.Serialize(prior, ModelJson.Options) == JsonSerializer.Serialize(next, ModelJson.Options),
                    "New incorrectly changed original energy or condition history.");
                newMoon++;
            }
        }
        Require(full >= 2 && newMoon >= 2 && gains > 0, "Missing repeated native turns, positive Full energy or New controls.");
        Console.WriteLine($"MOON-PHASE-ENERGY-CALLBACKS PASS: {full} Full/{newMoon} New original callbacks, {gains} positive gains; once-per-turn histories match.");
    }
    internal static void PhaseHealRelic(CombatContext context)
    {
        CombatRelicState owner = context.Relics!.Single(relic => relic.DataId == "6d977136-5aac-44fd-9843-54cf2a0bd01d");
        Require(owner.AssetKey == "MooncycleRestoreFriendlyHealth" && owner.IsCovenant == false &&
            owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual([RelicMoonPhaseHealModel.Effect]) &&
            owner.MoonPhaseHealRules!.Select(rule => (rule.EffectIndex, rule.TargetPlayer, rule.TargetEnemy, rule.TargetMode,
                rule.RequiredSubtype, rule.HealthAmount, rule.MoonPhaseMask, rule.ConditionCount))
                .SequenceEqual([(0, true, false, "WeakestAllRooms", (string?)null, 10, -1, 0)]) &&
            RelicModel.Validate(context) == null, "Original phase-healing relic signature changed.");
    }
    internal static void PhaseHealCallbacks(FixtureValue fixture)
    {
        FixtureValue[] operations = fixture.GetProperty("RelicMoonPhaseHealOperations").EnumerateArray().ToArray();
        Require(operations.Length >= 2 && operations.All(row =>
            row.GetProperty("OwnerDataId").GetString() == "6d977136-5aac-44fd-9843-54cf2a0bd01d" &&
            row.GetProperty("EffectIndex").GetInt32() == 0 && row.GetProperty("SourceTeam").GetString() == "Monsters" &&
            row.GetProperty("TargetMode").GetString() == "WeakestAllRooms" && row.GetProperty("HealthAmount").GetInt32() == 10 &&
            row.GetProperty("MoonPhaseMask").GetInt32() == -1 && row.GetProperty("After").ValueKind == FixtureKind.Array) &&
            new[] { "New", "Full" }.All(phase => operations.Any(row => row.GetProperty("Phase").GetString() == phase)),
            "Missing settled native healing callbacks in both phases while moon spells are active.");
        int positive = operations.Sum(row => row.GetProperty("After").EnumerateArray().Count(after =>
            row.GetProperty("Before").EnumerateArray().Any(before =>
                before.GetProperty("UnitId").GetInt32() == after.GetProperty("UnitId").GetInt32() &&
                before.GetProperty("Health").GetInt32() < after.GetProperty("Health").GetInt32())));
        Console.WriteLine($"MOON-PHASE-HEAL-CALLBACKS PASS: {operations.Length} native New/Full callbacks, {positive} positive heals; full states are checked by the battle/pair gate.");
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
