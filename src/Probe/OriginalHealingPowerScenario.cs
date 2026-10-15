using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalHealingPowerScenario
    {
        internal const string Name = "original-healing-power-relic";
        internal const string DataId = "03613302-e55a-4e6c-99a1-c198c8d3175c";
        internal const string CarrierId = "fc119000-0000-4000-8000-000000000401";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool CaptureReady { get; private set; }
        private static int spellId;
        private static bool played;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Healing relic must be acquired before battle.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("Healing relic already present.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-HEALING-POWER-ACQUIRED unchanged IncreaseSpellHealing +10.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (CaptureReady) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Healing relic missed deployment.");
            SaveManager save = managers.GetSaveManager(); AllGameData data = save.GetAllGameData();
            CheckOriginal(data.GetAllCollectableRelicData().Single(item => item.GetID() == DataId));
            CardData[] deck = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID())!).ToArray();
            CardData monster = deck.First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            bool Scales(CardData card) => new CardState(card, save, setupStartingUpgrades: false).CanScaleWithMagicPower();
            CardData spell = deck.First(card => card.GetCardType() == CardType.Spell && Scales(card));
            CardData noScale = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell && !Scales(card));
            CardData blight = data.FindCardData("21038774-c0a4-413c-ab7e-27e458228e5f")!;
            var stronger = UnityEngine.Object.Instantiate(spell);
            stronger.name = "PojuHealingPowerStrong"; Set(stronger, "id", "fc119000-0000-4000-8000-000000000402");
            var trait = new CardTraitData(); trait.Setup("CardTraitStrongerMagicPower");
            Set(stronger, "traits", new List<CardTraitData> { trait });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(stronger);
            CaptureReady = true;
            foreach (CardData definition in new[] { monster, spell, noScale, blight, stronger })
            {
                var card = new CardState(definition, save, setupStartingUpgrades: false);
                managers.GetRelicManager().ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                managers.GetRelicManager().ApplyCardStateModifiers(card);
                managers.GetRelicManager().ApplyCardStateModifiers(card);
            }
            // A controlled spell damages then heals the same player room. The
            // original relic is unchanged; only this isolated carrier is synthetic.
            var carrier = UnityEngine.Object.Instantiate(noScale);
            carrier.name = "PojuHealingPowerCarrier"; Set(carrier, "id", CarrierId);
            Set(carrier, "cost", 0); Set(carrier, "costType", CardData.CostType.Default);
            Set(carrier, "targetless", false); Set(carrier, "targetsRoom", true);
            Set(carrier, "traits", new List<CardTraitData>());
            Set(carrier, "triggers", new List<CardTriggerEffectData>());
            Set(carrier, "effectTriggers", new List<CharacterTriggerData>());
            Set(carrier, "startingUpgrades", new List<CardUpgradeData>());
            var damage = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
            damage.Cheat_SetTargetMode(TargetMode.FrontInRoom); Set(damage, "paramInt", 6);
            var heal = new CardEffectData("CardEffectHeal", null!, Team.Type.Monsters);
            heal.Cheat_SetTargetMode(TargetMode.LastTargetedCharacters); Set(heal, "paramInt", 1);
            Set(carrier, "effects", new List<CardEffectData> { damage, heal });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(carrier);
            CardState added = managers.GetCardManager()!.AddNewCard(carrier, CardPile.HandPile, false, false, animate: false);
            spellId = FullBattleTrace.Active!.CardId(added);
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-HEALING-POWER-PREPARED 15 detached refreshes: scalable/non-scalable spells, monster, blight, stronger trait; synthetic damage-then-heal carrier.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (played) return null;
            var plays = BattleActionModel.EnumerateSupportedPlays(state);
            return plays.FirstOrDefault(action => action.CardInstanceId == spellId &&
                state.Spawn.Train.Rooms.Any(room => room.RoomIndex == action.RoomIndex &&
                    room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre)?.Health > 6));
        }
        internal static bool IsScenarioPrelude(PlayCardAction action)
        { if (action.CardInstanceId != spellId || played) return false; played = true; return true; }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != "IncreaseSpellHealing" || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectModifyCardHealingPower" || effect.GetParamInt() != 10 ||
                effect.GetParamInt2() != 0 || effect.GetParamFloat() != 0 || effect.GetParamBool() ||
                effect.GetParamBool2() || effect.GetParamBool3() || effect.GetParamBool4() ||
                effect.GetParamCardType() != CardType.Monster || effect.GetParamCardFilter() != null ||
                effect.GetParamCharacterSubtype()?.IsNone != true || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original IncreaseSpellHealing definition changed.");
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
