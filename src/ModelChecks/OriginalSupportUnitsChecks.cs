using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalSupportUnitsChecks
{
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
        RecordedBattleChecks.Native(root);
        if (expectedId == EelGorgonId)
            Console.WriteLine("ORIGINAL-EEL-GORGON PASS: original 10/5 size-2 paid spawn and three captured self PostCombat remove-all categories; no positive status-removal stimulus was present.");
        else if (expectedId == EntombedExplosiveId)
            Console.WriteLine("ORIGINAL-ENTOMBED-EXPLOSIVE PASS: original 1-cost 0/1 birth and captured 65 FrontInRoom OnDeath damage; the protected unit survived, so damage did not trigger.");
        else if (expectedId == OverworkedAssistantId)
            Console.WriteLine("ORIGINAL-OVERWORKED-ASSISTANT PASS: original zero-cost 3/9 birth and captured CardSpellPlayed +4 Unstable; no spell was played, so the trigger remained inactive.");
        else if (expectedId == CrucibleCollectorId)
            Console.WriteLine("ORIGINAL-CRUCIBLE-COLLECTOR PASS: original 1-cost 30/35 size-2 birth and captured OnFeed +1 Lifesteal; no feed occurred, so the trigger remained inactive.");
        else
        Console.WriteLine("ORIGINAL-ISOLATED-UNIT PASS: native " + expectedId + " birth matched its captured original spawn template.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
