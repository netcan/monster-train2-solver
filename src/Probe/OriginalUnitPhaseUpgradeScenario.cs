using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalUnitPhaseUpgradeScenario
    {
        internal const string Name = "original-unit-phase-upgrade-relics";
        internal const string Selene = "fc119000-0000-4000-8000-000000000511";
        internal const string Silent = "fc119000-0000-4000-8000-000000000512";
        internal const string Ordinary = "fc119000-0000-4000-8000-000000000513";
        internal const string Fragile = "fc119000-0000-4000-8000-000000000514";
        internal const string Sacrifice = "fc119000-0000-4000-8000-000000000515";
        internal static readonly string[] Ids = { "52c41ba0-f720-4b7e-9dd0-bd318be6acae", "e54e6cb7-f424-44be-a7d2-869c6fb7bb15" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var cards = managers.GetCardManager();
            if (Prepared || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0) throw new InvalidOperationException("Phase upgrades require isolated deployment.");
            var owned = cards.GetAllCards(new List<CardState>()).ToArray();
            var template = owned.Select(card => data.FindCardData(card.GetCardDataID())).First(card => card?.GetSpawnCharacterData() != null)!;
            foreach (var definition in owned.Select(card => data.FindCardData(card.GetCardDataID())).Where(card => card?.GetSpawnCharacterData() != null).Distinct())
            { Set(definition!.GetSpawnCharacterData()!, "health", 80); Set(definition.GetSpawnCharacterData()!, "attackDamage", 20); Set(definition.GetSpawnCharacterData()!, "size", 1); Set(definition, "cost", 0); }
            CardData Unit(string key, string id, string subtype, int hp)
            {
                var unit = UnityEngine.Object.Instantiate(template.GetSpawnCharacterData()!); unit.name = "TrainStewardPoju" + key;
                Set(unit, "id", id.Substring(0, id.Length - 3) + (int.Parse(id.Substring(id.Length - 3)) + 10));
                Set(unit, "health", hp); Set(unit, "attackDamage", hp == 10 ? 1 : 20); Set(unit, "size", 1); Set(unit, "subtypeKeys", subtype == "" ? new List<string>() : new List<string> { subtype });
                Set(unit, "unitAbility", null); Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                var triggers = new List<CharacterTriggerData>();
                foreach (string kind in new[] { "OnMoonLit", "OnMoonlitLost", "OnMoonShade", "OnMoonshadeLost" })
                { var trigger = HealingScenario.HealGold(0, false, true); Set(trigger, "trigger", Enum.Parse<CharacterTriggerData.Trigger>(kind)); triggers.Add(trigger); }
                Set(unit, "triggers", triggers);
                var card = UnityEngine.Object.Instantiate(template); Configure(card, "PojuPhase" + key, id);
                var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters); Set(spawn, "paramCharacterData", unit); Set(card, "effects", new List<CardEffectData> { spawn });
                ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(unit); return card;
            }
            var carriers = new[] { Unit("Selene", Selene, "SubtypesData_Selene", 80), Unit("Silent", Silent, "SubtypesData_Silent", 70), Unit("Ordinary", Ordinary, "", 60), Unit("Fragile", Fragile, "", 10) };
            var spell = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell && card.GetEffects().Count == 1));
            Configure(spell, "PojuPhaseSacrifice", Sacrifice); var sacrifice = new CardEffectData("CardEffectSacrifice", null!, Team.Type.Monsters); sacrifice.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(spell, "effects", new List<CardEffectData> { sacrifice });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).AddRange(carriers.Concat(new[] { spell }));
            foreach (var card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (var card in carriers.Concat(new[] { spell })) if (cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false) == null) throw new InvalidOperationException("Phase carrier creation failed.");
            foreach (string id in Ids)
            {
                var owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == id);
                if (owner.DisallowInDeploymentPhase || owner.GetEffects().Any(effect => effect.GetEffectClassName() != "RelicEffectAddRemoveTempUpgradeOnUnitTrigger" || effect.GetEffectConditions().Count != 0)) throw new InvalidOperationException("Original phase-upgrade owner changed.");
                save.AddRelic(owner);
            }
            Set(managers.GetPlayerManager(), "isMoonPhaseTrackingActive", true);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-UNIT-PHASE-UPGRADES-PREPARED two unchanged owners; Selene/Silent/ordinary carriers and actual sacrifice.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var context = state.Spawn.Train.Context!; var plays = BattleActionModel.EnumerateSupportedPlays(state); var room = state.Spawn.Train.Rooms[0];
            bool sacrificed = context.Statistics!.Values.Any(value => value.Type == "TimesPlayed" && value.Duration == "ThisBattle" && value.Value > 0 && context.FindCard(value.CardId)?.DataId == Sacrifice);
            foreach (string id in new[] { Selene, Silent, Ordinary, Fragile })
            {
                if (id == Fragile && sacrificed || room.Units.Any(unit => context.FindCard(unit.SpawnerCardId)?.DataId == id)) continue;
                var summon = plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)?.DataId == id && play.RoomIndex == 0 && play.PlayerPosition == room.Units.Count(unit => unit.Team == CombatTeam.Player));
                if (summon != null) return summon;
            }
            var victim = room.Units.FirstOrDefault(unit => context.FindCard(unit.SpawnerCardId)?.DataId == Fragile);
            if (victim != null)
            { var action = plays.FirstOrDefault(play => context.FindCard(play.CardInstanceId)?.DataId == Sacrifice && play.TargetUnitId == victim.Id); if (action != null) return action; }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => context.FindCard(card.InstanceId)?.DataId != Sacrifice);
        }
        private static void Configure(CardData card, string key, string id)
        {
            card.name = key; Set(card, "id", id); Set(card, "cost", 0); Set(card, "traits", new List<CardTraitData>());
            Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
