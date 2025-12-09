using UnityEngine;
using UIModule;

namespace UIModule
{
    /// <summary>
    /// TitleScreen Screen
    /// </summary>
    public class TitleScreen : BaseScreen
    {
        // UI 요소 참조
        [SerializeField] private UIButton _buttonNext;
        [SerializeField] private UIButton _buttonBack;
        [SerializeField] private UIButton _buttonPopup;

        protected override void OnScreenInitialize()
        {
            // 버튼 클릭 이벤트 등록
            if (_buttonNext != null)
            {
                _buttonNext.OnClick += OnButtonNextClicked;
            }
            
            if (_buttonBack != null)
            {
                _buttonBack.OnClick += OnButtonBackClicked;
            }

            if (_buttonPopup != null)
            {
                _buttonPopup.OnClick += OnButtonPopupClicked;
            }
        }

        protected override void OnScreenShow()
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
            if (_buttonNext != null)
            {
                _buttonNext.OnClick -= OnButtonNextClicked;
            }
            
            if (_buttonBack != null)
            {
                _buttonBack.OnClick -= OnButtonBackClicked;
            }
            
            if (_buttonPopup != null)
            {
                _buttonPopup.OnClick -= OnButtonPopupClicked;
            }
        }
        
        /// <summary>
        /// ButtonNext 클릭 처리 - LobbyScreen으로 이동
        /// </summary>
        private void OnButtonNextClicked()
        {
            Debug.Log("ButtonNext 클릭됨 - LobbyScreen으로 이동");
            UIManager.Instance.ShowScreen<LobbyScreen>();
        }
        
        /// <summary>
        /// ButtonBack 클릭 처리
        /// </summary>
        private void OnButtonBackClicked()
        {
            Debug.Log("ButtonBack 클릭됨");
        }

        /// <summary>
        /// ButtonPopup 클릭 처리
        /// </summary>
        private void OnButtonPopupClicked()
        {
            Debug.Log("ButtonPopup 클릭됨");
            // MessageBox 팝업 표시
            UIManager.Instance.ShowPopup<MessageBox>();
        }
    }
}
