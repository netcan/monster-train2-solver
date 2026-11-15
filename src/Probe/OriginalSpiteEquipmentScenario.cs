using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSpiteEquipmentScenario
    {
        internal const string Name = "original-spite-equipment";
        internal const string Shackles = "a1375ed2-179f-4ede-9233-b88b604836ac";
        internal const string Sineater = "ad143b21-be42-43a5-84bb-780ab697f7c1";
        internal const string Ability = "489a5c34-b17d-47ed-a1a5-b07fe1b5b370";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definitions { get; private set; }
        internal static object? StatusDefinition { get; private set; }
        internal static object? Calibration { get; private set; }
        private static int step;
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Spite equipment requires isolated deployment.");
            Definitions = new[] { Shackles, Sineater, Ability }.Select(id => all.FindCardData(id)!).Select(card => new {
                Id = card.GetID(), Key = card.name, Cost = card.GetCost(), Rule = BattleActionProbe.Definition(card),
                Effects = card.GetEffects().Select(RuleCatalogProbe.Effect).ToArray(),
                Traits = card.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray() }).ToArray();
            var status = managers.GetStatusEffectManager().GetStatusEffectDataById("spite")!;
            StatusDefinition = new { Type = status.GetStatusEffectStateName(), Rule = BattleActionProbe.Status("spite", 1),
                Stage = status.GetPrimaryTriggerStage().ToString(), Stages = status.GetAdditionalTriggerStages().Select(stage => stage.ToString()).ToArray() };
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!; var anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 1000); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 1000);
            Set(all.GetBalanceData(), "maxHandSize", 32);
            CardData Host(int suffix, int equipmentLimit)
            {
                var data = UnityEngine.Object.Instantiate(steward); data.name = "PojuSpiteHost" + suffix;
                Set(data, "id", "fc231b89-0000-4000-8000-" + suffix.ToString("D12"));
                var unit = UnityEngine.Object.Instantiate(anchor); unit.name = data.name + "Body";
                Set(unit, "attackDamage", 10); Set(unit, "health", 40); Set(unit, "equipmentLimit", equipmentLimit);
                var spawn = new CardEffectData("CardEffectSpawnMonster", unit, Team.Type.Monsters); spawn.Cheat_SetTargetMode(TargetMode.Room);
                Set(data, "effects", new List<CardEffectData> { spawn }); definitions.Add(data); return data;
            }
            CardData Control(int suffix, string type, int value, bool room = false, string? statusId = null)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalSpareScenario.PainId)); data.name = "PojuSpiteControl" + suffix;
                Set(data, "id", "fc231b89-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", room); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, room ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(room ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", statusId == null ? value : 0);
                if (statusId != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = statusId, count = value } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(10, "CardEffectGainEnergy", 40, true); var hp34 = Control(11, "CardEffectSetHealth", 34);
            var hp33 = Control(12, "CardEffectSetHealth", 33); var reset = Control(13, "CardEffectRemoveStatusEffect", 999, statusId: "cooldown");
            var heal = Control(14, "CardEffectHeal", 9999); var hp38 = Control(15, "CardEffectSetHealth", 38);
            var remove = Control(16, "CardEffectRemoveEquipment", 0); var hp8 = Control(17, "CardEffectSetHealth", 8);
            var blank = Control(18, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing Spite equipment input: " + data.name);
            var shackles = all.FindCardData(Shackles)!; var sineater = all.FindCardData(Sineater)!;
            var created = new CardState?[] { Add(steward), Add(Host(1, 3)), Add(Host(2, 1)), Add(battery), Add(shackles), Add(sineater),
                Add(hp34), null, Add(reset), Add(hp33), null, Add(heal), Add(reset), null, Add(shackles), Add(reset), Add(hp38), null,
                Add(remove), Add(sineater), Add(hp8), null };
            foreach (var card in created.Skip(3).Where(card => card != null))
            { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card!.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(card => card == null ? 0 : FullBattleTrace.Active!.CardId(card)).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SPITE-EQUIPMENT-PREPARED unchanged Shackles/Sineater; five skill uses across current half-health, armor/spikes, healing, nonstackable duplicate status, natural combat, removal and lethal self-hit.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= CardIds.Length || step >= 3 && source.Spawn.Turn < 1 || step >= 18 && source.Spawn.Turn < 2) return null;
            if (Calibration == null && source.Spawn.Turn >= 1) Calibration = SpiteStatusProbe.Run(AllGameManagers.Instance!);
            int floor = step is 0 or 3 ? 2 : 0;
            var context = source.Spawn.Train.Context!;
            int position = step == 2 ? 1 : step <= 1 ? 0 : -1;
            int target = step >= 4 && CardIds[step] > 0 ? source.Spawn.Train.Rooms[0].Units
                .Single(unit => unit.SpawnerCardId == CardIds[step >= 19 ? 2 : 1]).Id : 0;
            PlayCardAction action;
            if (CardIds[step] == 0)
            {
                var actor = source.Spawn.Train.Rooms[0].Units.Single(unit => unit.SpawnerCardId == CardIds[step == 21 ? 2 : 1]);
                var cached = AbilityCardModel.Get(context, actor.Ability!.CardCreation!);
                if (!cached.Supported) throw new InvalidOperationException(cached.UnsupportedReason);
                action = new PlayCardAction(cached.Card!.InstanceId, 0, activatorUnitId: actor.Id);
            }
            else action = new PlayCardAction(CardIds[step], floor, position, target);
            var predicted = BattleActionModel.PlayCard(source, action);
            if (!predicted.Supported) throw new InvalidOperationException("Spite equipment input " + step + " unsupported: " + predicted.Reason);
            step++; return action;
        }
    }
}
