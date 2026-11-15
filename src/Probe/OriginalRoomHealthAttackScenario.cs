using System;
namespace MonsterTrain2Poju.Probe
{
#if !DEVELOPMENT_PROBE
    internal static class OriginalRoomHealthAttackScenario { internal static bool Enabled => false; }
#else
    using System.Collections.Generic;
    using System.Linq;
    using BepInEx.Logging;
    using HarmonyLib;
    using MonsterTrain2Poju.Model;

    internal static class OriginalRoomHealthAttackScenario
    {
        internal const string Name = "original-room-health-attack";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definitions { get; private set; }
        internal static object? CurrentDefinitions => all == null ? null : CaptureDefinitions();
        internal sealed class Input
        {
            public string Tag { get; }
            public int Room { get; }
            public int TargetCardIndex { get; }
            internal readonly CardState Card;
            internal Input(string tag, CardState card, int room, int target = -1)
            { Tag = tag; Card = card; Room = room; TargetCardIndex = target; }
        }
        internal static readonly List<Input> Inputs = new List<Input>();
        private static AllGameData? all;
        private static CardData champion = null!;
        private static CardUpgradeData[] upgrades = Array.Empty<CardUpgradeData>();
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        private static object CaptureDefinitions() => new {
            Champion = BattleActionProbe.Definition(champion),
            Upgrades = upgrades.Select(upgrade => { var state = new CardUpgradeState(); state.Setup(upgrade);
                return new { Id = upgrade.GetID(), Key = upgrade.name, Rule = CardModifierProbe.Upgrade(state),
                    Modifiers = upgrade.GetRoomModifierUpgrades().Select(RuleCatalogProbe.RoomModifier).ToArray() }; }).ToArray()
        };
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Room-health aura requires isolated deployment.");
            upgrades = all.GetAllCardUpgradeData().Where(upgrade => upgrade.GetRoomModifierUpgrades().Any(modifier =>
                modifier.GetRoomStateModifierClassName() == RoomHealthDamageModel.FriendlyOwn))
                .OrderBy(upgrade => upgrade.GetRoomModifierUpgrades().Single().GetParamInt()).ToArray();
            if (upgrades.Length != 3 || !upgrades.Select(upgrade => upgrade.GetRoomModifierUpgrades().Single().GetParamInt()).SequenceEqual(new[] { 1, 2, 3 }))
                throw new InvalidOperationException("Changed original friendly missing-health upgrade family.");
            champion = all.GetAllCardData().Single(card => card.GetSpawnCharacterData() != null &&
                PunishedChampionDefinitionProbe.Tree(all, card)?.GetUpgradeTrees().SelectMany(path => path.GetCardUpgrades())
                    .Any(upgrade => upgrade.GetID() == upgrades[0].GetID()) == true);
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            Set(steward, "cost", 0); Set(steward.GetSpawnCharacterData(), "attackDamage", 1000);
            Set(steward.GetSpawnCharacterData(), "health", 500); Set(steward.GetSpawnCharacterData(), "size", 1);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            Set(all.GetBalanceData(), "maxHandSize", 48);
            var host = UnityEngine.Object.Instantiate(steward); host.name = "PojuRoomHealthHost";
            Set(host, "id", "fc231b92-0000-4000-8000-000000000099");
            var body = UnityEngine.Object.Instantiate(steward.GetSpawnCharacterData()); body.name = host.name + "Unit";
            Set(body, "attackDamage", 0); Set(body, "health", 500);
            var summon = new CardEffectData("CardEffectSpawnMonster", body, Team.Type.Monsters); summon.Cheat_SetTargetMode(TargetMode.Room);
            Set(host, "effects", new List<CardEffectData> { summon }); definitions.Add(host);
            CardData Control(int suffix, string type, int value, bool room = false, string? status = null)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalSpareScenario.PainId)); data.name = "PojuRoomHealthControl" + suffix;
                Set(data, "id", "fc231b92-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", room); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, room ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(room ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", status == null ? value : 0);
                if (status != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = value } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var battery = Control(1, "CardEffectGainEnergy", 40, true);
            var damage = Control(2, "CardEffectDamage", 5); var heal = Control(3, "CardEffectHeal", 3);
            var fullHeal = Control(4, "CardEffectHeal", 9999); var ascend = Control(5, "CardEffectBump", 1);
            var descend = Control(6, "CardEffectBump", -1); var kill = Control(7, "CardEffectDamage", 1000);
            var silence = Control(8, "CardEffectAddStatusEffect", 1, status: "silenced");
            var unmute = Control(9, "CardEffectRemoveStatusEffect", 99, status: "silenced");
            var blank = Control(10, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false)
                ?? throw new InvalidOperationException("Missing room-health input.");
            void Input(string tag, CardData data, int room, int target = -1) => Inputs.Add(new Input(tag, Add(data), room, target));
            Input("anchor", steward, 2);
            for (int floor = 0; floor < 3; floor++) Input("host-" + floor, host, floor);
            for (int rank = 0; rank < 3; rank++)
            {
                var card = Add(champion); var upgrade = new CardUpgradeState(); upgrade.Setup(upgrades[rank]); card.ApplyPermanentUpgrade(upgrade, save);
                Inputs.Add(new Input("rank-" + (rank + 1), card, rank));
            }
            Input("energy", battery, 2);
            for (int floor = 0; floor < 3; floor++)
            { Input("hurt-host-" + floor, damage, floor, 1 + floor); Input("hurt-source-" + floor, damage, floor, 4 + floor); }
            Input("heal-host-0", heal, 0, 1); Input("reset-source-0", fullHeal, 0, 4);
            Input("stack-ranks-1-2", ascend, 0, 4); Input("move-recipient-in", ascend, 0, 1);
            Input("stack-all-ranks", descend, 2, 6); Input("silence-source", silence, 1, 5); Input("unsilence-source", unmute, 1, 5);
            Input("reset-host-1", fullHeal, 1, 2); Input("hurt-stacked-host", damage, 1, 2);
            Input("unstack-rank-1", descend, 1, 4); Input("move-recipient-out", descend, 1, 1);
            Input("kill-rank-1", kill, 0, 4); Input("hurt-no-source-0", damage, 0, 1);
            Input("move-rank-2", ascend, 1, 5); Input("kill-rank-3", kill, 1, 6); Input("hurt-no-source-1", damage, 1, 2);
            Input("reset-host-2", fullHeal, 2, 3); Input("hurt-remaining-aura", damage, 2, 3); Input("hurt-remaining-source", damage, 2, 5);
            Input("late-host", host, 2); Input("hurt-late-host", damage, 2, Inputs.Count - 1);
            // These zero-attack controls have finished their assertions. Remove
            // them before the final boss to avoid irrelevant zero-damage rounds.
            Input("remove-control-0", kill, 0, 1); Input("remove-control-1", kill, 1, 2);
            foreach (var input in Inputs.Skip(7))
            { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); input.Card.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = Inputs.Select(input => FullBattleTrace.Active!.CardId(input.Card)).ToArray();
            // Installed upgrades enable actor-world lifecycle fields. Capture
            // both definition snapshots with that same observation layout.
            Definitions = CaptureDefinitions();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-ROOM-HEALTH-ATTACK-PREPARED three unchanged original champion upgrades; native shared aura lifecycle.");
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            int index = Array.FindIndex(CardIds, id => source.Spawn.Train.Context!.Statistics!.Value(id, "TimesPlayed", "ThisBattle") == 0);
            if (index < 0 || index >= 7 && source.Spawn.Turn < 1) return null;
            var input = Inputs[index];
            int target = input.TargetCardIndex < 0 ? 0 : source.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Single(unit => unit.SpawnerCardId == CardIds[input.TargetCardIndex]).Id;
            var action = new PlayCardAction(CardIds[index], input.Room, playerPosition: input.TargetCardIndex < 0 && index != 7 ? 0 : -1, targetUnitId: target);
            var result = BattleActionModel.PlayCard(source, action);
            if (!result.Supported) throw new InvalidOperationException("Room-health input " + input.Tag + " rejected: " + result.Reason);
            return action;
        }
    }
#endif
}
