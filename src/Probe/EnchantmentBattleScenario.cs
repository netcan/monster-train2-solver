using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class EnchantmentBattleScenario
    {
        internal static bool Prepared;
        internal static bool SourceDeaths => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "persistent-enchantment-deaths";
        private static readonly Dictionary<int, (CharacterState Native, int Room)> observed = new Dictionary<int, (CharacterState, int)>();
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            string sourceCard = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
            foreach (CardData data in owned.Where(card => card.GetSpawnCharacterData() != null &&
                card.GetSpawnCharacterData()!.name.StartsWith("TrainSteward", StringComparison.Ordinal))
                .Select(card => save.GetAllGameData().FindCardData(card.GetCardDataID())!).Distinct())
            {
                CharacterData unit = data.GetSpawnCharacterData()!;
                Set(unit, "size", 1); Set(unit, "health", 30); Set(unit, "attackDamage", 8);
                var triggers = new List<CharacterTriggerData>();
                CharacterTriggerData changed = HealingScenario.HealGold(1, false, true);
                Set(changed, "trigger", CharacterTriggerData.Trigger.OnStatusEffectChanged); triggers.Add(changed);
                if (data.GetID() == sourceCard)
                {
                    var aura = new CardEffectData("CardEffectEnchant", null!, Team.Type.Heroes | Team.Type.Monsters);
                    aura.Cheat_SetTargetMode(TargetMode.Room);
                    Set(aura, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "armor", count = 2 } });
                    CharacterTriggerData spawn = HealingScenario.HealGold(0, false, true);
                    Set(spawn, "trigger", CharacterTriggerData.Trigger.OnSpawn);
                    Set(spawn, "effects", new List<CardEffectData> { aura }); triggers.Add(spawn);
                    if (SourceDeaths)
                    {
                        var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
                        damage.Cheat_SetTargetMode(TargetMode.Self); Set(damage, "paramInt", 9999);
                        CharacterTriggerData turn = HealingScenario.HealGold(0, false, true);
                        Set(turn, "trigger", CharacterTriggerData.Trigger.OnTurnBegin);
                        Set(turn, "effects", new List<CardEffectData> { damage }); triggers.Add(turn);
                        CharacterTriggerData death = HealingScenario.HealGold(3, false, true);
                        Set(death, "trigger", CharacterTriggerData.Trigger.OnDeath); triggers.Add(death);
                    }
                }
                Set(unit, "triggers", triggers);
                foreach (CardState card in owned.Where(card => card.GetCardDataID() == data.GetID())) card.Setup(data, save);
            }
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("PERSISTENT-ENCHANTMENT-PREPARED paid Steward armor aura on both teams, status callbacks, unchanged original Boss/waves; sourceDeaths=" + SourceDeaths);
        }
        internal static EnchantmentWorld CaptureWorld(FullBattleTrace trace)
        {
            TrainCombatState train = trace.CaptureEnchantmentRooms();
            var live = train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            foreach (RoomCombatState room in train.Rooms)
                foreach (CombatUnit unit in room.Units)
                {
                    CharacterState native = trace.KnownUnits.Single(actor => trace.UnitId(actor) == unit.Id);
                    observed[unit.Id] = (native, room.RoomIndex);
                }
            var retained = new List<EnchantmentRetainedUnit>();
            var enchanters = new List<int>();
            foreach (var pair in observed)
            {
                CharacterState actor = pair.Value.Native;
                using (new CharacterState.SetAllowDestroyedAccessHelper(actor, true))
                {
                    if (actor.IsEnchanter) enchanters.Add(pair.Key);
                    if (!live.Contains(pair.Key)) retained.Add(new EnchantmentRetainedUnit(trace.CaptureUnit(actor),
                        actor.GetCurrentRoomIndex(), actor.PreviewMode));
                }
            }
            var referenced = train.Rooms.SelectMany(room => room.Units).Concat(retained.Select(actor => actor.Unit))
                .SelectMany(unit => unit.Triggers).SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null)
                .SelectMany(effect => effect.Enchantment!.State.PrimaryTargets.Concat(effect.Enchantment.State.PreviewTargets))
                .Select(target => target.UnitId).Concat(enchanters).ToHashSet();
            retained = retained.Where(actor => referenced.Contains(actor.Unit.Id)).ToList();
            AllGameManagers managers = AllGameManagers.Instance!;
            RoomManager rooms = managers.GetRoomManager()!;
            uint[] test = RngCalibration.Words(RandomManager.GetState(RngId.BattleTest));
            return new EnchantmentWorld(train.Rooms, train.Movement, train.EnemySlotsPerRoom, retained, enchanters,
                rooms.AllowEnchantmentUpdates, (bool)AccessTools.Field(typeof(RoomManager), "_updatingEnchantments").GetValue(rooms),
                managers.GetSaveManager().PreviewMode, new UnityRng(test[0], test[1], test[2], test[3]), true);
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
