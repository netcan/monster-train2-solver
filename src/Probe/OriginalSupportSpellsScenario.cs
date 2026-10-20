using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSupportSpellsScenario
    {
        internal const string Name = "original-support-spells";
        internal const string IsolatedPrefix = "original-isolated-spells:";
        internal static string[]? IsolatedIds => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is string scenario &&
            scenario.StartsWith(IsolatedPrefix, StringComparison.Ordinal) ? scenario.Substring(IsolatedPrefix.Length).Split(',') : null;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name || IsolatedIds != null;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static ManualLogSource? logger;
        private static string? lastRejection;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            if (IsolatedIds is string[] isolated) { PrepareIsolated(managers, log, isolated); return; }
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            Set(all.GetBalanceData(), "maxHandSize", 30); save.SetDragonsHoardCap(4); save.SetDragonsHoardAmount(3);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData Control(int suffix, string effectName, int value, Team.Type team)
            {
                CardData data = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
                data.name = "PojuSupportControl" + suffix; Set(data, "id", "ac110052-0000-4000-8000-00000000000" + suffix);
                Set(data, "cost", 0); Set(data, "costType", CardData.CostType.Default); Set(data, "targetless", true); Set(data, "targetsRoom", true);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "triggers", new List<CardTriggerEffectData>()); Set(data, "effectTriggers", new List<CharacterTriggerData>());
                var effect = new CardEffectData(effectName, null!, team); effect.Cheat_SetTargetMode(TargetMode.Room); Set(effect, "paramInt", value);
                if (effectName == "CardEffectAdjustCorruption") Set(effect, "paramBool", true);
                Set(data, "effects", new List<CardEffectData> { effect });
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data); return data;
            }
            CardData battery = Control(1, "CardEffectGainEnergy", 30, Team.Type.None), wound = Control(2, "CardEffectDamage", 30, Team.Type.Monsters),
                health = Control(3, "CardEffectBuffMaxHealth", 200, Team.Type.Heroes), corruption = Control(4, "CardEffectAdjustCorruption", 3, Team.Type.None),
                pyre = Control(5, "CardEffectDamage", 10, Team.Type.None);
            pyre.GetEffects()[0].Cheat_SetTargetMode(TargetMode.Pyre);
            CardData hit = Control(6, "CardEffectDamage", 50, Team.Type.Monsters), piercing = Control(7, "CardEffectDamage", 50, Team.Type.Monsters),
                heal = Control(8, "CardEffectHeal", 15, Team.Type.Monsters);
            heal.GetEffects()[0].Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            hit.GetEffects()[0].Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            piercing.GetEffects()[0].Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            var ignoreArmor = new CardTraitData(); ignoreArmor.Setup("CardTraitIgnoreArmor"); Set(piercing, "traits", new List<CardTraitData> { ignoreArmor });
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(battery.GetID()), Add(wound.GetID()), Add(health.GetID()), Add(corruption.GetID()), Add(pyre.GetID()),
                Add("8ccdb317-e3e3-47b1-95c0-662ffe3fb7c6"), Add("2ff4ef73-00f4-4bd5-b868-607320188980"), Add("acc303db-30ea-4ff1-8e46-97344210cb6b"),
                Add("6534ff5b-d036-4b2f-ac21-70ee4d68700e"), Add("24e4729c-f256-4524-84c8-99564affb42c"), Add("f25d8b79-056a-4db3-940d-c6cfc540bc4d"),
                Add("4ca4ed47-9867-4004-acf0-171c3282751f"), Add(heal.GetID()), Add("297c2618-1a10-4367-8b7c-d068a6c5b3f4"),
                Add("3c96e9b7-bdfb-4784-88f7-4b01bd58b4b7"), Add("1754dece-cf0b-455e-b9ca-d238134d7762"), Add("c354dd2c-0087-4eae-82d9-d4ab17b15f19"),
                Add("996a0953-168a-44b9-bb90-5e46cbbb3e09"), Add("7f3dcb62-e418-4120-861c-02f8372173ce"), Add("7f3dcb62-e418-4120-861c-02f8372173ce"),
                Add(piercing.GetID()), Add(hit.GetID()), Add(hit.GetID()), Add(hit.GetID()) };
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SUPPORT-SPELLS-PREPARED thirteen unchanged originals; wounded/full-health controls, Conduit, permanent Corruption, Hoard healing and positive/zero X capacity.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 3 && source.Spawn.Turn < 1) return null;
            if (IsolatedIds != null)
            {
                string? replayKind = source.PlayRules!.Cards.Single(rule => rule.DataId == source.Spawn.Train.Context!.FindCard(ids[index])!.DataId)
                    .Effects.FirstOrDefault(effect => effect.Type == "PlayUnitTrigger")?.UnitTriggerKind;
                IEnumerable<PlayCardAction> candidates = index < 4 ? Array.Empty<PlayCardAction>() : BattleActionModel.EnumerateSupportedPlays(source, ids[index]);
                if (replayKind != null) candidates = candidates.OrderByDescending(action => source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == action.TargetUnitId && unit.Triggers.Any(trigger => trigger.Kind == replayKind)));
                PlayCardAction? chosen = index < 4 ? new PlayCardAction(ids[index], index < 3 ? index : 0,
                    playerPosition: index < 3 ? 0 : -1) : candidates.FirstOrDefault();
                BattleActionResult? isolatedResult = chosen == null ? null : BattleActionModel.PlayCard(source, chosen);
                if (isolatedResult?.Supported == true) return chosen;
                string rejection = index + ":" + source.Spawn.Train.Context!.FindCard(ids[index])!.DataId;
                if (lastRejection != rejection)
                { lastRejection = rejection; logger?.LogInfo("ORIGINAL-ISOLATED-SPELL-REJECT index=" + index + " dataId=" + source.Spawn.Train.Context.FindCard(ids[index])!.DataId +
                    " reason=" + (isolatedResult?.Reason ?? string.Join("; ", BattleActionModel.EnumerateActions(source, ids[index]).UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct()) +
                        " default=" + BattleActionModel.PlayCard(source, new PlayCardAction(ids[index], 0)).Reason)); }
                return null;
            }
            int target = index is >= 8 and <= 16 or >= 23 ? source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == ids[0]).Id : 0;
            var action = new PlayCardAction(ids[index], index is 2 or 22 ? 1 : 0, playerPosition: index < 3 ? (index == 1 ? 1 : 0) : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(source, action);
            if (result.Supported) return action;
            if (lastRejection != result.Reason) { lastRejection = result.Reason; logger?.LogInfo("ORIGINAL-SUPPORT-SPELLS-REJECT index=" + index + " reason=" + result.Reason); }
            return null;
        }
        private static void PrepareIsolated(AllGameManagers managers, ManualLogSource log, string[] spellIds)
        {
            SaveManager save = managers.GetSaveManager(); AllGameData all = save.GetAllGameData(); CardManager cards = managers.GetCardManager()!;
            if (spellIds.Length == 0 || spellIds.Length > 30 || spellIds.Any(id => all.FindCardData(id)?.GetCardType() is not (CardType.Spell or CardType.Equipment or CardType.Monster)))
                throw new InvalidOperationException("Isolated spell IDs must name one to thirty native spell, equipment or supporting monster definitions.");
            Set(all.GetBalanceData(), "maxHandSize", 40);
            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!; CharacterData anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 1000); Set(anchor, "health", 10000); Set(anchor, "size", 1);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            cards.GetDrawPile().Clear();
            CardData energy = UnityEngine.Object.Instantiate(all.GetAllCardData().First(card => card.GetCardType() == CardType.Spell));
            energy.name = "PojuIsolatedSpellEnergy"; Set(energy, "id", "ac110052-0000-4000-8000-000000000099");
            Set(energy, "cost", 0); Set(energy, "costType", CardData.CostType.Default); Set(energy, "targetless", true); Set(energy, "targetsRoom", true);
            Set(energy, "traits", new List<CardTraitData>()); Set(energy, "startingUpgrades", new List<CardUpgradeData>());
            Set(energy, "triggers", new List<CardTriggerEffectData>()); Set(energy, "effectTriggers", new List<CharacterTriggerData>());
            var gain = new CardEffectData("CardEffectGainEnergy", null!, Team.Type.None); gain.Cheat_SetTargetMode(TargetMode.Room);
            Set(gain, "paramInt", Math.Max(1, spellIds.Sum(id => all.FindCardData(id)!.GetCost()) - 3));
            Set(energy, "effects", new List<CardEffectData> { gain });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(energy);
            CardState Add(string id) => cards.AddNewCard(all.FindCardData(id)!, CardPile.HandPile, false, false, animate: false);
            CardState[] created = new[] { Add(steward.GetID()), Add(steward.GetID()), Add(steward.GetID()), Add(energy.GetID()) }
                .Concat(spellIds.Select(Add)).ToArray();
            if (spellIds.Any(id => all.FindCardData(id)!.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectDrawType")))
                for (int drawControl = 0; drawControl < 6; drawControl++)
                    cards.AddNewCard(steward, CardPile.DeckPile, false, false, animate: false);
            if (spellIds.Any(id => all.FindCardData(id)!.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectRecursion" &&
                effect.GetTargetCardSelectionMode() == CardEffectData.CardSelectionMode.RandomToRoomUntilCapacityFull)))
                for (int recursionControl = 0; recursionControl < 6; recursionControl++)
                {
                    CardState exhausted = Add(steward.GetID());
                    cards.MoveToStandByPile(exhausted, wasPlayed: false, wasExhausted: true,
                        condition: new RemoveFromStandByCondition(() => CardPile.KeepInStandBy));
                }
            foreach (CardState card in created.Skip(3)) { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ISOLATED-SPELLS-PREPARED three native anchor controls, energy and unchanged originals " + string.Join(",", spellIds));
        }
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
