#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPactDrawWitness
    {
        private const string OwnerId = "17ed1b69-ffb7-436c-b87c-98c07aba0242";
        private const string OwnerKey = "PyreHeartPact_ArtifactData";
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly string[][] Plan =
        {
            new[] { "Neutral", "Secondary", "Secondary", "Primary" },
            new[] { "Primary", "Secondary", "AnyClan" }
        };
        internal enum Step { Wait, EndTurn, Complete }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_PACT_DRAW") == "1";
        internal static bool FullBattle => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_PACT_BATTLE") == "1";
        internal static bool Prepared => owner != null;
        private static RelicEffectDrawCardAfterEachClanPlayed? owner;
        private static string? pyreId, primary, secondary;
        private static UnitInput? input;
        private static PlaySample? pending;
        private static readonly List<PlaySample> plays = new();
        private static readonly List<Snapshot> starts = new();
        private static readonly List<ResetSample> resets = new();

        internal static void Prepare(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(CardState).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.GetGameSequence() != SaveData.GameSequence.BattleIntro || save.GetMutators().Count != 0)
                throw new InvalidOperationException("Expected the genuine original 2.3.1 BattleIntro without mutators.");
            var character = save.GetBalanceData().GetPyreHeartCharacterDatas()
                .Single(data => data.GetPyreHeartData().GetPyreArtifact()?.GetID() == OwnerId);
            var definition = character.GetPyreHeartData().GetPyreArtifact();
            var effect = definition.GetEffects().Single();
            pyreId = character.GetID();
            primary = save.GetMainClass().GetID();
            secondary = save.GetSubClass().GetID();
            if (character.GetAssetKey() != "PyreHeartPact" || !save.GetMetagameSave().HasUnlockedPyreHeart(pyreId) ||
                string.IsNullOrEmpty(primary) || string.IsNullOrEmpty(secondary) || primary == secondary ||
                definition.name != OwnerKey || definition.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != nameof(RelicEffectDrawCardAfterEachClanPlayed) ||
                effect.GetParamInt2() != 1 || !effect.GetParamBool() || effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Expected the unlocked original Pact Pyre and its unchanged draw definition.");
            save.SetActiveSavePyreCharacter(pyreId, "", loading: false);
            var active = save.GetActiveSavePyreArtifactState();
            if (save.GetActiveSavePyreCharacterData().GetID() != pyreId || active.GetRelicDataID() != OwnerId ||
                active.GetEffects() is not [RelicEffectDrawCardAfterEachClanPlayed live])
                throw new InvalidOperationException("The original Pact artifact did not initialize on its actual Pyre.");
            owner = live;
        }

        internal static Step Tick(FullBattleTrace trace, AllGameManagers managers)
        {
            var save = managers.GetSaveManager();
            var combat = managers.GetCombatManager();
            var cards = managers.GetCardManager();
            var rooms = managers.GetRoomManager();
            int turn = combat.GetTurnCount();
            if (!Prepared || save.PreviewMode || rooms.GetPyreRoom().GetPyreHeart().GetSourceCharacterData().GetID() != pyreId)
                throw new InvalidOperationException("Expected the original settled Pact Pyre.");
            if (turn == 0)
            {
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                return Step.EndTurn;
            }

            if (pending != null)
            {
                pending.After = Capture();
                int expected = pending.Before.Flags.Rewarded ? pending.Before.DrawModifier :
                    pending.After.Flags.Rewarded ? pending.Before.DrawModifier + 1 : pending.Before.DrawModifier;
                bool secondClan = pending.Role == "Primary" && pending.Before.Flags.Secondary ||
                    pending.Role == "Secondary" && pending.Before.Flags.Primary;
                bool rewardNow = !pending.Before.Flags.Rewarded && secondClan;
                if (pending.After.DrawModifier != expected ||
                    pending.After.Flags.Rewarded != (pending.Before.Flags.Rewarded || rewardNow) ||
                    pending.After.Flags.Primary != (pending.Before.Flags.Primary || pending.Role == "Primary") ||
                    pending.After.Flags.Secondary != (pending.Before.Flags.Secondary || pending.Role == "Secondary") ||
                    pending.Callbacks.Count != (pending.Before.Flags.Rewarded ? 0 : 1) ||
                    pending.Callbacks.Any(call => call.After == null) ||
                    pending.After.Decision.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == pending.Action.CardInstanceId))
                    throw new InvalidOperationException("Original Pact paid play differs on turn " + turn + ", role " + pending.Role + ".");
                plays.Add(pending);
                pending = null;
                input = null;
            }

            if (starts.Count < turn)
            {
                Snapshot start = Capture();
                if (start.Flags is not { Primary: false, Secondary: false, Rewarded: false } || start.DrawModifier != 0)
                    throw new InvalidOperationException("Original Pact flags and scheduled draw did not settle after EndTurn.");
                starts.Add(start);
            }
            if (turn == 3)
            {
                var battleResets = resets.Where(sample => sample.Before.Rewarded).ToArray();
                if (plays.Count != Plan.Sum(round => round.Length) || battleResets.Length != 2 ||
                    battleResets.Any(item => item.After is not { Primary: false, Secondary: false, Rewarded: false }))
                    throw new InvalidOperationException("Missing exact native Pact reset after either mixed round.");
                using var archive = NativeFixtureCapture.Capture(new
                {
                    Schema = 1, Boundary = "OriginalPactDrawAfterEachClan", GameVersion = Application.version, GameModuleMvid = Module,
                    OwnerId, OwnerKey, PyreId = pyreId, PrimaryClanId = primary, SecondaryClanId = secondary,
                    DrawNextTurn = true, DrawCount = 1, Starts = starts, Plays = plays, Resets = resets
                });
                using var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "pact-draw-after-each-clan.mt2f"));
                archive.Write(stream);
                return Step.Complete;
            }
            if (turn is < 1 or > 2) throw new InvalidOperationException("Original Pact control missed its two ordinary turns.");
            string[] roles = Plan[turn - 1];
            int index = plays.Count(play => play.Before.Decision.Spawn.Turn == turn);
            if (index == roles.Length) return Step.EndTurn;

            string requested = roles[index];
            input ??= FindInput(requested);
            if (rooms.GetSelectedRoom() != input.Room)
            {
                save.StartCoroutine(rooms.GetRoomUI().SetSelectedRoom(input.Room));
                return Step.Wait;
            }
            int handIndex = cards.GetHand().IndexOf(input.Card);
            if (handIndex < 0 || !cards.CanPlayHandCard(input.Card, input.Room, input.Point, null, null, out var error))
                throw new InvalidOperationException("Original Pact candidate was unavailable after native room selection.");
            pending = new PlaySample
            {
                Role = Role(input.Card),
                DataId = input.Card.GetCardDataID(),
                Action = new PlayCardAction(trace.CardId(input.Card), input.Room,
                    input.Card.IsSpawnerCard() ? input.Point.GetIndexInRoom() : -1,
                    targetUnitId: input.Target == null ? 0 : trace.UnitId(input.Target)),
                Before = Capture()
            };
            if (!cards.PlayCard(handIndex, input.Point, ref error))
                throw new InvalidOperationException("Original Pact candidate was rejected: " + error);
            return Step.Wait;

            Snapshot Capture() => new Snapshot(trace.CaptureDecision(), Flags(), cards.GetDrawCountModifier());
            UnitInput FindInput(string role)
            {
                foreach (CardState card in cards.GetHand().Where(card => !card.IsAnyAbility() &&
                    (Role(card) == role || role == "AnyClan" && Role(card) != "Neutral")))
                {
                    for (int roomIndex = 0; roomIndex < rooms.GetNumRooms(); roomIndex++)
                    {
                        RoomState room = rooms.GetRoom(roomIndex);
                        if (!room.IsRoomEnabled() || room.GetIsPyreRoom()) continue;
                        if (card.IsSpawnerCard())
                        {
                            SpawnPoint? point = room.GetFirstEmptyMonsterPoint();
                            if (point != null && cards.CanPlayHandCard(card, roomIndex, point, null, null, out _))
                                return new UnitInput(card, roomIndex, point, null);
                            continue;
                        }
                        if (card.GetCardType() != CardType.Spell) continue;
                        var actors = new List<CharacterState>();
                        room.AddCharactersToList(actors, Team.Type.Heroes);
                        room.AddCharactersToList(actors, Team.Type.Monsters);
                        foreach (CharacterState actor in actors.Where(unit => unit.IsAlive && !unit.IsDestroyed))
                            if (actor.GetSpawnPoint() is SpawnPoint point && cards.CanPlayHandCard(card, roomIndex, point, null, null, out _))
                                return new UnitInput(card, roomIndex, point, actor);
                    }
                }
                throw new InvalidOperationException("No legal original " + role + " Pact input on turn " + turn +
                    ". Hand=" + string.Join(",", cards.GetHand().Select(card => card.GetCardDataID() + "/" + Role(card))));
            }
        }

        private static string Role(CardState card) => card.GetLinkedClassID() == primary ? "Primary" :
            card.GetLinkedClassID() == secondary ? "Secondary" : "Neutral";

        private static FlagsSample Flags()
        {
            bool Read(string name) => (bool)AccessTools.Field(typeof(RelicEffectDrawCardAfterEachClanPlayed), name).GetValue(owner);
            return new FlagsSample(Read("hasPlayedMainClan"), Read("hasPlayedSubClan"), Read("hasDrawnBonusCardsThisTurn"));
        }

        private sealed class FlagsSample(bool primary, bool secondary, bool rewarded)
        {
            public bool Primary { get; } = primary;
            public bool Secondary { get; } = secondary;
            public bool Rewarded { get; } = rewarded;
        }

        private sealed class Snapshot(BattleTurnState decision, FlagsSample flags, int drawModifier)
        {
            public BattleTurnState Decision { get; } = decision;
            public FlagsSample Flags { get; } = flags;
            public int DrawModifier { get; } = drawModifier;
        }

        private sealed class UnitInput(CardState card, int room, SpawnPoint point, CharacterState? target)
        {
            internal CardState Card { get; } = card;
            internal int Room { get; } = room;
            internal SpawnPoint Point { get; } = point;
            internal CharacterState? Target { get; } = target;
        }

        private sealed class PlaySample
        {
            public string Role { get; set; } = null!;
            public string DataId { get; set; } = null!;
            public PlayCardAction Action { get; set; } = null!;
            public Snapshot Before { get; set; } = null!;
            public Snapshot? After { get; set; }
            public List<CallbackSample> Callbacks { get; } = new();
        }

        private sealed class CallbackSample(FlagsSample before, int drawBefore)
        {
            public FlagsSample Before { get; } = before;
            public int DrawBefore { get; } = drawBefore;
            public FlagsSample? After { get; set; }
            public int DrawAfter { get; set; }
        }

        private sealed class ResetSample(int turn, int drawModifier, FlagsSample before)
        {
            public int Turn { get; } = turn;
            public int DrawModifier { get; } = drawModifier;
            public FlagsSample Before { get; } = before;
            public FlagsSample? After { get; set; }
        }

        [HarmonyPatch(typeof(RelicEffectDrawCardAfterEachClanPlayed), nameof(RelicEffectDrawCardAfterEachClanPlayed.ApplyEffectOnCardPlayed))]
        private static class Apply
        {
            private static bool Prepare() => Enabled;
            private static void Postfix(RelicEffectDrawCardAfterEachClanPlayed __instance, CardPlayedRelicEffectParams relicEffectParams,
                ref IEnumerator __result)
            {
                if (ReferenceEquals(__instance, owner) && pending != null && ReferenceEquals(relicEffectParams.cardState, input?.Card) &&
                    !AllGameManagers.Instance.GetSaveManager().PreviewMode)
                    __result = Observe(__result, pending.Callbacks);
            }

            private static IEnumerator Observe(IEnumerator native, List<CallbackSample> callbacks)
            {
                var cards = AllGameManagers.Instance.GetCardManager();
                var sample = new CallbackSample(Flags(), cards.GetDrawCountModifier());
                callbacks.Add(sample);
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    sample.After = Flags();
                    sample.DrawAfter = cards.GetDrawCountModifier();
                }
                finally { (native as IDisposable)?.Dispose(); }
            }
        }

        [HarmonyPatch(typeof(RelicEffectDrawCardAfterEachClanPlayed), "ResetState")]
        private static class Reset
        {
            private static bool Prepare() => Enabled;
            private static void Prefix(RelicEffectDrawCardAfterEachClanPlayed __instance, out ResetSample? __state)
            {
                var managers = AllGameManagers.Instance;
                __state = ReferenceEquals(__instance, owner) && !managers.GetSaveManager().PreviewMode
                    ? new ResetSample(managers.GetCombatManager().GetTurnCount(), managers.GetCardManager().GetDrawCountModifier(), Flags()) : null;
            }
            private static void Postfix(ResetSample? __state)
            {
                if (__state == null) return;
                __state.After = Flags();
                resets.Add(__state);
            }
        }
    }
}
#endif
