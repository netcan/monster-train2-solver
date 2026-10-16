using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCardDrawOnHealScenario
    {
        internal const string Name = "original-card-draw-on-heal";
        internal const string OwnerId = "9a38370b-150c-45bd-9e3e-93a688a04850";
        internal const string Wound = "fc119000-0000-4000-8000-000000000851";
        internal const string Heal = "fc119000-0000-4000-8000-000000000852";
        internal const string EnemyHeal = "fc119000-0000-4000-8000-000000000853";
        internal const string EnemyWound = "fc119000-0000-4000-8000-000000000854";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        private static readonly List<CardState> TestCards = new List<CardState>();
        private static readonly HashSet<int> ScenarioActionIds = new HashSet<int>();

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("CardDrawOnHeal must be acquired before battle in an isolated save.");
            AllGameData data = save.GetAllGameData();
            RelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != "CardDrawOnHeal" || !relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != RelicCardDrawOnHealModel.Effect || effect.GetParamInt() != 1 ||
                effect.GetParamSourceTeam().ToString() != "Monsters" || effect.GetEffectConditions().Count != 1 ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("The unchanged CardDrawOnHeal owner or isolated acquisition state changed.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-CARD-DRAW-ON-HEAL-ACQUIRED unchanged owner; one card per allied heal.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Prepared || !Acquired || save.PreviewMode || managers.GetCombatManager()!.GetTurnCount() != 0 ||
                !save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("CardDrawOnHeal requires its isolated deployment state.");

            AllGameData data = save.GetAllGameData()!;
            CardManager cards = managers.GetCardManager()!;
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardData carrier = owned.Select(card => data.FindCardData(card.GetCardDataID()))
                .FirstOrDefault(card => card?.GetSpawnCharacterData() != null)
                ?? throw new InvalidOperationException("A native unit carrier is required for the heal callback.");
            CharacterData character = carrier.GetSpawnCharacterData()!;
            Set(character, "health", 80);
            Set(character, "attackDamage", 20);
            Set(character, "size", 1);
            Set(character, "equipmentLimit", 3);
            Set(character, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(character, "triggers", new List<CharacterTriggerData>());
            Set(character, "unitAbility", null);
            Set(carrier, "cost", 0);

            CardData spell = data.GetAllCardData().First(card => card.GetCardType() == CardType.Spell &&
                card.GetEffects().Count == 1 && card.GetEffects()[0].GetEffectStateName() == "CardEffectDamage");
            CardData MakeSpell(string name, string id, string effectType, Team.Type team, int amount)
            {
                var result = UnityEngine.Object.Instantiate(spell);
                Configure(result, name, id);
                Set(result, "effects", new List<CardEffectData> { Effect(effectType, team, amount) });
                return result;
            }
            CardData wound = MakeSpell("PojuCardDrawOnHealWound", Wound, "CardEffectDamage", Team.Type.Monsters, 10);
            CardData heal = MakeSpell("PojuCardDrawOnHealHeal", Heal, "CardEffectHeal", Team.Type.Monsters, 1);
            CardData enemyHeal = MakeSpell("PojuCardDrawOnHealEnemyHeal", EnemyHeal, "CardEffectHeal", Team.Type.Heroes, 1);
            CardData enemyWound = MakeSpell("PojuCardDrawOnHealEnemyWound", EnemyWound, "CardEffectDamage", Team.Type.Heroes, 1);
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)!).AddRange([wound, heal, enemyHeal, enemyWound]);

            foreach (CardState card in owned) card.Setup(data.FindCardData(card.GetCardDataID())!, save);
            foreach (CardState card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            TestCards.Clear();
            ScenarioActionIds.Clear();
            foreach (CardData definition in new[] { carrier, wound, wound, wound, heal, heal, heal, enemyWound, enemyHeal })
            {
                CardState? added = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
                if (added == null)
                    throw new InvalidOperationException("CardDrawOnHeal carrier creation failed.");
                if (definition.GetID() == Wound || definition.GetID() == Heal ||
                    definition.GetID() == EnemyWound || definition.GetID() == EnemyHeal)
                    TestCards.Add(added);
            }

            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-CARD-DRAW-ON-HEAL-PREPARED two same-turn allied heals, a next-turn allied heal, and an enemy-heal control.");
        }

        internal static PlayCardAction? Choose(BattleTurnState state)
        {
            CombatContext context = state.Spawn.Train.Context!;
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(state);
            string? Data(int id) => context.FindCard(id)?.DataId;
            int Played(string dataId, string duration) => context.Statistics?.Values.Where(value => value.Type == "TimesPlayed" &&
                value.Duration == duration && Data(value.CardId) == dataId).Sum(value => value.Value) ?? 0;

            var room = state.Spawn.Train.Rooms[0];
            CombatUnit? carrier = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == 80 && unit.Health > 0);
            if (carrier == null)
            {
                return plays.FirstOrDefault(play => play.RoomIndex == 0 &&
                    play.PlayerPosition == room.Units.Count(unit => unit.Team == CombatTeam.Player) &&
                    state.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == Data(play.CardInstanceId))?.SpawnUnit?.MaxHealth == 80);
            }

            if (state.Spawn.Turn == 0)
                return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card =>
                    Data(card.InstanceId) != Wound && Data(card.InstanceId) != Heal &&
                    Data(card.InstanceId) != EnemyWound && Data(card.InstanceId) != EnemyHeal);

            int healsThisTurn = Played(Heal, "ThisTurn");
            int woundsThisTurn = Played(Wound, "ThisTurn");
            int healTargetThisTurn = Played(Heal, "ThisBattle") < 2 ? 2 : 1;
            if (healsThisTurn < healTargetThisTurn)
            {
                string wanted = healsThisTurn == 0 && woundsThisTurn == 0 ? Wound :
                    carrier.Health < carrier.MaxHealth || woundsThisTurn > healsThisTurn ? Heal : Wound;
                if (DrawTestCard(wanted))
                {
                    state = FullBattleTrace.Active!.CaptureDecision();
                    context = state.Spawn.Train.Context!;
                    plays = BattleActionModel.EnumerateSupportedPlays(state);
                    room = state.Spawn.Train.Rooms[0];
                    carrier = room.Units.First(unit => unit.Team == CombatTeam.Player && unit.MaxHealth == 80 && unit.Health > 0);
                }
                PlayCardAction? action = plays.FirstOrDefault(play => Data(play.CardInstanceId) == wanted &&
                    play.TargetUnitId == carrier.Id && play.RoomIndex == room.RoomIndex);
                if (action != null) return MarkScenarioAction(action);
            }

            if (Played(EnemyHeal, "ThisBattle") == 0)
            {
                var damagedEnemy = state.Spawn.Train.Rooms.SelectMany(candidateRoom => candidateRoom.Units
                    .Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0 && unit.Health < unit.MaxHealth)
                    .Select(unit => new { Room = candidateRoom, Unit = unit })).FirstOrDefault();
                if (damagedEnemy != null && DrawTestCard(EnemyHeal))
                {
                    state = FullBattleTrace.Active!.CaptureDecision();
                    context = state.Spawn.Train.Context!;
                    plays = BattleActionModel.EnumerateSupportedPlays(state);
                    room = state.Spawn.Train.Rooms[0];
                }
                PlayCardAction? control = damagedEnemy == null ? null : plays.FirstOrDefault(play =>
                    Data(play.CardInstanceId) == EnemyHeal && play.TargetUnitId == damagedEnemy.Unit.Id &&
                    play.RoomIndex == damagedEnemy.Room.RoomIndex);
                if (control != null) return MarkScenarioAction(control);
                if (Played(EnemyWound, "ThisBattle") == 0)
                {
                    var enemy = state.Spawn.Train.Rooms.SelectMany(candidateRoom => candidateRoom.Units
                        .Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0)
                        .Select(unit => new { Room = candidateRoom, Unit = unit })).FirstOrDefault();
                    if (enemy != null && DrawTestCard(EnemyWound))
                    {
                        state = FullBattleTrace.Active!.CaptureDecision();
                        context = state.Spawn.Train.Context!;
                        plays = BattleActionModel.EnumerateSupportedPlays(state);
                    }
                    PlayCardAction? wound = enemy == null ? null : plays.FirstOrDefault(play =>
                        Data(play.CardInstanceId) == EnemyWound && play.TargetUnitId == enemy.Unit.Id &&
                        play.RoomIndex == enemy.Room.RoomIndex);
                    if (wound != null) return MarkScenarioAction(wound);
                }
            }

            return BattleActionModel.ChooseUnitSpellAndJunkPlay(state, card =>
                Data(card.InstanceId) != Wound && Data(card.InstanceId) != Heal &&
                Data(card.InstanceId) != EnemyHeal && Data(card.InstanceId) != EnemyWound);
        }

        private static bool DrawTestCard(string dataId)
        {
            CardManager cards = AllGameManagers.Instance!.GetCardManager()!;
            if (!cards.GetBelowHandSize() || cards.GetHand().Any(card => card.GetCardDataID() == dataId)) return false;
            CardState? card = TestCards.FirstOrDefault(item => item.GetCardDataID() == dataId && !cards.GetHand().Contains(item));
            return card != null && cards.DrawSpecificCard(card, useEffectDelay: false);
        }

        internal static bool IsScenarioAction(PlayCardAction action) => Enabled && ScenarioActionIds.Contains(action.CardInstanceId);

        private static PlayCardAction MarkScenarioAction(PlayCardAction action)
        {
            ScenarioActionIds.Add(action.CardInstanceId);
            return action;
        }

        private static void Configure(CardData card, string key, string id)
        {
            card.name = key; Set(card, "id", id); Set(card, "cost", 0);
            Set(card, "traits", new List<CardTraitData>()); Set(card, "triggers", new List<CardTriggerEffectData>());
            Set(card, "effectTriggers", new List<CharacterTriggerData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }

        private static CardEffectData Effect(string type, Team.Type team, int amount)
        {
            var effect = new CardEffectData(type, null!, team);
            effect.Cheat_SetTargetMode(TargetMode.DropTargetCharacter);
            Set(effect, "paramInt", amount);
            return effect;
        }

        private static void Set(object target, string field, object? value) => AccessTools.Field(target.GetType(), field).SetValue(target, value);
    }
}
