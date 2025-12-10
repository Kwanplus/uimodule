using System;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule
{
    /// <summary>
    /// Unity Button 오브젝트에 붙는 UI 버튼 컴포넌트
    /// 버튼 액션을 처리할 수 있음
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButton : MonoBehaviour
    {
        [Header("버튼 설정")]
        [SerializeField] private Button _button;
        
        /// <summary>
        /// 버튼 클릭 이벤트
        /// </summary>
        public event Action OnClick;
        
        /// <summary>
        /// 버튼 컴포넌트 참조
        /// </summary>
        public Button Button => _button;
        
        private void Awake()
        {
            // Button 컴포넌트가 없으면 자동으로 가져오기
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
            
            // 버튼 클릭 이벤트 등록
            if (_button != null)
            {
                _button.onClick.AddListener(HandleButtonClick);
            }
        }
        
        private void OnDestroy()
        {
            // 버튼 클릭 이벤트 해제
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleButtonClick);
            }
        }
        
        /// <summary>
        /// 버튼 클릭 처리
        /// </summary>
        private void HandleButtonClick()
        {
            OnClick?.Invoke();
        }
        
        /// <summary>
        /// 버튼 활성화/비활성화
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }
        
        /// <summary>
        /// 버튼이 상호작용 가능한지 여부
        /// </summary>
        public bool IsInteractable()
        {
            return _button != null && _button.interactable;
        }
    }
}

