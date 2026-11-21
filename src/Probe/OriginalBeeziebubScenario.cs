#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBeeziebubScenario
    {
        internal const string Name = "original-beeziebub";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static object? CurrentDefinition => original == null ? null : Describe(original);
        internal static object? Witness { get; private set; }
        internal static readonly List<Record> Callbacks = new();

        private static SoulData? original;
        private static CardState? spell;
        private static int spellId;
        private static int[] defenders = Array.Empty<int>();
        private static bool casting;

        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public bool? WasPlayed { get; set; }
            public bool? IsRecast { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public bool Completed { get; set; }
            public object Before { get; set; } = null!;
            public object? After { get; set; }
        }

        private static object Snapshot()
        {
            var managers = AllGameManagers.Instance!;
            var cardManager = managers.GetCardManager();
            var save = managers.GetSaveManager();
            var trace = FullBattleTrace.Active!;
            var queued = (IEnumerable<CardUpgradeState>)AccessTools.Field(typeof(CardManager), "nextDrawnTempCardUpgrades").GetValue(cardManager);
            return new { Turn = managers.GetCombatManager().GetTurnCount(), Gold = save.GetGold(), SaveHP = save.GetTowerHP(),
                PyreHP = managers.GetRoomManager().GetPyreRoom().GetPyreHeart()?.GetHP(),
                InSavedDeck = save.GetDeckState().Any(card => ReferenceEquals(card, spell)),
                Purged = cardManager.GetPurgedPile().Count(card => ReferenceEquals(card, spell)),
                Hand = cardManager.GetHand().Select(card => new { Id = trace.CardId(card), DataId = card.GetCardDataID(),
                    TempUpgradeIds = card.GetTemporaryCardStateModifiers().GetCardUpgrades().Select(upgrade => upgrade.GetCardUpgradeDataId()).ToArray() }).ToArray(),
                QueuedUpgradeIds = queued.Select(upgrade => upgrade.GetCardUpgradeDataId()).ToArray() };
        }

        private static object Describe(SoulData soul)
        {
            var relic = soul.GetEffects().Single();
            var upgrade = soul.GetSoulCardUpgrade()!;
            var draw = upgrade.GetTraitDataUpgrades().Single(trait => trait.GetTraitStateName() == nameof(CardTraitDrawOnPlay));
            var queued = draw.GetCardUpgradeDataParam();
            return new { Id = soul.GetID(), Key = soul.name, Tier = soul.GetTierLevel(), Type = relic.GetEffectClassName(),
                UpgradeId = upgrade.GetID(), UpgradeKey = upgrade.name, ExcludeFromClones = upgrade.GetExcludeFromClones(),
                Traits = upgrade.GetTraitDataUpgrades().Select(trait => trait.GetTraitStateName()).ToArray(),
                DrawCount = draw.GetParamInt(), DrawUpgrade = queued == null ? null : new { Id = queued.GetID(), Key = queued.name,
                    Damage = queued.GetBonusDamage(), Heal = queued.GetBonusHeal(), CostReduction = queued.GetCostReduction(),
                    Unique = queued.IsUnique(), Traits = queued.GetTraitDataUpgrades().Select(trait => trait.GetTraitStateName()).ToArray() },
                OnCast = upgrade.GetCardTriggerUpgrades().Where(trigger => trigger.GetTrigger() == CardTriggerType.OnCast)
                    .SelectMany(trigger => trigger.GetCardEffects()).Select(effect => new { Type = effect.GetEffectStateName(),
                        Target = effect.GetTargetMode().ToString(), Team = effect.GetTargetTeamType().ToString(),
                        Percent = effect.GetParamInt(), RoundUp = effect.GetParamBool(), Range = effect.GetUseIntRange() }).ToArray() };
        }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (original != null) return;
            var save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetSelectedSouls().Count != 0 || save.GetMutators().Count != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Original Beeziebub requires a fresh isolated save.");
            int tier = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_BEEZIEBUB_TIER")!);
            string id = tier switch
            {
                1 => "5bdcd6b3-e604-484d-90ee-5c50402aaf2d",
                2 => "049d39c9-c487-4ad2-ab38-ea31eff7705b",
                3 => "a4099411-05cd-4a79-8d85-c410501ae132",
                _ => throw new InvalidOperationException("Expected Beeziebub tier 1, 2 or 3.")
            };
            var all = save.GetAllGameData();
            var soul = all.FindSoulData(id)!;
            var relic = soul.GetEffects().Single();
            var upgrade = soul.GetSoulCardUpgrade()!;
            var draw = upgrade.GetTraitDataUpgrades().Single(trait => trait.GetTraitStateName() == nameof(CardTraitDrawOnPlay));
            var onCast = upgrade.GetCardTriggerUpgrades().Single(trigger => trigger.GetTrigger() == CardTriggerType.OnCast)
                .GetCardEffects().Single();
            if (soul.name != "BeeziebubT" + tier || soul.GetTierLevel() != tier || relic.GetEffectClassName() != nameof(RelicEffectCardUpgrade) ||
                relic.GetParamCardType() != CardType.Spell || !upgrade.GetExcludeFromClones() ||
                !upgrade.GetTraitDataUpgrades().Select(trait => trait.GetTraitStateName())
                    .SequenceEqual(new[] { nameof(CardTraitSelfPurge), nameof(CardTraitDrawOnPlay) }) ||
                draw.GetParamInt() != (tier == 3 ? 5 : 3) || (draw.GetCardUpgradeDataParam() == null) != (tier == 1) ||
                onCast.GetEffectStateName() != nameof(CardEffectDamageByPercentRemainingHealth) || onCast.GetTargetMode() != TargetMode.Pyre ||
                onCast.GetParamInt() != 10 || onCast.GetUseIntRange() || onCast.GetParamBool())
                throw new InvalidOperationException("The original Beeziebub definition changed.");
            var treasure = all.FindCardData(OriginalSmeltedTreasureScenario.CardId)!;
            if (treasure.name != "SmeltedTreasure" || treasure.GetCardType() != CardType.Spell)
                throw new InvalidOperationException("Original Beeziebub control spell definition changed.");
            spell = save.AddCardToDeck(treasure, applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddSelectedSoul(soul);
            var soulState = save.GetSelectedSouls().Single(owner => owner.GetRelicDataID() == id);
            SoulHelper.AttachSoulEffectToCard(soulState, spell, save);
            if (!spell.GetCardStateModifiers().HasUpgrade(upgrade.GetID()))
                throw new InvalidOperationException("Native Soul helper did not attach the original Beeziebub upgrade.");
            save.SetGold(100, isTotalReset: true);
            original = soul;
            Definition = Describe(soul);
            log.LogInfo("ORIGINAL-BEEZIEBUB-ACQUIRED " + soul.name + " attached to unchanged Smelted Treasure by the native Soul helper.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager();
            var cards = managers.GetCardManager();
            if (original == null || spell == null || !managers.GetCombatManager().IsPlacementPhase || save.GetGold() != 100 ||
                !cards.GetAllCards(new List<CardState>()).Any(card => ReferenceEquals(spell, card)))
                throw new InvalidOperationException("Original Beeziebub assignment did not reach the native deployment input.");
            var units = OriginalRandomRoomCapacityScenario.CreateDeploymentControls(managers, 3, 1);
            if (!cards.DrawSpecificCard(spell, useEffectDelay: false))
                throw new InvalidOperationException("Could not draw the original Beeziebub control spell.");
            spellId = FullBattleTrace.Active!.CardId(spell);
            defenders = units.Select(FullBattleTrace.Active!.CardId).ToArray();
            Witness = new { SpellId = spellId, SpellDataId = spell.GetCardDataID(), DefenderIds = defenders };
            Prepared = true;
            OriginalRandomRoomCapacityScenario.RefreshDeploymentControls(managers);
            log.LogInfo("ORIGINAL-BEEZIEBUB-PREPARED three defenders and one unchanged original soul-enchanted spell.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var statistics = state.Spawn.Train.Context!.Statistics!;
            for (int floor = 0; floor < defenders.Length; floor++)
            {
                if (statistics.Value(defenders[floor], "TimesPlayed", "ThisBattle") == 0)
                    return BattleActionModel.EnumerateSupportedPlays(state, defenders[floor]).First(action => action.RoomIndex == floor);
            }
            if (casting) return null;
            int target = state.Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units
                .Single(unit => unit.SpawnerCardId == defenders[0]).Id;
            var play = BattleActionModel.EnumerateSupportedPlays(state, spellId).First(action => action.RoomIndex == 0 && action.TargetUnitId == target);
            casting = true;
            return play;
        }

        internal static bool IsScenarioAction(PlayCardAction action) => action.CardInstanceId == spellId || defenders.Contains(action.CardInstanceId);

        private static bool ObserveDiscard(CardManager.DiscardCardParams request) => Prepared && ReferenceEquals(request.discardCard, spell) &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        [HarmonyPatch(typeof(CardTraitDrawOnPlay), nameof(CardTraitDrawOnPlay.OnCardDiscarded))]
        private static class DrawPatch
        {
            private static void Postfix(CardManager.DiscardCardParams discardCardParams, ref IEnumerator __result)
            {
                if (ObserveDiscard(discardCardParams)) __result = Wrap(__result, "DrawOnPlay", discardCardParams);
            }
        }

        [HarmonyPatch(typeof(CardTraitSelfPurge), nameof(CardTraitSelfPurge.OnCardDiscarded))]
        private static class PurgePatch
        {
            private static void Postfix(CardManager.DiscardCardParams discardCardParams, ref IEnumerator __result)
            {
                if (ObserveDiscard(discardCardParams)) __result = Wrap(__result, "SelfPurge", discardCardParams);
            }
        }

        [HarmonyPatch(typeof(CardEffectDamageByPercentRemainingHealth), nameof(CardEffectDamageByPercentRemainingHealth.ApplyEffect))]
        private static class CastPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (!Prepared || !casting || AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                    cardEffectState.GetTargetMode() != TargetMode.Pyre || cardEffectState.GetParamInt() != 10) return;
                int[] targets = cardEffectParams.targets.Select(FullBattleTrace.Active!.UnitId).ToArray();
                __result = Wrap(__result, "OnCast", targets: targets);
            }
        }

        private static IEnumerator Wrap(IEnumerator native, string kind, CardManager.DiscardCardParams? discard = null, int[]? targets = null)
        {
            var row = new Record { Kind = kind, WasPlayed = discard?.wasPlayed, IsRecast = discard?.isRecastCopy,
                Targets = targets ?? Array.Empty<int>(), Before = Snapshot() };
            Callbacks.Add(row);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                row.After = Snapshot();
                row.Completed = true;
            }
            finally { (native as IDisposable)?.Dispose(); }
        }
    }
}
#endif
