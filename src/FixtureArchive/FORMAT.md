# MT2 fixture binary format, version 1

`.mt2f` stores captured values as a typed, deduplicated directed acyclic graph.
It does not contain a JSON document or reconstruct JSON when reading model
inputs. Objects, arrays, strings, numbers, booleans and null have distinct tags.
Identical subtrees share a node, and identical text shares a string-table entry.
Object property order, duplicate keys, array order, nulls, Unicode and exact
number lexemes are preserved. The original formatting and string escape spelling
are not stored; this is a value-preserving format, not a byte archive of JSON.

The outer header is little endian:

| Field | Encoding |
| --- | --- |
| Magic | 8 bytes: `MT2FDAG` followed by NUL |
| Format version | Int32, currently 1 |
| Original uncompressed capture length | Int64 |
| Original uncompressed capture SHA-256 | 32 bytes |
| Binary payload length | Int64 |
| Binary payload SHA-256 | 32 bytes |
| Compressed payload length | Int64 |
| Payload | Brotli-compressed binary tables |

Direct native captures use zero for the text-source length and 32 zero bytes
for its hash: no source JSON document exists. `HasTextSource` distinguishes these
from imported captures. The binary payload SHA-256 and full archive manifest
hash remain applicable. This uses the existing version-one metadata layout;
previous imported archives remain readable without migration.

The payload contains a string count and UTF-8 strings, then a node count, root
ID and node records in that order.
Counts, string IDs and node references use .NET's seven-bit integer encoding.
Node tags are the explicit `FixtureKind` values. Arrays store child IDs; objects
store alternating string-key-node and value-node IDs. Every reference points to
an earlier node, so cycles and forward references are invalid. Version one
admits at most 128 nested containers and a 1 GiB binary payload.

Numbers use a numeric subtag: 0 is an Int64 encoded with ZigZag and a seven-bit
integer; 1 is a UInt64; 2 is IEEE-754 binary64. These encodings are used only when
they reproduce the original numeric lexeme exactly. Subtag 3 references literal
numeric text for unusual exponent/decimal spellings, avoiding silent rounding.
No entire object, array or document is stored as textual JSON.

The reader validates lengths, version, payload SHA-256, table references, object
keys, depth and trailing bytes before exposing the immutable graph. Constructor
hydration uses cached expression plans for the model's single public constructor,
matching constructor arguments to public properties and respecting optional
defaults. Collections and models are freshly constructed; concurrent branches
share only immutable fixture nodes. It does not call a JSON deserializer.

`LegacyFixtureImport` is an explicit import/verification path for old native
probe captures. Migration checks every original value against the decoded
binary graph, independently of the binary model hydrator. `manifest.tsv` records
original provenance and full archive SHA-256 values; regression validates the
curated archive inventory and those hashes before executing model checks.

To retain a newly verified native capture:

```powershell
dotnet run --project src/FixtureTools/FixtureTools.csproj -c Release -- pack <capture.json> tests/fixtures/<name>.mt2f
dotnet run --project src/FixtureTools/FixtureTools.csproj -c Release -- verify <capture.json> tests/fixtures/<name>.mt2f
```

Add the resulting archive to the curated regression list and its printed
provenance to the manifest. Local original captures stay in ignored probe/output
directories. The native capture schema and this archive format version are
independent.

The full-battle probe exports native binary captures by default. Use
`-BinaryCapture:$false` for the legacy diagnostic JSON path.
Its capture adapter uses Json.NET's contracts for property order, ignored fields
and exact scalar spellings, but constructs typed graph nodes directly and visits
shared object instances once. It requires a stable snapshot with supported
serialization settings and rejects cycles, converters and unsupported contracts.
No JSON container or document is produced on this path. The launcher inspects
the binary graph with shared dictionaries and arrays; model regression still
hydrates fresh model instances from the immutable nodes.

Use `-CaptureJson` to additionally export diagnostic JSON from the same snapshot.
It enables binary capture and is intended for validation, not speed measurement:

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy units-spells-and-junk -TriggeredStatus -CaptureJson
dotnet run --project src/FixtureTools -c Release -- compare-values <profile>/full-battle.json <profile>/full-battle.mt2f
```

An offline export benchmark can reuse a retained capture without starting Steam
or the game. It verifies the complete source graph and diagnostic JSON against
the emitted binary before reporting timings. These are .NET tool timings,
separate from Unity battle/process timings:

```powershell
dotnet run --project src/FixtureTools -c Release -- benchmark-capture tests/fixtures/full-battle-status-registry.mt2f results/capture-benchmark
```

For a direct native fixture, copy the validated archive into the curated fixture
directory and record its full archive hash, byte length and node count in the
manifest. Use a `native:` source label and zero text-source length/hash. The
capture's game version and module MVID identify the native runtime.

The native capture checks and benchmark use the installed game's Json.NET DLL.
`GameDir` can be overridden at build time, as for the probe project.
