using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using MonsterTrain2Poju.Fixtures;

// A source-code audit denominator, not a replacement for native runtime evidence.
internal static class MechanismInventory
{
    private const string Version = "2.3.1";
    private const string Module = "bf93f060-beda-46b1-b808-95ebf1e9a99d";
    private static readonly Dictionary<string, string> Bases = new(StringComparer.Ordinal)
    {
        ["CardEffectBase"] = "card-effect", ["CardTraitState"] = "card-trait",
        ["RelicEffectBase"] = "relic-effect", ["RoomStateModifierBase"] = "room-modifier",
        ["StatusEffectState"] = "status-effect"
    };

    private sealed class Mechanism(string family, string name, string hooks)
    {
        internal readonly string Family = family, Name = name, Hooks = hooks;
        internal readonly HashSet<string> Owners = new(StringComparer.Ordinal);
        internal readonly HashSet<string> Configurations = new(StringComparer.Ordinal);
        internal readonly SortedSet<string> Locations = new(StringComparer.Ordinal);
        internal readonly Dictionary<ReferenceKind, HashSet<string>> References = new();
        internal readonly HashSet<string> DefinitionConfigurations = new(StringComparer.Ordinal);
        internal readonly SortedSet<string> DefinitionLocations = new(StringComparer.Ordinal);
        internal readonly SortedSet<string> ReferenceLocations = new(StringComparer.Ordinal);
        internal int Count(ReferenceKind kind) => References.TryGetValue(kind, out var owners) ? owners.Count : 0;
        internal Evidence? Evidence;
    }

    // A captured class name is not necessarily a configured dispatch target.
    // Keep selectors, generic/default parameters and projected descriptors visible.
    private enum ReferenceKind { Definition, Selector, Parameter, Descriptor, Other }
    private static ReferenceKind Classify(string field, bool descriptor) => descriptor ? ReferenceKind.Descriptor : field switch
    {
        "Type" or "ClassName" or "RuntimeType" or "DeclaredName" or "AddedTraits" => ReferenceKind.Definition,
        "requiredCardEffects" or "excludedCardEffects" or "requiredCardTraits" or "excludedCardTraits" or
            "RequiredTraits" or "RemovedTraits" => ReferenceKind.Selector,
        "SourceTrait" or "TargetTrait" => ReferenceKind.Parameter,
        "MainEffectNames" or "Effects" => ReferenceKind.Descriptor,
        _ => ReferenceKind.Other
    };

    private sealed record Evidence(string Level, string Fixture, string CheckScript, string Boundary);

