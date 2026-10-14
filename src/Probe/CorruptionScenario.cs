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
        internal const string KeeperId = "1acb9146-44b9-476c-a30e-5778750710a1";
        internal const string CorruptionRelicId = "f302d4a1-13ba-4907-a26d-f0d8eead6f2e";
        private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
        private static ManualLogSource? logger;
        private static int stewardCardId;
        private static int teammateCardId;
        private static int unearthedCardId;
        private static int echoesCardId;
        private static int nextSpell;
        private static readonly bool[] marked = new bool[2];
        internal static bool RelicTrigger => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "corruption-relic-trigger";
        internal static bool RelicAcquired { get; private set; }

        internal static bool UnitTrigger => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "corruption-unit-trigger";
        internal static bool CarverTrigger => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "corruption-carver-trigger";
        internal static bool KeeperTrigger => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "corruption-keeper-trigger";
        internal static bool Enabled => UnitTrigger || CarverTrigger || KeeperTrigger || RelicTrigger ||
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "corruption";
        internal static void AcquireRelicBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (!RelicTrigger || RelicAcquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Corruption relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == CorruptionRelicId);
            RelicEffectData[] effects = relic.GetEffects().ToArray();
            if (relic.name != "DamageOnCorruptPlay" || effects.Length != 2 || effects.Any(effect =>
                effect.GetEffectClassName() != "RelicEffectAddStatusEffectOnCorruptionAdded" ||
                effect.GetTargetMode() != TargetMode.RandomInRoom ||
                effect.GetParamSourceTeam() != Team.Type.Heroes || !effect.GetParamBool() ||
                effect.GetEffectConditions().Count != 0 || effect.GetParamStatusEffects().Length != 1 ||
                effect.GetParamStatusEffects()[0].statusId != "corrupt_poison" ||
                effect.GetParamStatusEffects()[0].count != 1))
                throw new InvalidOperationException("Original DamageOnCorruptPlay definition changed.");
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == CorruptionRelicId))
                throw new InvalidOperationException("Corruption relic is already present.");
            save.AddRelic(relic);
            RelicAcquired = true;
            log.LogInfo("CORRUPTION-RELIC-ACQUIRED unchanged DamageOnCorruptPlay; two RandomInRoom Heroes corrupt_poison+1 effects.");
        }
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
            stewardCardId = teammateCardId = unearthedCardId = echoesCardId = 0;
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
            CardData steward = data.FindCardData(UnitTrigger ? GlugsiderId : CarverTrigger ? CarverId : KeeperTrigger ? KeeperId : StewardId) ??
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
            if (KeeperTrigger)
            {
                CharacterData keeper = steward.GetSpawnCharacterData()!;
                CharacterTriggerData callback = keeper.GetTriggers().Single(trigger =>
                    trigger.GetTrigger() == CharacterTriggerData.Trigger.CorruptionAdded);
                CardEffectData[] effects = callback.GetEffects().ToArray();
                if (steward.GetAssetKey() != "KeeperOfEchoes" || keeper.GetAssetKey() != "MonsterKeeperOfEchoes" ||
                    effects.Length != 1 || effects[0].GetEffectStateName() != "CardEffectAddTempCardUpgradeToUnits" ||
                    effects[0].GetTargetMode() != TargetMode.Room ||
                    effects[0].GetTargetTeamType() != Team.Type.Monsters || effects[0].GetParamCardUpgradeData() == null)
                    throw new InvalidOperationException("Original Keeper of Echoes corruption callback changed.");
                log.LogInfo("CORRUPTION-KEEPER-DEFINITION cost=" + steward.GetCost() + " attack=" +
                    keeper.GetAttackDamage() + " hp=" + keeper.GetHealth() + " upgrade=" +
                    effects[0].GetParamCardUpgradeData()!.name);
            }
            if (echoes.GetEffects().Count != 2 || echoes.GetEffects()[1].GetEffectStateName() != "CardEffectAdjustCorruption" ||
                echoes.GetEffects()[1].GetParamInt() != -1 || echoes.GetEffects()[1].GetParamBool())
                throw new InvalidOperationException("Original remove-corruption definition changed.");
            CardManager cards = managers.GetCardManager()!;
            if (cards.GetHand().Count > (KeeperTrigger ? 6 : 7))
                throw new InvalidOperationException("Corruption fixture requires free hand slots.");
            CardState unit = cards.AddNewCard(steward, CardPile.HandPile, false, false, animate: false);
            CardState? teammate = KeeperTrigger ? cards.AddNewCard(data.FindCardData(StewardId)!,
                CardPile.HandPile, false, false, animate: false) : null;
            CardState? added = RelicTrigger ? null : cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
            CardState? remove = RelicTrigger ? null : cards.AddNewCard(echoes, CardPile.HandPile, false, false, animate: false);
            if (unit == null || (!RelicTrigger && (added == null || remove == null)) ||
                !cards.GetHand().Contains(unit) || (KeeperTrigger && (teammate == null || !cards.GetHand().Contains(teammate))) ||
                (added != null && !cards.GetHand().Contains(added)) ||
                (remove != null && !cards.GetHand().Contains(remove)))
                throw new InvalidOperationException("Corruption fixture could not add its original cards.");
            if (added != null) AccessTools.Field(typeof(CardState), "cost").SetValue(added, 0);
            if (remove != null) AccessTools.Field(typeof(CardState), "cost").SetValue(remove, 0);
            stewardCardId = FullBattleTrace.Active!.CardId(unit);
            teammateCardId = teammate == null ? 0 : FullBattleTrace.Active.CardId(teammate);
            unearthedCardId = added == null ? 0 : FullBattleTrace.Active.CardId(added);
            echoesCardId = remove == null ? 0 : FullBattleTrace.Active.CardId(remove);
            RoomState room = managers.GetRoomManager()!.GetRoom(0);
            log.LogInfo("CORRUPTION-PREPARED " + (UnitTrigger || CarverTrigger || KeeperTrigger ? "carrier=" : "steward=") + stewardCardId + " unearthed=" + unearthedCardId +
                " echoes=" + echoesCardId + " room=0 current=" +
                room.GetCurrentCorruption() + " max=" + room.GetMaxCorruption() + " permanent=" + room.GetPermanentCorruption());
        }

        internal static void PrepareRelicDecision(AllGameManagers managers, ManualLogSource log)
        {
            if (!RelicTrigger || unearthedCardId != 0 ||
                managers.GetRoomManager()!.GetRoom(0).GetNumLivingCharacters(Team.Type.Heroes) == 0) return;
            CardManager cards = managers.GetCardManager()!;
            if (cards.GetHand().Count > 7) throw new InvalidOperationException("Corruption relic fixture requires two hand slots.");
            AllGameData data = managers.GetSaveManager().GetAllGameData();
            CardState added = cards.AddNewCard(data.FindCardData(UnearthedId)!, CardPile.HandPile, false, false, animate: false);
            CardState remove = cards.AddNewCard(data.FindCardData(EchoesId)!, CardPile.HandPile, false, false, animate: false);
            if (added == null || remove == null || !cards.GetHand().Contains(added) || !cards.GetHand().Contains(remove))
                throw new InvalidOperationException("Corruption relic fixture could not add its original spells.");
            AccessTools.Field(typeof(CardState), "cost").SetValue(added, 0);
            AccessTools.Field(typeof(CardState), "cost").SetValue(remove, 0);
            unearthedCardId = FullBattleTrace.Active!.CardId(added);
            echoesCardId = FullBattleTrace.Active.CardId(remove);
            log.LogInfo("CORRUPTION-RELIC-SPELLS unearthed=" + unearthedCardId + " echoes=" + echoesCardId + " enemyRoom=0");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (nextSpell >= 2) return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            PlayCardAction[] plays = BattleActionModel.EnumerateSupportedPlays(source).ToArray();
            CombatUnit? steward = source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.SpawnerCardId == stewardCardId);
            if (steward == null) return plays.FirstOrDefault(item => item.CardInstanceId == stewardCardId && item.RoomIndex == 0)
                ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            if (KeeperTrigger && source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .All(unit => unit.SpawnerCardId != teammateCardId))
                return plays.FirstOrDefault(item => item.CardInstanceId == teammateCardId && item.RoomIndex == 0)
                    ?? BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
            if (UnitTrigger && nextSpell == 0 && steward.Health == steward.MaxHealth) return null;
            if (RelicTrigger && unearthedCardId == 0) return null;
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
