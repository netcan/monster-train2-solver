using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMissingHealthStatusScenario
    {
        internal const string Name = "original-missing-health-status";
        internal const string Collector = "53374762-31cd-4947-a65a-426a01c519cc";
        internal const string Ability = "02bcd324-4bef-4aa7-93f1-01277973f7c0";
        internal const string Oil = "a62b7ef1-efde-4daf-825a-ddf64fbe81f2";
        private const string Cantor = "a824b80b-4fac-4b20-9d2a-1790804e1232";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definitions { get; private set; }
        internal static object? PainDefinition { get; private set; }
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Missing-health status requires isolated deployment.");
            // Tower aggregation includes the Pyre's monster spawn group as well.
            save.Cheat_SetTowerHP(save.GetMaxTowerHP() - 7);
            var pyre = managers.GetRoomManager().GetPyreRoom().GetPyreHeart();
            pyre.SetHealth(save.GetTowerHP(), save.GetMaxTowerHP());
            Definitions = new[] { Collector, Ability, Oil }.Select(id => all.FindCardData(id)!).Select(card => new {
                Id = card.GetID(), Key = card.name, Cost = card.GetCost(), IsAbility = card.IsUnitAbility(),
                Traits = card.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(), Rule = BattleActionProbe.Definition(card),
                Effects = card.GetEffects().Select(RuleCatalogProbe.Effect).ToArray() }).ToArray();
            var pain = StatusEffectManager.Instance.GetStatusEffectDataById("pain")!;
            PainDefinition = new { Type = pain.GetStatusEffectStateName(), Rule = BattleActionProbe.Status("pain", 1),
                Stage = pain.GetPrimaryTriggerStage().ToString(), Stages = pain.GetAdditionalTriggerStages().Select(stage => stage.ToString()).ToArray() };
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!; var anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 1000);
            Set(all.GetBalanceData(), "maxHandSize", 40);
            CardData Control(int suffix, string type, int value, bool room = false, string? status = null,
                int count = 0, bool allFloors = false)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalSpareScenario.PainId)); data.name = "PojuMissingHealthControl" + suffix;
                Set(data, "id", "fc231b86-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", room); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, type is "CardEffectGainEnergy" or "CardEffectNULL" ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(room ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", value); Set(effect, "paramBool", allFloors);
                if (status != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = count } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(1, "CardEffectGainEnergy", 30, true);
            var hit3 = Control(2, "CardEffectDamage", 3); var hit5 = Control(3, "CardEffectDamage", 5);
            var hit2 = Control(4, "CardEffectDamage", 2); var hit4 = Control(5, "CardEffectDamage", 4);
            var heal = Control(6, "CardEffectHeal", 9999);
            var cooldown = Control(7, "CardEffectRemoveStatusEffect", 0, status: "cooldown", count: 999);
            var tower = Control(8, "CardEffectAddStatusEffectPerFriendlyMissingHp", 3, true, "armor", 2, true);
            var noCaster = Control(9, "CardEffectAddStatusEffectPerFriendlyMissingHp", 0, true, "armor", 3);
            var armor = Control(10, "CardEffectRemoveStatusEffect", 0, status: "armor", count: 999);
            var ignoredCount = Control(11, "CardEffectAddStatusEffectPerMissingHp", 99, true, "armor", 7);
            var zeroDivisor = Control(12, "CardEffectAddStatusEffectPerFriendlyMissingHp", 0, true, "armor", 1, true);
            var blank = Control(13, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing status scenario input " + data.name);
            var collector = all.FindCardData(Collector)!; var cantor = all.FindCardData(Cantor)!; var oil = all.FindCardData(Oil)!;
            var created = new CardState?[] { Add(steward), Add(battery), Add(collector), Add(cantor), Add(cantor), Add(cantor), null,
                Add(hit3), Add(hit5), Add(hit2), Add(cooldown), null, Add(oil), Add(heal), Add(cooldown), null,
                Add(heal), Add(heal), Add(cooldown), null, Add(tower), Add(noCaster), Add(armor), Add(hit4), Add(ignoredCount),
                Add(heal), Add(oil), Add(zeroDivisor) };
            foreach (var card in created.Skip(1).Where(card => card != null))
            { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card!.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(card => card == null ? 0 : FullBattleTrace.Active!.CardId(card)).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-MISSING-HEALTH-STATUS-PREPARED unchanged Collector/Oil, four original skill activations, room/tower/divisor/ignored-count controls.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            var context = source.Spawn.Train.Context!; int uses = 0;
            if (CardIds.Where(id => id > 0).All(id => context.Statistics!.Value(id, "TimesPlayed", "ThisBattle") > 0)) return null;
            foreach (var entry in context.AbilityCardCache?.Where(entry => entry.DataId == Ability) ?? Enumerable.Empty<AbilityCardCacheEntry>())
                // Cached skills are absent from per-card deck statistics, but are in global played history.
                uses += context.Statistics!.CardsPlayedThisTurn.Count(id => id == entry.InstanceId);
            int index = -1, ordinal = 0;
            for (int i = 0; i < CardIds.Length; i++)
            {
                if (CardIds[i] == 0) { ordinal++; if (uses >= ordinal) continue; }
                else if (context.Statistics!.Value(CardIds[i], "TimesPlayed", "ThisBattle") > 0) continue;
                index = i; break;
            }
            if (index < 0 || index > 0 && source.Spawn.Turn < 1) return null;
            int floor = index is 0 or 1 ? 2 : index is 5 or 9 or 27 ? 1 : 0;
            int targetCard = index is 7 or 13 or 22 or 23 or 25 ? 3 : index is 8 or 17 ? 4 : index == 9 ? 5 :
                index is 10 or 14 or 16 or 18 ? 2 : -1;
            int target = targetCard < 0 ? 0 : source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.SpawnerCardId == CardIds[targetCard]).Id;
            int actor = 0;
            if (CardIds[index] == 0)
            {
                actor = source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == CardIds[2]).Id;
                target = source.Spawn.Train.Rooms[0].Units.First(unit => unit.Team == CombatTeam.Enemy).Id;
            }
            int position = index == 0 || index == 2 || index == 5 ? 0 : index is 3 or 4 ? index - 2 : -1;
            var frontier = BattleActionModel.EnumerateActions(source);
            var action = frontier.SupportedActions.FirstOrDefault(play =>
                (actor > 0 ? play.ActivatorUnitId == actor : play.CardInstanceId == CardIds[index]) &&
                play.RoomIndex == floor && play.TargetUnitId == target && (position < 0 || play.PlayerPosition == position));
            if (action == null) throw new InvalidOperationException("Missing-health planned input " + index + " has no supported action: " +
                string.Join("; ", frontier.UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct().Take(6)));
            return action;
        }
    }
}
