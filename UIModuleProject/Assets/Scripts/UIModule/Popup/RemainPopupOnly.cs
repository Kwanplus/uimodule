using UnityEngine;
using UIModule;

namespace UIModule
{
    /// <summary>
    /// RemainPopupOnly Popup
    /// </summary>
    public class RemainPopupOnly : BasePopup
    {
        // UI 요소 참조
        [SerializeField] private UIButton _buttonClose;

        protected override void OnPopupInitialize()
        {
            // 초기화 로직
            // 버튼 클릭 이벤트 등록
            if (_buttonClose != null)
            {
                _buttonClose.OnClick += OnButtonCloseClicked;
            }            
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
            // 제거 시 로직
            // 버튼 클릭 이벤트 해제
            if (_buttonClose != null)
            {
                _buttonClose.OnClick -= OnButtonCloseClicked;
            }            
        }

        /// <summary>
        /// ButtonClose 클릭 처리 - 팝업 닫기
        /// </summary>
        private void OnButtonCloseClicked()
        {
            Close();
        }        
    }
}
