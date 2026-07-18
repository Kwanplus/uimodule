using UnityEngine;
using UnityEngine.UI;

namespace UIModule.Tests
{
    /// <summary>
    /// 실제 Resources Prefab과 UI Pool 재사용을 검증하는 Popup 테스트 대역이다.
    /// </summary>
    public sealed class PooledFocusTestPopup : BasePopup
    {
        /// <summary>재사용 뒤 포커스를 확인할 Button이다.</summary>
        public Button PrimaryButton { get; private set; }

        protected override void OnPopupInitialize()
        {
            GameObject buttonObject = new GameObject(
                "PooledPopupButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            PrimaryButton = buttonObject.GetComponent<Button>();
        }

        protected override void OnPopupShow()
        {
        }

        protected override void OnPopupHide()
        {
        }

        protected override void OnPopupDestroy()
        {
        }
    }
}
