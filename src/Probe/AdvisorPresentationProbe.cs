using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrain2Poju.Probe
{
    // Isolated acceptance only. Observe the actual player UI across native
    // card/EndTurn hooks, animation settlement and verified publications.
    internal static class AdvisorPresentationProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR_PRESENTATION") == "1";
        private static Type type = null!;
        private static object overlay = null!;
        private static ManualLogSource log = null!;
        private static bool expanded;
        private static int retainedInputs, pendingFrames, reusedPublications, scrolledPublications, resets, budgetChanges, collapsedTurnResets;
        private static int cardIdentityChecks, cardHoverChecks, forecastScopeChecks;
        internal static object Evidence => new { RetainedInputs = retainedInputs, PendingFrames = pendingFrames,
            ReusedPublications = reusedPublications, ScrolledPublications = scrolledPublications, Resets = resets,
            BudgetChanges = budgetChanges, CollapsedTurnResets = collapsedTurnResets,
            CardIdentityChecks = cardIdentityChecks, CardHoverChecks = cardHoverChecks, ForecastScopeChecks = forecastScopeChecks };

        private static T Field<T>(string name) => (T)AccessTools.Field(type, name).GetValue(overlay);
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Player presentation acceptance: " + message); }

        internal static void Install(Type playerType, object advisor, ManualLogSource logger)
        {
            type = playerType; overlay = advisor; log = logger;
            var harmony = new Harmony("netcan.poju.isolated-presentation-acceptance");
            harmony.Patch(AccessTools.Method(type, "InvalidateState"),
                prefix: new HarmonyMethod(typeof(AdvisorPresentationProbe), nameof(BeforeInvalidation)),
                postfix: new HarmonyMethod(typeof(AdvisorPresentationProbe), nameof(AfterInvalidation)));
            harmony.Patch(AccessTools.Method(type, "Tick"),
                prefix: new HarmonyMethod(typeof(AdvisorPresentationProbe), nameof(BeforeTick)),
                postfix: new HarmonyMethod(typeof(AdvisorPresentationProbe), nameof(AfterTick)));
        }

        private sealed class Snapshot
        {
            internal readonly BattleAdvice? Advice = Field<BattleAdvice?>("displayed");
            internal readonly string Text = Field<Text>("body")?.text ?? "";
            internal readonly float Scroll = Field<ScrollRect>("planScroll")?.verticalNormalizedPosition ?? 1;
            internal readonly long Searches = Field<BattleAdviceSession>("session").SearchCount;
            internal readonly long PublishedSearches = Field<long>("displayedSearches");
        }

        internal static void ExpandAndScroll()
        {
            AccessTools.Method(type, "SetFullPlan").Invoke(overlay, new object[] { true });
            expanded = true;
            ScrollForNextInput();
            CheckSearchBudget();
            CheckForecastScope();
            CheckCardIdentity();
        }

        private static void CheckForecastScope()
        {
            Type formatter = type.Assembly.GetType(typeof(AdvisorForecastPresentation).FullName!, true)!;
            string Header(RoomOutcome outcome, int turns, int current, int final) =>
                (string)AccessTools.Method(formatter, "Describe").Invoke(null, new object[] { outcome, turns, current, final });
            string partial = Header(RoomOutcome.Exchanged, 2, 23, 23);
            Require(partial.Contains("部分方案") && partial.Contains("仅预测 2 回合") && partial.Contains("窗口内预计损失 0") &&
                partial.Contains("整场战损尚未确定") && !partial.Contains("预计获胜"), "partial zero-loss forecast looks like a complete victory");
            string victory = Header(RoomOutcome.BattleWon, 2, 22, 4);
            Require(victory.Contains("整场预计获胜") && victory.Contains("22 → 4") && victory.Contains("后续预计损失 18") &&
                !victory.Contains("尚未确定"), "complete forecast hides current versus final Pyre health");
            Require(Header(RoomOutcome.PlayerDefeated, 1, 22, 0).Contains("整场预计战败") &&
                Header(RoomOutcome.Stalemate, 1, 22, 22).Contains("预计僵持"), "terminal loss/stalemate scope changed");
            string ability = (string)AccessTools.Method(formatter, "Ability").Invoke(null,
                new object[] { "吞火龙", "火焰吐息", 1, 0 });
            Require(ability.Contains("【单位技能】吞火龙：火焰吐息") && ability.Contains("当前 0 费") &&
                ability.Contains("来源：当前第 2 层"), "unit ability lacks an explicit source or free cost");
            Require((string)AccessTools.Method(formatter, "SearchScope").Invoke(null, new object[] { true, 3 }) == "整场搜索" &&
                (string)AccessTools.Method(formatter, "SearchScope").Invoke(null, new object[] { false, 3 }) == "未来 3 回合",
                "whole-battle search is labeled as a three-turn window");
            forecastScopeChecks += 7;
            log.LogInfo("PLAYER-ADVISOR-FORECAST-SCOPE-PASS cases=" + forecastScopeChecks);
        }

        private static void CheckCardIdentity()
        {
            AllGameManagers managers = AllGameManagers.Instance;
            var hand = managers.GetCardManager().GetHand();
            CardState original = hand.First();
            Type formatter = type.Assembly.GetType(typeof(AdvisorCardPresentation).FullName!, true)!;
            string Describe(CardState card, int index, int? cost) =>
                (string)AccessTools.Method(formatter, "Describe").Invoke(null, new object?[] { card, index, cost });
            // Detached native objects with the same localized title. Do not
            // install upgrades on, reorder, or otherwise mutate the real hand.
            var plain = new CardState(); var upgraded = new CardState();
            foreach (CardState card in new[] { plain, upgraded })
            {
                AccessTools.Field(typeof(CardState), "titleKey").SetValue(card, original.GetTitleKey());
                AccessTools.Field(typeof(CardState), "cardType").SetValue(card, CardType.Spell);
            }
            var damage = new CardUpgradeState(); damage.SetAttackDamage(10); damage.SetCostReduction(1);
            upgraded.GetCardStateModifiers().GetCardUpgrades().Add(damage);
            var heal = new CardUpgradeState(); heal.SetAdditionalHeal(5);
            upgraded.GetTemporaryCardStateModifiers().GetCardUpgrades().Add(heal);
            string first = Describe(plain, 0, 2), second = Describe(upgraded, 2, 1);
            Require(plain.GetTitle() == upgraded.GetTitle() && first != second &&
                first.Contains("当前手牌左起第 1 张") && first.Contains("当前 2 费") && first.Contains("当前升级：无") &&
                second.Contains("当前手牌左起第 3 张") && second.Contains("当前 1 费") &&
                second.Contains("法强+10") && second.Contains("费用-1") && second.Contains("临时：治疗+5"),
                "same-title upgraded cards lack distinct location, current cost or upgrade details");
            string future = Describe(upgraded, -1, null);
            Require(future.Contains("当前不在手牌") && !future.Contains("左起") && !future.Contains(" 费"),
                "a future card was assigned a current hand slot or cost");
            string generated = (string)AccessTools.Method(formatter, "Predicted").Invoke(null, new object[] { "<b>魔纺术</b>" });
            Require(generated == "魔纺术 · 预计生成" && !generated.Contains("当前手牌"),
                "a forecast-generated card must not look like a current hand card");
            cardIdentityChecks += 4;

            var before = new Snapshot();
            int captures = (int)AccessTools.Property(type, "Captures").GetValue(overlay);
            string Stamp() => TwoTurnScenario.CaptureSnapshot("card-hover", managers, managers.GetSaveManager(),
                managers.GetCombatManager(), managers.GetCardManager(), log, writeLog: false, includePresentationRng: true);
            string native = Stamp(); UnityEngine.Random.State rng = UnityEngine.Random.state;
            object hover = Field<object>("cardHover"); Type hoverType = hover.GetType();
            var entries = (IList)AccessTools.Field(hoverType, "Entries").GetValue(hover);
            var body = Field<Text>("body"); var scroll = Field<ScrollRect>("planScroll");
            CardState? Highlighted() => (CardState?)AccessTools.Property(hoverType, "HighlightedCard").GetValue(hover);
            void Update(Vector2 point) => AccessTools.Method(hoverType, "UpdateHover").Invoke(hover, new object[] { point });
            // Exercise actual native hand instances, including duplicate names
            // when present, wrapped rows, clipped rows and the collapsed view.
            CardState[] selected = hand.GroupBy(card => card.GetTitle()).Where(group => group.Count() >= 2)
                .Select(group => group.Take(2).ToArray()).FirstOrDefault() ?? hand.Take(2).ToArray();
            Require(selected.Length == 2, "hover acceptance needs two native hand cards");
            try
            {
                foreach (bool expandedView in new[] { true, false })
                {
                    AccessTools.Method(type, "SetFullPlan").Invoke(overlay, new object[] { expandedView });
                    entries.Clear();
                    body.text = "\n" + string.Join("\n", selected.Select((card, index) =>
                        (index + 1) + ". " + Describe(card, hand.IndexOf(card), 1) + "\n第 3 层")) + "\n结束回合";
                    int start = body.text.IndexOf("1. ", StringComparison.Ordinal);
                    int split = body.text.IndexOf("2. ", StringComparison.Ordinal);
                    int end = body.text.IndexOf("结束回合", StringComparison.Ordinal);
                    AccessTools.Method(hoverType, "AddEntry").Invoke(hover, new object[] { start, split - 1, selected[0] });
                    AccessTools.Method(hoverType, "AddEntry").Invoke(hover, new object[] { split, end - 1, selected[1] });
                    Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1;
                    foreach (var row in new[] { (Start: start, Card: selected[0]), (Start: split, Card: selected[1]) })
                    {
                        UILineInfo line = body.cachedTextGenerator.lines.Last(item => item.startCharIdx <= row.Start);
                        Vector2 Point() => RectTransformUtility.WorldToScreenPoint(null,
                            body.rectTransform.TransformPoint(new Vector3(10, (line.topY - line.height * .5f) / body.pixelsPerUnit)));
                        Vector2 point = Point();
                        if (!RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, point))
                        {
                            Update(point); Require(Highlighted() == null, "clipped recommendation highlighted a card");
                            scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases(); point = Point();
                        }
                        Require(RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, point), "test row is outside viewport");
                        Update(point);
                        Require(ReferenceEquals(Highlighted(), row.Card), "hover highlighted a different native card instance");
                        Require(Field<GameObject>("canvas").GetComponentsInChildren<Image>(true)
                            .Where(image => image.transform.parent.name == "Suggested hand card").All(image => !image.raycastTarget),
                            "card highlight blocks native input");
                        Update(new Vector2(-100, -100)); Require(Highlighted() == null, "leaving advice retained its highlight");
                        cardHoverChecks++;
                    }
                }
                var gate = AccessTools.Field(hoverType, "CanHover");
                object originalGate = gate.GetValue(hover);
                try
                {
                    gate.SetValue(hover, (Func<bool>)(() => false));
                    Update(RectTransformUtility.WorldToScreenPoint(null, body.rectTransform.position));
                    Require(Highlighted() == null, "pending advice kept a hand highlight");
                    cardHoverChecks++;
                }
                finally { gate.SetValue(hover, originalGate); }
            }
            finally
            {
                AccessTools.Method(type, "SetFullPlan").Invoke(overlay, new object[] { true });
                body.text = (string)AccessTools.Method(type, "Describe").Invoke(overlay, new object[] { before.Advice! });
                Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = before.Scroll;
                AccessTools.Method(hoverType, "Clear").Invoke(hover, Array.Empty<object>());
            }
            Require(native == Stamp() && rng.Equals(UnityEngine.Random.state) &&
                captures == (int)AccessTools.Property(type, "Captures").GetValue(overlay) && Field<BattleAdviceSession>("session").SearchCount == before.Searches &&
                ReferenceEquals(Field<BattleAdvice?>("displayed"), before.Advice),
                "card descriptions/hover changed native state, RNG, capture/search counts or published advice");
            log.LogInfo("PLAYER-ADVISOR-CARD-IDENTITY-PASS descriptions=" + cardIdentityChecks + " hovers=" + cardHoverChecks +
                " duplicateNativeTitles=" + (selected[0].GetTitle() == selected[1].GetTitle()) + " captures=0 searches=0");
        }

        private static void CheckSearchBudget()
        {
            var before = new Snapshot();
            long generation = Field<BattleAdviceSession>("session").Generation;
            int original = Field<int>("timeLimitMs");
            var label = Field<Text>("budgetLabel");
            var read = Field<Func<int>>("readTimeLimitMs");
            var save = Field<Action<int>>("saveTimeLimitMs");
            for (int index = 0; index < 3; index++)
            {
                int current = Field<int>("timeLimitMs");
                int expected = current < 3000 ? 3000 : current < 10000 ? 10000 : current < 30000 ? 30000 : 3000;
                label.transform.parent.GetComponent<Button>().onClick.Invoke();
                Require(Field<int>("timeLimitMs") == expected && read() == expected &&
                    label.text == "时长 " + (expected / 1000) + " 秒", "search budget button did not persist its displayed value");
                Require(Field<BattleAdviceSession>("session").SearchCount == before.Searches &&
                    Field<BattleAdviceSession>("session").Generation == generation &&
                    ReferenceEquals(Field<BattleAdvice?>("displayed"), before.Advice) &&
                    Field<Text>("body").text == before.Text &&
                    Math.Abs(Field<ScrollRect>("planScroll").verticalNormalizedPosition - before.Scroll) < .005,
                    "changing search duration restarted search or changed the readable advice");
                budgetChanges++;
            }
            // The configuration manager writes the same provider. Observe it
            // on the normal UI tick, preserving the existing publication.
            save(5000);
            AccessTools.Method(type, "Tick").Invoke(overlay, Array.Empty<object>());
            Require(Field<int>("timeLimitMs") == 5000 && label.text == "时长 5 秒" &&
                Field<BattleAdviceSession>("session").SearchCount == before.Searches &&
                Field<BattleAdviceSession>("session").Generation == generation &&
                ReferenceEquals(Field<BattleAdvice?>("displayed"), before.Advice),
                "configuration duration was not applied live or restarted search");
            budgetChanges++;
            save(original);
            AccessTools.Method(type, "SetSearchTimeLimit").Invoke(overlay, new object[] { original });
            log.LogInfo("PLAYER-ADVISOR-BUDGET-PASS changes=" + budgetChanges + " newSearches=0 persisted=true");
        }

        private static void ScrollForNextInput()
        {
            ScrollRect scroll = Field<ScrollRect>("planScroll");
            LayoutRebuilder.ForceRebuildLayoutImmediate(Field<Text>("body").rectTransform);
            if (scroll.content.rect.height > scroll.viewport.rect.height + 10)
                scroll.verticalNormalizedPosition = .37f;
        }

        private static void BeforeInvalidation(out Snapshot __state) => __state = new Snapshot();
        private static void AfterInvalidation(bool preserveContinuation, Snapshot __state)
        {
            if (__state.Advice == null) return;
            if (!preserveContinuation)
            {
                Require(Field<BattleAdvice?>("displayed") == null, "explicit reset kept old advice");
                Require(!Field<Text>("body").text.Contains("本回合步骤") && !Field<Text>("body").text.Contains("完整方案"),
                    "explicit reset kept the old forecast text");
                resets++; return;
            }
            Require(ReferenceEquals(Field<BattleAdvice?>("displayed"), __state.Advice), "input cleared the readable forecast");
            Require(Field<Text>("body").text == __state.Text, "input replaced forecast text with a waiting message");
            Require(Math.Abs(Field<ScrollRect>("planScroll").verticalNormalizedPosition - __state.Scroll) < .005,
                "input moved the reading position");
            Require(Field<Text>("statusLabel").text.Contains("待确认"), "retained forecast was not marked pending");
            Require(Field<BattleAdviceSession>("session").Current == null, "pending forecast remained a valid publication");
            retainedInputs++;
        }

        private static void BeforeTick(out Snapshot __state) => __state = new Snapshot();
        private static void AfterTick(Snapshot __state)
        {
            BattleAdvice? displayed = Field<BattleAdvice?>("displayed");
            // The worker can start a new search between UI ticks after an
            // exhausted partial plan. Compare with the displayed publication's
            // search, rather than this frame's already incremented counter.
            if (__state.Advice != null && displayed == null && Field<BattleAdviceSession>("session").SearchCount == __state.PublishedSearches &&
                !Field<bool>("paused") && Field<bool>("panelVisible") && Field<string?>("captureError") == null &&
                Field<BattleAdviceSession>("session").Status != BattleAdviceStatus.Unavailable &&
                AllGameManagers.Instance?.GetSaveManager()?.GetGameSequence() == SaveData.GameSequence.InBattle)
                Require(false, "native loading/animation frame cleared the readable forecast without a new search");
            if (displayed == null) return;
            if (ReferenceEquals(displayed, __state.Advice))
            {
                Require(Field<Text>("body").text == __state.Text, "waiting frame rewrote retained text");
                if (Field<Text>("statusLabel").text.Contains("待确认")) pendingFrames++;
                return;
            }
            if (__state.Advice == null || Field<BattleAdviceSession>("session").SearchCount != __state.Searches ||
                !(displayed.ReusedContinuation || displayed.ReusedSelection)) return;
            ScrollRect scroll = Field<ScrollRect>("planScroll");
            bool newTurn = displayed.Root.Spawn.Turn != __state.Advice.Root.Spawn.Turn;
            if (newTurn && !Field<bool>("showFullPlan"))
            {
                // Unity reports normalized position zero for some content
                // that fits entirely in the viewport. Check its actual top
                // offset in that case; every step is already visible.
                bool fits = scroll.content.rect.height <= scroll.viewport.rect.height + 1;
                bool atTop = fits ? Math.Abs(scroll.content.anchoredPosition.y) < .5f :
                    Math.Abs(scroll.verticalNormalizedPosition - 1) < .005;
                Require(atTop && scroll.velocity == Vector2.zero,
                    "collapsed new turn did not return to its first step or kept scroll inertia" +
                    " position=" + scroll.verticalNormalizedPosition + " offset=" + scroll.content.anchoredPosition.y +
                    " height=" + scroll.content.rect.height + "/" + scroll.viewport.rect.height + " velocity=" + scroll.velocity);
                string text = Field<Text>("body").text;
                Require(text.Contains("本回合步骤") && text.Contains("\n1. "), "new turn omitted its first current-turn step");
                collapsedTurnResets++;
            }
            else if (scroll.content.rect.height > scroll.viewport.rect.height + 10)
            {
                Require(Math.Abs(scroll.verticalNormalizedPosition - __state.Scroll) < .005,
                    "reused publication reset the reading position");
                if (__state.Scroll < .95f) scrolledPublications++;
            }
            Require(!Field<Text>("statusLabel").text.Contains("待确认"), "verified publication still marked pending");
            reusedPublications++;
            log.LogInfo("PLAYER-ADVISOR-PRESENTATION-PASS reuse=" + reusedPublications +
                " retainedInputs=" + retainedInputs + " pendingFrames=" + pendingFrames + " scroll=" + scroll.verticalNormalizedPosition);
            // First exercise the existing expanded reading position, then keep
            // the panel collapsed through actual native turn transitions.
            if (expanded && scrolledPublications > 0)
            {
                AccessTools.Method(type, "SetFullPlan").Invoke(overlay, new object[] { false });
                expanded = false;
            }
            ScrollForNextInput();
        }
    }
}
