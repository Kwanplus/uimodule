using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// 모든 UI의 기본 추상 클래스
    /// </summary>
    public abstract class BaseUI : MonoBehaviour
    {
        [Header("UI 기본 설정")]
        [SerializeField] protected UILayer layer;
        
        /// <summary>
        /// 이 UI가 속한 레이어
        /// </summary>
        public UILayer Layer => layer;
        
        /// <summary>
        /// UI가 현재 활성화되어 있는지 여부
        /// </summary>
        public bool IsActive { get; protected set; }
        
        /// <summary>
        /// UI가 초기화되었는지 여부
        /// </summary>
        public bool IsInitialized { get; protected set; }
        
        /// <summary>
        /// UI 초기화 (생성 시 한 번 호출)
        /// </summary>
        public virtual void Initialize()
        {
            if (IsInitialized) return; // 중복 초기화 방지
            
            IsActive = false;
            IsInitialized = true;
            OnInitialize();
        }
        
        /// <summary>
        /// UI 표시
        /// </summary>
        public virtual void Show()
        {
            if (IsActive) return;
            
            // 초기화가 안 되어있으면 먼저 초기화
            if (!IsInitialized)
            {
                Initialize();
            }
            
            gameObject.SetActive(true);
            IsActive = true;
            OnShow();
        }
        
        /// <summary>
        /// UI 숨김
        /// </summary>
        public virtual void Hide()
        {
            if (!IsActive) return;
            
            IsActive = false;
            OnHide();
            
            // Pooling으로 관리되지 않는 경우에만 비활성화
            // Pooling으로 관리되는 경우 OnHide()에서 풀로 반환하면서 비활성화됨
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// UI 제거 (메모리에서 완전히 제거)
        /// </summary>
        public virtual void Destroy()
        {
            OnDestroy();
            Destroy(gameObject);
        }
        
        // 추상 메서드 - 하위 클래스에서 반드시 구현해야 함
        protected abstract void OnInitialize();
        protected abstract void OnShow();
        protected abstract void OnHide();
        protected abstract void OnDestroy();
    }
}

