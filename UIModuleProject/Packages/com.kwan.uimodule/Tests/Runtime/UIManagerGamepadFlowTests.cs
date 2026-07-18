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

        /// <summary>
        /// 중첩 Popup이 Hide를 거치지 않고 파괴돼도 모달 차단을 깊이별로 복구하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator DestroyedNestedPopup_RestoresLowerModalState()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            FocusTestScreen screen = manager.ShowScreen<FocusTestScreen>();
            yield return null;

            FocusTestPopup firstPopup = manager.ShowPopup<FocusTestPopup>();
            yield return null;
            FocusTestPopup secondPopup = manager.ShowPopup<FocusTestPopup>();
            yield return null;

            Assert.That(screen.PrimaryButton.interactable, Is.False);
            Assert.That(firstPopup.PrimaryButton.interactable, Is.False);

            Object.Destroy(secondPopup.gameObject);
            yield return null;
            yield return null;

            Assert.That(firstPopup.PrimaryButton.interactable, Is.True);
            Assert.That(screen.PrimaryButton.interactable, Is.False);

            Object.Destroy(firstPopup.gameObject);
            yield return null;

            Assert.That(screen.PrimaryButton.interactable, Is.True);
        }

        /// <summary>
        /// 복귀 시 Scope 기본 대상보다 마지막 유효 선택을 우선 복원하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator PopupClose_RestoresRememberedSelectionOverDefaultSelection()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            FocusTestScreen screen = manager.ShowScreen<FocusTestScreen>();
            yield return null;

            UIFocusScope scope = screen.gameObject.AddComponent<UIFocusScope>();
            scope.Configure(screen.PrimaryButton, UICancelBehavior.Default);
            manager.EventSystem.SetSelectedGameObject(screen.SecondaryButton.gameObject);

            FocusTestPopup popup = manager.ShowPopup<FocusTestPopup>();
            yield return null;
            popup.Close();
            yield return null;
            yield return null;

            Assert.That(manager.EventSystem.currentSelectedGameObject, Is.EqualTo(screen.SecondaryButton.gameObject));
        }

        /// <summary>
        /// Popup이 열려도 상위 Overlay의 상호작용 상태를 바꾸지 않는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator Popup_DoesNotDisableHigherPriorityOverlay()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            manager.ShowScreen<FocusTestScreen>();
            FocusTestOverlay overlay = manager.ShowOverlay<FocusTestOverlay>();
            yield return null;

            manager.ShowPopup<FocusTestPopup>();
            yield return null;

            Assert.That(overlay.PrimaryButton.interactable, Is.True);
        }

        /// <summary>
        /// 런타임 생성 Scope도 Custom Cancel listener를 안전하게 등록하고 호출하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator RuntimeFocusScope_CustomCancelListener_IsInvoked()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            FocusTestScreen screen = manager.ShowScreen<FocusTestScreen>();
            yield return null;

            int invokeCount = 0;
            UIFocusScope scope = screen.gameObject.AddComponent<UIFocusScope>();
            scope.Configure(screen.PrimaryButton, UICancelBehavior.Custom);
            scope.AddCancelListener(() => invokeCount++);

            Assert.That(manager.TryRouteCancel(), Is.True);
            Assert.That(invokeCount, Is.EqualTo(1));
        }
    }

    /// <summary>
    /// 동적으로 Selectable을 만드는 Screen 테스트 대역이다.
    /// </summary>
    public class FocusTestScreen : BaseScreen
    {
        /// <summary>테스트에서 선택을 확인할 첫 Button이다.</summary>
        public Button PrimaryButton { get; private set; }

        /// <summary>두 번째 선택 복원 검증을 위한 Button이다.</summary>
        public Button SecondaryButton { get; private set; }

        /// <summary>가상 Gamepad Submit 검증을 위한 클릭 횟수다.</summary>
        public int SubmitCount { get; private set; }

        protected override void OnScreenInitialize()
        {
        }

        protected override void OnScreenBegin()
        {
            PrimaryButton = CreateButton("ScreenButton");
            PrimaryButton.onClick.AddListener(() => SubmitCount++);
            SecondaryButton = CreateButton("ScreenSecondButton");
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

    /// <summary>
    /// Popup보다 높은 레이어의 상호작용 보존을 검증하는 Overlay 대역이다.
    /// </summary>
    public class FocusTestOverlay : BaseOverlay
    {
        /// <summary>테스트에서 상호작용 상태를 확인할 Button이다.</summary>
        public Button PrimaryButton { get; private set; }

        protected override void OnOverlayInitialize()
        {
            GameObject buttonObject = new GameObject("OverlayButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            PrimaryButton = buttonObject.GetComponent<Button>();
        }

        protected override void OnOverlayShow()
        {
        }

        protected override void OnOverlayHide()
        {
        }

        protected override void OnOverlayDestroy()
        {
        }
    }
}
