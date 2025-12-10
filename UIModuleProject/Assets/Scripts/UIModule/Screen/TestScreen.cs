using UnityEngine;
using UIModule;

namespace UIModule
{
    /// <summary>
    /// TestScreen Screen
    /// </summary>
    public class TestScreen : BaseScreen
    {
        // UI 요소 참조
        [SerializeField] private UIButton _buttonConfirm;
        [SerializeField] private UIButton _buttonBack;

        protected override void OnScreenInitialize()
        {
            // 버튼 클릭 이벤트 등록
            if (_buttonConfirm != null)
            {
                _buttonConfirm.OnClick += OnButtonConfirmClicked;
            }
            
            if (_buttonBack != null)
            {
                _buttonBack.OnClick += OnButtonBackClicked;
            }

            // 초기화 로직
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
            if (_buttonConfirm != null)
            {
                _buttonConfirm.OnClick -= OnButtonConfirmClicked;
            }
            
            if (_buttonBack != null)
            {
                _buttonBack.OnClick -= OnButtonBackClicked;
            }

            // 제거 시 로직
        }

        /// <summary>
        /// 확인 버튼 클릭 처리
        /// </summary>
        private void OnButtonConfirmClicked()
        {
            // 확인 버튼 로직
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideScreen();
            }
        }

        /// <summary>
        /// 뒤로가기 버튼 클릭 처리
        /// </summary>
        private void OnButtonBackClicked()
        {
            // 뒤로가기 버튼 로직
            if (UIManager.Instance != null)
            {
                UIManager.Instance.BackScreen();
            }
        }
    }
}
