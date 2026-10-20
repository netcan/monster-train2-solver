using System;

namespace MonsterTrain2Poju.Model
{
    public static class RoomAttachmentTriggerIdentity
    {
        public static bool Known(string dataId) => dataId is
            "00f55f64-c5c1-4d5e-9f59-f50413341c1e" or
            "08001aa4-2e9d-42da-9ba7-ff4b0fd5ce80" or "57921ae8-ca98-4a3b-a659-5e741d2e904a" or
            "6ee4f735-fc54-4f85-aa13-fd0d169af3b2" or "93759003-bb97-44e4-9ef6-472c55bee2d9" or
            "627f12a8-081e-437d-8a54-a20d719163b7" or
            "e221f8ac-7b5f-4485-a4d1-dedd22f61466" or
            "f73bc157-ef23-4641-a938-ecd31945ca28" or
            "9e203beb-65a2-4c3d-9f16-83724d98b002" or "db494633-3dda-4410-b9bf-c7e4d2d47d1f";

        // Native uses a fresh Guid as a trigger-removal key. Canonicalize only
        // these card-created attachments, preserving identity without RNG draws.
        public static string Key(string dataId, int roomIndex, int ordinal)
        {
            byte[] bytes = Guid.Parse(dataId).ToByteArray();
            bytes[11] ^= checked((byte)roomIndex);
            uint serial = checked((uint)ordinal);
            for (int index = 0; index < 4; index++) bytes[12 + index] ^= (byte)(serial >> (index * 8));
            return new Guid(bytes).ToString();
        }

        internal static RoomCapacityAttachmentState Bind(RoomCapacityAttachmentState value, int roomIndex, int ordinal) =>
            new RoomCapacityAttachmentState(value.DataId, value.AssetKey, value.AttachmentType, value.CapacityDelta,
                value.CharacterUpgrades, value.AffectsHeroes, value.AffectsMonsters, value.IgnoreBosses,
                Key(value.DataId, roomIndex, ordinal), value.SpellCostDelta, value.MoonPhase, value.EnergyPerTurnDelta,
                value.CharacterFilters, value.PostCombat, value.PreCombat, value.StatusStackBonuses, value.SpawnCopySubtype,
                value.SpellUpgrade?.WithIdentity("room-spell:" + Key(value.DataId, roomIndex, ordinal)), value.DeathEffect, value.Ability, value.CorruptionModifier, value.Echo);
    }
}
