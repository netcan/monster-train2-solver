#if DEVELOPMENT_PROBE
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class HallGraftedBirthProbe
    {
        internal sealed class Callback(string phase, int card, int result, int graft, bool? cardless)
        {
            public string Phase { get; } = phase;
            public int Card { get; } = card;
            public int Result { get; } = result;
            public int Graft { get; } = graft;
            public bool? Cardless { get; } = cardless;
        }

        internal sealed class CloneObservation
        {
            private readonly List<CardState> cards = new();
            public int PaidSpawner { get; }
            public int PaidGraft { get; }
            public bool Completed { get; internal set; }
            public List<Callback> Callbacks { get; } = new();

            internal CloneObservation(CardState source)
            {
                PaidSpawner = Reference(source);
                PaidGraft = Reference(Graft(source));
            }

            // These tokens identify native object references only within this observer.
            // The regular trace assigns combat card IDs through a separate capture path.
            internal int Reference(CardState? card)
            {
                if (card == null) return 0;
                int index = cards.FindIndex(previous => ReferenceEquals(previous, card));
                if (index >= 0) return index + 1;
                cards.Add(card);
                return cards.Count;
            }
        }

        private static CloneObservation? active;
        internal static readonly List<CloneObservation> Clones = new();

        private static CardState? Graft(CardState? card) =>
            card?.GetCardStateModifiers().GraftedEquipmentCardState;

        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.CloneMonsterState))]
        private static class ClonePatch
        {
            private static void Postfix(object[] __args, ref IEnumerator __result)
            {
                if (!OriginalSupportSpellsScenario.ObserveHallGraftedBirth || FullBattleTrace.Active == null ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode || active != null ||
                    __args.OfType<CharacterState>().FirstOrDefault()?.GetSpawnerCard() is not CardState source ||
                    source.GetCardDataID() != OriginalSupportSpellsScenario.AssistantId || Graft(source) == null) return;
                __result = Observe(__result, source);
            }

            private static IEnumerator Observe(IEnumerator native, CardState source)
            {
                var observation = new CloneObservation(source);
                Clones.Add(observation);
                active = observation;
                try
                {
                    while (native.MoveNext()) yield return native.Current;
                    observation.Completed = true;
                }
                finally
                {
                    active = null;
                    (native as System.IDisposable)?.Dispose();
                }
            }
        }

        [HarmonyPatch(typeof(CardManager), nameof(CardManager.CopyCardState))]
        private static class CopyPatch
        {
            private static void Prefix(CardState sourceCardState, out int __state)
            {
                __state = 0;
                if (active == null) return;
                __state = active.Reference(sourceCardState);
                active.Callbacks.Add(new Callback("CopyCardEnter", __state, 0,
                    active.Reference(Graft(sourceCardState)), null));
            }

            private static void Postfix(CardState __result, int __state)
            {
                if (active == null || __state == 0) return;
                active.Callbacks.Add(new Callback("CopyCardExit", __state, active.Reference(__result),
                    active.Reference(Graft(__result)), null));
            }
        }

        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.CreateMonsterState))]
        private static class BirthPatch
        {
            private static void Prefix(CardState spawnerCard, CardState fromPlayedCard, bool isCardless)
            {
                if (active == null) return;
                active.Callbacks.Add(new Callback("CreateMonster", active.Reference(spawnerCard),
                    active.Reference(fromPlayedCard), active.Reference(Graft(spawnerCard)), isCardless));
            }
        }

        internal static void ObserveAttachment(CharacterState host, CardState card)
        {
            if (active == null) return;
            CardState? spawner = host.GetSpawnerCard();
            active.Callbacks.Add(new Callback("AddEquipment", active.Reference(spawner),
                active.Reference(card), active.Reference(Graft(spawner)), null));
        }
    }
}
#endif
