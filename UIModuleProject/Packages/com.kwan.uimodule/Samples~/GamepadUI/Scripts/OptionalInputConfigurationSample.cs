using UIModule;
using UnityEngine;

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
            manager.ShowScreen<GamepadUISampleScreen>();
        }
    }
}
