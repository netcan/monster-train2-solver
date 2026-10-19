using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPerfectInsanityScenario
    {
        internal const string Name = "original-perfect-insanity";
        internal const string CardId = "9d04efb5-8794-42a8-8282-e8ef895e6c3a";
        internal const string UpgradeId = "10c215e1-0317-4e83-b551-d601b7858219";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData spell = all.FindCardData(CardId) ??
                throw new InvalidOperationException("Original Perfect Insanity CardData lookup failed: " + CardId);
            IReadOnlyList<CardEffectData> effects = spell.GetEffects();
            CardTraitData[] traits = spell.GetTraits().ToArray();
            CardUpgradeData? upgrade = effects.Count == 1 ? effects[0].GetParamCardUpgradeData() : null;
            var upgradeState = new CardUpgradeState();
            if (upgrade != null) upgradeState.Setup(upgrade);
            CardUpgradeModifier? projectedUpgrade = upgrade == null ? null : CardModifierProbe.Upgrade(upgradeState);
            if (spell.name != "PerfectInsanity" || spell.GetCardType() != CardType.Spell || spell.GetCost() != 1 ||
                effects.Count != 1 || effects[0].GetEffectStateName() != "CardEffectAddTempCardUpgradeToUnits" ||
                effects[0].GetTargetMode().ToString() != "DropTargetCharacter" ||
                !effects[0].GetTargetTeamType().HasFlag(Team.Type.Heroes) ||
                !effects[0].GetTargetTeamType().HasFlag(Team.Type.Monsters) || effects[0].GetParamInt() != 1 ||
                upgrade?.GetID() != UpgradeId || upgrade.name != "Minus666Health" || projectedUpgrade?.Stats.Health != -666 ||
                traits.Length != 1 || traits[0].GetTraitStateName() != "CardTraitCorruptRestricted" ||
                traits[0].GetParamInt() != 6 || traits[0].GetParamInt2() != 0 || traits[0].GetParamInt3() != 0 ||
                traits[0].GetParamBool() || traits[0].GetParamStr() != "" || traits[0].GetParamFloat() != 0 ||
                traits[0].GetUseScalingParams() || traits[0].GetParamTrackedValue().ToString() != "SubtypeInDeck" ||
                traits[0].GetParamEntryDuration().ToString() != "ThisTurn" || spell.GetCardTriggers().Count != 0)
                throw new InvalidOperationException("Original Perfect Insanity definition changed.");

            CardData steward = all.FindCardData(StewardId) ??
                throw new InvalidOperationException("Original Train Steward CardData lookup failed: " + StewardId);
            CharacterData unit = steward.GetSpawnCharacterData() ??
                throw new InvalidOperationException("Train Steward spawn data lookup failed.");
            Set(steward, "cost", 0); Set(unit, "attackDamage", 9999); Set(unit, "health", 9999); Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState existing in cards.GetAllCards(new List<CardState>()).Where(item => item.GetCardDataID() == steward.GetID()))
                existing.Setup(steward, save);
            foreach (CardState existing in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(existing);
            cards.GetDrawPile().Clear();

            CardState anchor = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState target = cards.AddNewCard(spell, CardPile.HandPile, false, false, animate: false);
            if (anchor == null || target == null || !cards.GetHand().Contains(anchor) || !cards.GetHand().Contains(target))
                throw new InvalidOperationException("Original Perfect Insanity scenario could not add its controlled cards.");
            CardIds = new[] { anchor, target }.Select(FullBattleTrace.Active!.CardId).ToArray();

            RoomManager rooms = managers.GetRoomManager()!;
            RoomState room = rooms.GetRoom(0);
            object stats = AccessTools.Field(typeof(RoomState), "corruptionStats").GetValue(room);
            object preview = AccessTools.Field(typeof(RoomState), "previewCorruptionStats").GetValue(room);
            int maximum = Math.Max(6, (int)AccessTools.Field(stats.GetType(), "maxCorruption").GetValue(stats));
            Set(stats, "maxCorruption", maximum);
            Set(preview, "maxCorruption", Math.Max(6, (int)AccessTools.Field(preview.GetType(), "maxCorruption").GetValue(preview)));
            Set(stats, "currentCorruption", 6);
            Set(preview, "currentCorruption", 6);
            Set(rooms.GetRoomUI(), "selectedRoom", 0);

            save.SetForgePoints(0);
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PERFECT-INSANITY-PREPARED unchanged one-cost spell, six selected-room corruption and durable friendly target for the temporary minus-666-health upgrade.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            CombatContext context = source.Spawn.Train.Context!;
            if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
            {
                var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
                if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
            }
            if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0)
            {
                CombatUnit? target = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.Health > 0 && unit.SpawnerCardId == ids[0]);
                if (target == null) return null;
                PlayCardAction? play = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                    action.CardInstanceId == ids[1] && action.RoomIndex == 0 && action.TargetUnitId == target.Id);
                return play != null && BattleActionModel.PlayCard(source, play).Supported ? play : null;
            }
            return null;
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
