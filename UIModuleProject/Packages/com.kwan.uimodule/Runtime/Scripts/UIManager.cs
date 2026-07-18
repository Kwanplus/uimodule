using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

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
        /// 새 Manager를 생성하지 않고 현재 인스턴스만 반환한다.
        /// </summary>
        internal static UIManager ExistingInstance => _instance;
        
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
                    // 동적으로 생성된 경우 기본값 사용
                    // 경로를 변경하려면 UIManager.Instance.SetPrefabPathPrefix()를 호출하거나
                    // 씬에 UIManager를 미리 배치하여 Inspector에서 설정 가능
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
        
        // Background/Overlay/System은 타입당 1개 재사용을 보장하기 위한 캐시
        private Dictionary<System.Type, BaseUI> _singlePerTypeLayerCache = new Dictionary<System.Type, BaseUI>();
        private List<BaseUI> _singleLayerPresentationOrder = new List<BaseUI>();
        private HashSet<string> _reportedDiagnostics = new HashSet<string>();
        
        // 프리팹 경로 설정 (기본값: Resources/UIPrefabs)
        [SerializeField] private string _prefabPathPrefix = "UIPrefabs/";
        
        // Pooling 사용 여부
        [Header("Pooling 설정")]
        [SerializeField] private bool _usePooling = true;

        [Header("Gamepad UI Input")]
        [Tooltip("비표준 UI Input Action을 사용하는 프로젝트에서만 지정합니다.")]
        [SerializeField] private UIInputConfiguration _inputConfiguration;
        
        // Canvas Scaler 설정
        [Header("Canvas Scaler 설정")]
        [SerializeField] private CanvasScaler.ScaleMode _uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        [SerializeField] private Vector2 _referenceResolution = new Vector2(1920, 1080);
        [SerializeField] [Range(0f, 1f)] private float _matchWidthOrHeight = 0.5f;
        [SerializeField] private CanvasScaler.ScreenMatchMode _screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // 동적 생성 레이어 Canvas의 CanvasScaler.Reference Pixels Per Unit (스프라이트/UI 단위 환산)
        [SerializeField] private float _referencePixelsPerUnit = 100f;
        
        // 레이어별 Sorting Order 설정
        private const int BASE_SORTING_ORDER = 100;

        private EventSystem _eventSystem;
        private UIFocusController _focusController;
        private UIInputCaptureState _inputCaptureState;
        private UIInputDeviceState _inputDeviceState;
        private int _lastCancelFrame = -1;

        /// <summary>
        /// UI 입력에 사용 중인 EventSystem을 반환한다.
        /// </summary>
        public EventSystem EventSystem => _eventSystem;

        /// <summary>
        /// 현재 UI 입력 점유 상태를 반환한다.
        /// </summary>
        public UIInputCaptureState InputCaptureState => _inputCaptureState;

        /// <summary>
        /// UI가 게임플레이 입력을 점유하고 있는지 반환한다.
        /// </summary>
        public bool IsInputCaptured => _inputCaptureState.IsCaptured;

        /// <summary>
        /// UI 입력 점유 상태가 바뀔 때 발행한다.
        /// </summary>
        public event System.Action<UIInputCaptureState> InputCaptureChanged;

        /// <summary>
        /// 마지막 UI 입력 장치와 Gamepad 연결 상태를 반환한다.
        /// </summary>
        public UIInputDeviceState InputDeviceState => _inputDeviceState;

        /// <summary>
        /// UI 입력 장치 상태가 바뀔 때 발행한다.
        /// </summary>
        public event System.Action<UIInputDeviceState> InputDeviceChanged;

        /// <summary>
        /// EventSystem 생성 전 선택 UI 입력 설정을 지정한다.
        /// </summary>
        public void SetInputConfiguration(UIInputConfiguration configuration)
        {
            if (_eventSystem != null)
            {
                Debug.LogWarning("[UIModule] UIInputConfiguration은 UIManager 초기화 전에 지정해야 합니다.");
                return;
            }

            _inputConfiguration = configuration;
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLayers();
                _focusController = new UIFocusController(this);
                BaseUI.Presented += HandleUiPresented;
                BaseUI.Hiding += HandleUiHiding;
                BaseUI.Hidden += HandleUiHidden;
                BaseScreen.ScreenBegan += HandleScreenBegan;
                BaseScreen.ScreenResumed += HandleScreenResumed;
                InputSystem.onDeviceChange += HandleInputDeviceChange;
                UpdateInputDeviceState(UIInputDeviceType.None);
                UpdateInputCaptureState();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            BaseUI.Presented -= HandleUiPresented;
            BaseUI.Hiding -= HandleUiHiding;
            BaseUI.Hidden -= HandleUiHidden;
            BaseScreen.ScreenBegan -= HandleScreenBegan;
            BaseScreen.ScreenResumed -= HandleScreenResumed;
            InputSystem.onDeviceChange -= HandleInputDeviceChange;
            _focusController?.Dispose();
            _instance = null;
        }

        private void Update()
        {
            TrackInputDevice();
            HandleNavigationInput();
        }

        private void LateUpdate()
        {
            HandleCancelInput();
            _focusController?.ApplyPointerSelectionPolicy();
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
            _eventSystem = UIInputBootstrap.Ensure(transform, _inputConfiguration);
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
            scaler.uiScaleMode = _uiScaleMode;
            scaler.referenceResolution = _referenceResolution;
            scaler.matchWidthOrHeight = _matchWidthOrHeight;
            scaler.screenMatchMode = _screenMatchMode;
            scaler.referencePixelsPerUnit = _referencePixelsPerUnit;
            
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
        /// <returns>생성된 Screen 인스턴스</returns>
        public T ShowScreen<T>() where T : BaseScreen
        {
            System.Type screenType = typeof(T);
            
            // 같은 타입의 Screen 제거 (Screen은 타입당 하나만 존재)
            Stack<BaseScreen> tempStack = new Stack<BaseScreen>();
            while (_screenStack.Count > 0)
            {
                BaseScreen screen = _screenStack.Pop();
                if (screen != null && screen.GetType() == screenType)
                {
                    screen.Hide();
                }
                else
                {
                    tempStack.Push(screen);
                }
            }
            
            // 스택 복원
            while (tempStack.Count > 0)
            {
                _screenStack.Push(tempStack.Pop());
            }
            
            // 기존 Screen이 있으면 숨김 (스택에 유지)
            if (_screenStack.Count > 0)
            {
                BaseScreen currentScreen = _screenStack.Peek();
                if (currentScreen != null)
                {
                    currentScreen.Hide();
                }
            }
            
            // 새 Screen 생성 및 표시
            T newScreen = FindOrCreateUI<T>(UILayer.Screen);
            if (newScreen != null)
            {
                ClosePopupsOnScreenChange();
                _screenStack.Push(newScreen);
                newScreen.Show();
                newScreen.NotifyScreenBegin();
                UpdateInputCaptureState();
            }
            
            return newScreen;
        }
        
        /// <summary>
        /// 스크린 이동 시 닫혀야 하는 팝업들 닫기
        /// </summary>
        private void ClosePopupsOnScreenChange()
        {
            Stack<BasePopup> tempStack = new Stack<BasePopup>();
            
            while (_popupStack.Count > 0)
            {
                BasePopup popup = _popupStack.Pop();
                if (popup != null)
                {
                    if (popup.CloseOnScreenChange)
                    {
                        // 스크린 이동 시 닫혀야 하는 팝업
                        popup.Hide(); // 풀로 반환됨
                    }
                    else
                    {
                        // 남아있어야 하는 팝업
                        tempStack.Push(popup);
                    }
                }
            }
            
            // 남아있어야 하는 팝업들을 스택에 복원
            while (tempStack.Count > 0)
            {
                _popupStack.Push(tempStack.Pop());
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
                    // 풀링 사용 시 비활성화된 경우 풀에서 다시 가져오기
                    if (_usePooling && UIPoolManager.Instance != null && !previousScreen.gameObject.activeSelf)
                    {
                        _screenStack.Pop();
                        System.Type screenType = previousScreen.GetType();
                        BaseScreen newScreen = FindOrCreateUIByType(screenType, UILayer.Screen) as BaseScreen;
                        
                        if (newScreen != null)
                        {
                            _screenStack.Push(newScreen);
                            newScreen.Show();
                            if (newScreen != previousScreen)
                            {
                                // 풀에서 다른 인스턴스를 받았다면 이전 화면의 런타임 동적 콘텐츠가
                                // 존재한다는 보장이 없으므로 최초 구성 수명주기를 다시 실행한다.
                                newScreen.NotifyScreenBegin();
                            }
                            else
                            {
                                newScreen.NotifyScreenResumed();
                            }
                        }
                    }
                    else
                    {
                        previousScreen.Show();
                        previousScreen.NotifyScreenResumed();
                    }
                }
            }

            UpdateInputCaptureState();
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

            UpdateInputCaptureState();
        }
        
        /// <summary>
        /// Popup 표시 (스택에 추가)
        /// </summary>
        /// <returns>생성된 Popup 인스턴스</returns>
        public T ShowPopup<T>() where T : BasePopup
        {
            System.Type popupType = typeof(T);
            
            // 싱글톤 팝업인 경우 기존 팝업 닫기
            Stack<BasePopup> tempStack = new Stack<BasePopup>();
            while (_popupStack.Count > 0)
            {
                BasePopup popup = _popupStack.Pop();
                if (popup != null && popup.GetType() == popupType && popup.IsSingleton)
                {
                    popup.Hide();
                }
                else
                {
                    tempStack.Push(popup);
                }
            }
            
            // 스택 복원
            while (tempStack.Count > 0)
            {
                _popupStack.Push(tempStack.Pop());
            }
            
            // 새 팝업 생성 및 표시
            T newPopup = FindOrCreateUI<T>(UILayer.Popup);
            if (newPopup != null)
            {
                _focusController?.HandleBeforePopupShown(newPopup);
                _popupStack.Push(newPopup);
                newPopup.Show();
                UpdateInputCaptureState();
            }
            return newPopup;
        }

        /// <summary>
        /// 외부에서 생성한 Popup을 UIManager 계약에 등록해 표시한다.
        /// </summary>
        public BasePopup ShowPopup(BasePopup popup)
        {
            if (popup == null)
            {
                return null;
            }

            if (!ContainsPopup(popup))
            {
                _focusController?.HandleBeforePopupShown(popup);
                _popupStack.Push(popup);
            }

            popup.Show();
            UpdateInputCaptureState();
            return popup;
        }

        /// <summary>
        /// Background 표시 (타입당 1개 재사용)
        /// </summary>
        public T ShowBackground<T>() where T : BaseBackground
        {
            return ShowSinglePerTypeLayer<T>(UILayer.Background);
        }

        /// <summary>
        /// Overlay 표시 (타입당 1개 재사용)
        /// </summary>
        public T ShowOverlay<T>() where T : BaseOverlay
        {
            return ShowSinglePerTypeLayer<T>(UILayer.Overlay);
        }

        /// <summary>
        /// System 표시 (타입당 1개 재사용)
        /// </summary>
        public T ShowSystem<T>() where T : BaseSystem
        {
            return ShowSinglePerTypeLayer<T>(UILayer.System);
        }
        
        /// <summary>
        /// 현재 최상위 UI의 Cancel 정책을 적용한다.
        /// </summary>
        public void CloseTopPopup()
        {
            TryRouteCancel();
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

            UpdateInputCaptureState();
        }
        
        /// <summary>
        /// Popup을 스택에서 제거 (내부 사용)
        /// </summary>
        internal void RemovePopupFromStack(BasePopup popup)
        {
            if (_popupStack.Count > 0 && ReferenceEquals(_popupStack.Peek(), popup))
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
                    if (ReferenceEquals(p, popup) && !found)
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

            UpdateInputCaptureState();
        }

        /// <summary>
        /// Hide를 거치지 않은 파괴 시 스택의 stale 참조를 정리한다.
        /// </summary>
        internal void HandleExternallyDestroyedUi(BaseUI ui)
        {
            if (ui is BasePopup popup)
            {
                RemovePopupFromStack(popup);
                return;
            }

            RemoveScreenFromStack(ui as BaseScreen);
            RemoveCachedUi(ui);
            _singleLayerPresentationOrder.Remove(ui);
            UpdateInputCaptureState();

            ResumeTopScreenAfterExternalRemoval();
        }

        /// <summary>
        /// 파괴된 Screen을 스택의 어느 위치에서든 제거한다.
        /// </summary>
        private void RemoveScreenFromStack(BaseScreen screen)
        {
            if (screen == null)
            {
                return;
            }

            Stack<BaseScreen> retained = new Stack<BaseScreen>();
            while (_screenStack.Count > 0)
            {
                BaseScreen current = _screenStack.Pop();
                if (!ReferenceEquals(current, screen))
                {
                    retained.Push(current);
                }
            }

            while (retained.Count > 0)
            {
                _screenStack.Push(retained.Pop());
            }
        }

        /// <summary>
        /// 파괴된 UI를 타입 캐시에서 제거한다.
        /// </summary>
        private void RemoveCachedUi(BaseUI ui)
        {
            List<System.Type> keysToRemove = new List<System.Type>();
            foreach (KeyValuePair<System.Type, BaseUI> pair in _uiInstanceCache)
            {
                if (ReferenceEquals(pair.Value, ui))
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            foreach (System.Type key in keysToRemove)
            {
                _uiInstanceCache.Remove(key);
            }

            keysToRemove.Clear();
            foreach (KeyValuePair<System.Type, BaseUI> pair in _singlePerTypeLayerCache)
            {
                if (ReferenceEquals(pair.Value, ui))
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            foreach (System.Type key in keysToRemove)
            {
                _singlePerTypeLayerCache.Remove(key);
            }
        }

        /// <summary>
        /// 최상위 Screen이 외부 파괴로 제거됐을 때 남은 Screen을 재개한다.
        /// </summary>
        private void ResumeTopScreenAfterExternalRemoval()
        {
            BaseScreen screen = GetCurrentScreen();
            if (screen != null && !screen.IsActive)
            {
                screen.Show();
                screen.NotifyScreenResumed();
            }
        }
        
        /// <summary>
        /// UI 찾기 또는 생성 (프리팹 인스턴스화 또는 풀링)
        /// </summary>
        private T FindOrCreateUI<T>(UILayer targetLayer) where T : BaseUI
        {
            return FindOrCreateUIByType(typeof(T), targetLayer) as T;
        }

        /// <summary>
        /// Background/Overlay/System 레이어를 타입당 1개 인스턴스로 표시
        /// </summary>
        private T ShowSinglePerTypeLayer<T>(UILayer targetLayer) where T : BaseUI
        {
            System.Type uiType = typeof(T);
            if (_singlePerTypeLayerCache.TryGetValue(uiType, out BaseUI cachedUI))
            {
                if (cachedUI != null && cachedUI.gameObject != null)
                {
                    if (!cachedUI.IsActive)
                    {
                        TrackSingleLayerPresentation(cachedUI);
                        cachedUI.Show();
                    }

                    return cachedUI as T;
                }

                _singlePerTypeLayerCache.Remove(uiType);
            }

            T ui = FindOrCreateUI<T>(targetLayer);
            if (ui != null)
            {
                _singlePerTypeLayerCache[uiType] = ui;
                TrackSingleLayerPresentation(ui);
                ui.Show();
            }

            return ui;
        }
        
        /// <summary>
        /// UI 찾기 또는 생성 (System.Type 버전)
        /// </summary>
        private BaseUI FindOrCreateUIByType(System.Type uiType, UILayer targetLayer)
        {
            bool canReuseByType = targetLayer == UILayer.Screen || IsSinglePerTypeLayer(targetLayer);

            // Pooling 사용 시 풀에서 가져오기
            if (_usePooling)
            {
                BaseUI pooledInstance = UIPoolManager.Instance.GetFromPool(uiType, targetLayer);
                if (pooledInstance != null)
                {
                    if (targetLayer == UILayer.Screen || IsSinglePerTypeLayer(targetLayer))
                    {
                        _uiInstanceCache[uiType] = pooledInstance;
                    }
                    return pooledInstance;
                }
                return null;
            }
            
            // 캐시에서 확인 (Pooling 미사용 시)
            if (canReuseByType && _uiInstanceCache.TryGetValue(uiType, out BaseUI cachedUI))
            {
                if (cachedUI != null && cachedUI.gameObject != null)
                {
                    // 올바른 레이어로 이동
                    Canvas targetCanvas = GetLayerCanvas(targetLayer);
                    if (targetCanvas != null && cachedUI.transform.parent != targetCanvas.transform)
                    {
                        cachedUI.transform.SetParent(targetCanvas.transform, false);
                    }

                    ConfigureRectTransform(cachedUI.GetComponent<RectTransform>(), targetLayer);
                    return cachedUI;
                }
                else
                {
                    // 캐시에 있지만 null이면 제거
                    _uiInstanceCache.Remove(uiType);
                }
            }
            
            // Screen 및 단일 레이어만 씬의 기존 인스턴스를 재사용한다.
            // Popup은 같은 타입이라도 중첩될 수 있으므로 항상 별도 인스턴스를 만든다.
            BaseUI existingUI = null;
            if (canReuseByType)
            {
                var findMethod = typeof(Object).GetMethod("FindObjectOfType", new System.Type[] { typeof(System.Type) });
                if (findMethod != null)
                {
                    existingUI = findMethod.Invoke(null, new object[] { uiType }) as BaseUI;
                }
                else
                {
                    var findObjectsMethod = typeof(Object).GetMethod("FindObjectsOfType", new System.Type[] { typeof(System.Type) });
                    if (findObjectsMethod != null)
                    {
                        BaseUI[] objects = findObjectsMethod.Invoke(null, new object[] { uiType }) as BaseUI[];
                        if (objects != null && objects.Length > 0)
                        {
                            existingUI = objects[0];
                        }
                    }
                }
            }
            if (existingUI != null)
            {
                // 올바른 레이어로 이동
                Canvas targetCanvas = GetLayerCanvas(targetLayer);
                if (targetCanvas != null && existingUI.transform.parent != targetCanvas.transform)
                {
                    existingUI.transform.SetParent(targetCanvas.transform, false);
                }

                ConfigureRectTransform(existingUI.GetComponent<RectTransform>(), targetLayer);
                if (canReuseByType)
                {
                    _uiInstanceCache[uiType] = existingUI;
                }
                return existingUI;
            }
            
            // Pooling을 끈 경우 프리팹이 없는 타입은 런타임 UI 생성을 의도한 것으로 보고
            // 불필요한 경고 없이 아래의 빈 UI 생성 경로로 진행한다.
            string prefabPath = _prefabPathPrefix + uiType.Name;
            BaseUI prefabInstance = Resources.Load<GameObject>(prefabPath) == null
                ? null
                : InstantiateFromPrefabByType(uiType, targetLayer);
            if (prefabInstance != null)
            {
                if (canReuseByType)
                {
                    _uiInstanceCache[uiType] = prefabInstance;
                }
                return prefabInstance;
            }
            
            // 프리팹이 없으면 새로 생성
            Canvas canvas = GetLayerCanvas(targetLayer);
            if (canvas != null)
            {
                GameObject uiGO = new GameObject(uiType.Name, typeof(RectTransform));
                uiGO.transform.SetParent(canvas.transform, false);
                BaseUI newUI = uiGO.AddComponent(uiType) as BaseUI;
                if (canReuseByType)
                {
                    _uiInstanceCache[uiType] = newUI;
                }
                return newUI;
            }
            
            return null;
        }
        
        /// <summary>
        /// 프리팹에서 UI 인스턴스화 (System.Type 버전)
        /// </summary>
        private BaseUI InstantiateFromPrefabByType(System.Type uiType, UILayer targetLayer)
        {
            string prefabName = uiType.Name;
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
            
            BaseUI uiComponent = instance.GetComponent(uiType) as BaseUI;
            if (uiComponent == null)
            {
                Debug.LogError($"프리팹에 {uiType.Name} 컴포넌트가 없습니다: {prefabPath}");
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
            ConfigureRectTransform(rectTransform, targetLayer);
            
            return uiComponent;
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
            ConfigureRectTransform(rectTransform, targetLayer);
            
            return uiComponent;
        }

        /// <summary>
        /// 단일 재사용 정책을 적용하는 레이어인지 반환
        /// </summary>
        private bool IsSinglePerTypeLayer(UILayer layer)
        {
            return layer == UILayer.Background || layer == UILayer.Overlay || layer == UILayer.System;
        }

        /// <summary>
        /// 레이어별 RectTransform 규칙 적용
        /// </summary>
        private void ConfigureRectTransform(RectTransform rectTransform, UILayer targetLayer)
        {
            if (rectTransform == null)
            {
                return;
            }

            if (rectTransform.localScale == Vector3.zero)
            {
                rectTransform.localScale = Vector3.one;
            }

            if (targetLayer == UILayer.Screen || targetLayer == UILayer.Background || targetLayer == UILayer.Overlay || targetLayer == UILayer.System)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
            }
            // Popup은 프리팹의 Anchor/Size/Position을 그대로 유지 (Stretch/Center 등 의도 존중)
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

        /// <summary>
        /// UI 풀링 사용 여부를 설정한다.
        /// 샘플 또는 런타임 생성 UI를 사용할 때는 false로 설정할 수 있다.
        /// </summary>
        public void SetPoolingEnabled(bool usePooling)
        {
            if (_usePooling == usePooling)
            {
                return;
            }

            _usePooling = usePooling;
            if (!usePooling && UIPoolManager.Instance != null)
            {
                UIPoolManager.Instance.ClearAllPools();
            }
        }
        
        /// <summary>
        /// 프리팹 경로 접두사 가져오기
        /// </summary>
        public string GetPrefabPathPrefix()
        {
            return _prefabPathPrefix;
        }
        
        /// <summary>
        /// 프리팹 경로 접두사 설정 (런타임에 변경 가능)
        /// </summary>
        public void SetPrefabPathPrefix(string pathPrefix)
        {
            if (string.IsNullOrEmpty(pathPrefix))
            {
                _prefabPathPrefix = "UIPrefabs/";
                return;
            }
            
            // 경로 끝에 슬래시가 없으면 추가
            string newPathPrefix = pathPrefix.EndsWith("/") ? pathPrefix : pathPrefix + "/";
            
            // 경로가 변경되었으면 기존 풀 클리어
            if (_prefabPathPrefix != newPathPrefix && UIPoolManager.Instance != null)
            {
                UIPoolManager.Instance.ClearAllPools();
            }
            
            _prefabPathPrefix = newPathPrefix;
        }

        /// <summary>
        /// 현재 입력을 받아야 하는 최상위 UI를 반환한다.
        /// </summary>
        internal BaseUI GetTopInputUI()
        {
            BaseUI systemUi = GetActiveSingleLayerUI(UILayer.System);
            if (systemUi != null)
            {
                return systemUi;
            }

            BaseUI overlayUi = GetActiveSingleLayerUI(UILayer.Overlay);
            if (overlayUi != null)
            {
                return overlayUi;
            }

            while (_popupStack.Count > 0
                && (_popupStack.Peek() == null || !_popupStack.Peek().IsActive))
            {
                BasePopup stalePopup = _popupStack.Pop();
                if (stalePopup != null)
                {
                    _focusController?.Forget(stalePopup);
                }
            }

            if (_popupStack.Count > 0)
            {
                return _popupStack.Peek();
            }

            BaseScreen screen = GetCurrentScreen();
            if (screen != null && screen.IsActive)
            {
                return screen;
            }

            return GetActiveSingleLayerUI(UILayer.Background);
        }

        /// <summary>
        /// 관리 레이어 안에서 대상 Selectable을 소유한 UI 루트를 반환한다.
        /// </summary>
        internal BaseUI GetManagedUiOwner(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            Transform current = target.transform;
            while (current != null)
            {
                BaseUI owner = current.GetComponent<BaseUI>();
                if (owner != null)
                {
                    return IsManagedUiObject(owner.gameObject) ? owner : null;
                }

                current = current.parent;
            }

            return null;
        }

        /// <summary>
        /// Popup 모달 차단 대상이 입력 우선순위상 실제로 아래인지 반환한다.
        /// </summary>
        internal bool IsLowerInputPriority(BaseUI candidate, BasePopup popup)
        {
            if (candidate == null || popup == null || candidate == popup)
            {
                return false;
            }

            int candidatePriority = GetLayerInputPriority(candidate.Layer);
            int popupPriority = GetLayerInputPriority(UILayer.Popup);
            if (candidatePriority != popupPriority)
            {
                return candidatePriority < popupPriority;
            }

            if (!(candidate is BasePopup candidatePopup))
            {
                return false;
            }

            BasePopup[] popups = _popupStack.ToArray();
            int candidateIndex = System.Array.IndexOf(popups, candidatePopup);
            int popupIndex = System.Array.IndexOf(popups, popup);
            return candidateIndex >= 0 && popupIndex >= 0 && candidateIndex > popupIndex;
        }

        /// <summary>
        /// 대상이 이 UIManager가 소유한 레이어 Canvas 아래에 있는지 반환한다.
        /// </summary>
        internal bool IsManagedUiObject(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            foreach (Canvas canvas in _layerCanvases.Values)
            {
                if (target.transform.IsChildOf(canvas.transform))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 코드에서 Cancel을 한 번 라우팅한다.
        /// </summary>
        /// <returns>Cancel을 UI가 처리했으면 true다.</returns>
        public bool TryRouteCancel()
        {
            if (_lastCancelFrame == Time.frameCount)
            {
                return false;
            }

            _lastCancelFrame = Time.frameCount;
            BaseUI targetUi = GetTopInputUI();
            if (targetUi == null)
            {
                return false;
            }

            UIFocusScope scope = targetUi.GetComponent<UIFocusScope>();
            UICancelBehavior behavior = scope == null ? UICancelBehavior.Default : scope.CancelBehavior;
            if (behavior == UICancelBehavior.Ignore)
            {
                return true;
            }

            if (behavior == UICancelBehavior.Custom)
            {
                scope.InvokeCustomCancel();
                return true;
            }

            if (targetUi is BasePopup popup)
            {
                popup.OnBackKeyPressed();
                return true;
            }

            if (targetUi is BaseScreen)
            {
                BackScreen();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 활성 단일 레이어 UI를 반환한다.
        /// </summary>
        private BaseUI GetActiveSingleLayerUI(UILayer layer)
        {
            for (int index = _singleLayerPresentationOrder.Count - 1; index >= 0; index--)
            {
                BaseUI ui = _singleLayerPresentationOrder[index];
                if (ui != null && ui.Layer == layer && ui.IsActive)
                {
                    return ui;
                }
            }

            return null;
        }

        /// <summary>
        /// Dictionary 순서 대신 실제 마지막 표시 순서로 단일 레이어 입력 우선순위를 결정한다.
        /// </summary>
        private void TrackSingleLayerPresentation(BaseUI ui)
        {
            _singleLayerPresentationOrder.Remove(ui);
            _singleLayerPresentationOrder.Add(ui);
        }

        /// <summary>
        /// Popup 스택에 같은 인스턴스가 이미 등록됐는지 확인한다.
        /// </summary>
        private bool ContainsPopup(BasePopup popup)
        {
            foreach (BasePopup item in _popupStack)
            {
                if (ReferenceEquals(item, popup))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 레이어의 입력 우선순위를 반환한다.
        /// </summary>
        private static int GetLayerInputPriority(UILayer layer)
        {
            return (int)layer;
        }

        /// <summary>
        /// 동일한 런타임 구성 경고를 한 번만 출력한다.
        /// </summary>
        private void ReportOnce(string key, string message)
        {
            if (_reportedDiagnostics.Add(key))
            {
                Debug.LogWarning(message);
            }
        }

        /// <summary>
        /// UI 표시 이벤트를 포커스와 입력 점유 상태에 반영한다.
        /// </summary>
        private void HandleUiPresented(BaseUI ui)
        {
            if (ui is BasePopup popup && !ContainsPopup(popup))
            {
                // 외부 Instantiate 후 BasePopup.Show()를 직접 호출한 경우에도
                // 입력 점유와 Cancel 대상이 분리되지 않도록 같은 스택 계약에 등록한다.
                _focusController?.HandleBeforePopupShown(popup);
                _popupStack.Push(popup);
                ReportOnce(
                    "DirectPopupShow",
                    "[UIModule] BasePopup.Show() 직접 호출을 감지했습니다. UIManager.ShowPopup(popup)을 사용해 Popup 표시를 등록하세요.");
            }

            _focusController?.HandleShown(ui);
            UpdateInputCaptureState();
        }

        /// <summary>
        /// UI 숨김 직전 stale selection을 제거한다.
        /// </summary>
        private void HandleUiHiding(BaseUI ui)
        {
            _focusController?.HandleHiding(ui);
        }

        /// <summary>
        /// UI 숨김 뒤 포커스와 입력 점유 상태를 갱신한다.
        /// </summary>
        private void HandleUiHidden(BaseUI ui)
        {
            _focusController?.HandleHidden(ui);
            UpdateInputCaptureState();
        }

        /// <summary>
        /// 동적 Screen 콘텐츠 생성이 끝난 뒤 포커스를 적용한다.
        /// </summary>
        private void HandleScreenBegan(BaseScreen screen)
        {
            _focusController?.HandleScreenBegan(screen);
        }

        /// <summary>
        /// Screen 스택 복귀 뒤 포커스를 복원한다.
        /// </summary>
        private void HandleScreenResumed(BaseScreen screen)
        {
            _focusController?.HandleScreenResumed(screen);
        }

        /// <summary>
        /// Input System UI 모듈이 처리하지 못하는 Cancel만 최상위 UI로 전달한다.
        /// </summary>
        private void HandleCancelInput()
        {
            if (!(_eventSystem?.currentInputModule is InputSystemUIInputModule inputModule)
                || inputModule.cancel?.action == null
                || !inputModule.cancel.action.WasPerformedThisFrame()
                || _lastCancelFrame == Time.frameCount)
            {
                return;
            }

            GameObject selected = _eventSystem.currentSelectedGameObject;
            if (selected != null && ExecuteEvents.CanHandleEvent<ICancelHandler>(selected))
            {
                return;
            }

            TryRouteCancel();
        }

        /// <summary>
        /// Navigate 입력에서 선택이 비어 있으면 현재 UI 범위로 복구한다.
        /// </summary>
        private void HandleNavigationInput()
        {
            if (_eventSystem?.currentInputModule is InputSystemUIInputModule inputModule
                && inputModule.move?.action != null
                && inputModule.move.action.WasPerformedThisFrame())
            {
                _focusController?.EnsureSelectionForNavigation();
            }
        }

        /// <summary>
        /// UI 입력 액션에서 마지막 사용 장치를 갱신한다.
        /// </summary>
        private void TrackInputDevice()
        {
            if (!(_eventSystem?.currentInputModule is InputSystemUIInputModule inputModule))
            {
                return;
            }

            TrackInputDevice(inputModule.move?.action);
            TrackInputDevice(inputModule.submit?.action);
            TrackInputDevice(inputModule.cancel?.action);
            TrackInputDevice(inputModule.point?.action);
            TrackInputDevice(inputModule.leftClick?.action);
        }

        /// <summary>
        /// 이번 프레임에 수행된 액션의 장치 종류를 반영한다.
        /// </summary>
        private void TrackInputDevice(InputAction action)
        {
            if (action == null || !action.WasPerformedThisFrame() || action.activeControl == null)
            {
                return;
            }

            UpdateInputDeviceState(GetDeviceType(action.activeControl.device));
        }

        /// <summary>
        /// 장치 연결 상태 변화에 맞춰 공개 상태와 포커스를 갱신한다.
        /// </summary>
        private void HandleInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!(device is Gamepad))
            {
                return;
            }

            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            {
                UpdateInputDeviceState();
                _focusController?.EnsureSelectionForNavigation();
                return;
            }

            if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            {
                UpdateInputDeviceState();
            }
        }

        /// <summary>
        /// Input System 장치를 공개 UI 장치 유형으로 변환한다.
        /// </summary>
        private static UIInputDeviceType GetDeviceType(InputDevice device)
        {
            if (device is Gamepad)
            {
                return UIInputDeviceType.Gamepad;
            }

            if (device is Keyboard)
            {
                return UIInputDeviceType.Keyboard;
            }

            if (device is Touchscreen)
            {
                return UIInputDeviceType.Touch;
            }

            if (device is Pointer)
            {
                return UIInputDeviceType.Pointer;
            }

            return UIInputDeviceType.Other;
        }

        /// <summary>
        /// 입력 장치 상태를 변경 이벤트와 함께 갱신한다.
        /// </summary>
        private void UpdateInputDeviceState(UIInputDeviceType? deviceType = null)
        {
            UIInputDeviceType lastInputDevice = deviceType ?? _inputDeviceState.LastInputDevice;
            UIInputDeviceState nextState = new UIInputDeviceState(lastInputDevice, Gamepad.all.Count > 0);
            if (_inputDeviceState.Equals(nextState))
            {
                return;
            }

            _inputDeviceState = nextState;
            InputDeviceChanged?.Invoke(_inputDeviceState);
        }

        /// <summary>
        /// 실제 활성 UI 계층으로 입력 점유 상태를 계산한다.
        /// </summary>
        private void UpdateInputCaptureState()
        {
            BaseUI topUi = GetTopInputUI();
            UIInputCaptureReason reason = UIInputCaptureReason.None;
            if (topUi != null)
            {
                switch (topUi.Layer)
                {
                    case UILayer.Background:
                        reason = UIInputCaptureReason.Background;
                        break;
                    case UILayer.Screen:
                        reason = UIInputCaptureReason.Screen;
                        break;
                    case UILayer.Popup:
                        reason = UIInputCaptureReason.Popup;
                        break;
                    case UILayer.Overlay:
                        reason = UIInputCaptureReason.Overlay;
                        break;
                    case UILayer.System:
                        reason = UIInputCaptureReason.System;
                        break;
                }
            }

            UIInputCaptureState nextState = new UIInputCaptureState(
                topUi != null,
                reason,
                _screenStack.Count,
                _popupStack.Count);
            if (_inputCaptureState == nextState)
            {
                return;
            }

            _inputCaptureState = nextState;
            InputCaptureChanged?.Invoke(_inputCaptureState);
        }
    }
}

