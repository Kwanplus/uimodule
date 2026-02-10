using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// Background UI의 기본 추상 클래스
    /// 타입당 1개 인스턴스를 재사용합니다.
    /// </summary>
    public abstract class BaseBackground : BaseUI
    {
        protected virtual void Awake()
        {
            layer = UILayer.Background;
        }

        protected override void OnInitialize()
        {
            layer = UILayer.Background;
            OnBackgroundInitialize();
        }

        protected override void OnShow()
        {
            if (TryAttachToLayerCanvas(UILayer.Background, out RectTransform rectTransform))
            {
                ApplyStretchRect(rectTransform);
            }

            OnBackgroundShow();
        }

        protected override void OnHide()
        {
            OnBackgroundHide();

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
            OnBackgroundDestroy();
        }

        /// <summary>
        /// Background 초기화 시 호출
        /// </summary>
        protected abstract void OnBackgroundInitialize();

        /// <summary>
        /// Background 표시 시 호출
        /// </summary>
        protected abstract void OnBackgroundShow();

        /// <summary>
        /// Background 숨김 시 호출
        /// </summary>
        protected abstract void OnBackgroundHide();

        /// <summary>
        /// Background 제거 시 호출
        /// </summary>
        protected abstract void OnBackgroundDestroy();
    }
}
