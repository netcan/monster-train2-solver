#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEquilibriusDrawWitness
    {
        private const string OwnerId = "b7648a1e-47c3-4d52-a0ab-065079c7ae06";
        internal const string FileName = "equilibrius-t3-draw.mt2f";
        private static readonly List<Snapshot> samples = new();
        private static string[]? removedCards;
        private static bool started, prepared, advanced;
        private static Exception? failure;

        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_EQUILIBRIUS_DRAW") == "1";

        internal static bool Prepare(AllGameManagers managers)
        {
            if (failure != null) throw new InvalidOperationException("Original Equilibrius purge setup failed.", failure);
            if (started) return prepared;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.InTestScenario || save.GetCurrentDistance() != 3 ||
                save.GetRelicPersistentState().GetCountCardPurgesThisRun() != 0)
                throw new InvalidOperationException("Expected the original distance-three Equilibrius checkpoint before a purge.");
            SoulState owner = save.GetSelectedSouls().Single(soul => soul.GetRelicDataID() == OwnerId);
            RelicEffectPurgeCardsUntilDeckLimitMet effect = owner.GetEffects().OfType<RelicEffectPurgeCardsUntilDeckLimitMet>().Single();
            CardState[] cards = save.GetDeckState().Where(card => !card.IsChampionCard()).Take(5).ToArray();
            if (cards.Length != 5) throw new InvalidOperationException("The original Equilibrius checkpoint requires five purge targets.");
            var relicManager = managers.GetRelicManager();
            started = true;
            relicManager.StartCoroutine(Purge());
            return false;

            IEnumerator Purge()
            {
                Capture();
                foreach (CardState card in cards)
                {
                    save.RemoveCardFromDeck(card);
                    yield return relicManager.ApplyOnPurgeRelicEffectsNotInCombat(card, relicManager);
                    Capture();
                }
                removedCards = cards.Select(card => card.GetCardDataID()).ToArray();
                prepared = failure == null;
            }

            void Capture()
            {
                try
                {
                    samples.Add(new Snapshot(save.GetRelicPersistentState().GetCountCardPurgesThisRun(), save.GetVisibleDeckCount(),
                        save.GetStartOfTurnCards(), effect.Value, RelicResourceProbe.Definition(effect, 0, managers)));
                }
                catch (Exception error) { failure = error; }
            }
        }

        internal static bool Tick(AllGameManagers managers, Action advanceTurn)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager();
            int turn = combat.GetTurnCount();
            if (!prepared || save.PreviewMode) return false;
            if (turn == 0)
            {
                if (advanced) return false;
                advanced = true;
                save.MarkOneTimeMessageComplete(OneTimeMessage.EndTurnWithoutPlayingChampion);
                advanceTurn();
                return false;
            }

            if (turn != 1 || samples.Count != 6 || !samples.Select(sample => sample.Count).SequenceEqual(Enumerable.Range(0, 6)) ||
                !samples.Select(sample => sample.Value).SequenceEqual(new[] { 0f, 0f, 0f, 0f, 0f, 1f }) ||
                samples[^1].Draw != samples[0].Draw + 1 ||
                !samples.Select(sample => sample.Deck).SequenceEqual(Enumerable.Range(samples[0].Deck - 5, 6).Reverse()))
                throw new InvalidOperationException("The original Equilibrius purge threshold did not affect the native draw query.");
            CombatRelicState[] relics = RelicProbe.Capture(managers);
            CombatRelicState owner = relics.Single(relic => relic.DataId == OwnerId);
            if (!RelicResourceModel.IsPurgeCountDrawOwner(owner) ||
                owner.ResourceEffects is not [{ Amount: 1, DrawValue: 1, DrawMutationOrder: >= 0 }] ||
                save.GetRelicPersistentState().GetCountCardPurgesThisRun() != 5 || save.GetStartOfTurnCards() != samples[^1].Draw)
                throw new InvalidOperationException("The original Equilibrius positive draw value did not survive into combat.");

            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "NativeEquilibriusT3Draw", GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, OwnerId, RemovedCards = removedCards,
                Samples = samples, CombatTurn = turn, CombatDraw = save.GetStartOfTurnCards(),
                HandCount = managers.GetCardManager().GetHand().Count, Relics = relics, Resources = RelicResourceProbe.CaptureResources(managers)
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName));
            document.Write(output);
            return true;
        }

        private sealed class Snapshot(int count, int deck, int draw, float value, RelicResourceEffect rule)
        {
            public int Count { get; } = count;
            public int Deck { get; } = deck;
            public int Draw { get; } = draw;
            public float Value { get; } = value;
            public RelicResourceEffect Rule { get; } = rule;
        }
    }
}
#endif
