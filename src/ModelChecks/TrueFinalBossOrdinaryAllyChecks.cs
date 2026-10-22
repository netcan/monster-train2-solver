using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TrueFinalBossOrdinaryAllyChecks
{
    private static void Require(bool value, string reason) => TrueFinalBossChecks.Require(value, reason);
    private static string Json(object value) => JsonSerializer.Serialize(value, ModelJson.Options);
    internal static void Native(FixtureValue root)
    {
        FixtureValue route = root.GetProperty("OriginalTrueFinalBossRoute");
        Require(route.GetProperty("Scenario").GetString() == "Level9TrueFinalBoss_V2" &&
            route.GetProperty("ScenarioAssetsUnchanged").GetBoolean() && route.GetProperty("CharacterAssetsUnchanged").GetBoolean() &&
            !route.GetProperty("StartingDeckUnchanged").GetBoolean() && route.GetProperty("PyreHealth").GetInt32() == 80 &&
            root.GetProperty("Policy").GetString() == "units-and-junk" && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            !root.GetProperty("NativeWon").GetBoolean(), "Missing original TFB route with explicit added-card control.");
        FixtureValue control = root.GetProperty("TrueFinalBossOrdinaryAllyControl");
        string kind = control.GetProperty("Kind").GetString()!;
        string upgradeKind = control.TryGetProperty("UpgradeKind", out FixtureValue upgradeValue) ? upgradeValue.GetString()! : "";
        Require(kind is "Steward" or "Warrior" or "Animus" or "Avenger", "Unknown ordinary allied signature.");
        Require(kind != "Avenger" || upgradeKind.Length == 0, "Triggered ally upgrades require separate calibration.");
        Require(upgradeKind is "" or "Health" or "Damage" or "Both", "Unknown ordinary numeric upgrade combination.");
        int cardId = control.GetProperty("CardId").GetInt32();
        int anchorId = control.GetProperty("AnchorCardId").GetInt32();
        BattleTurnState prepared = control.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleTurnState before = control.GetProperty("Before").Deserialize<BattleTurnState>()!;
        CardPlayRule definition = control.GetProperty("Definition").Deserialize<CardPlayRule>()!;
        Require(definition.DataId == control.GetProperty("CardDataId").GetString() && definition.SpawnUnit != null &&
            definition.Effect == "SpawnMonster" && prepared.Spawn.Turn == 0 && prepared.Spawn.QueuedTrueFinalBoss != null &&
            prepared.Energy == before.Energy && prepared.Energy == 4 && prepared.Spawn.Train.Rooms.SelectMany(room => room.Units).Single() is { IsPyre: true, Health: 80 } &&
            prepared.Spawn.Train.Context!.Cards.Hand.Count == before.Spawn.Train.Context!.Cards.Hand.Count + 1 &&
            prepared.Spawn.Train.Context.FindCard(cardId)!.DataId == definition.DataId,
            "Missing an original unit card added before the unchanged deployment boundary.");
        FixtureValue actions = root.GetProperty("Actions");
        FixtureValue turns = root.GetProperty("Turns");
        Require(actions.GetArrayLength() == 2 && turns.GetArrayLength() == 6, "Expected two paid original allies and six EndTurns.");
        BattleTurnState deployed = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        CombatUnit ally = deployed.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == cardId);
        bool damageUpgrade = upgradeKind is "Damage" or "Both";
        bool healthUpgrade = upgradeKind is "Health" or "Both";
        int attack = (kind == "Steward" ? 14 : kind == "Warrior" ? 15 : kind == "Avenger" ? 20 : 3) + (damageUpgrade ? 10 : 0);
        int hits = kind is "Steward" or "Avenger" ? 1 : kind == "Warrior" ? 2 : 3;
        Require(ally.BaseAttack == attack && ally.MaxHealth == (kind == "Steward" ? 5 : kind == "Avenger" ? 40 : 8) + (healthUpgrade ? 20 : 0) && ally.Size == 2 &&
            ally.AssetKey == definition.SpawnUnit!.AssetKey && TrueFinalBossCombatModel.SupportsAlly(ally, deployed.Spawn.Train.Context) &&
            !TrueFinalBossCombatModel.SupportsAlly(ally), "Missing calibrated ordinary attack signature and original source definition.");
        CardInstanceState original = deployed.Spawn.Train.Context!.FindCard(cardId)!;
        Require(original.Permanent.Upgrades.Count == (healthUpgrade ? 1 : 0) + (damageUpgrade ? 1 : 0) &&
            original.Permanent.Upgrades.Count(upgrade => upgrade.DataId == "32c1b067-d47d-4841-b166-1a227378a937" && upgrade.Stats.Health == 20) == (healthUpgrade ? 1 : 0) &&
            original.Permanent.Upgrades.Count(upgrade => upgrade.DataId == "1f0a08f2-ab63-442e-a089-a1063bf7e7aa" && upgrade.Stats.Damage == 10) == (damageUpgrade ? 1 : 0),
            "Expected the explicit original permanent numeric upgrades.");
        if (original.Permanent.Upgrades.Count > 0)
        {
            CombatContext absent = deployed.Spawn.Train.Context.WithCard(original.WithModifiers(CardModifiers.Empty(), original.Temporary));
            Require(!TrueFinalBossCombatModel.SupportsAlly(ally, absent), "Numeric ally stats require matching source upgrade provenance.");
            JsonNode triggered = JsonNode.Parse(Json(original))!;
            triggered["Permanent"]!["Upgrades"]![0]!["Lifecycle"]!["Triggers"] = JsonNode.Parse(
                "[{\"Type\":\"OnCast\",\"Effects\":[],\"Actions\":[],\"ExternalInteractions\":[],\"Id\":\"uncalibrated\"}]");
            CombatContext callbacks = deployed.Spawn.Train.Context.WithCard(triggered.Deserialize<CardInstanceState>(ModelJson.Options)!);
            Require(!TrueFinalBossCombatModel.SupportsAlly(ally, callbacks), "Numeric upgrades with card callbacks must retain their guard.");
            JsonNode temporary = JsonNode.Parse(Json(original))!;
            temporary["Temporary"]!["Upgrades"] = temporary["Permanent"]!["Upgrades"]!.DeepClone();
            temporary["Permanent"]!["Upgrades"] = new JsonArray();
            CombatContext transient = deployed.Spawn.Train.Context.WithCard(temporary.Deserialize<CardInstanceState>(ModelJson.Options)!);
            Require(!TrueFinalBossCombatModel.SupportsAlly(ally, transient), "Temporary upgrades need separate native calibration.");
        }
        var extra = new CardUpgradeModifier("unverified-upgrade", "unverified-upgrade", new CardStatModifier(), [], false, false, false, 0, 0, []);
        CombatContext modified = deployed.Spawn.Train.Context.WithCard(original.WithPermanentUpgrade(extra, false));
        Require(!TrueFinalBossCombatModel.SupportsAlly(ally, modified), "Ordinary signatures must still reject an unverified upgrade.");
        JsonNode missingHealth = JsonNode.Parse(Json(original))!;
        missingHealth["MaskDescriptor"]!["Definition"]!["UnmodifiedSpawnHealth"] = null;
        CombatContext legacy = deployed.Spawn.Train.Context.WithCard(missingHealth.Deserialize<CardInstanceState>(ModelJson.Options)!);
        Require(!TrueFinalBossCombatModel.SupportsAlly(ally, legacy), "Missing original spawn HP metadata must retain the ordinary-ally guard.");
        Require((ally.Status("multistrike")?.Stacks ?? 0) == hits - 1, "Missing original native repeated-attack status.");
        if (kind == "Avenger") VerifyOnHit(root, ally, deployed.Spawn.Train.Context!);
        LifeLinkContext health = turns[1].GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.LifeLinks!;
        Require(health.Actors.Any(actor => !actor.OuterBoss && actor.Health == 2750 - hits * attack),
            "Ordinary native repeated attacks must reduce Savagery by the complete damage.");
        string parent = Json(prepared);
        void Replay()
        {
            BattleTurnState current = prepared;
            int index = 0;
            foreach (FixtureValue entry in actions.EnumerateArray())
            {
                PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
                Require(action.CardInstanceId == (index == 0 ? anchorId : cardId) && action.RoomIndex == 0 && action.PlayerPosition == index,
                    "Expected unchanged Steward in front and selected ordinary ally behind it.");
                Require(BattleTurnChecks.Comparable(current) == BattleTurnChecks.Comparable(entry.GetProperty("Before").Deserialize<BattleTurnState>()!),
                    "Independent ordinary-ally input differs at play " + index);
                BattleActionResult played = BattleActionModel.PlayCard(current, action);
                Require(played.Supported, "Independent ordinary summon rejected: " + played.Reason);
                current = played.State!;
                Require(BattleTurnChecks.Comparable(current) == BattleTurnChecks.Comparable(entry.GetProperty("Actual").Deserialize<BattleTurnState>()!),
                    "Independent ordinary-ally successor differs at play " + index);
                index++;
            }
            BattleTurnChecks.RunIndependentChain(current, turns, compareRoomEnabled: true);
        }
        Replay();
        Parallel.For(0, 16, _ => Replay());
        Require(Json(prepared) == parent, "Independent ordinary-ally chains mutated their prepared root.");
        TrueFinalBossAllyUpgradeChecks.Previews(root);
        Require(new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => root.GetProperty(key).GetInt32() == 0),
            "Native ordinary-ally acceptance requires all four failure counters zero.");
        Console.WriteLine("TRUE-FINAL-BOSS-ORDINARY-ALLY PASS kind=" + kind + "; upgrade=" + upgradeKind +
            (kind == "Avenger" ? "; original OnHit melee-weakness signature" : "; original inert summon/attack signature") + ", native damage=" + hits * attack +
            "; two paid allies, six EndTurns, five UI previews and 16 independent immutable full chains.");
    }

    private static void VerifyOnHit(FixtureValue root, CombatUnit ally, CombatContext context)
    {
        int witnesses = 0;
        foreach (FixtureValue row in root.GetProperty("UnitTurns").EnumerateArray())
        {
            Require(row.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                row.GetProperty("Predicted").GetProperty("Supported").GetBoolean(), "Native triggered-ally unit turn was not matched.");
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            int actorId = row.GetProperty("ActorId").GetInt32();
            CombatUnit? actor = before.Units.SingleOrDefault(unit => unit.Id == actorId);
            CombatUnit? after = actual.Units.SingleOrDefault(unit => unit.Id == actorId);
            if (actor?.Status("lifelink") == null || after == null ||
                !row.GetProperty("AttackedTargetIds").EnumerateArray().Any(target => target.GetInt32() == ally.Id) ||
                (after.Status("melee weakness")?.Stacks ?? 0) <= (actor.Status("melee weakness")?.Stacks ?? 0)) continue;
            RoomCombatResult result = RoomCombatModel.ApplyUnitTurn(before, actorId);
            string? difference = result.Supported ? ModelJson.Difference(Json(result.State!), Json(actual)) : result.UnsupportedReason;
            Require(difference == null, "Original linked attacker/OnHit callback differs: " + difference);
            if (witnesses == 0)
            {
                JsonNode terminal = JsonNode.Parse(Json(before))!;
                foreach (JsonNode? link in terminal["Context"]!["LifeLinks"]!["Actors"]!.AsArray())
                {
                    link!["Health"] = 1;
                    link["PreviewHealth"] = 1;
                }
                void ReduceBodies(JsonArray units)
                {
                    foreach (JsonNode? unit in units)
                        if (unit!["Team"]!.GetValue<int>() == (int)CombatTeam.Enemy) unit["Health"] = 1;
                }
                ReduceBodies(terminal["Units"]!.AsArray());
                foreach (JsonNode? floor in terminal["Context"]!["Enchantments"]!["Rooms"]!.AsArray())
                    ReduceBodies(floor!["Units"]!.AsArray());
                RoomCombatState lethal = terminal.Deserialize<RoomCombatState>(ModelJson.Options)!;
                Require(TrueFinalBossCombatModel.Supports(lethal) &&
                    !TrueFinalBossTerminalCombatModel.SupportsRoot(lethal, actorId),
                    "Calibrated nonterminal OnHit must not bypass the uncalibrated terminal group-death guard.");
            }
            witnesses++;
        }
        Require(witnesses > 0, "No native OnHit callback applied melee weakness to a linked attacker.");
        void Reject(Action<JsonNode> mutation)
        {
            JsonNode changed = JsonNode.Parse(Json(ally))!;
            mutation(changed["Triggers"]![0]!);
            Require(!TrueFinalBossCombatModel.SupportsAlly(changed.Deserialize<CombatUnit>(ModelJson.Options)!, context),
                "An uncalibrated OnHit signature bypassed the true-final-boss guard.");
        }
        Reject(trigger => trigger["Once"] = true);
        Reject(trigger => trigger["Kind"] = "OnAttack");
        Reject(trigger => trigger["FireCount"] = 2);
        Reject(trigger => trigger["IgnoreSilence"] = true);
        Reject(trigger => trigger["NoCountModifiersAllowed"] = true);
        Reject(trigger => trigger["Origin"]!["IsFromEquipment"] = true);
        Reject(trigger => trigger["Effects"]![0]!["Action"]!["Target"] = "Self");
        Reject(trigger => trigger["Effects"]![0]!["Action"]!["Statuses"]![0]!["Stacks"] = 2);
        Reject(trigger => trigger["Effects"]![0]!["StatusScaling"]!["MagicPower"] = true);
        Reject(trigger => trigger["Effects"]!.AsArray().Add(trigger["Effects"]![0]!.DeepClone()));
        JsonNode valor = JsonNode.Parse(Json(ally))!;
        valor["Statuses"]!.AsArray().Add(JsonNode.Parse("{\"Id\":\"valor\",\"Stacks\":5,\"ParamInt\":1}"));
        Require(!TrueFinalBossCombatModel.SupportsAlly(valor.Deserialize<CombatUnit>(ModelJson.Options)!, context),
            "Triggered ally plus commanded Valor needs separate native calibration.");
        Console.WriteLine($"TRUE-FINAL-BOSS-ON-HIT PASS witnesses={witnesses}: original linked attacker, full unit-turn/context/RNG, ten signature counterexamples, and terminal/Valor guards.");
    }
}
