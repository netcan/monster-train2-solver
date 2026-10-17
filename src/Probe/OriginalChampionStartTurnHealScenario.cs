using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalChampionStartTurnHealScenario
    {
        internal const string Name = "original-champion-start-turn-heal";
        internal const string OwnerId = "dbd5aec6-5046-4df2-ae61-4b646b8d1702";
        internal const string ChampZero = "fc119000-0000-4000-8000-000000000801";
        internal const string PeerOne = "fc119000-0000-4000-8000-000000000802";
        internal const string ChampOne = "fc119000-0000-4000-8000-000000000803";
        internal static readonly string[] Wounds =
        [
            "fc119000-0000-4000-8000-000000000811",
            "fc119000-0000-4000-8000-000000000812",
            "fc119000-0000-4000-8000-000000000813"
        ];
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object Evidence => new { OwnerId, ChampZero, PeerOne, ChampOne, Wounds, CardIds };
        private static int step;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Champion start-turn healing requires pre-battle acquisition.");
            RelicData owner = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            if (owner.GetEffects().Count != 1 || owner.GetEffects()[0].GetEffectClassName() != RelicStartTurnHealModel.Effect ||
                owner.DisallowInDeploymentPhase != true)
                throw new InvalidOperationException("Original ChampsHealBeginningOfTurn owner changed.");
            save.AddRelic(owner);
            Acquired = true;
            log.LogInfo("ORIGINAL-CHAMPION-START-TURN-HEAL-ACQUIRED unchanged owner=" + OwnerId + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            if (Prepared || !Acquired || save.PreviewMode || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Champion healing controls require isolated deployment.");

            CardData[] owned = cards.GetAllCards(new List<CardState>()).Select(card => data.FindCardData(card.GetCardDataID()))
                .Where(card => card != null).Cast<CardData>().ToArray();
            CardData unitTemplate = owned.First(card => card.GetSpawnCharacterData() != null);
            CardData spellTemplate = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell);
            var carriers = new[]
            {
                Unit("ChampZero", ChampZero, "fc119000-0000-4000-8000-000000000821", unitTemplate, data, true),
                Unit("PeerOne", PeerOne, "fc119000-0000-4000-8000-000000000822", unitTemplate, data, false),
                Unit("ChampOne", ChampOne, "fc119000-0000-4000-8000-000000000823", unitTemplate, data, true)
            };
            CardData[] wounds = Wounds.Select((id, index) => Wound("Wound" + index, id, spellTemplate, index, data)).ToArray();
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)!).AddRange(carriers.Concat(wounds));
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (CardData card in carriers.Concat(wounds))
                if (cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false) == null)
                    throw new InvalidOperationException("Could not add a champion healing control card to hand.");

            CombatRelicState ownerState = RelicProbe.Capture(managers).Single(item => item.DataId == OwnerId);
            RelicStartTurnHealRule rule = ownerState.ResourceEffects!.Single().StartTurnHeal!;
            if (ownerState.AssetKey != "ChampsHealBeginningOfTurn" || ownerState.DisallowedInPlacementPhase != true ||
                rule.EffectIndex != 0 || rule.MaxTurns != 999 || rule.Behavior != 0 || rule.HealingAmount != 0 ||
                rule.SourceTeam != "Monsters" || rule.TargetMode != "Tower" ||
                rule.RequiredSubtype != RelicStartTurnHealModel.ChampionSubtype || rule.ConditionCount != 0)
                throw new InvalidOperationException("Original ChampsHealBeginningOfTurn payload changed.");

            CardIds = carriers.Concat(wounds).Select(card => FullBattleTrace.Active!.CardId(cards.GetAllCards(new List<CardState>())
                .Single(state => state.GetCardDataID() == card.GetID()))).ToArray();
            step = 0;
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-CHAMPION-START-TURN-HEAL-PREPARED unchanged + full-health behavior; two Champ floors, " +
                "non-Champ same-floor control, wounded selected floor and deployment-phase exclusion.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= 6) return null;
            var context = state.Spawn.Train.Context!;
            var plays = BattleActionModel.EnumerateSupportedPlays(state);
            string[] dataIds = [ChampZero, ChampOne, PeerOne, Wounds[0], Wounds[1], Wounds[2]];
            int[] rooms = [0, 1, 1, 0, 1, 1];
            string? targetData = step switch
            {
                3 => ChampZero,
                4 => PeerOne,
                5 => ChampOne,
                _ => null
            };
            int targetId = targetData == null ? 0 : state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => context.FindCard(unit.SpawnerCardId)?.DataId == targetData)
                .Select(unit => unit.Id).FirstOrDefault();
            PlayCardAction? action = targetData == null
                ? plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)?.DataId == dataIds[step] &&
                    play.RoomIndex == rooms[step] && play.PlayerPosition == state.Spawn.Train.Rooms
                        .Single(room => room.RoomIndex == rooms[step]).Units.Count(unit => unit.Team == CombatTeam.Player && !unit.IsPyre))
                : targetId == 0 ? null : plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)?.DataId == dataIds[step] &&
                    play.TargetUnitId == targetId);
            if (action != null) step++;
            return action;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => Enabled &&
            CardIds.Contains(action.CardInstanceId);

        private static CardData Unit(string key, string id, string characterId, CardData template, AllGameData data, bool champion)
        {
            CharacterData unit = UnityEngine.Object.Instantiate(template.GetSpawnCharacterData()!);
            unit.name = "TrainStewardChampionHealPoju" + key;
            Set(unit, "id", characterId); Set(unit, "health", 40); Set(unit, "attackDamage", 5); Set(unit, "size", 1);
            Set(unit, "canBeHealed", true); Set(unit, "subtypeKeys", champion
                ? new List<string> { RelicStartTurnHealModel.ChampionSubtype } : new List<string>());
            Set(unit, "unitAbility", null); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(unit, "triggers", new List<CharacterTriggerData>());
            CardData card = UnityEngine.Object.Instantiate(template);
            card.name = "PojuChampionHeal" + key; Configure(card, id);
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
            Set(spawn, "paramCharacterData", unit); Set(card, "effects", new List<CardEffectData> { spawn });
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)!).Add(unit);
            return card;
        }

        private static CardData Wound(string key, string id, CardData template, int index, AllGameData data)
        {
            _ = data;
            CardData card = UnityEngine.Object.Instantiate(template);
            card.name = "PojuChampionHeal" + key; Configure(card, id);
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
            damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(damage, "paramInt", 5 + index);
            Set(card, "effects", new List<CardEffectData> { damage });
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
