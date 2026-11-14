using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DevotionLateSpawnBattleScenario
    {
        internal const string Name = "devotion-late-spawn-battle";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        private static int[] cards = Array.Empty<int>();
        private static int step;
        private static object? definition, gates;
        private static ManualLogSource? logger;
        internal static object Evidence => new { Prepared, CompletedControlPlays = step, ControlCardIds = cards,
            OwnerDefinition = definition, Gates = gates, Records = DevotionLateSpawnProbe.Records };

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = managers.GetAllGameData(); var manager = managers.GetCardManager();
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0 ||
                DevotionProbe.Read(save, "GetDevotionLevel") != 0 || save.GetCollectedRelics().Any(owner =>
                    owner.GetRelicDataID() == RelicUnitSpawnEffectModel.DevotionDataId || owner.GetRelicDataID() == RelicUnitSpawnEffectModel.DataId))
                throw new InvalidOperationException("Late-spawn battle requires isolated deployment at zero Devotion.");
            RelicData owner = all.GetAllCollectableRelicData().Single(relic => relic.GetID() == RelicUnitSpawnEffectModel.DevotionDataId);
            RelicEffectData effect = owner.GetEffects().Single();
            definition = new { Id = owner.GetID(), Key = owner.name, Placement = owner.DisallowInDeploymentPhase,
                Type = effect.GetEffectClassName(), LateSpawn = effect.GetParamBool(), Nested = effect.GetParamCardEffectData()
                    .Select(item => new { Type = item.GetEffectStateName(), Divisor = item.GetAdditionalParamInt(),
                        Statuses = item.GetParamStatusEffects().Select(status => new { status.statusId, status.count }).ToArray() }).ToArray() };
            if (owner.name != "FriendlyUnitPunish" || effect.GetEffectClassName() != RelicUnitSpawnEffectModel.Effect || !effect.GetParamBool())
                throw new InvalidOperationException("Original late-spawn owner changed.");
            CardData steward = all.FindCardData("d14a50f3-728d-43e1-87f0-ef1b013f6678");
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(unit, "health", 100); Set(unit, "attackDamage", 60); Set(unit, "size", 1);
            Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null!);
            Set(unit, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "cooldown", count = 8 } });
            CardData venerate = all.FindCardData(DevotionSpawnBattleScenario.VenerateId);
            CardData whelp = UnityEngine.Object.Instantiate(steward); whelp.name = "PojuLateSpawnWhelp";
            Set(whelp, "id", "fc231d40-0000-4000-8000-000000000001");
            CharacterData whelpUnit = UnityEngine.Object.Instantiate(unit); whelpUnit.name = "PojuLateSpawnWhelpUnit";
            Set(whelpUnit, "id", "fc231d41-0000-4000-8000-000000000001");
            Set(whelpUnit, "subtypeKeys", new List<string> { RelicUnitSpawnEffectModel.Subtype });
            var gain = new CardEffectData("CardEffectGainDevotionPoints", null!, Team.Type.Monsters);
            gain.Cheat_SetTargetMode(TargetMode.Self); Set(gain, "paramInt", 3);
            CharacterTriggerData trigger = HealingScenario.HealGold(0, false, false);
            Set(trigger, "trigger", CharacterTriggerData.Trigger.OnSpawn); Set(trigger, "effects", new List<CardEffectData> { gain });
            Set(whelpUnit, "triggers", new List<CharacterTriggerData> { trigger });
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all)).Add(whelpUnit);
            Set(whelp.GetEffects().Single(), "paramCharacterData", whelpUnit);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(whelp);
            CardData divisor = UnityEngine.Object.Instantiate(venerate); divisor.name = "PojuDevotionDivisorControl";
            Set(divisor, "id", "fc231d40-0000-4000-8000-000000000002"); Set(divisor, "cost", 0);
            Set(divisor, "targetless", false); Set(divisor, "targetsRoom", true);
            var statusEffect = new CardEffectData("CardEffectAddStatusEffectPerDevotionLevel", null!, Team.Type.Monsters);
            statusEffect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(statusEffect, "additionalParamInt", 2);
            Set(statusEffect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = "zeal", count = 2 } });
            var noop = new CardEffectData("CardEffectNULL", null!, Team.Type.Monsters); noop.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(divisor, "effects", new List<CardEffectData> { statusEffect, noop }); Set(divisor, "traits", new List<CardTraitData>());
            Set(divisor, "triggers", new List<CardTriggerEffectData>()); Set(divisor, "effectTriggers", new List<CharacterTriggerData>());
            Set(divisor, "startingUpgrades", new List<CardUpgradeData>());
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(divisor);
            CardData copy = UnityEngine.Object.Instantiate(divisor); copy.name = "PojuLateSpawnCopyControl";
            Set(copy, "id", "fc231d40-0000-4000-8000-000000000003");
            var copyEffect = new CardEffectData("CardEffectCopyUnits", null!, Team.Type.Monsters);
            copyEffect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(copyEffect, "paramInt", 1);
            Set(copy, "effects", new List<CardEffectData> { copyEffect });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(copy);
            gates = DevotionStatusGateCalibration.Run(managers);
            foreach (CardState card in manager.GetHand().ToArray()) manager.MoveCardFromHandToTopOfDeck(card);
            manager.GetDrawPile().Clear();
            save.AddRelic(owner);
            save.AddRelic(all.GetAllCollectableRelicData().Single(relic => relic.GetID() == RelicUnitSpawnEffectModel.DataId));
            cards = new[] { steward, divisor, whelp, divisor, venerate, divisor, steward, copy }.Select(data =>
                manager.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                    throw new InvalidOperationException("Unable to add late-spawn input.")).Select(FullBattleTrace.Active!.CardId).ToArray();
            managers.GetPlayerManager().AddEnergy(10);
            managers.GetCardStatistics().GetStatValue(new CardStatistics.StatValueData { trackedValue = CardStatistics.TrackedValueType.TimesPlayed,
                cardState = manager.GetHand()[0], entryDuration = CardStatistics.EntryDuration.ThisBattle, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
            FullBattleTrace.Active!.InvalidateCardRules(); logger = log; Prepared = true;
            Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("DEVOTION-LATE-SPAWN-PREPARED unchanged FriendlyUnitPunish and Whelp cooldown owners; zero/one/two levels, OnSpawn level-up before OnSpawnLate, divisor-two controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (step >= cards.Length) return null;
            if (step == 1) DevotionLateSpawnProbe.Controls();
            bool summon = step is 0 or 2 or 6;
            int target = step is 1 or 3 or 5 or 7 ? state.Spawn.Train.Rooms[2].Units.Single(unit => unit.SpawnerCardId == cards[0]).Id : 0;
            var action = new PlayCardAction(cards[step], 2, playerPosition: summon ? 0 : -1, targetUnitId: target);
            BattleActionResult result = BattleActionModel.PlayCard(state, action);
            if (!result.Supported) { logger?.LogInfo("DEVOTION-LATE-SPAWN-REJECT " + step + ": " + result.Reason); return null; }
            step++; return action;
        }
        internal static bool IsScenarioAction(PlayCardAction action) => cards.Contains(action.CardInstanceId);
        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
