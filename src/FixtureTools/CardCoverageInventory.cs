using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MonsterTrain2Poju.Fixtures;

internal static class CardCoverageInventory
{
    private const string GameModule = "8fb07b96-f4db-4d2b-884d-c00536d6ccf4";
    private const string CatalogHash = "c9ea090d631034900d459f456e0027b8aa6863bb2f57e6de31fba6e6a4027723";
    private sealed class Card(FixtureValue definition)
    {
        internal readonly FixtureValue Definition = definition;
        internal FixtureValue? NativeKillTriggers;
        internal string Id => Text(Definition, "Id");
        internal string Name => Text(Definition, "AssetKey");
        internal string Type => Text(Definition, "Type");
        internal bool Logbook => Flag(Definition, "AppearsInLogbook");
        internal string Role => Logbook ? "logbook" : Flag(Definition, "IsUnitAbility") ? "unit-ability" :
            Flag(Definition, "IsRoomAbility") ? "room-ability" : "other-definition";
        internal readonly SortedDictionary<string, Plays> Evidence = new(StringComparer.Ordinal);
        internal readonly SortedSet<string> Gaps = new(StringComparer.Ordinal);
        internal readonly SortedSet<string> Mechanisms = new(StringComparer.Ordinal);
        internal string Coverage = "unreviewed";
        internal int DirectPlays => Evidence.Values.Sum(value => value.Direct.Count);
        internal int OtherPlays => Evidence.Values.Sum(value => value.Other.Count);
    }
    private sealed class Plays
    {
        internal readonly List<int> Direct = [];
        internal readonly List<int> Other = [];
    }
    private sealed record TurnWitness(string DataId, string Archive, int TurnIndex, int SourceCardId, string Effect, string Observation);
    private sealed record LegalityWitness(string DataId, string Archive, int QueryIndex, int CardId, int RoomIndex, string Error, bool CanDiscard);

    internal static void Run(string workspace, bool check)
    {
        workspace = Path.GetFullPath(workspace);
        string fixtureDirectory = Path.Combine(workspace, "tests", "fixtures");
        string catalogPath = Path.Combine(workspace, "tests", "catalogs", "card-catalog.mt2f");
        Require(Hash(catalogPath) == CatalogHash, "Original card catalog hash changed; review its provenance before updating the inventory.");
        using var catalog = FixtureDocument.Read(catalogPath);
        FixtureValue root = catalog.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && Text(root, "Scope") == "AllGameData" &&
            Text(root, "GameModuleMvid") == GameModule && Flag(root, "BranchCardMasksEnabled") && Flag(root, "DecisionUnchanged") &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")), "Native catalog identity/state/RNG/frame guard failed.");
        Card[] cards = root.GetProperty("Cards").EnumerateArray().Select(value => new Card(value)).OrderBy(card => card.Name, StringComparer.Ordinal).ToArray();
        var byId = cards.ToDictionary(card => card.Id, StringComparer.Ordinal);
        foreach (FixtureValue nativeDefinition in Array(root, "CardKillTriggerDefinitions"))
            if (byId.TryGetValue(Text(nativeDefinition, "CardDataId"), out Card? triggered) &&
                Array(nativeDefinition, "Triggers").Any(trigger => Strings(trigger, "Effects").Any(effect => effect.StartsWith("CardTriggerEffect", StringComparison.Ordinal))))
                triggered.NativeKillTriggers = nativeDefinition.GetProperty("Triggers");
        Require(cards.Length == 668 && cards.Count(card => card.Logbook) == 563, "Incomplete original card or logbook catalog.");
        using (var calibration = FixtureDocument.Read(Path.Combine(fixtureDirectory, "card-upgrade-mask-calibration.mt2f")))
        {
            FixtureValue[] facts = calibration.RootElement.GetProperty("CardData").EnumerateArray().ToArray();
            Require(facts.Length == cards.Length, "Accepted original definition calibration has a different scope.");
            foreach (FixtureValue fact in facts)
                Require(byId.TryGetValue(Text(fact, "DataId"), out Card? card) &&
                    Equivalent(card.Definition.GetProperty("Mask"), fact, legacyMask: true), "Catalog differs from accepted raw definition facts: " + Text(fact, "DataId"));
        }

        string[] manifest = File.ReadAllLines(Path.Combine(fixtureDirectory, "manifest.tsv"));
        Require(manifest[0] == "archive\tsource\tsource_bytes\tsource_sha256\tarchive_bytes\tarchive_sha256\tunique_nodes", "Unknown fixture manifest schema.");
        var mechanisms = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var excluded = new SortedDictionary<string, string>(StringComparer.Ordinal);
        int indexed = 0, actionCount = 0, nonCatalogActions = 0, unresolvedActions = 0;
        var triggerWitnesses = new List<TurnWitness>();
        var legalityWitnesses = new List<LegalityWitness>();
        foreach (string line in manifest.Skip(1).Order(StringComparer.Ordinal))
        {
            string[] row = line.Split('\t');
            Require(row.Length == 7 && Path.GetFileName(row[0]) == row[0], "Invalid fixture manifest row.");
            string archive = row[0];
            if (!archive.StartsWith("full-battle-", StringComparison.Ordinal)) continue;
            string path = Path.Combine(fixtureDirectory, archive);
            Require(new FileInfo(path).Length == long.Parse(row[4], CultureInfo.InvariantCulture) && Hash(path).Equals(row[5], StringComparison.OrdinalIgnoreCase), "Fixture manifest integrity failed: " + archive);
            using var fixture = FixtureDocument.Read(path);
            FixtureValue battle = fixture.RootElement;
            Require(Text(battle, "GameModuleMvid") == GameModule, "Fixture has a different game module: " + archive);
            string[] counters = ["CaptureFailures", "Mismatches", "Unsupported", "Pending"];
            string[] counterErrors = counters.Select(name => !battle.TryGetProperty(name, out FixtureValue value) || value.ValueKind != FixtureKind.Number ? name + "=absent" :
                value.GetInt32() != 0 ? name + "=" + value.GetInt32().ToString(CultureInfo.InvariantCulture) : "").Where(error => error != "").ToArray();
            if (counterErrors.Length > 0)
            {
                excluded.Add(archive, string.Join(";", counterErrors));
                continue;
            }
            indexed++;
            if (!battle.TryGetProperty("Actions", out FixtureValue actions)) continue;
            int index = -1;
            foreach (FixtureValue record in actions.EnumerateArray())
            {
                index++;
                if (!record.TryGetProperty("Predicted", out FixtureValue predicted) || !Flag(predicted, "Supported") ||
                    !record.TryGetProperty("Actual", out FixtureValue actual) || actual.ValueKind != FixtureKind.Object ||
                    !record.TryGetProperty("Difference", out FixtureValue difference) || difference.ValueKind != FixtureKind.Null) continue;
                actionCount++;
                FixtureValue before = record.GetProperty("Before"), action = record.GetProperty("Action");
                FixtureValue context = before.GetProperty("Spawn").GetProperty("Train").GetProperty("Context");
                string? id = FindCardId(context, action.GetProperty("CardInstanceId").GetInt32());
                if (id == null) { unresolvedActions++; continue; }
                if (!before.TryGetProperty("PlayRules", out FixtureValue rules) || rules.ValueKind != FixtureKind.Object) { unresolvedActions++; continue; }
                FixtureValue rule = rules.GetProperty("Cards").EnumerateArray().FirstOrDefault(value => Text(value, "DataId") == id);
                if (rule.ValueKind != FixtureKind.Object) { unresolvedActions++; continue; }
                foreach (string primitive in Primitives(rule, includeUnplayed: false)) mechanisms.TryAdd(primitive, archive);
                if (!byId.TryGetValue(id, out Card? card)) { nonCatalogActions++; continue; }
                if (!card.Evidence.TryGetValue(archive, out Plays? plays)) card.Evidence.Add(archive, plays = new());
                FixtureValue original = card.Definition.GetProperty("Rule");
                bool matches = original.ValueKind == FixtureKind.Object && Equivalent(original, rule, ruleRoot: true, cardRule: true) &&
                    MatchesSourceMetadata(card.Definition, context, action.GetProperty("CardInstanceId").GetInt32()) &&
                    MatchesNativeKillTriggers(card, context, action.GetProperty("CardInstanceId").GetInt32());
                (matches ? plays.Direct : plays.Other).Add(index);
            }
            CollectUnplayedWitnesses(battle, archive, mechanisms, triggerWitnesses);
            CollectLegalityWitnesses(battle, archive, byId, legalityWitnesses);
        }
        foreach (Card card in cards)
        {
            FixtureValue rule = card.Definition.GetProperty("Rule");
            if (rule.ValueKind != FixtureKind.Object || Text(card.Definition, "ProjectionError") != "")
            {
                card.Gaps.Add("definition-projection-error");
                continue;
            }
            CollectGuards(rule, "$", card.Gaps);
            string pipeline = Text(rule, "Effect");
            bool unsupported = Strings(rule, "ExternalInteractions").Any(value => value.StartsWith("Unimplemented play effect ", StringComparison.Ordinal));
            if (Flag(card.Definition, "IsRoomAbility"))
            {
                if (!rule.TryGetProperty("Ability", out FixtureValue ability) || Text(ability, "Kind") != "Room")
                { unsupported = true; card.Gaps.Add("room-ability-activation-not-modeled"); }
                else if (card.DirectPlays == 0) card.Gaps.Add("room-ability-needs-native-activation-witness");
            }
            foreach (FixtureValue effect in Array(rule, "Effects"))
                if (effect.TryGetProperty("RoomAttachment", out FixtureValue attachment) && attachment.ValueKind == FixtureKind.Object &&
                    attachment.TryGetProperty("Ability", out FixtureValue roomAbility) && roomAbility.ValueKind == FixtureKind.Object &&
                    (!byId.TryGetValue(Text(roomAbility, "DataId"), out Card? abilityCard) || abilityCard.DirectPlays == 0))
                    card.Gaps.Add("room-ability-needs-native-activation-witness");
            if (Flag(card.Definition, "IsUnitAbility") && pipeline != "Spell" && !(pipeline == "Null" && Array(rule, "Effects").Length == 0))
            {
                unsupported = true;
                card.Gaps.Add("unit-ability-pipeline-not-modeled:" + pipeline);
            }
            bool missingEvidence = false;
            foreach (string primitive in Primitives(rule))
            {
                if (mechanisms.TryGetValue(primitive, out string? archive)) card.Mechanisms.Add(primitive + "=" + archive);
                else { missingEvidence = true; card.Gaps.Add("no-indexed-transition-witness:" + primitive); }
            }
            card.Coverage = unsupported ? "unsupported" : card.Gaps.Any(gap => !gap.StartsWith("no-indexed-transition-witness:", StringComparison.Ordinal)) ? "partial" :
                card.DirectPlays > 0 ? "direct-native-play" : missingEvidence ? "unreviewed" : "mechanism-only";
        }

        var inventory = new StringBuilder();
        Row(inventory, "DataId", "AssetKey", "CardType", "Role", "InLogbook", "CountsTowardsMastery", "Rarity", "LinkedClan", "SpawnCharacter",
            "MainEffects", "Traits", "CardTriggers", "Coverage", "MatchingDefinitionPlays", "OtherRecordedPlays", "DirectFixtures", "OtherFixtures", "MechanismEvidence", "KnownGaps", "ProjectionError");
        foreach (Card card in cards)
            Row(inventory, card.Id, card.Name, card.Type, card.Role, card.Logbook.ToString().ToLowerInvariant(), Flag(card.Definition, "CountsTowardsMastery").ToString().ToLowerInvariant(),
                Text(card.Definition, "Rarity"), Text(card.Definition, "LinkedClan"), Text(card.Definition, "SpawnCharacter"),
                Join(Array(card.Definition, "Effects").Select(effect => Text(effect, "Type"))), Join(Array(card.Definition, "Traits").Select(trait => Text(trait, "Type"))),
                Join(Array(card.Definition, "Triggers").Select(trigger => Text(trigger, "Kind"))), card.Coverage,
                card.DirectPlays.ToString(CultureInfo.InvariantCulture), card.OtherPlays.ToString(CultureInfo.InvariantCulture),
                Join(card.Evidence.Where(pair => pair.Value.Direct.Count > 0).Select(pair => pair.Key)), Join(card.Evidence.Where(pair => pair.Value.Other.Count > 0).Select(pair => pair.Key)),
                Join(card.Mechanisms), Join(card.Gaps), Text(card.Definition, "ProjectionError"));
        var evidence = new StringBuilder();
        Row(evidence, "DataId", "AssetKey", "Archive", "MatchingDefinitionPlays", "OtherRecordedPlays", "MatchingActionIndices", "OtherActionIndices");
        foreach (Card card in cards)
        foreach ((string archive, Plays plays) in card.Evidence)
            Row(evidence, card.Id, card.Name, archive, plays.Direct.Count.ToString(CultureInfo.InvariantCulture), plays.Other.Count.ToString(CultureInfo.InvariantCulture),
                string.Join(",", plays.Direct), string.Join(",", plays.Other));
        var triggers = new StringBuilder();
        Row(triggers, "DataId", "AssetKey", "Archive", "TurnIndex", "SourceCardId", "Effect", "PositiveObservation");
        foreach (TurnWitness witness in triggerWitnesses.Where(witness => byId.ContainsKey(witness.DataId)))
            Row(triggers, witness.DataId, byId[witness.DataId].Name, witness.Archive, witness.TurnIndex.ToString(CultureInfo.InvariantCulture),
                witness.SourceCardId.ToString(CultureInfo.InvariantCulture), witness.Effect, witness.Observation);
        var legality = new StringBuilder();
        Row(legality, "DataId", "AssetKey", "Archive", "QueryIndex", "CardId", "RoomIndex", "NativeError", "CanDiscard");
        foreach (LegalityWitness witness in legalityWitnesses)
            Row(legality, witness.DataId, byId[witness.DataId].Name, witness.Archive, witness.QueryIndex.ToString(CultureInfo.InvariantCulture),
                witness.CardId.ToString(CultureInfo.InvariantCulture), witness.RoomIndex.ToString(CultureInfo.InvariantCulture), witness.Error,
                witness.CanDiscard.ToString().ToLowerInvariant());
        string report = Report(cards, indexed, excluded, actionCount, nonCatalogActions, unresolvedActions, legalityWitnesses);
        foreach ((string name, string content) in new[] { ("card-inventory.tsv", inventory.ToString()), ("card-play-evidence.tsv", evidence.ToString()),
            ("card-trigger-evidence.tsv", triggers.ToString()), ("card-legality-evidence.tsv", legality.ToString()), ("card-coverage.md", report) })
        {
            string path = Path.Combine(fixtureDirectory, name);
            if (check) Require(File.Exists(path) && File.ReadAllText(path) == content, "Card inventory is stale: " + name);
            else File.WriteAllText(path, content, new UTF8Encoding(false));
        }
        Console.WriteLine($"CARD-INVENTORY {(check ? "CHECK" : "WRITE")} PASS: {cards.Length} definitions/{cards.Count(card => card.Logbook)} logbook entries; {indexed} indexed battles/{actionCount} native actions; {cards.Count(card => card.DirectPlays > 0)} definition-matched card identities; {excluded.Count} excluded captures; no battle regression executed.");
    }

