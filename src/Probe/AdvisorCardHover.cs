using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrain2Poju.Probe
{
    // Draw a separate, noninteractive overlay. Native selection, focus, edge
    // effects and gameplay observation must not change when reading advice.
    internal sealed class AdvisorCardHover : MonoBehaviour
    {
        internal readonly struct Entry
        {
            internal readonly int Start, End;
            internal readonly CardState Card;
            internal Entry(int start, int end, CardState card) { Start = start; End = end; Card = card; }
        }

        internal Text Body = null!;
        internal RectTransform Viewport = null!, Layer = null!;
        internal Func<bool> CanHover = null!;
        internal readonly List<Entry> Entries = new List<Entry>();
        internal void AddEntry(int start, int end, CardState card) => Entries.Add(new Entry(start, end, card));
        private readonly Vector3[] corners = new Vector3[4];
        private readonly RectTransform[] edges = new RectTransform[4];
        private GameObject? highlight;
        internal CardState? HighlightedCard { get; private set; }

        internal void Clear()
        {
            HighlightedCard = null;
            if (highlight != null) highlight.SetActive(false);
        }

        private void LateUpdate()
        {
            if (Input.GetMouseButton(0)) Clear();
            else UpdateHover(Input.mousePosition);
        }

        internal CardState? CardAt(Vector2 screenPoint)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(Viewport, screenPoint) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(Body.rectTransform, screenPoint, null, out Vector2 local) ||
                !Body.rectTransform.rect.Contains(local)) return null;
            // Legacy Text's generator uses pixels; the RectTransform uses
            // canvas units. startCharIdx also accounts for wrapped lines.
            float y = local.y * Body.pixelsPerUnit;
            var lines = Body.cachedTextGenerator.lines;
            for (int index = 0; index < lines.Count; index++)
            {
                UILineInfo line = lines[index];
                if (y > line.topY || y < line.topY - line.height) continue;
                int end = index + 1 < lines.Count ? lines[index + 1].startCharIdx : Body.text.Length;
                foreach (Entry entry in Entries)
                    if (entry.Start < end && entry.End > line.startCharIdx) return entry.Card;
                break;
            }
            return null;
        }

        internal void UpdateHover(Vector2 screenPoint)
        {
            CardState? card = CanHover() ? CardAt(screenPoint) : null;
            CardManager? manager = AllGameManagers.Instance?.GetCardManager();
            CardUI? ui = card == null ? null : manager?.GetCardInHand(card);
            // CardUI objects are pooled/reused. Verify the exact native card,
            // not just its former hand slot, before drawing the outline.
            if (ui == null || !ui.gameObject.activeInHierarchy || !ui.IsCardVisible() ||
                !ReferenceEquals(ui.GetCardState(), card)) { Clear(); return; }
            RectTransform frame = ui.GetCardFrameRectTransform();
            if (frame == null) { Clear(); return; }
            EnsureHighlight();
            frame.GetWorldCorners(corners);
            Camera camera = ui.GetCanvas().renderMode == RenderMode.ScreenSpaceOverlay ? null! : ui.GetCanvas().worldCamera;
            for (int index = 0; index < 4; index++)
            {
                Vector2 first = RectTransformUtility.WorldToScreenPoint(camera, corners[index]);
                Vector2 second = RectTransformUtility.WorldToScreenPoint(camera, corners[(index + 1) % 4]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(Layer, first, null, out first);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(Layer, second, null, out second);
                Vector2 delta = second - first;
                edges[index].anchoredPosition = (first + second) * .5f;
                edges[index].sizeDelta = new Vector2(delta.magnitude + 3, 4);
                edges[index].localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
            HighlightedCard = card;
            highlight!.SetActive(true);
        }

        private void EnsureHighlight()
        {
            if (highlight != null) return;
            highlight = new GameObject("Suggested hand card", typeof(RectTransform));
            highlight.transform.SetParent(Layer, false);
            for (int index = 0; index < 4; index++)
            {
                var edge = new GameObject("Edge", typeof(RectTransform), typeof(Image));
                edge.transform.SetParent(highlight.transform, false);
                edges[index] = edge.GetComponent<RectTransform>();
                edge.GetComponent<Image>().color = new Color(1, .8f, .15f, .95f);
                edge.GetComponent<Image>().raycastTarget = false;
            }
            // Edge positions above are relative to the full advisor canvas.
            RectTransform rect = highlight.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void OnDisable() => Clear();
        private void OnDestroy() { if (highlight != null) Destroy(highlight); }
    }
}
