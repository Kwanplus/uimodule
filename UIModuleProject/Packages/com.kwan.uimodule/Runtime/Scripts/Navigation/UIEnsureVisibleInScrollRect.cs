using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIModule
{
    /// <summary>
    /// 선택된 UI 항목이 ScrollRect의 Viewport 안에 보이도록 Content를 이동한다.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class UIEnsureVisibleInScrollRect : MonoBehaviour, ISelectHandler
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private float _padding = 8f;

        /// <summary>
        /// 선택될 때 해당 항목을 Viewport에 노출한다.
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {
            EnsureVisible();
        }

        /// <summary>
        /// 현재 항목이 Viewport 밖에 있으면 Content를 필요한 만큼 이동한다.
        /// </summary>
        public void EnsureVisible()
        {
            if (_scrollRect == null || _scrollRect.viewport == null || _scrollRect.content == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            RectTransform item = transform as RectTransform;
            RectTransform viewport = _scrollRect.viewport;
            if (item == null)
            {
                return;
            }

            Bounds itemBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
            Rect viewportRect = viewport.rect;
            Vector2 offset = Vector2.zero;

            if (_scrollRect.vertical)
            {
                if (itemBounds.max.y > viewportRect.yMax - _padding)
                {
                    offset.y = itemBounds.max.y - (viewportRect.yMax - _padding);
                }
                else if (itemBounds.min.y < viewportRect.yMin + _padding)
                {
                    offset.y = itemBounds.min.y - (viewportRect.yMin + _padding);
                }
            }

            if (_scrollRect.horizontal)
            {
                if (itemBounds.max.x > viewportRect.xMax - _padding)
                {
                    offset.x = itemBounds.max.x - (viewportRect.xMax - _padding);
                }
                else if (itemBounds.min.x < viewportRect.xMin + _padding)
                {
                    offset.x = itemBounds.min.x - (viewportRect.xMin + _padding);
                }
            }

            if (offset != Vector2.zero)
            {
                _scrollRect.content.anchoredPosition -= offset;
            }
        }
    }
}
