using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UIModule
{
    /// <summary>
    /// 복합 UI에만 선택 대상과 Cancel 정책을 명시적으로 부여한다.
    /// </summary>
    public class UIFocusScope : MonoBehaviour
    {
        [SerializeField] private Selectable _defaultSelection;
        [SerializeField] private UICancelBehavior _cancelBehavior = UICancelBehavior.Default;
        [SerializeField] private bool _keepSelectionOnPointerInput = true;
        [SerializeField] private bool _blocksLowerScopes = true;
        [SerializeField] private UnityEvent _onCancel = new UnityEvent();

        /// <summary>이 범위의 최초 선택 대상을 반환한다.</summary>
        public Selectable DefaultSelection => _defaultSelection;

        /// <summary>Cancel 입력 처리 정책을 반환한다.</summary>
        public UICancelBehavior CancelBehavior => _cancelBehavior;

        /// <summary>포인터 입력 시 기존 선택을 유지할지 반환한다.</summary>
        public bool KeepSelectionOnPointerInput => _keepSelectionOnPointerInput;

        /// <summary>하위 UI의 Selectable을 잠시 차단할지 반환한다.</summary>
        public bool BlocksLowerScopes => _blocksLowerScopes;

        /// <summary>
        /// 런타임 생성 UI에서 선택과 Cancel 정책을 설정한다.
        /// </summary>
        public void Configure(
            Selectable defaultSelection,
            UICancelBehavior cancelBehavior,
            bool keepSelectionOnPointerInput = true,
            bool blocksLowerScopes = true)
        {
            _defaultSelection = defaultSelection;
            _cancelBehavior = cancelBehavior;
            _keepSelectionOnPointerInput = keepSelectionOnPointerInput;
            _blocksLowerScopes = blocksLowerScopes;
            EnsureCancelEvent();
        }

        /// <summary>
        /// 런타임 생성 UI의 Custom Cancel 콜백을 추가한다.
        /// </summary>
        public void AddCancelListener(UnityAction listener)
        {
            if (listener == null)
            {
                return;
            }

            EnsureCancelEvent();
            _onCancel.AddListener(listener);
        }

        /// <summary>
        /// Custom Cancel 이벤트를 호출한다.
        /// </summary>
        public void InvokeCustomCancel()
        {
            EnsureCancelEvent();
            _onCancel.Invoke();
        }

        /// <summary>
        /// 런타임으로 추가된 Scope도 Custom Cancel 이벤트를 안전하게 보유하도록 보장한다.
        /// </summary>
        private void EnsureCancelEvent()
        {
            if (_onCancel == null)
            {
                _onCancel = new UnityEvent();
            }
        }
    }

    /// <summary>
    /// UI 범위에서 Cancel을 처리하는 방법이다.
    /// </summary>
    public enum UICancelBehavior
    {
        Default,
        Ignore,
        Custom
    }
}
