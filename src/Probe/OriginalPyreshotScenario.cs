using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPyreshotScenario
    {
        internal const string Name = "original-pyreshot";
        internal const string CardDataId = "c30c479c-e7aa-4628-9091-dd5d2fb8e5a6";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            AllGameData all = save.GetAllGameData();
            CardManager cards = managers.GetCardManager()!;
            CardData pyreshot = all.FindCardData(CardDataId)!;
            CardEffectData effect = pyreshot.GetEffects().Single();
            if (pyreshot.name != "Pyreshot" || pyreshot.GetCardType() != CardType.Spell || pyreshot.GetCost() != 3 ||
                pyreshot.GetTraits().Count != 0 || effect.GetEffectStateName() != "CardEffectAttackWithUnit" ||
                effect.GetTargetMode() != TargetMode.Pyre || effect.GetTargetTeamType() != Team.Type.Heroes ||
                effect.GetParamBool() || !effect.GetParamBool2())
                throw new InvalidOperationException($"Original Pyreshot definition changed: asset={pyreshot.name}, type={pyreshot.GetCardType()}, cost={pyreshot.GetCost()}, traits={pyreshot.GetTraits().Count}, effect={effect.GetEffectStateName()}, target={effect.GetTargetMode()}, team={effect.GetTargetTeamType()}, bool={effect.GetParamBool()}, bool2={effect.GetParamBool2()}.");

            CardData steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            CharacterData unit = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0);
            Set(unit, "attackDamage", 9999);
            Set(unit, "health", 9999);
            Set(unit, "size", 1);
            Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            foreach (CardState state in cards.GetAllCards(new List<CardState>()).Where(state => state.GetCardDataID() == steward.GetID()))
                state.Setup(steward, save);
            foreach (CardState state in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(state);
            cards.GetDrawPile().Clear();
            CardState protector = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState spell = cards.AddNewCard(pyreshot, CardPile.HandPile, false, false, animate: false);
            CardIds = new[] { FullBattleTrace.Active!.CardId(protector), FullBattleTrace.Active.CardId(spell) };
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true;
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PYRESHOT-PREPARED unchanged three-cost Pyreshot and one high-attack native protector.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
        {
            int index = Array.FindIndex(ids, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0) return null;
            if (index == 0)
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
            if (source.Spawn.Turn < 1 || !source.Spawn.Train.Rooms[0].Units.Any(unit => unit.Team == CombatTeam.Enemy)) return null;
            return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == ids[index] && play.RoomIndex == 0);
        }

        private static void Set(object target, string field, object value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
