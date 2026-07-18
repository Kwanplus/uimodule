using UIModule;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule.GamepadSample
{
    /// <summary>
    /// 비표준 이름의 UI Action을 UIInputConfiguration으로 연결하는 선택 설정 샘플이다.
    /// </summary>
    public sealed class OptionalInputConfigurationSample : MonoBehaviour
    {
        [SerializeField] private UIInputConfiguration _inputConfiguration;

        /// <summary>
        /// 시작 시 선택 설정을 가진 UIManager를 생성하고 샘플 Screen을 연다.
        /// </summary>
        private void Start()
        {
            if (_inputConfiguration == null)
            {
                Debug.LogError("[Gamepad UI Sample] CustomUI.inputactions의 MoveSelection, ConfirmSelection, DismissPanel을 UIInputConfiguration에 연결한 뒤 할당하세요.");
                return;
            }

            // UIManager가 EventSystem을 만들기 전에 설정해야 하므로 이 Scene에는
            // 다른 GamepadUISampleBootstrap을 함께 배치하지 않는다.
            GameObject managerObject = new GameObject("ConfiguredUIManager");
            managerObject.SetActive(false);
            UIManager manager = managerObject.AddComponent<UIManager>();
            manager.SetInputConfiguration(_inputConfiguration);
            managerObject.SetActive(true);
            manager.SetPoolingEnabled(false);
            manager.ShowScreen<CustomInputSampleScreen>();
        }
    }

    /// <summary>
    /// 비표준 Action과 runtime Custom Cancel을 함께 확인하는 동적 Grid 샘플이다.
    /// </summary>
    public sealed class CustomInputSampleScreen : BaseScreen
    {
        protected override void OnScreenInitialize()
        {
            Button firstButton = null;
            for (int index = 0; index < 9; index++)
            {
                GameObject buttonObject = new GameObject($"Grid Button {index + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(transform, false);
                RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(150f, 52f);
                rectTransform.anchoredPosition = new Vector2((index % 3 - 1) * 170f, (1 - index / 3) * 70f);
                buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.35f, 1f);
                firstButton ??= buttonObject.GetComponent<Button>();
            }

            UIGridNavigation navigation = gameObject.AddComponent<UIGridNavigation>();
            navigation.Configure(3);
            navigation.RebuildNavigation();

            UIFocusScope scope = gameObject.AddComponent<UIFocusScope>();
            scope.Configure(firstButton, UICancelBehavior.Custom);
            // 이 Scene은 Custom Cancel이 Action 설정과 독립적으로 UI에서만 처리됨을 보여준다.
            scope.AddCancelListener(() => Debug.Log("[Gamepad UI Sample] Custom Cancel invoked."));
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
