using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSoloUnitUpgradeScenario
    {
        internal const string Name = "original-add-stats-to-solo-units";
        internal const string OwnerId = "724a1c19-192f-48c1-8099-8ae3ecb9c1e5";
        internal const string Solo = "fc119000-0000-4000-8000-000000000601";
        internal const string SoloSecond = "fc119000-0000-4000-8000-000000000602";
        internal const string PeerOne = "fc119000-0000-4000-8000-000000000603";
        internal const string PeerTwo = "fc119000-0000-4000-8000-000000000604";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static object Evidence => new { OwnerId, Solo, SoloSecond, PeerOne, PeerTwo };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Solo-unit owner requires pre-battle acquisition.");
            RelicData owner = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            if (owner.GetEffects().Count != 1 || owner.GetEffects()[0].GetEffectClassName() != RelicSoloUnitUpgradeModel.Effect ||
                owner.GetEffects()[0].GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original AddStatsToSoloUnits owner changed.");
            save.AddRelic(owner);
            Acquired = true;
            log.LogInfo("ORIGINAL-SOLO-UNIT-UPGRADE-ACQUIRED unchanged owner=" + OwnerId + " effect=" + RelicSoloUnitUpgradeModel.Effect + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager();
            AllGameData data = save.GetAllGameData();
            if (Prepared || !Acquired || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Solo-unit carriers require isolated deployment.");
            CardData template = cards.GetAllCards(new List<CardState>()).Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData() != null)!;
            var carriers = new[]
            {
                Unit("Solo", Solo, "fc119000-0000-4000-8000-000000000611", template, data),
                Unit("SoloSecond", SoloSecond, "fc119000-0000-4000-8000-000000000612", template, data),
                Unit("PeerOne", PeerOne, "fc119000-0000-4000-8000-000000000613", template, data),
                Unit("PeerTwo", PeerTwo, "fc119000-0000-4000-8000-000000000614", template, data)
            };
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).AddRange(carriers);
            foreach (CardData carrier in carriers)
                if (cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false) == null)
                    throw new InvalidOperationException("Could not add a solo-unit carrier to hand.");

            CombatRelicState ownerState = RelicProbe.Capture(managers).Single(item => item.DataId == OwnerId);
            RelicSoloUnitUpgrade rule = ownerState.SoloUnitUpgrades!.Single();
            CardUpgradeModifier? upgrade = rule.Upgrade;
            string payload = upgrade == null ? "none" :
                upgrade.AssetKey + "/" + upgrade.DataId + ":dmg=" + upgrade.Stats.Damage + ":hp=" + upgrade.Stats.Health +
                ":unhealed=" + upgrade.UnhealedHealth + ":size=" + upgrade.Stats.Size + ":unique=" + upgrade.Unique +
                ":statuses=" + string.Join(",", upgrade.Statuses.Select(status => status.Id + ":" + status.Stacks));
            Prepared = true;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SOLO-UNIT-UPGRADE-PREPARED native single=" + rule.SourceMonsters + " subtype=" +
                (rule.SubtypeKey ?? "<none>") + " removable=" + rule.Removable + " upgrade=" + payload +
                "; initial solo floor 0, multi floor 1, empty floors 2/3, later second floor-0 unit.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var context = state.Spawn.Train.Context!;
            var plays = BattleActionModel.EnumerateSupportedPlays(state);
            PlayCardAction? Find(string dataId, int roomIndex)
            {
                RoomCombatState room = state.Spawn.Train.Rooms.Single(item => item.RoomIndex == roomIndex);
                int position = room.Units.Count(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
                return plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)?.DataId == dataId &&
                    play.RoomIndex == roomIndex && play.PlayerPosition == position);
            }
            foreach ((string card, int room) in new[] { (Solo, 0), (PeerOne, 1), (PeerTwo, 1) })
            {
                if (state.Spawn.Train.Rooms[room].Units.Any(unit => context.FindCard(unit.SpawnerCardId)?.DataId == card)) continue;
                PlayCardAction? action = Find(card, room);
                if (action != null) return action;
            }
            if (state.Spawn.Turn >= 2 &&
                !state.Spawn.Train.Rooms[0].Units.Any(unit => context.FindCard(unit.SpawnerCardId)?.DataId == SoloSecond))
                return Find(SoloSecond, 0);
            return null;
        }

        private static CardData Unit(string key, string id, string characterId, CardData template, AllGameData data)
        {
            CharacterData unit = UnityEngine.Object.Instantiate(template.GetSpawnCharacterData()!);
            unit.name = "TrainStewardSoloPoju" + key;
            Set(unit, "id", characterId); Set(unit, "health", 120); Set(unit, "attackDamage", 5); Set(unit, "size", 1);
            Set(unit, "subtypeKeys", new List<string>()); Set(unit, "unitAbility", null);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "triggers", new List<CharacterTriggerData>());
            var card = UnityEngine.Object.Instantiate(template);
            card.name = "PojuSolo" + key; Configure(card, id);
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
            Set(spawn, "paramCharacterData", unit); Set(card, "effects", new List<CardEffectData> { spawn });
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit);
            return card;
        }

        private static void Configure(CardData card, string id)
        {
            Set(card, "id", id); Set(card, "cost", 0); Set(card, "traits", new List<CardTraitData>());
            Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
            Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
