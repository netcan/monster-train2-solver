using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamageByUnitsKilledScenario
    {
        internal const string Name = "damage-by-units-killed";
        internal const string HigherRanksName = "damage-by-units-killed-higher-ranks";
        internal const string ImpCardId = "d76be2d0-f851-4d43-bdc0-7c7a7779724f";
        internal const string SpellCardId = "d9c0c510-ccb5-4a89-9f3b-99799e765540";
        private const string RankTwoId = "fb0d1638-347d-4b4a-98c9-96bff42a968e";
        private const string RankThreeId = "c807999e-f9b9-423e-9af1-d52666127082";
        private const string ImpSubtype = "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c";
        private static readonly string[] HigherRankCards = { RankTwoId, RankThreeId };
        private static bool HigherRanks => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == HigherRanksName;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is Name or HigherRanksName;
        private static ManualLogSource? logger;
        private static bool candidateLogged;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            logger = log;
            candidateLogged = false;
            CardManager cards = managers.GetCardManager()!;
            string[] spellIds = HigherRanks ? HigherRankCards : new[] { SpellCardId };
            foreach ((string id, CardPile pile) in spellIds.Select(_ => (ImpCardId, CardPile.HandPile))
                .Concat(spellIds.Reverse().Select(id => (id, CardPile.DeckPileTop))))
            {
                CardData definition = managers.GetSaveManager().GetAllGameData().FindCardData(id) ??
                    throw new InvalidOperationException("Damage-by-killed-units fixture requires original Imp and Shardtail cards.");
                if (pile == CardPile.HandPile && !cards.GetBelowHandSize())
                    throw new InvalidOperationException("Damage-by-killed-units fixture requires one free hand slot.");
                CardState added = cards.AddNewCard(definition, pile, false, false, animate: false);
                if (added == null || !(pile == CardPile.HandPile ? cards.GetHand() : cards.GetDrawPile()).Contains(added))
                    throw new InvalidOperationException("Damage-by-killed-units fixture could not place original card in its pile: " + id);
            }
            List<CardState> draw = cards.GetDrawPile();
            foreach (CardState card in draw.Where(card => !spellIds.Contains(card.GetCardDataID())).ToArray())
            {
                draw.Remove(card);
                cards.GetExhaustedPile().Add(card);
            }
            log.LogInfo("DAMAGE-BY-UNITS-KILLED-PREPARED native StarterImp and original spell IDs " + string.Join(",", spellIds));
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (HigherRanks) return ChooseHigherRanks(source);
            CombatContext context = source.Spawn.Train.Context!;
            RoomCombatState floor = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
            CardToken? imp = context.Cards.Hand.FirstOrDefault(card => card.DataId == ImpCardId);
            if (imp != null)
            {
                int friendlyCount = floor.Units.Count(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
                if (floor.Deployment && OriginalAllSubtypesModel.Active(context, deployment: true) && friendlyCount < 2)
                {
                    bool needChampion = floor.Units.All(unit => unit.Modifiers?.IsChampion != true);
                    PlayCardAction? control = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                        action.RoomIndex == 0 && action.CardInstanceId != imp.InstanceId && context.FindCard(action.CardInstanceId) is { } candidate &&
                        source.PlayRules!.Cards.Any(rule => rule.DataId == candidate.DataId && rule.SpawnUnit is { } unit &&
                            (unit.Modifiers?.IsChampion == true) == needChampion));
                    if (control != null) return control;
                }
                if (friendlyCount == 0)
                {
                    PlayCardAction? defender = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                        action.RoomIndex == 0 && source.Spawn.Train.Context.Cards.Hand.Any(card =>
                            card.InstanceId == action.CardInstanceId && card.InstanceId != imp.InstanceId &&
                            source.PlayRules?.Cards.Any(rule => rule.DataId == card.DataId && rule.CardType == "Monster") == true));
                    if (defender != null) return defender;
                }
                else
                {
                    var summon = new PlayCardAction(imp.InstanceId, 0, playerPosition: friendlyCount);
                    BattleActionResult result = BattleActionModel.PlayCard(source, summon);
                    if (result.Supported) return summon;
                    LogCandidate(result, "summon");
                }
            }
            bool hasImp = floor.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(ImpSubtype));
            bool hasEnemy = floor.Units.Any(unit => unit.Team == CombatTeam.Enemy);
            CardToken? spell = context.Cards.Hand.FirstOrDefault(card => card.DataId == SpellCardId);
            if (spell != null && hasImp && hasEnemy)
            {
                var action = new PlayCardAction(spell.InstanceId, 0);
                BattleActionResult result = BattleActionModel.PlayCard(source, action);
                if (result.Supported) return action;
                LogCandidate(result, "spell");
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }

        private static PlayCardAction? ChooseHigherRanks(BattleTurnState source)
        {
            CombatContext context = source.Spawn.Train.Context!;
            CardToken? imp = context.Cards.Hand.FirstOrDefault(card => card.DataId == ImpCardId);
            for (int index = 0; index < HigherRankCards.Length; index++)
            {
                string spellId = HigherRankCards[index];
                RoomCombatState floor = source.Spawn.Train.Rooms.Single(room => room.RoomIndex == index);
                CardToken? spell = context.Cards.Hand.FirstOrDefault(card => card.DataId == spellId);
                bool hasImp = floor.Units.Any(unit => unit.Team == CombatTeam.Player && unit.Subtypes.Contains(ImpSubtype));
                if (!hasImp && imp != null)
                {
                    int friendlyCount = floor.Units.Count(unit => unit.Team == CombatTeam.Player);
                    if (friendlyCount == 0)
                    {
                        PlayCardAction? defender = BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                            action.RoomIndex == index && action.CardInstanceId != imp.InstanceId &&
                            context.FindCard(action.CardInstanceId) is { } card &&
                            source.PlayRules!.Cards.Any(rule => rule.DataId == card.DataId && rule.SpawnUnit != null));
                        if (defender != null) return defender;
                    }
                    var summon = new PlayCardAction(imp.InstanceId, index, playerPosition: friendlyCount);
                    BattleActionResult summonResult = BattleActionModel.PlayCard(source, summon);
                    if (summonResult.Supported) return summon;
                    LogCandidate(summonResult, "summon");
                }
                if (spell == null || !hasImp || !floor.Units.Any(unit => unit.Team == CombatTeam.Enemy)) continue;
                var cast = new PlayCardAction(spell.InstanceId, index);
                BattleActionResult castResult = BattleActionModel.PlayCard(source, cast);
                if (castResult.Supported) return cast;
                LogCandidate(castResult, "spell");
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card =>
                card.DataId != ImpCardId && !HigherRankCards.Contains(card.DataId));
        }

        private static void LogCandidate(BattleActionResult result, string phase)
        {
            if (candidateLogged) return;
            logger!.LogInfo("DAMAGE-BY-UNITS-KILLED-CANDIDATE phase=" + phase + " supported=" + result.Supported +
                " rejection=" + result.Rejection + " reason=" + result.Reason);
            candidateLogged = true;
        }
    }
}
