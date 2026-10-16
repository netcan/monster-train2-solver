using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalTriggerRelicBatchScenario
    {
        internal const string Name = "original-trigger-relic-batch";
        internal const string Angel = "SubtypesData_Angel";
        internal const string Dragon = "SubtypesData_Dragon";
        internal const string Whelp = "SubtypesData_Whelp";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Stationary => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_TRIGGER_RELIC_BATCH_STATIONARY") == "1";
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        private static string angelCardId = "", whelpCardId = "", woundCardId = "", gearCardId = "", shiftCardId = "";
        internal static readonly (string Key, string Id)[] Artifacts =
        {
            ("EquippedUnitsGainArmor", "9af0364b-8e05-45d9-be46-7b7dd1e721e7"),
            ("DamageShieldOnEquip", "0c50313f-1f07-40c4-8a3e-54748b57981f"),
            ("ManyBuffonAngelEquip", "c97500a9-b0a4-4c18-b203-b9f4447c84c3"),
            ("SurgeonsStitcher", "8f22145d-5c41-45da-8abf-95bc5f369648"),
            ("AngelsSpawnWithShiftyValor", "3d4705ec-ee6b-433b-88c8-1b4ce0ed7cc9"),
            ("RageOnHymnistDeath", "ec46f782-87ad-4cbd-aacb-023701417283"),
            ("ExtinguishSpreadPyregel", "9142aaa7-072e-43ca-bc5e-2c9995061174"),
            ("WhelpsGainExtinguishHealPyre", "e6a167d5-f57e-4a02-a0f0-b8cf9373c838"),
            ("DragonsGainStrikeAddPyregel", "1fdf2fde-a5e7-4eae-86eb-d432145c4621"),
            ("WhelpStrikeAddPyregel_DH", "588825ab-8517-48db-8447-31b48e931aaa")
        };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData[] stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!)
                .Where(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                .Distinct().ToArray();
            if (stewards.Length < 2) throw new InvalidOperationException("Trigger relic batch needs two Steward definitions.");
            angelCardId = stewards[0].GetID(); whelpCardId = stewards[1].GetID();
            for (int index = 0; index < 2; index++)
            {
                CharacterData unit = stewards[index].GetSpawnCharacterData()!;
                AddSubtype(unit, index == 0 ? Angel : Whelp);
                if (index == 0) AddSubtype(unit, Dragon);
                Set(unit, "health", index == 0 ? 80 : 12); Set(unit, "attackDamage", 2); Set(unit, "size", 1);
                Set(unit, "startingStatusEffects", index == 1
                    ? new[] { new StatusEffectStackData { statusId = "pyregel", count = 1 } }
                    : Array.Empty<StatusEffectStackData>());
                Set(unit, "equipmentLimit", 2); Set(stewards[index], "cost", 0);
                var shift = HealingScenario.HealGold(5, false, true); Set(shift, "trigger", CharacterTriggerData.Trigger.OnShift);
                Set(unit, "triggers", unit.GetTriggers().Concat(new[] { shift }).ToList());
            }
            CardData wound = owned.Select(card => data.FindCardData(card.GetCardDataID())!).First(card => card.GetEffects().Any(
                effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter));
            woundCardId = wound.GetID();
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
            damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(damage, "paramInt", 12);
            Set(wound, "effects", new List<CardEffectData> { damage }); Set(wound, "cost", 0);
            CardData shiftCard = owned.Select(card => data.FindCardData(card.GetCardDataID())!).First(card => card.GetEffects().Any(
                effect => effect.GetEffectStateName() == "CardEffectFloorRearrange"));
            shiftCardId = shiftCard.GetID();
            var bump = new CardEffectData("CardEffectBump", null!, Team.Type.Monsters | Team.Type.Heroes);
            bump.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(bump, "paramInt", 1);
            Set(shiftCard, "effects", new List<CardEffectData> { bump }); Set(shiftCard, "cost", 0);
            CardData gear = data.GetAllCardData().First(card => card.GetCardType() == CardType.Equipment &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectAttachEquipment");
            gearCardId = gear.GetID();
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade("PojuTriggerRelicGear", "bad71000-0000-4000-8000-000000000001", 0, 0, 0, 0, "armor", 0);
            Set(upgrade, "statusEffectUpgrades", new List<StatusEffectStackData>());
            var equipped = HealingScenario.HealGold(5, false, true); Set(equipped, "trigger", CharacterTriggerData.Trigger.OnEquipmentAdded);
            Set(upgrade, "triggerUpgrades", new List<CharacterTriggerData> { equipped });
            var attach = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Monsters | Team.Type.Heroes);
            attach.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(attach, "paramCardUpgradeData", upgrade);
            Set(gear, "effects", new List<CardEffectData> { attach }); Set(gear, "traits", new List<CardTraitData>()); Set(gear, "cost", 0);

            // Ordinary enemies are authored Angel carriers, leaving the Boss and
            // native wave schedule intact. This tests the original 255 source team.
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(managers.GetHeroManager());
            var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(save, waves);
            CharacterData[] enemies = waves.SelectMany(wave => ((IEnumerable)AccessTools.Field(typeof(SpawnGroupPoolData), "possibleGroups")
                .GetValue(wave)).Cast<SpawnGroupData>()).SelectMany(group => group.GetCharacters(save.GetCovenantsForSpawnPattern(), save, save.GetGeneratedBoss()))
                .Where(character => !character.IsMiniboss()).GroupBy(character => character.GetID()).Select(group => group.First()).ToArray();
            if (enemies.Length == 0) throw new InvalidOperationException("Trigger relic batch needs an ordinary enemy carrier.");
            foreach (CharacterData enemy in enemies) { AddSubtype(enemy, Angel); Set(enemy, "equipmentLimit", 2); }
            foreach (CardState card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            var handCards = new List<CardData> { stewards[0], stewards[1], wound, wound, gear, gear };
            if (!Stationary) handCards.Add(shiftCard);
            foreach (CardData card in handCards)
                cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
            CardState laterGear = cards.AddNewCard(gear, CardPile.HandPile, false, false, animate: false);
            cards.MoveCardFromHandToTopOfDeck(laterGear);

            foreach (var entry in Artifacts)
            {
                CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == entry.Id);
                if (relic.name != entry.Key || relic.GetEffects().Count != 1 ||
                    relic.GetEffects()[0].GetEffectClassName() != "RelicEffectAddTrigger" || relic.GetEffects()[0].GetEffectConditions().Count != 0)
                    throw new InvalidOperationException("Original trigger relic definition changed: " + entry.Key);
                save.AddRelic(relic);
            }
            // These already accepted originals supply native modifiers. Added
            // Death/Shift callbacks must retain their original no-count flags.
            save.AddRelic(data.GetAllCollectableRelicData().Single(item => item.GetID() == "a5d67620-a9ec-4257-91b5-305336e11987"));
            save.AddRelic(data.GetAllCollectableRelicData().Single(item => item.GetID() == "8c493cd7-8048-4598-bdd5-6cc2801cd842"));
            var countRelic = UnityEngine.Object.Instantiate(data.GetAllCollectableRelicData().Single(item => item.GetID() == "8c493cd7-8048-4598-bdd5-6cc2801cd842"));
            countRelic.name = "PojuEquipmentTriggerCountCarrier";
            Set(countRelic, "id", "bad71000-0000-4000-8000-000000000002");
            var countEffect = new RelicEffectData(); Set(countEffect, "relicEffectClassName", typeof(RelicEffectModifyTriggerCount).AssemblyQualifiedName!);
            Set(countEffect, "effectConditions", new List<RelicEffectCondition>()); Set(countEffect, "paramTrigger", CharacterTriggerData.Trigger.OnEquipmentAdded);
            Set(countEffect, "paramInt", 1); Set(countEffect, "paramBool", false); Set(countEffect, "paramBool2", true);
            Set(countRelic, "effects", new List<RelicEffectData> { countEffect });
            ((IList)AccessTools.Field(typeof(AllGameData), "collectableRelicDatas").GetValue(data)).Add(countRelic); save.AddRelic(countRelic);
            CaptureReady = true; Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-TRIGGER-RELIC-BATCH-PREPARED ten unchanged artifacts; authored player/enemy subtype and count carriers; native Boss/waves retained.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            var plays = BattleActionModel.EnumerateSupportedPlays(source);
            var context = source.Spawn.Train.Context!;
            foreach (string id in new[] { angelCardId, whelpCardId })
            foreach (CardToken card in context.Cards.Hand.Where(card => card.DataId == id))
            {
                PlayCardAction? summon = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.RoomIndex == 0);
                if (summon != null) return summon;
            }
            CombatUnit? carrier = source.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Team == CombatTeam.Player &&
                unit.Subtypes.Contains(Angel) && unit.EquipmentCards?.Count == 0 && unit.Health == unit.MaxHealth);
            if (carrier != null)
            foreach (CardToken card in context.Cards.Hand.Where(card => card.DataId == woundCardId))
            {
                PlayCardAction? wound = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.TargetUnitId == carrier.Id);
                if (wound != null) return wound;
            }
            // Settle one fragile Whelp death while an Angel and enemies still
            // share its floor. Later natural Whelps provide the Strike witness.
            var fragile = source.Spawn.Train.Rooms.Where(room => room.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(Angel)) &&
                room.Units.Any(unit => unit.Team == CombatTeam.Enemy)).SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(Whelp) && unit.Health <= 12);
            if (fragile != null)
            foreach (CardToken card in context.Cards.Hand.Where(card => card.DataId == woundCardId))
            {
                var death = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.TargetUnitId == fragile.Id);
                if (death != null) return death;
            }
            foreach (CardToken card in context.Cards.Hand.Where(card => card.DataId == gearCardId))
            {
                var equip = plays.Where(action => action.CardInstanceId == card.InstanceId).OrderBy(action =>
                    source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId).EquipmentCards?.Count ?? 0)
                    .ThenBy(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId).Team == CombatTeam.Player ? 0 : 1)
                    .FirstOrDefault(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == action.TargetUnitId &&
                        unit.Subtypes.Contains(Angel) && unit.IsBoss != true && (unit.Team == CombatTeam.Player ||
                            unit.Triggers.Any(trigger => trigger.Kind == "OnEquipmentAdded" && trigger.Effects.Any(effect => effect.Action?.Statuses.Any(status => status.Id == "valor") == true)))));
                if (equip != null) return equip;
            }
            if (!Stationary)
            foreach (CardToken card in context.Cards.Hand.Where(card => card.DataId == shiftCardId))
            {
                var shift = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                    source.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == action.TargetUnitId && unit.Team == CombatTeam.Player && unit.Subtypes.Contains(Angel)));
                if (shift != null) return shift;
            }
            // Friendly damage is restricted to the wound/death steps above.
            return plays.FirstOrDefault(action => context.FindCard(action.CardInstanceId)?.DataId != woundCardId &&
                (!Stationary || context.FindCard(action.CardInstanceId)?.DataId != shiftCardId));
        }
        private static void AddSubtype(CharacterData unit, string key)
        {
            var subtypes = (List<string>)AccessTools.Field(typeof(CharacterData), "subtypeKeys").GetValue(unit);
            if (!subtypes.Contains(key)) subtypes.Add(key);
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
