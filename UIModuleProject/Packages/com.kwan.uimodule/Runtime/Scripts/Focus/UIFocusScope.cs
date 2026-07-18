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
        [SerializeField] private UnityEvent _onCancel;

        /// <summary>이 범위의 최초 선택 대상을 반환한다.</summary>
        public Selectable DefaultSelection => _defaultSelection;

        /// <summary>Cancel 입력 처리 정책을 반환한다.</summary>
        public UICancelBehavior CancelBehavior => _cancelBehavior;

        /// <summary>포인터 입력 시 기존 선택을 유지할지 반환한다.</summary>
        public bool KeepSelectionOnPointerInput => _keepSelectionOnPointerInput;

        /// <summary>하위 UI의 Selectable을 잠시 차단할지 반환한다.</summary>
        public bool BlocksLowerScopes => _blocksLowerScopes;

        /// <summary>
        /// Custom Cancel 이벤트를 호출한다.
        /// </summary>
        public void InvokeCustomCancel()
        {
            _onCancel?.Invoke();
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