    private static string? FindCardId(FixtureValue context, int instanceId)
    {
        foreach (string name in new[] { "CardInstances", "CardRegistry", "AbilityCardCache" })
        foreach (FixtureValue card in Array(context, name))
            if (card.TryGetProperty("InstanceId", out FixtureValue id) && id.GetInt32() == instanceId) return Text(card, "DataId");
        if (context.TryGetProperty("Cards", out FixtureValue cycle))
        foreach (string pile in new[] { "Hand", "Draw", "Discard" })
        foreach (FixtureValue card in Array(cycle, pile))
            if (card.GetProperty("InstanceId").GetInt32() == instanceId) return Text(card, "DataId");
        return null;
    }

    private static bool MatchesNativeKillTriggers(Card card, FixtureValue context, int instanceId)
    {
        if (card.NativeKillTriggers is not FixtureValue expected) return true;
        return new[] { "CardInstances", "CardRegistry" }.SelectMany(source => Array(context, source))
            .Where(instance => instance.GetProperty("InstanceId").GetInt32() == instanceId)
            .Any(instance => instance.TryGetProperty("MaskDescriptor", out FixtureValue mask) &&
                mask.ValueKind == FixtureKind.Object && mask.TryGetProperty("KillTriggers", out FixtureValue actual) && Equivalent(actual, expected));
    }

    private static bool MatchesSourceMetadata(FixtureValue definition, FixtureValue context, int instanceId)
    {
        // Rule projections do not retain every raw trait parameter. Until those
        // parameters have their own original-card witnesses, do not promote them.
        FixtureValue[] traits = Array(definition, "Traits");
        if (traits.Any(trait => !CalibratedSourceTrait(trait, Text(definition, "Id")))) return false;
        bool requiresRaw = traits.Any(trait => Text(trait, "Type") is "CardTraitScalingAddDamage" or "CardTraitScalingAddStatusEffect" or "CardTraitUnpurgable" or "CardTraitIncreaseCostOfCardsInHand" or "CardTraitLimitedRange" or "CardTraitDrawInDeploymentPhase" or "CardTraitUnplayable" or "CardTraitPersistent" or "CardTraitRefine" or "CardTraitPermafrost" or "CardTraitDamageOverflow" or "CardTraitCorruptRestricted" or "CardTraitAddDamagePerStatusEffect" or "CardTraitScalingAddDamagePerCard" or "CardTraitScalingAddEnergy" or "CardTraitScalingRemoveStatusEffect" or "CardTraitScalingAddEffectApplications" or "CardTraitScalingCardDrawCountPerPile");
        requiresRaw |= traits.Any(trait => Text(trait, "Type") is "CardTraitScalingBuffDamage" or "CardTraitScalingStatusEffectPerPile" or "CardTraitScalingTempCardUpgradeToCard" or "CardTraitScalingByConsumedTempCardUpgradeToCard" or "CardTraitShowCardTargets" or "CardTraitScalingAdvanceMoonPhase" or "CardTraitScalingAddCards" or "CardTraitAddCardUpgradeOnMoonPhase" or "CardTraitScalingFeederRules");
        requiresRaw |= traits.Any(trait => Text(trait, "Type") is "CardTraitGraftedEquipment" or "CardTraitTreasure");
        requiresRaw |= traits.Any(trait => Text(trait, "Type") is "CardTraitScalingAdjustCapacity" or "CardTraitScalingHealTrain");
        requiresRaw |= traits.Any(trait => Text(trait, "Type") == "CardTraitInfusion");
        foreach (string source in new[] { "CardInstances", "CardRegistry" })
        foreach (FixtureValue instance in Array(context, source))
        {
            if (instance.GetProperty("InstanceId").GetInt32() != instanceId) continue;
            if (!instance.TryGetProperty("MaskDescriptor", out FixtureValue mask) || mask.ValueKind != FixtureKind.Object) continue;
            bool hasRaw = mask.TryGetProperty("SourceTraits", out FixtureValue raw) && raw.ValueKind == FixtureKind.Array;
            if (requiresRaw && !hasRaw || hasRaw && !Equivalent(raw, definition.GetProperty("Traits"))) return false;
            if (!mask.TryGetProperty("Definition", out FixtureValue sourceDefinition) || sourceDefinition.ValueKind != FixtureKind.Object ||
                Text(sourceDefinition, "DataId") != Text(definition, "Id") || Text(sourceDefinition, "CardType") != Text(definition, "Type") ||
                !Strings(sourceDefinition, "MainEffectNames").SequenceEqual(Array(definition, "Effects").Select(effect => Text(effect, "Type")))) return false;
            if (!mask.TryGetProperty("BaseTraits", out FixtureValue baseTraits) || baseTraits.ValueKind != FixtureKind.Array) continue;
            FixtureValue[] bases = baseTraits.EnumerateArray().ToArray();
            if (Text(definition, "Id") == "108b9153-0236-4ba2-ad20-d03a991bbd57" && bases.Length == traits.Length + 1 &&
                bases.Count(trait => Text(trait, "RuntimeType") == "CardTraitStrongerMagicPower") == 1 &&
                bases.Any(trait => Text(trait, "RuntimeType") == "CardTraitAddCardUpgradeOnMoonPhase" &&
                    trait.TryGetProperty("MoonPhaseUpgradeState", out FixtureValue state) && Flag(state, "Active") && Flag(state, "Cached")) &&
                instance.TryGetProperty("Permanent", out FixtureValue permanent) && Array(permanent, "Upgrades").Any(upgrade =>
                    Text(upgrade, "DataId") == "328c86e6-19c4-4bf8-a717-4bae655edf2b" &&
                    upgrade.TryGetProperty("Lifecycle", out FixtureValue life) && Strings(life, "TraitsModified").SequenceEqual(new[] { "CardTraitStrongerMagicPower" })))
                bases = bases.Where(trait => Text(trait, "RuntimeType") != "CardTraitStrongerMagicPower").ToArray();
            if (!bases.Select(trait => Text(trait, "RuntimeType")).SequenceEqual(traits.Select(trait => Text(trait, "Type")))) return false;
            for (int index = 0; index < traits.Length; index++)
                if (!bases[index].TryGetProperty("DefinitionParamInt", out FixtureValue value) ||
                    !value.ContentEquals(traits[index].GetProperty("Int")) || Text(traits[index], "Type") != "CardTraitSelfPurge" &&
                    (Text(bases[index], "DeclaredName") != Text(traits[index], "Type") || !bases[index].GetProperty("ParamInt").ContentEquals(value)) ||
                    Text(traits[index], "Type") == "CardTraitIncreaseCostOfCardsInHand" &&
                    Text(bases[index], "HandCostTargetType") != Text(traits[index], "HandCostTargetType") ||
                    Text(traits[index], "Type") == "CardTraitGraftedEquipment" &&
                    (!bases[index].TryGetProperty("GraftSevered", out FixtureValue severed) || severed.ValueKind is not (FixtureKind.True or FixtureKind.False))) return false;
                else if (Text(traits[index], "Type") == "CardTraitScalingFeederRules" &&
                    (!bases[index].TryGetProperty("FeedingQuery", out FixtureValue query) || query.ValueKind != FixtureKind.Object ||
                     Text(query, "Type") != "PlayedCost" || Text(query, "Duration") != "ThisBattle" ||
                     !query.TryGetProperty("TypeMatchDataIds", out FixtureValue typeIds) || typeIds.ValueKind != FixtureKind.Null ||
                     Strings(query, "SubtypeMatchDataIds").Any() || !Flag(query, "SubtypeIsNone") || Text(query, "Subtype") != "SubtypesData_None" ||
                     !query.TryGetProperty("SpecificCardDataId", out FixtureValue specific) || specific.ValueKind != FixtureKind.Null ||
                     !query.TryGetProperty("SourceRawCost", out FixtureValue rawCost) || rawCost.ValueKind != FixtureKind.Null ||
                     Flag(query, "VariableCost") || Text(query, "TargetTeam") != "None" || Flag(query, "CurrentCostCardModifiersOnly") ||
                     Flag(query, "CurrentCostSourceIsSpell") || !query.TryGetProperty("StatusIds", out FixtureValue statusIds) || statusIds.ValueKind != FixtureKind.Null)) return false;
            return true;
        }
        return false; // Legacy schemas without base-trait evidence stay unverified.
    }

