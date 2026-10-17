using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFrontRoomHealthUpgradeScenario
    {
        internal const string Name = "original-front-room-health-upgrade";
        internal const string OwnerId = "cb2c9d18-9a05-4523-acdc-6e4af73152aa";
        internal const string FrontZero = "fc119000-0000-4000-8000-000000000701";
        internal const string BackZero = "fc119000-0000-4000-8000-000000000702";
        internal const string FrontOne = "fc119000-0000-4000-8000-000000000703";
        internal const string BackOne = "fc119000-0000-4000-8000-000000000704";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static object Evidence => new { OwnerId, FrontZero, BackZero, FrontOne, BackOne };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Front-room health relic requires pre-battle acquisition.");
            RelicData owner = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId)!;
            if (owner.GetEffects().Count != 1 || owner.GetEffects()[0].GetEffectClassName() != RelicSoloUnitUpgradeModel.FrontRoomEffect ||
                owner.GetEffects()[0].GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original front-room health relic owner changed.");
            save.AddRelic(owner);
            Acquired = true;
            log.LogInfo("ORIGINAL-FRONT-ROOM-HEALTH-ACQUIRED unchanged owner=" + OwnerId + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            if (Prepared || !Acquired || save.PreviewMode || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Front-room health carriers require isolated deployment.");
            CardData template = cards.GetAllCards(new List<CardState>())!.Select(card => data.FindCardData(card.GetCardDataID()))
                .First(card => card?.GetSpawnCharacterData() != null)!;
            var carriers = new[]
            {
                Unit("FrontZero", FrontZero, "fc119000-0000-4000-8000-000000000711", template, data),
                Unit("BackZero", BackZero, "fc119000-0000-4000-8000-000000000712", template, data),
                Unit("FrontOne", FrontOne, "fc119000-0000-4000-8000-000000000713", template, data),
                Unit("BackOne", BackOne, "fc119000-0000-4000-8000-000000000714", template, data)
            };
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)!).AddRange(carriers);
            foreach (CardData carrier in carriers)
                if (cards.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false) == null)
                    throw new InvalidOperationException("Could not add a front-room health carrier to hand.");

            CombatRelicState ownerState = RelicProbe.Capture(managers).Single(item => item.DataId == OwnerId);
            RelicSoloUnitUpgrade rule = ownerState.SoloUnitUpgrades!.Single();
            CardUpgradeModifier upgrade = rule.Upgrade!;
            if (!rule.FrontInEachRoom || upgrade.AssetKey != "EmblemOfExiles_Upgrade" ||
                upgrade.DataId != "45bdb6aa-dd9a-42b7-930d-f586a8c7979a" || upgrade.Stats.Health != 15 ||
                upgrade.Stats.Damage != 0 || upgrade.UnhealedHealth != 0 || upgrade.Unique ||
                upgrade.ExternalInteractions.Count != 0)
                throw new InvalidOperationException("Original front-room relic upgrade payload changed.");
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-FRONT-ROOM-HEALTH-PREPARED native +15 max/current HP per turn; floor 0/1 front units and back-unit controls.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var context = state.Spawn.Train.Context!;
            var plays = BattleActionModel.EnumerateSupportedPlays(state);
            foreach ((string card, int room, int position) in new[]
            {
                (FrontZero, 0, 0), (BackZero, 0, 1), (FrontOne, 1, 0), (BackOne, 1, 1)
            })
            {
                if (state.Spawn.Train.Rooms[room].Units.Any(unit => context.FindCard(unit.SpawnerCardId)?.DataId == card)) continue;
                PlayCardAction? action = plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)?.DataId == card &&
                    play.RoomIndex == room && play.PlayerPosition == position);
                if (action != null) return action;
            }
            return null;
        }

        private static CardData Unit(string key, string id, string characterId, CardData template, AllGameData data)
        {
            CharacterData unit = UnityEngine.Object.Instantiate(template.GetSpawnCharacterData()!);
            unit.name = "TrainStewardFrontHealthPoju" + key;
            Set(unit, "id", characterId); Set(unit, "health", 120); Set(unit, "attackDamage", 5); Set(unit, "size", 1);
            Set(unit, "subtypeKeys", new List<string>()); Set(unit, "unitAbility", null);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "triggers", new List<CharacterTriggerData>());
            var card = UnityEngine.Object.Instantiate(template);
            card.name = "PojuFrontHealth" + key; Configure(card, id);
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
            Set(spawn, "paramCharacterData", unit); Set(card, "effects", new List<CardEffectData> { spawn });
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)!).Add(unit);
            return card;
        }

        private static void Configure(CardData card, string id)
        {
            Set(card, "id", id); Set(card, "cost", 0); Set(card, "traits", new List<CardTraitData>());
            Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
            Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }

        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
