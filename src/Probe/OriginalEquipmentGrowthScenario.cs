using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEquipmentGrowthScenario
    {
        internal const string Name = "original-equipment-growth-relics";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool CaptureEncounterComplete => Prepared || OriginalSupportSpellsScenario.IsolatedIds?.Contains("f3406a6d-69f0-46e8-8db9-d829db017716") == true;
        internal const string GearId = "fc119000-0000-4000-8000-000000000391";
        internal const string WoundId = "fc119000-0000-4000-8000-000000000393";
        internal const string HealId = "fc119000-0000-4000-8000-000000000394";
        internal const string SacrificeId = "fc119000-0000-4000-8000-000000000395";
        internal static readonly string[] Ids = { "b3e7888f-8f5e-4082-89b0-078d80ce4a9b", "449f6464-8164-48ad-bf66-6107b10f7f5c", "19f0d8e3-a3f8-4cac-b73d-46dc189a70ac", "cec4a632-fc04-48ba-b982-ffcd16a0710e" };
        internal sealed class EncounterCompleteRecord
        {
            public TrainCombatState? Before { get; set; }
            public TrainCombatState? After { get; set; }
            public bool Completed { get; set; }
            public string? CaptureError { get; set; }
            public int[]? ActiveMonsterOrder { get; set; }
        }
        internal static readonly List<EncounterCompleteRecord> EncounterCompleteRecords = new List<EncounterCompleteRecord>();
        internal static bool EncounterCompleteSettled => EncounterCompleteRecords.Count == 1 &&
            EncounterCompleteRecords[0].Completed && EncounterCompleteRecords[0].After != null;
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            EncounterCompleteRecords.Clear();
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (managers.GetCombatManager()!.GetTurnCount() != 0 || save.PreviewMode)
                throw new InvalidOperationException("Original equipment growth requires deployment setup.");
            var owned = cards.GetAllCards(new List<CardState>()).ToArray();
            var stewards = owned.Select(card => data.FindCardData(card.GetCardDataID())!).Where(card =>
                card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true).Distinct().Take(2).ToArray();
            if (stewards.Length != 2) throw new InvalidOperationException("Two distinct Steward carriers required.");
            for (int index = 0; index < 2; index++)
            {
                var unit = stewards[index].GetSpawnCharacterData()!;
                Set(unit, "health", index == 0 ? 80 : 12); Set(unit, "attackDamage", index == 0 ? 20 : 2);
                Set(unit, "size", 1); Set(unit, "equipmentLimit", 2);
                Set(unit, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
                Set(unit, "triggers", new List<CharacterTriggerData>()); Set(unit, "unitAbility", null); Set(stewards[index], "cost", 0);
            }
            var gear = UnityEngine.Object.Instantiate(data.GetAllCardData().First(card => card.GetCardType() == CardType.Equipment &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectAttachEquipment"));
            Configure(gear, "PojuGrowthGear", GearId);
            var upgrade = DynamicUpgradeScenario.Upgrade("PojuGrowthGearUpgrade", "fc119000-0000-4000-8000-000000000392", 1, 0, 0, 0, "armor", 0);
            Set(upgrade, "statusEffectUpgrades", new List<StatusEffectStackData>());
            var attach = Effect("CardEffectAttachEquipment", 0); Set(attach, "paramCardUpgradeData", upgrade);
            Set(gear, "effects", new List<CardEffectData> { attach });
            var returning = new CardTraitData(); returning.Setup("CardTraitReturnToHandEquipment"); gear.GetTraits().Add(returning);
            var spell = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectDamage");
            CardData Spell(string key, string id, string effect, int amount)
            {
                var clone = UnityEngine.Object.Instantiate(spell); Configure(clone, key, id);
                Set(clone, "effects", new List<CardEffectData> { Effect(effect, amount) }); return clone;
            }
            var wound = Spell("PojuGrowthWound", WoundId, "CardEffectDamage", 8);
            var heal = Spell("PojuGrowthHeal", HealId, "CardEffectHeal", 12);
            var sacrifice = Spell("PojuGrowthSacrifice", SacrificeId, "CardEffectSacrifice", 0);
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data);
            definitions.AddRange(new[] { gear, wound, heal, sacrifice });
            foreach (var card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            foreach (var definition in new[] { stewards[0], stewards[1], gear, gear, wound, heal, sacrifice })
                if (cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false) == null)
                    throw new InvalidOperationException("Equipment growth carrier creation failed.");
            foreach (var id in Ids)
            {
                var relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == id);
                var effect = relic.GetEffects().Single();
                if (relic.DisallowInDeploymentPhase || effect.GetEffectClassName() != "RelicEffectAddTrigger" ||
                    !effect.GetParamCharacterSubtype().IsNone || !effect.GetParamBool() || effect.GetEffectConditions().Count != 0 ||
                    save.GetCollectedRelics().Any(item => item.GetRelicDataID() == id))
                    throw new InvalidOperationException("Original equipment growth owner changed.");
                save.AddRelic(relic);
            }
            Prepared = true; Set(managers.GetCombatManager()!, "combatStateChanged", true);
            log.LogInfo("ORIGINAL-EQUIPMENT-GROWTH-PREPARED four unchanged owners; equipped slayer, friendly damage/heal and equipped sacrifice carriers; original reposition spells, waves and boss.");
        }
        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            var context = state.Spawn.Train.Context!;
            var plays = BattleActionModel.EnumerateSupportedPlays(state);
            string? Data(int cardId) => context.FindCard(cardId)?.DataId;
            bool Played(string data) => context.Statistics!.Values.Any(value => value.Type == "TimesPlayed" && value.Duration == "ThisBattle" &&
                value.Value > 0 && context.FindCard(value.CardId)?.DataId == data);
            var players = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0).ToArray();
            foreach (bool strong in new[] { true, false })
            {
                bool Matches(CombatUnit unit) => unit.MaxHealth == (strong ? 80 : 12);
                if (players.Any(Matches)) continue;
                if (!strong && Played(SacrificeId)) continue;
                var summon = plays.OrderBy(play => play.RoomIndex).FirstOrDefault(play =>
                    state.Spawn.Train.Rooms.Single(room => room.RoomIndex == play.RoomIndex).Units.Count(unit => unit.Team == CombatTeam.Player) == play.PlayerPosition &&
                    state.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == Data(play.CardInstanceId))?.SpawnUnit is CombatUnit template && Matches(template));
                if (summon != null) return summon;
            }
            foreach (var unit in players.OrderByDescending(unit => unit.MaxHealth))
            {
                if (unit.EquipmentCards?.Count != 0) continue;
                var gear = plays.FirstOrDefault(play => Data(play.CardInstanceId) == GearId && play.TargetUnitId == unit.Id);
                if (gear != null) return gear;
            }
            var fragile = players.FirstOrDefault(unit => unit.MaxHealth == 12 && unit.EquipmentCards?.Count > 0);
            if (fragile != null)
            {
                string? wanted = !Played(WoundId) && fragile.Health == 12 ? WoundId :
                    Played(WoundId) && !Played(HealId) && fragile.Health < 6 ? HealId :
                    Played(HealId) ? SacrificeId : null;
                var action = plays.FirstOrDefault(play => wanted != null && Data(play.CardInstanceId) == wanted && play.TargetUnitId == fragile.Id);
                if (action != null) return action;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card => Data(card.InstanceId) != GearId && Data(card.InstanceId) != WoundId &&
                Data(card.InstanceId) != HealId && Data(card.InstanceId) != SacrificeId);
        }
        private static void Configure(CardData card, string key, string id)
        {
            card.name = key; Set(card, "id", id); Set(card, "cost", 0);
            Set(card, "traits", new List<CardTraitData>()); Set(card, "triggers", new List<CardTriggerEffectData>());
            Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }
        private static CardEffectData Effect(string type, int amount)
        {
            var effect = new CardEffectData(type, null!, Team.Type.Monsters);
            effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter); Set(effect, "paramInt", amount); return effect;
        }
        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);

        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.DoEncounterCompleteEffects))]
        private static class EncounterCompletePatch
        {
            private static void Postfix(ref IEnumerator __result)
            {
                FullBattleTrace? trace = FullBattleTrace.Active;
                if (!CaptureEncounterComplete || trace == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                if (EncounterCompleteRecords.Count != 0)
                { trace.CaptureFailure(new InvalidOperationException("Encounter-complete callback was observed more than once.")); return; }
                __result = Observe(__result, trace);
            }

            private static IEnumerator Observe(IEnumerator native, FullBattleTrace trace)
            {
                var record = new EncounterCompleteRecord();
                EncounterCompleteRecords.Add(record);
                try
                {
                    record.Before = trace.CaptureTrain();
                    record.ActiveMonsterOrder = ((IEnumerable<CharacterState>)AccessTools.Field(typeof(MonsterManager), "activeMonsters")
                        .GetValue(AllGameManagers.Instance!.GetMonsterManager())).Select(trace.UnitId).ToArray();
                }
                catch (Exception error) { record.CaptureError = error.ToString(); trace.CaptureFailure(error); }
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    record.Completed = true;
                }
                finally
                {
                    (native as IDisposable)?.Dispose();
                    try { record.After = trace.CaptureTrain(); }
                    catch (Exception error) { record.CaptureError = error.ToString(); trace.CaptureFailure(error); }
                }
            }
        }
    }
}
