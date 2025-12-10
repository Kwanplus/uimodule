using UnityEngine;
using UIModule;

namespace UIModule
{
    /// <summary>
    /// SamplePopup Popup
    /// </summary>
    public class SamplePopup : BasePopup
    {
        // UI 요소 참조
        [SerializeField] private UIButton _buttonConfirm;
        [SerializeField] private UIButton _buttonClose;

        protected override void OnPopupInitialize()
        {
            // 버튼 클릭 이벤트 등록
            if (_buttonConfirm != null)
            {
                _buttonConfirm.OnClick += OnButtonConfirmClicked;
            }
            
            if (_buttonClose != null)
            {
                _buttonClose.OnClick += OnButtonCloseClicked;
            }

            // 초기화 로직
        }

        protected override void OnPopupShow()
        {
            // 표시 시 로직
        }

        protected override void OnPopupHide()
        {
            // 숨김 시 로직
        }

        protected override void OnPopupDestroy()
        {
            // 버튼 클릭 이벤트 해제
            if (_buttonConfirm != null)
            {
                _buttonConfirm.OnClick -= OnButtonConfirmClicked;
            }
            
            if (_buttonClose != null)
            {
                _buttonClose.OnClick -= OnButtonCloseClicked;
            }

            // 제거 시 로직
        }

        /// <summary>
        /// 확인 버튼 클릭 처리
        /// </summary>
        private void OnButtonConfirmClicked()
        {
            // 확인 버튼 로직
            Close();
        }

        /// <summary>
        /// 닫기 버튼 클릭 처리
        /// </summary>
        private void OnButtonCloseClicked()
        {
            // 닫기 버튼 로직
            Close();
        }
    }
}
