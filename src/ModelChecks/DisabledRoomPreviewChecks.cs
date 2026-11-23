using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DisabledRoomPreviewChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (fixture.GetProperty("GameVersion").GetString() != Game231FixtureCatalog.Version ||
            fixture.GetProperty("GameModuleMvid").GetString() != Game231FixtureCatalog.Module)
            throw new InvalidDataException("Expected the original 2.3.1 decision archive.");
        FixtureValue[] decisions = fixture.GetProperty("Actions").EnumerateArray()
            .Concat(fixture.GetProperty("Turns").EnumerateArray()).SelectMany(row => new[]
                { row.GetProperty("Before"), row.GetProperty("Actual") }).ToArray();
        if (decisions.Length == 0 || decisions.Any(frame => frame.GetProperty("BattlePreviewEnabled").GetBoolean()))
            throw new InvalidDataException("Expected room preview disabled across the recorded battle.");
        BattleTurnState root = decisions[0].Deserialize<BattleTurnState>()!;
        CombatRelicState owner = root.Spawn.Train.Context!.Relics!.Single(
            item => item.DataId == RelicModel.DisableRoomPreviewOwnerId);
        if (owner.AssetKey != RelicModel.DisableRoomPreviewOwnerKey ||
            !owner.EffectTypes.SequenceEqual([RelicModel.DisableRoomPreview]) || RelicModel.Validate([owner]) != null)
            throw new InvalidDataException("Expected the unchanged original preview mutator.");
        RecordedBattleChecks.Native(fixture);
    }
}
