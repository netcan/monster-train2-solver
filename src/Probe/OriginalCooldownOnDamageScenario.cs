using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCooldownOnDamageScenario
    {
        internal const string Name = "original-cooldown-on-damage-dh";
        internal const string OwnerId = RelicCardDamageStatusModel.DataId;
        internal const string OwnerKey = RelicCardDamageStatusModel.AssetKey;
        internal const string CarrierCardId = "fc119000-0000-4000-8000-000000000551";
        internal const string CarrierCharacterId = "fc119000-0000-4000-8000-000000000552";
        internal const string DamageCardId = "fc119000-0000-4000-8000-000000000553";
        internal const string HealCardId = "fc119000-0000-4000-8000-000000000554";
        internal const string CarrierAssetKey = "PojuCooldownOnDamageCarrier";

        private static int step;
        private static int lastLoggedStep = -1;
        private static int lastDiagnosticTurn = -1;
        private static bool scenarioActionMarkAvailable;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static int CarrierCardInstanceId { get; private set; }
        internal static int DamageCardInstanceId { get; private set; }
        internal static int HealCardInstanceId { get; private set; }
        internal static int CarrierUnitId { get; private set; }
        internal static IReadOnlyList<int> CardIds => new[] { CarrierCardInstanceId, DamageCardInstanceId, HealCardInstanceId };
        internal static object Evidence => new
        {
            OwnerId, AssetKey = OwnerKey, CarrierCardId, CarrierCharacterId, CarrierAssetKey,
            DamageCardId, HealCardId, CarrierCardInstanceId, DamageCardInstanceId,
            HealCardInstanceId, CarrierUnitId, InitialCooldown = 3, ExpectedCooldownAfterDamage = 2,
            NativeControl = "empty-spell-without-damage-does-not-fire"
        };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("CooldownOnDamage must be acquired once before the isolated battle.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == OwnerId);
            CheckOriginal(relic);
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-COOLDOWN-ON-DAMAGE-ACQUIRED unchanged owner=" + OwnerKey + " id=" + OwnerId + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            if (Prepared || !Acquired || save.PreviewMode || managers.GetCombatManager()!.GetTurnCount() != 0 ||
                save.GetCollectedRelics().Count(item => item.GetRelicDataID() == OwnerId) != 1)
                throw new InvalidOperationException("CooldownOnDamage requires one unchanged owner at deployment.");
            CheckOriginal(data.GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId));

            CardData[] owned = cards.GetAllCards(new List<CardState>()).Select(card => data.FindCardData(card.GetCardDataID()))
                .Where(card => card != null).Cast<CardData>().ToArray();
            CardData unitTemplate = owned.FirstOrDefault(card => card.GetSpawnCharacterData()?.name
                .StartsWith("TrainSteward", StringComparison.Ordinal) == true)
                ?? throw new InvalidOperationException("CooldownOnDamage needs an owned Train Steward template.");
            CardData spellTemplate = data.GetAllCardData().Where(card => card != null && card.GetCardType() == CardType.Spell &&
                !card.IsUnitAbility() && !card.IsRoomAbility()).OrderBy(card => card.GetID(), StringComparer.Ordinal).First();
            if (data.FindCharacterData(CarrierCharacterId) != null || data.FindCardData(CarrierCardId) != null ||
                data.FindCardData(DamageCardId) != null || data.FindCardData(HealCardId) != null)
                throw new InvalidOperationException("CooldownOnDamage witness asset IDs are already registered.");

            CharacterData character = UnityEngine.Object.Instantiate(unitTemplate.GetSpawnCharacterData()!);
            character.name = CarrierAssetKey;
            Set(character, "id", CarrierCharacterId);
            Set(character, "health", 40);
            Set(character, "attackDamage", 0);
            Set(character, "size", 1);
            Set(character, "startingStatusEffects", new[] { new StatusEffectStackData { statusId = "cooldown", count = 3 } });
            Set(character, "triggers", new List<CharacterTriggerData>());
            Set(character, "unitAbility", null!);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)!).Add(character);

            CardData carrier = UnityEngine.Object.Instantiate(unitTemplate);
            carrier.name = "PojuCooldownOnDamageCarrier";
            Configure(carrier, CarrierCardId, CardType.Monster);
            Set(carrier, "targetless", false);
            Set(carrier, "targetsRoom", true);
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
            spawn.Cheat_SetTargetMode(TargetMode.Room);
            Set(spawn, "paramCharacterData", character);
            Set(spawn, "paramBool", false);
            Set(spawn, "paramInt", 1);
            Set(carrier, "effects", new List<CardEffectData> { spawn });

            CardData damage = Spell(spellTemplate, DamageCardId, "PojuCooldownDamageWitness");
            var hit = new CardEffectData("CardEffectDamage", null!, Team.Type.Monsters);
            hit.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(hit, "paramInt", 2);
            Set(damage, "effects", new List<CardEffectData> { hit });

            CardData heal = Spell(spellTemplate, HealCardId, "PojuCooldownDamageHealControl");
            Set(heal, "targetless", true);
            Set(heal, "effects", new List<CardEffectData>());

            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)!)
                .AddRange(new[] { carrier, damage, heal });
            CardState carrierState = Add(carrier);
            CardState damageState = Add(damage);
            CardState healState = Add(heal);
            CarrierCardInstanceId = FullBattleTrace.Active!.CardId(carrierState);
            DamageCardInstanceId = FullBattleTrace.Active.CardId(damageState);
            HealCardInstanceId = FullBattleTrace.Active.CardId(healState);
            step = 0;
            lastLoggedStep = -1;
            lastDiagnosticTurn = -1;
            scenarioActionMarkAvailable = false;
            CarrierUnitId = 0;
            Prepared = true;
            FullBattleTrace.Active!.InvalidateCardRules();
            Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-COOLDOWN-ON-DAMAGE-PREPARED unchanged owner; cardIds=" + CarrierCardInstanceId + "/" +
                DamageCardInstanceId + "/" + HealCardInstanceId + " cooldown=3 Monster, one friendly 2-damage spell and one empty-Spell no-damage control.");

            CardState Add(CardData card)
            {
                CardState state = cards.AddNewCard(card, CardPile.HandPile, false, false, animate: false);
                if (state == null || !cards.GetHand().Contains(state))
                    throw new InvalidOperationException("Could not add CooldownOnDamage witness card " + card.name + " to hand.");
                return state;
            }
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            if (!Prepared || step >= 3) return null;
            BattleActionEnumeration enumeration = BattleActionModel.EnumerateActions(state);
            IReadOnlyList<PlayCardAction> plays = enumeration.SupportedActions;
            PlayCardAction? action;
            if (step == 0)
            {
                action = plays.FirstOrDefault(play => play.CardInstanceId == CarrierCardInstanceId);
            }
            else
            {
                CarrierUnitId = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Where(unit => unit.SpawnerCardId == CarrierCardInstanceId && unit.AssetKey == CarrierAssetKey)
                    .Select(unit => unit.Id).FirstOrDefault();
                if (CarrierUnitId == 0) return null;
                action = step == 1
                    ? plays.FirstOrDefault(play => play.CardInstanceId == DamageCardInstanceId && play.TargetUnitId == CarrierUnitId)
                    : plays.FirstOrDefault(play => play.CardInstanceId == HealCardInstanceId);
            }
            if (action != null)
            {
                step++;
                scenarioActionMarkAvailable = true;
            }
            if (lastLoggedStep != step || lastDiagnosticTurn != state.Spawn.Turn)
            {
                lastLoggedStep = step;
                lastDiagnosticTurn = state.Spawn.Turn;
                CombatContext context = state.Spawn.Train.Context!;
                string witnessHand = string.Join(",", context.Cards.Hand.Where(card => CardIds.Contains(card.InstanceId))
                    .Select(card => card.InstanceId + ":" + card.DataId));
                string witnessRules = string.Join(",", state.PlayRules!.Cards.Where(rule =>
                        context.Cards.Hand.Any(card => CardIds.Contains(card.InstanceId) && card.DataId == rule.DataId))
                    .Select(rule => rule.DataId + ":" + rule.CardType + ":" + rule.Effect + ":" +
                        string.Join("+", rule.Effects.Select(effect => effect.Type))));
                string unsupported = string.Join(";", enumeration.UnsupportedAttempts.Where(item =>
                        item.Action != null && CardIds.Contains(item.Action.CardInstanceId))
                    .Select(item => item.Action!.CardInstanceId + ":" + item.Reason).Distinct());
                UnityEngine.Debug.Log("[Monster Train 2 Isolation Probe] ORIGINAL-COOLDOWN-ON-DAMAGE-SELECT step=" +
                    step + " turn=" + state.Spawn.Turn + " carrierUnit=" + CarrierUnitId +
                    " hand=" + witnessHand + " rules=" + witnessRules + " unsupported=" + unsupported +
                    " chosen=" + (action == null ? "none" : action.CardInstanceId + "@" + action.RoomIndex) +
                    " legal=" + string.Join(",", plays.Where(play => CardIds.Contains(play.CardInstanceId))
                        .Select(play => play.CardInstanceId + "@" + play.RoomIndex + ":target=" + play.TargetUnitId)));
            }
            return action;
        }

        internal static PlayCardAction? AvoidUnsequencedWitness(BattleTurnState state, PlayCardAction? action)
        {
            if (action == null || !CardIds.Contains(action.CardInstanceId)) return action;
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => !CardIds.Contains(card.InstanceId));
        }

        internal static bool IsScenarioAction(PlayCardAction action)
        {
            if (!Enabled || !scenarioActionMarkAvailable || !CardIds.Contains(action.CardInstanceId)) return false;
            scenarioActionMarkAvailable = false;
            return true;
        }

        internal static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            StatusEffectStackData status = effect.GetParamStatusEffects().Single();
            if (relic.GetID() != OwnerId || relic.name != OwnerKey ||
                effect.GetEffectClassName() != RelicCardDamageStatusModel.Effect ||
                effect.GetParamSourceTeam() != Team.Type.Monsters || !effect.GetParamBool() ||
                status.statusId != "cooldown" || status.count != -1 || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original CooldownOnDamage_DH identity or payload changed.");
        }

        private static CardData Spell(CardData template, string id, string name)
        {
            CardData card = UnityEngine.Object.Instantiate(template);
            card.name = name;
            Configure(card, id, CardType.Spell);
            Set(card, "targetless", false);
            Set(card, "targetsRoom", true);
            return card;
        }

        private static void Configure(CardData card, string id, CardType type)
        {
            Set(card, "id", id);
            Set(card, "cardType", type);
            Set(card, "cost", 0);
            Set(card, "costType", CardData.CostType.Default);
            Set(card, "isUnitAbility", false);
            Set(card, "isRoomAbility", false);
            Set(card, "traits", new List<CardTraitData>());
            Set(card, "triggers", new List<CardTriggerEffectData>());
            Set(card, "effectTriggers", new List<CharacterTriggerData>());
            Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }

        private static void Set(object target, string field, object value) =>
            AccessTools.Field(target.GetType(), field)!.SetValue(target, value);
    }
}
