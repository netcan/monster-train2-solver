using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSupportUnitsChecks
{
    private const string AstrologerId = "4b50aa70-8918-40f0-8efd-c66ce78bbf52";
    private const string BloatedWhelpId = "32dad9d3-f839-4077-8f73-9837d398df7b";
    private const string FaeMeddlerId = "d10e2dac-fa58-4509-b0ee-112b3bf78800";
    private const string SnottyWhelpId = "eef8abd9-3550-4da6-a8bd-d5f63e9d2b16";
    private const string BlightedTotemId = "21ad4f07-1568-4461-8be4-6fb2670bd90d";
    private const string ProsperityTotemId = "21567cc4-f684-48db-b377-b8289729dedc";
    private const string BattleDancerId = "a0b051b2-7898-454a-8bd8-300b72b99c93";
    private const string HeroInTrainingId = "57ffb1c3-0728-4254-ab35-7e34adab459f";
    private const string DivineDancerId = "a1e9a37b-504a-4a04-99e2-6d933727b427";
    private const string BrandedWarriorId = "276fdd51-4bb8-4810-9bbf-cb663522b70f";
    private const string ColdstrikeSirenId = "ff11e59c-d696-4aea-b626-432574508f40";
    private const string EelGorgonId = "eaaae54e-58ed-4926-b5bb-a0b36404c4b7";
    private const string EntombedExplosiveId = "9a8943a3-d6a6-454b-91d4-d6d08dd6fd81";
    private const string OverworkedAssistantId = "f4a5935a-ea84-41b8-944f-7d08cd228a58";
    private const string CrucibleCollectorId = "e27358a6-e42f-410b-be76-75dfbf2bfab4";
    private const string CrucibleWardenId = "abae9cd1-d180-41cb-956c-5cf09cf85fa9";
    private const string MakeshiftGolemId = "92084a94-6cc6-4af3-9a7d-de61ec7d161c";
    private const string DeathcappedProphetId = "ec522dfa-0b0d-41a9-860d-1a18fae0efb6";
    private const string ParrafinEnforcerId = "b9788a77-50da-4654-acfa-960c23649984";
    private const string SycophantId = "6d2053e9-1a6a-4862-b348-76a92a92ea83";
    private const string VotivaryId = "e9bb0dfe-ecf7-4046-be6d-f6f00893662f";
    private const string BountyStalkerId = "3e86422e-c8e4-4ec7-bc0a-0c622b7237b4";
    private const string PlagueDoctorId = "34909c50-4cba-41fa-8b33-64c1e463691c";
    private const string HulkingMassId = "a0c52fd5-e77f-43ec-b9f7-2ebf7e3ac8ac";
    private const string ReformingOozeId = "357a72e3-935c-4946-9e5a-6449482641c1";
    private const string MoltenEncasementId = "b20aaea2-cf9e-4475-aac3-abba7a0bed7e";
    private const string ShatteredShellId = "57985d47-d8ac-46be-8545-20a51209fc4f";
    private const string WeldlingId = "acb9a1cb-f6fc-40df-b287-fc82b8df156b";
    private const string HostguardId = "c682012e-b5fc-4f75-9cb4-f6ade997b72f";
    private const string RubbleRaiserId = "e2ed318d-754c-41b8-8df6-0a6af221e54e";
    private const string JuggernautId = "952db29f-b585-41be-bfdd-0a36b9b05010";
    private const string HoardAcolyteId = "3224dfe8-41f9-49b9-8917-3afefb67e9dd";
    private const string NightingaleId = "7775c5b5-eed9-4864-9976-9c86896becc1";
    private const string HotHeadId = "ee2a1d34-f1de-43ac-a06b-f3a414c93363";
    private const string SteadfastCrusaderId = "075a061c-480c-4873-8f9f-88cf836eec27";
    private const string TrufflesId = "9a63e469-ae29-4199-ae41-4060ecd92853";
    private const string PyrebloodedId = "e962f77b-d5bc-441b-be79-8cb3207d8a42";
    private const string KinhostCarapaceId = "8fcb523c-690f-44cb-887d-7112795a50a1";
    private const string MeathookId = "6c070dfb-65b7-4955-a651-999990fd0c90";
    private const string RailgunId = "9b8f9080-0001-4994-bf1f-cf8bb247222f";
    private const string SilentSentinelId = "4f0f249f-2a0e-4704-b8e2-8395505e6bdb";
    private const string SpikedriverFactoryId = "acf2b858-7dc9-492f-b01c-cbb8278fd43a";
    private const string ScrivenerId = "c2baee0e-4bc6-4da6-8330-71f5a299133e";
    private const string BanishedChamp2Id = "27340034-9ee7-4535-bb63-2e0bd8c321fb";
    private const string PyreborneChamp1Id = "7488b439-316c-4a13-8897-90de239b6250";
    private const string DeathEaterId = "68a76ecf-2108-4b7f-b805-b3653cdd96bf";
    private const string LunaCovenChamp1Id = "5d2b15ed-e9b4-464a-b029-f337f2ed6cd2";
    private const string LunaCovenChamp2Id = "63eba1e1-3118-44a3-8906-c8bfbe559667";
    private const string SporelauncherId = "aa20f108-ca78-42b1-b949-55fad0f84b15";
    private const string ConductorStewardId = "52b17dc7-968f-4748-a2ea-b6c09c5c8931";
    private const string EnchantrelleId = "658321b4-08bd-443c-b4e1-b8a8931d3299";
    private const string FeederDamageEnemiesUnitId = "aa94ef0c-0fd8-4d42-992f-25345d3717b5";
    private const string LadyOfTheReformedId = "5ac22130-17f1-4f8e-967f-a2d3b71ef7ca";
    private const string FeederPermanentAttackBoostId = "ed072114-b62e-4f61-8892-adf775d5a34c";
    private const string ZealotId = "071d7448-0be6-4a40-860c-ede856a42228";
    private const string ParrafinThugId = "a2bc0531-4037-418b-a30b-9189cc580adf";
    private const string MonstrosityId = "5336f488-1e09-40a5-97bc-5a5fde3c5e55";
    private const string WicklessBaronId = "4cf9eb1d-b852-4514-8f32-831bc38276ec";
    private const string WicklessTycoonId = "918f8345-8660-49de-a764-d16e76343abc";
    private static readonly string[] Originals = ["4aa37169-c5cf-4a5b-b927-dc31b05f6e24", "82e21764-56cb-4e41-882f-ab3dd0c1dcbc",
        "c8b428ac-a1e9-4e52-96e0-5eed522195a2", "0c68b12a-0779-4c3c-bf64-6bc5bcf83e9f", "8c404be7-7179-461c-8e7d-b5fdb296174c",
        "d76be2d0-f851-4d43-bdc0-7c7a7779724f", "600ef4bf-59ec-45b6-a3e7-273cbb43c7bb", "85bddcfb-8e7a-4797-a209-c7d62f40ecc7",
        "61d9f4f1-80f5-416b-bf22-1836925c8ccd", "5a8399dd-a958-4317-b814-de0e2972ca3d", "a736e605-a4f4-4cc1-8eba-63cb0cf3515e",
        "64a3fd58-b7b3-466a-be8d-78bb2b9bdea4", "d653e83e-3f70-419d-b3f9-4b920d5c9e56", "dcfdf8cc-0b0b-41e2-97a2-383fe303e617",
        "69fcbe85-a5b7-4a83-bf6d-21888604d99e"];
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
        if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
        if (ids.Length >= 4)
        {
            int roomIndex = index < 3 ? index : 0;
            int isolatedPosition = index < 3 ? 0 : source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Count(unit => unit.Team == CombatTeam.Player);
            var isolatedAction = new PlayCardAction(ids[index], roomIndex, playerPosition: isolatedPosition);
            return BattleActionModel.PlayCard(source, isolatedAction).Supported ? isolatedAction : null;
        }
        int room = index < 3 ? index : index == 5 || index is >= 14 and <= 18 || index is 35 or 36 ? 1 :
            index is 6 or 19 or 20 or 21 or 22 or 30 or 31 or 32 or 33 or 34 ? 2 : 0;
        int position = index < 3 ? 0 : index is >= 8 and <= 13 ? index - 7 : index is >= 14 and <= 18 ? index - 13 : index is >= 19 and <= 22 ? index - 18 :
            index is 32 or 33 or 34 or 35 or 36 ? source.Spawn.Train.Rooms.Single(currentRoom => currentRoom.RoomIndex == room).Units.Count(unit => unit.Team == CombatTeam.Player) : -1;
        int host = index is 23 or 24 ? 11 : index is 25 or 26 ? 12 : index == 27 ? 9 : index == 28 ? 8 : index == 29 ? 10 : index == 31 ? 21 : 19;
        int target = index >= 23 && index != 32 && index != 33 && index != 34 && index != 35 && index != 36
            ? source.Spawn.Train.Rooms.SelectMany(floor => floor.Units).Single(unit => unit.SpawnerCardId == ids[host]).Id : 0;
        var action = new PlayCardAction(ids[index], room, playerPosition: position, targetUnitId: target);
        return BattleActionModel.PlayCard(source, action).Supported ? action : null;
    }
    internal static void Native(FixtureValue root)
    {
        if (root.GetProperty("ModifierScenario").GetString() == "original-divine-dancer")
        {
            NativeDivineDancer(root);
            return;
        }
        if (root.GetProperty("ModifierScenario").GetString() == "original-isolated-unit-batch")
        {
            NativeIsolatedUnit(root, BrandedWarriorId, "BrandedWarrior");
            return;
        }
        if (root.GetProperty("ModifierScenario").GetString() == "original-isolated-coldstrike-siren")
        {
            NativeIsolatedUnit(root, ColdstrikeSirenId);
            return;
        }
        string? isolatedUnitScenario = root.GetProperty("ModifierScenario").GetString();
        const string isolatedUnitPrefix = "original-isolated-unit:";
        const string isolatedUnitBatchPrefix = "original-isolated-unit-batch:";
        if (isolatedUnitScenario?.StartsWith(isolatedUnitPrefix, StringComparison.Ordinal) == true ||
            isolatedUnitScenario?.StartsWith(isolatedUnitBatchPrefix, StringComparison.Ordinal) == true)
        {
            string prefix = isolatedUnitScenario.StartsWith(isolatedUnitBatchPrefix, StringComparison.Ordinal) ? isolatedUnitBatchPrefix : isolatedUnitPrefix;
            string[] expectedIds = isolatedUnitScenario[prefix.Length..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Require(expectedIds.Length > 0 && expectedIds.All(expectedId => Guid.TryParseExact(expectedId, "D", out _)),
                "Isolated unit scenario did not identify original card UUIDs.");
            FixtureValue[] isolatedActions = root.GetProperty("Actions").EnumerateArray().ToArray();
            int energyControlActions = root.TryGetProperty("OriginalIsolatedEnergyControlId", out FixtureValue energyControlIdValue) &&
                energyControlIdValue.GetInt32() > 0 ? 1 : 0;
            Require(isolatedActions.Length == expectedIds.Length + 3 + energyControlActions,
                "Isolated unit scenario must record three anchors, optional energy control, and every requested original unit birth.");
            for (int index = 0; index < expectedIds.Length; index++)
                NativeIsolatedUnit(root, expectedIds[index], actionIndex: index + 3 + energyControlActions);
            return;
        }
        Require(root.GetProperty("OriginalSupportUnitsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original support unit witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalSupportUnitsIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 37 && actions.Length == 36, "Missing twenty original births or positive trigger controls.");
        BattleTurnState Before(int i) => actions[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int i) => actions[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Actor(BattleTurnState state, int index) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.SpawnerCardId == ids[index]);
        static int Stacks(CombatUnit unit, string id) => unit.Status(id)?.Stacks ?? 0;
        for (int i = 8; i <= 22; i++)
        {
            BattleTurnState before = Before(i), after = After(i);
            CardPlayRule rule = before.PlayRules!.Cards.Single(rule => rule.DataId == Originals[i - 8]);
            CombatUnit unit = Actor(after, i), template = rule.SpawnUnit!;
            Require(before.Spawn.Train.Context!.FindCard(ids[i])!.DataId == rule.DataId && rule.ExternalInteractions.Count == 0 &&
                before.Energy - after.Energy == rule.Cost - (i == 18 ? 4 : 0) && unit.Modifiers!.AttackDamage == template.Modifiers!.AttackDamage && unit.Size == template.Size,
                "Original support unit identity, paid cost, raw attack or size differs.");
            if (i is 11 or 12) Require(unit.Health == 25 && unit.MaxHealth == 85, "Hollow birth must add sixty maximum HP without healing current HP.");
            if (i == 21)
            {
                CombatTrigger onDeath = template.Triggers.Single(trigger => trigger.Kind == "OnDeath");
                PlayCardAction action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
                Require(action.RoomIndex == 2 && action.PlayerPosition == 3 &&
                    rule.AssetKey == "AlchemicalAssistant" && unit.AssetKey == "MonsterAlchemicalAssistant" &&
                    unit.Health == 30 && unit.MaxHealth == 30 && unit.Modifiers.AttackDamage == 10 && unit.Size == 2 &&
                    onDeath.Effects.Single().Type == "CardEffectAdjustDragonsHoard" && onDeath.Effects.Single().Value == 1,
                    "Alchemical Assistant must preserve its original 10/30 spawn and untriggered Dragon's Hoard OnDeath definition.");
            }
            if (i == 22)
            {
                CombatTrigger onHit = template.Triggers.Single(trigger => trigger.Kind == "OnHit");
                CardActionEffect effect = onHit.Effects.Single().Action!;
                PlayCardAction action = actions[i].GetProperty("Action").Deserialize<PlayCardAction>()!;
                Require(action.RoomIndex == 2 && action.PlayerPosition == 4 && rule.AssetKey == "Avenger" &&
                    unit.AssetKey == "MonsterAvenger" && unit.Health == 40 && unit.MaxHealth == 40 &&
                    unit.Modifiers.AttackDamage == 20 && unit.Size == 2 && !onHit.HasTriggered &&
                    effect.Type == "AddStatus" && effect.Target == "LastAttackerCharacter" &&
                    effect.Statuses.Single().Id == "melee weakness" && effect.Statuses.Single().Stacks == 1,
                    "Avenger must preserve its original 20/40 spawn and captured but untriggered LastAttacker Melee Weakness callback.");
            }
        }
        Require(Stacks(Actor(After(16), 1), "armor") - Stacks(Actor(Before(16), 1), "armor") == 20,
            "Welder Helper must positively armor the existing front ally.");
        Require(Before(17).Spawn.Train.Rooms[1].Units.Where(unit => unit.Team == CombatTeam.Player).All(unit =>
            Stacks(After(17).Spawn.Train.Rooms[1].Units.Single(changed => changed.Id == unit.Id), "buff") - Stacks(unit, "buff") == 5),
            "Aggressive Imp must positively give the room five Rage.");
        foreach (int i in new[] { 16, 17, 18 })
            Require(Before(i).Spawn.Train.Rooms[1].Units.Where(unit => unit.Team == CombatTeam.Player).All(unit =>
                Stacks(After(i).Spawn.Train.Rooms[1].Units.Single(changed => changed.Id == unit.Id), "regen") - Stacks(unit, "regen") == 1 &&
                After(i).Spawn.Train.Rooms[1].Units.Single(changed => changed.Id == unit.Id).MaxHealth - unit.MaxHealth == 1),
                "Amorous Enoki must positively Rally with Regen and maximum HP on three later original units.");
        int starterImpDamage = Before(13).PlayRules!.Cards.Single(rule => rule.DataId == Originals[5]).SpawnUnit!.Triggers
            .Single(trigger => trigger.Kind == "OnSpawn").Effects.Single(effect => effect.Type == "CardEffectDamage").Value;
        CombatUnit starterImpVictimBefore = Before(13).Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack");
        CombatUnit starterImpVictimAfter = After(13).Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == starterImpVictimBefore.Id);
        Require(starterImpDamage > 0 && starterImpVictimBefore.Health == starterImpVictimAfter.Health &&
            Stacks(starterImpVictimBefore, "armor") - Stacks(starterImpVictimAfter, "armor") == starterImpDamage,
            "Starter Imp must positively route its captured spawn damage through front-unit armor.");
        CardCycleState drawBefore = Before(20).Spawn.Train.Context!.Cards, drawAfter = After(20).Spawn.Train.Context!.Cards;
        CombatEffect typedDraw = Before(20).PlayRules!.Cards.Single(rule => rule.DataId == Originals[12]).SpawnUnit!.Triggers
            .SelectMany(trigger => trigger.Effects).Single(effect => effect.Action?.Type == "DrawType");
        Require(typedDraw.Action!.TypedDraw?.CardType == "Equipment" && typedDraw.Action.Value == 1 &&
            drawBefore.Hand.Any(card => card.InstanceId == ids[30]) && !drawBefore.Draw.Concat(drawBefore.Discard).Any(card => card.InstanceId == ids[30]) &&
            drawAfter.Hand.Any(card => card.InstanceId == ids[30]) && !drawAfter.Draw.Concat(drawAfter.Discard).Any(card => card.InstanceId == ids[30]),
            "Swordmaiden's captured equipment draw must leave unmatched hand equipment untouched when no eligible card is in deck or discard.");
        Require(Actor(After(24), 11).Health - Actor(Before(24), 11).Health == 10 && Actor(After(24), 13).Modifiers!.AttackDamage - Actor(Before(24), 13).Modifiers!.AttackDamage == 3 &&
            Actor(After(24), 13).MaxHealth - Actor(Before(24), 13).MaxHealth == 3, "Awoken Hollow healing must buff the weakest current-HP ally by +3/+3.");
        Require(Actor(After(26), 12).Health - Actor(Before(26), 12).Health == 10 && Stacks(Actor(After(26), 12), "spikes") - Stacks(Actor(Before(26), 12), "spikes") == 4,
            "Thorned Hollow must positively gain four Spikes when healed.");
        Require(Actor(Before(27), 9).Health - Actor(After(27), 9).Health == 1 && Actor(After(27), 9).Modifiers!.AttackDamage - Actor(Before(27), 9).Modifiers!.AttackDamage == 3,
            "Husk Hermit must gain three raw attack from the selected hit.");
        Require(Actor(After(28), 8).Modifiers!.AttackDamage - Actor(Before(28), 8).Modifiers!.AttackDamage == 15,
            "Alpha Fiend must gain fifteen attack before its native forced attack.");
        CombatUnit railVictimBefore = Before(29).Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack"),
            railVictimAfter = After(29).Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == railVictimBefore.Id);
        Require(railVictimBefore.Health - railVictimAfter.Health == 10 && Stacks(railVictimBefore, "melee weakness") == 0 &&
            Stacks(railVictimAfter, "melee weakness") == 1,
            "Railbeater must land its native ten-damage hit and leave one Melee Weakness on the victim.");
        BattleTurnState equipmentBefore = Before(30), equipmentAfter = After(30);
        CardPlayRule equipment = equipmentBefore.PlayRules!.Cards.Single(rule => rule.DataId == "f1c2a098-e20d-4587-b8de-5bef315de42d");
        Require(Actor(equipmentAfter, 19).Modifiers!.AttackDamage - Actor(equipmentBefore, 19).Modifiers!.AttackDamage == equipment.Equipment!.Upgrades.Sum(upgrade => upgrade.Stats.Damage) + 3 &&
            Actor(equipmentAfter, 19).MaxHealth - Actor(equipmentBefore, 19).MaxHealth == 3 &&
            equipmentAfter.Spawn.Train.Context!.FindCard(ids[19])!.Permanent.Upgrades.Count - equipmentBefore.Spawn.Train.Context!.FindCard(ids[19])!.Permanent.Upgrades.Count == 1 &&
            Stacks(Actor(equipmentAfter, 20), "valor") - Stacks(Actor(equipmentBefore, 20), "valor") == 8,
            "Equipment must positively give Pincushion permanent +3/+3 and Swordmaiden eight Valor.");
        var phase = root.GetProperty("TrainPhases").EnumerateArray().First(item => item.GetProperty("Turn").GetInt32() == 1 && item.GetProperty("Kind").GetString() == "Combat");
        TrainCombatState combatBefore = phase.GetProperty("Before").Deserialize<TrainCombatState>()!, combatAfter = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        Require(combatAfter.Rooms[1].Units.Where(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId != ids[1]).All(unit =>
            Stacks(unit, "armor") - Stacks(combatBefore.Rooms[1].Units.Single(prior => prior.Id == unit.Id), "armor") == 6), "Steelworker needs positive postcombat room armor.");
        BattleTurnState alchemicalDeathBefore = Before(31), alchemicalDeathAfter = After(31);
        PlayCardAction alchemicalDeath = actions[31].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext hoardBefore = alchemicalDeathBefore.Spawn.Train.Context!, hoardAfter = alchemicalDeathAfter.Spawn.Train.Context!;
        int amount = hoardBefore.QueryFrame!.DragonsHoard!.Value, cap = hoardBefore.Resources!.DragonsHoardCap!.Value;
        Require(alchemicalDeath.RoomIndex == 2 && alchemicalDeath.TargetUnitId == Actor(alchemicalDeathBefore, 21).Id &&
            alchemicalDeathBefore.PlayRules!.Cards.Single(rule => rule.DataId == "ac110058-0000-4000-8000-000000000008").Effects.Single().Value == 30 &&
            !alchemicalDeathAfter.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == ids[21]) &&
            hoardBefore.EnergyState!.PyreAlive && cap == 4 && amount == 2 && hoardAfter.QueryFrame!.DragonsHoard == Math.Min(amount + 1, cap),
            "Alchemical Assistant must be killed by the captured 30-damage control and apply its native capped Dragon's Hoard death effect.");
        BattleTurnState totemBefore = Before(32), totemAfter = After(32);
        PlayCardAction totemAction = actions[32].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule totemRule = totemBefore.PlayRules!.Cards.Single(rule => rule.DataId == BlightedTotemId);
        CombatUnit totemTemplate = totemRule.SpawnUnit!, totem = Actor(totemAfter, 32);
        CombatTrigger spellTrigger = totemTemplate.Triggers.Single(trigger => trigger.Kind == "CardSpellPlayed");
        CombatTrigger actualSpellTrigger = totem.Triggers.Single(trigger => trigger.Kind == "CardSpellPlayed");
        CardActionEffect spellEffect = spellTrigger.Effects.Single().Action!;
        RoomCombatState totemRoom = totemBefore.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2);
        int appendPosition = totemRoom.Units.Count(unit => unit.Team == CombatTeam.Player);
        Require(totemBefore.Spawn.Train.Context!.FindCard(ids[32])!.DataId == BlightedTotemId &&
            totemAction.RoomIndex == 2 && totemAction.PlayerPosition == appendPosition && totemAction.TargetUnitId == 0 &&
            totemBefore.Energy - totemAfter.Energy == totemRule.Cost && totemRule.AssetKey == "BlightedTotem" &&
            totem.AssetKey == "MonsterBlightedTotem" && totem.Health == 1 && totem.MaxHealth == 1 &&
            totem.Modifiers!.AttackDamage == 0 && totem.Size == 1 && Stacks(totem, "poison") == 0 &&
            spellTrigger.Effects.Single().Type == "CardEffectAddStatusEffect" && spellEffect.Type == "AddStatus" &&
            spellEffect.Target == "Room" && spellEffect.Statuses.Single().Id == "poison" &&
            spellEffect.Statuses.Single().Stacks == 2 && !spellTrigger.HasTriggered && !actualSpellTrigger.HasTriggered,
            "Blighted Totem must preserve its original 0/1 size-1 spawn and captured, untriggered two-Poison CardSpellPlayed callback.");
        BattleTurnState prosperityBefore = Before(33), prosperityAfter = After(33);
        PlayCardAction prosperityAction = actions[33].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule prosperityRule = prosperityBefore.PlayRules!.Cards.Single(rule => rule.DataId == ProsperityTotemId);
        CombatUnit prosperityTemplate = prosperityRule.SpawnUnit!, prosperity = Actor(prosperityAfter, 33);
        CombatTrigger drawTrigger = prosperityTemplate.Triggers.Single(trigger => trigger.Kind == "CardSpellPlayed");
        CombatTrigger actualDrawTrigger = prosperity.Triggers.Single(trigger => trigger.Kind == "CardSpellPlayed");
        CardActionEffect drawEffect = drawTrigger.Effects.Single().Action!;
        RoomCombatState prosperityRoom = prosperityBefore.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2);
        int prosperityAppendPosition = prosperityRoom.Units.Count(unit => unit.Team == CombatTeam.Player);
        Require(prosperityBefore.Spawn.Train.Context!.FindCard(ids[33])!.DataId == ProsperityTotemId &&
            prosperityAction.RoomIndex == 2 && prosperityAction.PlayerPosition == prosperityAppendPosition && prosperityAction.TargetUnitId == 0 &&
            prosperityBefore.Energy - prosperityAfter.Energy == prosperityRule.Cost && prosperityRule.AssetKey == "ProsperityTotem" &&
            prosperity.AssetKey == "MonsterProsperityTotem" && prosperity.Health == 1 && prosperity.MaxHealth == 1 &&
            prosperity.Modifiers!.AttackDamage == 0 && prosperity.Size == 1 &&
            drawEffect.Type == "DrawNextTurn" && drawEffect.Target == "Room" &&
            !drawTrigger.HasTriggered && !actualDrawTrigger.HasTriggered,
            "Prosperity Totem must preserve its original 0/1 size-1 spawn and captured, untriggered CardSpellPlayed draw callback.");
        BattleTurnState dancerBefore = Before(34), dancerAfter = After(34);
        PlayCardAction dancerAction = actions[34].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule dancerRule = dancerBefore.PlayRules!.Cards.Single(rule => rule.DataId == BattleDancerId);
        CombatUnit dancerTemplate = dancerRule.SpawnUnit!, dancer = Actor(dancerAfter, 34);
        CombatTrigger shiftTrigger = dancerTemplate.Triggers.Single(trigger => trigger.Kind == "OnShift");
        CombatTrigger actualShiftTrigger = dancer.Triggers.Single(trigger => trigger.Kind == "OnShift");
        CardActionEffect valorEffect = shiftTrigger.Effects.Single().Action!;
        RoomCombatState dancerRoom = dancerBefore.Spawn.Train.Rooms.Single(room => room.RoomIndex == 2);
        int dancerAppendPosition = dancerRoom.Units.Count(unit => unit.Team == CombatTeam.Player);
        Require(dancerBefore.Spawn.Train.Context!.FindCard(ids[34])!.DataId == BattleDancerId &&
            dancerAction.RoomIndex == 2 && dancerAction.PlayerPosition == dancerAppendPosition && dancerAction.TargetUnitId == 0 &&
            dancerBefore.Energy - dancerAfter.Energy == dancerRule.Cost && dancerRule.AssetKey == "BattleDancer" &&
            dancer.AssetKey == "MonsterBattleDancer" && dancer.Health == 14 && dancer.MaxHealth == 14 &&
            dancer.Modifiers!.AttackDamage == 7 && dancer.Size == 2 && Stacks(dancer, "valor") == 0 &&
            valorEffect.Type == "AddStatus" && valorEffect.Target == "Self" &&
            valorEffect.Statuses.Single().Id == "valor" && valorEffect.Statuses.Single().Stacks == 3 &&
            !shiftTrigger.HasTriggered && !actualShiftTrigger.HasTriggered,
            "Battle Dancer must preserve its original 7/14 size-2 spawn and captured, untriggered three-Valor OnShift callback.");
        BattleTurnState heroBefore = Before(35), heroAfter = After(35);
        PlayCardAction heroAction = actions[35].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule heroRule = heroBefore.PlayRules!.Cards.Single(rule => rule.DataId == HeroInTrainingId);
        CombatUnit heroTemplate = heroRule.SpawnUnit!, hero = Actor(heroAfter, 35);
        CombatTrigger heroShift = heroTemplate.Triggers.Single(trigger => trigger.Kind == "OnShift");
        CombatTrigger actualHeroShift = hero.Triggers.Single(trigger => trigger.Kind == "OnShift");
        CardActionEffect buffEffect = heroShift.Effects.Single().Action!;
        RoomCombatState heroRoom = heroBefore.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1);
        int heroAppendPosition = heroRoom.Units.Count(unit => unit.Team == CombatTeam.Player);
        Require(heroBefore.Spawn.Train.Context!.FindCard(ids[35])!.DataId == HeroInTrainingId &&
            heroAction.RoomIndex == 1 && heroAction.PlayerPosition == heroAppendPosition && heroAction.TargetUnitId == 0 &&
            heroBefore.Energy - heroAfter.Energy == heroRule.Cost && heroRule.AssetKey == "HeroInTraining" &&
            heroTemplate.Health == 10 && heroTemplate.MaxHealth == 10 && heroTemplate.Modifiers!.AttackDamage == 10 && heroTemplate.Size == 2 &&
            hero.AssetKey == "MonsterHeroInTraining" && hero.Health == 11 && hero.MaxHealth == 11 &&
            hero.Modifiers!.AttackDamage == 10 && hero.Size == 2 && Stacks(hero, "regen") == 1 && Stacks(hero, "buff") == 0 &&
            buffEffect.Type == "AddStatus" && buffEffect.Target == "Self" &&
            buffEffect.Statuses.Single().Id == "buff" && buffEffect.Statuses.Single().Stacks == 2 &&
            !heroShift.HasTriggered && !actualHeroShift.HasTriggered,
            "Hero in Training must preserve its original 10/10 size-2 spawn and captured, untriggered two-buff OnShift callback.");
        BattleTurnState afterHero = After(35);
        CardInstanceState divineCard = afterHero.Spawn.Train.Context!.FindCard(ids[36])!;
        CardPlayRule divineRule = Before(35).PlayRules!.Cards.Single(rule => rule.DataId == DivineDancerId);
        CombatTrigger divineShift = divineRule.SpawnUnit!.Triggers.Single(trigger => trigger.Kind == "OnShift");
        CardActionEffect armorEffect = divineShift.Effects.Single().Action!;
        RoomCombatState divineRoom = afterHero.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1);
        int divineAppendPosition = divineRoom.Units.Count(unit => unit.Team == CombatTeam.Player);
        BattleActionResult divineAttempt = BattleActionModel.PlayCard(afterHero,
            new PlayCardAction(ids[36], 1, playerPosition: divineAppendPosition));
        Require(divineCard.DataId == DivineDancerId && afterHero.Spawn.Train.Context.Cards.Hand.Any(card => card.InstanceId == ids[36]) &&
            afterHero.Spawn.Train.Context.Statistics!.Value(ids[36], "TimesPlayed", "ThisBattle") == 0 &&
            !afterHero.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.SpawnerCardId == ids[36]) &&
            divineRule.AssetKey == "DivineDancer" && divineRule.SpawnUnit is { AssetKey: "MonsterDivineDancer", Health: 14, MaxHealth: 14, Size: 2 } divineTemplate &&
            divineTemplate.Modifiers!.AttackDamage == 7 && divineRoom.Units.Count(unit => unit.Team == CombatTeam.Player) ==
                afterHero.PlayRules!.Rooms.Single(room => room.RoomIndex == 1).PlayerSlots &&
            armorEffect.Type == "AddStatus" && armorEffect.Target == "Room" &&
            armorEffect.Statuses.Single().Id == "armor" && armorEffect.Statuses.Single().Stacks == 5 &&
            !divineShift.HasTriggered && !divineAttempt.Supported && divineAttempt.Reason == "The room cannot accept this unit's size or another spawn slot.",
            "Divine Dancer remains an explicit coverage gap: its original 7/14 definition is captured, but the native witness has no free room slot to spawn it.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-SUPPORT-UNITS PASS: twenty unchanged units, Alchemical Assistant native spawn/death with capped Dragon's Hoard, Avenger OnHit, Blighted/Prosperity Totem CardSpellPlayed and Battle Dancer/Hero in Training OnShift boundaries, explicit Divine Dancer room-slot gap, positive spawn/rally/heal/hit/attack/equipment/postcombat callbacks, native no-match typed-draw control and 16 immutable full chains.");
    }
    private static void NativeDivineDancer(FixtureValue root)
    {
        Require(root.GetProperty("OriginalSupportUnitsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed isolated Divine Dancer witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalSupportUnitsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length == 4, "Isolated Divine Dancer witness needs three anchor controls and one original birth.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        for (int index = 0; index < 3; index++)
        {
            BattleTurnState before = Before(index), after = After(index);
            PlayCardAction action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CombatUnit unit = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(candidate => candidate.SpawnerCardId == ids[index]);
            Require(card.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678" && action.RoomIndex == index && action.PlayerPosition == 0 &&
                before.Energy - after.Energy == 0 && unit.AssetKey == "TrainStewardBig" && unit.Health == 10000 &&
                unit.Modifiers!.AttackDamage == 1000 && unit.Size == 1, "Isolated Divine Dancer anchor control changed.");
        }
        BattleTurnState beforeDancer = Before(3), afterDancer = After(3);
        PlayCardAction dancerAction = actions[3].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule dancerRule = beforeDancer.PlayRules!.Cards.Single(rule => rule.DataId == DivineDancerId);
        CombatUnit template = dancerRule.SpawnUnit!, dancer = afterDancer.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.SpawnerCardId == ids[3]);
        CombatTrigger shift = template.Triggers.Single(trigger => trigger.Kind == "OnShift"), actualShift = dancer.Triggers.Single(trigger => trigger.Kind == "OnShift");
        CardActionEffect effect = shift.Effects.Single().Action!;
        RoomCombatState room = beforeDancer.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
        int position = room.Units.Count(unit => unit.Team == CombatTeam.Player);
        Require(beforeDancer.Spawn.Train.Context!.FindCard(ids[3])!.DataId == DivineDancerId &&
            dancerAction.RoomIndex == 0 && dancerAction.PlayerPosition == position && dancerAction.TargetUnitId == 0 &&
            beforeDancer.PlayRules!.Rooms.Single(item => item.RoomIndex == 0).PlayerSlots > position &&
            beforeDancer.Energy - afterDancer.Energy == dancerRule.Cost && dancerRule.AssetKey == "DivineDancer" &&
            template.AssetKey == "MonsterDivineDancer" && template.Health == 14 && template.MaxHealth == 14 &&
            template.Modifiers!.AttackDamage == 7 && template.Size == 2 && dancer.AssetKey == "MonsterDivineDancer" &&
            dancer.Health == 14 && dancer.MaxHealth == 14 && dancer.Modifiers!.AttackDamage == 7 && dancer.Size == 2 &&
            (dancer.Status("armor")?.Stacks ?? 0) == 0 && effect.Type == "AddStatus" && effect.Target == "Room" &&
            effect.Statuses.Single().Id == "armor" && effect.Statuses.Single().Stacks == 5 && !shift.HasTriggered && !actualShift.HasTriggered,
            "Divine Dancer must preserve its original 7/14 size-2 birth and captured, untriggered five-Armor room OnShift callback.");
        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-DIVINE-DANCER PASS: native 7/14 size-2 birth in an available room slot; captured five-Armor room OnShift remains untriggered.");
    }
    private static void NativeIsolatedUnit(FixtureValue root, string expectedId, string? expectedAsset = null, int actionIndex = 3)
    {
        Require(root.GetProperty("OriginalSupportUnitsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed isolated original unit witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalSupportUnitsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == actions.Length && ids.Length > actionIndex && actionIndex >= 3,
            "Isolated unit witness needs three anchors and a captured direct unit birth.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        for (int index = 0; index < 3; index++)
        {
            BattleTurnState before = Before(index), after = After(index);
            PlayCardAction action = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState card = before.Spawn.Train.Context!.FindCard(ids[index])!;
            CombatUnit unit = after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(candidate => candidate.SpawnerCardId == ids[index]);
            Require(card.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678" && action.RoomIndex == index && action.PlayerPosition == 0 &&
                before.Energy - after.Energy == 0 && unit.AssetKey == "TrainStewardBig" && unit.Health == 10000 &&
                unit.Modifiers!.AttackDamage == 1000 && unit.Size == 1, "Isolated unit batch anchor control changed.");
        }
        int energyControlId = root.TryGetProperty("OriginalIsolatedEnergyControlId", out FixtureValue energyControlValue)
            ? energyControlValue.GetInt32() : 0;
        if (energyControlId > 0)
        {
            BattleTurnState energyBefore = Before(3), energyAfter = After(3);
            PlayCardAction energyAction = actions[3].GetProperty("Action").Deserialize<PlayCardAction>()!;
            CardInstanceState energyCard = energyBefore.Spawn.Train.Context!.FindCard(energyControlId)!;
            CardPlayRule energyRule = energyBefore.PlayRules!.Cards.Single(item => item.DataId == energyCard.DataId);
            CardActionEffect gain = energyRule.Effects.Single();
            Require(ids[3] == energyControlId && energyAction.CardInstanceId == energyControlId &&
                energyAction.RoomIndex == 0 && energyAction.PlayerPosition == -1 && energyAction.TargetUnitId == 0 &&
                energyCard.DataId == "ac110058-0000-4000-8000-000000000009" && energyRule.Cost == 0 &&
                energyRule.CardType == "Spell" && energyRule.CostType == "Default" &&
                gain is { Type: "GainEnergy", Target: "Room", Value: > 0 } &&
                energyAfter.Energy - energyBefore.Energy == gain.Value,
                "Isolated-unit energy control must be a distinct 0-cost card that natively adds its captured energy amount.");
        }
        BattleTurnState unitBefore = Before(actionIndex), unitAfter = After(actionIndex);
        PlayCardAction unitAction = actions[actionIndex].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardInstanceState unitCard = unitBefore.Spawn.Train.Context!.FindCard(ids[actionIndex])!;
        CardPlayRule rule = unitBefore.PlayRules!.Cards.Single(item => item.DataId == expectedId);
        CombatUnit template = rule.SpawnUnit!, spawnedUnit = unitAfter.Spawn.Train.Rooms.SelectMany(item => item.Units)
            .Single(candidate => candidate.SpawnerCardId == ids[actionIndex]);
        int appendPosition = unitBefore.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0).Units.Count(candidate => candidate.Team == CombatTeam.Player);
        bool juggernautXCost = expectedId == JuggernautId;
        Require(unitCard.DataId == expectedId && unitAction.RoomIndex == 0 && unitAction.PlayerPosition == appendPosition &&
            unitAction.TargetUnitId == 0 && unitBefore.PlayRules.Rooms.Single(item => item.RoomIndex == 0).PlayerSlots > appendPosition &&
            unitBefore.Energy >= rule.Cost && (juggernautXCost ? unitBefore.Energy == 3 && unitAfter.Energy == 0 : unitBefore.Energy - unitAfter.Energy == rule.Cost) &&
            rule.Effect == "SpawnMonster" &&
            !string.IsNullOrEmpty(rule.AssetKey) && (expectedAsset == null || rule.AssetKey == expectedAsset) &&
            spawnedUnit.AssetKey == template.AssetKey && spawnedUnit.Size == template.Size &&
            (juggernautXCost || spawnedUnit.Health == template.Health && spawnedUnit.MaxHealth == template.MaxHealth &&
                spawnedUnit.Modifiers!.AttackDamage == template.Modifiers!.AttackDamage),
            expectedAsset + " must preserve its captured original card identity, paid spawn and native birth template.");
        if (expectedId == MonstrosityId)
        {
            CombatTrigger death = template.Triggers.Single(trigger => trigger.Kind == "OnAnyUnitDeathOnFloor");
            CombatEffect growth = death.Effects.Single();
            CardActionEffect action = growth.Action!;
            var buff = action.Statuses.Single();
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "Monstrosity" && rule.Cost == 2 && template.AssetKey == "MonsterMonstrosity" &&
                template.Modifiers!.AttackDamage == 10 && template.Health == 30 && template.MaxHealth == 30 && template.Size == 3 &&
                death.Effects.Count == 1 && growth.Type == "CardEffectAddStatusEffect" && growth.Value == 100 &&
                action.Type == "AddStatus" && action.Target == "Self" && buff.Id == "buff" && buff.Stacks == 5 && buff.ParamInt == 2 &&
                buff.RemoveStackAtEnd && spawnedUnit.Modifiers!.AttackDamage == 10 && spawnedUnit.Health == 30 &&
                spawnedUnit.MaxHealth == 30 && spawnedUnit.Size == 3 && unitTurns.Length == 4 &&
                unitTurns.All(unit => unit.Triggers.Single(trigger => trigger.Kind == "OnAnyUnitDeathOnFloor").HasTriggered &&
                    unit.Status("buff") is { Stacks: > 0 }),
                "Monstrosity must preserve its original 2-cost 10/30 size-3 birth and positively gain temporary self buff on floor-unit deaths.");
            Console.WriteLine("ORIGINAL-MONSTROSITY PASS: native 2-cost 10/30 size-3 birth; OnAnyUnitDeathOnFloor added temporary buff in all four turn snapshots.");
        }
        if (expectedId == WicklessBaronId)
        {
            CombatTrigger death = template.Triggers.Single(trigger => trigger.Kind == "OnAnyUnitDeathOnFloor");
            CombatEffect growth = death.Effects.Single();
            CardActionEffect upgrade = growth.UnitUpgrade!;
            int[] turnAttacks = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!.Modifiers!.AttackDamage).ToArray();
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "WicklessBaron" && rule.Cost == 1 && template.AssetKey == "WicklessBaron" &&
                template.Modifiers!.AttackDamage == 5 && template.Health == 15 && template.MaxHealth == 15 && template.Size == 2 &&
                death.Effects.Count == 1 && growth.Type == "CardEffectAddTempCardUpgradeToUnits" &&
                upgrade is { Type: "UnitUpgrade", Target: "Self", Lifetime: "TemporaryUntilEndOfBattle" } &&
                upgrade.Upgrade is { AssetKey: "WicklessBaronTrigger" } && upgrade.Upgrade.Stats.Damage == 5 && upgrade.Upgrade.Stats.Health == 5 &&
                spawnedUnit.Modifiers!.AttackDamage == 5 && spawnedUnit.Health == 15 && spawnedUnit.MaxHealth == 15 && spawnedUnit.Size == 2 &&
                turnAttacks.SequenceEqual([15, 25, 25, 35]) && unitTurns.All(unit => unit.Triggers.Single(trigger =>
                    trigger.Kind == "OnAnyUnitDeathOnFloor").HasTriggered),
                "Wickless Baron must retain its 1-cost 5/15 size-2 birth and gain temporary +5/+5 on floor-unit deaths.");
            Console.WriteLine("ORIGINAL-WICKLESS-BARON PASS: native OnAnyUnitDeathOnFloor applied temporary +5/+5; attacks across four snapshots were 15,25,25,35.");
        }
        if (expectedId == WicklessTycoonId)
        {
            CombatTrigger death = template.Triggers.Single(trigger => trigger.Kind == "OnAnyUnitDeathOnFloor");
            CombatEffect reward = death.Effects.Single();
            CombatUnit? InTurn(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]);
            bool rewardedOnFirstDeath = root.GetProperty("Turns").EnumerateArray().Any(row =>
            {
                BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
                BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CombatUnit? prior = InTurn(before), current = InTurn(after);
                int goldBefore = before.Spawn.Train.Context!.Gold;
                int goldAfter = after.Spawn.Train.Context!.Gold;
                return prior != null && current != null && goldBefore == 50 && goldAfter == 70 &&
                    !prior.Triggers.Single(trigger => trigger.Kind == "OnAnyUnitDeathOnFloor").HasTriggered &&
                    current.Triggers.Single(trigger => trigger.Kind == "OnAnyUnitDeathOnFloor").HasTriggered;
            });
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(InTurn).Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "WicklessTycoon" && rule.Cost == 1 && template.AssetKey == "WicklessTycoon" &&
                template.Modifiers!.AttackDamage == 10 && template.Health == 40 && template.MaxHealth == 40 && template.Size == 2 &&
                death.Effects.Count == 1 && reward is { Type: "CardEffectRewardGold", Value: 10 } &&
                spawnedUnit.Modifiers!.AttackDamage == 10 && spawnedUnit.Health == 40 && spawnedUnit.MaxHealth == 40 && spawnedUnit.Size == 2 &&
                rewardedOnFirstDeath && unitTurns.Length == 4 && unitTurns.All(unit => unit.Triggers.Single(trigger =>
                    trigger.Kind == "OnAnyUnitDeathOnFloor").HasTriggered),
                "Wickless Tycoon must retain its 1-cost 10/40 size-2 birth and reward 10 gold for each floor-unit death.");
            Console.WriteLine("ORIGINAL-WICKLESS-TYCOON PASS: native OnAnyUnitDeathOnFloor reward was defined at +10 gold; first transition was 50 to 70.");
        }
        if (expectedId == DeathEaterId)
        {
            CombatTrigger death = template.Triggers.Single(trigger => trigger.Kind == "OnAnyUnitDeathOnFloor");
            CardActionEffect soul = death.Effects.Single().Action!;
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            int[] soulStacks = unitTurns.Select(unit => unit.Status("captured_soul")?.Stacks ?? 0).ToArray();
            Require(rule.AssetKey == "DeathEater" && rule.Cost == 1 && template.AssetKey == "MonsterDeathEater" &&
                template.Modifiers!.AttackDamage == 0 && template.Health == 5 && template.MaxHealth == 5 && template.Size == 1 &&
                death.Effects.Count == 1 && soul is { Type: "AddStatus", Target: "Self" } &&
                soul.Statuses.Single() is { Id: "captured_soul", Stacks: 1, Stackable: true } &&
                spawnedUnit.Status("captured_soul") == null && unitTurns.Length == 4 && soulStacks[0] > 0 &&
                soulStacks.Zip(soulStacks.Skip(1), (before, after) => after >= before).All(increasedOrEqual => increasedOrEqual) &&
                unitTurns.Any(unit => unit.Triggers.Single(trigger => trigger.Kind == "OnAnyUnitDeathOnFloor").HasTriggered),
                "Death Eater must retain its original 1-cost 0/5 birth and gain captured_soul only after same-floor unit deaths.");
            Console.WriteLine("ORIGINAL-DEATH-EATER PASS: native OnAnyUnitDeathOnFloor added captured_soul after birth (four turn stacks: " +
                string.Join(",", soulStacks) + ").");
        }
        if (expectedId == LunaCovenChamp1Id)
        {
            CombatTrigger onDeath = template.Triggers.Single(trigger => trigger.Kind == "OnDeath");
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "LunaCovenChamp1" && rule.Cost == 0 && template.AssetKey == "MonsterChampionLunaCoven1" &&
                template.Modifiers!.AttackDamage == 5 && template.Health == 5 && template.MaxHealth == 5 && template.Size == 2 &&
                template.Ability == null && onDeath.Effects.Single().Type == "CardEffectNULL" &&
                spawnedUnit.AssetKey == template.AssetKey && spawnedUnit.Health == 5 && spawnedUnit.Size == 2 &&
                unitTurns.Length == 4 && unitTurns.All(unit => unit.AssetKey == template.AssetKey && unit.Health == 5 && unit.MaxHealth == 5 &&
                    unit.Modifiers!.AttackDamage == 5 && unit.Size == 2 && unit.Ability == null &&
                    !unit.Triggers.Single(trigger => trigger.Kind == "OnDeath").HasTriggered),
                "Luna Coven Champion 1 must retain its original 0-cost 5/5 size-2 birth without activating a unit ability or OnDeath callback.");
            Console.WriteLine("ORIGINAL-LUNA-COVEN-CHAMPION-1 PASS: native 0-cost 5/5 size-2 birth remained unchanged across four turn snapshots.");
        }
        if (expectedId == LunaCovenChamp2Id)
        {
            CombatTrigger onSpawn = template.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "LunaCovenChamp2" && rule.Cost == 0 && template.AssetKey == "MonsterChampionLunaCoven2" &&
                template.Modifiers!.AttackDamage == 10 && template.Health == 10 && template.MaxHealth == 10 && template.Size == 2 &&
                template.Status("mageblade") is { Stacks: 2, ParamInt: 1 } && template.Ability == null &&
                onSpawn.Effects.Single().Type == "CardEffectNULL" && spawnedUnit.Status("mageblade") is { Stacks: 2, ParamInt: 1 } &&
                unitTurns.Length == 4 && unitTurns.All(unit => unit.AssetKey == template.AssetKey && unit.Health == 10 && unit.MaxHealth == 10 &&
                    unit.Modifiers!.AttackDamage == 10 && unit.Size == 2 && unit.Status("mageblade") is { Stacks: 2, ParamInt: 1 } &&
                    unit.Ability == null && !unit.Triggers.Single(trigger => trigger.Kind == "OnSpawn").HasTriggered),
                "Luna Coven Champion 2 must retain its 0-cost 10/10 size-2 birth and Mageblade 2 without activating the inert OnSpawn callback.");
            Console.WriteLine("ORIGINAL-LUNA-COVEN-CHAMPION-2 PASS: native 10/10 size-2 birth retained Mageblade 2; inert OnSpawn stayed inactive across four snapshots.");
        }
        if (expectedId == SporelauncherId)
        {
            CombatTrigger played = template.Triggers.Single(trigger => trigger.Kind == "CardMonsterPlayed");
            CardActionEffect decay = played.Effects.Single().Action!;
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "Sporelauncher" && rule.Cost == 2 && template.AssetKey == "MonsterSporelauncher" &&
                template.Modifiers!.AttackDamage == 30 && template.Health == 30 && template.MaxHealth == 30 && template.Size == 3 &&
                played.Effects.Single().Type == "CardEffectAddStatusEffect" && decay is { Type: "AddStatus", Target: "Room", AllowEnemy: true, AllowPlayer: false } &&
                decay.Statuses.Single() is { Id: "decay", Stacks: 2 } && unitTurns.Length == 4 &&
                unitTurns.All(unit => unit.Health == 30 && unit.MaxHealth == 30 && unit.Modifiers!.AttackDamage == 30 && unit.Size == 3 &&
                    !unit.Triggers.Single(trigger => trigger.Kind == "CardMonsterPlayed").HasTriggered),
                "Sporelauncher must retain its original 2-cost 30/30 size-3 birth and inactive enemy-only CardMonsterPlayed Decay definition.");
            Console.WriteLine("ORIGINAL-SPORELAUNCHER PASS: native 2-cost 30/30 size-3 birth; enemy Decay callback remained inactive in four snapshots.");
        }
        if (expectedId == ConductorStewardId)
        {
            CombatTrigger onSpawn = template.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
            CardActionEffect available = onSpawn.Effects.Single(effect => effect.Action?.Type == "AddStatus").Action!;
            CardActionEffect reset = onSpawn.Effects.Single(effect => effect.Action?.Type == "ResetCooldown").Action!;
            CombatTrigger played = template.Triggers.Single(trigger => trigger.Kind == "CardMonsterPlayed");
            CardActionEffect temporaryUpgrade = played.Effects.Single().UnitUpgrade!;
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "ConductorSteward" && rule.Cost == 2 && template.AssetKey == "MonsterForemanSteward" &&
                template.Modifiers!.AttackDamage == 10 && template.Health == 10 && template.MaxHealth == 10 && template.Size == 2 &&
                template.Ability is { DataId: "8a8b394d-3dda-48dd-b157-20ca0c5fd7fb", Cooldown: 3, CooldownAtSpawn: 0, HasAbility: true } &&
                onSpawn.Effects.Count == 2 && available is { Type: "AddStatus", Target: "Self" } &&
                available.Statuses.Single() is { Id: "unit_ability_available", Stacks: 1, ParamInt: 1 } &&
                reset is { Type: "ResetCooldown", Target: "Self", CooldownParameter: true } &&
                spawnedUnit.Status("unit_ability_available") is { Stacks: 1, ParamInt: 1 } &&
                spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnSpawn").HasTriggered &&
                played.Effects.Single().Type == "CardEffectAddTempCardUpgradeToUnits" &&
                temporaryUpgrade is { Type: "UnitUpgrade", Target: "LastSpawnedCharacter", Lifetime: "TemporaryUntilEndOfBattle" } &&
                temporaryUpgrade.Upgrade is { DataId: "5b38fa40-8776-4a23-af0c-22845e4ea552", AssetKey: "Plus5Attack5Health" } &&
                temporaryUpgrade.Upgrade.Stats.Damage == 5 && temporaryUpgrade.Upgrade.Stats.Health == 5 &&
                !played.HasTriggered && !spawnedUnit.Triggers.Single(trigger => trigger.Kind == "CardMonsterPlayed").HasTriggered &&
                unitTurns.Length == 4 && unitTurns.All(unit => unit.Health == 10 && unit.MaxHealth == 10 &&
                    unit.Modifiers!.AttackDamage == 10 && unit.Size == 2 &&
                    unit.Ability is { DataId: "8a8b394d-3dda-48dd-b157-20ca0c5fd7fb", Cooldown: 3, CooldownAtSpawn: 0, HasAbility: true } &&
                    unit.Status("unit_ability_available") is { Stacks: 1, ParamInt: 1 } &&
                    !unit.Triggers.Single(trigger => trigger.Kind == "OnOwnAbilityActivated").HasTriggered &&
                    !unit.Triggers.Single(trigger => trigger.Kind == "CardMonsterPlayed").HasTriggered),
                "Conductor Steward must preserve its original 2-cost 10/10 size-2 birth, expose but not use its cooldown-3 ability, and retain an inactive temporary +5/+5 CardMonsterPlayed callback.");
            Console.WriteLine("ORIGINAL-CONDUCTOR-STEWARD PASS: native 2-cost 10/10 size-2 birth; cooldown-3 ability remained available but unused, and temporary +5/+5 CardMonsterPlayed stayed inactive across four snapshots.");
        }
        if (expectedId == EnchantrelleId)
        {
            CombatTrigger played = template.Triggers.Single(trigger => trigger.Kind == "CardMonsterPlayed");
            CardActionEffect debuff = played.Effects.Single().Action!;
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "Enchantrelle" && rule.Cost == 3 && template.AssetKey == "MonsterEnchantrelle" &&
                template.Modifiers!.AttackDamage == 8 && template.Health == 8 && template.MaxHealth == 8 && template.Size == 2 &&
                template.Ability == null && spawnedUnit.AssetKey == template.AssetKey && spawnedUnit.Health == 8 &&
                spawnedUnit.MaxHealth == 8 && spawnedUnit.Modifiers!.AttackDamage == 8 && spawnedUnit.Size == 2 &&
                played.Effects.Single().Type == "CardEffectAddStatusEffect" &&
                debuff is { Type: "AddStatus", Target: "Room", AllowEnemy: true, AllowPlayer: false } &&
                debuff.Statuses.Single() is { Id: "debuff", Stacks: 1, ParamInt: 2, Stackable: true, RemoveStackAtEnd: true, ExcludeMonsterPropagation: true } &&
                !played.HasTriggered && unitTurns.Length == 4 && unitTurns.All(unit => unit.Health == 8 && unit.MaxHealth == 8 &&
                    unit.Modifiers!.AttackDamage == 8 && unit.Size == 2 && unit.Ability == null && unit.Status("debuff") == null &&
                    !unit.Triggers.Single(trigger => trigger.Kind == "CardMonsterPlayed").HasTriggered),
                "Enchantrelle must retain its original 3-cost 8/8 size-2 birth and enemy-only CardMonsterPlayed debuff definition, which remains inactive without a later Monster play.");
            Console.WriteLine("ORIGINAL-ENCHANTRELLE PASS: native 3-cost 8/8 size-2 birth; enemy-only CardMonsterPlayed debuff remained inactive across four snapshots.");
        }
        if (expectedId == FeederDamageEnemiesUnitId)
        {
            CombatTrigger onFeed = template.Triggers.Single(trigger => trigger.Kind == "OnFeed");
            CardActionEffect heal = onFeed.Effects.Single(effect => effect.Action?.Type == "Heal").Action!;
            CardActionEffect damage = onFeed.Effects.Single(effect => effect.Action?.Type == "Damage").Action!;
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "FeederDamageEnemiesUnit" && rule.Cost == 2 && template.AssetKey == "MonsterGlutton" &&
                template.Modifiers!.AttackDamage == 0 && template.Health == 50 && template.MaxHealth == 50 && template.Size == 3 &&
                template.Ability == null && onFeed.SkipDuringDeployment == true &&
                onFeed.Effects.Single(effect => effect.Action?.Type == "Heal").Type == "CardEffectHeal" &&
                heal is { Type: "Heal", Target: "Self", Value: 10, AllowEnemy: true, AllowPlayer: false } &&
                onFeed.Effects.Single(effect => effect.Action?.Type == "Damage").Type == "CardEffectDamage" &&
                damage is { Type: "Damage", Target: "Room", Value: 20, AllowEnemy: true, AllowPlayer: false } &&
                spawnedUnit.AssetKey == template.AssetKey && spawnedUnit.Health == 50 && spawnedUnit.MaxHealth == 50 &&
                spawnedUnit.Modifiers!.AttackDamage == 0 && spawnedUnit.Size == 3 && !onFeed.HasTriggered &&
                unitTurns.Length == 4 && unitTurns.All(unit => unit.Health == 50 && unit.MaxHealth == 50 &&
                    unit.Modifiers!.AttackDamage == 0 && unit.Size == 3 && unit.Ability == null &&
                    !unit.Triggers.Single(trigger => trigger.Kind == "OnFeed").HasTriggered),
                "Feeder Damage Enemies must retain its original 2-cost 0/50 size-3 birth and inactive OnFeed +10 self-heal / enemy-room 20-damage definition.");
            Console.WriteLine("ORIGINAL-FEEDER-DAMAGE-ENEMIES PASS: native 2-cost 0/50 size-3 birth; OnFeed heal/damage callback remained inactive across four snapshots.");
        }
        if (expectedId == LadyOfTheReformedId)
        {
            CombatTrigger onHit = template.Triggers.Single(trigger => trigger.Kind == "OnHit");
            CardActionEffect addEphemeral = onHit.Effects.Single().Action!;
            BattleTurnState[] turnStates = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
            CombatUnit? InTurn(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]);
            CombatUnit[] unitTurns = turnStates.Select(InTurn).Where(unit => unit != null).Select(unit => unit!).ToArray();
            int[] ephemeralStacks = unitTurns.Select(unit => unit.Status("ephemeral")?.Stacks ?? 0).ToArray();
            int lastPresentTurn = Array.FindLastIndex(turnStates, state => InTurn(state) != null);
            Require(rule.AssetKey == "LadyOfTheReformed" && rule.Cost == 1 && template.AssetKey == "LadyOfTheReformed" &&
                template.Modifiers!.AttackDamage == 10 && template.Health == 40 && template.MaxHealth == 40 && template.Size == 2 &&
                template.Status("ephemeral") is { Stacks: 3, RemoveWhenTriggered: true } && template.Ability == null &&
                onHit.Effects.Single().Type == "CardEffectAddStatusEffect" &&
                addEphemeral is { Type: "AddStatus", Target: "Room", AllowEnemy: false, AllowPlayer: true } &&
                addEphemeral.Filters?.RequiredStatuses.SequenceEqual(new[] { "ephemeral" }) == true &&
                addEphemeral.Statuses.Single() is { Id: "ephemeral", Stacks: 1, RemoveWhenTriggered: true } &&
                spawnedUnit.AssetKey == template.AssetKey && spawnedUnit.Health == 40 && spawnedUnit.MaxHealth == 40 &&
                spawnedUnit.Modifiers!.AttackDamage == 10 && spawnedUnit.Size == 2 &&
                spawnedUnit.Status("ephemeral") is { Stacks: 3, RemoveWhenTriggered: true } && !onHit.HasTriggered &&
                unitTurns.Length == 2 && ephemeralStacks.SequenceEqual(new[] { 2, 1 }) &&
                unitTurns.All(unit => unit.Health == 40 && unit.MaxHealth == 40 && unit.Modifiers!.AttackDamage == 10 &&
                    unit.Size == 2 && !unit.Triggers.Single(trigger => trigger.Kind == "OnHit").HasTriggered) &&
                lastPresentTurn >= 0 && turnStates.Skip(lastPresentTurn + 1).All(state => InTurn(state) == null),
                "Lady of the Reformed must preserve its 1-cost 10/40 size-2 Ephemeral-3 birth, native 2/1 decay snapshots, and inactive Ephemeral-gated OnHit callback.");
            Console.WriteLine("ORIGINAL-LADY-OF-THE-REFORMED PASS: native 1-cost 10/40 size-2 birth; Ephemeral fell 3 to 2 to 1 before the unit disappeared, while its gated OnHit callback stayed inactive.");
        }
        if (expectedId == FeederPermanentAttackBoostId)
        {
            CombatTrigger onFeed = template.Triggers.Single(trigger => trigger.Kind == "OnFeed");
            CombatEffect triggerEffect = onFeed.Effects.Single();
            CardActionEffect permanentUpgrade = triggerEffect.UnitUpgrade!;
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "FeederPermanentAttackBoost" && rule.Cost == 2 && template.AssetKey == "MonsterEpicure" &&
                template.Modifiers!.AttackDamage == 0 && template.Health == 35 && template.MaxHealth == 35 && template.Size == 3 &&
                template.Ability == null && onFeed.SkipDuringDeployment == true &&
                triggerEffect.Type == "CardEffectAddCardUpgradeToUnits" &&
                permanentUpgrade is { Type: "UnitUpgrade", Target: "Self", Lifetime: "Permanent", ScaleUpgrade: true } &&
                permanentUpgrade.Upgrade is { DataId: "5a22121e-80f7-447c-85b3-1582994457ad", AssetKey: "Plus2AttackScalable" } &&
                permanentUpgrade.Upgrade.Stats.Damage == 2 && permanentUpgrade.Upgrade.Stats.Health == 0 &&
                spawnedUnit.AssetKey == template.AssetKey && spawnedUnit.Health == 35 && spawnedUnit.MaxHealth == 35 &&
                spawnedUnit.Modifiers!.AttackDamage == 0 && spawnedUnit.Size == 3 && spawnedUnit.Modifiers.Upgrades!.Count == 0 &&
                !onFeed.HasTriggered && unitTurns.Length == 4 && unitTurns.All(unit => unit.Health == 35 &&
                    unit.MaxHealth == 35 && unit.Modifiers!.AttackDamage == 0 && unit.Size == 3 &&
                    unit.Modifiers.Upgrades!.Count == 0 && !unit.Triggers.Single(trigger => trigger.Kind == "OnFeed").HasTriggered),
                "Feeder Permanent Attack Boost must retain its original 2-cost 0/35 size-3 birth and inactive permanent scalable +2 attack OnFeed upgrade.");
            Console.WriteLine("ORIGINAL-FEEDER-PERMANENT-ATTACK-BOOST PASS: native 2-cost 0/35 size-3 birth; permanent scalable +2 attack OnFeed upgrade remained inactive across four snapshots.");
        }
        if (expectedId == ZealotId)
        {
            CombatTrigger onTurnBegin = template.Triggers.Single(trigger => trigger.Kind == "OnTurnBegin");
            CardActionEffect pyregel = onTurnBegin.Effects.Single().Action!;
            BattleTurnState[] turnStates = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
            CombatUnit[] unitTurns = turnStates.SelectMany(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units))
                .Where(unit => unit.SpawnerCardId == ids[actionIndex]).ToArray();
            bool noEnemyPyregel = turnStates.All(state => state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                .Where(unit => unit.Team != CombatTeam.Player).All(unit => unit.Status("pyregel") == null));
            Require(rule.AssetKey == "Zealot" && rule.Cost == 3 && template.AssetKey == "MonsterZealot" &&
                template.Modifiers!.AttackDamage == 10 && template.Health == 20 && template.MaxHealth == 20 && template.Size == 2 &&
                template.Status("sweep") is { Stacks: 1 } && template.Ability == null &&
                onTurnBegin.SkipDuringDeployment == true && onTurnBegin.Effects.Single().Type == "CardEffectAddStatusEffect" &&
                pyregel is { Type: "AddStatus", Target: "Room", AllowEnemy: true, AllowPlayer: false } &&
                pyregel.Statuses.Single() is { Id: "pyregel", Stacks: 5, ParamInt: 1, ExcludeMonsterPropagation: true } &&
                spawnedUnit.AssetKey == template.AssetKey && spawnedUnit.Health == 20 && spawnedUnit.MaxHealth == 20 &&
                spawnedUnit.Modifiers!.AttackDamage == 10 && spawnedUnit.Size == 2 && spawnedUnit.Status("sweep") is { Stacks: 1 } &&
                unitTurns.Length == 4 && unitTurns.All(unit => unit.Health == 20 && unit.MaxHealth == 20 &&
                    unit.Modifiers!.AttackDamage == 10 && unit.Size == 2 && unit.Status("sweep") is { Stacks: 1 } &&
                    unit.Triggers.Single(trigger => trigger.Kind == "OnTurnBegin") is { HasTriggered: true, FireCount: 1 }) &&
                noEnemyPyregel,
                "Zealot must preserve its original 3-cost 10/20 size-2 Sweep birth and one native OnTurnBegin Pyregel definition; no enemy Pyregel application was observed.");
            Console.WriteLine("ORIGINAL-ZEALOT PASS: native 3-cost 10/20 size-2 Sweep birth; OnTurnBegin fired once, but Pyregel was absent from all observed enemy snapshots.");
        }
        if (expectedId == ParrafinThugId)
        {
            CombatTrigger onKill = template.Triggers.Single(trigger => trigger.Kind == "OnKill");
            BattleTurnState[] turnStates = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
            CombatUnit? InTurn(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]);
            bool paidThirtyGoldOnKill = root.GetProperty("Turns").EnumerateArray().Any(row =>
            {
                BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
                BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CombatUnit? prior = InTurn(before), current = InTurn(after);
                int goldBefore = row.GetProperty("Before").GetProperty("Spawn").GetProperty("Train").GetProperty("Context").GetProperty("Gold").GetInt32();
                int goldAfter = row.GetProperty("Actual").GetProperty("Spawn").GetProperty("Train").GetProperty("Context").GetProperty("Gold").GetInt32();
                return prior != null && current != null && goldBefore == 50 && goldAfter == 80 &&
                    !prior.Triggers.Single(trigger => trigger.Kind == "OnKill").HasTriggered &&
                    current.Triggers.Single(trigger => trigger.Kind == "OnKill").HasTriggered;
            });
            CombatUnit[] unitTurns = turnStates.Select(InTurn).Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "ParrafinThug" && rule.Cost == 1 && template.AssetKey == "ParaffinThug" &&
                template.Modifiers!.AttackDamage == 40 && template.Health == 10 && template.MaxHealth == 10 &&
                template.Size == 2 && template.Ability == null && onKill.Effects.Single() is { Type: "CardEffectRewardGold", Value: 30 } &&
                spawnedUnit.AssetKey == template.AssetKey && spawnedUnit.Modifiers!.AttackDamage == 40 &&
                spawnedUnit.Health == 10 && spawnedUnit.MaxHealth == 10 && spawnedUnit.Size == 2 &&
                paidThirtyGoldOnKill && unitTurns.Length == 4 && unitTurns.All(unit => unit.Health == 10 &&
                    unit.MaxHealth == 10 && unit.Modifiers!.AttackDamage == 40 && unit.Size == 2 &&
                    unit.Triggers.Single(trigger => trigger.Kind == "OnKill").HasTriggered),
                "Parrafin Thug must retain its original 1-cost 40/10 size-2 birth and positively reward its first native OnKill with exactly 30 gold.");
            Console.WriteLine("ORIGINAL-PARRAFIN-THUG PASS: native 1-cost 40/10 size-2 birth; first OnKill changed gold 50 to 80 for the captured +30 reward.");
        }
        if (expectedId == JuggernautId)
        {
            CombatTrigger onSpawn = template.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
            CombatTrigger spawnedOnSpawn = spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
            CombatEffect effect = onSpawn.Effects.Single();
            CardActionEffect upgradeEffect = effect.UnitUpgrade!;
            ScalingUnitUpgradeTrait[] scaling = unitCard.UnitUpgradeScalingTraits!.ToArray();
            ScalingUnitUpgradeTrait attack = scaling.Single(trait => trait.Stat == "Damage");
            ScalingUnitUpgradeTrait health = scaling.Single(trait => trait.Stat == "Health");
            CardUpgradeModifier appliedUpgrade = spawnedUnit.Modifiers!.Upgrades.Single();
            Require(rule.AssetKey == "Juggernaut" && rule.Cost == 0 && rule.CostType == "ConsumeRemainingEnergy" &&
                template.AssetKey == "MonsterJuggernaut" && template.Modifiers!.AttackDamage == 30 &&
                template.Health == 30 && template.MaxHealth == 30 && template.Size == 3 && !onSpawn.Once &&
                effect.Type == "CardEffectAddTempCardUpgradeToUnits" &&
                upgradeEffect is { Type: "UnitUpgrade", Target: "Self", Lifetime: "TemporaryUntilEndOfBattle" } &&
                upgradeEffect.Upgrade is { DataId: "270a2a5d-2d9e-450b-8b19-d347ea47867f", AssetKey: "Juggernaut_Upgrade" } &&
                upgradeEffect.Upgrade.Stats.Damage == 0 && upgradeEffect.Upgrade.Stats.Health == 0 &&
                scaling.Length == 2 && attack.AmountPerStat == 30 && attack.Restriction == 1 && attack.Query.Type == "PlayedCost" &&
                attack.Query.Duration == "ThisTurn" && attack.Query.SourceRawCost == 0 && attack.Query.VariableCost &&
                health.AmountPerStat == 30 && health.Restriction == 1 && health.Query.Type == "PlayedCost" &&
                health.Query.Duration == "ThisTurn" && health.Query.SourceRawCost == 0 && health.Query.VariableCost &&
                spawnedOnSpawn.HasTriggered && spawnedUnit.AssetKey == "MonsterJuggernaut" && spawnedUnit.Size == 3 &&
                spawnedUnit.Modifiers.AttackDamage == 120 && spawnedUnit.Health == 120 && spawnedUnit.MaxHealth == 120 &&
                appliedUpgrade is { DataId: "270a2a5d-2d9e-450b-8b19-d347ea47867f", AssetKey: "Juggernaut_Upgrade" } &&
                appliedUpgrade.Stats.Damage == 90 && appliedUpgrade.Stats.Health == 90,
                "Juggernaut must spend three remaining Energy and scale its native OnSpawn upgrade to +90/+90 from the two +30-per-PlayedCost traits.");
        }
        if (expectedId == HoardAcolyteId)
        {
            CombatTrigger onSpawn = template.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
            CombatTrigger spawnedOnSpawn = spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
            CardActionEffect available = onSpawn.Effects.Single(effect => effect.Action?.Type == "AddStatus").Action!;
            CardActionEffect reset = onSpawn.Effects.Single(effect => effect.Action?.Type == "ResetCooldown").Action!;
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            const string abilityId = "7a0103bd-9bbb-4a9e-af38-334ed948db5f";
            Require(rule.AssetKey == "HoardAcolyte" && rule.Cost == 0 && template.AssetKey == "MonsterHoardTotem" &&
                template.Modifiers!.AttackDamage == 5 && template.Health == 3 && template.MaxHealth == 3 && template.Size == 1 &&
                template.Ability is { DataId: abilityId, Cooldown: 2, CooldownAtSpawn: 0, HasAbility: true } &&
                !onSpawn.Once && onSpawn.Effects.Count == 2 && spawnedOnSpawn.HasTriggered &&
                available is { Type: "AddStatus", Target: "Self" } &&
                available.Statuses.Single() is { Id: "unit_ability_available", Stacks: 1, ParamInt: 1 } &&
                reset is { Type: "ResetCooldown", Target: "Self", CooldownParameter: true } &&
                spawnedUnit.Ability is { DataId: abilityId, Cooldown: 2, CooldownAtSpawn: 0, HasAbility: true } &&
                spawnedUnit.Status("unit_ability_available") is { Stacks: 1, ParamInt: 1 } &&
                unitTurns.Length == 4 && unitTurns.All(unit => unit.Ability is
                    { DataId: abilityId, Cooldown: 2, CooldownAtSpawn: 0, HasAbility: true } &&
                    unit.Status("unit_ability_available") is { Stacks: 1, ParamInt: 1 } &&
                    !unit.Triggers.Single(trigger => trigger.Kind == "OnOwnAbilityActivated").HasTriggered),
                "Hoard Acolyte must retain its native 0-cost 5/3 birth, expose its cooldown-2 ability on spawn and leave it unused.");
        }
        if (expectedId == HotHeadId)
        {
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "HotHead", "HotHead", "MonsterHotHead", 2, 5, 20, 2, 2, "bd217216-3d1d-4139-9003-5d6a42f059d4");
        }
        if (expectedId == SteadfastCrusaderId)
        {
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "Steadfast Crusader", "SteadfastCrusader", "MonsterSteadfastCrusader", 1, 5, 30, 2, 2, "3366ff9e-1beb-4246-a025-c448b47dacae");
        }
        if (expectedId == TrufflesId)
        {
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "Truffles", "Truffles", "MonsterTruffles", 1, 5, 5, 1, 2, "8d54f68b-5660-4507-84c3-a0cf092bfbf1");
        }
        if (expectedId == PyrebloodedId)
        {
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "Pyreblooded", "Pyreblooded", "MonsterPyreblooded", 1, 5, 10, 1, 3, "7eb9fd9a-0d9b-4509-80b9-991de5f217c0");
            ScalingUnitUpgradeTrait abilityScaling = template.Ability!.CardCreation!.UnitUpgradeScalingTraits!.Single();
            Require(abilityScaling is { Stat: "Damage", AmountPerStat: 1, Restriction: 0 } &&
                abilityScaling.Query.Type == "StatusEffectCountInTargetRoom" && abilityScaling.Query.Duration == "ThisTurn" &&
                abilityScaling.Query.CurrentCostCardModifiersOnly && abilityScaling.Query.CurrentCostSourceIsSpell &&
                abilityScaling.Query.StatusIds?.SequenceEqual(["pyregel"]) == true,
                "Pyreblooded's inactive unit ability must retain the native +1 Damage-per-pyregel scaling query and Spell source flag.");
        }
        if (expectedId == KinhostCarapaceId)
        {
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "Kinhost Carapace", "KinhostCarapace", "MonsterKinhostCarapace", 2, 10, 10, 3, 2,
                "b1d96383-909d-4df1-8942-297454910820");
            ScalingUnitUpgradeTrait[] abilityScaling = template.Ability!.CardCreation!.UnitUpgradeScalingTraits!.ToArray();
            bool MatchesCorruptionScaling(ScalingUnitUpgradeTrait trait, string stat) => trait.Stat == stat &&
                trait.AmountPerStat == 2 && trait.Restriction == 0 && trait.Query.Type == "CorruptionInTargetRoom" &&
                trait.Query.Duration == "ThisTurn" && trait.Query.CurrentCostCardModifiersOnly &&
                trait.Query.CurrentCostSourceIsSpell;
            Require(abilityScaling.Length == 2 && abilityScaling.Count(trait => MatchesCorruptionScaling(trait, "Damage")) == 1 &&
                abilityScaling.Count(trait => MatchesCorruptionScaling(trait, "Health")) == 1,
                "Kinhost Carapace's inactive ability must retain its native +2 Damage and Health per Corruption queries with Spell-source flags.");
        }
        if (expectedId == MeathookId)
        {
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "Meathook", "Meathook", "MonsterMeathook", 1, 25, 15, 2, 2,
                "a7dac277-0718-442d-8903-e21187ff56e6");
        }
        if (expectedId == RailgunId)
        {
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "Railgun", "Railgun", "MonsterRailgun", 1, 20, 5, 2, 1,
                "15e01480-4faf-429e-ad21-71190a3d3106");
            ScalingStatusTrait abilityScaling = template.Ability!.CardCreation!.StatusScalingTraits!.Single();
            Require(abilityScaling.StacksPerStat == 1 && !abilityScaling.OnlyWhenSourceZero && abilityScaling.Filter == 0 &&
                abilityScaling.StatusIds.SequenceEqual(["burst"]) && abilityScaling.Query.Type == "PlayedCost" &&
                abilityScaling.Query.Duration == "ThisTurn" && abilityScaling.Query.SourceRawCost == 0 &&
                abilityScaling.Query.VariableCost && abilityScaling.Query.CurrentCostCardModifiersOnly &&
                abilityScaling.Query.CurrentCostSourceIsSpell,
                "Railgun's inactive unit ability must preserve its native +1 Burst-per-PlayedCost query with the Spell source flag.");
        }
        if (expectedId == SilentSentinelId)
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "Silent Sentinel", "SilentSentinel", "MonsterSilentSentinel", 1, 10, 40, 2, 3,
                "7617850d-a72f-46de-87e4-dbcaa50fe1c2");
        if (expectedId == SpikedriverFactoryId)
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "Spikedriver Factory", "SpikedriverFactory", "MonsterSpikedriverFactory", 1, 0, 50, 3, 2,
                "798142fe-fdf3-4f4f-8af7-dbc5dbc333c6");
        if (expectedId == ScrivenerId)
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "Scrivener", "Scrivener", "Scrivener", 2, 15, 8, 1, 2,
                "c9992436-99b3-45bd-b4c4-5305ab00d0d6");
        if (expectedId == BanishedChamp2Id)
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "Banished Champion 2", "BanishedChamp2", "MonsterBanishedChampion2", 0, 4, 6, 2, 2,
                "dc3337da-96a6-469b-88fc-561c588dc3bb");
        if (expectedId == PyreborneChamp1Id)
            CheckUnactivatedUnitAbilityLifecycle(root, ids, actionIndex, rule, template, spawnedUnit,
                "Pyreborne Champion 1", "PyreborneChamp1", "MonsterChampionPyreborne1", 0, 15, 20, 3, 3,
                "c12a58a4-0bae-4120-9921-f93d91b20bcb");
        if (expectedId == EelGorgonId)
        {
            CombatTrigger postCombat = template.Triggers.Single(trigger => trigger.Kind == "PostCombat");
            int[] categories = postCombat.Effects.Select(effect => effect.Action?.Value ?? -1).ToArray();
            Require(rule.AssetKey == "EelGorgon" && template.AssetKey == "MonsterEelGorgon" && template.Health == 5 &&
                template.MaxHealth == 5 && template.Modifiers.AttackDamage == 10 && template.Size == 2 && rule.Cost == 2 &&
                postCombat.Effects.Count == 3 && categories.SequenceEqual([2, 1, 0]) && postCombat.Effects.All(effect =>
                    effect.Type == "CardEffectRemoveAllStatusEffects" && effect.Action is { Type: "RemoveAllStatusEffects", Target: "Self", AllowPlayer: true, AllowEnemy: false,
                        RemoveHalfStatusEffects: false, Range: null } && effect.Action.Statuses.Count == 0),
                "Eel Gorgon must retain its native 10/5 size-2 birth and self-only PostCombat cleanse definitions for Positive, Negative and Persistent statuses.");
        }
        if (expectedId == EntombedExplosiveId)
        {
            CombatTrigger onDeath = template.Triggers.Single(trigger => trigger.Kind == "OnDeath");
            CombatEffect damage = onDeath.Effects.Single();
            Require(rule.AssetKey == "EntombedExplosive" && template.AssetKey == "MonsterTombExploding" &&
                rule.Cost == 1 && template.Health == 1 && template.MaxHealth == 1 && template.Modifiers.AttackDamage == 0 && template.Size == 1 &&
                damage.Type == "CardEffectDamage" && damage.Value == 65 && damage.Action is { Type: "Damage", Target: "FrontInRoom", Value: 65,
                    AllowEnemy: true, AllowPlayer: false, Range: null } && damage.Action.Statuses.Count == 0 &&
                !onDeath.HasTriggered && spawnedUnit.Health == 1 && !spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnDeath").HasTriggered,
                "Entombed Explosive must preserve its native 1-cost 0/1 birth and untriggered 65 FrontInRoom OnDeath damage definition.");
        }
        if (expectedId == OverworkedAssistantId)
        {
            CombatTrigger spellPlayed = template.Triggers.Single(trigger => trigger.Kind == "CardSpellPlayed");
            CombatEffect unstable = spellPlayed.Effects.Single();
            Require(rule.AssetKey == "OverworkedAssistant" && template.AssetKey == "MonsterLowRankedAssistant" &&
                rule.Cost == 0 && template.Health == 9 && template.MaxHealth == 9 && template.Modifiers.AttackDamage == 3 && template.Size == 1 &&
                unstable.Type == "CardEffectAddStatusEffect" && unstable.Action is { Type: "AddStatus", Target: "Self" } &&
                unstable.Action.Statuses.Single() is { Id: "unstable", Stacks: 4 } && !spellPlayed.HasTriggered &&
                !spawnedUnit.Triggers.Single(trigger => trigger.Kind == "CardSpellPlayed").HasTriggered && spawnedUnit.Status("unstable") == null,
                "Overworked Assistant must preserve its native zero-cost 3/9 spawn and untriggered CardSpellPlayed +4 Unstable definition.");
        }
        if (expectedId == CrucibleCollectorId)
        {
            CombatTrigger onFeed = template.Triggers.Single(trigger => trigger.Kind == "OnFeed");
            CombatEffect lifesteal = onFeed.Effects.Single();
            Require(rule.AssetKey == "CrucibleCollector" && template.AssetKey == "MonsterDrainingHunter" &&
                rule.Cost == 1 && template.Health == 35 && template.MaxHealth == 35 && template.Modifiers.AttackDamage == 30 && template.Size == 2 &&
                lifesteal.Type == "CardEffectAddStatusEffect" && lifesteal.Action is { Type: "AddStatus", Target: "Self" } &&
                lifesteal.Action.Statuses.Single() is { Id: "lifesteal", Stacks: 1 } && !onFeed.HasTriggered &&
                !spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnFeed").HasTriggered && spawnedUnit.Status("lifesteal") == null,
                "Crucible Collector must preserve its native 1-cost 30/35 size-2 birth and untriggered OnFeed +1 Lifesteal definition.");
        }
        if (expectedId == CrucibleWardenId)
        {
            CombatTrigger onFeed = template.Triggers.Single(trigger => trigger.Kind == "OnFeed");
            CombatEffect damageShield = onFeed.Effects.Single();
            Require(rule.AssetKey == "CrucibleWarden" && template.AssetKey == "MonsterDrainingSalvager" &&
                rule.Cost == 1 && template.Health == 30 && template.MaxHealth == 30 && template.Modifiers.AttackDamage == 20 && template.Size == 2 &&
                damageShield.Type == "CardEffectAddStatusEffect" && damageShield.Action is { Type: "AddStatus", Target: "Self" } &&
                damageShield.Action.Statuses.Single() is { Id: "damage shield", Stacks: 1 } && !onFeed.HasTriggered &&
                !spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnFeed").HasTriggered && spawnedUnit.Status("damage shield") == null,
                "Crucible Warden must preserve its native 1-cost 20/30 size-2 birth and untriggered OnFeed +1 Damage Shield definition.");
        }
        if (expectedId == MakeshiftGolemId)
        {
            CombatTrigger onDeath = template.Triggers.Single(trigger => trigger.Kind == "OnDeath");
            CombatEffect armor = onDeath.Effects.Single();
            Require(rule.AssetKey == "MakeshiftGolem" && template.AssetKey == "MonsterGolem" &&
                rule.Cost == 1 && template.Health == 12 && template.MaxHealth == 12 && template.Modifiers.AttackDamage == 12 && template.Size == 2 &&
                armor.Type == "CardEffectAddStatusEffect" && armor.Action is { Type: "AddStatus", Target: "Room" } &&
                armor.Action.Statuses.Single() is { Id: "armor", Stacks: 12 } && !onDeath.HasTriggered &&
                !spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnDeath").HasTriggered &&
                template.Status("armor")?.Stacks == 12 && spawnedUnit.Status("armor")?.Stacks == 12,
                "Makeshift Golem must preserve its native 1-cost 12/12 size-2 birth and untriggered OnDeath room +12 Armor definition.");
        }
        if (expectedId == DeathcappedProphetId)
        {
            CombatTrigger monsterPlayed = template.Triggers.Single(trigger => trigger.Kind == "CardMonsterPlayed");
            CombatEffect draw = monsterPlayed.Effects.Single();
            Require(rule.AssetKey == "DeathcappedProphet" && template.AssetKey == "MonsterDeathcappedProphet" &&
                rule.Cost == 1 && template.Health == 8 && template.MaxHealth == 8 && template.Modifiers.AttackDamage == 3 && template.Size == 1 &&
                draw.Type == "CardEffectDrawAdditionalNextTurn" && draw.Value == 1 &&
                draw.Action is { Type: "DrawNextTurn", Target: "Self", Value: 1, AllowEnemy: true, AllowPlayer: true } &&
                !monsterPlayed.HasTriggered && !spawnedUnit.Triggers.Single(trigger => trigger.Kind == "CardMonsterPlayed").HasTriggered &&
                unitBefore.DrawPerTurn == unitAfter.DrawPerTurn,
                "Deathcapped Prophet must preserve its native 1-cost 3/8 size-1 birth and inactive CardMonsterPlayed +1 DrawNextTurn definition.");
        }
        if (expectedId == ParrafinEnforcerId)
        {
            CombatTrigger onAttacking = template.Triggers.Single(trigger => trigger.Kind == "OnAttacking");
            CombatEffect buff = onAttacking.Effects.Single();
            bool triggeredInNativeTurn = root.GetProperty("Turns").EnumerateArray().Any(row =>
                row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit =>
                    unit.SpawnerCardId == ids[actionIndex] && unit.Triggers.Single(trigger => trigger.Kind == "OnAttacking") is
                        { HasTriggered: true, FireCount: 1 }));
            Require(rule.AssetKey == "ParrafinEnforcer" && template.AssetKey == "ParrafinEnforcer" &&
                rule.Cost == 1 && template.Health == 25 && template.MaxHealth == 25 && template.Modifiers.AttackDamage == 15 && template.Size == 2 &&
                buff.Type == "CardEffectAddStatusEffect" && buff.Action is { Type: "AddStatus", Target: "Room" } &&
                buff.Action.Statuses.Single() is { Id: "buff", Stacks: 3 } && triggeredInNativeTurn,
                "Parrafin Enforcer must preserve its native 1-cost 15/25 size-2 birth and positively trigger OnAttacking room +3 buff once.");
        }
        if (expectedId == SycophantId)
        {
            CombatTrigger onAttacking = template.Triggers.Single(trigger => trigger.Kind == "OnAttacking");
            CombatEffect pyregel = onAttacking.Effects.Single();
            bool triggeredInNativeTurn = root.GetProperty("Turns").EnumerateArray().Any(row =>
                row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit =>
                    unit.SpawnerCardId == ids[actionIndex] && unit.Triggers.Single(trigger => trigger.Kind == "OnAttacking") is
                        { HasTriggered: true, FireCount: 1 }));
            BattleTurnState finalTurn = root.GetProperty("Turns").EnumerateArray().Last().GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool pyregelPersists = finalTurn.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Status("pyregel")?.Stacks is > 0);
            Require(rule.AssetKey == "Sycophant" && template.AssetKey == "MonsterSycophant" &&
                rule.Cost == 1 && template.Health == 25 && template.MaxHealth == 25 && template.Modifiers.AttackDamage == 10 && template.Size == 2 &&
                pyregel.Type == "CardEffectAddStatusEffect" && pyregel.Action is { Type: "AddStatus", Target: "LastAttackedCharacter" } &&
                pyregel.Action.Statuses.Single() is { Id: "pyregel", Stacks: 8, ParamInt: 1 } && triggeredInNativeTurn && !pyregelPersists,
                "Sycophant must preserve its native 1-cost 10/25 size-2 birth, single OnAttacking Pyregel callback and no lingering end-turn Pyregel status.");
        }
        if (expectedId == VotivaryId)
        {
            CombatTrigger onDeath = template.Triggers.Single(trigger => trigger.Kind == "OnDeath");
            CombatEffect draw = onDeath.Effects.Single();
            bool remainsUntriggered = root.GetProperty("Turns").EnumerateArray().All(row =>
                row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Where(unit => unit.SpawnerCardId == ids[actionIndex])
                    .All(unit => !unit.Triggers.Single(trigger => trigger.Kind == "OnDeath").HasTriggered));
            Require(rule.AssetKey == "Votivary" && template.AssetKey == "Votivary" &&
                rule.Cost == 0 && template.Health == 1 && template.MaxHealth == 1 && template.Modifiers.AttackDamage == 1 && template.Size == 1 &&
                draw.Type == "CardEffectDrawAdditionalNextTurn" && draw.Value == 2 &&
                draw.Action is { Type: "DrawNextTurn", Target: "Room", Value: 2, AllowEnemy: false, AllowPlayer: false } &&
                !onDeath.HasTriggered && remainsUntriggered && unitBefore.DrawPerTurn == unitAfter.DrawPerTurn,
                "Votivary must preserve its native zero-cost 1/1 size-1 birth and inactive OnDeath room +2 DrawNextTurn definition.");
        }
        if (expectedId == BountyStalkerId)
        {
            CombatTrigger onDeath = template.Triggers.Single(trigger => trigger.Kind == "OnDeath");
            CombatEffect upgradeEffect = onDeath.Effects.Single();
            CardActionEffect upgrade = upgradeEffect.UnitUpgrade!;
            bool onDeathAppliedUpgrade = root.GetProperty("Turns").EnumerateArray().Any(row =>
            {
                BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
                BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CombatUnit? dying = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]);
                if (dying == null) return false;
                CombatTrigger death = dying.Triggers.Single(trigger => trigger.Kind == "OnDeath");
                CardInstanceState beforeCard = before.Spawn.Train.Context!.FindCard(ids[actionIndex])!;
                CardInstanceState afterCard = after.Spawn.Train.Context!.FindCard(ids[actionIndex])!;
                bool removed = after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != dying.Id);
                return dying.Health == 5 && !death.HasTriggered && death.FireCount == 1 && removed &&
                    afterCard.Permanent.Upgrades.Count == beforeCard.Permanent.Upgrades.Count + 1 &&
                    afterCard.Permanent.Upgrades.Any(item => item.AssetKey == "Plus10Attack" && item.Stats.Damage == 10);
            });
            Require(rule.AssetKey == "BountyStalker" && template.AssetKey == "BountyStalker" &&
                rule.Cost == 1 && template.Health == 5 && template.MaxHealth == 5 && template.Modifiers.AttackDamage == 30 && template.Size == 2 &&
                upgradeEffect.Type == "CardEffectAddCardUpgradeToUnits" && upgrade.Type == "UnitUpgrade" && upgrade.Target == "Self" &&
                upgrade.Lifetime == "Permanent" && upgrade.Upgrade is { AssetKey: "Plus10Attack" } && upgrade.Upgrade.Stats.Damage == 10 &&
                !onDeath.HasTriggered && onDeathAppliedUpgrade,
                "Bounty Stalker must preserve its native 1-cost 30/5 size-2 birth and apply its permanent self +10 Attack upgrade when it dies.");
        }
        if (expectedId == PlagueDoctorId)
        {
            CombatTrigger beforeDamage = template.Triggers.Single(trigger => trigger.Kind == "OnAttackingBeforeDamage");
            CombatEffect unstable = beforeDamage.Effects.Single();
            CardSourceTrait source = unitCard.MaskDescriptor!.SourceTraits!.Single();
            ScalingStatusTrait scaling = unitCard.StatusScalingTraits!.Single();
            bool triggeredInNativeTurn = root.GetProperty("Turns").EnumerateArray().Any(row =>
                row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit =>
                    unit.SpawnerCardId == ids[actionIndex] && unit.Triggers.Single(trigger => trigger.Kind == "OnAttackingBeforeDamage") is
                        { HasTriggered: true, FireCount: 1 }));
            Require(rule.AssetKey == "PlagueDoctor" && template.AssetKey == "MonsterPlagueDoctor" &&
                rule.Cost == 2 && template.Health == 9 && template.MaxHealth == 9 && template.Modifiers!.AttackDamage == 9 && template.Size == 1 &&
                source.Type == "CardTraitScalingAddStatusEffect" && source.Int == 1 && source.Int2 == 0 && source.Int3 == 0 && source.Bool &&
                source.Str == "" && source.Float == 0 && !source.Scaling && source.TrackedValue == "LastAttackDamageDealt" &&
                source.EntryDuration == "ThisTurn" && source.StatusEffectIds!.SequenceEqual(["unstable"]) &&
                scaling.Query.Type == "LastAttackDamageDealt" && scaling.Query.Duration == "ThisTurn" && scaling.StacksPerStat == 1 &&
                scaling.OnlyWhenSourceZero && scaling.Filter == 0 && scaling.StatusIds.SequenceEqual(["unstable"]) &&
                beforeDamage.FireCount == 1 && !beforeDamage.Once && unstable.Type == "CardEffectAddStatusEffect" &&
                unstable.Action is { Type: "AddStatus", Target: "LastAttackedCharacter" } &&
                unstable.Action.Statuses.Single() is { Id: "unstable", Stacks: 0 } && triggeredInNativeTurn,
                "Plague Doctor must preserve its native 2-cost 9/9 size-1 birth, exact zero-only LastAttackDamageDealt Unstable source trait and positively fired pre-damage callback.");
        }
        if (expectedId == HulkingMassId)
        {
            CombatTrigger reanimated = template.Triggers.Single(trigger => trigger.Kind == "OnReanimated");
            CombatEffect armor = reanimated.Effects.Single();
            bool remainsUntriggered = root.GetProperty("Turns").EnumerateArray().SelectMany(row =>
                row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms.SelectMany(room => room.Units))
                .Where(unit => unit.SpawnerCardId == ids[actionIndex]).Any() && root.GetProperty("Turns").EnumerateArray().All(row =>
                    row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms.SelectMany(room => room.Units)
                        .Where(unit => unit.SpawnerCardId == ids[actionIndex]).All(unit => !unit.Triggers.Single(trigger => trigger.Kind == "OnReanimated").HasTriggered));
            Require(rule.AssetKey == "HulkingMass" && template.AssetKey == "MonsterHulkingMass" && rule.Cost == 2 &&
                template.Modifiers!.AttackDamage == 15 && template.Health == 20 && template.MaxHealth == 20 && template.Size == 3 &&
                template.Status("armor")?.Stacks == 40 && spawnedUnit.Status("armor")?.Stacks == 40 &&
                reanimated.Effects.Count == 1 && !reanimated.Once && armor.Type == "CardEffectAddStatusEffect" &&
                armor.Action is { Type: "AddStatus", Target: "Self" } && armor.Action.Statuses.Single() is { Id: "armor", Stacks: 40 } &&
                remainsUntriggered,
                "Hulking Mass must preserve its native 2-cost 15/20 size-3, 40-Armor birth and untriggered OnReanimated +40 Armor callback.");
        }
        if (expectedId == ReformingOozeId)
        {
            CombatTrigger reanimated = template.Triggers.Single(trigger => trigger.Kind == "OnReanimated");
            CombatEffect upgradeEffect = reanimated.Effects.Single();
            CardActionEffect upgrade = upgradeEffect.UnitUpgrade!;
            bool remainsUntriggered = root.GetProperty("Turns").EnumerateArray().SelectMany(row =>
                row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms.SelectMany(room => room.Units))
                .Where(unit => unit.SpawnerCardId == ids[actionIndex]).Any() && root.GetProperty("Turns").EnumerateArray().All(row =>
                    row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms.SelectMany(room => room.Units)
                        .Where(unit => unit.SpawnerCardId == ids[actionIndex]).All(unit => !unit.Triggers.Single(trigger => trigger.Kind == "OnReanimated").HasTriggered));
            Require(rule.AssetKey == "ReformingOoze" && template.AssetKey == "MonsterReformingOoze" && rule.Cost == 1 &&
                template.Modifiers!.AttackDamage == 7 && template.Health == 15 && template.MaxHealth == 15 && template.Size == 2 &&
                template.Status("undying")?.Stacks == 1 && spawnedUnit.Status("undying")?.Stacks == 1 &&
                !reanimated.Once && upgradeEffect.Type == "CardEffectAddCardUpgradeToUnits" && upgrade.Type == "UnitUpgrade" &&
                upgrade.Target == "Self" && upgrade.Lifetime == "Permanent" && upgrade.ScaleUpgrade &&
                upgrade.Upgrade is { AssetKey: "ReformingOoze_Upgrade" } && upgrade.Upgrade.Stats.Damage == 6 &&
                upgrade.Upgrade.Stats.Health == 0 && remainsUntriggered,
                "Reforming Ooze must preserve its native 1-cost 7/15 size-2 Undying birth and inactive permanent self +6 Attack OnReanimated upgrade.");
        }
        if (expectedId == RubbleRaiserId)
        {
            CombatTrigger onEaten = template.Triggers.Single(trigger => trigger.Kind == "OnEaten");
            CombatEffect energyEffect = onEaten.Effects.Single(effect => effect.Type == "CardEffectGainEnergy");
            CombatEffect upgradeEffect = onEaten.Effects.Single(effect => effect.Type == "CardEffectAddTempCardUpgradeToUnits");
            CardActionEffect energy = energyEffect.Action!;
            CardActionEffect upgrade = upgradeEffect.UnitUpgrade!;
            Require(rule.AssetKey == "RubbleRaiser" && template.AssetKey == "MorselRubbleRaiser" && rule.Cost == 0 &&
                template.Modifiers!.AttackDamage == 0 && template.Health == 1 && template.MaxHealth == 1 && template.Size == 1 &&
                template.Subtypes.Contains("SubtypesData_Snack") && !onEaten.Once && onEaten.Effects.Count == 2 &&
                energyEffect.Value == 1 && energy is { Type: "GainEnergy", Target: "Room", Value: 1 } &&
                upgrade.Type == "UnitUpgrade" && upgrade.Target == "LastFeederCharacter" && upgrade.Lifetime == "TemporaryUntilEndOfBattle" &&
                upgrade.Upgrade is { DataId: "ae726e44-0b89-4455-9835-bfa55d2ef6fe", AssetKey: "RubbleMorselTrigger" } &&
                upgrade.Upgrade.Stats.Damage == 3 && upgrade.Upgrade.Stats.Health == 3 && !onEaten.HasTriggered &&
                !spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnEaten").HasTriggered && spawnedUnit.LastFeederId == 0,
                "Rubble Raiser must preserve its native zero-cost 0/1 Snack birth and exact inactive OnEaten energy/LastFeeder upgrade definition.");
        }
        if (expectedId == NightingaleId)
        {
            CombatTrigger onMoonShade = template.Triggers.Single(trigger => trigger.Kind == "OnMoonShade");
            CombatTrigger onMoonshadeLost = template.Triggers.Single(trigger => trigger.Kind == "OnMoonshadeLost");
            CardActionEffect addUpgrade = onMoonShade.Effects.Single().UnitUpgrade!;
            CardActionEffect removeUpgrade = onMoonshadeLost.Effects.Single().UnitUpgrade!;
            FixtureValue[] turnRows = root.GetProperty("Turns").EnumerateArray().ToArray();
            BattleTurnState[] actualTurns = turnRows.Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
            CombatUnit[] unitTurns = actualTurns.Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex])).Where(unit => unit != null).Select(unit => unit!).ToArray();
            bool HasUpgrade(CombatUnit unit) => unit.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "Nightingale_Upgrade");
            bool addedOnMoonShade = turnRows.Any(row =>
            {
                BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
                BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CombatUnit? beforeUnit = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]);
                CombatUnit? afterUnit = after.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]);
                return beforeUnit != null && afterUnit != null && !HasUpgrade(beforeUnit) && HasUpgrade(afterUnit) &&
                    afterUnit.Triggers.Single(trigger => trigger.Kind == "OnMoonShade").HasTriggered;
            });
            bool removedOnMoonshadeLost = turnRows.Any(row =>
            {
                BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
                BattleTurnState after = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                CombatUnit? beforeUnit = before.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]);
                CombatUnit? afterUnit = after.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]);
                return beforeUnit != null && afterUnit != null && HasUpgrade(beforeUnit) && !HasUpgrade(afterUnit) &&
                    afterUnit.Triggers.Single(trigger => trigger.Kind == "OnMoonshadeLost").HasTriggered;
            });
            Require(rule.AssetKey == "Nightingale" && template.AssetKey == "MonsterNightshadow" && rule.Cost == 1 &&
                template.Modifiers!.AttackDamage == 10 && template.Health == 5 && template.MaxHealth == 5 && template.Size == 2 &&
                template.Status("stealth")?.Stacks == 2 && !onMoonShade.Once && !onMoonshadeLost.Once &&
                onMoonShade.Effects.Single().Type == "CardEffectAddTempCardUpgradeToUnits" &&
                addUpgrade is { Type: "UnitUpgrade", Target: "Self", Lifetime: "TemporaryUntilUnitDeath" } &&
                addUpgrade.Upgrade is { DataId: "ed61942e-8c6c-400d-a61f-3d60e792a085", AssetKey: "Nightingale_Upgrade" } &&
                addUpgrade.Upgrade.Statuses.Single() is { Id: "multistrike", Stacks: 2, ParamInt: 2 } &&
                onMoonshadeLost.Effects.Single().Type == "CardEffectRemoveTempUpgradeFromUnit" &&
                removeUpgrade is { Type: "RemoveUnitUpgrade", Target: "Self", Lifetime: "TemporaryUntilEndOfBattle" } &&
                removeUpgrade.Upgrade?.DataId == addUpgrade.Upgrade.DataId &&
                unitTurns.Length == 4 && unitTurns.Select(HasUpgrade).SequenceEqual([true, false, true, true]) &&
                addedOnMoonShade && removedOnMoonshadeLost,
                "Nightingale must preserve its native 1-cost Nightshadow birth and execute the +2 Multistrike moonshade add/remove callbacks across repeated phases.");
        }
        if (expectedId == HostguardId)
        {
            CombatTrigger onSpawn = template.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
            CombatTrigger spawnedOnSpawn = spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
            CombatEffect availableEffect = onSpawn.Effects.Single(effect => effect.Action?.Type == "AddStatus");
            CombatEffect resetEffect = onSpawn.Effects.Single(effect => effect.Action?.Type == "ResetCooldown");
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            const string abilityId = "126787b8-2d30-4c9c-bd3f-d6ec936aa8d6";
            Require(rule.AssetKey == "Hostguard" && template.AssetKey == "MonsterHostguard" && rule.Cost == 3 &&
                template.Modifiers!.AttackDamage == 25 && template.Health == 60 && template.MaxHealth == 60 && template.Size == 3 &&
                template.Ability is { DataId: abilityId, Cooldown: 3, CooldownAtSpawn: 0, HasAbility: true } &&
                !onSpawn.Once && onSpawn.IgnoreSilence && onSpawn.Effects.Count == 2 && spawnedOnSpawn.HasTriggered &&
                availableEffect.Action is { Type: "AddStatus", Target: "Self" } &&
                availableEffect.Action.Statuses.Single() is { Id: "unit_ability_available", Stacks: 1, ParamInt: 1 } &&
                resetEffect.Action is { Type: "ResetCooldown", Target: "Self", CooldownParameter: true } &&
                spawnedUnit.Status("unit_ability_available") is { Stacks: 1, ParamInt: 1 } &&
                spawnedUnit.Ability is { DataId: abilityId, Cooldown: 3, CooldownAtSpawn: 0, HasAbility: true } &&
                unitTurns.Length == 4 && unitTurns.All(unit => unit.Ability is
                    { DataId: abilityId, Cooldown: 3, CooldownAtSpawn: 0, HasAbility: true } &&
                    unit.Status("unit_ability_available") is { Stacks: 1, ParamInt: 1 } &&
                    !unit.Triggers.Single(trigger => trigger.Kind == "OnOwnAbilityActivated").HasTriggered),
                "Hostguard must preserve its native 3-cost 25/60 size-3 birth, fire OnSpawn to expose/reset its cooldown-3 ability and leave it unused.");
        }
        if (expectedId == WeldlingId)
        {
            CombatTrigger postCombat = template.Triggers.Single(trigger => trigger.Kind == "PostCombat");
            CombatEffect armorEffect = postCombat.Effects.Single();
            CardActionEffect armor = armorEffect.Action!;
            FixtureValue[] turnRows = root.GetProperty("Turns").EnumerateArray().ToArray();
            BattleTurnState[] actualTurns = turnRows.Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
            CombatUnit[] unitTurns = actualTurns.Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex])).Where(unit => unit != null).Select(unit => unit!).ToArray();
            int StewardArmor(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.SpawnerCardId == ids[0]).Status("armor")?.Stacks ?? 0;
            bool armorGainedTwice = new[] { 1, 3 }.All(index =>
            {
                BattleTurnState before = turnRows[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
                BattleTurnState after = turnRows[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
                return StewardArmor(after) - StewardArmor(before) == 3;
            });
            BattleTurnState unchangedBefore = turnRows[2].GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState unchangedAfter = turnRows[2].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(rule.AssetKey == "Weldling" && template.AssetKey == "MonsterWeldling" && rule.Cost == 2 &&
                template.Modifiers!.AttackDamage == 6 && template.Health == 3 && template.MaxHealth == 3 && template.Size == 1 &&
                template.Subtypes.Contains("SubtypesData_Machine") && !postCombat.Once && postCombat.SkipDuringDeployment is true &&
                !postCombat.HasTriggered && armorEffect.Type == "CardEffectAddStatusEffectPerSelfAttack" && armorEffect.Value == 2 &&
                armorEffect.Count == 3 && armor is { Type: "AddStatus", Target: "FrontInRoom", AllowEnemy: false, AllowPlayer: true } &&
                armor.Statuses.Single() is { Id: "armor", Stacks: 0, ParamInt: 1, RemoveWhenTriggered: true, RemoveDuringDeployment: true } &&
                unitTurns.Length == 4 && unitTurns.All(unit => unit.Health == 3 && unit.Modifiers!.AttackDamage == 6 &&
                    unit.Triggers.Single(trigger => trigger.Kind == "PostCombat").HasTriggered) && armorGainedTwice &&
                StewardArmor(unchangedBefore) == StewardArmor(unchangedAfter),
                "Weldling must preserve its native 2-cost 6/3 Machine birth and show two +3 FrontInRoom armor gains with an unchanged control.");
        }
        if (expectedId == ShatteredShellId)
        {
            CombatTrigger onKill = template.Triggers.Single(trigger => trigger.Kind == "OnKill");
            CombatEffect upgradeEffect = onKill.Effects.Single();
            CardActionEffect upgrade = upgradeEffect.UnitUpgrade!;
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            int[] attacks = unitTurns.Select(unit => unit.Modifiers!.AttackDamage).ToArray();
            int[] upgradesPerTurn = unitTurns.Select(unit => unit.Modifiers!.Upgrades.Count(item =>
                item.DataId == "214658eb-db42-4734-879c-ff7414d8f21b" && item.Stats.Damage == 3)).ToArray();
            Require(rule.AssetKey == "ShatteredShell" && template.AssetKey == "ShatteredShell" && rule.Cost == 1 &&
                template.Modifiers!.AttackDamage == 3 && template.Health == 10 && template.MaxHealth == 10 && template.Size == 2 &&
                template.Subtypes.Contains("SubtypesData_Animus_c0f95969-ae86-4477-9458-57bb235d965b") &&
                template.Subtypes.Contains("SubtypesData_BannerUnit") && !onKill.Once && onKill.FireCount == 1 && !onKill.HasTriggered &&
                upgradeEffect.Type == "CardEffectAddTempCardUpgradeToUnits" && upgrade.Type == "UnitUpgrade" &&
                upgrade.Target == "Self" && upgrade.Lifetime == "TemporaryUntilEndOfBattle" &&
                upgrade.Upgrade is { DataId: "214658eb-db42-4734-879c-ff7414d8f21b", AssetKey: "ShatteredShellTrigger" } &&
                upgrade.Upgrade.Stats.Damage == 3 && upgrade.Upgrade.Stats.Health == 0 && unitTurns.Length == 4 &&
                attacks.SequenceEqual([6, 9, 9, 9]) && upgradesPerTurn.SequenceEqual([1, 2, 2, 2]) &&
                unitTurns.All(unit => unit.Health == 10 && unit.Triggers.Single(trigger => trigger.Kind == "OnKill").HasTriggered),
                "Shattered Shell must preserve its native 1-cost 3/10 size-2 birth and positively apply two self +3 Attack OnKill upgrades, then hold steady.");
        }
        if (expectedId == MoltenEncasementId)
        {
            CombatTrigger onDeath = template.Triggers.Single(trigger => trigger.Kind == "OnDeath");
            CombatEffect stealthEffect = onDeath.Effects.Single();
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == "MoltenEncasement" && template.AssetKey == "MoltenEncasement" && rule.Cost == 1 &&
                template.Modifiers!.AttackDamage == 0 && template.Health == 1 && template.MaxHealth == 1 && template.Size == 1 &&
                template.Subtypes.Contains("SubtypesData_Tomb") && stealthEffect.Type == "CardEffectAddStatusEffect" &&
                stealthEffect.Action is { Type: "AddStatus", Target: "Room", AllowEnemy: false, AllowPlayer: true } &&
                stealthEffect.Action.Statuses.Single() is { Id: "stealth", Stacks: 2, RemoveWhenTriggered: true } &&
                !onDeath.HasTriggered && unitTurns.Length == 4 && unitTurns.All(unit => unit.Health == 1 &&
                    !unit.Triggers.Single(trigger => trigger.Kind == "OnDeath").HasTriggered) && spawnedUnit.Status("stealth") == null,
                "Molten Encasement must preserve its native 1-cost 0/1 Tomb birth and inactive OnDeath room +2 Stealth definition.");
        }
        if (expectedId == AstrologerId)
        {
            CombatTrigger phaseShift = template.Triggers.Single(trigger => trigger.Kind == "OnMoonPhaseShift");
            CombatTrigger spawnedPhaseShift = spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnMoonPhaseShift");
            CombatEffect upgradeEffect = phaseShift.Effects.Single();
            CardActionEffect upgrade = upgradeEffect.UnitUpgrade!;
            int[] phaseAttacks = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!.Modifiers!.AttackDamage).ToArray();
            bool phaseCallbacksFired = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).All(unit => unit!.Triggers.Single(trigger => trigger.Kind == "OnMoonPhaseShift").HasTriggered);
            Require(rule.AssetKey == "Astrologer" && template.AssetKey == "MonsterAstrologer" && rule.Cost == 1 &&
                template.Modifiers!.AttackDamage == 5 && template.Health == 10 && template.MaxHealth == 10 && template.Size == 2 &&
                template.Subtypes.Contains("SubtypesData_Selene") && phaseShift.Effects.Count == 1 && !phaseShift.Once &&
                phaseShift.FireCount == 1 && !phaseShift.HasTriggered && upgradeEffect.Type == "CardEffectAddTempCardUpgradeToUnits" &&
                !spawnedPhaseShift.HasTriggered && spawnedUnit.Modifiers!.AttackDamage == 5 &&
                upgrade is { Type: "UnitUpgrade", Target: "Self", Lifetime: "TemporaryUntilEndOfBattle" } &&
                upgrade.Upgrade is { DataId: "de311466-d8c2-4d46-a2cb-0525fb4f3f01", AssetKey: "AstrologerAttackUpgrade" } &&
                upgrade.Upgrade.Stats.Damage == 5 && phaseAttacks.SequenceEqual([10, 15, 20, 20]) && phaseCallbacksFired,
                "Astrologer must retain its native 1-cost 5/10 size-2 birth and three positive +5 Attack OnMoonPhaseShift callbacks followed by a no-shift control.");
        }
        if (expectedId is SnottyWhelpId or BloatedWhelpId)
        {
            bool bloated = expectedId == BloatedWhelpId;
            string expectedCard = bloated ? "BloatedWhelp" : "SnottyWhelp";
            string expectedUnit = bloated ? "MonsterBloatedWhelp" : "MonsterSneezyWhelp";
            string expectedAbility = bloated ? "46ffe4e9-eb1e-417b-a37a-70d343706bdb" : "f3c7769a-4f9b-43cf-aacd-cf32c14e3082";
            int expectedCooldown = bloated ? 1 : 2;
            CombatTrigger onSpawn = template.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
            CombatTrigger spawnedOnSpawn = spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
            CombatEffect availableEffect = onSpawn.Effects.Single(effect => effect.Action?.Type == "AddStatus");
            CombatEffect resetEffect = onSpawn.Effects.Single(effect => effect.Action?.Type == "ResetCooldown");
            CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
                .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
                .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
                .Where(unit => unit != null).Select(unit => unit!).ToArray();
            Require(rule.AssetKey == expectedCard && template.AssetKey == expectedUnit && rule.Cost == 1 &&
                template.Modifiers!.AttackDamage == 4 && template.Health == 1 && template.MaxHealth == 1 && template.Size == 1 &&
                template.Subtypes.SequenceEqual(["SubtypesData_Whelp"]) && template.Status("unit_ability")?.Stacks == 1 &&
                !onSpawn.Once && onSpawn.IgnoreSilence && onSpawn.Effects.Count == 2 && spawnedOnSpawn.HasTriggered &&
                availableEffect.Action is { Target: "Self" } && availableEffect.Action.Statuses.Single() is { Id: "unit_ability_available", Stacks: 1, ParamInt: 1 } &&
                resetEffect.Type == "CardEffectResetCooldown" && resetEffect.Action is { Target: "Self" } &&
                spawnedUnit.Status("unit_ability_available") is { Stacks: 1, ParamInt: 1 } &&
                template.Ability is { DataId: var abilityId, HasAbility: true } && abilityId == expectedAbility && template.Ability.Cooldown == expectedCooldown &&
                spawnedUnit.Ability is { DataId: var liveAbilityId, HasAbility: true } && liveAbilityId == expectedAbility && spawnedUnit.Ability.Cooldown == expectedCooldown &&
                unitTurns.Length == 4 && unitTurns.All(unit => unit.Ability is { HasAbility: true } && unit.Ability.DataId == expectedAbility &&
                    unit.Ability.Cooldown == expectedCooldown &&
                    unit.Status("unit_ability_available") is { Stacks: 1, ParamInt: 1 } &&
                    !unit.Triggers.Single(trigger => trigger.Kind == "OnOwnAbilityActivated").HasTriggered),
                expectedCard + " must preserve its native 1-cost 4/1 Whelp birth, fire OnSpawn to expose its cooldown-" + expectedCooldown + " ability, and leave that ability unused in the captured turns.");
        }
        if (expectedId == FaeMeddlerId)
        {
            CombatTrigger phaseAdvance = template.Triggers.Single(trigger => trigger.Kind == "OnSpawn" &&
                trigger.Effects.Any(effect => effect.Type == "CardEffectAdvanceMoonPhase"));
            CombatEffect advanceEffect = phaseAdvance.Effects.Single();
            CombatTrigger livePhaseAdvance = spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnSpawn" &&
                trigger.Effects.Any(effect => effect.Type == "CardEffectAdvanceMoonPhase"));
            Require(rule.AssetKey == "FaeMeddler" && template.AssetKey == "MonsterFaeMeddler" && rule.Cost == 1 &&
                template.Modifiers!.AttackDamage == 6 && template.Health == 3 && template.MaxHealth == 3 && template.Size == 1 &&
                template.Ability is { DataId: "d1ab03ed-fe63-4eca-b68e-3090524f325e", Cooldown: 2 } &&
                spawnedUnit.Status("unit_ability_available") is { Stacks: 1, ParamInt: 1 } &&
                !phaseAdvance.Once && advanceEffect.Action is { Type: "AdvanceMoonPhase", Target: "Self", Value: 1 } &&
                !phaseAdvance.HasTriggered && livePhaseAdvance.HasTriggered &&
                unitBefore.Spawn.Train.Context!.QueryFrame!.MoonPhase == 2 && unitAfter.Spawn.Train.Context!.QueryFrame!.MoonPhase == 1,
                "Fae Meddler must preserve its native 1-cost 6/3 birth, available cooldown-2 ability and a positive OnSpawn moon advance from phase 2 to 1.");
        }
        RecordedBattleChecks.Native(root);
        if (expectedId == EelGorgonId)
            Console.WriteLine("ORIGINAL-EEL-GORGON PASS: original 10/5 size-2 paid spawn and three captured self PostCombat remove-all categories; no positive status-removal stimulus was present.");
        else if (expectedId == EntombedExplosiveId)
            Console.WriteLine("ORIGINAL-ENTOMBED-EXPLOSIVE PASS: original 1-cost 0/1 birth and captured 65 FrontInRoom OnDeath damage; the protected unit survived, so damage did not trigger.");
        else if (expectedId == OverworkedAssistantId)
            Console.WriteLine("ORIGINAL-OVERWORKED-ASSISTANT PASS: original zero-cost 3/9 birth and captured CardSpellPlayed +4 Unstable; no spell was played, so the trigger remained inactive.");
        else if (expectedId == CrucibleCollectorId)
            Console.WriteLine("ORIGINAL-CRUCIBLE-COLLECTOR PASS: original 1-cost 30/35 size-2 birth and captured OnFeed +1 Lifesteal; no feed occurred, so the trigger remained inactive.");
        else if (expectedId == CrucibleWardenId)
            Console.WriteLine("ORIGINAL-CRUCIBLE-WARDEN PASS: original 1-cost 20/30 size-2 birth and captured OnFeed +1 Damage Shield; no feed occurred, so the trigger remained inactive.");
        else if (expectedId == MakeshiftGolemId)
            Console.WriteLine("ORIGINAL-MAKESHIFT-GOLEM PASS: original 1-cost 12/12 size-2 birth and captured OnDeath room +12 Armor; the unit survived, so the trigger remained inactive.");
        else if (expectedId == DeathcappedProphetId)
            Console.WriteLine("ORIGINAL-DEATHCAPPED-PROPHET PASS: original 1-cost 3/8 size-1 birth and captured CardMonsterPlayed +1 DrawNextTurn; no later monster play occurred, so the trigger remained inactive.");
        else if (expectedId == ParrafinEnforcerId)
            Console.WriteLine("ORIGINAL-PARRAFIN-ENFORCER PASS: original 1-cost 15/25 size-2 birth and a positive single OnAttacking room +3 buff callback.");
        else if (expectedId == SycophantId)
            Console.WriteLine("ORIGINAL-SYCOPHANT PASS: original 1-cost 10/25 size-2 birth and a single OnAttacking +8 Pyregel definition; the ending snapshot has no lingering Pyregel status.");
        else if (expectedId == VotivaryId)
            Console.WriteLine("ORIGINAL-VOTIVARY PASS: original zero-cost 1/1 size-1 birth and captured OnDeath room +2 DrawNextTurn; no death occurred, so draw remained unchanged.");
        else if (expectedId == BountyStalkerId)
            Console.WriteLine("ORIGINAL-BOUNTY-STALKER PASS: original 1-cost 30/5 size-2 birth; native OnDeath removed the unit and added permanent Plus10Attack (+10) to its card.");
        else if (expectedId == PlagueDoctorId)
            Console.WriteLine("ORIGINAL-PLAGUE-DOCTOR PASS: original 2-cost 9/9 size-1 birth, exact zero-only LastAttackDamageDealt Unstable source trait and a native OnAttackingBeforeDamage fire.");
        else if (expectedId == HulkingMassId)
            Console.WriteLine("ORIGINAL-HULKING-MASS PASS: original 2-cost 15/20 size-3 birth with 40 Armor; its captured OnReanimated +40 Armor callback remained inactive.");
        else if (expectedId == ReformingOozeId)
            Console.WriteLine("ORIGINAL-REFORMING-OOZE PASS: original 1-cost 7/15 size-2 Undying birth; its permanent self +6 Attack OnReanimated upgrade remained inactive.");
        else if (expectedId == AstrologerId)
            Console.WriteLine("ORIGINAL-ASTROLOGER PASS: original 1-cost 5/10 size-2 birth and three native +5 Attack OnMoonPhaseShift upgrades, then a no-shift control.");
        else if (expectedId == SnottyWhelpId)
            Console.WriteLine("ORIGINAL-SNOTTY-WHELP PASS: original 1-cost 4/1 Whelp birth; native OnSpawn grants ability availability and resets cooldown, while its cooldown-2 ability remains unused.");
        else if (expectedId == BloatedWhelpId)
            Console.WriteLine("ORIGINAL-BLOATED-WHELP PASS: original 1-cost 4/1 Whelp birth; native OnSpawn grants ability availability and resets cooldown, while its cooldown-1 ability remains unused.");
        else if (expectedId == FaeMeddlerId)
            Console.WriteLine("ORIGINAL-FAE-MEDDLER PASS: original 1-cost 6/3 birth and positive OnSpawn phase advance 2 to 1; cooldown-2 unit ability remains outside this action witness.");
        else if (expectedId == MoltenEncasementId)
            Console.WriteLine("ORIGINAL-MOLTEN-ENCASEMENT PASS: original 1-cost 0/1 Tomb birth and inactive OnDeath room +2 Stealth definition; no death occurred.");
        else if (expectedId == ShatteredShellId)
            Console.WriteLine("ORIGINAL-SHATTERED-SHELL PASS: original 1-cost 3/10 size-2 birth; two native OnKill events add +3 Attack each and no further upgrades occur.");
        else if (expectedId == WeldlingId)
            Console.WriteLine("ORIGINAL-WELDLING PASS: original 2-cost 6/3 Machine birth; two native PostCombat transitions add +3 Armor to the front ally with an unchanged control.");
        else if (expectedId == HostguardId)
            Console.WriteLine("ORIGINAL-HOSTGUARD PASS: original 3-cost 25/60 size-3 birth; native OnSpawn exposes/resets the cooldown-3 ability, which remains unused.");
        else if (expectedId == RubbleRaiserId)
            Console.WriteLine("ORIGINAL-RUBBLE-RAISER PASS: original zero-cost 0/1 Snack birth and exact inactive OnEaten +1 energy/LastFeeder +3/+3 upgrade definition.");
        else
        Console.WriteLine("ORIGINAL-ISOLATED-UNIT PASS: native " + expectedId + " birth matched its captured original spawn template.");
    }
    private static void CheckUnactivatedUnitAbilityLifecycle(FixtureValue root, int[] ids, int actionIndex,
        CardPlayRule rule, CombatUnit template, CombatUnit spawnedUnit, string displayName, string cardAsset, string unitAsset,
        int cost, int attack, int health, int size, int cooldown, string abilityId)
    {
        CombatTrigger onSpawn = template.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
        CombatTrigger onActivated = template.Triggers.Single(trigger => trigger.Kind == "OnOwnAbilityActivated");
        CombatTrigger availableTrigger = template.Triggers.Single(trigger => trigger.Kind == "OnUnitAbilityAvailable");
        CombatTrigger unavailableTrigger = template.Triggers.Single(trigger => trigger.Kind == "OnUnitAbilityUnavailable");
        CombatTrigger spawnedOnSpawn = spawnedUnit.Triggers.Single(trigger => trigger.Kind == "OnSpawn");
        CardActionEffect available = onSpawn.Effects.Single(effect => effect.Action?.Type == "AddStatus").Action!;
        CardActionEffect reset = onSpawn.Effects.Single(effect => effect.Action?.Type == "ResetCooldown").Action!;
        CardActionEffect activatedReset = onActivated.Effects.Single().Action!;
        CardActionEffect showAvailable = availableTrigger.Effects.Single().Action!;
        CardActionEffect hideAvailable = unavailableTrigger.Effects.Single().Action!;
        CombatUnit[] unitTurns = root.GetProperty("Turns").EnumerateArray()
            .Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!)
            .Select(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .SingleOrDefault(unit => unit.SpawnerCardId == ids[actionIndex]))
            .Where(unit => unit != null).Select(unit => unit!).ToArray();
        bool HasExpectedAbility(CombatUnit unit) => unit.Ability is
            { DataId: var dataId, Cooldown: var actualCooldown, CooldownAtSpawn: 0, HasAbility: true } &&
            dataId == abilityId && actualCooldown == cooldown;
        bool templateHasExpectedAbility = template.Ability is
            { DataId: var dataId, Cooldown: var actualCooldown, CooldownAtSpawn: 0, HasAbility: true } &&
            dataId == abilityId && actualCooldown == cooldown;
        Require(rule.AssetKey == cardAsset && rule.Cost == cost && template.AssetKey == unitAsset &&
            template.Modifiers!.AttackDamage == attack && template.Health == health && template.MaxHealth == health && template.Size == size &&
            templateHasExpectedAbility && !onSpawn.Once && onSpawn.Effects.Count == 2 && spawnedOnSpawn.HasTriggered &&
            available is { Type: "AddStatus", Target: "Self" } &&
            available.Statuses.Single() is { Id: "unit_ability_available", Stacks: 1, ParamInt: 1 } &&
            reset is { Type: "ResetCooldown", Target: "Self", CooldownParameter: true } &&
            activatedReset is { Type: "ResetCooldown", Target: "Self", CooldownParameter: false } &&
            showAvailable is { Type: "AddStatus", Target: "Self" } &&
            showAvailable.Statuses.Single() is { Id: "unit_ability_available", Stacks: 1, ParamInt: 1 } &&
            hideAvailable is { Type: "RemoveStatus", Target: "Self" } &&
            hideAvailable.Statuses.Single().Id == "unit_ability_available" &&
            HasExpectedAbility(spawnedUnit) &&
            spawnedUnit.Status("unit_ability") is { Stacks: 1, ParamInt: 0 } &&
            spawnedUnit.Status("unit_ability_available") is { Stacks: 1, ParamInt: 1 } &&
            unitTurns.Length == 4 && unitTurns.All(unit => HasExpectedAbility(unit) &&
                unit.Status("unit_ability") is { Stacks: 1, ParamInt: 0 } &&
                unit.Status("unit_ability_available") is { Stacks: 1, ParamInt: 1 } &&
                !unit.Triggers.Single(trigger => trigger.Kind == "OnOwnAbilityActivated").HasTriggered),
            $"{displayName} must retain its native {cost}-cost {attack}/{health} birth and unactivated cooldown-{cooldown} ability across four post-deployment snapshots, with calibrated availability callbacks.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
