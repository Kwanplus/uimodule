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
            if (IsInitialized) return;
            
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
            
            if (!IsInitialized)
            {
                Initialize();
            }
            
            // OnShow를 먼저 호출하여 레이어 이동 및 RectTransform 설정을 완료한 후 활성화
            OnShow();
            gameObject.SetActive(true);
            IsActive = true;
        }
        
        /// <summary>
        /// UI 숨김
        /// </summary>
        public virtual void Hide()
        {
            // 디버깅: Hide() 호출 시 스택 추적
            Debug.Log($"[{GetType().Name}] Hide() 호출됨. IsActive={IsActive}\n{System.Environment.StackTrace}");
            
            if (!IsActive) return;
            
            IsActive = false;
            OnHide();
            
            // Pooling 사용 시 OnHide()에서 풀로 반환되며 비활성화됨
            // Pooling 미사용 시에만 여기서 비활성화
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
        
        /// <summary>
        /// 하위 클래스에서 구현해야 하는 추상 메서드들
        /// </summary>
        protected abstract void OnInitialize();
        protected abstract void OnShow();
        protected abstract void OnHide();
        protected abstract void OnDestroy();
    }
}

