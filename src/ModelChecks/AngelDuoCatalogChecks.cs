using System.Text.Json.Nodes;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;

internal static class AngelDuoCatalogChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 3 &&
            fixture.GetProperty("CharacterCatalogScope").GetString() == "AllGameData" &&
            fixture.GetProperty("CardCatalogScope").GetString() == "AllGameData",
            "Angel Duo inventory requires the complete original native definition catalog.");
        var variants = new[] {
            (Scenario: "LoopAttackResolveHealth", Outer: "Variant1_ResolveHealth", Inner: "Variant1_LoopAttack",
                HealthTrigger: "PostCombat", Health: 60, AttackTrigger: "OnTrainRoomLoop", Attack: 8),
            (Scenario: "RallyAttackIncantHealth", Outer: "Variant2_IncantHealth", Inner: "Variant2_SlayAttack",
                HealthTrigger: "CardSpellPlayed", Health: 30, AttackTrigger: "OnKill", Attack: 3),
            (Scenario: "RevengeAttackHarvestHealth", Outer: "Variant3_HarvestHealth", Inner: "Variant3_RevengeAttack",
                HealthTrigger: "OnAnyHeroDeathOnFloor", Health: 50, AttackTrigger: "OnHit", Attack: 1)
        };
        var scenarios = fixture.GetProperty("BossScenarios").EnumerateArray()
            .Where(row => row.GetProperty("OuterBoss").GetString()!.Contains("AngelDuo")).ToArray();
        Require(scenarios.Length == 3, "The original Angel Duo scenario inventory changed.");
        string? outerShape = null, innerShape = null;
        foreach (var variant in variants)
        {
            string outer = "Boss_Level6_AngelDuo_Entropic_" + variant.Outer;
            string inner = "Boss_Level6_AngelDuo_Savage_" + variant.Inner;
            FixtureValue scenario = scenarios.Single(row => row.GetProperty("AssetKey").GetString() == "Level6AngelDuo_" + variant.Scenario);
            Require(scenario.GetProperty("OuterBoss").GetString() == outer &&
                scenario.GetProperty("HarderOuterBoss").GetString() == outer,
                "Angel Duo scenario selected an unexpected original outer boss.");
            FixtureValue[] relics = scenario.GetProperty("EnemyRelics").EnumerateArray().ToArray();
            Require(relics.Length == 1, "Angel Duo scenario must retain its one original battle relic.");
            FixtureValue[] effects = relics[0].GetProperty("Effects").EnumerateArray().ToArray();
            Require(effects.Length == 2, "Angel Duo battle relic must retain both original trigger owners.");
            CheckTrigger(effects, outer, variant.HealthTrigger, "CardEffectBuffMaxHealth", variant.Health, "SubtypesData_SavageAngel");
            CheckTrigger(effects, inner, variant.AttackTrigger, "CardEffectBuffDamage", variant.Attack, "SubtypesData_EntropicAngel");
            foreach (string asset in new[] { outer, inner })
            {
                FixtureValue character = fixture.GetProperty("Characters").EnumerateArray()
                    .Single(row => row.GetProperty("AssetKey").GetString() == asset);
                var shape = JsonSerializer.SerializeToNode(character.ToObjectGraph())!.AsObject();
                shape.Remove("AssetKey");
                string serialized = shape.ToJsonString();
                if (asset == outer)
                {
                    outerShape ??= serialized;
                    Require(serialized == outerShape, "Original Entropic birth/death payloads differ between variants.");
                }
                else
                {
                    innerShape ??= serialized;
                    Require(serialized == innerShape, "Original Savage birth/death payloads differ between variants.");
                }
            }
        }
        Console.WriteLine("ANGEL-DUO-CATALOG PASS: all three original scenarios, six identical role definitions and six distinct growth callbacks; runtime calibration remains separate.");
    }

    private static void CheckTrigger(FixtureValue[] effects, string actor, string kind, string type, int value, string subtype)
    {
        FixtureValue owner = effects.Single(effect => effect.GetProperty("Characters").GetArrayLength() == 1 &&
            effect.GetProperty("Characters")[0].GetString() == actor);
        Require(owner.GetProperty("Type").GetString() == "RelicEffectAddTrigger" &&
            owner.GetProperty("Triggers").GetArrayLength() == 1, "Unexpected original Angel Duo relic effect.");
        FixtureValue trigger = owner.GetProperty("Triggers")[0];
        Require(trigger.GetProperty("Kind").GetString() == kind && !trigger.GetProperty("Once").GetBoolean() &&
            !trigger.GetProperty("OnlyIfEquipped").GetBoolean() && trigger.GetProperty("Threshold").GetInt32() == 0 &&
            trigger.GetProperty("Effects").GetArrayLength() == 1, "Unexpected Angel Duo growth trigger signature.");
        FixtureValue action = trigger.GetProperty("Effects")[0];
        Require(action.GetProperty("Type").GetString() == type && action.GetProperty("Int").GetInt32() == value &&
            action.GetProperty("Target").GetString() == "Tower" && action.GetProperty("Team").GetString() == "Heroes, Monsters" &&
            action.GetProperty("CardSubtype").GetString() == subtype && !action.GetProperty("UseRange").GetBoolean(),
            "Unexpected original Angel Duo growth effect payload.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
