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
        if (isolatedUnitScenario?.StartsWith(isolatedUnitPrefix, StringComparison.Ordinal) == true)
        {
            string expectedId = isolatedUnitScenario[isolatedUnitPrefix.Length..];
            Require(Guid.TryParseExact(expectedId, "D", out _), "Isolated unit scenario did not identify an original card UUID.");
            NativeIsolatedUnit(root, expectedId);
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
    private static void NativeIsolatedUnit(FixtureValue root, string expectedId, string? expectedAsset = null)
    {
        Require(root.GetProperty("OriginalSupportUnitsPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed isolated original unit witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }) Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);
        int[] ids = root.GetProperty("OriginalSupportUnitsIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length == 4, "Isolated unit witness needs three anchors and one direct unit birth.");
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
        int actionIndex = 3;
        BattleTurnState unitBefore = Before(actionIndex), unitAfter = After(actionIndex);
        PlayCardAction unitAction = actions[actionIndex].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardInstanceState unitCard = unitBefore.Spawn.Train.Context!.FindCard(ids[actionIndex])!;
        CardPlayRule rule = unitBefore.PlayRules!.Cards.Single(item => item.DataId == expectedId);
        CombatUnit template = rule.SpawnUnit!, spawnedUnit = unitAfter.Spawn.Train.Rooms.SelectMany(item => item.Units)
            .Single(candidate => candidate.SpawnerCardId == ids[actionIndex]);
        int appendPosition = unitBefore.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0).Units.Count(candidate => candidate.Team == CombatTeam.Player);
        Require(unitCard.DataId == expectedId && unitAction.RoomIndex == 0 && unitAction.PlayerPosition == appendPosition &&
            unitAction.TargetUnitId == 0 && unitBefore.PlayRules.Rooms.Single(item => item.RoomIndex == 0).PlayerSlots > appendPosition &&
            unitBefore.Energy >= rule.Cost && unitBefore.Energy - unitAfter.Energy == rule.Cost && rule.Effect == "SpawnMonster" &&
            !string.IsNullOrEmpty(rule.AssetKey) && (expectedAsset == null || rule.AssetKey == expectedAsset) &&
            spawnedUnit.AssetKey == template.AssetKey && spawnedUnit.Size == template.Size &&
            spawnedUnit.Health == template.Health && spawnedUnit.MaxHealth == template.MaxHealth && spawnedUnit.Modifiers!.AttackDamage == template.Modifiers!.AttackDamage,
            expectedAsset + " must preserve its captured original card identity, paid spawn and native birth template.");
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
        else
        Console.WriteLine("ORIGINAL-ISOLATED-UNIT PASS: native " + expectedId + " birth matched its captured original spawn template.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
