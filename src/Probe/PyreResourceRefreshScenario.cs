using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class PyreResourceRefreshScenario
    {
        internal const string Name = "pyre-resource-refresh";
        internal const string PositiveUpgradeId = "51db335c-1a47-4dce-9353-f9acb1e48001";
        internal const string NegativeUpgradeId = "51db335c-1a47-4dce-9353-f9acb1e48002";
        internal const string SecondNegativeUpgradeId = "51db335c-1a47-4dce-9353-f9acb1e48003";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static readonly (string Key, string Id)[] Artifacts =
        {
            ("BossIncreaseTurnDraw", "f9cc17ee-b7e5-45de-a02f-bd271e2dcd1a"),
            ("BossIncreaseTurnDrawDH", "401f30f6-6206-4c1c-b014-a8047c01597e"),
            ("BossIncreaseTurnDraw2", "341001b3-3fa9-47a5-9d88-eb04390b82fa")
        };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Pyre refresh relics must be acquired before native battle setup.");
            AllGameData data = save.GetAllGameData();
            CollectableRelicData[] relics = Artifacts.Select(item =>
            {
                CollectableRelicData relic = data.GetAllCollectableRelicData().Single(definition => definition.GetID() == item.Id);
                CheckOriginal(relic, item.Key);
                if (save.GetCollectedRelics().Any(owned => owned.GetRelicDataID() == item.Id))
                    throw new InvalidOperationException("Pyre refresh batch already owns " + item.Key + ".");
                return relic;
            }).ToArray();

            CardState[] owned = save.GetDeckState().ToArray();
            if (owned.Length == 0) throw new InvalidOperationException("Pyre refresh requires the natural starting deck.");
            CardData[] definitions = owned.Select(card => data.FindCardData(card.GetCardDataID())!).Distinct().ToArray();
            CardUpgradeData positive = Upgrade("PojuPyreRefreshPositive", PositiveUpgradeId, 7);
            CardUpgradeData negative = Upgrade("PojuPyreRefreshNegative", NegativeUpgradeId, -10);
            CardUpgradeData secondNegative = Upgrade("PojuPyreRefreshSecondNegative", SecondNegativeUpgradeId, -3);
            foreach (CardData card in definitions)
            {
                Set(card, "cardType", CardType.Spell);
                Set(card, "cost", 0); Set(card, "costType", CardData.CostType.Default);
                Set(card, "targetless", false); Set(card, "targetsRoom", true);
                Set(card, "isUnitAbility", false); Set(card, "isRoomAbility", false);
                Set(card, "traits", new List<CardTraitData>());
                Set(card, "effectTriggers", new List<CharacterTriggerData>());
                Set(card, "triggers", new List<CardTriggerEffectData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>());
                var status = Effect("CardEffectAddStatusEffect", Team.Type.Monsters, 0);
                Set(status, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "buff", count = 2 } });
                Set(card, "effects", new List<CardEffectData>
                {
                    UpgradeEffect(positive), UpgradeEffect(negative),
                    Effect("CardEffectBuffDamage", Team.Type.Monsters, 11), status,
                    Effect("CardEffectRewardGold", Team.Type.None, 0),
                    Effect("CardEffectRewardGold", Team.Type.None, 5),
                    UpgradeEffect(secondNegative),
                    Effect("CardEffectRewardGold", Team.Type.None, -5)
                });
            }
            foreach (CardState card in owned)
                card.Setup(data.FindCardData(card.GetCardDataID())!, save, setupStartingUpgrades: false);
            for (int index = 0; index < 20; index++)
                save.AddCardToDeck(definitions[0], applyExistingRelicModifiers: false, extraCopies: 0,
                    applyExtraCopiesMutator: false, showAnimation: false, setupStartingUpgrades: false, notifyDeckCount: false);
            CheckCards(save.GetDeckState());
            foreach (CollectableRelicData relic in relics) save.AddRelic(relic);
            Prepared = true;
            log.LogInfo("PYRE-RESOURCE-REFRESH-ACQUIRED three unchanged Boss draw originals before deployment; " +
                "natural deck authored as free Pyre spells plus 20 native deck additions; " +
                "one actual compound cast uses temporary attack +7/-10, damage buff +11, Rage2, Gold0/+5, attack -3 and Gold-5; " +
                "no resource setters, manual hand draws, player units or altered Pyre/Boss/wave definitions.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (CaptureReady) return;
            if (!Prepared) throw new InvalidOperationException("Pyre refresh missed pre-battle acquisition.");
            SaveManager save = managers.GetSaveManager();
            foreach (var artifact in Artifacts)
            {
                if (save.GetCollectedRelics().Count(relic => relic.GetRelicDataID() == artifact.Id) != 1)
                    throw new InvalidOperationException("Pyre refresh original owner missing or duplicated: " + artifact.Key);
                CheckOriginal(save.GetAllGameData().GetAllCollectableRelicData().Single(relic => relic.GetID() == artifact.Id), artifact.Key);
            }
            CheckCards(save.GetDeckState());
            CaptureReady = true;
            log.LogInfo("PYRE-RESOURCE-REFRESH-PREPARED one state-selected compound play in the native Pyre room; " +
                "later turns discard/draw naturally and retain all original enemy/Pyre combat.");
        }

        // The applied unique upgrade is a native state marker. No action trace,
        // private scenario counter or future state chooses this one cast.
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
            CombatUnit? pyre = source.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre);
            if (pyre?.Modifiers == null || pyre.Modifiers.Upgrades.Any(upgrade => upgrade.DataId == PositiveUpgradeId)) return null;
            RoomPlayRule? room = source.PlayRules.Rooms.SingleOrDefault(item => item.IsPyre);
            if (room == null) return null;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
            foreach (CardToken card in source.Spawn.Train.Context.Cards.Hand)
            {
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule == null || !IsCompound(rule)) continue;
                PlayCardAction? play = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                    action.RoomIndex == room.RoomIndex && action.ActivatorUnitId == 0 && action.TargetUnitId == 0);
                if (play != null) return play;
            }
            return null;
        }

        internal static bool IsCompound(CardPlayRule rule)
        {
            if (rule.CardType != "Spell" || rule.IsAnyAbility != false || rule.Effect != "Spell" || rule.Cost != 0 ||
                rule.CostType != "Default" || rule.SpawnUnit != null || rule.Summon != null || rule.Ability != null ||
                rule.Equipment != null || rule.ExternalInteractions.Count != 0 || rule.Effects.Count != 8) return false;
            string[] types = { "UnitUpgrade", "UnitUpgrade", "BuffAttack", "AddStatus", "RewardGold", "RewardGold", "UnitUpgrade", "RewardGold" };
            int[] values = { 0, 0, 11, 0, 0, 5, 0, -5 };
            for (int index = 0; index < types.Length; index++)
            {
                CardActionEffect effect = rule.Effects[index];
                bool unitEffect = index < 4 || index == 6;
                if (effect.Type != types[index] || effect.Target != "Pyre" || effect.Value != values[index] ||
                    effect.AllowEnemy || effect.AllowPlayer != unitEffect || effect.RequiredGold != 0) return false;
                if (index == 3)
                {
                    if (effect.Statuses.Count != 1 || effect.Statuses[0].Id != "buff" || effect.Statuses[0].Stacks != 2) return false;
                }
                else if (effect.Statuses.Count != 0) return false;
                if (index == 0 || index == 1 || index == 6)
                {
                    string id = index == 0 ? PositiveUpgradeId : index == 1 ? NegativeUpgradeId : SecondNegativeUpgradeId;
                    int damage = index == 0 ? 7 : index == 1 ? -10 : -3;
                    if (effect.Lifetime != "TemporaryUntilUnitDeath" || effect.Upgrade?.DataId != id ||
                        !effect.Upgrade.Unique || effect.Upgrade.Stats.Damage != damage || effect.Upgrade.Statuses.Count != 0) return false;
                }
                else if (effect.Upgrade != null) return false;
            }
            return true;
        }

        private static CardUpgradeData Upgrade(string key, string id, int damage)
        {
            CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade(key, id, damage, 0, 0, 0, "armor", 0);
            upgrade.GetStatusEffectUpgrades().Clear();
            Set(upgrade, "isUnique", true);
            return upgrade;
        }

        private static CardEffectData UpgradeEffect(CardUpgradeData upgrade)
        {
            CardEffectData effect = Effect("CardEffectAddTempCardUpgradeToUnits", Team.Type.Monsters, 0);
            Set(effect, "paramCardUpgradeData", upgrade);
            Set(effect, "additionalParamInt1", (int)UnitUpgradeLifetime.TemporaryUntilUnitDeath);
            return effect;
        }

        private static CardEffectData Effect(string type, Team.Type team, int amount)
        {
            var effect = new CardEffectData(type, null!, team);
            effect.Cheat_SetTargetMode(TargetMode.Pyre);
            Set(effect, "paramInt", amount);
            return effect;
        }

        private static void CheckCards(IEnumerable<CardState> cards)
        {
            if (cards.Any(card => card.GetCardType() != CardType.Spell || card.IsAnyAbility() ||
                card.GetSpawnCharacterData() != null || card.GetEffects().Count != 8 || card.GetTraits().Count != 0 ||
                card.GetEffectStates().Any(effect => effect.GetTargetMode() != TargetMode.Pyre || !effect.GetCardEffect().CanPlayInEngineRoom)))
                throw new InvalidOperationException("Pyre refresh requires ordinary Pyre spells with native engine-room support.");
        }

        private static void CheckOriginal(CollectableRelicData relic, string key)
        {
            int upgradeIndex = key == "BossIncreaseTurnDraw" ? 0 : 1;
            (string Type, int Int, int Int2)[] expected =
            {
                ("RelicEffectChangeStartingHandSize", 1, 0),
                ("RelicEffectIncreasePyreHealth", 30, upgradeIndex),
                ("RelicEffectIncreasePyreAttack", 10, upgradeIndex)
            };
            List<RelicEffectData> effects = relic.GetEffects();
            if (relic.name != key || relic.DisallowInDeploymentPhase || effects.Count != expected.Length)
                throw new InvalidOperationException("Original Boss draw relic identity or effect count changed: " + key);
            for (int index = 0; index < expected.Length; index++)
            {
                RelicEffectData effect = effects[index];
                var rule = expected[index];
                if (effect.GetEffectClassName() != rule.Type || effect.GetParamInt() != rule.Int || effect.GetParamInt2() != rule.Int2 ||
                    effect.GetParamSourceTeam() != Team.Type.None || effect.GetParamBool() || effect.GetParamBool2() ||
                    effect.GetEffectConditions().Count != 0 || effect.GetParamMutatorData() != null ||
                    effect.GetParamCardUpgradeData() != null || effect.GetParamCharacters().Count != 0)
                    throw new InvalidOperationException("Original Boss draw relic payload changed: " + key + "/" + index);
            }
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
