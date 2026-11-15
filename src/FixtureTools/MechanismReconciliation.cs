using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;

// Code navigation and a finite review queue. References are not behavioral proof.
internal static class MechanismReconciliation
{
    private const string Module = "bf93f060-beda-46b1-b808-95ebf1e9a99d";
    private sealed record Native(string Family, string Name, int Owners, string StatusId, string[] Parents);
    private sealed record Link(string Symbol, string Source, string Match, bool Guard);
    private sealed record Review(string Class, string Disposition, string Model, string Probe, string Checker, string Boundary);
    private sealed record Index(string Hash, Dictionary<string, List<Link>> Links, HashSet<string> Symbols, string SourceHash);
    private static readonly Dictionary<ushort, OpCode> Codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(code => unchecked((ushort)code.Value));

    internal static void Run(string workspace, string gameAssembly, bool check)
    {
        workspace = Path.GetFullPath(workspace);
        // Validate the pinned catalogs and their complete generated class denominator first.
        MechanismInventory.Run(workspace, gameAssembly, true);
        var native = ReadNative(workspace, gameAssembly);
        var model = ReadIndex(workspace, "src/Model/bin/Release/netstandard2.1/Model.dll", native);
        var probe = ReadIndex(workspace, "src/Probe/bin/Release/netstandard2.1/Probe.dll", native);
        var checks = ReadIndex(workspace, "src/ModelChecks/bin/Release/net8.0/ModelChecks.dll", native);
        foreach (string consumer in new[] { "src/Probe/bin/Release/netstandard2.1/Model.dll", "src/ModelChecks/bin/Release/net8.0/Model.dll" })
            if (Hash(File.ReadAllBytes(Path.Combine(workspace, consumer))) != model.Hash)
                throw new InvalidDataException("Stale consumer model: " + consumer);
        string reviewPath = "tests/compatibility/game-2.3.1/mechanism-reviews.tsv";
        var reviews = ReadReviews(workspace, reviewPath, native, model, probe);
        var rows = new List<(Native Native, string Queue, Review? Review, List<Link> Model, List<Link> Probe, List<Link> Checks)>();
        foreach (var item in native.Values)
        {
            var m = model.Links[item.Name]; var p = probe.Links[item.Name]; var c = checks.Links[item.Name];
            reviews.TryGetValue(item.Name, out Review? review);
            Link[] directModel = m.Where(link => !link.Match.StartsWith("base-", StringComparison.Ordinal)).ToArray();
            string queue = review?.Disposition == "known-gap" ? "known-gap" : review != null ? "scoped-review-recorded" : item.Owners == 0 ? "resolve-entry" :
                m.Count == 0 && p.Count == 0 ? "inspect-no-code-link" : directModel.Length == 0 && m.Count > 0 ? "inspect-inherited-code" :
                m.Count == 0 ? "inspect-projection-normalization" : directModel.All(link => link.Guard) ? "inspect-guard-links" : "review-existing-code";
            rows.Add((item, queue, review, m, p, c));
        }
        rows = rows.OrderBy(row => Rank(row.Queue)).ThenByDescending(row => row.Native.Owners).ThenBy(row => row.Native.Name, StringComparer.Ordinal).ToList();
        var table = new StringBuilder("Queue\tFamily\tNativeClass\tCatalogOwners\tNativeStatusId\tNativeParentClasses\tModelMethods\tProbeMethods\tCheckerMethods\tModelExamples\tProbeExamples\tCheckerExamples\tReviewedDisposition\tReviewedModelSymbols\tReviewedProbeSymbols\tCheckScript\tBoundary\n");
        foreach (var row in rows)
            table.AppendLine(string.Join("\t", row.Queue, row.Native.Family, row.Native.Name, row.Native.Owners,
                Empty(row.Native.StatusId), Empty(string.Join(",", row.Native.Parents)), row.Model.Count, row.Probe.Count, row.Checks.Count,
                Examples(row.Model), Examples(row.Probe), Examples(row.Checks), row.Review?.Disposition ?? "unreviewed",
                row.Review?.Model ?? "-", row.Review?.Probe ?? "-", row.Review?.Checker ?? "-", row.Review?.Boundary ?? "No behavioral completion inference from code references."));
        var summary = new StringBuilder("# 固定版本机制与现有代码对账\n\n");
        summary.AppendLine("固定 2.3.1 / build 26650011，MVID `" + Module + "`。由 `scripts/Get-MechanismReconciliation.ps1` 生成。\n");
        summary.AppendLine("扫描编译后的模型、采集探针与检查器 IL，索引实际字符串和原生类型操作数；状态类同时读取原生 `const StatusId`。非公共根类的继承引用标为 `base-*`，用于查共享实现，不能据此确认派生类行为。PDB 必须与程序集匹配，工作区源码校验和必须与 PDB 一致，两个消费者的模型字节必须一致。注释、未使用常量和文档不参与代码匹配。\n");
        summary.AppendLine("这是有限的源码审查队列，不是剩余未实现数量或完成率。一个方法中出现类型／状态名不能证明该类已实现：可能只是保护、快照、构造测试或归一化入口。状态 ID 引用同样不证明完整生命周期；探针包含生产采集与隔离测试代码，不能直接当作执行模型。\n");
        summary.AppendLine("| 待办分类 | 类数 | 含捕获定义引用 |\n| --- | ---: | ---: |");
        foreach (var group in rows.GroupBy(row => row.Queue))
            summary.AppendLine($"| {group.Key} | {group.Count()} | {group.Count(row => row.Native.Owners > 0)} |");
        summary.AppendLine($"\n总计 {rows.Count} 个具体类，{rows.Count(row => row.Native.Owners > 0)} 类有目录引用；{native.Values.Count(item => item.StatusId.Length > 0)} 个状态类取得原生 StatusId。已记录 {reviews.Count} 项有边界的人工审查，仍未将任何类自动判为完整完成。\n");
        summary.AppendLine("`known-gap` 是源码审查确认的有边界缺口；`inspect-no-code-link` 优先核对入口与别名；`inspect-inherited-code` 只有父类的模型链接；`inspect-projection-normalization` 核对采集端是否已转为通用模型动作；`inspect-guard-links` 仅提示所有直接模型引用都落在按名称识别的保护方法，需人工确认。无链接不等于未实现。`resolve-entry` 没有捕获目录引用，仍需查动态入口和未捕获定义。\n");
        summary.AppendLine("共享机制审查一次记录原生类、模型消费者、采集入口、定向检查器和明确边界，后续按真实反例修复；不为同一实现的每张卡各造一场战斗。`input-only` 记录战前取得的输入或定义标记，不认证其战役消费者已实现。清单 TSV 按审查类别、目录引用数排序，源码例子最多各列两个方法。\n");
        summary.AppendLine("## 已有源码审查结论\n\n| 原生类 | 结论 | 边界 |\n| --- | --- | --- |");
        foreach (Review review in reviews.Values.OrderBy(item => item.Disposition == "known-gap" ? 0 : 1).ThenBy(item => item.Class, StringComparer.Ordinal))
            summary.AppendLine($"| {review.Class} | {review.Disposition} | {review.Boundary.Replace("|", "/")} |");
        summary.AppendLine("## 第一批需核对的代码入口\n\n| 原生类 | 队列 | 目录引用 | 模型／探针示例 |\n| --- | --- | ---: | --- |");
        foreach (var row in rows.Where(row => row.Native.Owners > 0 && row.Review == null).Take(15))
            summary.AppendLine($"| {row.Native.Name} | {row.Queue} | {row.Native.Owners} | {MarkdownExample(row.Model.Concat(row.Probe).FirstOrDefault())} |");
        summary.AppendLine("\n## 可重建输入\n");
        summary.AppendLine("- 原版 DLL SHA-256: `" + Hash(File.ReadAllBytes(gameAssembly)) + "`。");
        summary.AppendLine("- 原生类清单 SHA-256: `" + Hash(File.ReadAllBytes(Path.Combine(workspace, "docs/inventory/game-2.3.1-mechanisms.tsv"))) + "`。");
        summary.AppendLine("- 人工审查索引 SHA-256: `" + Hash(File.ReadAllBytes(Path.Combine(workspace, reviewPath))) + "`。");
        foreach (var entry in new[] { ("Model", model), ("Probe", probe), ("ModelChecks", checks) })
            summary.AppendLine($"- {entry.Item1}: DLL `{entry.Item2.Hash}`；PDB 校验过的工作区源码集合 `{entry.Item2.SourceHash}`。");
        summary.AppendLine("\n`-Check` 要求生成内容逐字一致，源码、构建或审查声明变化后必须重建。检查器链接只证明存在，仍需运行相应检查器确认当前通过；旧版本证据不能据此升级为新版整场证据。");
        Write("docs/inventory/game-2.3.1-reconciliation.tsv", table.ToString());
        Write("docs/inventory/game-2.3.1-reconciliation.md", summary.ToString());
        Console.WriteLine($"MECHANISM-RECONCILIATION PASS: {rows.Count} native classes; {rows.Count(row => row.Model.Count > 0)} model-linked, {rows.Count(row => row.Probe.Count > 0)} probe-linked, {rows.Count(row => row.Checks.Count > 0)} checker-linked; {reviews.Count} scoped reviews. References are not runtime coverage.");

        void Write(string relative, string content)
        {
            string path = Path.Combine(workspace, relative); content = content.Replace("\r\n", "\n");
            if (check) { if (!File.Exists(path) || File.ReadAllText(path) != content) throw new InvalidDataException("Stale mechanism reconciliation: " + relative); }
            else File.WriteAllText(path, content, new UTF8Encoding(false));
        }
    }

