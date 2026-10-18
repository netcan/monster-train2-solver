using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSelfAttackScenario
    {
        internal const string Name = "original-self-attack";
        internal const string AnimusId = "d632e3f2-c5cf-49d0-aa05-0c79331f6c85";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CharacterData enemy in all.GetAllCharacterData().Where(character => character.name == "ShieldGuardT1_BasicNonAttack" || character.name == "MageT1_Junker"))
                Set(enemy, "health", enemy.name == "MageT1_Junker" ? 200 : 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardEffectData Status(string type, string id, int count)
            {
                var effect = new CardEffectData(type, null!, Team.Type.Monsters); effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
                Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = id, count = count } }); return effect;
            }
            CardData Control(int suffix, params CardEffectData[] effects)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuSelfAttackControl" + suffix; Set(data, "id", "ac110039-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", false); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                Set(data, "effects", effects.ToList());
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData daze = Control(1, Status("CardEffectRemoveStatusEffect", "cooldown", 99), Status("CardEffectAddStatusEffect", "dazed", 1));
            CardData sweep = Control(2, Status("CardEffectRemoveStatusEffect", "cooldown", 99), Status("CardEffectRemoveStatusEffect", "dazed", 99), Status("CardEffectAddStatusEffect", "sweep", 1));
            CardData multi = Control(3, Status("CardEffectRemoveStatusEffect", "cooldown", 99), Status("CardEffectRemoveStatusEffect", "sweep", 99), Status("CardEffectAddStatusEffect", "multistrike", 1));
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Heroes); damage.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(damage, "paramInt", 125);
            CardData wound = Control(4, damage);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward), Add(steward), Add(all.FindCardData(AnimusId)!), Add(daze), Add(sweep), Add(multi), Add(wound) };
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SELF-ATTACK-PREPARED unchanged Animus of Speed/ability, selected back enemy, Dazed prevention, Sweep and lethal Multistrike retargeting; authored durable anchors/enemies and cooldown/status controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            int pending = Enumerable.Range(0, 3).Where(i => context.Statistics!.Value(ids[i], "TimesPlayed", "ThisBattle") == 0).DefaultIfEmpty(-1).First();
            PlayCardAction action;
            if (pending >= 0) action = new PlayCardAction(ids[pending], pending == 1 ? 1 : 0, playerPosition: pending == 2 ? 1 : 0);
            else
            {
                if (source.Spawn.Turn != 1) return null;
                CombatUnit? actor = source.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.SpawnerCardId == ids[2]);
                if (actor == null) return null;
                AbilityCardResult cached = AbilityCardModel.Get(context, actor.Ability!.CardCreation!);
                if (!cached.Supported) return null;
                int played = context.Statistics!.CardsPlayedThisTurn.Count(id => id == cached.Card!.InstanceId);
                if (played >= 4) return null;
                int control = played > 0 && context.Statistics.Value(ids[played + 2], "TimesPlayed", "ThisBattle") == 0 ? played + 2 :
                    played == 3 && context.Statistics.Value(ids[6], "TimesPlayed", "ThisBattle") == 0 ? 6 : -1;
                CombatUnit back = source.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "MageT1_Junker");
                action = control >= 0 ? new PlayCardAction(ids[control], 0, targetUnitId: control == 6 ? back.Id : actor.Id) :
                    new PlayCardAction(cached.Card.InstanceId, 0, targetUnitId: back.Id, activatorUnitId: actor.Id);
            }
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-SELF-ATTACK-REJECT " + result.Reason); }
            return null;
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
