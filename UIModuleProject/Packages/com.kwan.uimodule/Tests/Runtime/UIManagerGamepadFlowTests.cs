using System.Collections;
using System.Reflection;
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
        /// 초기화 단계에 구성된 정적 Screen의 최초 Selectable을 선택하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator StaticScreen_SelectsFirstSelectable()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);

            StaticFocusTestScreen screen = manager.ShowScreen<StaticFocusTestScreen>();
            yield return null;

            Assert.That(
                manager.EventSystem.currentSelectedGameObject,
                Is.EqualTo(screen.PrimaryButton.gameObject));
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

            PooledFocusTestPopup firstPopup = manager.ShowPopup<PooledFocusTestPopup>();
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

        /// <summary>
        /// Touch 입력도 포인터 선택 유지 정책에 따라 현재 선택을 해제하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator TouchPress_WhenSelectionIsNotKept_ClearsSelection()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            FocusTestScreen screen = manager.ShowScreen<FocusTestScreen>();
            yield return null;

            screen.gameObject
                .AddComponent<UIFocusScope>()
                .Configure(screen.PrimaryButton, UICancelBehavior.Default, false);
            manager.EventSystem.SetSelectedGameObject(screen.PrimaryButton.gameObject);
            Touchscreen touchscreen = InputSystem.AddDevice<Touchscreen>();
            try
            {
                InputSystem.QueueStateEvent(touchscreen, new TouchState
                {
                    touchId = 1,
                    phase = UnityEngine.InputSystem.TouchPhase.Began,
                    position = new Vector2(120f, 80f),
                    pressure = 1f
                });
                InputSystem.Update();
                ApplyPointerSelectionPolicy(manager);

                Assert.That(manager.EventSystem.currentSelectedGameObject, Is.Null);
            }
            finally
            {
                InputSystem.RemoveDevice(touchscreen);
            }
        }

        /// <summary>
        /// 저장된 선택이 파괴되면 같은 Screen의 유효한 대상으로 복구하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator PopupClose_WithDestroyedRememberedSelection_UsesFallback()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            FocusTestScreen screen = manager.ShowScreen<FocusTestScreen>();
            yield return null;

            manager.EventSystem.SetSelectedGameObject(screen.SecondaryButton.gameObject);
            FocusTestPopup popup = manager.ShowPopup<FocusTestPopup>();
            yield return null;
            Object.Destroy(screen.SecondaryButton.gameObject);
            popup.Close();
            yield return null;
            yield return null;

            Assert.That(manager.EventSystem.currentSelectedGameObject, Is.EqualTo(screen.PrimaryButton.gameObject));
        }

        /// <summary>
        /// UIManager를 거치지 않은 Popup 표시도 스택에 등록하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator DirectPopupShow_RegistersPopupStack()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            manager.ShowScreen<FocusTestScreen>();
            GameObject popupObject = new GameObject("DirectPopup", typeof(RectTransform), typeof(FocusTestPopup));
            popupObject.transform.SetParent(manager.GetLayerCanvas(UILayer.Popup).transform, false);
            FocusTestPopup popup = popupObject.GetComponent<FocusTestPopup>();
            LogAssert.Expect(
                LogType.Warning,
                "[UIModule] BasePopup.Show() 직접 호출을 감지했습니다. UIManager.ShowPopup(popup)을 사용해 Popup 표시를 등록하세요.");

            popup.Show();
            yield return null;

            Assert.That(manager.GetPopupCount(), Is.EqualTo(1));
            Assert.That(manager.IsInputCaptured, Is.True);
            popup.Close();
            yield return null;
        }

        /// <summary>
        /// 외부 생성 Popup을 명시 API로 경고 없이 스택에 등록하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator ShowPopupInstance_RegistersPopupStack()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            manager.ShowScreen<FocusTestScreen>();
            GameObject popupObject = new GameObject("ExternalPopup", typeof(RectTransform), typeof(FocusTestPopup));
            popupObject.transform.SetParent(manager.GetLayerCanvas(UILayer.Popup).transform, false);
            FocusTestPopup popup = popupObject.GetComponent<FocusTestPopup>();

            manager.ShowPopup(popup);
            yield return null;

            Assert.That(manager.GetPopupCount(), Is.EqualTo(1));
            Assert.That(manager.IsInputCaptured, Is.True);
            popup.Close();
            yield return null;
        }

        /// <summary>
        /// 현재 Screen이 강제 파괴돼도 Screen 스택과 입력 점유 상태를 정리하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator DestroyedScreen_ClearsStackAndCaptureState()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            FocusTestScreen screen = manager.ShowScreen<FocusTestScreen>();
            yield return null;

            Object.Destroy(screen.gameObject);
            yield return null;

            Assert.That(manager.GetScreenStackCount(), Is.Zero);
            Assert.That(manager.IsInputCaptured, Is.False);
        }

        /// <summary>
        /// Popup을 Pool에 반환한 뒤 스택과 포커스를 정리하고 같은 인스턴스를 재사용하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator PopupPool_ReturnsAndReusesCleanInstance()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(true);

            PooledFocusTestPopup firstPopup = manager.ShowPopup<PooledFocusTestPopup>();
            yield return null;
            Assert.That(manager.GetPopupCount(), Is.EqualTo(1));
            Assert.That(manager.EventSystem.currentSelectedGameObject, Is.EqualTo(firstPopup.PrimaryButton.gameObject));

            firstPopup.Close();
            yield return null;
            Assert.That(manager.GetPopupCount(), Is.Zero);
            Assert.That(manager.IsInputCaptured, Is.False);

            PooledFocusTestPopup reusedPopup = manager.ShowPopup<PooledFocusTestPopup>();
            yield return null;

            Assert.That(reusedPopup, Is.SameAs(firstPopup));
            Assert.That(manager.GetPopupCount(), Is.EqualTo(1));
            Assert.That(manager.EventSystem.currentSelectedGameObject, Is.EqualTo(reusedPopup.PrimaryButton.gameObject));
        }

        /// <summary>
        /// 실제 UI 액션과 Gamepad 연결 변화가 공개 장치 상태에 반영되는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator InputDevices_UpdateLastDeviceAndGamepadConnection()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            manager.ShowScreen<FocusTestScreen>();
            yield return null;

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Touchscreen touchscreen = InputSystem.AddDevice<Touchscreen>();
            bool wasGamepadConnected = manager.InputDeviceState.IsGamepadConnected;
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                UpdateTrackedDevice(manager, keyboard);
                Assert.That(manager.InputDeviceState.LastInputDevice, Is.EqualTo(UIInputDeviceType.Keyboard));

                UpdateTrackedDevice(manager, mouse);
                Assert.That(manager.InputDeviceState.LastInputDevice, Is.EqualTo(UIInputDeviceType.Pointer));

                UpdateTrackedDevice(manager, touchscreen);
                Assert.That(manager.InputDeviceState.LastInputDevice, Is.EqualTo(UIInputDeviceType.Touch));

                UpdateTrackedDevice(manager, gamepad);
                Assert.That(manager.InputDeviceState.LastInputDevice, Is.EqualTo(UIInputDeviceType.Gamepad));
                Assert.That(manager.InputDeviceState.IsGamepadConnected, Is.True);

                InputSystem.RemoveDevice(gamepad);
                yield return null;
                Assert.That(manager.InputDeviceState.IsGamepadConnected, Is.EqualTo(wasGamepadConnected));
                Assert.That(manager.InputDeviceState.LastInputDevice, Is.EqualTo(UIInputDeviceType.Gamepad));
                gamepad = null;
            }
            finally
            {
                if (gamepad != null && gamepad.added)
                {
                    InputSystem.RemoveDevice(gamepad);
                }

                InputSystem.RemoveDevice(touchscreen);
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(keyboard);
            }
        }

        /// <summary>
        /// 화면별 비표준 Input Action 수행이 마지막 UI 입력 장치와 변경 이벤트를 갱신하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator CustomPerformedAction_ReportsInputDevice()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            manager.ShowScreen<FocusTestScreen>();
            yield return null;

            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InputAction customAction = new InputAction("CustomStart", InputActionType.Button);
            customAction.AddBinding("<Gamepad>/buttonSouth");
            customAction.AddBinding("<Keyboard>/enter");
            int changedCount = 0;

            try
            {
                customAction.performed += manager.ReportInputDevice;
                customAction.Enable();
                manager.InputDeviceChanged += HandleInputDeviceChanged;

                InputSystem.QueueStateEvent(gamepad, new GamepadState
                {
                    buttons = 1u << (int)GamepadButton.South
                });
                yield return null;
                yield return null;

                Assert.That(manager.InputDeviceState.LastInputDevice, Is.EqualTo(UIInputDeviceType.Gamepad));
                Assert.That(changedCount, Is.EqualTo(1));

                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                yield return null;
                yield return null;

                Assert.That(manager.InputDeviceState.LastInputDevice, Is.EqualTo(UIInputDeviceType.Keyboard));
                Assert.That(changedCount, Is.EqualTo(2));
            }
            finally
            {
                manager.InputDeviceChanged -= HandleInputDeviceChanged;
                customAction.performed -= manager.ReportInputDevice;
                customAction.Dispose();
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(gamepad);
            }

            void HandleInputDeviceChanged(UIInputDeviceState state)
            {
                changedCount++;
            }
        }

        /// <summary>
        /// 입력 장치 분류 결과를 공개 상태 갱신 경로에 전달한다.
        /// </summary>
        private static void UpdateTrackedDevice(UIManager manager, InputDevice device)
        {
            UIInputDeviceType deviceType = (UIInputDeviceType)typeof(UIManager)
                .GetMethod(
                    "GetDeviceType",
                    BindingFlags.Static | BindingFlags.NonPublic)
                ?.Invoke(null, new object[] { device });
            typeof(UIManager)
                .GetMethod(
                    "UpdateInputDeviceState",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(UIInputDeviceType?) },
                    null)
                ?.Invoke(manager, new object[] { (UIInputDeviceType?)deviceType });
        }

        /// <summary>
        /// 현재 Input System 프레임에서 포인터 선택 정책을 즉시 적용한다.
        /// </summary>
        private static void ApplyPointerSelectionPolicy(UIManager manager)
        {
            object focusController = typeof(UIManager)
                .GetField("_focusController", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(manager);
            focusController
                ?.GetType()
                .GetMethod(
                    "ApplyPointerSelectionPolicy",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(focusController, null);
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
