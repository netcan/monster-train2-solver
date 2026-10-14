using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CorruptionScenario
    {
        internal const string UnearthedId = "17fb5276-2311-44c9-b662-b73581d9af56";
        internal const string EchoesId = "78996030-e8d1-4731-897e-ee4b809fa26c";
        internal const string GlugsiderId = "e83182b1-ae94-4f7e-b7fe-aea5b825ecbe";
        internal const string CarverId = "8cd475e4-6e39-4306-b544-026bf30f4eac";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static ManualLogSource? logger;
        private static int stewardCardId;
        private static int unearthedCardId;
        private static int echoesCardId;
        private static int nextSpell;
        private static readonly bool[] marked = new bool[2];

        internal static bool UnitTrigger => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "corruption-unit-trigger";
        internal static bool CarverTrigger => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "corruption-carver-trigger";
        internal static bool Enabled => UnitTrigger || CarverTrigger || Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "corruption";
        internal static bool IsScenarioAction(PlayCardAction action)
        {
            if (!Enabled) return false;
            int index = action.CardInstanceId == unearthedCardId ? 0 :
                action.CardInstanceId == echoesCardId ? 1 : -1;
            if (index < 0 || marked[index]) return false;
            marked[index] = true;
            return true;
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            nextSpell = 0;
            Array.Clear(marked, 0, marked.Length);
            SaveManager save = managers.GetSaveManager();
            AllGameData data = save.GetAllGameData();
            CardData definition = data.FindCardData(UnearthedId) ??
                throw new InvalidOperationException("Original Unearthed Remains is missing.");
            if (definition.GetEffects().Count != 2 ||
                definition.GetEffects()[0].GetEffectStateName() != "CardEffectAdjustRoomMaxCorruption" ||
                definition.GetEffects()[1].GetEffectStateName() != "CardEffectAdjustCorruption")
                throw new InvalidOperationException("Original Unearthed Remains corruption effects changed.");
            CardData echoes = data.FindCardData(EchoesId) ??
                throw new InvalidOperationException("Original Echoes of the Past is missing.");
            CardData steward = data.FindCardData(UnitTrigger ? GlugsiderId : CarverTrigger ? CarverId : StewardId) ??
                throw new InvalidOperationException("Original corruption carrier is missing.");
            if (UnitTrigger)
            {
                CharacterData glugsider = steward.GetSpawnCharacterData()!;
                CharacterTriggerData callback = glugsider.GetTriggers().Single(trigger =>
                    trigger.GetTrigger() == CharacterTriggerData.Trigger.CorruptionAdded);
                if (steward.GetAssetKey() != "Glugsider" || glugsider.GetAssetKey() != "MonsterGlugsider" ||
                    callback.GetEffects().Count != 1 || callback.GetEffects()[0].GetEffectStateName() != "CardEffectHeal")
                    throw new InvalidOperationException("Original Glugsider corruption-heal definition changed.");
                CardEffectData heal = callback.GetEffects()[0];
                log.LogInfo("CORRUPTION-UNIT-DEFINITION cost=" + steward.GetCost() + " hp=" + glugsider.GetHealth() +
                    " heal=" + heal.GetParamInt() + " target=" + heal.GetTargetMode() + " team=" + heal.GetTargetTeamType());
            }
            if (CarverTrigger)
            {
                CharacterData carver = steward.GetSpawnCharacterData()!;
                CharacterTriggerData callback = carver.GetTriggers().Single(trigger =>
                    trigger.GetTrigger() == CharacterTriggerData.Trigger.CorruptionAdded);
                CardEffectData[] effects = callback.GetEffects().ToArray();
                if (steward.GetAssetKey() != "ShardsoulCarver" || carver.GetAssetKey() != "MonsterShardsoulCarver" ||
                    effects.Length != 2 || effects[0].GetEffectStateName() != "CardEffectAddTempCardUpgradeToUnits" ||
                    effects[0].GetTargetMode() != TargetMode.Self || effects[0].GetParamCardUpgradeData() == null ||
                    effects[1].GetEffectStateName() != "CardEffectAddStatusEffect" ||
                    effects[1].GetParamStatusEffects().Length != 1 ||
                    effects[1].GetParamStatusEffects()[0].statusId != "armor" ||
                    effects[1].GetParamStatusEffects()[0].count != 3)
                    throw new InvalidOperationException("Original Shardsoul Carver corruption callback changed.");
                log.LogInfo("CORRUPTION-CARVER-DEFINITION cost=" + steward.GetCost() + " attack=" +
                    carver.GetAttackDamage() + " hp=" + carver.GetHealth() + " upgrade=" +
                    effects[0].GetParamCardUpgradeData()!.name + " armor=3");
            }
            if (echoes.GetEffects().Count != 2 || echoes.GetEffects()[1].GetEffectStateName() != "CardEffectAdjustCorruption" ||
                echoes.GetEffects()[1].GetParamInt() != -1 || echoes.GetEffects()[1].GetParamBool())
                throw new InvalidOperationException("Original remove-corruption definition changed.");
            CardManager cards = managers.GetCardManager()!;
            if (cards.GetHand().Count > 7) throw new InvalidOperationException("Corruption fixture requires three free hand slots.");
            CardState unit = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            CardState remove = cards.AddNewCard(echoes, CardPile.HandPile, false, false, animate: false);
            if (unit == null || added == null || remove == null ||
                !cards.GetHand().Contains(unit) || !cards.GetHand().Contains(added) ||
                !cards.GetHand().Contains(remove))
                throw new InvalidOperationException("Corruption fixture could not add its original cards.");
            AccessTools.Field(typeof(CardState), "cost").SetValue(added, 0);
            AccessTools.Field(typeof(CardState), "cost").SetValue(remove, 0);
            stewardCardId = FullBattleTrace.Active!.CardId(unit);
            unearthedCardId = FullBattleTrace.Active.CardId(added);
            echoesCardId = FullBattleTrace.Active.CardId(remove);
            RoomState room = managers.GetRoomManager()!.GetRoom(0);
            log.LogInfo("CORRUPTION-PREPARED " + (UnitTrigger || CarverTrigger ? "carrier=" : "steward=") + stewardCardId + " unearthed=" + unearthedCardId +
                " echoes=" + echoesCardId + " room=0 current=" +
                room.GetCurrentCorruption() + " max=" + room.GetMaxCorruption() + " permanent=" + room.GetPermanentCorruption());
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (nextSpell >= 2) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction[] plays = BattleActionModel.EnumerateSupportedPlays(source).ToArray();
            CombatUnit? steward = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.SpawnerCardId == stewardCardId);
            if (steward == null) return plays.FirstOrDefault(item => item.CardInstanceId == stewardCardId && item.RoomIndex == 0)
                ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            if (UnitTrigger && nextSpell == 0 && steward.Health == steward.MaxHealth) return null;
            int cardId = nextSpell == 0 ? unearthedCardId : echoesCardId;
            PlayCardAction? action = plays.FirstOrDefault(item => item.CardInstanceId == cardId && item.RoomIndex == 0 &&
                (nextSpell != 1 || item.TargetUnitId == steward.Id));
            if (action == null) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            BattleActionResult predicted = BattleActionModel.PlayCard(source, action);
            RoomCapacityState before = source.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0);
            RoomCapacityState? after = predicted.State?.Spawn.Train.Context?.RoomCapacities?.Single(room => room.RoomIndex == 0);
            logger!.LogInfo("CORRUPTION-CANDIDATE spell=" + nextSpell + " supported=" + predicted.Supported +
                " current=" + before.Corruption + "->" + after?.Corruption +
                " max=" + before.MaxCorruption + "->" + after?.MaxCorruption + " reason=" + predicted.Reason);
            if (!predicted.Supported) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            nextSpell++;
            return action;
        }
    }
}
