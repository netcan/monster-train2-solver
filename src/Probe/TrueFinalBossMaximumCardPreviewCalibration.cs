using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrueFinalBossMaximumCardPreviewCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_MAXIMUM_CARD_PREVIEW") == "1";
        private sealed class Input
        {
            internal string Label = "";
            internal (string Type, int Amount)[] Effects = Array.Empty<(string, int)>();
            internal (string Type, int Amount)[] OnCast = Array.Empty<(string, int)>();
            internal bool Room;
        }
        private sealed class Record
        {
            public string Label = "";
            public int CardId, TargetId, RoomIndex;
            public int SelectedRoomIndex;
            public bool MismatchedFocus;
            public CardPlayRule Rule = null!;
            public TrainCombatState Before = null!, After = null!, Cleared = null!;
            public TrainCombatState? CopiedBefore, CopiedActual;
            public int EnableCount, DisableCount;
            public int[] CopiedIds = Array.Empty<int>();
            public HealthChangedEvent[] Events = Array.Empty<HealthChangedEvent>();
            public object RngBefore = null!, RngAfter = null!;
            public bool NativeReturned, QueueEmpty, TemporaryDisabled;
        }
        private static IReadOnlyList<CharacterState> actors = Array.Empty<CharacterState>();
        private static Record? active;
        private static readonly List<HealthChangedEvent> events = new List<HealthChangedEvent>();
        private static void Set(object owner, string name, object value) => AccessTools.Field(owner.GetType(), name).SetValue(owner, value);
        private static TrainCombatState Snapshot(IReadOnlyList<CharacterState>? copied)
        {
            var selected = AllGameManagers.Instance!.GetRoomManager().GetRoom(active?.RoomIndex ?? 0);
            var room = TrueFinalBossMaximumPreviewCalibration.Snapshot(actors, copied, selected);
            var world = room.Context!.Enchantments!;
            return new TrainCombatState(world.Rooms.Select(floor => new RoomCombatState(floor.RoomIndex, floor.Deployment,
                floor.Units, floor.ExternalInteractions, room.Context, floor.Preview, floor.ActiveCompanionBossIds)).ToArray(),
                world.Movement, world.EnemySlotsPerRoom, room.Context);
        }
        internal static IEnumerator Capture(IReadOnlyList<CharacterState> originalActors)
        {
            actors = originalActors.ToArray();
            var managers = AllGameManagers.Instance!; var trace = FullBattleTrace.Active!;
            var combat = managers.GetCombatManager(); var cards = managers.GetCardManager();
            var all = managers.GetAllGameData(); var save = managers.GetSaveManager();
            if (actors.Count != 5 || save.PreviewMode || actors.Any(actor => actor.PreviewMode || actor.TemporaryPreviewsEnabled))
                throw new InvalidOperationException("Card hover controls require five original primary final bosses.");
            bool suppression = CombatPreviewControl.Save(combat);
            int originalSuppression = (int)AccessTools.Field(typeof(CombatManager), "combatPreviewSuppressionCount").GetValue(combat);
            int originalCounter = combat.TrueFinalBossBattlePreviewHpChange;
            var inputs = new[] {
                new Input { Label = "debuff-small", Effects = new[] { ("CardEffectDebuffMaxHealth", 3) } },
                new Input { Label = "debuff-protected", Effects = new[] { ("CardEffectDebuffMaxHealth", 99999) } },
                new Input { Label = "buff-healed", Effects = new[] { ("CardEffectBuffMaxHealth", 9) } },
                new Input { Label = "buff-then-debuff", Effects = new[] { ("CardEffectBuffMaxHealth", 9), ("CardEffectDebuffMaxHealth", 2) } },
                new Input { Label = "debuff-buff-debuff", Effects = new[] { ("CardEffectDebuffMaxHealth", 99999), ("CardEffectBuffMaxHealth", 9), ("CardEffectDebuffMaxHealth", 2) } },
                new Input { Label = "zero", Effects = new[] { ("CardEffectDebuffMaxHealth", 0) } },
                new Input { Label = "negative", Effects = new[] { ("CardEffectDebuffMaxHealth", -3) } },
                new Input { Label = "oncast-buff", Effects = new[] { ("CardEffectDebuffMaxHealth", 3) }, OnCast = new[] { ("CardEffectBuffMaxHealth", 9) } },
                new Input { Label = "oncast-debuff", Effects = new[] { ("CardEffectBuffMaxHealth", 9) }, OnCast = new[] { ("CardEffectDebuffMaxHealth", 99999) } },
                new Input { Label = "room-sweep", Room = true, Effects = new[] { ("CardEffectDebuffMaxHealth", 3) }, OnCast = new[] { ("CardEffectBuffMaxHealth", 1) } }
            };
            CardEffectData Effect((string Type, int Amount) input, bool room)
            {
                var effect = new CardEffectData(input.Type, null!, Team.Type.Heroes);
                effect.Cheat_SetTargetMode(room ? TargetMode.Room : TargetMode.DropTargetCharacter);
                Set(effect, "paramInt", input.Amount);
                if (input.Type == "CardEffectBuffMaxHealth") Set(effect, "additionalParamInt1", (int)UnitUpgradeLifetimeTempOnly.TemporaryUntilEndOfBattle);
                return effect;
            }
            foreach (var card in cards.GetHand().ToArray()) cards.MoveCardFromHandToTopOfDeck(card);
            var controlled = inputs.Select((input, index) =>
            {
                var data = UnityEngine.Object.Instantiate(all.FindCardData(DevotionSpawnBattleScenario.VenerateId));
                data.name = "PojuLifeLinkHover" + index;
                Set(data, "id", "fc231b62-0000-4000-8000-" + (index + 1).ToString("D12")); Set(data, "cost", 2);
                Set(data, "effects", input.Effects.Select(effect => Effect(effect, input.Room)).ToList());
                var triggers = new List<CardTriggerEffectData>();
                if (input.OnCast.Length != 0)
                {
                    var trigger = new CardTriggerEffectData(); Set(trigger, "trigger", CardTriggerType.OnCast);
                    Set(trigger, "cardEffects", input.OnCast.Select(effect => Effect(effect, input.Room)).ToList()); triggers.Add(trigger);
                }
                Set(data, "traits", new List<CardTraitData>()); Set(data, "triggers", triggers);
                Set(data, "effectTriggers", new List<CharacterTriggerData>()); Set(data, "startingUpgrades", new List<CardUpgradeData>());
                Set(data, "targetless", input.Room); Set(data, "targetsRoom", true);
                ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(all)).Add(data);
                return cards.AddNewCard(data, CardPile.HandPile, false, false, animate: false) ?? throw new InvalidOperationException("Missing authored hover input.");
            }).ToArray();
            trace.InvalidateCardRules();
            foreach (var card in controlled) { trace.CardId(card); cards.RefreshCardInHand(card); }
            var samples = new List<Record>();
            foreach (var actor in actors) actor.AddHpChangedListener((changed, health) =>
            { if (active != null) events.Add(new HealthChangedEvent(trace.UnitId(changed), health)); });
            CombatPreviewControl.Set(combat, true);
            try
            {
                for (int caseIndex = 0; caseIndex <= inputs.Length; caseIndex++)
                foreach (var target in caseIndex == inputs.Length ? actors.Take(1) : actors)
                {
                    bool mismatch = caseIndex == inputs.Length;
                    int inputIndex = mismatch ? inputs.Length - 1 : caseIndex;
                    var card = controlled[inputIndex]; var room = target.GetCurrentRoom();
                    yield return managers.GetRoomManager().GetRoomUI().SetSelectedRoom(mismatch ? 0 : room.GetRoomIndex());
                    active = new Record { Label = inputs[inputIndex].Label, CardId = trace.CardId(card), TargetId = trace.UnitId(target),
                        RoomIndex = room.GetRoomIndex(), SelectedRoomIndex = managers.GetRoomManager().GetSelectedRoom(),
                        MismatchedFocus = mismatch, Rule = BattleActionProbe.Definition(all.FindCardData(card.GetCardDataID())!) };
                    if ((active.SelectedRoomIndex != active.RoomIndex) != mismatch)
                        throw new InvalidOperationException("Native room selection did not reach the authored focus control.");
                    active.Before = Snapshot(null); active.RngBefore = HalfHealthLifecycleCalibration.Rng(); events.Clear();
                    UnityEngine.Debug.Log("LIFE-LINK-MAXIMUM-CARD-PREVIEW sample=" + samples.Count + " label=" + active.Label + " target=" + active.TargetId);
                    combat.PreviewCardPlay(true, card, room, target, target.GetSpawnPoint(), null!, null!);
                    active.NativeReturned = true; active.After = Snapshot(null);
                    combat.PreviewCardPlay(false, card, room, target, target.GetSpawnPoint(), null!, null!);
                    active.Cleared = Snapshot(null); active.RngAfter = HalfHealthLifecycleCalibration.Rng(); active.Events = events.ToArray();
                    var queue = (Queue<CombatManager.TriggerQueueData>)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat);
                    active.QueueEmpty = queue.Count == 0;
                    active.TemporaryDisabled = !save.PreviewMode && actors.All(actor => !actor.PreviewMode && !actor.TemporaryPreviewsEnabled);
                    samples.Add(active);
                    if (active.EnableCount != 1 || active.DisableCount != 1 || active.CopiedBefore == null || active.CopiedActual == null ||
                        !active.QueueEmpty || !active.TemporaryDisabled || actors.Any(actor => !actor.IsAlive || actor.IsDestroyed))
                        throw new InvalidOperationException("Native hover did not complete its independent copy/cancel boundary.");
                    active = null;
                }
            }
            finally
            {
                active = null; combat.TrueFinalBossBattlePreviewHpChange = originalCounter;
                CombatPreviewControl.Set(combat, suppression);
            }
            int finalSuppression = (int)AccessTools.Field(typeof(CombatManager), "combatPreviewSuppressionCount").GetValue(combat);
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId, Boundary = "NativeLifeLinkMaximumCardHover",
                NativeCompleted = true, AuthoredCards = true, OriginalBosses = true, Samples = samples,
                OriginalCounter = originalCounter, FinalCounter = combat.TrueFinalBossBattlePreviewHpChange,
                OriginalSuppression = originalSuppression, FinalSuppression = finalSuppression }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "true-final-boss-maximum-card-preview.mt2f")))
                document.Write(output);
            if (finalSuppression != originalSuppression) throw new InvalidOperationException("Hover probe did not restore its native suppression token.");
            yield break;
        }
        [HarmonyPatch(typeof(CombatManager), "SetTemporaryStateEnabled")]
        private static class CopyPatch
        {
            private static void Prefix(bool enable, List<CharacterState> characters, bool isCardPreview)
            {
                if (active == null || !isCardPreview || enable) return;
                active.CopiedActual = Snapshot(characters); active.DisableCount++;
            }
            private static void Postfix(bool enable, List<CharacterState> characters, bool isCardPreview)
            {
                if (active == null || !isCardPreview || !enable) return;
                active.CopiedBefore = Snapshot(characters); active.CopiedIds = characters.Select(FullBattleTrace.Active!.UnitId).ToArray(); active.EnableCount++;
            }
        }
    }
}
