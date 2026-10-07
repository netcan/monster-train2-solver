using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class PostCombatHealingChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(97);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 3, 10, cardInstances: [], statistics: BattleStatistics.Empty());
        var gold = new CombatEffect("CardEffectRewardGold", 1, 0, "", 0, [], false);
        CombatEffect Heal(int value, string target = "Self", bool both = false) => new("CardEffectHeal", value, 0, "", 0, [], false,
            action: new("Heal", target, value, both, true, []));
        CombatEffect Unhealed(string name) => new("CardEffectAddTempCardUpgradeToUnits", 0, 0, "", 0, [], false,
            unitUpgrade: new("UnitUpgrade", "Self", 0, true, true, [], new(name, name, new(), [], false, false, false, 5, 0, []), "TemporaryUntilUnitDeath"));
        CombatTrigger Trigger(string kind, CombatEffect[] effects, bool once = false, bool ignored = false, bool deployment = false) =>
            new(kind, once, false, ignored, 1, effects, deployment);
        CombatUnit Unit(int id, CombatTeam team, CombatTrigger[] triggers, CombatStatus[]? statuses = null, bool attack = false) =>
            new(id, "unit", team, attack ? 1 : 0, 2, 10, attack, false, false, statuses ?? [], triggers,
                modifiers: new(attack ? 1 : 0, 0, 0, 1, 1, true, false, []), isBoss: false);
        RoomCombatState Room(params CombatUnit[] units) => new(0, false, units, [], context);
        var root = Room(Unit(1, CombatTeam.Enemy, [Trigger("PostCombatHealing", [Heal(999)]), Trigger("PostCombat", [Unhealed("enemy")])]),
            Unit(2, CombatTeam.Player, [Trigger("PostCombatHealing", [Heal(5, "Room", both: true)]), Trigger("PostCombat", [Unhealed("player")])]));
        string parent = JsonSerializer.Serialize(root);
        var result = RoomCombatModel.ApplyUnitPostCombat(root, [], []);
        Require(result.Supported && result.State!.Units.Single(unit => unit.Id == 1).Health == 15 &&
            result.State.Units.Single(unit => unit.Id == 2).Health == 7 && result.State.Units.All(unit => unit.MaxHealth == 15),
            "Post-combat phase grouped all healing before ordinary triggers or reversed team/actor order.");
        var blockedRoot = Room(Unit(1, CombatTeam.Player, [Trigger("PostCombatHealing", [Heal(999), gold], once: true, ignored: true),
            Trigger("PostCombat", [gold]), Trigger("PostCombat", [gold], ignored: true)]));
        var blocked = RoomCombatModel.ApplyUnitPostCombat(blockedRoot, [1], [1]);
        Require(blocked.Supported && blocked.State!.Units.Single().Health == 2 && blocked.State.Context!.Gold == 5 &&
            !blocked.State.Units.Single().Triggers[0].HasTriggered, "Healing prevention did not block ignored-silence effects or ordinary trigger gate differs.");
        var unable = RoomCombatModel.Resolve(blockedRoot);
        Require(unable.State!.Units.Single().Health == 10 && unable.State.Context!.Gold == 15,
            "An attack-incapable unit was prevented from healing.");
        var dazedRoot = Room(Unit(1, CombatTeam.Player, blockedRoot.Units[0].Triggers.ToArray(), [new("dazed", 1, removeAllAtEnd: true)]));
        var dazed = RoomCombatModel.Resolve(dazedRoot);
        Require(dazed.Supported && dazed.State!.Units.Single().Health == 2 && dazed.State.Context!.Gold == 5 &&
            dazed.State.Units.Single().Statuses.All(status => status.Id != "dazed") && !dazed.State.Units.Single().Triggers[0].HasTriggered,
            "Daze prevention vanished when the status cleared before post-combat.");
        var silentRoot = Room(Unit(1, CombatTeam.Player, [Trigger("PostCombatHealing", [Heal(5)]),
            Trigger("PostCombatHealing", [Heal(1)], ignored: true)], [new("silenced", 1)]));
        Require(RoomCombatModel.Resolve(silentRoot).State!.Units.Single().Health == 3, "Silence/ignored exception differs in healing phase.");
        var onceRoot = Room(Unit(1, CombatTeam.Player, [Trigger("PostCombatHealing", [Heal(1)], once: true)]));
        var first = RoomCombatModel.ApplyUnitPostCombat(onceRoot, [], []);
        Require(RoomCombatModel.ApplyUnitPostCombat(first.State!, [], []).State!.Units.Single().Health == 3, "Post-combat healing lost once-only state.");
        var deployed = new RoomCombatState(0, true, [Unit(1, CombatTeam.Player, [Trigger("PostCombatHealing", [Heal(1)], deployment: true)])], [], context);
        Require(RoomCombatModel.Resolve(deployed).State!.Units.Single().Health == 2, "Deployment gate was ignored for healing phase.");
        var repeated = Room(Unit(1, CombatTeam.Enemy, [], [new("relentless", 1)]),
            Unit(2, CombatTeam.Player, [Trigger("PostCombatHealing", [Heal(3)], once: true)], [new("dazed", 1, removeAllAtEnd: true)], attack: true));
        var relentless = RoomCombatModel.Resolve(repeated);
        Require(relentless.Supported && relentless.Rounds == 3 && relentless.State!.Units.Single().Health == 5,
            "Relentless kept prevention from an earlier exchange or repeated the final healing phase per round.");
        var preview = RoomCombatModel.ApplyUnitPostCombat(new(0, false, root.Units, [], context, preview: true), [], []);
        Require(preview.Supported && preview.State!.Units.Single(unit => unit.Id == 1).Health == 15 && preview.State.Context!.Gold == 0,
            "Post-combat preview lost healing/buffs or changed live gold.");
        var attackUpgrade = new CardUpgradeModifier("heal-scaling", "heal-scaling", new(damage: 4), [], false, false, false, 0, 0, []);
        var healer = Room(Unit(1, CombatTeam.Player, [Trigger("PostCombatHealing", [Heal(3), Heal(9)]), Trigger("PostCombatHealing", [Heal(2)])]));
        var upgraded = UnitModifierModel.Apply(healer, 1, attackUpgrade, "TemporaryUntilUnitDeath");
        var doubled = UnitModifierModel.Apply(upgraded.State!, 1, attackUpgrade, "TemporaryUntilUnitDeath");
        var removed = UnitModifierModel.Apply(doubled.State!, 1, attackUpgrade, "TemporaryUntilUnitDeath", remove: true);
        Require(upgraded.State!.Units.Single().Triggers[0].Effects[0].Action!.Value == 7 &&
            doubled.State!.Units.Single().Triggers[0].Effects[0].Value == 11 && removed.State!.Units.Single().Triggers[0].Effects[0].Value == 3 &&
            upgraded.State.Units.Single().Triggers[0].Effects[1].Value == 9 && upgraded.State.Units.Single().Triggers[1].Effects[0].Value == 2,
            "Attack upgrade/removal changed the wrong healing effect or lost continuous removal quantities.");
        var previewUpgrade = UnitModifierModel.Apply(new(0, false, healer.Units, [], context, preview: true), 1, attackUpgrade, "TemporaryUntilUnitDeath");
        Require(previewUpgrade.State!.Units.Single().Triggers[0].Effects[0].Value == 3, "Preview attack upgrade changed the live healing parameter.");
        var beforeHeal = new CombatEffect("CardEffectAddTempCardUpgradeToUnits", 0, 0, "", 0, [], false,
            unitUpgrade: new("UnitUpgrade", "Self", 0, false, true, [], attackUpgrade, "TemporaryUntilUnitDeath"));
        var chained = RoomCombatModel.ApplyUnitPostCombat(Room(Unit(1, CombatTeam.Player, [Trigger("PostCombatHealing", [beforeHeal, Heal(3)])])), [], []);
        Require(chained.State!.Units.Single().Health == 9 && chained.State.Units.Single().Triggers[0].Effects[1].Value == 7,
            "A healing sequence ignored its own earlier upgrade or restored a stale effect descriptor.");
        var negativeDamage = new CardUpgradeModifier("negative", "negative", new(damage: -10), [], false, false, false, 0, 0, []);
        var lowered = UnitModifierModel.Apply(healer, 1, negativeDamage, "TemporaryUntilUnitDeath");
        var raised = UnitModifierModel.Apply(lowered.State!, 1, negativeDamage, "TemporaryUntilUnitDeath", remove: true);
        Require(lowered.State!.Units.Single().Triggers[0].Effects[0].Value == 0 && raised.State!.Units.Single().Triggers[0].Effects[0].Value == 10,
            "Healing parameter clamping incorrectly restored the pre-clamp amount on removal.");
        var ranged = new CombatEffect("CardEffectHeal", 0, 0, "", 0, [], false, action: new("Heal", "Self", 0, false, true, [], range: new(2, 7, .5f)));
        var rangedUpgrade = UnitModifierModel.Apply(Room(Unit(1, CombatTeam.Player, [Trigger("PostCombatHealing", [ranged])])), 1, attackUpgrade, "TemporaryUntilUnitDeath");
        CombatEffect changedRange = rangedUpgrade.State!.Units.Single().Triggers[0].Effects[0];
        Require(changedRange.Value == 4 && changedRange.Action!.Range!.Min == 2 && changedRange.Action.Range.Max == 7,
            "Healer attack upgrade changed range endpoints instead of its unused scalar.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(RoomCombatModel.ApplyUnitPostCombat(root, [], [])) == JsonSerializer.Serialize(result) &&
            JsonSerializer.Serialize(RoomCombatModel.Resolve(dazedRoot)) == JsonSerializer.Serialize(dazed), "Parallel post-combat healing diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Post-combat healing changed its parent.");
        Console.WriteLine("POST-COMBAT-HEALING-CHECKS PASS: per-actor/team order, healing vs trigger prevention, daze after status clearing, incapable/silenced units, once/deployment/relentless/preview and 32 parallel branches.");
    }
    internal static void Native(JsonElement fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out JsonElement scenario) || scenario.GetString() != "post-combat-healing") return;
        int phases = 0, blocked = 0, healed = 0, silenced = 0, ordinary = 0, once = 0;
        foreach (JsonElement phase in fixture.GetProperty("UnitPostCombats").EnumerateArray())
        {
            var before = phase.GetProperty("Before").Deserialize<RoomCombatState>(ModelJson.Options)!;
            var after = phase.GetProperty("Actual").Deserialize<RoomCombatState>(ModelJson.Options)!;
            int[] cannotHeal = phase.GetProperty("CannotAttackOrHeal").Deserialize<int[]>()!;
            int[] cannotTrigger = phase.GetProperty("CannotFireTriggers").Deserialize<int[]>()!;
            var result = RoomCombatModel.ApplyUnitPostCombat(before, cannotHeal, cannotTrigger);
            Require(result.Supported && phase.GetProperty("Difference").ValueKind == JsonValueKind.Null &&
                ModelJson.Difference(JsonSerializer.Serialize(result.State), JsonSerializer.Serialize(after)) == null,
                "Independent native unit post-combat phase differs.");
            foreach (int id in cannotHeal)
            {
                CombatUnit unit = before.Units.Single(unit => unit.Id == id);
                CombatUnit? next = after.Units.FirstOrDefault(unit => unit.Id == id);
                if (next == null) continue;
                for (int index = 0; index < unit.Triggers.Count; index++)
                    if (unit.Triggers[index].Kind == "PostCombatHealing")
                        Require(unit.Triggers[index].HasTriggered == next.Triggers[index].HasTriggered,
                            "Native prevented healing consumed a trigger's once flag.");
                if (unit.Triggers.Any(trigger => trigger.Kind == "PostCombatHealing" && trigger.IgnoreSilence)) blocked++;
            }
            phases++; silenced += before.Units.Count(unit => unit.Statuses.Any(status => status.Id == "silenced") &&
                unit.Triggers.Any(trigger => trigger.Kind == "PostCombatHealing" && trigger.IgnoreSilence));
            once += after.Units.Sum(unit => unit.Triggers.Count(trigger => trigger.Kind == "PostCombatHealing" && trigger.Once && trigger.HasTriggered));
        }
        foreach (JsonElement heal in fixture.GetProperty("TriggeredHeals").EnumerateArray().Where(heal => heal.GetProperty("TriggerKind").GetString() == "PostCombatHealing"))
        {
            Require(heal.GetProperty("Completed").GetBoolean() && heal.GetProperty("Difference").ValueKind == JsonValueKind.Null, "Native post-combat heal is incomplete.");
            healed++;
        }
        int inSequence = 0;
        foreach (JsonElement sample in fixture.GetProperty("UnitUpgradeScaling").EnumerateArray())
        {
            string? kind = sample.GetProperty("TriggerKind").GetString();
            if (kind is not ("PostCombat" or "PostCombatHealing")) continue;
            Require(sample.GetProperty("Difference").ValueKind == JsonValueKind.Null && sample.GetProperty("CaptureError").ValueKind == JsonValueKind.Null,
                "Native post-combat upgrade callback is incomplete.");
            var before = sample.GetProperty("Before").Deserialize<CombatContext>(ModelJson.Options)!;
            var actualContext = sample.GetProperty("After").Deserialize<CombatContext>(ModelJson.Options)!;
            var trait = sample.GetProperty("Trait").Deserialize<ScalingUnitUpgradeTrait>(ModelJson.Options)!;
            var upgrade = sample.GetProperty("BeforeUpgrade").Deserialize<CardUpgradeModifier>(ModelJson.Options)!;
            var actualUpgrade = sample.GetProperty("AfterUpgrade").Deserialize<CardUpgradeModifier>(ModelJson.Options)!;
            var result = UnitUpgradeScalingModel.ApplyTrait(before, trait, sample.GetProperty("OwnerCardId").GetInt32(), upgrade, kind);
            Require(result.Supported && ModelJson.Difference(JsonSerializer.Serialize(result.Context), JsonSerializer.Serialize(actualContext)) == null &&
                ModelJson.Difference(JsonSerializer.Serialize(result.Upgrade), JsonSerializer.Serialize(actualUpgrade)) == null, "Native post-combat upgrade scaling differs.");
            if (kind == "PostCombat") ordinary++; else inSequence++;
        }
        Require(phases > 0 && blocked > 0 && healed > 0 && silenced > 0 && once > 0 && ordinary > 0 && inSequence > 0, "Native post-combat healing coverage is incomplete.");
        Console.WriteLine($"POST-COMBAT-HEALING-NATIVE PASS: exact phases={phases}, prevented-ignored={blocked}, healing effects={healed}, silenced={silenced}, once-flags={once}, ordinary upgrade callbacks={ordinary}, in-sequence upgrades={inSequence}.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
