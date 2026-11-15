using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPenitentScenario
    {
        internal const string Name = "original-penitent";
        internal const string OriginalId = "f4360a0a-2be3-40c0-b1f1-da59ca42ffac";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static object? Definition { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Penitent Sanctuary requires isolated deployment.");
            var original = all.FindCardData(OriginalId)!;
            var originalUpgrade = new CardUpgradeState(); originalUpgrade.Setup(original.GetEffects().Single().GetParamCardUpgradeData());
            Definition = new { Id = original.GetID(), Key = original.name, Cost = original.GetCost(),
                Effects = original.GetEffects().Select(RuleCatalogProbe.Effect).ToArray(),
                Traits = original.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(),
                Upgrade = CardModifierProbe.Upgrade(originalUpgrade, rejectFilters: false, filtersAlreadyApplied: true, roomAttachmentUnitUpgrade: true) };
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!; var anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 60); Set(anchor, "health", 500); Set(anchor, "size", 1);
            var guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 400);
            foreach (CardState card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            Set(all.GetBalanceData(), "maxHandSize", 32);
            var ally = UnityEngine.Object.Instantiate(steward); ally.name = "PojuPenitentAlly";
            Set(ally, "id", "fc231b83-0000-4000-8000-000000000001");
            var body = UnityEngine.Object.Instantiate(anchor); body.name = "PojuPenitentAllyBody";
            Set(body, "attackDamage", 0); Set(body, "health", 30);
            var summon = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters);
            summon.Cheat_SetTargetMode(steward.GetEffects()[0].GetTargetMode()); Set(ally, "effects", new List<CardEffectData> { summon }); definitions.Add(ally);
            CardData Control(int suffix, string type, int value, bool targetless, string? status = null, bool enemy = false)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalSpareScenario.PainId)); data.name = "PojuPenitentControl" + suffix;
                Set(data, "id", "fc231b83-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", targetless); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, targetless ? Team.Type.None : enemy ? Team.Type.Heroes : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(targetless ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", status == null ? value : 0);
                if (status != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = value } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(10, "CardEffectGainEnergy", 20, true);
            var damage = Control(11, "CardEffectDamage", 4, false); var zero = Control(12, "CardEffectDamage", 0, false);
            var armor = Control(13, "CardEffectAddStatusEffect", 4, false, "armor");
            var silence = Control(14, "CardEffectAddStatusEffect", 1, false, "silenced");
            var unmute = Control(15, "CardEffectRemoveStatusEffect", 99, false, "silenced");
            var ascend = Control(16, "CardEffectBump", 1, false); var descend = Control(17, "CardEffectBump", -1, false);
            var blank = Control(18, "CardEffectNULL", 0, true); var enemyDamage = Control(19, "CardEffectDamage", 1, false, enemy: true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing Penitent control card: " + data.name);
            var created = new[] { Add(steward), Add(ally), Add(ally), Add(original), Add(battery),
                Add(damage), Add(damage), Add(zero), Add(armor), Add(damage), Add(silence), Add(damage), Add(unmute),
                Add(original), Add(damage), Add(all.FindCardData(RoomCapacityAttachmentScenario.CardDataId)!), Add(damage),
                Add(original), Add(ascend), Add(damage), Add(descend), Add(damage), Add(ally), Add(damage), Add(enemyDamage) };
            foreach (var card in created.Skip(3))
            { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PENITENT-PREPARED unchanged room attachment; Punish healing/upgrade, armor/zero/silence, replacement/removal/reinstall, movement and late-birth controls.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 4 && source.Spawn.Turn < 1) return null;
            int floor = index is 0 or 4 ? 2 : index is 2 or 6 or 19 or 20 ? 1 : 0;
            bool targetsUnit = index is 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 14 or 16 or 18 or 19 or 20 or 21 or 23 or 24;
            int target = !targetsUnit ? 0 : index == 24 ? source.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor)
                .Units.Single(unit => unit.AssetKey == "ShieldGuardT1_BasicNonAttack").Id :
                source.Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units.Single(unit =>
                    unit.SpawnerCardId == CardIds[index == 6 ? 2 : index == 23 ? 22 : 1]).Id;
            var chosen = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == floor && play.TargetUnitId == target);
            if (chosen == null)
            {
                var rejected = BattleActionModel.PlayCard(source, new PlayCardAction(CardIds[index], floor, targetUnitId: target));
                throw new InvalidOperationException("Penitent planned action " + index + " rejected: " + rejected.Reason);
            }
            return chosen;
        }
    }
}
