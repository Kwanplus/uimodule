using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// 모든 UI의 기본 추상 클래스
    /// </summary>
    public abstract class BaseUI : MonoBehaviour
    {
        /// <summary>UI가 표시(활성화) 완료된 직후 발행. 인자는 표시된 UI 인스턴스.</summary>
        public static event System.Action<BaseUI> Presented;

        /// <summary>UI가 숨겨지기 직전에 발행. 인자는 숨겨질 UI 인스턴스.</summary>
        public static event System.Action<BaseUI> Hiding;

        /// <summary>UI가 숨겨진 직후 발행. 인자는 숨겨진 UI 인스턴스.</summary>
        public static event System.Action<BaseUI> Hidden;

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

            // OnShow에서 생성된 하위 요소까지 서브트리에 존재하는 시점에 발행되도록 Show() 말미에서 통지
            Presented?.Invoke(this);
        }
        
        /// <summary>
        /// UI 숨김
        /// </summary>
        public virtual void Hide()
        {
            if (!IsActive) return;

            Hiding?.Invoke(this);
            IsActive = false;
            OnHide();
            
            // Pooling 사용 시 OnHide()에서 풀로 반환되며 비활성화됨
            // Pooling 미사용 시에만 여기서 비활성화
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            Hidden?.Invoke(this);
        }
        
        /// <summary>
        /// UI 제거 (메모리에서 완전히 제거)
        /// </summary>
        public virtual void Destroy()
        {
            UnityEngine.Object.Destroy(gameObject);
        }

        /// <summary>
        /// 현재 UI를 지정한 레이어 Canvas에 배치하고 RectTransform을 반환
        /// </summary>
        protected bool TryAttachToLayerCanvas(UILayer targetLayer, out RectTransform rectTransform)
        {
            rectTransform = GetComponent<RectTransform>();
            if (UIManager.Instance == null)
            {
                return false;
            }

            Canvas layerCanvas = UIManager.Instance.GetLayerCanvas(targetLayer);
            if (layerCanvas == null)
            {
                return false;
            }

            if (transform.parent != layerCanvas.transform)
            {
                transform.SetParent(layerCanvas.transform, false);
            }

            return true;
        }

        /// <summary>
        /// 전체 화면 Stretch RectTransform 적용
        /// </summary>
        protected static void ApplyStretchRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            EnsureValidScale(rectTransform);
        }

        /// <summary>
        /// Popup 기본 가운데 정렬 RectTransform 적용
        /// </summary>
        protected static void ApplyPopupCenterRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            if (rectTransform.anchorMin == Vector2.zero && rectTransform.anchorMax == Vector2.one)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                if (rectTransform.sizeDelta == Vector2.zero)
                {
                    rectTransform.sizeDelta = new Vector2(400, 300);
                }

                rectTransform.anchoredPosition = Vector2.zero;
            }

            EnsureValidScale(rectTransform);
        }

        /// <summary>
        /// RectTransform scale이 0으로 깨졌을 때 기본값으로 복원
        /// </summary>
        protected static void EnsureValidScale(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            if (rectTransform.localScale == Vector3.zero)
            {
                rectTransform.localScale = Vector3.one;
            }
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

