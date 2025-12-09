using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule
{
    /// <summary>
    /// UI 시스템의 루트 매니저
    /// 5단계 레이어 시스템을 관리하고, Screen과 Popup을 제어함
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        
        /// <summary>
        /// UIManager 싱글톤 인스턴스
        /// </summary>
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("UIManager");
                    _instance = go.AddComponent<UIManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        // 레이어별 Canvas 관리
        private Dictionary<UILayer, Canvas> _layerCanvases = new Dictionary<UILayer, Canvas>();
        
        // Screen 관리 (스택 구조로 뒤로가기 지원)
        private Stack<BaseScreen> _screenStack = new Stack<BaseScreen>();
        
        // Popup 관리 (여러 개 가능, 스택 구조)
        private Stack<BasePopup> _popupStack = new Stack<BasePopup>();
        
        // UI 인스턴스 캐시 (타입별로 관리)
        private Dictionary<System.Type, BaseUI> _uiInstanceCache = new Dictionary<System.Type, BaseUI>();
        
        // 프리팹 경로 설정 (기본값: Resources/UIPrefabs)
        [SerializeField] private string _prefabPathPrefix = "UIPrefabs/";
        
        // Pooling 사용 여부
        [Header("Pooling 설정")]
        [SerializeField] private bool _usePooling = true;
        
        // 레이어별 Sorting Order 설정
        private const int BASE_SORTING_ORDER = 100;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLayers();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 5단계 레이어 Canvas 초기화
        /// </summary>
        private void InitializeLayers()
        {
            // EventSystem 생성 (UI 상호작용을 위해 필수)
            CreateEventSystem();
            
            // 각 레이어별로 Canvas 생성
            CreateLayerCanvas(UILayer.Background, BASE_SORTING_ORDER);
            CreateLayerCanvas(UILayer.Screen, BASE_SORTING_ORDER + 100);
            CreateLayerCanvas(UILayer.Popup, BASE_SORTING_ORDER + 200);
            CreateLayerCanvas(UILayer.Overlay, BASE_SORTING_ORDER + 300);
            CreateLayerCanvas(UILayer.System, BASE_SORTING_ORDER + 400);
        }
        
        /// <summary>
        /// EventSystem 생성 (UI 상호작용을 위해 필수)
        /// </summary>
        private void CreateEventSystem()
        {
            // 이미 EventSystem이 있으면 생성하지 않음
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                return;
            }
            
            GameObject eventSystemGO = new GameObject("EventSystem");
            // UIManager의 자식으로 설정 (UIManager가 이미 DontDestroyOnLoad이므로 자식도 함께 유지됨)
            eventSystemGO.transform.SetParent(transform);
            
            // EventSystem 컴포넌트 추가
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            // Input System 패키지 사용 여부 확인
            bool useInputSystem = IsInputSystemPackageEnabled();
            
            if (useInputSystem)
            {
                // 새로운 Input System 사용
                #if ENABLE_INPUT_SYSTEM
                var inputModule = eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                // 기본 설정
                inputModule.enabled = true;
                #else
                // Input System 패키지는 있지만 활성화되지 않은 경우
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                #endif
            }
            else
            {
                // 구 Input System 사용
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            // UIManager가 이미 DontDestroyOnLoad이므로 자식인 EventSystem도 함께 유지됨
            // 별도로 DontDestroyOnLoad를 호출할 필요 없음
        }
        
        /// <summary>
        /// Input System 패키지가 활성화되어 있는지 확인
        /// </summary>
        private bool IsInputSystemPackageEnabled()
        {
            #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            return true;
            #elif ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            // 둘 다 활성화된 경우, Input System이 우선
            return true;
            #else
            return false;
            #endif
        }
        
        /// <summary>
        /// 레이어별 Canvas 생성
        /// </summary>
        private void CreateLayerCanvas(UILayer layer, int sortingOrder)
        {
            GameObject canvasGO = new GameObject($"{layer}Layer");
            canvasGO.transform.SetParent(transform);
            
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            _layerCanvases[layer] = canvas;
        }
        
        /// <summary>
        /// 특정 레이어의 Canvas 가져오기
        /// </summary>
        public Canvas GetLayerCanvas(UILayer layer)
        {
            if (_layerCanvases.TryGetValue(layer, out Canvas canvas))
            {
                return canvas;
            }
            return null;
        }
        
        /// <summary>
        /// Screen 표시 (스택에 추가, 기존 Screen은 숨김)
        /// </summary>
        public void ShowScreen<T>() where T : BaseScreen
        {
            // 기존 Screen이 있으면 숨김 (스택에 유지)
            if (_screenStack.Count > 0)
            {
                BaseScreen currentScreen = _screenStack.Peek();
                if (currentScreen != null)
                {
                    currentScreen.Hide();
                }
            }
            
            // 새 Screen 찾기 또는 생성
            T screen = FindOrCreateUI<T>(UILayer.Screen);
            if (screen != null)
            {
                // 스택에 추가
                _screenStack.Push(screen);
                screen.Show();
            }
        }
        
        /// <summary>
        /// 이전 Screen으로 돌아가기 (뒤로가기)
        /// </summary>
        public void BackScreen()
        {
            // 현재 Screen이 있으면 스택에서 제거
            if (_screenStack.Count > 0)
            {
                BaseScreen currentScreen = _screenStack.Pop();
                if (currentScreen != null)
                {
                    currentScreen.Hide();
                }
            }
            
            // 이전 Screen 표시
            if (_screenStack.Count > 0)
            {
                BaseScreen previousScreen = _screenStack.Peek();
                if (previousScreen != null)
                {
                    previousScreen.Show();
                }
            }
        }
        
        /// <summary>
        /// Screen 숨김
        /// </summary>
        public void HideScreen()
        {
            if (_screenStack.Count > 0)
            {
                BaseScreen currentScreen = _screenStack.Pop();
                if (currentScreen != null)
                {
                    currentScreen.Hide();
                }
            }
        }
        
        /// <summary>
        /// Popup 표시 (스택에 추가)
        /// </summary>
        public T ShowPopup<T>() where T : BasePopup
        {
            T popup = FindOrCreateUI<T>(UILayer.Popup);
            if (popup != null)
            {
                _popupStack.Push(popup);
                popup.Show();
            }
            return popup;
        }
        
        /// <summary>
        /// 가장 위의 Popup 닫기 (Back 키 처리)
        /// </summary>
        public void CloseTopPopup()
        {
            if (_popupStack.Count > 0)
            {
                BasePopup topPopup = _popupStack.Peek();
                topPopup.OnBackKeyPressed();
            }
        }
        
        /// <summary>
        /// 모든 Popup 닫기
        /// </summary>
        public void CloseAllPopups()
        {
            while (_popupStack.Count > 0)
            {
                BasePopup popup = _popupStack.Pop();
                popup.Hide();
            }
        }
        
        /// <summary>
        /// Popup을 스택에서 제거 (내부 사용)
        /// </summary>
        internal void RemovePopupFromStack(BasePopup popup)
        {
            if (_popupStack.Count > 0 && _popupStack.Peek() == popup)
            {
                _popupStack.Pop();
            }
            else
            {
                // 스택의 중간에 있는 경우를 대비한 처리
                Stack<BasePopup> tempStack = new Stack<BasePopup>();
                bool found = false;
                
                while (_popupStack.Count > 0)
                {
                    BasePopup p = _popupStack.Pop();
                    if (p == popup && !found)
                    {
                        found = true;
                        continue; // 제거
                    }
                    tempStack.Push(p);
                }
                
                // 원래 순서로 복원
                while (tempStack.Count > 0)
                {
                    _popupStack.Push(tempStack.Pop());
                }
            }
        }
        
        /// <summary>
        /// UI 찾기 또는 생성 (프리팹 인스턴스화 또는 풀링)
        /// </summary>
        private T FindOrCreateUI<T>(UILayer targetLayer) where T : BaseUI
        {
            System.Type uiType = typeof(T);
            
            // Pooling 사용 시 풀에서만 가져오기 (캐시나 씬에서 찾지 않음)
            if (_usePooling)
            {
                T pooledInstance = UIPoolManager.Instance.GetFromPool<T>(targetLayer);
                if (pooledInstance != null)
                {
                    // 캐시에 저장 (Screen의 경우) - 참조용으로만 사용
                    if (targetLayer == UILayer.Screen)
                    {
                        _uiInstanceCache[uiType] = pooledInstance;
                    }
                    // Show 시점에 Initialize가 호출되므로 여기서는 호출하지 않음
                    return pooledInstance;
                }
                // 풀에서 가져오지 못하면 null 반환 (캐시나 씬에서 찾지 않음)
                Debug.LogWarning($"풀에서 {uiType.Name}을 가져올 수 없습니다.");
                return null;
            }
            
            // 캐시에서 확인 (Pooling 미사용 시)
            if (_uiInstanceCache.TryGetValue(uiType, out BaseUI cachedUI))
            {
                if (cachedUI != null && cachedUI.gameObject != null)
                {
                    T ui = cachedUI as T;
                    if (ui != null)
                    {
                        // 올바른 레이어에 있는지 확인
                        Canvas targetCanvas = GetLayerCanvas(targetLayer);
                        if (targetCanvas != null && ui.transform.parent != targetCanvas.transform)
                        {
                            ui.transform.SetParent(targetCanvas.transform, false);
                        }
                        // Show 시점에 Initialize가 호출되므로 여기서는 호출하지 않음
                        return ui;
                    }
                }
                else
                {
                    // 캐시에 있지만 null이면 제거
                    _uiInstanceCache.Remove(uiType);
                }
            }
            
            // 씬에서 찾기
            T existingUI = FindFirstObjectByType<T>();
            if (existingUI != null)
            {
                // 올바른 레이어에 있는지 확인
                Canvas targetCanvas = GetLayerCanvas(targetLayer);
                if (targetCanvas != null && existingUI.transform.parent != targetCanvas.transform)
                {
                    existingUI.transform.SetParent(targetCanvas.transform, false);
                }
                // 캐시에 저장
                _uiInstanceCache[uiType] = existingUI;
                // Show 시점에 Initialize가 호출되므로 여기서는 호출하지 않음
                return existingUI;
            }
            
            // 프리팹에서 인스턴스화 시도
            T prefabInstance = InstantiateFromPrefab<T>(targetLayer);
            if (prefabInstance != null)
            {
                // 캐시에 저장
                _uiInstanceCache[uiType] = prefabInstance;
                // Show 시점에 Initialize가 호출되므로 여기서는 호출하지 않음
                return prefabInstance;
            }
            
            // 프리팹이 없으면 새로 생성 (폴백)
            Canvas canvas = GetLayerCanvas(targetLayer);
            if (canvas != null)
            {
                GameObject uiGO = new GameObject(uiType.Name);
                uiGO.transform.SetParent(canvas.transform, false);
                T newUI = uiGO.AddComponent<T>();
                // 캐시에 저장
                _uiInstanceCache[uiType] = newUI;
                // Show 시점에 Initialize가 호출되므로 여기서는 호출하지 않음
                return newUI;
            }
            
            return null;
        }
        
        /// <summary>
        /// 프리팹에서 UI 인스턴스화
        /// </summary>
        private T InstantiateFromPrefab<T>(UILayer targetLayer) where T : BaseUI
        {
            string prefabName = typeof(T).Name;
            string prefabPath = _prefabPathPrefix + prefabName;
            
            // Resources에서 로드
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogWarning($"프리팹을 찾을 수 없습니다: {prefabPath}. Resources/{_prefabPathPrefix} 폴더에 {prefabName}.prefab 파일이 있는지 확인하세요.");
                return null;
            }
            
            // 프리팹 인스턴스화
            Canvas targetCanvas = GetLayerCanvas(targetLayer);
            if (targetCanvas == null)
            {
                Debug.LogError($"레이어 Canvas를 찾을 수 없습니다: {targetLayer}");
                return null;
            }
            
            GameObject instance = Instantiate(prefab);
            instance.name = prefabName; // 프리팹 이름에서 (Clone) 제거
            
            T uiComponent = instance.GetComponent<T>();
            if (uiComponent == null)
            {
                Debug.LogError($"프리팹에 {typeof(T).Name} 컴포넌트가 없습니다: {prefabPath}");
                Destroy(instance);
                return null;
            }
            
            // 프리팹에 Canvas가 있는지 확인 (있으면 경고)
            Canvas prefabCanvas = instance.GetComponent<Canvas>();
            if (prefabCanvas != null)
            {
                Debug.LogWarning($"프리팹 {prefabName}에 Canvas가 포함되어 있습니다. " +
                    $"UIManager가 레이어별 Canvas를 관리하므로 프리팹의 Canvas는 제거하는 것을 권장합니다. " +
                    $"프리팹의 Canvas를 제거하고 레이어 Canvas의 자식으로 인스턴스화됩니다.");
                
                // Canvas 관련 컴포넌트 제거
                CanvasScaler scaler = instance.GetComponent<CanvasScaler>();
                if (scaler != null) DestroyImmediate(scaler);
                
                GraphicRaycaster raycaster = instance.GetComponent<GraphicRaycaster>();
                if (raycaster != null) DestroyImmediate(raycaster);
                
                DestroyImmediate(prefabCanvas);
            }
            
            // 레이어 Canvas의 자식으로 설정
            instance.transform.SetParent(targetCanvas.transform, false);
            
            // RectTransform Scale 확인 및 수정 (0,0,0이면 1,1,1로 변경)
            RectTransform rectTransform = instance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector3 scale = rectTransform.localScale;
                if (scale.x == 0 && scale.y == 0 && scale.z == 0)
                {
                    rectTransform.localScale = Vector3.one;
                }
                
                // Screen은 Stretch, Popup은 MiddleCenter로 설정
                if (targetLayer == UILayer.Screen)
                {
                    // Screen: 전체 화면을 채우도록 설정
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = Vector2.zero;
                    rectTransform.anchoredPosition = Vector2.zero;
                }
                else if (targetLayer == UILayer.Popup)
                {
                    // Popup: MiddleCenter로 설정 (프리팹에 설정이 없을 경우에만)
                    // anchor가 이미 설정되어 있으면 변경하지 않음
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
                }
            }
            
            return uiComponent;
        }
        
        /// <summary>
        /// 현재 활성화된 Screen 가져오기
        /// </summary>
        public BaseScreen GetCurrentScreen()
        {
            if (_screenStack.Count > 0)
            {
                return _screenStack.Peek();
            }
            return null;
        }
        
        /// <summary>
        /// Screen 스택 개수
        /// </summary>
        public int GetScreenStackCount()
        {
            return _screenStack.Count;
        }
        
        /// <summary>
        /// 현재 활성화된 Popup 개수
        /// </summary>
        public int GetPopupCount()
        {
            return _popupStack.Count;
        }
        
        /// <summary>
        /// Pooling 사용 여부
        /// </summary>
        public bool IsUsingPooling()
        {
            return _usePooling;
        }
    }
}

