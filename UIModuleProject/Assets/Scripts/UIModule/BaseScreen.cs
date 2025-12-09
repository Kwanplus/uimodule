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
            // PoolRoot에 있으면 레이어 Canvas로 이동
            if (UIManager.Instance != null)
            {
                Canvas layerCanvas = UIManager.Instance.GetLayerCanvas(UILayer.Screen);
                if (layerCanvas != null)
                {
                    // 현재 부모가 레이어 Canvas가 아니면 이동 (PoolRoot 또는 그 하위에 있을 수 있음)
                    if (transform.parent != layerCanvas.transform)
                    {
                        transform.SetParent(layerCanvas.transform, false);
                        
                        // RectTransform 설정 (Screen은 Stretch)
                        RectTransform rectTransform = GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            rectTransform.anchorMin = Vector2.zero;
                            rectTransform.anchorMax = Vector2.one;
                            rectTransform.sizeDelta = Vector2.zero;
                            rectTransform.anchoredPosition = Vector2.zero;
                            
                            // Scale 확인
                            if (rectTransform.localScale == Vector3.zero)
                            {
                                rectTransform.localScale = Vector3.one;
                            }
                        }
                    }
                }
            }
            
            OnScreenShow();
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
                else
                {
                    Debug.LogError("UIPoolManager.Instance가 null입니다!");
                }
            }
        }
        
        protected override void OnDestroy()
        {
            OnScreenDestroy();
        }
        
        // Screen 전용 추상 메서드
        protected abstract void OnScreenInitialize();
        protected abstract void OnScreenShow();
        protected abstract void OnScreenHide();
        protected abstract void OnScreenDestroy();
    }
}

