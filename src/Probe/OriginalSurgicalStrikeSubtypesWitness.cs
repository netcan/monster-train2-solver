#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSurgicalStrikeSubtypesWitness
    {
        internal const string FileName = "surgical-strike-subtypes.mt2f";
        private const string SourceId = OriginalKillCopyScenario.OriginalId;
        private const string DregId = "63a48bad-07da-4687-bfef-2f7c6653af5e";
        private const string FungusKey = "SubtypesData_FunGuy";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly List<CardState> deployed = new List<CardState>();
        private static readonly List<object> rows = new List<object>();
        private static PendingPlay? pending;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_SURGICAL_STRIKE_SUBTYPES") == "1";

        internal static bool Tick(FullBattleTrace trace, AllGameManagers managers, Action advanceTurn)
        {
            SaveManager save = managers.GetSaveManager();
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module || save.PreviewMode ||
                save.InTestScenario || save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Expected a stock first battle on the pinned original module.");
            CardManager cards = managers.GetCardManager();
            RoomManager rooms = managers.GetRoomManager();
            RoomState room = rooms.GetRoom(0);
            if (rooms.GetSelectedRoom() != 0) throw new InvalidOperationException("Expected the original default bottom floor.");
            int turn = managers.GetCombatManager().GetTurnCount();
            if (turn == 0)
            {
                if (deployed.Count == 2)
                {
                    save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                    advanceTurn();
                    return false;
                }
                CardData definition = save.GetAllGameData().FindCardData(DregId)!;
                CardState dreg = cards.AddNewCard(definition, CardPile.HandPile, false, false, animate: false);
                Play(dreg, room.GetFirstEmptyMonsterPoint()!, cards);
                deployed.Add(dreg);
                return false;
            }
            if (turn != 1 || deployed.Count != 2)
                throw new InvalidOperationException("Surgical Strike controls require the settled first ordinary decision.");

            if (pending is { } completed)
            {
                BattleTurnState actual = trace.CaptureDecision();
                var children = actual.Spawn.Train.Context!.Cards.Hand.Where(card => card.DataId == SourceId &&
                    card.InstanceId >= completed.Before.Spawn.Train.Context!.NextCardId).Select(card => card.InstanceId).ToArray();
                int expectedCopies = completed.Kind == "ActiveFriendly" ? 0 : 1;
                if (completed.Calls.Count != 1 || completed.Calls[0] != (1, expectedCopies) || children.Length != expectedCopies ||
                    completed.Before.Energy - actual.Energy != 1 || actual.Spawn.Train.Rooms.SelectMany(floor => floor.Units)
                        .Any(unit => unit.Id == completed.TargetId))
                    throw new InvalidOperationException("Native Surgical Strike observation differs at " + completed.Kind +
                        ": trigger=" + string.Join(",", completed.Calls) + " copies=" + children.Length + " paid=" + (completed.Before.Energy - actual.Energy));
                rows.Add(new { completed.Kind, completed.Before, Actual = actual, completed.CardId, completed.TargetId,
                    completed.RawSubtypes, completed.EffectiveFungus, Copies = children,
                    Calls = completed.Calls.Select(call => new { Before = call.Before, After = call.After }).ToArray() });
                pending = null;
                if (rows.Count == 3)
                {
                    using var archive = NativeFixtureCapture.Capture(new { Schema = 1, Boundary = "OriginalSurgicalStrikeAllSubtypes",
                        GameVersion = Application.version, GameModuleMvid = Module, OwnerId = OriginalAllSubtypesModel.OwnerId, SourceId, Rows = rows });
                    using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName));
                    archive.Write(stream);
                    return true;
                }
            }

            if (rows.Count == 1) OriginalAllSubtypesWitness.Acquire(managers);
            CardData source = save.GetAllGameData().FindCardData(SourceId)!;
            if (source.GetCost() != 1 || source.GetEffects() is not [var effect] || effect.GetEffectStateName() != "CardEffectDamage" ||
                effect.GetTargetMode() != TargetMode.DropTargetCharacter || !effect.GetTargetTeamType().HasFlag(Team.Type.Monsters | Team.Type.Heroes) ||
                effect.GetParamInt() != 4 || source.GetTraits().SingleOrDefault(trait => trait.GetTraitStateName() == CardKillTriggerModel.SuppressTrait)
                    is not CardTraitData suppression || !suppression.GetParamBool() || suppression.GetParamTeamType() != Team.Type.Monsters ||
                suppression.GetParamSubtype().Key != FungusKey)
                throw new InvalidOperationException("Original Surgical Strike targeting or suppression differs.");
            var candidates = new List<CharacterState>();
            room.AddCharactersToList(candidates, rows.Count < 2 ? Team.Type.Monsters : Team.Type.Heroes);
            CharacterState target = rows.Count < 2 ? candidates.Single(actor => ReferenceEquals(actor.GetSpawnerCard(), deployed[rows.Count])) :
                candidates.First(actor => actor.IsAlive && !actor.IsAnyBoss() && actor.GetHP() <= 4);
            string[] raw = target.GetSubtypes().Select(subtype => subtype.Key).ToArray();
            bool fungus = target.GetCharacterManager().DoesCharacterPassSubtypeCheck(target, SubtypeManager.GetSubtypeData(FungusKey));
            if (target.GetHP() is < 1 or > 4 || raw.Contains(FungusKey) || fungus != (rows.Count == 1))
                throw new InvalidOperationException("Unexpected original non-FunGuy lethal target.");
            CardState card = cards.AddNewCard(source, CardPile.HandPile, false, false, animate: false);
            BattleTurnState before = trace.CaptureDecision();
            string kind = rows.Count == 0 ? "BaselineFriendly" : rows.Count == 1 ? "ActiveFriendly" : "ActiveFoe";
            pending = new PendingPlay(kind, before, card, trace.CardId(card), target, trace.UnitId(target), raw, fungus);
            Play(card, target.GetSpawnPoint(), cards);
            return false;
        }

        private static void Play(CardState card, SpawnPoint point, CardManager cards)
        {
            if (!cards.CanPlayHandCard(card, 0, point, null, null, out var error) ||
                !cards.PlayCard(cards.GetHand().IndexOf(card), point, ref error))
                throw new InvalidOperationException("Original native subtype test input rejected: " + error);
        }

        private sealed class PendingPlay(string kind, BattleTurnState before, CardState card, int cardId, CharacterState target,
            int targetId, string[] rawSubtypes, bool effectiveFungus)
        {
            internal readonly string Kind = kind;
            internal readonly BattleTurnState Before = before;
            internal readonly CardState Card = card;
            internal readonly int CardId = cardId;
            internal readonly CharacterState Target = target;
            internal readonly int TargetId = targetId;
            internal readonly string[] RawSubtypes = rawSubtypes;
            internal readonly bool EffectiveFungus = effectiveFungus;
            internal readonly List<(int Before, int After)> Calls = new List<(int, int)>();
        }

        [HarmonyPatch(typeof(CardTraitModifyCardTriggerSlayFireCount), nameof(CardTraitModifyCardTriggerSlayFireCount.GetModifiedCardTriggerFireCount))]
        private static class OnKillPatch
        {
            private static void Postfix(CardTriggerType triggerType, int currentFireCount, CardState? playedCard, CharacterState? dyingCharacter, int __result)
            {
                if (triggerType == CardTriggerType.OnKill && pending is { } play &&
                    ReferenceEquals(playedCard, play.Card) && ReferenceEquals(dyingCharacter, play.Target) &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    play.Calls.Add((currentFireCount, __result));
            }
        }
    }
}
#endif
