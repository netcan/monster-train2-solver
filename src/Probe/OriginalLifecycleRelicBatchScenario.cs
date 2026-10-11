using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalLifecycleRelicBatchScenario
    {
        internal const string Name = "original-lifecycle-relic-batch";
        internal const string Shroom = "SubtypesData_Shroom";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static readonly (string Key, string Id)[] Artifacts =
        {
            ("IncreaseArmorStacks_DH", "c9b18f6f-20d1-4228-bfab-b740609db8dc"),
            ("AvariceOnSummon", "644749c2-6ee3-4ce6-91ed-420077ee0193"),
            ("ShroomsGainRegen", "bbe03be9-dae9-40bb-bb46-ab7553a17bd9"),
            ("DamageOnGoingDownFloor", "19147050-9859-4cf1-b826-f258f6c48c42"),
            ("LifeInsurer", "c1ae061c-5f95-4f1d-a8f0-6e027636a81e")
        };

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData[] stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!)
                .Where(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                .GroupBy(card => card.GetSpawnCharacterData()).Select(group => group.First()).ToArray();
            if (stewards.Length < 2) throw new InvalidOperationException("Lifecycle relic batch needs two distinct Steward carrier definitions.");
            CardData wound = owned.Select(card => data.FindCardData(card.GetCardDataID())!).First(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter));

            // The ability goes through native card creation, cooldown and the
            // common unit-ability callbacks. No authored callback grants buffs.
            CardData ability = UnityEngine.Object.Instantiate(wound);
            ability.name = "PojuLifecycleAbility";
            Set(ability, "id", "ba115000-0000-4000-8000-000000000001");
            Set(ability, "isUnitAbility", true); Set(ability, "isRoomAbility", false);
            Set(ability, "cooldownAtSpawn", 0); Set(ability, "cooldownAfterActivated", 2);
            Set(ability, "canAbilityTargetOtherFloors", false);
            Set(ability, "cost", 0); Set(ability, "costType", CardData.CostType.Default);
            Set(ability, "targetsRoom", true); Set(ability, "targetless", false);
            Set(ability, "traits", new List<CardTraitData>());
            Set(ability, "triggers", new List<CardTriggerEffectData>());
            Set(ability, "effectTriggers", new List<CharacterTriggerData>());
            Set(ability, "startingUpgrades", new List<CardUpgradeData>());
            Set(ability, "effects", new List<CardEffectData> { Effect("Heal", 1, TargetMode.Self) });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(ability);

            for (int index = 0; index < 2; index++)
            {
                CharacterData unit = stewards[index].GetSpawnCharacterData()!;
                Set(unit, "health", index == 0 ? 80 : 12); Set(unit, "attackDamage", 2); Set(unit, "size", 1);
                Set(unit, "subtypeKeys", index == 0 ? new List<string> { Shroom } : new List<string>());
                Set(unit, "startingStatusEffects", index == 0 ? Array.Empty<StatusEffectStackData>() :
                    new[] { new StatusEffectStackData { statusId = "undying", count = 1 } });
                Set(unit, "unitAbility", index == 0 ? ability : null);
                Set(stewards[index], "cost", 0);
            }
            Set(wound, "effects", new List<CardEffectData> { Effect("Damage", 12, TargetMode.DropTargetCharacter) });
            Set(wound, "traits", new List<CardTraitData>()); Set(wound, "cost", 0);
            foreach (CardState card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (CardData card in new[] { stewards[0], stewards[1], wound, wound })
                cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);

            foreach (var entry in Artifacts)
            {
                CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == entry.Id);
                CheckOriginal(relic, entry.Key, log);
                save.AddRelic(relic);
            }
            CaptureReady = true; Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-LIFECYCLE-RELIC-BATCH-PREPARED five unchanged original added-trigger artifacts; Shroom ability and subtype-negative Undying carriers, friendly wound and lethal damage; native Boss/waves retained.");
        }

        // Every choice comes from the captured state, so the independent replay
        // can choose the same actions without native IDs or mutable phase flags.
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
            CombatContext context = source.Spawn.Train.Context;
            for (int index = 0; index < 2; index++)
            foreach (CardToken card in context.Cards.Hand)
            {
                CombatUnit? template = source.PlayRules.Cards.FirstOrDefault(rule => rule.DataId == card.DataId)?.SpawnUnit;
                bool match = template != null && (index == 0 ? template.Subtypes.Contains(Shroom) :
                    !template.Subtypes.Contains(Shroom) && HasStatus(template, "undying"));
                if (!match) continue;
                int position = index == 0 ? 0 : source.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Count(unit => unit.Team == CombatTeam.Player);
                PlayCardAction? summon = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                    action.RoomIndex == 0 && action.PlayerPosition == position);
                if (summon != null) return summon;
            }
            CombatUnit[] players = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0).ToArray();
            CombatUnit? woundTarget = players.FirstOrDefault(unit => unit.Subtypes.Contains(Shroom) &&
                unit.Health == unit.MaxHealth && !HasStatus(unit, "armor"));
            CombatUnit? reviveTarget = players.FirstOrDefault(unit => !unit.Subtypes.Contains(Shroom) &&
                unit.Health <= 12 && HasStatus(unit, "undying") && !HasStatus(unit, "armor"));
            foreach (CombatUnit? target in new[] { woundTarget, reviveTarget })
            {
                if (target == null) continue;
                PlayCardAction? damage = plays.FirstOrDefault(action => action.TargetUnitId == target.Id &&
                    IsWound(source, action.CardInstanceId));
                if (damage != null) return damage;
            }
            PlayCardAction? activation = plays.FirstOrDefault(action => action.ActivatorUnitId != 0);
            if (activation != null) return activation;
            return plays.FirstOrDefault(action => !IsWound(source, action.CardInstanceId));
        }

        private static bool IsWound(BattleTurnState source, int instanceId)
        {
            CardInstanceState? card = source.Spawn.Train.Context!.FindCard(instanceId);
            CardPlayRule? rule = source.PlayRules!.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
            return rule?.Effects.Count == 1 && rule.Effects[0].Type == "Damage" && rule.Effects[0].Value == 12 &&
                rule.Effects[0].Target == "DropTargetCharacter" && rule.Effects[0].AllowPlayer && !rule.Effects[0].AllowEnemy;
        }
        private static bool HasStatus(CombatUnit unit, string id) => unit.Statuses.Any(status => status.Id == id && status.Stacks > 0);

        private static void CheckOriginal(CollectableRelicData relic, string key, ManualLogSource log)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            bool shroom = key == "ShroomsGainRegen", upgrade = key == "AvariceOnSummon";
            if (relic.name != key || effect.GetEffectClassName() != "RelicEffectAddTrigger" ||
                effect.GetParamSourceTeam() != Team.Type.Monsters || !effect.GetParamBool() || effect.GetParamBool2() ||
                effect.GetEffectConditions().Count != 0 || effect.GetParamCharacters().Count != 0 ||
                (shroom ? effect.GetParamCharacterSubtype().Key != Shroom : !effect.GetParamCharacterSubtype().IsNone))
                throw new InvalidOperationException("Original lifecycle relic definition changed: " + key);
            CharacterTriggerData trigger = effect.GetTriggers().Single();
            string kind = key == "LifeInsurer" ? "OnReanimated" : key == "DamageOnGoingDownFloor" ? "OnDeath" :
                shroom ? "OnAnyUnitDeathOnFloor" : "OnOwnAbilityActivated";
            if (trigger.GetTrigger().ToString() != kind || trigger.GetTriggerOnce() || !trigger.GetHideVisualAndIgnoreSilence() || trigger.GetOnlyTriggerIfEquipped())
                throw new InvalidOperationException("Original lifecycle trigger flags changed: " + key);
            CardEffectData action = trigger.GetEffects().Single();
            TargetMode target = key == "DamageOnGoingDownFloor" ? TargetMode.FrontInRoom :
                shroom || key == "LifeInsurer" ? TargetMode.Self : TargetMode.Room;
            Team.Type team = key == "LifeInsurer" ? Team.Type.Heroes | Team.Type.Monsters : Team.Type.Monsters;
            if (action.GetEffectStateName() != (upgrade ? "CardEffectAddTempCardUpgradeToUnits" : "CardEffectAddStatusEffect") ||
                action.GetTargetMode() != target || action.GetTargetTeamType() != team || action.GetParamBool() || action.GetParamBool2() ||
                action.GetParamInt() != (key == "DamageOnGoingDownFloor" ? 100 : 0))
                throw new InvalidOperationException("Original lifecycle effect changed: " + key);
            if (upgrade)
            {
                CardUpgradeData payload = action.GetParamCardUpgradeData();
                if (payload == null || payload.name != "Plus5Attack" || payload.GetBonusDamage() != 5)
                    throw new InvalidOperationException("Original lifecycle upgrade changed: " + key);
                log.LogInfo("ORIGINAL-LIFECYCLE-UPGRADE id=" + payload.GetID() + "; unique=" + payload.IsUnique() +
                    "; lifetime=" + ((UnitUpgradeLifetime)action.GetAdditionalParamInt1()));
            }
            else
            {
                StatusEffectStackData[] statuses = action.GetParamStatusEffects();
                int stacks = key == "LifeInsurer" ? 14 : key == "DamageOnGoingDownFloor" ? 5 : shroom ? 1 : 3;
                if (statuses.Length != 1 || statuses[0].statusId != (shroom ? "regen" : "armor") || statuses[0].count != stacks)
                    throw new InvalidOperationException("Original lifecycle status changed: " + key);
            }
            var interactions = new List<string>();
            EnemySpawningProbe.TriggerDefinition(trigger, interactions);
            if (interactions.Count != 0) throw new InvalidOperationException("Unsupported original lifecycle payload " + key + ": " + string.Join("; ", interactions));
        }

        private static CardEffectData Effect(string kind, int value, TargetMode target)
        {
            var effect = new CardEffectData("CardEffect" + kind, null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(target); Set(effect, "paramInt", value); return effect;
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
