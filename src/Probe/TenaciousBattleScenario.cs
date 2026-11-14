using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TenaciousBattleScenario
    {
        internal const string Name = "tenacious-battle";
        internal const string OwnerId = "2d68b9db-bcbc-487e-96de-b6398ad3c36b";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step, enemyTarget;
        private static object? definition, calibration;
        private static ManualLogSource? logger;
        private static string? rejection;
        internal static object Evidence => new { Prepared, ControlCardIds = cards, CompletedControlPlays = step,
            OwnerDefinition = definition, Calibration = calibration, EnemyTargetId = enemyTarget };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = managers.GetAllGameData();
            CardManager manager = managers.GetCardManager();
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                save.GetCollectedRelics().Any(relic => relic.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("Tenacious requires isolated 2.3.1 deployment without its owner.");
            RelicData owner = all.GetAllCollectableRelicData().Single(relic => relic.GetID() == OwnerId);
            RelicEffectData effect = owner.GetEffects().Single();
            definition = new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                Type = effect.GetEffectClassName(), Team = effect.GetParamSourceTeam().ToString(),
                Statuses = effect.GetParamStatusEffects().Select(status => new { status.statusId, status.count }).ToArray() };
            if (owner.name != "SpellsNoKillFriendlies" || owner.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicModel.AddStatusOnSpawn || effect.GetParamSourceTeam() != Team.Type.Monsters ||
                effect.GetParamStatusEffects().Single().statusId != "tenacious" || effect.GetParamStatusEffects()[0].count != 1)
                throw new InvalidOperationException("Changed original Tenacious owner.");
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "health", 100); Set(unit, "attackDamage", 60); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>()); Set(unit, "unitAbility", null!);
            var selfDamage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
            selfDamage.Cheat_SetTargetMode(TargetMode.Self); Set(selfDamage, "paramInt", 1);
            Set(unit, "triggers", new List<CharacterTriggerData> {
                new CharacterTriggerData(CharacterTriggerData.Trigger.OnTurnBegin, selfDamage) });
            CardData template = all.FindCardData("96c9ed37-5e59-4c4f-a914-ae7040d24755");
            CardData Control(int index, string type, int amount, bool enemy = false, string? statusId = null)
            {
                CardData card = UnityEngine.Object.Instantiate(template); card.name = "PojuTenaciousControl" + index;
                Set(card, "id", "fc231e10-0000-4000-8000-00000000000" + index); Set(card, "cost", 0);
                Set(card, "targetless", false); Set(card, "targetsRoom", true);
                var action = new CardEffectData(type, null!, enemy ? Team.Type.Heroes : Team.Type.Monsters);
                action.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(action, "paramInt", statusId == null ? amount : 0);
                if (statusId != null) Set(action, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = statusId, count = amount } });
                Set(card, "effects", new List<CardEffectData> { action });
                Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
                Set(card, "startingUpgrades", new List<CardUpgradeData>());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(card);
                return card;
            }
            CardData damage99 = Control(1, "CardEffectDamage", 99), damage1 = Control(2, "CardEffectDamage", 1),
                overkill = Control(3, "CardEffectDamage", 999), heal = Control(4, "CardEffectHeal", 8),
                armor = Control(5, "CardEffectAddStatusEffect", 4, statusId: "armor"), last = Control(6, "CardEffectDamage", 13),
                enemyStatus = Control(7, "CardEffectAddStatusEffect", 1, true, "tenacious"), enemyDamage = Control(8, "CardEffectDamage", 9, true);
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            manager.GetDrawPile().Clear(); save.AddRelic(owner);
            CardState Add(CardData data) => manager.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Unable to add Tenacious input " + data.name);
            CardState futureDamage = Add(enemyDamage); manager.MoveCardFromHandToTopOfDeck(futureDamage);
            CardState futureStatus = Add(enemyStatus); manager.MoveCardFromHandToTopOfDeck(futureStatus);
            cards = new[] { steward, damage99, damage1, overkill, heal, armor, last, steward, steward }.Select(Add)
                .Concat(new[] { futureStatus, futureDamage }).Select(FullBattleTrace.Active!.CardId).ToArray();
            // Direct pile surgery bypassed the normal CardManager statistics refresh.
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData {
                trackedValue = CardStatistics.TrackedValueType.TimesPlayed, cardState = manager.GetHand()[0],
                entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active!.InvalidateCardRules(); Prepared = true; logger = log;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("TENACIOUS-PREPARED unchanged original owner; friendly lethal/repeated/armor/Heal, native self-damage, hostile spell and natural Spikes retaliation.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return null;
            int room = step is 7 or 8 ? 2 : 0, target = 0;
            if (step >= 9)
            {
                if (state.Spawn.Turn == 0 || !state.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == cards[step])) return null;
                if (calibration == null) calibration = TenaciousCalibration.Run(AllGameManagers.Instance!);
                RoomCombatState? floor = state.Spawn.Train.Rooms.FirstOrDefault(candidate => candidate.Units.Any(unit =>
                    unit.Team == CombatTeam.Enemy && unit.Health > 0 && (step == 9 ? unit.Status("armor") == null : unit.Id == enemyTarget)));
                if (floor == null) return null;
                room = floor.RoomIndex;
                if (step == 9) enemyTarget = floor.Units.First(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0 && unit.Status("armor") == null).Id;
                target = enemyTarget;
            }
            else if (step is >= 1 and <= 6) target = state.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == cards[0]).Id;
            var action = new PlayCardAction(cards[step], room, playerPosition: step is 0 or 7 or 8 ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported)
            {
                if (rejection != result.Reason) { rejection = result.Reason; logger?.LogInfo("TENACIOUS-REJECT " + step + ": " + rejection); }
                return null;
            }
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
