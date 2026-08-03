using UnityEngine;
using UnityEngine.UI;

namespace UIModule
{
    /// <summary>
    /// 마지막 UI 입력 장치에 따라 공용 Xbox 버튼 프롬프트를 표시한다.
    /// </summary>
    public class UIInputPromptView : MonoBehaviour
    {
        // 프롬프트 전체를 숨겨 아이콘과 보조 텍스트를 함께 전환한다.
        [SerializeField] private GameObject _promptContainer;
        [SerializeField] private Image _iconImage;
        [SerializeField] private XboxButtonType _buttonType;

        private UIManager _subscribedManager;
        private bool _hasReportedSelfContainer;

        private void OnEnable()
        {
            SubscribeInputDeviceChanged();
            RefreshPrompt();
        }

        private void OnDisable()
        {
            UnsubscribeInputDeviceChanged();
        }

        private void OnDestroy()
        {
            UnsubscribeInputDeviceChanged();
        }

        /// <summary>
        /// 입력 장치 상태 변화에 맞춰 프롬프트 표시를 갱신한다.
        /// </summary>
        /// <param name="state">갱신된 UI 입력 장치 상태다.</param>
        private void HandleInputDeviceChanged(UIInputDeviceState state)
        {
            ApplyPrompt(state);
        }

        /// <summary>
        /// 현재 UIManager 입력 장치 상태를 즉시 반영한다.
        /// </summary>
        private void RefreshPrompt()
        {
            if (_subscribedManager == null)
            {
                SetPromptVisible(false);
                return;
            }

            ApplyPrompt(_subscribedManager.InputDeviceState);
        }

        /// <summary>
        /// Gamepad 상태와 공용 Sprite 설정이 모두 유효할 때만 프롬프트를 표시한다.
        /// </summary>
        /// <param name="state">표시에 사용할 UI 입력 장치 상태다.</param>
        private void ApplyPrompt(UIInputDeviceState state)
        {
            if (state.LastInputDevice != UIInputDeviceType.Gamepad
                || _promptContainer == null
                || _iconImage == null)
            {
                SetPromptVisible(false);
                return;
            }

            UIInputPromptConfiguration configuration = UIModuleSettings.Instance?.InputPromptConfiguration;
            Sprite sprite = configuration?.GetSprite(_buttonType);
            if (sprite == null)
            {
                SetPromptVisible(false);
                return;
            }

            _iconImage.sprite = sprite;
            SetPromptVisible(true);
        }

        /// <summary>
        /// UIManager 장치 상태 이벤트를 중복 없이 구독한다.
        /// </summary>
        private void SubscribeInputDeviceChanged()
        {
            UnsubscribeInputDeviceChanged();
            _subscribedManager = UIManager.Instance;
            _subscribedManager.InputDeviceChanged += HandleInputDeviceChanged;
        }

        /// <summary>
        /// UIManager 장치 상태 이벤트 구독을 해제한다.
        /// </summary>
        private void UnsubscribeInputDeviceChanged()
        {
            if (_subscribedManager != null)
            {
                _subscribedManager.InputDeviceChanged -= HandleInputDeviceChanged;
            }

            _subscribedManager = null;
        }

        /// <summary>
        /// 프롬프트 컨테이너 표시 상태를 변경한다.
        /// </summary>
        /// <param name="visible">표시할지 여부다.</param>
        private void SetPromptVisible(bool visible)
        {
            if (_promptContainer == null)
            {
                return;
            }

            if (_promptContainer == gameObject)
            {
                ReportSelfContainerConfiguration();
                if (_iconImage != null)
                {
                    _iconImage.enabled = visible;
                }

                return;
            }

            _promptContainer.SetActive(visible);
        }

        /// <summary>
        /// View 자신을 컨테이너로 연결한 구성을 한 번만 진단한다.
        /// </summary>
        private void ReportSelfContainerConfiguration()
        {
            if (_hasReportedSelfContainer)
            {
                return;
            }

            _hasReportedSelfContainer = true;
            Debug.LogError(
                "[UIModule] UIInputPromptView의 Prompt Container는 View 자신이 아닌 별도의 하위 오브젝트여야 합니다. " +
                "자기 자신을 연결한 경우 아이콘만 표시·숨김 처리합니다.",
                this);
        }
    }
}
