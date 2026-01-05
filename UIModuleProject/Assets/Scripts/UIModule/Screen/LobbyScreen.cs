using UnityEngine;
using UIModule;

namespace UIModule
{
    /// <summary>
    /// LobbyScreen Screen
    /// </summary>
    public class LobbyScreen : BaseScreen
    {
        // UI 요소 참조
        [SerializeField] private UIButton _buttonBack;

        protected override void OnScreenInitialize()
        {
            // 버튼 클릭 이벤트 등록
            if (_buttonBack != null)
            {
                _buttonBack.OnClick += OnButtonBackClicked;
            }
        }

        protected override void OnScreenBegin()
        {
            // 표시 시 로직
        }

        protected override void OnScreenHide()
        {
            // 숨김 시 로직
        }

        protected override void OnScreenDestroy()
        {
            // 버튼 클릭 이벤트 해제
            if (_buttonBack != null)
            {
                _buttonBack.OnClick -= OnButtonBackClicked;
            }
        }
        
        /// <summary>
        /// ButtonBack 클릭 처리 - 이전 Screen으로 돌아가기
        /// </summary>
        private void OnButtonBackClicked()
        {
            Debug.Log("ButtonBack 클릭됨 - 이전 Screen으로 돌아가기");
            UIManager.Instance.BackScreen();
        }
    }
}
