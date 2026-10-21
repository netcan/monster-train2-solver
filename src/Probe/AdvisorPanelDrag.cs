using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class AdvisorPanelDrag : MonoBehaviour, IDragHandler
    {
        internal RectTransform Target = null!;

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null) return;
            Canvas canvas = Target.GetComponentInParent<Canvas>();
            Rect bounds = ((RectTransform)Target.parent).rect;
            Vector2 next = Target.anchoredPosition + eventData.delta / Mathf.Max(.01f, canvas.scaleFactor);
            Target.anchoredPosition = new Vector2(
                Mathf.Clamp(next.x, 0, Mathf.Max(0, bounds.width - Target.rect.width)),
                Mathf.Clamp(next.y, -Mathf.Max(0, bounds.height - Target.rect.height), 0));
        }
    }
}
