using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// Hide 경로를 거치지 않고 UI가 파괴될 때 포커스와 모달 상태를 정리한다.
    /// </summary>
    internal sealed class UIFocusLifetimeRelay : MonoBehaviour
    {
        private UIFocusController _controller;
        private BaseUI _ui;

        /// <summary>
        /// 정리할 UI와 controller를 연결한다.
        /// </summary>
        internal void Initialize(UIFocusController controller, BaseUI ui)
        {
            _controller = controller;
            _ui = ui;
        }

        private void OnDestroy()
        {
            if (_ui != null)
            {
                _controller?.HandleDestroyed(_ui);
            }
        }
    }
}
