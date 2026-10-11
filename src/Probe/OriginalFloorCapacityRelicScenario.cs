using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFloorCapacityRelicScenario
    {
        internal const string Name = "original-floor-capacity-relics";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static readonly (string Key, string Id)[] Artifacts =
        {
            ("PurgeChampionReward", "be25af24-9516-4ed2-9cf1-c756a381a928"),
            ("BossCapacity", "9291142a-dc6f-4834-89f1-ceda778e06ec"),
            ("BossCapacity2", "2d313e11-aa80-4116-b359-7560d3307544"),
            ("BossCapacityDH", "f7b0c251-7f96-4115-897a-d5b4a40a36ef"),
            ("SilverIncreasedCapacity", "aab4971f-05c6-430a-8a6a-dfb654a41b0c")
        };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Floor capacity originals require acquisition before native room setup.");
            AllGameData data = save.GetAllGameData();
            CollectableRelicData[] relics = Artifacts.Select(item =>
            {
                CollectableRelicData relic = data.GetAllCollectableRelicData().Single(definition => definition.GetID() == item.Id);
                CheckOriginal(relic, item.Key);
                if (save.GetCollectedRelics().Any(owned => owned.GetRelicDataID() == item.Id))
                    throw new InvalidOperationException("Capacity batch already owns " + item.Key + ".");
                return relic;
            }).ToArray();
            CardState[] owned = save.GetDeckState().ToArray();
            CardData[] definitions = owned.Select(card => data.FindCardData(card.GetCardDataID())!).Distinct().ToArray();
            CardData steward = definitions.First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            CardData emptySpell = definitions.First(card => card.GetCardType() == CardType.Spell && !card.IsUnitAbility() && !card.IsRoomAbility());
            CharacterData prototype = steward.GetSpawnCharacterData()!;
            int[] sizes = { 6, 6, 2, 1 };
            var carriers = new List<CardData>();
            // Deployment is an actual native draw. Its card-upgrade trait flag
            // makes the four authored units deployable without a manual hand draw.
            CardUpgradeData deploy = DynamicUpgradeScenario.Upgrade("PojuCapacityDeployment", "fc119000-0000-4000-8000-000000000100", 0, 0, 0, 0, "armor", 0);
            Set(deploy, "statusEffectUpgrades", new List<StatusEffectStackData>());
            var trait = new CardTraitData(); trait.Setup("CardTraitIgnoreArmor");
            Set(trait, "drawInDeploymentPhase", true);
            Set(deploy, "traitDataUpgrades", new List<CardTraitData> { trait });
            ((List<CardUpgradeData>)AccessTools.Field(typeof(AllGameData), "cardUpgradeDatas").GetValue(data)).Add(deploy);
            for (int index = 0; index < sizes.Length; index++)
            {
                CharacterData unit = UnityEngine.Object.Instantiate(prototype);
                unit.name = "TrainStewardPojuCapacity" + index;
                Set(unit, "id", "fc119000-0000-4000-8000-00000000000" + (index + 1));
                Set(unit, "health", 80); Set(unit, "attackDamage", 2); Set(unit, "size", sizes[index]);
                Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null);
                ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit);
                CardData card = UnityEngine.Object.Instantiate(steward);
                card.name = "PojuCapacityCarrier" + index;
                Set(card, "id", "fc119000-0000-4000-8000-00000000001" + (index + 1));
                AuthorCard(card, CardType.Monster);
                var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
                spawn.Cheat_SetTargetMode(TargetMode.Room); Set(spawn, "paramInt", 1); Set(spawn, "paramCharacterData", unit);
                Set(card, "effects", new List<CardEffectData> { spawn });
                Set(card, "startingUpgrades", new List<CardUpgradeData> { deploy });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(card);
                carriers.Add(card);
            }
            foreach (CardData definition in definitions) AuthorCard(definition, CardType.Spell);
            foreach (CardState card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save, setupStartingUpgrades: false);
            foreach (CardData card in carriers)
                save.AddCardToDeck(card, applyExistingRelicModifiers: false, extraCopies: 0,
                    applyExtraCopiesMutator: false, showAnimation: false, setupStartingUpgrades: true, notifyDeckCount: false);
            for (int index = 0; index < 20; index++)
                save.AddCardToDeck(emptySpell, applyExistingRelicModifiers: false, extraCopies: 0,
                    applyExtraCopiesMutator: false, showAnimation: false, setupStartingUpgrades: false, notifyDeckCount: false);
            CardState[] authored = save.GetDeckState().Where(card => card.GetSpawnCharacterData() != null).ToArray();
            if (authored.Length != 4 || !authored.Select(card => card.GetSize()).OrderBy(size => size).SequenceEqual(sizes.OrderBy(size => size)) ||
                authored.Any(card => (int)AccessTools.Field(typeof(CardState), "cost").GetValue(card) != 0))
                throw new InvalidOperationException("Capacity carriers must be four real free deployable cards of size 6/6/2/1.");
            OriginalFloorCapacityRelicProbe.Begin(managers, relics);
            foreach (CollectableRelicData relic in relics) save.AddRelic(relic);
            Prepared = true;
            log.LogInfo("ORIGINAL-FLOOR-CAPACITY-ACQUIRED five unchanged originals before room initialization; " +
                "four native deployable Steward cards of size 6/6/2/1 plus empty-spell deck, with no capacity/resource setters or hand draws; original Boss/Pyre/waves retained.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (CaptureReady) return;
            if (!Prepared) throw new InvalidOperationException("Capacity batch missed pre-battle acquisition.");
            SaveManager save = managers.GetSaveManager();
            foreach (var entry in Artifacts)
            {
                if (save.GetCollectedRelics().Count(relic => relic.GetRelicDataID() == entry.Id) != 1)
                    throw new InvalidOperationException("Missing or duplicate capacity original: " + entry.Key);
                CheckOriginal(save.GetAllGameData().GetAllCollectableRelicData().Single(relic => relic.GetID() == entry.Id), entry.Key);
            }
            OriginalFloorCapacityRelicProbe.CaptureDeploymentCards(managers);
            CaptureReady = true;
            log.LogInfo("ORIGINAL-FLOOR-CAPACITY-PREPARED natural deployment draw; captured-state policy fills floor zero as 6/6/2/1, then clears free spells for actual subsequent draws.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
            RoomCombatState? floor = source.Spawn.Train.Rooms.FirstOrDefault(room => room.RoomIndex == 0);
            if (floor == null) return null;
            CombatContext context = source.Spawn.Train.Context;
            CombatUnit[] players = floor.Units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).ToArray();
            int[] sizes = { 6, 6, 2, 1 };
            if (players.Length < sizes.Length)
            {
                int desired = sizes[players.Length];
                foreach (CardToken card in context.Cards.Hand)
                {
                    CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                    if (rule == null || !IsCarrier(rule) || rule.SpawnUnit!.Size != desired) continue;
                    PlayCardAction? summon = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId &&
                        action.RoomIndex == 0 && action.PlayerPosition == players.Length && action.TargetUnitId == 0 && action.ActivatorUnitId == 0);
                    if (summon != null) return summon;
                }
            }
            foreach (CardToken card in context.Cards.Hand)
            {
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                if (rule?.CardType != "Spell" || rule.IsAnyAbility != false || rule.Effect != "Null" || rule.Cost != 0 ||
                    rule.CostType != "Default" || rule.Effects.Count != 0 || rule.SpawnUnit != null || rule.ExternalInteractions.Count != 0) continue;
                PlayCardAction? play = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.RoomIndex == 0 &&
                    action.TargetUnitId == 0 && action.ActivatorUnitId == 0);
                if (play != null) return play;
            }
            return null;
        }

        internal static bool IsCarrier(CardPlayRule rule) => rule.CardType == "Monster" && rule.IsAnyAbility == false &&
            rule.Effect == "SpawnMonster" && rule.Cost == 0 && rule.CostType == "Default" && rule.Destination == "Standby" &&
            rule.ExternalInteractions.Count == 0 && rule.SpawnUnit?.Team == CombatTeam.Player &&
            rule.SpawnUnit.MaxHealth == 80 && rule.SpawnUnit.BaseAttack == 2 && !rule.SpawnUnit.IsPyre &&
            rule.SpawnUnit.Triggers.Count == 0 && rule.SpawnUnit.Statuses.Count == 0 && new[] { 6, 2, 1 }.Contains(rule.SpawnUnit.Size);

        private static void AuthorCard(CardData card, CardType type)
        {
            Set(card, "cardType", type); Set(card, "cost", 0); Set(card, "costType", CardData.CostType.Default);
            Set(card, "targetless", type == CardType.Spell); Set(card, "targetsRoom", type == CardType.Monster);
            Set(card, "isUnitAbility", false); Set(card, "isRoomAbility", false);
            Set(card, "effects", new List<CardEffectData>()); Set(card, "traits", new List<CardTraitData>());
            Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "triggers", new List<CardTriggerEffectData>());
            Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }

        private static void CheckOriginal(CollectableRelicData relic, string key)
        {
            const string capacity = "RelicEffectChangeFloorCapacity", energy = "RelicEffectModifyEnergy",
                deployment = "RelicEffectModifyEnergyDuringDeployment", draw = "RelicEffectChangeStartingHandSize",
                health = "RelicEffectIncreasePyreHealth", attack = "RelicEffectIncreasePyreAttack";
            int upgradeIndex = key == "BossCapacity" ? 0 : 1;
            (string Type, int Int, int Int2)[] expected = key == "PurgeChampionReward"
                ? new[] { (energy, 1, 0), (deployment, 1, 0), (capacity, 2, 0), (draw, 1, 0) }
                : key == "SilverIncreasedCapacity" ? new[] { (capacity, 2, 0) }
                : new[] { (capacity, 2, 0), (health, 30, upgradeIndex), (attack, 10, upgradeIndex) };
            if (relic.name != key || relic.DisallowInDeploymentPhase || relic.GetEffects().Count != expected.Length)
                throw new InvalidOperationException("Capacity original identity/placement/count changed: " + key);
            for (int index = 0; index < expected.Length; index++)
            {
                RelicEffectData effect = relic.GetEffects()[index]; var rule = expected[index];
                if (effect.GetEffectClassName() != rule.Type || effect.GetParamInt() != rule.Int || effect.GetParamInt2() != rule.Int2 ||
                    effect.GetParamSourceTeam() != Team.Type.None || effect.GetParamBool() || effect.GetParamBool2() ||
                    effect.GetParamString() != "" || effect.GetEffectConditions().Count != 0 || effect.GetParamMutatorData() != null ||
                    effect.GetParamCardUpgradeData() != null || effect.GetParamCharacters().Count != 0 || effect.GetParamCardFilter() != null ||
                    effect.GetParamCardFilterSecondary() != null || !effect.GetParamCharacterSubtype().IsNone ||
                    effect.GetParamStatusEffects().Length != 0 || effect.GetParamExcludeCharacterSubtypes().Length != 0)
                    throw new InvalidOperationException("Capacity original payload changed: " + key + "/" + index);
            }
        }

        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