    private static Dictionary<string, Native> ReadNative(string workspace, string assembly)
    {
        using var pe = new PEReader(File.OpenRead(assembly)); var metadata = pe.GetMetadataReader();
        if (metadata.GetGuid(metadata.GetModuleDefinition().Mvid).ToString() != Module) throw new InvalidDataException("Unpinned native assembly.");
        var types = metadata.TypeDefinitions.Where(handle => !metadata.GetTypeDefinition(handle).IsNested)
            .ToDictionary(handle => Qualified(metadata.GetString(metadata.GetTypeDefinition(handle).Namespace), metadata.GetString(metadata.GetTypeDefinition(handle).Name)));
        var native = new Dictionary<string, Native>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(Path.Combine(workspace, "docs/inventory/game-2.3.1-mechanisms.tsv")).Skip(1))
        {
            string[] cells = line.Split('\t'); string id = "";
            if (cells[0] == "status-effect")
                foreach (var handle in metadata.GetTypeDefinition(types[cells[1]]).GetFields())
                {
                    var field = metadata.GetFieldDefinition(handle); var value = field.GetDefaultValue();
                    if (metadata.GetString(field.Name) != "StatusId" || value.IsNil) continue;
                    var constant = metadata.GetConstant(value);
                    if (constant.TypeCode != ConstantTypeCode.String) throw new InvalidDataException("Non-string native StatusId.");
                    id = Encoding.Unicode.GetString(metadata.GetBlobBytes(constant.Value));
                }
            var parents = new List<string>(); var parent = metadata.GetTypeDefinition(types[cells[1]]).BaseType;
            while (parent.Kind == HandleKind.TypeDefinition)
            {
                var definition = metadata.GetTypeDefinition((TypeDefinitionHandle)parent);
                string name = Qualified(metadata.GetString(definition.Namespace), metadata.GetString(definition.Name));
                if (name is "CardEffectBase" or "CardTraitState" or "RelicEffectBase" or "RoomStateModifierBase" or "StatusEffectState") break;
                if (parents.Contains(name, StringComparer.Ordinal)) throw new InvalidDataException("Native base-class cycle.");
                parents.Add(name); parent = definition.BaseType;
            }
            native.Add(cells[1], new Native(cells[0], cells[1], int.Parse(cells[2]), id, parents.ToArray()));
        }
        return native;
    }

    private static Index ReadIndex(string workspace, string relative, Dictionary<string, Native> native)
    {
        string path = Path.Combine(workspace, relative);
        using var pe = new PEReader(File.OpenRead(path)); var metadata = pe.GetMetadataReader();
        using var provider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(Path.ChangeExtension(path, ".pdb")));
        var pdb = provider.GetMetadataReader(); var id = new BlobContentId(pdb.DebugMetadataHeader!.Id);
        if (!pe.ReadDebugDirectory().Any(entry => entry.Type == DebugDirectoryEntryType.CodeView && pe.ReadCodeViewDebugDirectoryData(entry).Guid == id.Guid && entry.Stamp == id.Stamp))
            throw new InvalidDataException("PDB does not match: " + relative);
        string prefix = workspace.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var documents = new Dictionary<DocumentHandle, string>(); var sourceHashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var handle in pdb.Documents)
        {
            var document = pdb.GetDocument(handle); string file = Path.GetFullPath(pdb.GetString(document.Name));
            if (!file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("PDB source is outside the audited workspace: " + file);
            byte[] bytes = File.ReadAllBytes(file); Guid algorithm = pdb.GetGuid(document.HashAlgorithm);
            byte[] hash = algorithm == new Guid("8829d00f-11b8-4213-878b-770e8597ac16") ? SHA256.HashData(bytes) :
                algorithm == new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460") ? SHA1.HashData(bytes) : throw new InvalidDataException("Unsupported PDB checksum.");
            if (!hash.SequenceEqual(pdb.GetBlobBytes(document.Hash))) throw new InvalidDataException("Build is stale for source " + file);
            string source = Path.GetRelativePath(workspace, file).Replace('\\', '/');
            documents.Add(handle, source); sourceHashes[source] = Hash(bytes);
        }
        if (documents.Count == 0) throw new InvalidDataException("PDB has no verifiable workspace source: " + relative);
        var links = native.Keys.ToDictionary(name => name, _ => new List<Link>(), StringComparer.Ordinal);
        var statusIds = native.Values.Where(item => item.StatusId.Length > 0).GroupBy(item => item.StatusId).ToDictionary(group => group.Key, group => group.Select(item => item.Name).ToArray(), StringComparer.Ordinal);
        var bases = native.Values.SelectMany(item => item.Parents.Select(parent => (parent, item.Name))).GroupBy(item => item.parent)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Name).ToArray(), StringComparer.Ordinal);
        var symbols = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in metadata.MethodDefinitions)
        {
            var method = metadata.GetMethodDefinition(handle); string methodName = metadata.GetString(method.Name);
            string symbol = TypeName(method.GetDeclaringType()) + "." + methodName; symbols.Add(symbol);
            if (method.RelativeVirtualAddress == 0) continue;
            var debug = pdb.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(MetadataTokens.GetRowNumber(handle)));
            string source = "-";
            foreach (var point in debug.GetSequencePoints())
            {
                var doc = point.Document.IsNil ? debug.Document : point.Document;
                if (!point.IsHidden && documents.TryGetValue(doc, out string? file)) { source = file + ":" + point.StartLine; break; }
            }
            bool guard = new[] { "Validate", "Known", "Supports", "IsSupported", "IsNativeEffect" }
                .Any(name => methodName.StartsWith(name, StringComparison.Ordinal) || methodName.Contains("<" + name + ">", StringComparison.Ordinal));
            var found = new Dictionary<string, string>(StringComparer.Ordinal);
            byte[] body = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
            for (int offset = 0; offset < body.Length;)
            {
                ushort key = body[offset++]; if (key == 0xfe) key = (ushort)(0xfe00 | body[offset++]);
                if (!Codes.TryGetValue(key, out OpCode code)) throw new InvalidDataException("Unknown IL opcode.");
                int size = OperandSize(code.OperandType, body, offset);
                if (size < 0 || offset > body.Length - size) throw new InvalidDataException("Invalid IL operand.");
                if (code.OperandType == OperandType.InlineString)
                {
                    string literal = metadata.GetUserString(MetadataTokens.UserStringHandle(BitConverter.ToInt32(body, offset) & 0xffffff));
                    if (native.ContainsKey(literal)) found[literal] = "class-literal";
                    else if (statusIds.TryGetValue(literal, out string[]? classes)) foreach (string name in classes) found.TryAdd(name, "status-id-literal");
                    if (bases.TryGetValue(literal, out string[]? derived)) foreach (string name in derived) found.TryAdd(name, "base-class-literal:" + literal);
                }
                else if (code.OperandType is OperandType.InlineType or OperandType.InlineTok or OperandType.InlineMethod or OperandType.InlineField)
                {
                    string name = OperandTypeName(MetadataTokens.EntityHandle(BitConverter.ToInt32(body, offset)));
                    if (native.ContainsKey(name)) found[name] = "native-type";
                    if (bases.TryGetValue(name, out string[]? derived)) foreach (string child in derived) found.TryAdd(child, "base-native-type:" + name);
                }
                offset += size;
            }
            foreach (var entry in found) links[entry.Key].Add(new Link(symbol, source, entry.Value, guard));
        }
        foreach (string name in links.Keys.ToArray()) links[name] = links[name].Distinct().OrderBy(link => link.Source, StringComparer.Ordinal).ThenBy(link => link.Symbol, StringComparer.Ordinal).ToList();
        return new Index(Hash(File.ReadAllBytes(path)), links, symbols, Hash(Encoding.UTF8.GetBytes(string.Concat(sourceHashes.Select(pair => pair.Key + "\t" + pair.Value + "\n")))));

        string TypeName(TypeDefinitionHandle handle)
        {
            var type = metadata.GetTypeDefinition(handle); string name = metadata.GetString(type.Name);
            return type.IsNested ? TypeName(type.GetDeclaringType()) + "+" + name : metadata.GetString(type.Namespace) is string space && space.Length > 0 ? space + "." + name : name;
        }
        string OperandTypeName(EntityHandle handle) => handle.Kind switch
        {
            HandleKind.TypeReference => Qualified(metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)handle).Namespace), metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)handle).Name)),
            HandleKind.TypeDefinition => Qualified(metadata.GetString(metadata.GetTypeDefinition((TypeDefinitionHandle)handle).Namespace), metadata.GetString(metadata.GetTypeDefinition((TypeDefinitionHandle)handle).Name)),
            HandleKind.MemberReference => OperandTypeName(metadata.GetMemberReference((MemberReferenceHandle)handle).Parent),
            HandleKind.MethodDefinition => OperandTypeName(metadata.GetMethodDefinition((MethodDefinitionHandle)handle).GetDeclaringType()),
            HandleKind.FieldDefinition => OperandTypeName(metadata.GetFieldDefinition((FieldDefinitionHandle)handle).GetDeclaringType()),
            // Generic instantiations are not treated as a direct native type reference.
            _ => ""
        };
    }

    private static int OperandSize(OperandType type, byte[] body, int offset) => type switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => offset <= body.Length - 4 ? checked(4 + 4 * BitConverter.ToInt32(body, offset)) : -1,
        OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI or OperandType.InlineMethod or
        OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType or OperandType.ShortInlineR => 4,
        _ => throw new InvalidDataException("Unknown IL operand type.")
    };

    private static Dictionary<string, Review> ReadReviews(string workspace, string path, Dictionary<string, Native> native, Index model, Index probe)
    {
        string[] lines = File.ReadAllLines(Path.Combine(workspace, path));
        if (lines[0] != "NativeClass\tDisposition\tModelSymbols\tProbeSymbols\tCheckScript\tBoundary") throw new InvalidDataException("Unexpected review header.");
        var reviews = new Dictionary<string, Review>(StringComparer.Ordinal);
        foreach (string line in lines.Skip(1))
        {
            string[] cells = line.Split('\t');
            if (cells.Length != 6 || cells.Any(string.IsNullOrWhiteSpace) || !native.ContainsKey(cells[0]) ||
                cells[1] is not ("shared-path" or "campaign-only" or "input-only" or "known-gap") || cells[5].Length < 30)
                throw new InvalidDataException("Incomplete native review declaration.");
            foreach (var pair in new[] { (cells[2], model), (cells[3], probe) })
                if (pair.Item1 != "-") foreach (string symbol in pair.Item1.Split(';'))
                    if (!pair.Item2.Symbols.Contains(symbol)) throw new InvalidDataException("Review points to a missing method: " + symbol);
            if ((cells[1] is "shared-path" or "campaign-only") && cells[2] == "-") throw new InvalidDataException("Reviewed implementation needs a model binding.");
            if (cells[4] != "-")
            {
                string checker = Path.GetFullPath(Path.Combine(workspace, cells[4]));
                if (!checker.StartsWith(workspace.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(checker))
                    throw new InvalidDataException("Missing or external review checker.");
            }
            if (!reviews.TryAdd(cells[0], new Review(cells[0], cells[1], cells[2], cells[3], cells[4], cells[5]))) throw new InvalidDataException("Duplicate mechanism review.");
        }
        return reviews;
    }
    private static int Rank(string queue) => queue switch { "known-gap" => 0, "inspect-no-code-link" => 1, "inspect-inherited-code" => 2, "inspect-projection-normalization" => 3, "inspect-guard-links" => 4, "review-existing-code" => 5, "resolve-entry" => 6, _ => 7 };
    private static string Empty(string text) => text.Length == 0 ? "-" : text;
    private static string Qualified(string space, string name) => space.Length == 0 ? name : space + "." + name;
    private static string Examples(IEnumerable<Link> links) => Empty(string.Join(";", links.OrderBy(link => link.Guard).Take(2).Select(link => link.Source + " " + link.Symbol + " [" + link.Match + "]")));
    private static string MarkdownExample(Link? link) => link == null ? "无直接链接" : link.Source == "-" ? "`" + link.Symbol + "`" : "[" + link.Symbol + "](../../" + link.Source + ")";
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
