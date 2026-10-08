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
        internal static bool ChildUpgrades => (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "").EndsWith("-upgrades", StringComparison.Ordinal);
        internal static bool RandomPools => (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") ?? "").StartsWith("persistent-enchantment-random", StringComparison.Ordinal);
        internal static bool SourceRevivals => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "persistent-enchantment-revivals" || Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "persistent-enchantment-random-revivals";
        internal static bool SourceDeaths => SourceRevivals || Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "persistent-enchantment-deaths" || Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "persistent-enchantment-random-deaths";
        internal static bool IsScenario(string scenario) => scenario == "persistent-enchantment" || scenario == "persistent-enchantment-deaths" || scenario == "persistent-enchantment-revivals" ||
            scenario == "persistent-enchantment-random" || scenario == "persistent-enchantment-random-deaths" || scenario == "persistent-enchantment-random-revivals" ||
            scenario == "persistent-enchantment-upgrades" || scenario == "persistent-enchantment-random-upgrades";
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
                if (ChildUpgrades) triggers.Add(UpgradeChild());
                if (data.GetID() == sourceCard)
                {
                    if (SourceRevivals) Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "undying", count = 2 } });
                    var aura = new CardEffectData("CardEffectEnchant", null!, Team.Type.Heroes | Team.Type.Monsters);
                    aura.Cheat_SetTargetMode(TargetMode.Room);
                    Set(aura, "paramStatusEffects", RandomPools ? new[] { new StatusEffectStackData { statusId = "armor", count = 2 },
                        new StatusEffectStackData { statusId = "regen", count = 1 }, new StatusEffectStackData { statusId = "buff", count = 1 } } :
                        new[] { new StatusEffectStackData { statusId = "armor", count = 2 } });
                    CharacterTriggerData spawn = HealingScenario.HealGold(0, false, true);
                    Set(spawn, "trigger", CharacterTriggerData.Trigger.OnSpawn);
                    Set(spawn, "effects", new List<CardEffectData> { aura }); triggers.Add(spawn);
                    if (SourceDeaths)
                    {
                        var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
                        damage.Cheat_SetTargetMode(TargetMode.Self); Set(damage, "paramInt", 9999);
                        CharacterTriggerData turn = HealingScenario.HealGold(0, false, true);
                        Set(turn, "trigger", CharacterTriggerData.Trigger.OnTurnBegin);
                        var damageEffects = new List<CardEffectData> { damage };
                        if (SourceRevivals)
                        {
                            var second = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
                            second.Cheat_SetTargetMode(TargetMode.Self); Set(second, "paramInt", 9999); damageEffects.Add(second);
                            foreach (var pair in new[] { (CharacterTriggerData.Trigger.OnReanimated, 5),
                                (CharacterTriggerData.Trigger.OnAnyMonsterDeathOnFloor, 2), (CharacterTriggerData.Trigger.OnAnyUnitDeathOnFloor, 4) })
                            {
                                CharacterTriggerData child = HealingScenario.HealGold(pair.Item2, false, true);
                                Set(child, "trigger", pair.Item1); triggers.Add(child);
                            }
                        }
                        Set(turn, "effects", damageEffects); triggers.Add(turn);
                        CharacterTriggerData death = HealingScenario.HealGold(3, false, true);
                        Set(death, "trigger", CharacterTriggerData.Trigger.OnDeath); triggers.Add(death);
                    }
                }
                Set(unit, "triggers", triggers);
                foreach (CardState card in owned.Where(card => card.GetCardDataID() == data.GetID())) card.Setup(data, save);
            }
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("PERSISTENT-ENCHANTMENT-PREPARED paid Steward aura on both teams, status callbacks, unchanged original Boss/waves; sourceDeaths=" + SourceDeaths + " sourceRevivals=" + SourceRevivals + " randomPools=" + RandomPools + " childUpgrades=" + ChildUpgrades);
        }
        private static CharacterTriggerData UpgradeChild()
        {
            CardUpgradeData temporary = DynamicUpgradeScenario.Upgrade("PojuAuraTemporaryChild",
                "ed1091ee-b111-4db1-b5e1-b69f15a50701", 1, 1, 0, 0, "spikes", 1);
            CardUpgradeData permanent = DynamicUpgradeScenario.Upgrade("PojuAuraPermanentChild",
                "ed1091ee-b111-4db1-b5e1-b69f15a50702", 2, 3, 0, 0, "regen", 1);
            CharacterTriggerData temporaryChanged = HealingScenario.HealGold(2, false, true);
            Set(temporaryChanged, "trigger", CharacterTriggerData.Trigger.OnStatusEffectChanged);
            temporary.GetCharacterTriggerUpgrades().Add(temporaryChanged);
            CharacterTriggerData permanentChanged = HealingScenario.HealGold(4, false, true);
            Set(permanentChanged, "trigger", CharacterTriggerData.Trigger.OnStatusEffectChanged);
            permanent.GetCharacterTriggerUpgrades().Add(permanentChanged);
            CharacterTriggerData hit = HealingScenario.HealGold(3, false, true);
            Set(hit, "trigger", CharacterTriggerData.Trigger.OnHit);
            permanent.GetCharacterTriggerUpgrades().Add(hit);
            CardEffectData Effect(string type, CardUpgradeData upgrade, UnitUpgradeLifetime lifetime)
            {
                var effect = new CardEffectData(type, null!, Team.Type.Monsters);
                effect.Cheat_SetTargetMode(TargetMode.Self);
                Set(effect, "paramCardUpgradeData", upgrade); Set(effect, "additionalParamInt1", (int)lifetime);
                return effect;
            }
            CharacterTriggerData child = HealingScenario.HealGold(0, true, true);
            Set(child, "trigger", CharacterTriggerData.Trigger.OnStatusEffectChanged);
            Set(child, "effects", new List<CardEffectData> {
                Effect("CardEffectAddTempCardUpgradeToUnits", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle),
                Effect("CardEffectAddTempCardUpgradeToUnits", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle),
                Effect("CardEffectRemoveTempUpgradeFromUnit", temporary, UnitUpgradeLifetime.TemporaryUntilEndOfBattle),
                Effect("CardEffectAddCardUpgradeToUnits", permanent, UnitUpgradeLifetime.Permanent) });
            return child;
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
