using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// Popup UI의 기본 추상 클래스
    /// 여러 개가 중첩될 수 있으며, Back 키로 닫을 수 있음
    /// </summary>
    public abstract class BasePopup : BaseUI
    {
        /// <summary>
        /// Back 키로 닫을 수 있는지 여부
        /// </summary>
        public bool CanCloseByBackKey { get; protected set; } = true;
        
        /// <summary>
        /// 스크린 이동 시 팝업이 닫히는지 여부 (true: 닫힘, false: 남아있음)
        /// </summary>
        [SerializeField] protected bool _closeOnScreenChange = true;
        public bool CloseOnScreenChange => _closeOnScreenChange;
        
        /// <summary>
        /// 같은 종류의 팝업이 하나만 존재할 수 있는지 여부 (true: 싱글톤, false: 복수 가능)
        /// </summary>
        [SerializeField] protected bool _isSingleton = false;
        public bool IsSingleton => _isSingleton;
        
        protected virtual void Awake()
        {
            layer = UILayer.Popup;
        }
        
        protected override void OnInitialize()
        {
            layer = UILayer.Popup;
            OnPopupInitialize();
        }
        
        protected override void OnShow()
        {
            // PoolRoot에 있으면 레이어 Canvas로 이동
            if (UIManager.Instance != null)
            {
                Canvas layerCanvas = UIManager.Instance.GetLayerCanvas(UILayer.Popup);
                if (layerCanvas != null)
                {
                    // 현재 부모가 레이어 Canvas가 아니면 이동 (PoolRoot 또는 그 하위에 있을 수 있음)
                    if (transform.parent != layerCanvas.transform)
                    {
                        transform.SetParent(layerCanvas.transform, false);
                        
                        // RectTransform 설정 (Popup은 MiddleCenter)
                        RectTransform rectTransform = GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            // MiddleCenter로 설정 (프리팹에 설정이 없을 경우에만)
                            if (rectTransform.anchorMin == Vector2.zero && rectTransform.anchorMax == Vector2.one)
                            {
                                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                                if (rectTransform.sizeDelta == Vector2.zero)
                                {
                                    rectTransform.sizeDelta = new Vector2(400, 300); // 기본 크기
                                }
                                rectTransform.anchoredPosition = Vector2.zero;
                            }
                            
                            // Scale 확인
                            if (rectTransform.localScale == Vector3.zero)
                            {
                                rectTransform.localScale = Vector3.one;
                            }
                        }
                    }
                }
            }
            
            OnPopupShow();
        }
        
        protected override void OnHide()
        {
            // UIManager의 스택에서 제거
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RemovePopupFromStack(this);
            }
            OnPopupHide();
            
            // Pooling 사용 시 풀로 반환
            if (UIManager.Instance != null && UIManager.Instance.IsUsingPooling())
            {
                if (UIPoolManager.Instance != null)
                {
                    UIPoolManager.Instance.ReturnToPool(this);
                }
                else
                {
                    Debug.LogError("UIPoolManager.Instance가 null입니다!");
                }
            }
        }
        
        protected override void OnDestroy()
        {
            OnPopupDestroy();
        }
        
        /// <summary>
        /// Back 키 입력 처리
        /// </summary>
        public virtual void OnBackKeyPressed()
        {
            if (CanCloseByBackKey && IsActive)
            {
                Close();
            }
        }
        
        /// <summary>
        /// 팝업 닫기
        /// </summary>
        public virtual void Close()
        {
            Hide();
        }
        
        // Popup 전용 추상 메서드
        protected abstract void OnPopupInitialize();
        protected abstract void OnPopupShow();
        protected abstract void OnPopupHide();
        protected abstract void OnPopupDestroy();
    }
}