    private static bool CalibratedSourceTrait(FixtureValue trait, string cardId)
    {
        string type = Text(trait, "Type");
        bool infusion = cardId is "3cf25a9b-daa1-42cd-8174-eb0f59cd590f" or "b4029b77-12b3-4d88-b10b-c6c0e6b62db8" or
            "3f0d7968-ba33-4ad7-94d7-b0f867de837a" or "af13aa32-5442-408a-b491-229362c634f8";
        bool actorCorrupt = cardId is "c59d1814-0e0e-414c-b82f-a71cead2951f" or "127b5582-1e59-4ce0-afdb-9ccc2d59f913";
        if (infusion && (type is "CardTraitInfusion" or "CardTraitPersistent") || actorCorrupt && type == "CardTraitCorruptRestricted")
            return trait.GetProperty("Int").GetInt32() == (infusion ? 0 : cardId == "c59d1814-0e0e-414c-b82f-a71cead2951f" ? 1 : 4) &&
                trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
                Flag(trait, "Bool") == (cardId == "c59d1814-0e0e-414c-b82f-a71cead2951f") &&
                Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn" &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag", "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (cardId == "5afcb98c-e53a-4f70-b2f8-70b21bf2303c")
            return type == "CardTraitScalingAddStatusEffect" && trait.GetProperty("Int").GetInt32() == 1 &&
                trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
                !Flag(trait, "Bool") && Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 0 &&
                !Flag(trait, "Scaling") && Text(trait, "TrackedValue") == "TypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn" &&
                Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "multistrike" }) &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag", "SpecificCardDataId", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (cardId is "c6484604-b077-43ce-84a4-0179d2f36352" or "4cad5d6e-3c7e-4993-9d3d-53948dae4b6d" ||
            cardId == "6a3633b6-4ecc-43b1-bb56-548f56cf1d60" && type == "CardTraitShowCardTargets")
            return type == "CardTraitShowCardTargets" && trait.GetProperty("Int").GetInt32() == 0 &&
                trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
                !Flag(trait, "Bool") && Text(trait, "Str").Length == 0 && trait.GetProperty("Float").GetDouble() == 0 &&
                !Flag(trait, "Scaling") && Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn" &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag", "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (cardId is "7a01774e-1e0f-4551-8691-cd0dd2f372ea" or "70911970-988b-4535-b46e-e6aa3987fb6c")
        {
            bool collapse = cardId == "7a01774e-1e0f-4551-8691-cd0dd2f372ea";
            bool attackUpgrade = type == "CardTraitScalingUpgradeUnitAttack";
            return (collapse ? type is "CardTraitScalingAddDamage" or "CardTraitExhaustState" :
                    type is "CardTraitScalingAddDamage" or "CardTraitScalingUpgradeUnitAttack") &&
                trait.GetProperty("Int").GetInt32() == (collapse ? 30 : attackUpgrade ? 5 : 15) &&
                trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
                !Flag(trait, "Bool") && Text(trait, "Str").Length == 0 &&
                trait.GetProperty("Float").GetDouble() == (attackUpgrade ? 0 : 1) && !Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == (collapse ? "CapacityOfTargetRoom" : "PlayedCost") &&
                Text(trait, "EntryDuration") == "ThisTurn" &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag", "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        }
        if (cardId == "9d04efb5-8794-42a8-8282-e8ef895e6c3a")
            return type == "CardTraitCorruptRestricted" && trait.GetProperty("Int").GetInt32() == 6 &&
                trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
                !Flag(trait, "Bool") && Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 0 &&
                !Flag(trait, "Scaling") && Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn" &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag", "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (cardId == "78996030-e8d1-4731-897e-ee4b809fa26c")
            return type == "CardTraitScalingUpgradeUnitAttack" && trait.GetProperty("Int").GetInt32() == 8 &&
                trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
                !Flag(trait, "Bool") && Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 1 &&
                Flag(trait, "Scaling") && Text(trait, "TrackedValue") == "CorruptionInTargetRoom" && Text(trait, "EntryDuration") == "ThisTurn" &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag", "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (cardId == "e6e77821-f0c2-4d76-bc7f-2efece0e065f")
            return type == "CardTraitScalingAddDamage" && trait.GetProperty("Int").GetInt32() == 25 &&
                trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
                !Flag(trait, "Bool") && Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 1 &&
                !Flag(trait, "Scaling") && Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn" &&
                Text(trait, "SpecificCardDataId") == "" && Text(trait, "StatusQueryTeam") == "" &&
                !Strings(trait, "StatusQueryStatuses").Any() && !Strings(trait, "StatusEffectIds").Any();
        if (cardId == "34909c50-4cba-41fa-8b33-64c1e463691c")
            return type == "CardTraitScalingAddStatusEffect" && trait.GetProperty("Int").GetInt32() == 1 &&
                trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && Flag(trait, "Bool") &&
                Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "LastAttackDamageDealt" && Text(trait, "EntryDuration") == "ThisTurn" &&
                Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "unstable" }) &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag",
                    "SpecificCardDataId", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (cardId == "0c718572-ef91-4152-b6a9-0fdf907a9b06" &&
            type is "CardTraitScalingAddStatusEffect" or "CardTraitExhaustState")
            return trait.GetProperty("Int").GetInt32() == 1 && trait.GetProperty("Int2").GetInt32() == 2 &&
                trait.GetProperty("Int3").GetInt32() == 1 && !Flag(trait, "Bool") && Text(trait, "Str") == "" &&
                trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "TypeInExhaustPile" && Text(trait, "EntryDuration") == "ThisBattle" &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag",
                    "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (cardId == "e022d66a-f794-4ff9-95a6-b3cc53f49a27" && type == "CardTraitExhaustState")
            return trait.GetProperty("Int").GetInt32() == 0 && trait.GetProperty("Int2").GetInt32() == 0 &&
                trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") && Text(trait, "Str") == "" &&
                trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn" &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag",
                    "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (cardId == "8d291f9a-30a1-4a6d-8d9e-6dc2df4432d1" && type == "CardTraitScalingUpgradeUnitAttack")
            return trait.GetProperty("Int").GetInt32() == 4 && trait.GetProperty("Int2").GetInt32() == 0 &&
                trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") && Text(trait, "Str") == "" &&
                trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisBattle" &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag",
                    "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (cardId == "952db29f-b585-41be-bfdd-0a36b9b05010" &&
            type is "CardTraitScalingUpgradeUnitAttack" or "CardTraitScalingUpgradeUnitHealth")
            return trait.GetProperty("Int").GetInt32() == 30 && trait.GetProperty("Int2").GetInt32() == 0 &&
                trait.GetProperty("Int3").GetInt32() == 1 && !Flag(trait, "Bool") && Text(trait, "Str") == "" &&
                trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisTurn" &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag",
                    "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (cardId == "8d291f9a-30a1-4a6d-8d9e-6dc2df4432d1" && type == "CardTraitExhaustState")
            return trait.GetProperty("Int").GetInt32() == 0 && trait.GetProperty("Int2").GetInt32() == 0 &&
                trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") && Text(trait, "Str") == "" &&
                trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisBattle" &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag",
                    "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (type == "CardTraitSelfPurge") return true;
        if (cardId == "1d2e6259-e3ef-4a6a-8894-2c12db9fd4a9" && type == "CardTraitReturnToHandEquipment")
            return trait.GetProperty("Int").GetInt32() == 0 && trait.GetProperty("Int2").GetInt32() == 0 &&
                trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") && Text(trait, "Str") == "" &&
                trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn" &&
                new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag",
                    "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (type == "CardTraitModifyCardTriggerSlayFireCount") return Flag(trait, "Bool") &&
            Text(trait, "KillTargetTeam") == "Monsters" && Text(trait, "KillTargetSubtype") == "SubtypesData_FunGuy" &&
            trait.GetProperty("Int").GetInt32() == 0 && trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
            Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "AnyCharacter" && Text(trait, "EntryDuration") == "ThisTurn";
        if (type == "CardTraitAddCardUpgradeOnMoonPhase")
        {
            if (!trait.TryGetProperty("MoonPhaseUpgrade", out FixtureValue upgrade) || upgrade.ValueKind != FixtureKind.Object) return false;
            string id = Text(upgrade, "DataId");
            bool newMoon = id is "9492665d-c631-41a8-9b87-5acd0b80bd91" or "6b7b5abc-4397-4e6e-9c25-3db5a7de0dd2";
            bool gravity = id is "6b7b5abc-4397-4e6e-9c25-3db5a7de0dd2" or "3bbf752b-475b-4975-b3c7-b970f44a20b6";
            return (id is "328c86e6-19c4-4bf8-a717-4bae655edf2b" or "12793a9c-fae4-42ab-80bb-c3274e681c19" or
                "9492665d-c631-41a8-9b87-5acd0b80bd91" or "de575a7e-a3cf-45de-a67b-949ac05fcee6" or
                "6b7b5abc-4397-4e6e-9c25-3db5a7de0dd2" or "3bbf752b-475b-4975-b3c7-b970f44a20b6" or "0c025567-26a8-4dae-8dca-5d49cef0e291") &&
                trait.GetProperty("Int").GetInt32() == (newMoon ? 1 : 2) && trait.GetProperty("Int2").GetInt32() == (gravity ? 1 : 0) && trait.GetProperty("Int3").GetInt32() == 0 &&
                Flag(trait, "Bool") == (id != "9492665d-c631-41a8-9b87-5acd0b80bd91") && Text(trait, "Str").Length == 0 &&
                trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") && Text(trait, "TrackedValue") == "MoonPhase" && Text(trait, "EntryDuration") == "ThisTurn";
        }
        if (type == "CardTraitScalingAddCards")
            return trait.GetProperty("Int").GetInt32() == 1 && trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
                Flag(trait, "Bool") && Text(trait, "Str").Length == 0 && trait.GetProperty("Float").GetDouble() == 0 && Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisBattle";
        if (type == "CardTraitScalingFeederRules")
            return trait.GetProperty("Int").GetInt32() == 2 && trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
                !Flag(trait, "Bool") && Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisBattle";
        if (type == "CardTraitScalingAddStatusEffect" && trait.GetProperty("Int").GetInt32() == 1 &&
            trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") &&
            Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisTurn" &&
            (Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "melee weakness" }) ||
             Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "undying" }))) return true;
        if (type == "CardTraitScalingAddStatusEffect" && trait.GetProperty("Int").GetInt32() == 1 &&
            trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") &&
            Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisTurn" &&
            Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "damage shield" })) return true;
        if (type == "CardTraitScalingAddStatusEffect" && trait.GetProperty("Int").GetInt32() == 4 &&
            trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") &&
            Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisBattle" &&
            (Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "buff" }) ||
             Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "armor" }) ||
             Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "valor" }))) return true;
        if (type == "CardTraitExhaustState" && trait.GetProperty("Int").GetInt32() == 3 &&
            trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") &&
            Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisBattle") return true;
        if (type == "CardTraitScalingAddStatusEffect" && trait.GetProperty("Int").GetInt32() == 3 &&
            trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") &&
            Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisBattle" &&
            Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "conduit" })) return true;
        if (type == "CardTraitExhaustState" && trait.GetProperty("Int").GetInt32() == 1 &&
            trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") &&
            Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisBattle") return true;
        if (type == "CardTraitScalingAddStatusEffect" && trait.GetProperty("Int").GetInt32() is 1 or 5 &&
            trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") &&
            Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisBattle" &&
            Strings(trait, "StatusEffectIds").SequenceEqual(new[] { trait.GetProperty("Int").GetInt32() == 1 ? "debuff" : "poison" })) return true;
        if (type == "CardTraitScalingAddStatusEffect" && trait.GetProperty("Int").GetInt32() == 6 &&
            trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") &&
            Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisTurn" &&
            Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "corrupt_poison" })) return true;
        if (type == "CardTraitScalingAdvanceMoonPhase" || type == "CardTraitScalingAddStatusEffect" && trait.GetProperty("Int").GetInt32() == 1)
            return trait.GetProperty("Int").GetInt32() == (type == "CardTraitScalingAdvanceMoonPhase" ? 0 : 1) &&
                trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") &&
                Text(trait, "Str").Length == 0 && trait.GetProperty("Float").GetDouble() == 1 && !Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisTurn" &&
                (type == "CardTraitScalingAdvanceMoonPhase" || Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "conduit" }));
        if (type == "CardTraitScalingStatusEffectPerPile")
            return trait.GetProperty("Int").GetInt32() == 1 && trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 10 &&
                Flag(trait, "Bool") && Text(trait, "Str").Length == 0 && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn" && Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "horde" });
        if (type is "CardTraitScalingCardDrawCountPerPile" or "CardTraitExhaustState" or "CardTraitDistinct" &&
            trait.GetProperty("Int").GetInt32() == 1 && trait.GetProperty("Int2").GetInt32() == 2 && trait.GetProperty("Int3").GetInt32() == 5 &&
            !Flag(trait, "Bool") && Text(trait, "Str").Length == 0 && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn") return true;
        if (type == "CardTraitScalingCardDrawCountPerPile" || type == "CardTraitExhaustState" && trait.GetProperty("Int3").GetInt32() == 30)
            return trait.GetProperty("Int").GetInt32() == (type == "CardTraitExhaustState" ? 2 : 1) && trait.GetProperty("Int2").GetInt32() == 0 &&
                trait.GetProperty("Int3").GetInt32() == (type == "CardTraitExhaustState" ? 30 : 15) && Flag(trait, "Bool") == (type != "CardTraitExhaustState") &&
                Text(trait, "Str").Length == 0 && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
                Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn";
        if (cardId is "135cf22d-6d1b-47ee-b48e-e8f7b47e5750" or "6a3633b6-4ecc-43b1-bb56-548f56cf1d60" && type == "CardTraitDistinct")
            return trait.GetProperty("Int").GetInt32() == 0 && trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
                !Flag(trait, "Bool") && Text(trait, "Str") == "" && Text(trait, "TrackedValue") == "SubtypeInDeck" &&
                Text(trait, "EntryDuration") == "ThisTurn" && Text(trait, "SpecificCardDataId") == "" &&
                Text(trait, "StatusQueryTeam") == "" && !Strings(trait, "StatusQueryStatuses").Any() && !Strings(trait, "StatusEffectIds").Any();
        if (type == "CardTraitAddDamagePerStatusEffect")
            return trait.GetProperty("Int").GetInt32() == 3 && trait.GetProperty("Int2").GetInt32() == 2 &&
                trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") && !Flag(trait, "Scaling") &&
                Text(trait, "Str").Length == 0 && trait.GetProperty("Float").GetDouble() == 2 &&
                Text(trait, "TrackedValue") == "MagicPowerInTargetRoom" && Text(trait, "EntryDuration") == "ThisTurn";
        if (type == "CardTraitScalingAddStatusEffect" && trait.GetProperty("Int").GetInt32() == 2 &&
            trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") &&
            Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "CorruptionInTargetRoom" && Text(trait, "EntryDuration") == "ThisTurn" &&
            Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "armor" })) return true;
        if (type == "CardTraitScalingAddStatusEffect")
            return trait.GetProperty("Int").GetInt32() == 2 &&
                (trait.GetProperty("Int2").GetInt32() == 1 && Text(trait, "EntryDuration") == "ThisTurn" ||
                 trait.GetProperty("Int2").GetInt32() == 0 &&
                 (Text(trait, "EntryDuration") == "ThisBattle" && Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "avarice" }) ||
                  Text(trait, "EntryDuration") == "ThisTurn" && Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "armor" }))) &&
                trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") && !Flag(trait, "Scaling") &&
                Text(trait, "Str").Length == 0 && trait.GetProperty("Float").GetDouble() == 0 &&
                Text(trait, "TrackedValue") == "PlayedCost";
        if (type == "CardTraitScalingAddDamage" && trait.GetProperty("Int").GetInt32() == 1 &&
            trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 && Flag(trait, "Bool") &&
            Text(trait, "Str") == "" && trait.GetProperty("Float").GetDouble() == 1 && !Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "MagicPowerInTargetRoom" && Text(trait, "EntryDuration") == "ThisTurn") return true;
        if (trait.GetProperty("Int2").GetInt32() != 0 || trait.GetProperty("Int3").GetInt32() != 0 ||
            Flag(trait, "Bool") || Text(trait, "Str").Length > 0) return false;
        int value = trait.GetProperty("Int").GetInt32();
        if (Text(trait, "EntryDuration") == "ThisTurn" && !Flag(trait, "Scaling"))
        {
            if (type is "CardTraitScalingHealTrain" or "CardTraitExhaustState" && value == 2 &&
                trait.GetProperty("Float").GetDouble() == 1 && Text(trait, "TrackedValue") == "DragonsHoardAmount") return true;
            if (type == "CardTraitScalingAdjustCapacity" && value == 1 && trait.GetProperty("Float").GetDouble() == 1 && Text(trait, "TrackedValue") == "PlayedCost") return true;
            if (type == "CardTraitExhaustState" && value == 0 && trait.GetProperty("Float").GetDouble() == 0 && Text(trait, "TrackedValue") == "CorruptionInTargetRoom") return true;
        }
        if (type == "CardTraitScalingAddDamage" && trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") &&
            Text(trait, "EntryDuration") == "ThisBattle" && (value == 3 && Text(trait, "TrackedValue") == "AnyMonsterDeath" ||
                value == 2 && Text(trait, "TrackedValue") == "TypeInDeck" && Text(trait, "QueryCardType") == "Spell")) return true;
        if (type == "CardTraitScalingAddDamage" && value == 10 && trait.GetProperty("Int2").GetInt32() == 0 &&
            trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") && Text(trait, "Str") == "" &&
            trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") && Text(trait, "TrackedValue") == "AnyMonsterDeath" &&
            Text(trait, "EntryDuration") == "ThisBattle") return true;
        if (type == "CardTraitScalingAddDamage" && value == 20 && trait.GetProperty("Int2").GetInt32() == 0 &&
            trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") && Text(trait, "Str") == "" &&
            trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") && Text(trait, "TrackedValue") == "TypeInExhaustPile" &&
            Text(trait, "EntryDuration") == "ThisBattle") return true;
        // Ritual Tome and the resource spells retain unused ExhaustState parameters.
        // Native exhaustion reads play count/relics, not those source parameters.
        if (type == "CardTraitExhaustState" && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") && Text(trait, "EntryDuration") == "ThisTurn" &&
            (value == 4 && Text(trait, "TrackedValue") is "MoonPhase" or "PlayedCost" || value == 1 && Text(trait, "TrackedValue") == "PlayedCost")) return true;
        if (type == "CardTraitScalingAddForged" && value == 4 && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisTurn") return true;
        if (type == "CardTraitScalingAddDamage" && trait.GetProperty("Float").GetDouble() == 1 && !Flag(trait, "Scaling") && Text(trait, "EntryDuration") == "ThisTurn" &&
            (value == 25 && Text(trait, "TrackedValue") == "DragonsHoardAmount" || value == 35 && Text(trait, "TrackedValue") == "ForgePoints")) return true;
        if (type == "CardTraitScalingTempCardUpgradeToCard") return value == 1 && trait.GetProperty("Float").GetDouble() == 0 && Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisTurn";
        if (type == "CardTraitScalingByConsumedTempCardUpgradeToCard") return value == 1 && trait.GetProperty("Float").GetDouble() == 0 && Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn";
        if (type is "CardTraitScalingRemoveStatusEffect" or "CardTraitExhaustState" && trait.GetProperty("Float").GetDouble() == 0 && !Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisBattle" &&
            (type == "CardTraitExhaustState" ? value is 0 or 2 : value == 1 && Strings(trait, "StatusEffectIds").SequenceEqual(new[] { "cooldown" }))) return true;
        if (type is "CardTraitScalingAddEnergy" or "CardTraitExhaustState" && value == 2 && trait.GetProperty("Float").GetDouble() == 2 && Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisBattle") return true;
        if (type == "CardTraitScalingAddDamagePerCard") return value == 10 && trait.GetProperty("Float").GetDouble() == 1 && !Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "NumSpecificCardsInDeck" && Text(trait, "EntryDuration") == "ThisTurn" &&
            Text(trait, "SpecificCardDataId") == "913694e9-bace-4480-8691-24f699ae583e";
        if (type == "CardTraitExhaustState" && value == 10 && trait.GetProperty("Float").GetDouble() == 1 && !Flag(trait, "Scaling") &&
            Text(trait, "TrackedValue") == "NumSpecificCardsInDeck" && Text(trait, "EntryDuration") == "ThisTurn") return true;
        if (type == "CardTraitScalingAddDamage" && value == 3 && trait.GetProperty("Int2").GetInt32() == 0 &&
            trait.GetProperty("Int3").GetInt32() == 0 && !Flag(trait, "Bool") && Text(trait, "Str") == "" &&
            trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") && Text(trait, "TrackedValue") == "PlayedCost" &&
            Text(trait, "EntryDuration") == "ThisTurn") return true;
        if (type == "CardTraitScalingAddDamage")
            return trait.GetProperty("Float").GetDouble() == 1 && Flag(trait, "Scaling") && Text(trait, "EntryDuration") == "ThisTurn" &&
                (Text(trait, "TrackedValue") == "PlayedCost" && value is 5 or 80 ||
                 Text(trait, "TrackedValue") == "CorruptionInTargetRoom" && value is 1 or 8 ||
                 Text(trait, "TrackedValue") == "StatusEffectCountInTargetRoom" && value is 4 or 10 && Text(trait, "StatusQueryTeam") == "Monsters" &&
                 Strings(trait, "StatusQueryStatuses").SequenceEqual(new[] { value == 4 ? "armor" : value == 10 ? "spikes" : "" }));
        if (trait.GetProperty("Float").GetDouble() != 0 || Flag(trait, "Scaling")) return false;
        if (type == "CardTraitScalingAddEffectApplications") return value == 1 && Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisTurn";
        if (type == "CardTraitScalingBuffDamage") return value == 4 && Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisTurn";
        if (cardId is "fbf7e58a-9369-49d8-ba1a-782fbcaf6498" or "0e50f7bc-c7c4-46b5-98a4-33d32194d7b8" or "37a1de5d-4e3b-428a-9b83-006f1bf4dbce" or "dea99a2f-48c9-4246-ad44-01e84f46171e" or "f954f70d-e83b-4c64-9827-89e5770a56ef" or "d930a4fe-1419-4c21-9ef2-fda4e25ee90b" or "fa394e19-4279-4e43-b0b3-75da045fc1eb" or "5b88c130-70b3-42cc-9903-4d245b4f946f" &&
            type == "CardTraitShowCardTargets")
            return value == 0 && trait.GetProperty("Int2").GetInt32() == 0 && trait.GetProperty("Int3").GetInt32() == 0 &&
                !Flag(trait, "Bool") && Text(trait, "Str") == "" && Text(trait, "TrackedValue") == "SubtypeInDeck" &&
                Text(trait, "EntryDuration") == "ThisTurn" && Text(trait, "SpecificCardDataId") == "" &&
                (cardId != "37a1de5d-4e3b-428a-9b83-006f1bf4dbce" ||
                 new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag", "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                    .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null)) &&
                Text(trait, "StatusQueryTeam") == "" && !Strings(trait, "StatusQueryStatuses").Any() && !Strings(trait, "StatusEffectIds").Any();
        if (type == "CardTraitShowCardTargets") return value == 0 && Text(trait, "TrackedValue") == "PlayedCost" && Text(trait, "EntryDuration") == "ThisTurn";
        if (type == "CardTraitIncreaseCostOfCardsInHand") return value == 1 &&
            (Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "HandCostTargetType") is "Spell" or "Monster" or "TrainRoomAttachment" or "Equipment" ||
             Text(trait, "TrackedValue") == "AnyCharacter" && Text(trait, "HandCostTargetType") == "Monster") && Text(trait, "EntryDuration") == "ThisTurn";
        if (type == "CardTraitDrawInDeploymentPhase") return value == 0 && Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn" &&
            trait.TryGetProperty("DeploymentDrawFlag", out FixtureValue drawFlag) && drawFlag.ValueKind == FixtureKind.False;
        if (type == "CardTraitTreasure") return value == 0 && Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn" &&
            new[] { "StatusQueryTeam", "StatusQueryStatuses", "HandCostTargetType", "DeploymentDrawFlag", "SpecificCardDataId", "StatusEffectIds", "MoonPhaseUpgrade" }
                .All(name => trait.GetProperty(name).ValueKind == FixtureKind.Null);
        if (type is "CardTraitUnpurgable" or "CardTraitLimitedRange" or "CardTraitPermafrost" or "CardTraitGraftedEquipment" or "CardTraitEphemeral") return value == 0 && Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn";
        if (type == "CardTraitDamageOverflow") return value == 2 && Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn";
        if (type == "CardTraitCorruptRestricted") return value is 1 or 2 or 3 or 5 && Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn";
        if (type is "CardTraitUnplayable" or "CardTraitPersistent" or "CardTraitRefine") return
            (type == "CardTraitUnplayable" ? value is 0 or 3 : value == 3) && Text(trait, "TrackedValue") == "SubtypeInDeck" && Text(trait, "EntryDuration") == "ThisTurn";
        return type is "CardTraitStrongerMagicPower" or "CardTraitIgnoreArmor" ? value is 0 or 5 : type == "CardTraitExhaustState" && value == 0;
    }

    private static IEnumerable<string> Primitives(FixtureValue rule, bool includeUnplayed = true)
    {
        yield return "pipeline:" + Text(rule, "Effect");
        foreach (string name in includeUnplayed ? new[] { "Effects", "OnCastEffects", "OnUnplayedNegativeEffects" } : new[] { "Effects", "OnCastEffects" })
        foreach (FixtureValue effect in Array(rule, name)) yield return "effect:" + Text(effect, "Type");
    }

    private static void CollectUnplayedWitnesses(FixtureValue battle, string archive, IDictionary<string, string> mechanisms, List<TurnWitness> witnesses)
    {
        int turnIndex = -1;
        foreach (FixtureValue turn in Array(battle, "Turns"))
        {
            turnIndex++;
            if (!turn.TryGetProperty("Predicted", out FixtureValue prediction) || !Flag(prediction, "Supported") ||
                !turn.TryGetProperty("Difference", out FixtureValue difference) || difference.ValueKind != FixtureKind.Null ||
                !turn.TryGetProperty("Actual", out FixtureValue actual) || actual.ValueKind != FixtureKind.Object) continue;
            FixtureValue before = turn.GetProperty("Before");
            FixtureValue context = before.GetProperty("Spawn").GetProperty("Train").GetProperty("Context");
            FixtureValue after = actual.GetProperty("Spawn").GetProperty("Train").GetProperty("Context");
            FixtureValue[] registry = Array(context, "CardRegistry"), updated = Array(after, "CardRegistry");
            if (registry.Length == 0 || updated.Length == 0) continue;
            FixtureValue[] hand = Array(context.GetProperty("Cards"), "Hand");
            FixtureValue[] rules = Array(before.GetProperty("PlayRules"), "Cards");
            foreach (FixtureValue token in hand)
            {
                string id = Text(token, "DataId"); int instanceId = token.GetProperty("InstanceId").GetInt32();
                FixtureValue rule = rules.FirstOrDefault(item => Text(item, "DataId") == id);
                if (rule.ValueKind != FixtureKind.Object || Flag(rule, "HasUnsupportedUnplayedTriggers")) continue;
                foreach (FixtureValue effect in Array(rule, "OnUnplayedNegativeEffects"))
                {
                    string type = Text(effect, "Type"), observation = "";
                    if (type == "GenerateUpgradedCopy" && hand.Count(card => Text(card, "DataId") == id) == 1)
                    {
                        FixtureValue source = registry.FirstOrDefault(card => card.GetProperty("InstanceId").GetInt32() == instanceId);
                        int copies = updated.Count(card => Text(card, "DataId") == id &&
                            !registry.Any(previous => previous.GetProperty("InstanceId").ContentEquals(card.GetProperty("InstanceId"))));
                        if (source.ValueKind == FixtureKind.Object && Flag(source, "CanBeCopied") && copies == 1)
                            observation = "one-new-copy-in-registry";
                    }
                    if (type == "UpgradeDeckSpells" && effect.TryGetProperty("Upgrade", out FixtureValue upgrade) && upgrade.ValueKind == FixtureKind.Object)
                    {
                        string upgradeId = Text(upgrade, "DataId");
                        int possibleSources = hand.Count(candidate => rules.Any(definition => Text(definition, "DataId") == Text(candidate, "DataId") &&
                            Array(definition, "OnUnplayedNegativeEffects").Any(action => Text(action, "Type") == type &&
                                action.TryGetProperty("Upgrade", out FixtureValue payload) && payload.ValueKind == FixtureKind.Object && Text(payload, "DataId") == upgradeId)));
                        if (upgradeId.Length > 0 && possibleSources == 1 && registry.Any(card =>
                        {
                            if (!rules.Any(definition => Text(definition, "DataId") == Text(card, "DataId") && Text(definition, "CardType") == "Spell")) return false;
                            FixtureValue changed = updated.FirstOrDefault(item => item.GetProperty("InstanceId").ContentEquals(card.GetProperty("InstanceId")));
                            return changed.ValueKind == FixtureKind.Object &&
                                Array(changed.GetProperty("Temporary"), "Upgrades").Count(item => Text(item, "DataId") == upgradeId) >
                                Array(card.GetProperty("Temporary"), "Upgrades").Count(item => Text(item, "DataId") == upgradeId);
                        })) observation = "temporary-upgrade-added-to-existing-spell";
                    }
                    if (observation.Length == 0) continue;
                    mechanisms.TryAdd("effect:" + type, archive + "#Turns[" + turnIndex + "]");
                    witnesses.Add(new(id, archive, turnIndex, instanceId, type, observation));
                }
            }
        }
    }

    private static void CollectGuards(FixtureValue value, string path, SortedSet<string> guards)
    {
        if (value.ValueKind == FixtureKind.Object)
        foreach (FixtureProperty property in value.EnumerateObject())
        {
            string child = path + "." + property.Name;
            if (property.Name.EndsWith("Interactions", StringComparison.Ordinal) && property.Value.ValueKind == FixtureKind.Array)
                foreach (FixtureValue guard in property.Value.EnumerateArray()) guards.Add(child + ":" + guard.GetString());
            else if (property.Name == "Type" && property.Value.ValueKind == FixtureKind.String && property.Value.GetString() == "Unsupported") guards.Add(child + ":unsupported-effect");
            else if (property.Name == "HasUnsupportedUnplayedTriggers" && property.Value.ValueKind == FixtureKind.True) guards.Add(child);
            else CollectGuards(property.Value, child, guards);
        }
        else if (value.ValueKind == FixtureKind.Array)
        {
            int index = 0;
            foreach (FixtureValue element in value.EnumerateArray()) CollectGuards(element, path + "[" + index++ + "]", guards);
        }
    }

    // Only these root fields describe decision-context eligibility/order rather than
    // card definitions. Missing null properties are equivalent across capture schemas;
    // only two omitted SpawnUnit.Modifiers defaults and the ordinary Funguy
    // fallback's context-conditional bump defaults have additional calibration.
    private static bool Equivalent(FixtureValue left, FixtureValue right, bool ruleRoot = false,
        bool legacyMask = false, bool cardRule = false, string path = "$")
    {
        if (left.ValueKind != right.ValueKind) return false;
        if (left.ValueKind == FixtureKind.Object)
        {
            bool Include(FixtureProperty property) => property.Value.ValueKind != FixtureKind.Null &&
                !(legacyMask && property.Name == "FromPermanentUpgrade" && property.Value.ValueKind == FixtureKind.False) &&
                !(cardRule && path == "$" && property.Name == "HasKillTriggers" && property.Value.ValueKind == FixtureKind.False) &&
                !(ruleRoot && property.Name is "ForgeEligible" or "UnplayedNegativeOrder");
            var a = left.EnumerateObject().Where(Include).ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal);
            var b = right.EnumerateObject().Where(Include).ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal);
            if (cardRule && path == "$.SpawnUnit.Modifiers")
            {
                if (a.TryGetValue("IsChampion", out FixtureValue isChampion) && isChampion.ValueKind == FixtureKind.False &&
                    !b.ContainsKey("IsChampion"))
                    a.Remove("IsChampion");
                if (a.TryGetValue("StatusAttackModifiers", out FixtureValue statusAttackModifiers) &&
                    statusAttackModifiers.ValueKind == FixtureKind.Array && statusAttackModifiers.GetArrayLength() == 0 &&
                    !b.ContainsKey("StatusAttackModifiers"))
                    a.Remove("StatusAttackModifiers");
            }
            if (cardRule && path.Contains(".MaskDescriptor.KillTriggers[", StringComparison.Ordinal) &&
                path.EndsWith(".SpawnFallbackUnit", StringComparison.Ordinal) && Text(left, "AssetKey") == "MonsterFunguy" &&
                !a.ContainsKey("BumpRules") && b.TryGetValue("BumpRules", out FixtureValue bump) &&
                bump.ValueKind == FixtureKind.Object && bump.EnumerateObject().Count() == 5 &&
                new[] { "Loops", "CompanionBoss", "OuterBoss", "OuterBossRelentlessPhase" }
                    .All(name => bump.TryGetProperty(name, out FixtureValue flag) && flag.ValueKind == FixtureKind.False) &&
                bump.TryGetProperty("PairedBossUnitId", out FixtureValue paired) && paired.ValueKind == FixtureKind.Number && paired.GetInt32() == 0)
            {
                // Lunar Echo's native creation pool captures this unchanged fallback
                // with explicit ordinary-unit defaults only when bump capture is enabled.
                b.Remove("BumpRules");
            }
            if (cardRule && path.Contains("$.SpawnUnit.Triggers[", StringComparison.Ordinal) &&
                path.Contains(".Effects[", StringComparison.Ordinal) && a.TryGetValue("Type", out FixtureValue effectType) &&
                effectType.GetString() == "CardEffectSpawnMonsterOrAddStatusEffect" && !a.ContainsKey("Action") &&
                b.TryGetValue("Action", out FixtureValue spawnOrStatus) && Text(spawnOrStatus, "Type") == "SpawnOrStatus")
            {
                // Older frozen catalogs record this effect type before its structured action projection existed.
                // The effect identity remains exact; native scenario checks validate the richer action payload.
                b.Remove("Action");
            }
            return a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out FixtureValue other) &&
                Equivalent(pair.Value, other, legacyMask: legacyMask, cardRule: cardRule, path: path + "." + pair.Key));
        }
        if (left.ValueKind == FixtureKind.Array)
        {
            if (left.GetArrayLength() != right.GetArrayLength()) return false;
            FixtureValue[] a = left.EnumerateArray().ToArray(), b = right.EnumerateArray().ToArray();
            for (int index = 0; index < a.Length; index++)
                if (!Equivalent(a[index], b[index], legacyMask: legacyMask, cardRule: cardRule, path: path + "[" + index + "]")) return false;
            return true;
        }
        if (left.ValueKind == FixtureKind.Number) return left.GetDouble() == right.GetDouble();
        return left.ContentEquals(right);
    }

    private static void CollectLegalityWitnesses(FixtureValue battle, string archive, Dictionary<string, Card> byId, List<LegalityWitness> witnesses)
    {
        if (!Flag(battle, "OriginalUnplayableCardsPrepared") || !Flag(battle, "OriginalUnplayableCardsQueriesUnchanged") ||
            !Flag(battle, "NativeWon") || !Flag(battle, "TerminalEffectsSettled")) return;
        int index = -1;
        foreach (FixtureValue query in Array(battle, "OriginalUnplayableCardsQueries"))
        {
            index++;
            if (!query.TryGetProperty("CanPlay", out FixtureValue canPlay) || canPlay.ValueKind != FixtureKind.False ||
                !query.TryGetProperty("FromHand", out FixtureValue fromHand) || fromHand.ValueKind != FixtureKind.False ||
                !query.TryGetProperty("FromPlay", out FixtureValue fromPlay) || fromPlay.ValueKind != FixtureKind.False ||
                new[] { "Error", "HandError", "PlayError" }.Any(key => Text(query, key) != "Unplayable")) continue;
            string id = Text(query, "DataId");
            if (!byId.TryGetValue(id, out Card? card) || !Array(card.Definition, "Traits").Any(trait => Text(trait, "Type") == "CardTraitUnplayable") ||
                !query.TryGetProperty("SourceTraits", out FixtureValue traits) || !Equivalent(traits, card.Definition.GetProperty("Traits"))) continue;
            FixtureValue before = query.GetProperty("Before"), action = query.GetProperty("Action"), context = before.GetProperty("Spawn").GetProperty("Train").GetProperty("Context");
            int cardId = action.GetProperty("CardInstanceId").GetInt32();
            FixtureValue rule = Array(before.GetProperty("PlayRules"), "Cards").FirstOrDefault(value => Text(value, "DataId") == id);
            if (FindCardId(context, cardId) != id || rule.ValueKind != FixtureKind.Object || !Equivalent(card.Definition.GetProperty("Rule"), rule, ruleRoot: true) ||
                !MatchesSourceMetadata(card.Definition, context, cardId)) continue;
            // This table indexes whole-card native rejection queries. Trait-only range
            // queries cannot establish full-card eligibility and are deliberately separate.
            witnesses.Add(new(id, archive, index, cardId, action.GetProperty("RoomIndex").GetInt32(), "Unplayable", Flag(query, "CanDiscard")));
        }
    }

    private static string Report(Card[] cards, int indexed, SortedDictionary<string, string> excluded, int actions, int nonCatalog, int unresolved, List<LegalityWitness> legality)
    {
        var text = new StringBuilder();
        text.Append("# Original card coverage inventory\n\n");
        text.Append("Generated by `scripts/Get-CardCoverageInventory.ps1`; inputs are the frozen native catalog and manifest-verified battle archives. This indexes existing evidence and does not replay battles or run regression.\n\n");
        text.Append("The catalog/model projection is from 2026-10-21 (native callbacks and reachable generation branches). See [catalog provenance](../catalogs/README.md). Keep the snapshot date in mind after model changes; regenerate the native catalog when projections change.\n\n");
        text.Append("## Scope\n\n| Native role | Definitions |\n| --- | ---: |\n");
        foreach (var group in cards.GroupBy(card => card.Role).OrderBy(group => group.Key, StringComparer.Ordinal)) text.Append($"| {group.Key} | {group.Count()} |\n");
        text.Append($"| **All definitions** | **{cards.Length}** |\n\n");
        text.Append($"The 563 native logbook entries are the primary card-inventory denominator, not the 668 internal definitions. Logbook visibility does not imply collectible/reward-pool membership; generated, event and non-mastery cards are included. Native mastery eligibility is a separate recorded predicate ({cards.Count(card => Flag(card.Definition, "CountsTowardsMastery"))} definitions). Abilities and other hidden definitions stay in the inventory.\n\n");
        Card[] intrinsicallyUnplayable = cards.Where(card => card.Logbook &&
            (Text(card.Definition.GetProperty("Rule"), "CostType") == "NonPlayable" ||
                Array(card.Definition, "Traits").Any(trait => Text(trait, "Type") == "CardTraitUnplayable"))).ToArray();
        Card[] eligible = cards.Where(card => card.Logbook && !intrinsicallyUnplayable.Contains(card)).ToArray();
        text.Append($"## Direct-play eligibility\n\n{eligible.Count(card => card.Coverage == "direct-native-play")}/{eligible.Length} logbook entries without intrinsic unplayability have a definition-matched successful native play. This is a direct-play evidence gate, not proof of every branch or combination. The following {intrinsicallyUnplayable.Length} original entries retain their native restriction and are not promoted to successful-play evidence:\n\n| Asset key | Native restriction |\n| --- | --- |\n");
        foreach (Card card in intrinsicallyUnplayable.OrderBy(card => card.Name, StringComparer.Ordinal))
            text.Append($"| {card.Name} | {(Text(card.Definition.GetProperty("Rule"), "CostType") == "NonPlayable" ? "NonPlayable cost type" : "CardTraitUnplayable")} |\n");
        text.Append("\n## Evidence levels\n\n| Classification | Logbook | All definitions | Meaning |\n| --- | ---: | ---: | --- |\n");
        foreach ((string level, string meaning) in new[] {
            ("direct-native-play", "No captured projection guards; at least one successful native play has the same captured definition projection."),
            ("mechanism-only", "No captured projection guards; its pipeline/effect types have indexed native play or positive unplayed-trigger witnesses. The original card itself is not directly accepted."),
            ("partial", "Projection contains explicit play, upgrade, discard, consume or nested interaction guards. See each row."),
            ("unsupported", "The current definition/activation pipeline is explicitly unimplemented."),
            ("unreviewed", "Projection failed or a required primitive has no indexed transition witness; support is unknown.") })
            text.Append($"| {level} | {cards.Count(card => card.Logbook && card.Coverage == level)} | {cards.Count(card => card.Coverage == level)} | {meaning} |\n");
        text.Append("\nThese are evidence labels, not whole-card completion claims. A directly matched play proves that captured path; it does not prove every target, upgrade, relic combination, trigger or future branch. Indirect witnesses may execute an effect with no target; presence in a played chain is not a positive test of every parameter. The definition projection is conservative but is not a full raw-asset checksum.\n\n");
        text.Append("Definition matching compares all captured rule fields recursively, including source-unit statistics, statuses, triggers, ability metadata, effect parameters and nested creation rules. Only root `ForgeEligible` and `UnplayedNegativeOrder` are excluded because they depend on the decision context. Missing null fields are tolerated for older schemas; a missing false `SpawnUnit.Modifiers.IsChampion` and an empty `SpawnUnit.Modifiers.StatusAttackModifiers` are accepted only at those exact paths because the native action snapshot omits those defaults; other missing non-null fields are not, except the ordinary MonsterFunguy kill-trigger fallback's context-conditional bump defaults described below. Direct evidence also requires captured instance base-trait metadata. This matcher accepts trait-free definitions, SelfPurge, calibrated IgnoreArmor/ExhaustState/StrongerMagicPower parameters, and PlayedCost damage scalars five/eighty with exact source parameters and runtime/declared identity. Consume witnesses distinguish played exhaustion from unplayed discard/redraw. StrongerMagicPower covers raw Int zero/five and zero/five room power for damage/healing. The X-damage witnesses cover enemy-present zero and positive X. Imp Olate has exact source calibration and native zero/one-Imp `SubtypeInDeck` witnesses for zero/twenty-five damage. Pyreshot has an original-card play witness for its selected-floor Pyre attack, including Sweep damage and the live per-floor enemy target set. Retch has an exact original-card `RandomToHand` witness from a controlled starting Eaten pile; it does not cover how cards enter Eaten through native feeding. Astrologer has an original 1-cost 5/10 size-2 play and three native +5 Attack `OnMoonPhaseShift` callbacks followed by a no-shift control. Its isolated fixture suppresses the broad player-wide Moon-phase-card interaction guard; other card/relic Moon-phase interactions remain outside this card witness. Snotty and Bloated Whelp have original 1-cost 4/1 Whelp plays whose `OnSpawn` adds the ability-available marker and resets cooldown; their distinct cooldown-two and cooldown-one abilities remain unused in their respective captured turns. Fae Meddler's original 1-cost 6/3 play natively advances phase 2 to 1 on spawn; its cooldown-two unit ability remains unused. This does not imply broad player-wide Moon-phase card/relic coverage.\n\n");
        text.Append("The final hidden-definition batch adds unchanged original witnesses for all fifteen previously guarded definitions. Four Infusions calibrate exact zero-parameter Infusion/Persistent traits, played DiscardBuffer retention, suppressed SelfPurge and independent unplayed retention controls. Bog Fly calibrates source-floor Corrupt one with Bool true and actor-only teleport to a zero-corruption destination; Glugsider calibrates selected-floor Corrupt four with Bool false. Raw trait metadata remains mandatory. Extract Draw, Fetch, Savior levels two/three, Courage, Pyre Time Freeze, both Chomp feeding routes, Lunar Echo and both original spike-crafting choices have focused native checks and bounded search witnesses. The only additional missing-default normalization is MonsterFunguy inside a creation mask's kill-trigger SpawnFallbackUnit: four explicit false bump flags and a zero paired-boss ID match the catalog's omitted optional field. Nondefault bump metadata and all other fields still require exact matching. This keeps Lunar Echo's full original creation pool comparable across capture modes without ignoring nested definition changes.\n\n");
        text.Append("Molten Encasement has an original 1-cost 0/1 Tomb play and an exact captured OnDeath room +2 Stealth definition, but the unit survived all four post-deployment snapshots, so death-trigger execution is not covered. Shattered Shell has an original 1-cost 3/10 size-2 play and two positive native OnKill self +3 Attack upgrades; its four post-deployment snapshots show 6/9/9/9 Attack and no further upgrade. Weldling has an original 2-cost 6/3 Machine play whose PostCombat definition adds Armor to the friendly FrontInRoom target; two native transitions add three Armor to the original front Steward, with an unchanged control between gains. The final native cleanup is preserved without attributing its Armor removal to the Weldling callback. Hostguard has an original 3-cost 25/60 size-3 play whose OnSpawn adds the ability-available marker and resets its cooldown-three unit ability; that ability remains unused in all four post-deployment snapshots. Rubble Raiser has an original zero-cost 0/1 Snack play and an exact OnEaten +1 energy plus temporary LastFeederCharacter +3/+3 upgrade definition; no feeder was recorded in the direct birth witness, so the callback is not positively covered.\n\n");
        text.Append("PlayedCost status scaling is calibrated only at multiplier two/filter one with exact original source parameters. Spike of the Legion covers positive/zero X, propagatable Rage/Regeneration, unchanged armor, neighbor/floor controls and both Consume paths.\n\n");
        text.Append("CorruptionInTargetRoom damage scaling is calibrated at multiplier one, using original Echo Break at corruption zero/two/six. Original Unearthed Remains and Soul Siphon supply accumulation/fill-to-max; neighbor and selected-floor controls distinguish live selected-room state from cached/global values. Soul Siphon's negative amount with add mode fills the current maximum and also applies room-only Scorch. This witness targets friendly units; enemy-target paths need separate evidence.\n\n");
        text.Append("StatusEffectCountInTargetRoom damage scaling is calibrated only for native Monsters armor/spikes filters with scalars four/ten. Battering Ram and Bramble Lash cover zero and positive two-unit sums, enemy and outside-floor controls. These queries require current selected-room units and captured status IDs; old missing filters remain unsupported. Source descriptors retain the query team and ordered status mask separately from runtime traits.\n\n");
        text.Append("The default Unpurgable marker has native battle witnesses for fixed-cost play/discard, positive SelfPurge, and played/unplayed Pyre-damage controls. It has no native battle callback; campaign purge selection is outside these witnesses. Exact default source parameters and captured provenance are required. Other traits paired with this marker retain their own guards.\n\n");
        text.Append("Original Titan Bane hand costs have two-owner native stacking/removal witnesses for spells, monsters, room attachments and equipment, with Blight/Junk exclusions. Captured source and runtime type filters are required. Native query controls verify the hand-only scope, upper clamp before numeric reductions and restoration after each owner leaves. CurrentCost statistic queries with hand cost traits and other paired traits keep their guards.\n\n");
        text.Append("Limited Range has restored native hand/play queries from Ice and Pyre, Siren Song and Trap Chute: only the Pyre room and the room immediately below it pass. Original Ice and Pyre has a paid top-floor damage play and an empty-Pyre paid cast, with lower-floor rejection. This does not remove the separate room effect/ability guards on Siren Song or Trap Chute. Exact default source parameters and provenance are required.\n\n");
        text.Append("Original Titan Corruption has Monster-only hand cost stacking/removal and Spell/room/Blight exclusion witnesses. Its unchanged deployment marker passes native eligibility checks, with non-deployable controls and restored state/RNG/frame. The captured definition flag is false while the runtime class still grants eligibility. These are battle-play and eligibility witnesses; initial deployment draw ordering and upgraded eligibility are outside this fixture.\n\n");
        text.Append("Default Permafrost has original Preserve, Unnamed Tome and Ice Tornado witnesses for automatic Freeze, retained hand, thaw on play, natural redraw/re-freeze and repeated idempotent callbacks with unchanged decision/RNG/frame. Preserve positively freezes Junk, Tome applies enemy Silence and Consumes, and Ice Tornado has two positive random-damage plays across natural redraw. Exact default source parameters and runtime provenance are required; older missing metadata is not inferred.\n\n");
        text.Append("Original Pyreball and Pyreclasm calibrate DamageOverflow source Int two (native Hero team). A nonlethal friendly-primary control leaves enemies unchanged; five-health friendly primary deaths spill fifteen/145 to every enemy in the selected room, preserving friendly siblings and the outside floor. Armor is applied separately to each enemy. Exact default source parameters and runtime provenance are required. Fanning the Flame retains its separate OnKill trigger guard.\n\n");
        text.Append("Original Echo Infusion, Echo Transfer and Proclamation calibrate positive Corrupt Restricted requirements one/two on direct hand spells. Twelve restored native trait queries cover threshold boundaries with a different empty source floor. Three pre-effect observations match positive subtraction before health, attack and eighty damage; Soul Siphon fills/refills the selected room. Exact unchanged base source parameters and runtime provenance are required. Source-room/unit abilities, Corrupt State, upgrade-added/replaced requirements and positive-requirement relic combinations retain guards. Trait-only queries do not enter the full-card legality index.\n\n");
        text.Append("Original Wracking Pain calibrates three damage per visible target debuff stack. A positive-status-only target takes zero; a Sap/Frostbite target takes 27, then 33 after adding two Sap stacks. Three unchanged native callbacks match before armor; the outside floor is unchanged. Exact source parameters and live status visibility/category metadata are required. Attacker/list/buff modes, mixed scaling order and added/replaced traits remain guarded.\n\n");
        text.Append("Original Dante's Inferno calibrates ten damage per permanent Dante's Candle, overriding incoming zero/99 damage and adding a source +5 upgrade. Twelve native queries restore permanent deck, decision, deterministic RNG and frame. Two casts deal 30/35 to every enemy through armor and consume both cards. Source descriptors require the specific CardData identity, plus the original paired IgnoreArmor Int five and Consume's unused Int ten/Float one/query parameters. Attacker attack mutation, mixed scaling and added/replaced traits remain guarded.\n\n");
        text.Append("Original GainXEnergy calibrates played-discard ember scaling from the still-live paid-cost statistic: X three returns six current ember; X zero returns zero. An unplayed original repeatedly discards/redraws without gaining ember. Seven callbacks independently match complete context, leaving next-turn/every-turn income unchanged. Exact base source parameters, paired Consume metadata and live paid cost are required; next-turn routes and added/replaced traits retain guards.\n\n");
        text.Append($"Unplayable source values zero/three and Persistent/Refine value three have original native witnesses. The separate legality index records {legality.Count} restored full-card rejection queries across {legality.Select(row => row.DataId).Distinct().Count()} definitions ({legality.Select(row => row.DataId).Distinct().Count(id => cards.Any(card => card.Id == id && card.Logbook))} logbook), with exact rule/source metadata. These are native rejection observations, not successful card plays; the dedicated checker independently verifies Illegal results and immutable parents. Ordinary unplayable cards discard/redraw, Persistent Scrap Metal stays in hand, and original Starter Scrap smelts it for Refine 3. Explicit discard and upgrade-added Refine remain guarded. These observations do not raise direct-play counts.\n\n");
        text.Append("New descriptors retain the actual CardData supplied to CardState.Setup, with source trait parameters separate from runtime traits. All captured raw source fields must match the frozen catalog when present; X damage/status scaling requires this provenance and older missing metadata remains unverified. Missing provenance, changed multiplier/query/filter, and changed runtime parameters are rejected. Other trait families need separate calibration. A shared DataId alone never establishes direct coverage. Instance upgrades do not change the original definition, but authored definition changes do. Other recorded plays can be authored variants or older/incomplete projections; they are not automatically original-card evidence.\n\n");
        text.Append("Original Dig Deep calibrates visible permanent-deck Horde scaling with exact per-ten/List source parameters. Twenty-four restored threshold/status/incoming queries, first birth at four stacks and extension to eight match native. Card-sourced birth preserves added-status statistics; missing metadata, changed/added/replaced traits and relic stack-modifier combinations retain guards.\n\n");
        text.Append("Original Prism Retrieval calibrates X-scaled next-draw upgrades at three/zero/ten, typed Monster draws with a skipped Spell, shared queue mutation and clearing, clamped recipient costs and Consume. Exact unchanged source parameters, original upgrade payload and the display-only ShowCardTargets marker are retained; missing metadata, changed/added/replaced traits and multiple/filtered queued upgrades retain guards.\n\n");
        text.Append("Original Sacrificial Ressurection calibrates its private consumed-hand counter at three then zero on the same source, with fresh original damage-ten upgrades producing thirty then zero on generated Draff. Restored native controls cover zero/one/three/seven and null upgrading source. Source metadata, original upgrade payload and independent effect counters are required; altered status/other-stat payloads and different upgrading sources remain guarded.\n\n");
        text.Append("Original Lunacy calibrates X-scaled moon advances before Conduit addition at three/zero/ten. Complete phase-effect train contexts, matched/unmatched status queries, phase parity, floor controls and ordinary discard match. Exact Float-one/PlayedCost source parameters and the ordered Conduit List filter are required; added/replaced traits, changed multipliers and uncalibrated moon card-upgrade/relic callbacks retain guards.\n\n");
        text.Append("Original Awoken's Rail Spike calibrates played-discard draw at X three/zero/ten, original minus-one cost/plus-one X recipient upgrades, Consume and queue hiding/restoration/clearing. Ordinary and typed deck draws are blocked afterward, even at zero X; unplayed discard is a no-op and the next player turn resets blocking. Complete native discard contexts and sixteen immutable chains match. Exact source parameters and branch-owned draw-block state are required; the isolated thirty-card hand limit does not establish normal caps, reshuffles, OnCast or active relic combinations.\n\n");
        text.Append("Original Celestial Storm, Gravity, Heavenfall, Silver Strike and Starstruck add ten plays at New/Full. Seven original upgrade payloads, native private cache/active state, six complete hand callbacks, positive magic-power refresh, physical descend/ascend, OnCast effects and played/unplayed reset/redraw match. The detached component separately compares 135 complete card states with existing-trait and preview controls. Active Celestial Storm's installed Stronger Magic Power base trait is accepted only with its original source metadata, active cached state and the matching permanent upgrade's recorded trait modification. Room overrides, living-unit traits, recast copies and active relic combinations need separate witnesses.\n\n");
        text.Append("Original Symphony of the Soul calibrates Corrupt five and Positive-category multiplication for friendly/enemy/empty-positive targets. Rage/Regeneration double; Persistent armor is excluded from the first stage and doubled only by the friendly second effect. Native full effect contexts, pre-effect corruption payment, negative/floor controls and chance RNG even with zero armor match. Horde-positive and relic-modified combinations need separate witnesses. This adds evidence without changing the frozen definition projections.\n\n");
        text.Append("Original Bloody Moon and Antumbra Assault each add seven unchanged plays with deployment, exact kills, Undying revival, nonlethal and shield controls. Antumbra preserves the original 5/5/4/4/1 pool, two RNG draws when only one hand slot is free, and all pool definitions before selection. All five original Morsels have ordinary summon and eating witnesses, including Jeweler health/shield, Void attack/lifesteal, source-card Eaten routing and exact food statistics. Last-feeder references are captured consistently in food templates and live units. Added traits, relic combinations and other kill-trigger routes need separate witnesses.\n\n");
        text.Append("Original Surgical Strike adds six unchanged plays and one generated play. Native suppression requires both Monsters team and FunGuy subtype; ordinary friendly and enemy kills still copy. Piercing bypasses armor/shield, nonlethal hits do not copy, and a revived FunGuy remains suppressed. Three deterministic source-card copies preserve the original temporary Ephemeral upgrade, upgrade inheritance, allocation/RNG and played/unplayed purge. Source team/subtype metadata is required; bonus fire counts, other subtypes, starting upgrades, all-subtype relics and character-attributed kills remain guarded.\n\n");
        text.Append($"Across all classifications, {cards.Count(card => card.DirectPlays > 0)} identities ({cards.Count(card => card.Logbook && card.DirectPlays > 0)} logbook) have at least one definition-matched play. Partial cards retain their matched-play evidence without losing their guard classification.\n\n");
        text.Append("## Reproducible files\n\n- [Per-card inventory](card-inventory.tsv): identity, type/role, native effects/traits, evidence level, direct/other plays, primitive witnesses and exact projection guard paths.\n- [Played-card evidence](card-play-evidence.tsv): one row per original identity/archive, with zero-based indices into that archive's `Actions` array.\n- [Unplayed-trigger evidence](card-trigger-evidence.tsv): matched EndTurns with a positive new-copy or existing-spell-upgrade observation, source identity and zero-based `Turns` index. These supply mechanism evidence and do not count as direct card plays. Playing a card alone does not establish its unplayed trigger.\n- [Native legality evidence](card-legality-evidence.tsv): definition-matched full-card rejection queries, with zero-based `OriginalUnplayableCardsQueries` indices, floor and native discardability. Trait-only range queries are excluded; rejected attempts do not count as direct plays.\n- [Fixture manifest](manifest.tsv): archive byte counts and SHA-256; every indexed full battle is checked against it.\n\n");
        text.Append($"Indexed {indexed} full battles and {actions} native actions whose captured prediction is supported, actual state exists and difference is null. {nonCatalog} actions use scenario-only identities; {unresolved} actions lack a resolvable card/rule and are not counted as card evidence. Excluded captures with missing/nonzero original counters: {excluded.Count}. Accepted component fixtures are used for catalog-fact validation, not counted as native card plays. No current simulation checks are rerun by this indexer.\n\n");
        text.Append("## Largest recorded guard groups\n\nCounts are per logbook identity and can overlap. Conditional upgrade/discard guards do not imply every normal play fails.\n\n| Guard | Logbook identities |\n| --- | ---: |\n");
        foreach (var group in cards.Where(card => card.Logbook).SelectMany(card => card.Gaps.Where(gap => !gap.StartsWith("no-indexed-transition-witness:", StringComparison.Ordinal)).Select(gap => (card.Id, Gap: gap.Contains(':') ? gap[(gap.IndexOf(':') + 1)..] : gap)))
            .Distinct().GroupBy(pair => pair.Gap).OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal).Take(15))
            text.Append($"| {Escape(group.Key)} | {group.Count()} |\n");
        text.Append("\n## Explicitly unsupported logbook entries\n\n| Asset key | Main effects |\n| --- | --- |\n");
        foreach (Card card in cards.Where(card => card.Logbook && card.Coverage == "unsupported"))
            text.Append($"| `{Escape(card.Name)}` | {Escape(Join(Array(card.Definition, "Effects").Select(effect => Text(effect, "Type"))))} |\n");
        Card[] internalGaps = cards.Where(card => !card.Logbook && card.Coverage is "partial" or "unsupported").ToArray();
        text.Append($"\n## Internal definitions with remaining guards\n\n{internalGaps.Length} unit abilities or other hidden definitions remain partial/unsupported. They stay in the 668-definition inventory and are outside the logbook direct-play gate above. Grouped native signatures identify shared implementation work; exact guards remain in `card-inventory.tsv`.\n\n| Native effect/trait signature | Definitions | Evidence level |\n| --- | --- | --- |\n");
        foreach (var group in internalGaps.GroupBy(card => string.Join(" -> ", Array(card.Definition, "Effects").Select(effect => Text(effect, "Type"))) +
            (Array(card.Definition, "Traits").Any(trait => Text(trait, "Type") == "CardTraitInfusion") ? " + CardTraitInfusion" : ""))
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal))
            text.Append($"| {Escape(group.Key)} | {Escape(Join(group.Select(card => card.Name)))} | {Join(group.Select(card => card.Coverage))} |\n");
        if (excluded.Count > 0)
        {
            text.Append("\n## Excluded capture records\n\nThese may have separate later-model acceptance, but the indexer does not reinterpret their captured failure counters.\n\n");
            foreach ((string archive, string reason) in excluded) text.Append($"- `{archive}`: {reason}.\n");
        }
        text.Append("\n## Refresh\n\n```powershell\npwsh -NoProfile -File scripts/Get-CardCoverageInventory.ps1\npwsh -NoProfile -File scripts/Get-CardCoverageInventory.ps1 -Check -SkipBuild\n```\n\n`-Check` only verifies catalog facts, manifest hashes and deterministic inventory output. It does not run the full model regression. New original-card acceptance should add a native witness with positive/control paths and independent replay before increasing evidence claims.\n");
        return text.ToString();
    }

    private static FixtureValue[] Array(FixtureValue value, string name) => value.ValueKind == FixtureKind.Object && value.TryGetProperty(name, out FixtureValue items) && items.ValueKind == FixtureKind.Array ? items.EnumerateArray().ToArray() : [];
    private static IEnumerable<string> Strings(FixtureValue value, string name) => Array(value, name).Select(item => item.GetString()!);
    private static bool Flag(FixtureValue value, string name) => value.ValueKind == FixtureKind.Object && value.TryGetProperty(name, out FixtureValue flag) && flag.ValueKind == FixtureKind.True;
    private static string Text(FixtureValue value, string name) => value.ValueKind == FixtureKind.Object && value.TryGetProperty(name, out FixtureValue text) && text.ValueKind == FixtureKind.String ? text.GetString()! : "";
    private static string Join(IEnumerable<string> values) => string.Join(";", values.Distinct().Order(StringComparer.Ordinal));
    private static string Hash(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant(); }
    private static void Row(StringBuilder output, params string[] cells) => output.Append(string.Join('\t', cells.Select(cell => "\"" + cell.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\""))).Append('\n');
    private static string Escape(string value) => value.Replace("|", "\\|").Replace("\n", " ").Replace("\r", " ");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
