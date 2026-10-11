using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDecayRelicBatchScenario
    {
        internal const string Name = "original-decay-relic-batch";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static readonly (string Key, string Id)[] Artifacts =
        {
            ("RagePreservedOnFriendlies", "b7822614-96ec-4ace-9029-6efc8adef374"),
            ("IncreaseFrostbiteStacks", "9cb2b9b2-0308-4f71-92e8-e96a30b87416")
        };

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            SaveManager save = managers.GetSaveManager(); CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData[] stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!)
                .Where(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                .GroupBy(card => card.GetSpawnCharacterData()).Select(group => group.First()).ToArray();
            if (stewards.Length < 2) throw new InvalidOperationException("Decay relic batch needs two distinct Steward carrier definitions.");
            foreach (CardData card in stewards)
            {
                CharacterData unit = card.GetSpawnCharacterData()!;
                Set(unit, "health", 80); Set(unit, "attackDamage", 2); Set(unit, "size", 1);
                Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(card, "cost", 0);
            }

            // Apply statuses through an ordinary native card play. Armor keeps
            // the early enemy witness alive through natural turn-end decay.
            CardData spell = owned.Select(card => data.FindCardData(card.GetCardDataID())!).First(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDamage" && effect.GetTargetMode() == TargetMode.DropTargetCharacter));
            // A status array contains random alternatives in the native effect.
            // Separate effects apply all three statuses in this exact order.
            Set(spell, "effects", new List<CardEffectData> { Status("buff", 2), Status("poison", 1), Status("armor", 30) });
            Set(spell, "traits", new List<CardTraitData>()); Set(spell, "cost", 0);
            foreach (CardState card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (CardData card in new[] { stewards[0], stewards[1], spell, spell, spell, spell })
                cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);

            CollectableRelicData rage = data.GetAllCollectableRelicData().Single(item => item.GetID() == Artifacts[0].Id);
            if (rage.name != Artifacts[0].Key || rage.GetEffects().Count != 1)
                throw new InvalidOperationException("Original Rage preservation relic identity changed.");
            CheckEffect(rage.GetEffects()[0], "RelicEffectPreventStatusEffectDecay", Team.Type.Monsters, 0, false, "buff", 1);
            save.AddRelic(rage);
            CollectableRelicData frostbite = data.GetAllCollectableRelicData().Single(item => item.GetID() == Artifacts[1].Id);
            if (frostbite.name != Artifacts[1].Key || frostbite.GetEffects().Count != 2)
                throw new InvalidOperationException("Original Frostbite preservation relic identity changed.");
            CheckEffect(frostbite.GetEffects()[0], "RelicEffectAddStatusEffectOnStatusApplied", Team.Type.None, 1, true, "poison", 2);
            CheckEffect(frostbite.GetEffects()[1], "RelicEffectPreventStatusEffectDecay", Team.Type.None, 0, false, "poison", 1);
            save.AddRelic(frostbite);
            CaptureReady = true; Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-DECAY-RELIC-BATCH-PREPARED unchanged RagePreservedOnFriendlies and IncreaseFrostbiteStacks; three separate native single-target effects apply buff2 then poison1 then armor30 to both teams, each target once; native Boss/waves retained.");
        }

        // Native recording and independent replay derive this policy entirely
        // from captured units and card definitions, without private phase flags.
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
            CombatContext context = source.Spawn.Train.Context;
            foreach (CardToken card in context.Cards.Hand)
            {
                CombatUnit? template = source.PlayRules.Cards.FirstOrDefault(rule => rule.DataId == card.DataId)?.SpawnUnit;
                if (template?.AssetKey.StartsWith("TrainSteward", StringComparison.Ordinal) != true) continue;
                PlayCardAction? summon = plays.FirstOrDefault(action => action.CardInstanceId == card.InstanceId && action.RoomIndex == 0);
                if (summon != null) return summon;
            }
            CombatUnit[] untreated = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Health > 0 && !unit.IsPyre && !unit.Statuses.Any(status => status.Id == "poison" && status.Stacks > 0)).ToArray();
            // Favor the durable enemy, then protect the friendly carriers. The
            // spell never refreshes Rage or increases existing Frostbite stacks.
            IEnumerable<CombatUnit> targets = untreated.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 3)
                .OrderByDescending(unit => unit.MaxHealth).ThenBy(unit => unit.Id)
                .Concat(untreated.Where(unit => unit.Team == CombatTeam.Player && unit.AssetKey.StartsWith("TrainSteward", StringComparison.Ordinal))
                    .OrderBy(unit => unit.Id));
            foreach (CombatUnit target in targets)
            {
                PlayCardAction? apply = plays.FirstOrDefault(action => action.TargetUnitId == target.Id && IsStatusSpell(source, action.CardInstanceId));
                if (apply != null) return apply;
            }
            return plays.FirstOrDefault(action => !IsStatusSpell(source, action.CardInstanceId));
        }

        private static bool IsStatusSpell(BattleTurnState source, int instanceId)
        {
            CardInstanceState? card = source.Spawn.Train.Context!.FindCard(instanceId);
            CardPlayRule? rule = source.PlayRules!.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
            if (rule?.Effects.Count != 3) return false;
            for (int index = 0; index < 3; index++)
            {
                CardActionEffect effect = rule.Effects[index];
                string id = index == 0 ? "buff" : index == 1 ? "poison" : "armor";
                int stacks = index == 0 ? 2 : index == 1 ? 1 : 30;
                if (effect.Type != "AddStatus" || effect.Target != "DropTargetCharacter" || !effect.AllowEnemy || !effect.AllowPlayer ||
                    effect.Value != 0 || effect.Statuses.Count != 1 || effect.Statuses[0].Id != id || effect.Statuses[0].Stacks != stacks)
                    return false;
            }
            return true;
        }

        private static CardEffectData Status(string id, int count)
        {
            var effect = new CardEffectData("CardEffectAddStatusEffect", null!, Team.Type.Monsters | Team.Type.Heroes);
            effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(effect, "paramStatusEffects", new[] { Stack(id, count) }); return effect;
        }

        private static void CheckEffect(RelicEffectData effect, string type, Team.Type team, int parameter, bool flag, string statusId, int stacks)
        {
            StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
            if (effect.GetEffectClassName() != type || effect.GetParamSourceTeam() != team || effect.GetParamInt() != parameter ||
                effect.GetParamInt2() != 0 || effect.GetParamBool() != flag || effect.GetParamBool2() ||
                effect.GetEffectConditions().Count != 0 || statuses.Length != 1 || statuses[0].statusId != statusId || statuses[0].count != stacks)
                throw new InvalidOperationException("Original decay relic payload changed: " + type + "/" + statusId);
        }
        private static StatusEffectStackData Stack(string id, int count) => new StatusEffectStackData { statusId = id, count = count };
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
