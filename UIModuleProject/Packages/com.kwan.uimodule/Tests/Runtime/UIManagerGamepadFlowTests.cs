using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.LowLevel;

namespace UIModule.Tests
{
    /// <summary>
    /// UIManager의 무설정 포커스와 Popup 복원 흐름을 검증한다.
    /// </summary>
    public class UIManagerGamepadFlowTests
    {
        [TearDown]
        public void TearDown()
        {
            UIManager manager = Object.FindFirstObjectByType<UIManager>();
            if (manager != null)
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        /// <summary>
        /// 동적 Screen의 최초 선택과 Popup 닫기 뒤 선택 복원을 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator ScreenAndPopup_RestoreSelectionWithoutConfiguration()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);

            FocusTestScreen screen = manager.ShowScreen<FocusTestScreen>();
            yield return null;

            Assert.That(manager.EventSystem, Is.Not.Null);
            InputSystemUIInputModule inputModule = manager.EventSystem.currentInputModule as InputSystemUIInputModule;
            Assert.That(inputModule, Is.Not.Null);
            Assert.That(inputModule.move.action, Is.Not.Null);
            Assert.That(inputModule.submit.action, Is.Not.Null);
            Assert.That(inputModule.cancel.action, Is.Not.Null);
            Assert.That(manager.EventSystem.currentSelectedGameObject, Is.EqualTo(screen.PrimaryButton.gameObject));

            FocusTestPopup popup = manager.ShowPopup<FocusTestPopup>();
            yield return null;

            Assert.That(manager.EventSystem.currentSelectedGameObject, Is.EqualTo(popup.PrimaryButton.gameObject));
            Assert.That(manager.IsInputCaptured, Is.True);
            Assert.That(manager.InputCaptureState.Reason, Is.EqualTo(UIInputCaptureReason.Popup));

            Assert.That(manager.TryRouteCancel(), Is.True);
            yield return null;

            Assert.That(manager.EventSystem.currentSelectedGameObject, Is.EqualTo(screen.PrimaryButton.gameObject));
            Assert.That(manager.InputCaptureState.Reason, Is.EqualTo(UIInputCaptureReason.Screen));
        }

        /// <summary>
        /// 내장 기본 액션이 가상 Gamepad Submit을 선택된 Button으로 전달하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator DefaultInputModule_SubmitsSelectedButtonFromGamepad()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            FocusTestScreen screen = manager.ShowScreen<FocusTestScreen>();
            yield return null;

            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                InputSystem.QueueStateEvent(gamepad, new GamepadState
                {
                    buttons = 1u << (int)GamepadButton.South
                });
                yield return null;
                yield return null;

                Assert.That(screen.SubmitCount, Is.EqualTo(1));
            }
            finally
            {
                InputSystem.RemoveDevice(gamepad);
            }
        }
    }

    /// <summary>
    /// 동적으로 Selectable을 만드는 Screen 테스트 대역이다.
    /// </summary>
    public class FocusTestScreen : BaseScreen
    {
        /// <summary>테스트에서 선택을 확인할 첫 Button이다.</summary>
        public Button PrimaryButton { get; private set; }

        /// <summary>가상 Gamepad Submit 검증을 위한 클릭 횟수다.</summary>
        public int SubmitCount { get; private set; }

        protected override void OnScreenInitialize()
        {
        }

        protected override void OnScreenBegin()
        {
            PrimaryButton = CreateButton("ScreenButton");
            PrimaryButton.onClick.AddListener(() => SubmitCount++);
        }

        protected override void OnScreenHide()
        {
        }

        protected override void OnScreenDestroy()
        {
        }

        /// <summary>
        /// 테스트용 Button을 생성한다.
        /// </summary>
        private Button CreateButton(string buttonName)
        {
            GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            return buttonObject.GetComponent<Button>();
        }
    }

    /// <summary>
    /// Popup 포커스 복원을 검증하는 테스트 대역이다.
    /// </summary>
    public class FocusTestPopup : BasePopup
    {
        /// <summary>테스트에서 선택을 확인할 첫 Button이다.</summary>
        public Button PrimaryButton { get; private set; }

        protected override void OnPopupInitialize()
        {
            PrimaryButton = CreateButton("PopupButton");
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

        /// <summary>
        /// 테스트용 Button을 생성한다.
        /// </summary>
        private Button CreateButton(string buttonName)
        {
            GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            return buttonObject.GetComponent<Button>();
        }
    }
}