    internal static void Run(string workspace, string assemblyPath, bool check)
    {
        workspace = Path.GetFullPath(workspace);
        assemblyPath = Path.GetFullPath(assemblyPath);
        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();
        if (metadata.GetGuid(metadata.GetModuleDefinition().Mvid).ToString() != Module)
            throw new InvalidDataException("The native assembly does not match the pinned 2.3.1 catalogs.");
        var definitions = metadata.TypeDefinitions.Where(handle => !metadata.GetTypeDefinition(handle).IsNested)
            .ToDictionary(handle => Name(handle), handle => metadata.GetTypeDefinition(handle), StringComparer.Ordinal);
        var mechanisms = new SortedDictionary<string, Mechanism>(StringComparer.Ordinal);
        foreach (var pair in definitions)
        {
            if ((pair.Value.Attributes & (TypeAttributes.Abstract | TypeAttributes.Interface)) != 0 || pair.Value.IsNested) continue;
            string? family = Family(pair.Key, new HashSet<string>(StringComparer.Ordinal));
            if (family == null || Bases.ContainsKey(pair.Key)) continue;
            string hooks = string.Join(",", pair.Value.GetMethods().Select(metadata.GetMethodDefinition)
                .Where(method => (method.Attributes & MethodAttributes.Virtual) != 0)
                .Select(method => metadata.GetString(method.Name)).Distinct().Order(StringComparer.Ordinal));
            mechanisms.Add(pair.Key, new Mechanism(family, pair.Key, hooks));
        }
        foreach (string name in Bases.Keys)
            if (!definitions.ContainsKey(name)) throw new InvalidDataException("Missing native mechanism base " + name);

        string[] sources =
        {
            "tests/compatibility/game-2.3.1/projection-audits/punished-champions/catalog.mt2f",
            "tests/compatibility/game-2.3.1/2.3.1/relic-catalog.mt2f",
            "tests/compatibility/game-2.3.1/2.3.1/rule-catalog.mt2f"
        };
        var hashes = new List<string>();
        int cards = 0, relics = 0, characters = 0, statuses = 0;
        for (int source = 0; source < sources.Length; source++)
        {
            string path = Path.Combine(workspace, sources[source]);
            hashes.Add(Hash(File.ReadAllBytes(path)));
            using var document = FixtureDocument.Read(path);
            FixtureValue root = document.RootElement;
            if (Text(root, "GameModuleMvid") != Module || source < 2 && Text(root, "GameVersion") != Version)
                throw new InvalidDataException("Catalog version mismatch: " + sources[source]);
            if (source == 0)
            {
                foreach (FixtureValue card in root.GetProperty("Cards").EnumerateArray())
                { cards++; Visit(card, "card:" + Text(card, "Id"), "card"); }
                foreach (string section in new[] { "MoonPhaseUpgradeDefinitions", "CharacterRoomModifierDefinitions", "EquipmentRoomModifierDefinitions", "CardTriggerEffectDefinitions", "CardKillTriggerDefinitions", "PunishedChampionUpgradeDefinitions" })
                    if (root.TryGetProperty(section, out FixtureValue rows))
                        foreach (FixtureValue row in rows.EnumerateArray())
                            Visit(row, "card:" + First(row, "CardDataId", "Id", "Card"), section);
                if (cards != root.GetProperty("NativeDefinitionCount").GetInt32())
                    throw new InvalidDataException("The native card denominator is inconsistent.");
            }
            else if (source == 1)
                foreach (FixtureValue relic in root.GetProperty("Relics").EnumerateArray())
                { relics++; Visit(relic, "relic:" + Text(relic, "Collection") + ":" + Text(relic, "Id"), "relic"); }
            else
            {
                foreach (FixtureValue character in root.GetProperty("Characters").EnumerateArray())
                { characters++; Visit(character, "unit:" + Text(character, "AssetKey"), "character"); }
                foreach (string section in new[] { "BossScenarios", "OuterBosses", "ObservedCardUpgrades", "StatusRules" })
                    if (root.TryGetProperty(section, out FixtureValue rows) && rows.ValueKind == FixtureKind.Array)
                        foreach (FixtureValue row in rows.EnumerateArray())
                        {
                            if (section == "StatusRules") statuses++;
                            Visit(row, section + ":" + First(row, "Id", "AssetKey", "DataId"), section);
                        }
            }
        }

        string evidenceIndex = "tests/compatibility/game-2.3.1/mechanism-evidence.tsv";
        ReadEvidence(workspace, evidenceIndex, mechanisms);
        int queryMapped = mechanisms.Values.Count(item => item.Evidence != null);
        var table = new StringBuilder("Family\tNativeClass\tCatalogOwners\tCapturedConfigurations\tDeclaredVirtualMethods\tExampleLocations\tRuntimeEvidence\tEvidenceFixture\tEvidenceCheck\tEvidenceBoundary\tDefinitionOwners\tSelectorOwners\tParameterOwners\tDescriptorOwners\tOtherOwners\tDefinitionConfigurations\tDefinitionLocations\tReferenceLocations\n");
        foreach (Mechanism item in mechanisms.Values.OrderBy(item => item.Family, StringComparer.Ordinal).ThenBy(item => item.Name, StringComparer.Ordinal))
            table.AppendLine(string.Join("\t", item.Family, item.Name, item.Owners.Count, item.Configurations.Count,
                item.Hooks, string.Join(";", item.Locations.Take(3)), item.Evidence?.Level ?? "unmapped",
                item.Evidence?.Fixture ?? "-", item.Evidence?.CheckScript ?? "-", item.Evidence?.Boundary ?? "-",
                item.Count(ReferenceKind.Definition), item.Count(ReferenceKind.Selector), item.Count(ReferenceKind.Parameter),
                item.Count(ReferenceKind.Descriptor), item.Count(ReferenceKind.Other), item.DefinitionConfigurations.Count,
                string.Join(";", item.DefinitionLocations.Take(3)),
                item.ReferenceLocations.Count == 0 ? "-" : string.Join(";", item.ReferenceLocations.Take(3))));
        var summary = new StringBuilder("# Native mechanism audit inventory\n\n");
        summary.AppendLine($"Pinned game {Version}, MVID `{Module}`. Generated by `scripts/Get-MechanismInventory.ps1`.\n");
        summary.AppendLine("This is a finite list of concrete native classes for forward source review. It does **not** count runtime support, completed mechanisms, or completed cards. `unmapped` means existing native evidence has not been linked to this inventory.\n");
        summary.AppendLine($"{queryMapped} classes have an explicitly scoped `native-query-fixture` link. This means an archived query matrix is available, not that the class lifecycle is complete or that current code has passed its checker. The TSV links the checker and unresolved integration boundary.\n");
        summary.AppendLine("`DefinitionOwners` counts captured type declarations and added traits; it is a source-review lead, not proof that a live battle executes them. Selector/mask references, generic source/target-trait parameters (including unused defaults), and projected descriptors have separate owner counts. One owner may occur in multiple roles; their sum is not the unique owner count. Unknown fields remain in `OtherOwners`, never silently discarded.\n");
        summary.AppendLine("| Family | Concrete classes | Any catalog reference | Captured type declaration | Reference without declaration |\n| --- | ---: | ---: | ---: | ---: |");
        foreach (var group in mechanisms.Values.GroupBy(item => item.Family).OrderBy(group => group.Key, StringComparer.Ordinal))
            summary.AppendLine($"| {group.Key} | {group.Count()} | {group.Count(item => item.Owners.Count > 0)} | {group.Count(item => item.Count(ReferenceKind.Definition) > 0)} | {group.Count(item => item.Owners.Count > 0 && item.Count(ReferenceKind.Definition) == 0)} |");
        summary.AppendLine($"\nCatalog roots inspected: {cards} card definitions, {relics} relic-like definitions across all captured collections, {characters} characters, {statuses} status-rule definitions. Relic-like definitions include mutators, upgrades and other collections; this is not the collectable relic count.\n");
        summary.AppendLine("`CapturedConfigurations` groups distinct captured native parameter objects; it does not enumerate every code branch or live state. Locations omit array indices to group repeated lifecycle positions. Declared virtual methods help locate implementation hooks; inherited hooks still require source review.\n");
        summary.AppendLine("Zero catalog owners does not prove a class is unused. Only six status-rule definitions were captured; the assembly class list supplies the remaining source-review denominator. Boss behaviors, trigger dispatch, targeting, upgrade masks, queues, RNG, identity, preview/undo and other engine services require the separate acceptance gates in [the completion workflow](../mechanism-completion.md).\n");
        summary.AppendLine("## Reproducible inputs\n");
        summary.AppendLine($"- Native `Assembly-CSharp.dll` SHA-256: `{Hash(File.ReadAllBytes(assemblyPath))}`.");
        for (int i = 0; i < sources.Length; i++) summary.AppendLine($"- `{sources[i]}` SHA-256: `{hashes[i]}`.");
        summary.AppendLine($"- `{evidenceIndex}` SHA-256: `{Hash(File.ReadAllBytes(Path.Combine(workspace, evidenceIndex)))}`; linked archive hashes are validated against this index.");
        Write("docs/inventory/game-2.3.1-mechanisms.tsv", table.ToString());
        Write("docs/inventory/game-2.3.1-mechanisms.md", summary.ToString());
        Console.WriteLine($"MECHANISM-INVENTORY PASS: {mechanisms.Count} concrete classes, {mechanisms.Values.Count(item => item.Owners.Count > 0)} with catalog references, {mechanisms.Values.Count(item => item.Count(ReferenceKind.Definition) > 0)} with type declarations, {mechanisms.Values.Count(item => item.Owners.Count > 0 && item.Count(ReferenceKind.Definition) == 0)} reference-only; {cards} card roots, {relics} relic-like roots, {characters} character roots; {queryMapped} scoped query-fixture links, no completion inference.");

        string Name(EntityHandle handle) => handle.Kind switch
        {
            HandleKind.TypeDefinition => Qualified(metadata.GetTypeDefinition((TypeDefinitionHandle)handle).Namespace,
                metadata.GetTypeDefinition((TypeDefinitionHandle)handle).Name),
            HandleKind.TypeReference => Qualified(metadata.GetTypeReference((TypeReferenceHandle)handle).Namespace,
                metadata.GetTypeReference((TypeReferenceHandle)handle).Name),
            _ => ""
        };
        string Qualified(StringHandle space, StringHandle name) => metadata.GetString(space) is string ns && ns.Length > 0
            ? ns + "." + metadata.GetString(name) : metadata.GetString(name);
        string? Family(string name, HashSet<string> visited)
        {
            if (Bases.TryGetValue(name, out string? family)) return family;
            if (!visited.Add(name) || !definitions.TryGetValue(name, out TypeDefinition type) || type.BaseType.IsNil) return null;
            return Family(Name(type.BaseType), visited);
        }
        void Visit(FixtureValue value, string owner, string location, string field = "", bool descriptor = false)
        {
            if (value.ValueKind == FixtureKind.Object)
            {
                foreach (FixtureProperty property in value.EnumerateObject())
                {
                    // These are model projections, not native definition parameters.
                    if (property.Name is "Rule" or "Mask" or "ProjectionError") continue;
                    if (property.Value.ValueKind == FixtureKind.String && mechanisms.TryGetValue(property.Value.GetString()!, out Mechanism? item))
                        Record(item, value, owner, location + "." + property.Name, Classify(property.Name, descriptor));
                    else Visit(property.Value, owner, location + "." + property.Name, property.Name,
                        descriptor || property.Name == "MaskDescriptor");
                }
            }
            else if (value.ValueKind == FixtureKind.Array)
                foreach (FixtureValue element in value.EnumerateArray()) Visit(element, owner, location + "[]", field, descriptor);
            else if (value.ValueKind == FixtureKind.String && mechanisms.TryGetValue(value.GetString()!, out Mechanism? item))
                Record(item, value, owner, location, Classify(field, descriptor));
        }
        void Record(Mechanism item, FixtureValue value, string owner, string location, ReferenceKind kind)
        {
            item.Owners.Add(owner);
            string configuration = ConfigurationHash(value);
            item.Configurations.Add(configuration);
            item.Locations.Add(location);
            if (!item.References.TryGetValue(kind, out var owners)) item.References.Add(kind, owners = new(StringComparer.Ordinal));
            owners.Add(owner);
            if (kind == ReferenceKind.Definition)
            { item.DefinitionConfigurations.Add(configuration); item.DefinitionLocations.Add(location); }
            else item.ReferenceLocations.Add(kind + ":" + location);
        }
        void Write(string relative, string content)
        {
            string path = Path.Combine(workspace, relative);
            // Keep byte-for-byte regeneration independent of Windows newline defaults.
            content = content.Replace("\r\n", "\n");
            if (check)
            {
                if (!File.Exists(path) || File.ReadAllText(path) != content) throw new InvalidDataException("Stale mechanism inventory: " + relative);
            }
            else { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, content, new UTF8Encoding(false)); }
        }
    }

    private static void ReadEvidence(string workspace, string relativeIndex, SortedDictionary<string, Mechanism> mechanisms)
    {
        string[] lines = File.ReadAllLines(Path.Combine(workspace, relativeIndex));
        if (lines.Length == 0 || lines[0] != "NativeClass\tLevel\tFixture\tSha256\tCheckScript\tBoundary")
            throw new InvalidDataException("Invalid mechanism evidence index header.");
        var archives = new Dictionary<string, FixtureDocument>(StringComparer.Ordinal);
        foreach (string line in lines.Skip(1))
        {
            string[] cells = line.Split('\t');
            if (cells.Length != 6 || cells.Any(string.IsNullOrWhiteSpace) || cells[1] != "native-query-fixture" ||
                !mechanisms.TryGetValue(cells[0], out Mechanism? item) || item.Evidence != null)
                throw new InvalidDataException("Unknown, duplicate or unscoped mechanism evidence row.");
            string path = Local(cells[2]);
            if (!File.Exists(Local(cells[4]))) throw new InvalidDataException("Missing linked mechanism checker.");
            if (Hash(File.ReadAllBytes(path)) != cells[3]) throw new InvalidDataException("Mechanism evidence archive hash differs: " + cells[2]);
            if (!archives.TryGetValue(path, out FixtureDocument? document)) archives.Add(path, document = FixtureDocument.Read(path));
            FixtureValue root = document.RootElement;
            if (root.GetProperty("Schema").GetInt32() != 1 || Text(root, "GameModuleMvid") != Module || Text(root, "GameVersion") != Version ||
                Text(root, "Scope") != "DefinitionDrivenRoomHealthQueries" ||
                !root.GetProperty("DecisionUnchanged").GetBoolean() || !root.GetProperty("DefinitionsUnchanged").GetBoolean() ||
                !root.GetProperty("Before").ContentEquals(root.GetProperty("After")) ||
                !root.GetProperty("OriginalDefinitions").ContentEquals(root.GetProperty("OriginalDefinitionsAfter")) ||
                !root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")) ||
                root.GetProperty("FrameBefore").GetInt32() != root.GetProperty("FrameAfter").GetInt32() ||
                root.GetProperty("QueueBefore").GetInt32() != 0 || root.GetProperty("QueueAfter").GetInt32() != 0 ||
                root.GetProperty("Samples").GetArrayLength() != root.GetProperty("Definitions").GetArrayLength() * 720 ||
                !root.GetProperty("Definitions").EnumerateArray().Any(definition => Text(definition, "NativeType") == item.Name && Text(definition, "Collection") != "Control"))
                throw new InvalidDataException("Mechanism evidence version, scope, restoration or original identity is incomplete: " + cells[2]);
            item.Evidence = new Evidence(cells[1], cells[2], cells[4], cells[5]);
        }
        foreach (FixtureDocument document in archives.Values) document.Dispose();
        string Local(string relative)
        {
            string path = Path.GetFullPath(Path.Combine(workspace, relative));
            if (!path.StartsWith(workspace.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Mechanism evidence must stay inside the workspace.");
            return path;
        }
    }

    private static string Text(FixtureValue value, string name) => value.ValueKind == FixtureKind.Object &&
        value.TryGetProperty(name, out FixtureValue found) && found.ValueKind == FixtureKind.String ? found.GetString()! : "";
    private static string First(FixtureValue value, params string[] names) => names.Select(name => Text(value, name)).FirstOrDefault(text => text.Length != 0)
        ?? throw new InvalidDataException("Missing native inventory owner identity.");
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    internal static string ConfigurationHash(FixtureValue value) => Hash(Encoding.UTF8.GetBytes(Canonical(value)));
    private static string Canonical(FixtureValue value) => value.ValueKind switch
    {
        FixtureKind.Object => "{" + string.Concat(value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => Atom(property.Name) + Canonical(property.Value))) + "}",
        FixtureKind.Array => "[" + string.Concat(value.EnumerateArray().Select(Canonical)) + "]",
        FixtureKind.String => "s" + Atom(value.GetString()!),
        FixtureKind.Number => "n" + Atom(value.GetNumberText()),
        FixtureKind.True => "t", FixtureKind.False => "f", FixtureKind.Null => "0",
        _ => throw new InvalidDataException("Undefined native parameter.")
    };
    private static string Atom(string value) => value.Length + ":" + value;
}
