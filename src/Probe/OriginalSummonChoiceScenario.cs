using System;
namespace MonsterTrain2Poju.Probe
{
#if !DEVELOPMENT_PROBE
    internal static class OriginalSummonChoiceScenario { internal static bool Enabled => false; }
#else
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using BepInEx.Logging;
    using HarmonyLib;
    using MonsterTrain2Poju.Model;

    internal static class OriginalSummonChoiceScenario
    {
        internal const string Name = "original-summon-choice";
        internal const string Champion = "c58f8d77-8578-4d97-bcf1-beb4cdbe2f27";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static int[] CardIds { get; private set; } = Array.Empty<int>();
        internal static object? Definitions { get; private set; }
        internal static bool NativeDlcInstalled { get; private set; }
        internal static readonly List<DraftEvidence> Drafts = new List<DraftEvidence>();
        internal sealed class DraftEvidence
        {
            public string[] Cards { get; set; } = Array.Empty<string>();
            public string Choice { get; set; } = "";
        }
        private sealed class Input
        {
            internal CardState? Card;
            internal int Room, Rank = -1, Choice = -1;
            internal string Kind = "card", Target = "";
        }
        private static readonly List<Input> Inputs = new List<Input>();
        private static readonly string[][] Tokens = {
            new[] { "6c3eb0bb-4871-438d-9b5c-fcef1f94bf70", "c847d9b0-5af4-4076-9d99-111b81224ef5", "0127e3c2-b60d-4ac3-acf7-6395d44eb5ef" },
            new[] { "983495d0-11cb-49d7-9f3e-8531c6f18676", "841340c7-76e3-4336-b8a2-41f308a787fb", "491acbc6-c766-4ecf-8b10-6c51b5ae94e5" },
            new[] { "462f5b87-cace-447d-8818-3d4b7bc8eb7f", "c62f38bc-3b99-4f90-8d32-59a5588466e8", "4535073a-f484-4ecd-abb9-b83d6ddfcee1" } };
        private static int step, mainThread;
        private static string? expectedChoice;
        private static bool overrideInstalled;
        private static void Set(object owner, string field, object value) => AccessTools.Field(owner.GetType(), field).SetValue(owner, value);
        private static bool InstalledPrefix(ref bool __result)
        {
            if (!overrideInstalled || Environment.CurrentManagedThreadId != mainThread) return true;
            __result = true; return false;
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            var save = managers.GetSaveManager(); var all = save.GetAllGameData(); var cards = managers.GetCardManager()!;
            if (save.PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Summon choice requires isolated deployment.");
            mainThread = Environment.CurrentManagedThreadId;
            var platform = ShinyShoe.AppManager.PlatformServices;
            var installed = AccessTools.Method(platform.GetType(), "IsDlcInstalled", new[] { typeof(ShinyShoe.DLC) });
            var champion = all.FindCardData(Champion)!;
            // Explicit installed-control input in this isolated worker only. Original
            // assets, pools, filters and the effect/UI implementation stay unchanged.
            NativeDlcInstalled = (bool)installed.Invoke(platform, new object[] { champion.GetRequiredDLC() });
            var harmony = new Harmony("MonsterTrain2Poju.OriginalSummonChoiceScenario");
            harmony.Patch(installed, prefix: new HarmonyMethod(AccessTools.Method(typeof(OriginalSummonChoiceScenario), nameof(InstalledPrefix))));
            harmony.Patch(AccessTools.Method(typeof(ElixirDraftScreen), "SetupForCardChoice"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(OriginalSummonChoiceScenario), nameof(DraftPostfix))));
            overrideInstalled = true;
            var upgradeIds = new[] { "823ab526-9cfe-4383-9e17-a033d49a7fa9", "6b9e1108-af4e-468f-a912-48853b0f0413", "3de67996-0562-41ce-b892-1cedabf0bffc" };
            var treeUpgrades = PunishedChampionDefinitionProbe.Tree(all, champion)!.GetUpgradeTrees().SelectMany(path => path.GetCardUpgrades()).ToArray();
            var upgrades = upgradeIds.Select(id => treeUpgrades.Single(upgrade => upgrade.GetID() == id)).ToArray();
            Definitions = new { Profile = "installed-control", Champion = BattleActionProbe.Definition(champion),
                Upgrades = upgrades.Select(upgrade => { var state = new CardUpgradeState(); state.Setup(upgrade);
                    return new { Id = upgrade.GetID(), Key = upgrade.name, Rule = CardModifierProbe.Upgrade(state),
                        Ability = BattleActionProbe.Definition(upgrade.GetUnitAbilityUpgrade()) }; }).ToArray(),
                Tokens = Tokens.SelectMany(ids => ids).Select(id => new { Id = id, Rule = BattleActionProbe.Definition(all.FindCardData(id)!) }).ToArray() };
            var definitions = (List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all);
            var steward = all.FindCardData(MagneticRelayScenario.StewardId)!;
            Set(steward, "cost", 0); Set(steward.GetSpawnCharacterData(), "attackDamage", 1000);
            Set(steward.GetSpawnCharacterData(), "health", 500); Set(steward.GetSpawnCharacterData(), "size", 1);
            foreach (var card in cards.GetAllCards(new List<CardState>()).Where(card => card.GetCardDataID() == steward.GetID())) card.Setup(steward, save);
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card); cards.GetDrawPile().Clear();
            Set(all.GetBalanceData(), "maxHandSize", 40);
            CardData Control(int suffix, string type, int amount, bool room = false, string? status = null)
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(OriginalSpareScenario.PainId)); data.name = "PojuSummonChoice" + suffix;
                Set(data, "id", "fc231b91-0000-4000-8000-" + suffix.ToString("D12")); Set(data, "cost", 0);
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", new List<CardTriggerEffectData>());
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", room); Set(data, "targetsRoom", true);
                var effect = new CardEffectData(type, null!, room ? Team.Type.None : Team.Type.Monsters);
                effect.Cheat_SetTargetMode(room ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", status == null ? amount : 0);
                if (status != null) Set(effect, "paramStatusEffects", new[] { new StatusEffectStackData { statusId = status, count = amount } });
                Set(data, "effects", new List<CardEffectData> { effect }); definitions.Add(data); return data;
            }
            var tank = UnityEngine.Object.Instantiate(steward); tank.name = "PojuSummonChoiceTank";
            Set(tank, "id", "fc231b91-0000-4000-8000-000000000099");
            var tankUnit = UnityEngine.Object.Instantiate(steward.GetSpawnCharacterData());
            tankUnit.name = "PojuSummonChoiceTankUnit"; Set(tankUnit, "attackDamage", 0);
            var tankSpawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
            tankSpawn.Cheat_SetTargetMode(TargetMode.Room); Set(tankSpawn, "paramCharacterData", tankUnit);
            Set(tank, "effects", new List<CardEffectData> { tankSpawn });
            definitions.Add(tank);
            var battery = Control(1, "CardEffectGainEnergy", 40, true);
            var reset = Control(2, "CardEffectRemoveStatusEffect", 999, status: "cooldown");
            var damage = Control(3, "CardEffectDamage", 5); var kill = Control(4, "CardEffectDamage", 1000);
            var blank = Control(5, "CardEffectNULL", 0, true);
            CardState Add(CardData data) => cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ?? throw new InvalidOperationException("Missing choice input.");
            Inputs.Add(new Input { Card = Add(steward), Room = 2 });
            Inputs.Add(new Input { Card = Add(tank), Room = 0 }); Inputs.Add(new Input { Card = Add(tank), Room = 1 });
            for (int rank = 0; rank < 3; rank++)
            {
                var card = Add(champion); var upgrade = new CardUpgradeState(); upgrade.Setup(upgrades[rank]); card.ApplyPermanentUpgrade(upgrade, save);
                Inputs.Add(new Input { Card = card, Room = rank });
            }
            Inputs.Add(new Input { Card = Add(battery), Room = 2 });
            for (int rank = 0; rank < 3; rank++)
            {
                int other = (rank + 1) % 3;
                void Skill(int room, int choice, string kind = "flight", string target = "") => Inputs.Add(new Input { Room = room, Rank = rank, Choice = choice, Kind = kind, Target = target });
                void Card(CardData data, int room, string target) => Inputs.Add(new Input { Card = Add(data), Room = room, Rank = rank, Target = target });
                Skill(rank, 0); Card(reset, rank, "champion"); Skill(rank, -1); Card(reset, rank, "champion");
                Skill(other, 1); Card(damage, other, "champion"); Card(kill, rank, "blinding"); Card(reset, other, "champion");
                Skill(rank, 2); Skill(rank, -1, "shatter", "tank"); Card(kill, other, "looming");
            }
            for (int rank = 0; rank < 3; rank++)
            {
                Inputs.Add(new Input { Card = Add(reset), Room = rank, Rank = rank, Target = "champion" });
                Inputs.Add(new Input { Room = rank, Rank = rank, Choice = 2, Kind = "flight" });
                Inputs.Add(new Input { Room = rank, Rank = rank, Kind = "shatter", Target = "self" });
            }
            foreach (var input in Inputs.Skip(6).Where(input => input.Card != null))
            { var freeze = new CardTraitData(); freeze.Setup("CardTraitFreeze"); input.Card!.AddTemporaryTrait(freeze, managers.GetCoreManagers()); }
            for (int i = 0; i < 12; i++) cards.AddNewCard(blank, CardPile.DeckPile, false, false, animate: false);
            CardIds = Inputs.Select(input => input.Card == null ? 0 : FullBattleTrace.Active!.CardId(input.Card)).ToArray();
            AccessTools.Method(typeof(CardStatistics), "UpdateDeckStats").Invoke(managers.GetCardStatistics(), null);
            Prepared = true; Set(managers.GetCombatManager(), "combatStateChanged", true);
            log.LogInfo("ORIGINAL-SUMMON-CHOICE-PREPARED installed-control; unchanged champion/upgrades/three skills/nine fragments; native-platform-installed=" + NativeDlcInstalled);
        }
        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (step >= Inputs.Count || step >= 6 && source.Spawn.Turn < 1) return null;
            var input = Inputs[step]; var units = source.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
            CombatUnit ChampionUnit() => units.Single(unit => unit.SpawnerCardId == CardIds[3 + input.Rank]);
            int target = input.Target == "champion" ? ChampionUnit().Id : input.Target == "tank"
                ? units.Single(unit => unit.SpawnerCardId == CardIds[input.Room == 2 ? 0 : input.Room + 1]).Id
                : input.Target.Length == 0 || input.Target == "self" ? 0 : units.Single(unit => unit.Health > 0 && unit.AssetKey ==
                    source.PlayRules!.Cards.Single(rule => rule.DataId == Tokens[input.Rank][input.Target == "blinding" ? 0 : 1]).SpawnUnit!.AssetKey).Id;
            PlayCardAction action;
            if (input.Card != null) action = new PlayCardAction(CardIds[step], input.Room, playerPosition: step < 6 ? 0 : -1, targetUnitId: target);
            else
            {
                var actor = input.Kind == "flight" ? ChampionUnit() : units.Single(unit => unit.Health > 0 &&
                    unit.AssetKey == source.PlayRules!.Cards.Single(rule => rule.DataId == Tokens[input.Rank][2]).SpawnUnit!.AssetKey);
                var cached = AbilityCardModel.Get(source.Spawn.Train.Context!, actor.Ability!.CardCreation!);
                if (!cached.Supported) throw new InvalidOperationException(cached.UnsupportedReason);
                action = new PlayCardAction(cached.Card!.InstanceId, input.Room, targetUnitId: input.Target == "self" ? actor.Id : target, activatorUnitId: actor.Id,
                    targetSpawnPointIndex: input.Kind == "flight" ? 0 : -1,
                    summonSelections: input.Choice < 0 ? null : new[] { new SummonChoiceSelection(1, Tokens[input.Rank][input.Choice]) });
            }
            var predicted = BattleActionModel.PlayCard(source, action);
            if (!predicted.Supported) throw new InvalidOperationException("Summon input " + step + " (" + input.Kind + "/" + input.Target + ") rejected: " + predicted.Reason);
            step++; return action;
        }
        internal static void Expect(PlayCardAction action) => expectedChoice = action.SummonSelections.SingleOrDefault()?.CardDataId;
        private static void DraftPostfix(ElixirDraftScreen __instance) => __instance.StartCoroutine(Select(__instance));
        private static IEnumerator Select(ElixirDraftScreen screen)
        {
            yield return null;
            var offers = ((IList)AccessTools.Field(typeof(ElixirDraftScreen), "draftItems").GetValue(screen)).Cast<CardUI>().ToArray();
            string choice = expectedChoice ?? throw new InvalidOperationException("Unexpected summon-choice dialog.");
            if (offers.Length != 3) throw new InvalidOperationException("Expected three original summon offers.");
            Drafts.Add(new DraftEvidence { Cards = offers.Select(card => card.GetCardState().GetCardDataID()).ToArray(), Choice = choice });
            AccessTools.Method(typeof(ElixirDraftScreen), "ApplyDraft").Invoke(screen, new object[] { offers.Single(card => card.GetCardState().GetCardDataID() == choice) });
            expectedChoice = null;
        }
    }
#endif
}
