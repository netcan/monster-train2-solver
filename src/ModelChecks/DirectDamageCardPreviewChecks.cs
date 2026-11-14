using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class DirectDamageCardPreviewChecks
{
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);
    internal static void Native(string path, bool composite = false)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("Boundary").GetString() == (composite ? "NativeCompositeDamageCardHover" : "NativeDirectDamageCardHover") &&
            root.GetProperty("NativeCompleted").GetBoolean() && root.GetProperty("AuthoredControls").GetBoolean() &&
            root.GetProperty("SelfTargetId").GetInt32() == 0 &&
            root.GetProperty("OriginalSuppression").GetInt32() == root.GetProperty("FinalSuppression").GetInt32(),
            "Incomplete native direct-damage card-hover evidence.");
        string teamName = root.GetProperty("TargetTeam").GetString()!;
        Require(teamName is "Player" or "Enemy", "Unknown direct-damage hover target team.");
        CombatTeam team = teamName == "Enemy" ? CombatTeam.Enemy : CombatTeam.Player;
        int baseHealth = root.GetProperty("BaseHealth").GetInt32();
        var rows = root.GetProperty("Samples").EnumerateArray().ToArray();
        int expectedCount = composite ? 96 : 32;
        Require(rows.Length == expectedCount && rows.Select(row => (row.GetProperty("Horde").GetBoolean(), row.GetProperty("Undying").GetBoolean(),
            row.GetProperty("ActiveHalfHealth").GetBoolean(), row.GetProperty("Lethal").GetBoolean(), row.GetProperty("Armor").GetInt32(),
            composite && row.GetProperty("RoomEffect").GetBoolean(), composite && row.GetProperty("OnCastDamage").GetBoolean())).Distinct().Count() == expectedCount,
            "Missing direct-damage Horde/Undying/half-health/lethal/armor controls.");
        foreach (var row in rows)
        {
            string label = row.GetProperty("Label").GetString()!;
            var before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var rule = row.GetProperty("Rule").Deserialize<CardPlayRule>()!;
            var target = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == row.GetProperty("TargetId").GetInt32());
            var actual = row.GetProperty("ActualTarget").Deserialize<CombatUnit>()!;
            bool horde = row.GetProperty("Horde").GetBoolean(), undying = row.GetProperty("Undying").GetBoolean(), lethal = row.GetProperty("Lethal").GetBoolean();
            int armor = row.GetProperty("Armor").GetInt32(), amount = row.GetProperty("Amount").GetInt32();
            bool room = composite && row.GetProperty("RoomEffect").GetBoolean(), onCast = composite && row.GetProperty("OnCastDamage").GetBoolean();
            Require(!composite || room || onCast, "Missing composite damage mode: " + label);
            string targetMode = room ? "Room" : "DropTargetCharacter";
            if (composite)
            {
                var actors = row.GetProperty("ActualTargets").Deserialize<CombatUnit[]>()!;
                Require(actors.Length == row.GetProperty("CopiedIds").GetArrayLength() && actors.Length >= 3 &&
                    row.GetProperty("DropTargetId").GetInt32() == (room ? 0 : target.Id) &&
                    (!room || !lethal || undying || actors.Count(unit => unit.Health == 0) >= 2),
                    "Missing targetless or multiple-victim composite damage evidence: " + label);
            }
            var dispatches = row.GetProperty("Dispatches").EnumerateArray().Where(dispatch => dispatch.GetProperty("ActorId").GetInt32() == target.Id).ToArray();
            Require(baseHealth >= 2 && target.Team == team && armor is 0 or 3 && (target.Status("armor")?.Stacks ?? 0) == armor &&
                target.Triggers.Count(HalfHealthDeathwishModel.IsTrigger) == 2 && target.Triggers.Where(HalfHealthDeathwishModel.IsTrigger)
                    .All(trigger => trigger.DeathwishActive == row.GetProperty("ActiveHalfHealth").GetBoolean()) &&
                rule.Effects.Count == 1 && rule.OnCastEffects.Count == (onCast ? 1 : 0) &&
                rule.Effects.Concat(rule.OnCastEffects).All(effect => effect.Type == "Damage" && effect.Target == targetMode && effect.Range == null &&
                    (!room || effect.AllowEnemy && effect.AllowPlayer)) &&
                rule.Effects[0].Value == (onCast ? 1 : amount) && (!onCast || rule.OnCastEffects[0].Value == amount - 1) &&
                row.GetProperty("NativeReturned").GetBoolean() && row.GetProperty("QueueEmpty").GetBoolean() &&
                row.GetProperty("TemporaryDisabled").GetBoolean() && row.GetProperty("EnableCount").GetInt32() == 1 && row.GetProperty("DisableCount").GetInt32() == 1,
                "Native direct-damage control or temporary-state boundary differs: " + label);
            Require(actual.Health == (lethal ? undying ? 1 : 0 : target.Health - Math.Max(0, amount - armor)) &&
                actual.MaxHealth == target.MaxHealth - (horde && lethal ? baseHealth : 0) &&
                (actual.Status("armor")?.Stacks ?? 0) == Math.Max(0, armor - amount) &&
                (actual.Status("horde")?.Stacks ?? 0) == (horde ? lethal ? 1 : 2 : 0) &&
                (actual.Status("undying")?.Stacks ?? 0) == (undying && !lethal ? 1 : 0) &&
                actual.DeathState is { IsSacrifice: false } && actual.DeathState.HasFinishedDying == (lethal && !undying) &&
                dispatches.Count(dispatch => dispatch.GetProperty("Kind").GetString() == "OnDeath") == (lethal ? 1 : 0) &&
                dispatches.Count(dispatch => dispatch.GetProperty("Kind").GetString() == "OnReanimated") == (lethal && undying ? 1 : 0),
                "Native direct-damage armor, Horde casualty or revival differs: " + label);
            foreach (string stage in new[] { "CopiedActual", "After", "Cleared" })
                Require(row.GetProperty(stage).GetProperty("Context").GetProperty("Statistics").GetProperty("LastAttackDamageDealt").GetInt32() ==
                    before.Context!.Statistics!.LastAttackDamageDealt, "Ordinary spell hover has an unexpected attacker statistic: " + label + "/" + stage);
            CardHoverFixtureChecks.Match(row);
        }
        Console.WriteLine((composite ? "COMPOSITE" : "DIRECT") + "-DAMAGE-CARD-PREVIEW PASS: " + expectedCount + " native " + teamName + " hovers, ordinary/Horde/Undying/half-health, armor and lethal/nonlethal damage" +
            (composite ? ", room sweeps and main/OnCast composition" : "") + "; " +
            "complete temporary targets and copy/effect/cancel states, gameplay RNG and 16 immutable branches per root.");
    }
}
