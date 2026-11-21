namespace MonsterTrain2Poju.Fixtures;

public static class NativeBranchIdentity
{
    public static readonly Guid HistoricalModule = new("8fb07b96-f4db-4d2b-884d-c00536d6ccf4");

    public static Guid SourceModule(FixtureValue source) => source.TryGetProperty("GameModuleMvid", out var module)
        ? ReadModule(module) : HistoricalModule;

    public static Guid RequestModule(FixtureValue request)
    {
        if (request.GetProperty("Schema").GetInt32() != 1 ||
            request.GetProperty("EvidenceKind").GetString() != "ModelUnsupportedBranch")
            throw new InvalidDataException("Expected a rejected model branch.");
        // Unpinned requests were recorded against the historical 2.2.1 module.
        return request.TryGetProperty("SourceGameModuleMvid", out var module) ? ReadModule(module) : HistoricalModule;
    }

    public static void RequireModule(FixtureValue request, Guid nativeModule)
    {
        Guid requested = RequestModule(request);
        if (requested != nativeModule)
            throw new InvalidDataException($"Native branch requires source game module {requested}. The native module is {nativeModule}.");
    }

    public static Guid ObservationModule(FixtureValue observation)
    {
        if (observation.GetProperty("Schema").GetInt32() != 1 ||
            observation.GetProperty("EvidenceKind").GetString() != "NativeUnsupportedBranchObservation" ||
            !observation.GetProperty("NativeCompleted").GetBoolean())
            throw new InvalidDataException("Expected a completed native branch observation.");
        Guid module = ReadModule(observation.GetProperty("GameModuleMvid"));
        RequireModule(observation.GetProperty("Request"), module);
        return module;
    }

    private static Guid ReadModule(FixtureValue value)
    {
        if (value.ValueKind != FixtureKind.String || !Guid.TryParseExact(value.GetString(), "D", out Guid module) || module == Guid.Empty)
            throw new InvalidDataException("Invalid native branch game module MVID.");
        return module;
    }
}
