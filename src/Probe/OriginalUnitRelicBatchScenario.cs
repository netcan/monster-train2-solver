using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalUnitRelicBatchScenario
    {
        internal const string Name = "original-unit-relic-batch";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal const string Demon = "SubtypesData_Demon_fcebd195-f262-4d7a-ac9f-01ffa2007a12";
        internal const string Snack = "SubtypesData_Snack";
        private static string demonCardId = "", snackCardId = "", equipmentCardId = "", shiftCardId = "";
        private static readonly HashSet<string> queried = new HashSet<string>();
        internal static readonly List<ShiftQuery> ShiftQueries = new List<ShiftQuery>();

        internal sealed class ShiftQuery
        {
            public RoomCombatState Before { get; set; } = null!;
            public int UnitId { get; set; }
            public bool NoCountModifiersAllowed { get; set; }
            public int ActualCount { get; set; }
            public bool OriginalNoCountFlag { get; set; }
            public bool RestoredNoCountFlag { get; set; }
            public int NormalCountAfter { get; set; }
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData[] stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!)
                .Where(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                .Distinct().ToArray();
            if (stewards.Length < 2) throw new InvalidOperationException("Unit relic batch needs two Steward carrier definitions.");
            demonCardId = stewards[0].GetID(); snackCardId = stewards[1].GetID();
            for (int index = 0; index < 2; index++)
            {
                CharacterData unit = stewards[index].GetSpawnCharacterData()!;
                var subtypes = (List<string>)AccessTools.Field(typeof(CharacterData), "subtypeKeys").GetValue(unit);
                string subtype = index == 0 ? Demon : Snack;
                if (!subtypes.Contains(subtype)) subtypes.Add(subtype);
                Set(unit, "health", 80); Set(unit, "attackDamage", 2); Set(unit, "size", 1);
                CharacterTriggerData shift = HealingScenario.HealGold(5, false, false);
                Set(shift, "trigger", CharacterTriggerData.Trigger.OnShift);
                Set(unit, "triggers", unit.GetTriggers().Concat(new[] { shift }).ToList());
                Set(stewards[index], "cost", 0);
            }

            CardData shiftCard = owned.Select(card => data.FindCardData(card.GetCardDataID())!)
                .First(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectFloorRearrange"));
            shiftCardId = shiftCard.GetID();
            var bump = new CardEffectData("CardEffectBump", null!, Team.Type.Monsters | Team.Type.Heroes);
            bump.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(bump, "paramInt", 1);
            Set(shiftCard, "effects", new List<CardEffectData> { bump }); Set(shiftCard, "cost", 0);

            // Native multi-summon copies the first paid unit's card for subsequent
            // cardless units, exercising both relic spawn paths in one action.
            Set(stewards[1].GetEffects().Single(effect => effect.GetEffectStateName() == "CardEffectSpawnMonster"), "paramInt", 2);

            CardData equipment = data.GetAllCardData().First(card => card.GetCardType() == CardType.Equipment &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectAttachEquipment");
            equipmentCardId = equipment.GetID();
            CardUpgradeData gear = DynamicUpgradeScenario.Upgrade("PojuOriginalRelicShiftGear", "ba710ad2-0000-4000-8000-000000000001", 0, 0, 0, 0, "armor", 0);
            Set(gear, "statusEffectUpgrades", new List<StatusEffectStackData>());
            CharacterTriggerData gearShift = HealingScenario.HealGold(5, false, false);
            Set(gearShift, "trigger", CharacterTriggerData.Trigger.OnShift);
            Set(gear, "triggerUpgrades", new List<CharacterTriggerData> { gearShift });
            var attach = new CardEffectData("CardEffectAttachEquipment", null!, Team.Type.Heroes);
            attach.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(attach, "paramCardUpgradeData", gear);
            Set(equipment, "effects", new List<CardEffectData> { attach }); Set(equipment, "traits", new List<CardTraitData>());
            Set(equipment, "cost", 0);
            foreach (CardState card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);

            // Initial fixture preparation precedes the first recorded root.
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.AddNewCard(stewards[0], CardPile.HandPile, false, false, animate: false);
            cards.AddNewCard(stewards[1], CardPile.HandPile, false, false, animate: false);
            cards.AddNewCard(equipment, CardPile.HandPile, false, false, animate: false);
            cards.AddNewCard(shiftCard, CardPile.HandPile, false, false, animate: false);
            cards.AddNewCard(shiftCard, CardPile.HandPile, false, false, animate: false);
            Acquire("94595f3a-4624-4408-977d-b166a98f23d2", "ImpDamageBuff", "AddMultistrikeToDemons");
            Acquire("f6a4e405-98b9-4eeb-bae1-50cb204d12ed", "StewardSacrificeDamage", "StewardSacrificeDamage_Upgrade");
            Acquire("d1daea7e-3346-47df-a756-c839b044e5f3", "MorselsGetDamageShield", "AddDamageShieldToMorsels");
            Acquire("8c493cd7-8048-4598-bdd5-6cc2801cd842", "ValorTransferOnAngelDeath", null);
            CaptureReady = true;
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).ToArray())
            {
                managers.GetRelicManager().ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                managers.GetRelicManager().ApplyCardStateModifiers(card);
            }
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-UNIT-RELIC-BATCH-PREPARED unchanged ImpDamageBuff, StewardSacrificeDamage, MorselsGetDamageShield, ValorTransferOnAngelDeath; authored subtype, shift, cardless and equipment carriers.");

            void Acquire(string id, string key, string? upgradeKey)
            {
                CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == id);
                if (relic.name != key || relic.GetEffects().Any(effect => effect.GetEffectConditions().Count != 0))
                    throw new InvalidOperationException("Original unit relic identity/conditions changed: " + key);
                RelicEffectData effect = relic.GetEffects()[0];
                if (upgradeKey != null)
                {
                    CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
                    if (effect.GetEffectClassName() != "RelicEffectAddTempUpgrade" || upgrade == null || upgrade.name != upgradeKey || !upgrade.IsUnique())
                        throw new InvalidOperationException("Original unit relic upgrade changed: " + key);
                }
                else if (relic.GetEffects().Count != 1 || effect.GetEffectClassName() != "RelicEffectModifyTriggerCount" ||
                    effect.GetParamTrigger() != CharacterTriggerData.Trigger.OnShift || effect.GetParamInt() != 1 ||
                    effect.GetParamBool() || !effect.GetParamBool2()) throw new InvalidOperationException("Original Shift relic changed.");
                save.AddRelic(relic);
            }
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            CaptureShiftQueries(source);
            var plays = BattleActionModel.EnumerateSupportedPlays(source);
            CombatContext context = source.Spawn.Train.Context!;
            // Keep Demon carriers alive for actual repeated combat attacks.
            if (source.Spawn.Turn >= 2)
                foreach (PlayCardAction ability in UnitAbilityModel.EnumerateSupportedActivations(source))
                    if (!source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == ability.ActivatorUnitId).Subtypes.Contains(Demon))
                        return ability;
            foreach (string id in new[] { demonCardId, snackCardId })
                foreach (CardToken card in context.Cards.Hand.Where(card => card.DataId == id))
                {
                    PlayCardAction? play = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.RoomIndex == 0);
                    if (play != null) return play;
                }
            foreach (CardToken card in context.Cards.Hand.Where(card => card.DataId == equipmentCardId))
            {
                PlayCardAction? equip = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                    action.RoomIndex == 0 && source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.Id == action.TargetUnitId &&
                        unit.Team == CombatTeam.Enemy && unit.IsBoss != true));
                if (equip != null) return equip;
            }
            foreach (CardToken card in context.Cards.Hand.Where(card => card.DataId == shiftCardId))
            {
                PlayCardAction? shift = plays.Where(action => action.CardInstanceId == card.InstanceId).OrderBy(action =>
                    source.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == action.TargetUnitId).Team == CombatTeam.Enemy ? 0 : 1)
                    .FirstOrDefault(action => action.RoomIndex == 0 && source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.Id == action.TargetUnitId &&
                        (unit.Team == CombatTeam.Player || unit.EquipmentCards?.Count > 0)));
                if (shift != null) return shift;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }

        private static void CaptureShiftQueries(BattleTurnState source)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            foreach (RoomCombatState room in source.Spawn.Train.Rooms)
            foreach (CombatUnit unit in room.Units.Where(unit => !unit.IsPyre))
            {
                string key = unit.Id + "/" + (unit.EquipmentCards?.Count ?? 0);
                if (!queried.Add(key)) continue;
                CharacterState native = trace.KnownUnits.Single(actor => trace.UnitId(actor) == unit.Id);
                ShiftQueries.Add(new ShiftQuery { Before = room, UnitId = unit.Id,
                    ActualCount = native.GetTriggerFireCount(CharacterTriggerData.Trigger.OnShift) });
                CharacterTriggerState? trigger = native.GetTriggers().FirstOrDefault(item => item.GetTrigger() == CharacterTriggerData.Trigger.OnShift);
                if (trigger == null) continue;
                bool blocked = trigger.isNoCountModifiersAllowed;
                var excluded = new ShiftQuery { Before = room, UnitId = unit.Id, NoCountModifiersAllowed = true,
                    OriginalNoCountFlag = blocked };
                try
                {
                    trigger.isNoCountModifiersAllowed = true;
                    excluded.ActualCount = native.GetTriggerFireCount(CharacterTriggerData.Trigger.OnShift, trigger);
                }
                finally { trigger.isNoCountModifiersAllowed = blocked; }
                excluded.RestoredNoCountFlag = trigger.isNoCountModifiersAllowed;
                excluded.NormalCountAfter = native.GetTriggerFireCount(CharacterTriggerData.Trigger.OnShift);
                ShiftQueries.Add(excluded);
            }
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
