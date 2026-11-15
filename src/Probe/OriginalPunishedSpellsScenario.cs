using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPunishedSpellsScenario
    {
        internal const string Name = "original-punished-spells";
        internal static readonly string[] Originals = {
            "0a63cd82-9aee-4991-98af-4cc4233ed77e", "0c6d7cb8-510a-4658-8cb0-4ec6752d97db",
            "4e1ada4e-1460-4ebf-a8b3-5c0706242839", "64e5b645-a0a8-4143-8448-95c4ad4559a7",
            "dd7d8a6e-459d-4ccc-ab96-efb34b79f149", "ea72d9ed-e3f6-4a4f-989f-5288a30a59ea",
            "7713877b-c121-4e79-8838-3c2b58e8856e" };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definitions { get; private set; }
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (!ConditionalActivationProbe.HasVanguard || save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Punished spell batch requires isolated deployment.");
            Definitions = Originals.Select(id => all.FindCardData(id)!).Select(card => new {
                Id = card.GetID(), Key = card.name, Cost = card.GetCost(),
                Traits = card.GetTraits().Select(CardSourceTraitProbe.Capture).ToArray(), Rule = BattleActionProbe.Definition(card),
                Effects = card.GetEffects().Select(RuleCatalogProbe.Effect).ToArray() }).ToArray();
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!; var anchor = steward.GetSpawnCharacterData();
            Set(steward, "cost", 0); Set(anchor, "attackDamage", 1000); Set(anchor, "health", 500); Set(anchor, "size", 1);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var guard = ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(all))
                .Single(character => character.name == "ShieldGuardT1_BasicNonAttack"); Set(guard, "health", 1000);
            Set(all.GetBalanceData(), "maxHandSize", 40);
            CardData Unit(int suffix, int attack, int health)
            {
                var data = UnityEngine.Object.Instantiate(steward); data.name = "PojuPunishedSpellUnit" + suffix;
                Set(data, "id", "fc231b87-0000-4000-8000-" + suffix.ToString("D12"));
                var unit = UnityEngine.Object.Instantiate(anchor); unit.name = data.name + "Body";
                Set(unit, "attackDamage", attack); Set(unit, "health", health);
                var spawn = new CardEffectData("CardEffectSpawnMonster", unit, Team.Type.Monsters); spawn.Cheat_SetTargetMode(TargetMode.Room);
                Set(data, "effects", new List<CardEffectData> { spawn }); definitions.Add(data); return data;
            }
            CardData Control(int suffix, string type, int value, bool room = false, string? status = null)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalSpareScenario.PainId)); data.name = "PojuPunishedSpellControl" + suffix;
                Set(data, "id", "fc231b87-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", room); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, room ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(room ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", status == null ? value : 0);
                if (status != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = value } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var sturdy = Unit(1, 10, 100); var weak = Unit(2, 0, 4); var remote = Unit(3, 12, 100);
            var battery = Control(10, "CardEffectGainEnergy", 40, true); var heal = Control(11, "CardEffectHeal", 9999);
            var armor = Control(12, "CardEffectAddStatusEffect", 8, status: "armor");
            var zero = Control(13, "CardEffectDamage", 0); var blank = Control(14, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ??
                throw new InvalidOperationException("Missing original spell batch input: " + data.name);
            var original = Originals.Select(id => all.FindCardData(id)!).ToArray();
            var created = new[] { Add(steward), Add(battery), Add(sturdy), Add(weak), Add(remote),
                Add(original[1]), Add(original[0]), Add(original[2]), Add(original[0]), Add(original[3]),
                Add(original[1]), Add(original[6]), Add(original[4]), Add(original[1]), Add(original[5]),
                Add(heal), Add(original[2]), Add(armor), Add(original[3]), Add(original[1]),
                Add(original[4]), Add(original[5]), Add(original[2]), Add(original[0]), Add(original[2]),
                Add(weak), Add(original[2]), Add(zero), Add(original[1]) };
            foreach (var card in created.Skip(1))
            { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = created.Select(FullBattleTrace.Active!.CardId).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-PUNISHED-SPELLS-PREPARED seven unchanged originals; live damage statistics, last targets, heal/lethal/armor, two room damage waves, upgrades and forced attacks.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index > 0 && source.Spawn.Turn < 1) return null;
            int floor = index is 0 or 1 ? 2 : index is 4 or 20 or 21 or 23 or 24 ? 1 : 0;
            int targetCard = index is 6 or 7 or 11 or 12 or 14 or 15 or 16 or 17 or 27 ? 2 :
                index is 20 or 21 or 23 or 24 ? 4 : index == 26 ? 25 : -1;
            int target = targetCard >= 0 ? source.Spawn.Train.Rooms[floor].Units.Single(unit => unit.SpawnerCardId == CardIds[targetCard]).Id :
                index is 5 or 8 or 10 or 13 or 19 or 22 or 28 ? source.Spawn.Train.Rooms[floor].Units.First(unit => unit.Team == CombatTeam.Enemy).Id : 0;
            int position = index is 0 or 2 or 4 ? 0 : index is 3 or 25 ? 1 : -1;
            var frontier = BattleActionModel.EnumerateActions(source);
            var action = frontier.SupportedActions.FirstOrDefault(play => play.CardInstanceId == CardIds[index] &&
                play.RoomIndex == floor && play.TargetUnitId == target && (position < 0 || play.PlayerPosition == position));
            if (action == null) throw new InvalidOperationException("Punished spell input " + index + " unsupported: " +
                string.Join("; ", frontier.UnsupportedAttempts.Select(attempt => attempt.Reason).Distinct().Take(6)));
            return action;
        }
    }
}
