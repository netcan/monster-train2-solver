using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPunishedSkillsScenario
    {
        internal const string Name = "original-punished-skills";
        internal const string Grim = "7d98ab2a-97a2-4e47-b836-dfc6deed50d8";
        internal const string Chain = "a7e84160-5e7b-4fd7-9227-7bf8f766454c";
        internal const string Berserker = "1c98fd78-22ee-4227-a550-de4398fe427f";
        internal const string GrimAbility = "334705d9-ce34-40d5-bcc0-708edb246484";
        internal const string ChainAbility = "5c8950a8-a409-4bcf-9c67-1c6c1e130e9b";
        internal const string Flight = "dc3337da-96a6-469b-88fc-561c588dc3bb";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definitions { get; private set; }
        private static int step;
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                DevotionProbe.Read(save, "GetDevotionLevel") != 0 || DevotionProbe.Read(save, "GetDevotionPoints") != 0)
                throw new InvalidOperationException("Punished skills require isolated zero-Devotion deployment.");
            Definitions = new[] { Grim, Chain, Berserker, GrimAbility, ChainAbility, Flight }.Select(id => all.FindCardData(id)!).Select(card => new {
                Id = card.GetID(), Key = card.name, Cost = card.GetCost(), Rule = BattleActionProbe.Definition(card),
                Effects = card.GetEffects().Select(RuleCatalogProbe.Effect).ToArray(),
                Traits = card.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray() }).ToArray();
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!; var anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 1000); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 1000);
            Set(all.GetBalanceData(), "maxHandSize", 40);
            CardData Control(int suffix, string type, int value, bool room = false, string? statusId = null)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalSpareScenario.PainId)); data.name = "PojuPunishedSkills" + suffix;
                Set(data, "id", "fc231b90-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", room); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, room && type != "CardEffectAdjustRoomCapacity" ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(room ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", statusId == null ? value : 0);
                if (statusId != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = statusId, count = value } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(1, "CardEffectGainEnergy", 40, true);
            var capacity = Control(2, "CardEffectAdjustRoomCapacity", 10, true);
            var damage5 = Control(3, "CardEffectDamage", 5); var damage1 = Control(4, "CardEffectDamage", 1);
            Set(damage5, "effects", Enumerable.Range(0, 3).Select(_ => {
                var effect = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
                effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(effect, "paramInt", 5); return effect;
            }).ToList());
            var zero = Control(5, "CardEffectDamage", 0); var reset = Control(6, "CardEffectRemoveStatusEffect", 999, statusId: "cooldown");
            var blank = Control(7, "CardEffectNULL", 0, true);
            int first = DevotionProbe.Read(all.GetBalanceData(), "GetMonolithDevotionFirstThreshold");
            int increment = DevotionProbe.Read(all.GetBalanceData(), "GetMonolithDevotionThresholdIncreasePerLevel");
            var levels = Enumerable.Range(0, 4).Select(level => Control(10 + level, "CardEffectGainDevotionPoints", first + level * increment, true)).ToArray();
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing Punished skill input: " + data.name);
            var grim = all.FindCardData(Grim)!; var chain = all.FindCardData(Chain)!; var berserker = all.FindCardData(Berserker)!;
            var created = new CardState?[] { Add(steward), Add(grim), Add(chain), Add(capacity), Add(berserker), null, Add(battery),
                Add(damage5), null, Add(damage1), Add(zero), Add(reset), null, Add(damage1), Add(damage1), null,
                Add(levels[0]), Add(berserker), Add(levels[1]), Add(berserker), Add(levels[2]), Add(berserker),
                Add(levels[3]), Add(berserker), Add(reset), null };
            foreach (var card in created.Skip(3).Where(card => card != null))
            { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card!.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(card => card == null ? 0 : FullBattleTrace.Active!.CardId(card)).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PUNISHED-SKILLS-PREPARED three unchanged units/three unchanged skills; healing, restriction, skill replacement, teleport, hit cooldown, chained attacks and five Devotion births.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= CardIds.Length || step >= 3 && source.Spawn.Turn < 1 || step >= 6 && source.Spawn.Turn < 2) return null;
            int floor = step is 0 or 6 or 16 or 18 or 20 or 22 ? 2 : step is 3 or 4 or 12 or 13 or 14 or 17 or 19 or 21 or 23 ? 1 : 0;
            var context = source.Spawn.Train.Context!;
            int targetCard = step is 7 or 9 or 10 or 11 or 13 or 14 ? 1 : step == 24 ? 2 : -1;
            int target = targetCard < 0 ? 0 : source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.SpawnerCardId == CardIds[targetCard]).Id;
            PlayCardAction action;
            if (CardIds[step] == 0)
            {
                var actor = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.SpawnerCardId == CardIds[step is 5 or 25 ? 2 : 1]);
                var cached = AbilityCardModel.Get(context, actor.Ability!.CardCreation!);
                if (!cached.Supported) throw new InvalidOperationException(cached.UnsupportedReason);
                action = new PlayCardAction(cached.Card!.InstanceId, floor, activatorUnitId: actor.Id,
                    targetSpawnPointIndex: step is 12 or 15 ? 0 : -1);
            }
            else action = new PlayCardAction(CardIds[step], floor,
                playerPosition: step is 0 or 1 or 2 or 4 or 17 or 19 or 21 or 23 ? 0 : -1, targetUnitId: target);
            var predicted = BattleActionModel.PlayCard(source, action);
            if (!predicted.Supported) throw new InvalidOperationException("Punished skill input " + step + " unsupported: " + predicted.Reason);
            step++; return action;
        }
    }
}
