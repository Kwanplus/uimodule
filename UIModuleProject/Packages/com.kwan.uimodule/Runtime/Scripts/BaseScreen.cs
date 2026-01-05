using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// Screen UI의 기본 추상 클래스
    /// 한 화면에 1개만 존재할 수 있음
    /// </summary>
    public abstract class BaseScreen : BaseUI
    {
        protected virtual void Awake()
        {
            layer = UILayer.Screen;
        }
        
        protected override void OnInitialize()
        {
            layer = UILayer.Screen;
            OnScreenInitialize();
        }
        
        protected override void OnShow()
        {
            // 레이어 Canvas로 이동 및 RectTransform 설정만 수행
            // OnScreenBegin은 UIManager.ShowScreen에서 호출됨
            if (UIManager.Instance != null)
            {
                Canvas layerCanvas = UIManager.Instance.GetLayerCanvas(UILayer.Screen);
                if (layerCanvas != null && transform.parent != layerCanvas.transform)
                {
                    transform.SetParent(layerCanvas.transform, false);
                    
                    RectTransform rectTransform = GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.sizeDelta = Vector2.zero;
                        rectTransform.anchoredPosition = Vector2.zero;
                        
                        if (rectTransform.localScale == Vector3.zero)
                        {
                            rectTransform.localScale = Vector3.one;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// UIManager에서 ShowScreen 작업이 완료된 후 호출
        /// </summary>
        public void NotifyScreenBegin()
        {
            OnScreenBegin();
        }
        
        protected override void OnHide()
        {
            OnScreenHide();
            
            // Pooling 사용 시 풀로 반환
            if (UIManager.Instance != null && UIManager.Instance.IsUsingPooling())
            {
                if (UIPoolManager.Instance != null)
                {
                    UIPoolManager.Instance.ReturnToPool(this);
                }
            }
        }
        
        protected override void OnDestroy()
        {
            OnScreenDestroy();
        }
        
        // Screen 전용 추상 메서드
        protected abstract void OnScreenInitialize();
        /// <summary>
        /// Screen 표시 완료 후 호출 (ShowScreen 작업이 모두 완료된 상태)
        /// 이 시점에서 팝업을 띄워도 안전합니다.
        /// </summary>
        protected abstract void OnScreenBegin();
        protected abstract void OnScreenHide();
        protected abstract void OnScreenDestroy();
    }
}

