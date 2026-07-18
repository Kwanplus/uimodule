using UnityEngine;
using UnityEngine.UI;

namespace UIModule.Tests
{
    /// <summary>
    /// 초기화 단계에서 Selectable을 만드는 정적 Screen 테스트 대역이다.
    /// </summary>
    public sealed class StaticFocusTestScreen : BaseScreen
    {
        /// <summary>최초 선택을 확인할 Button이다.</summary>
        public Button PrimaryButton { get; private set; }

        protected override void OnScreenInitialize()
        {
            GameObject buttonObject = new GameObject(
                "StaticScreenButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            PrimaryButton = buttonObject.GetComponent<Button>();
        }

        protected override void OnScreenBegin()
        {
        }

        protected override void OnScreenHide()
        {
        }

        protected override void OnScreenDestroy()
        {
        }
    }
}
