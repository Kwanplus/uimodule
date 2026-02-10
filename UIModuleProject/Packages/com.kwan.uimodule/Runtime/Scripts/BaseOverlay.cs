using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// Overlay UI의 기본 추상 클래스
    /// 타입당 1개 인스턴스를 재사용합니다.
    /// </summary>
    public abstract class BaseOverlay : BaseUI
    {
        protected virtual void Awake()
        {
            layer = UILayer.Overlay;
        }

        protected override void OnInitialize()
        {
            layer = UILayer.Overlay;
            OnOverlayInitialize();
        }

        protected override void OnShow()
        {
            if (TryAttachToLayerCanvas(UILayer.Overlay, out RectTransform rectTransform))
            {
                ApplyStretchRect(rectTransform);
            }

            OnOverlayShow();
        }

        protected override void OnHide()
        {
            OnOverlayHide();

            if (UIManager.Instance == null || !UIManager.Instance.IsUsingPooling())
            {
                return;
            }

            if (UIPoolManager.Instance != null)
            {
                UIPoolManager.Instance.ReturnToPool(this);
            }
        }

        protected override void OnDestroy()
        {
            OnOverlayDestroy();
        }

        /// <summary>
        /// Overlay 초기화 시 호출
        /// </summary>
        protected abstract void OnOverlayInitialize();

        /// <summary>
        /// Overlay 표시 시 호출
        /// </summary>
        protected abstract void OnOverlayShow();

        /// <summary>
        /// Overlay 숨김 시 호출
        /// </summary>
        protected abstract void OnOverlayHide();

        /// <summary>
        /// Overlay 제거 시 호출
        /// </summary>
        protected abstract void OnOverlayDestroy();
    }
}
