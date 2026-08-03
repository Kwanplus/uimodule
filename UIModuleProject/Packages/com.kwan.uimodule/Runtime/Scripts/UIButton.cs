using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIModule
{
    /// <summary>
    /// Unity Button 오브젝트에 붙는 UI 버튼 컴포넌트.
    /// 클릭 이벤트와 선택/호버 스케일 피드백을 처리한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler,
        ISubmitHandler
    {
        // Hover/Select 시 적용할 배율. 인스펙터 옵션이 늘지 않도록 상수로 고정한다.
        private const float HighlightedScale = 1.05f;

        public static event Action OnAnyClicked;

        /// <summary>
        /// 키보드·게임패드 방향 Navigation으로 UIButton이 선택됐을 때 발생하는 전역 이벤트
        /// </summary>
        public static event Action OnAnyNavigationSelected;

        [Header("버튼 설정")]
        [SerializeField] private Button _button;
        [SerializeField] private bool enableSelectScale = true;

        // Awake 시점의 기준 스케일. 피드백 종료 시 이 값으로 되돌린다.
        private Vector3 _baseScale = Vector3.one;
        private bool _isPointerInside;
        private bool _isSelected;

        /// <summary>
        /// 버튼 클릭 이벤트
        /// </summary>
        public event Action OnClick;

        /// <summary>
        /// EventSystem이 이 버튼을 선택했을 때 발생하는 이벤트
        /// </summary>
        public event Action<BaseEventData> OnSelected;

        /// <summary>
        /// EventSystem이 이 버튼의 선택을 해제했을 때 발생하는 이벤트
        /// </summary>
        public event Action<BaseEventData> OnDeselected;

        /// <summary>
        /// EventSystem이 이 버튼에 Submit을 전달했을 때 발생하는 이벤트
        /// </summary>
        public event Action<BaseEventData> OnSubmitted;

        /// <summary>
        /// 버튼 컴포넌트 참조
        /// </summary>
        public Button Button => _button;

        /// <summary>
        /// EventSystem 기준으로 현재 버튼이 선택된 상태인지 여부
        /// </summary>
        public bool IsSelected => _isSelected;

        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            _baseScale = transform.localScale;
            if (_baseScale == Vector3.zero)
                _baseScale = Vector3.one;

            if (_button != null)
                _button.onClick.AddListener(HandleButtonClick);
        }

        private void OnDisable()
        {
            _isPointerInside = false;
            _isSelected = false;
            transform.localScale = _baseScale;
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleButtonClick);
        }

        /// <summary>
        /// 마우스 포인터가 버튼 위에 들어왔을 때 스케일 피드백을 적용한다.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanApplySelectScale())
                return;

            _isPointerInside = true;
            RefreshScale();
        }

        /// <summary>
        /// 마우스 포인터가 버튼을 벗어났을 때, 선택 상태가 아니면 스케일을 되돌린다.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerInside = false;
            RefreshScale();
        }

        /// <summary>
        /// 게임패드/키보드 등으로 선택되었을 때 상태를 갱신하고 선택 이벤트를 통지한다.
        /// </summary>
        public void OnSelect(BaseEventData eventData)
        {
            _isSelected = true;
            RefreshScale();
            OnSelected?.Invoke(eventData);

            // 방향 Navigation만 전역 통지한다. 포인터·프로그램 선택·Submit은 제외한다.
            if (eventData is AxisEventData)
                OnAnyNavigationSelected?.Invoke();
        }

        /// <summary>
        /// 선택이 해제되었을 때 상태를 갱신하고 선택 해제 이벤트를 통지한다.
        /// </summary>
        public void OnDeselect(BaseEventData eventData)
        {
            _isSelected = false;
            RefreshScale();
            OnDeselected?.Invoke(eventData);
        }

        /// <summary>
        /// EventSystem Submit을 별도 통지한다.
        /// </summary>
        public void OnSubmit(BaseEventData eventData)
        {
            if (!isActiveAndEnabled || !IsInteractable())
                return;

            OnSubmitted?.Invoke(eventData);
        }

        /// <summary>
        /// 버튼 클릭 처리
        /// </summary>
        private void HandleButtonClick()
        {
            OnAnyClicked?.Invoke();
            OnClick?.Invoke();
        }

        /// <summary>
        /// 버튼 활성화/비활성화
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (_button != null)
                _button.interactable = interactable;

            if (!interactable)
            {
                _isPointerInside = false;
                _isSelected = false;
            }

            RefreshScale();
        }

        /// <summary>
        /// 버튼이 상호작용 가능한지 여부
        /// </summary>
        public bool IsInteractable()
        {
            return _button != null && _button.interactable;
        }

        /// <summary>
        /// 스케일 피드백을 적용할 수 있는 상태인지 반환한다.
        /// </summary>
        private bool CanApplySelectScale()
        {
            return enableSelectScale && IsInteractable();
        }

        /// <summary>
        /// hover 또는 select 상태에 맞춰 즉시 목표 스케일로 맞춘다.
        /// </summary>
        private void RefreshScale()
        {
            bool highlighted = CanApplySelectScale() && (_isPointerInside || _isSelected);
            transform.localScale = highlighted ? _baseScale * HighlightedScale : _baseScale;
        }
    }
}
