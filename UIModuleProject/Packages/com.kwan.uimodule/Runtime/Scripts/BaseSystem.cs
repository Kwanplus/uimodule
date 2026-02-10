using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// System UI의 기본 추상 클래스
    /// 타입당 1개 인스턴스를 재사용합니다.
    /// </summary>
    public abstract class BaseSystem : BaseUI
    {
        protected virtual void Awake()
        {
            layer = UILayer.System;
        }

        protected override void OnInitialize()
        {
            layer = UILayer.System;
            OnSystemInitialize();
        }

        protected override void OnShow()
        {
            if (TryAttachToLayerCanvas(UILayer.System, out RectTransform rectTransform))
            {
                ApplyStretchRect(rectTransform);
            }

            OnSystemShow();
        }

        protected override void OnHide()
        {
            OnSystemHide();

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
            OnSystemDestroy();
        }

        /// <summary>
        /// System 초기화 시 호출
        /// </summary>
        protected abstract void OnSystemInitialize();

        /// <summary>
        /// System 표시 시 호출
        /// </summary>
        protected abstract void OnSystemShow();

        /// <summary>
        /// System 숨김 시 호출
        /// </summary>
        protected abstract void OnSystemHide();

        /// <summary>
        /// System 제거 시 호출
        /// </summary>
        protected abstract void OnSystemDestroy();
    }
}
