using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UIModule;

namespace UIModule.Editor
{
    /// <summary>
    /// Custom UI Dashboard - UI Screen과 Popup을 관리하는 Editor 툴
    /// </summary>
    public class CustomUIDashboard : EditorWindow
    {
        private const string DEFAULT_FOLDER_PATH = "Assets/Resources/UIPrefabs";
        private const string DEFAULT_SCREEN_SCRIPT_FOLDER = "Assets/Scripts/UIModule/Screen";
        private const string DEFAULT_POPUP_SCRIPT_FOLDER = "Assets/Scripts/UIModule/Popup";
        
        private const string PREF_KEY_FOLDER_PATH = "CustomUIDashboard_FolderPath";
        private const string PREF_KEY_SCREEN_SCRIPT_FOLDER = "CustomUIDashboard_ScreenScriptFolder";
        private const string PREF_KEY_POPUP_SCRIPT_FOLDER = "CustomUIDashboard_PopupScriptFolder";
        
        private string _targetFolderPath = DEFAULT_FOLDER_PATH;
        private string _screenScriptFolderPath = DEFAULT_SCREEN_SCRIPT_FOLDER;
        private string _popupScriptFolderPath = DEFAULT_POPUP_SCRIPT_FOLDER;
        
        private Vector2 _scrollPosition;
        private Vector2 _folderScrollPosition;
        
        // UI 생성 관련
        private string _newUIName = "";
        private bool _isCreatingScreen = true; // true: Screen, false: Popup
        
        // Popup 옵션 (팝업 선택 시에만 사용)
        private enum PopupCloseOnScreenChange
        {
            닫힘,
            남아있음
        }
        
        private enum PopupInstanceType
        {
            복수로열림,
            하나만존재
        }
        
        private PopupCloseOnScreenChange _popupCloseOnScreenChange = PopupCloseOnScreenChange.닫힘;
        private PopupInstanceType _popupInstanceType = PopupInstanceType.복수로열림;
        
        private List<UIInfo> _uiList = new List<UIInfo>();
        private bool _isRefreshing = false;
        
        // UI 생성 비동기 처리용
        private string _pendingClassName = null;
        private string _pendingScriptPath = null;
        private string _pendingPrefabFolderPath = null;
        private bool _isWaitingForCompilation = false;
        private double _compilationStartTime = 0;
        private const double COMPILATION_TIMEOUT = 30.0; // 30초 타임아웃
        
        private enum UIType
        {
            Screen,
            Popup,
            Unknown
        }
        
        private class UIInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public UIType Type { get; set; }
            public GameObject Prefab { get; set; }
            public BaseUI UIComponent { get; set; }
        }
        
        [MenuItem("Tools/Custom UI Dashboard")]
        public static void ShowWindow()
        {
            CustomUIDashboard window = GetWindow<CustomUIDashboard>("UI Dashboard");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        private void OnEnable()
        {
            // 저장된 폴더 경로 불러오기
            _targetFolderPath = EditorPrefs.GetString(PREF_KEY_FOLDER_PATH, DEFAULT_FOLDER_PATH);
            _screenScriptFolderPath = EditorPrefs.GetString(PREF_KEY_SCREEN_SCRIPT_FOLDER, DEFAULT_SCREEN_SCRIPT_FOLDER);
            _popupScriptFolderPath = EditorPrefs.GetString(PREF_KEY_POPUP_SCRIPT_FOLDER, DEFAULT_POPUP_SCRIPT_FOLDER);
            RefreshUIList();
            
            // EditorApplication.update 등록
            EditorApplication.update += OnUpdate;
        }
        
        private void OnDisable()
        {
            // EditorApplication.update 해제
            EditorApplication.update -= OnUpdate;
        }
        
        private void OnUpdate()
        {
            if (_isWaitingForCompilation)
            {
                // 타임아웃 체크
                if (EditorApplication.timeSinceStartup - _compilationStartTime > COMPILATION_TIMEOUT)
                {
                    EditorUtility.ClearProgressBar();
                    _isWaitingForCompilation = false;
                    
                    EditorUtility.DisplayDialog("타임아웃", 
                        $"스크립트 컴파일이 30초 내에 완료되지 않았습니다.\n\n" +
                        $"클래스 이름: {_pendingClassName}\n" +
                        $"스크립트 경로: {_pendingScriptPath}\n\n" +
                        $"콘솔에서 컴파일 에러를 확인해주세요.", 
                        "확인");
                    
                    _pendingClassName = null;
                    _pendingScriptPath = null;
                    _pendingPrefabFolderPath = null;
                    return;
                }
                
                // 컴파일 대기 중이고 컴파일이 완료되었을 때
                if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
                {
                    _isWaitingForCompilation = false;
                    EditorUtility.ClearProgressBar();
                    
                    // 컴파일 에러 확인
                    if (EditorUtility.scriptCompilationFailed)
                    {
                        EditorUtility.DisplayDialog("컴파일 에러", 
                            $"스크립트 컴파일에 실패했습니다.\n\n" +
                            $"클래스 이름: {_pendingClassName}\n" +
                            $"스크립트 경로: {_pendingScriptPath}\n\n" +
                            $"콘솔에서 컴파일 에러를 확인해주세요.\n" +
                            $"특히 패키지 참조가 올바른지 확인하세요.", 
                            "확인");
                        
                        _pendingClassName = null;
                        _pendingScriptPath = null;
                        _pendingPrefabFolderPath = null;
                        return;
                    }
                    
                    // 약간의 추가 대기 (타입이 로드될 시간)
                    EditorApplication.delayCall += () =>
                    {
                        System.Threading.Thread.Sleep(500);
                        CompletePrefabCreation();
                    };
                }
            }
        }
        
        private void OnDestroy()
        {
            // 폴더 경로 저장
            SaveFolderPaths();
        }
        
        /// <summary>
        /// 폴더 경로를 EditorPrefs에 저장
        /// </summary>
        private void SaveFolderPaths()
        {
            EditorPrefs.SetString(PREF_KEY_FOLDER_PATH, _targetFolderPath);
            EditorPrefs.SetString(PREF_KEY_SCREEN_SCRIPT_FOLDER, _screenScriptFolderPath);
            EditorPrefs.SetString(PREF_KEY_POPUP_SCRIPT_FOLDER, _popupScriptFolderPath);
            
            // ScriptableObject에도 저장 (런타임 사용을 위해)
            SaveToScriptableObject();
        }
        
        /// <summary>
        /// UIModuleSettings ScriptableObject에 설정 저장
        /// </summary>
        private void SaveToScriptableObject()
        {
            // Resources 경로로 변환
            string resourcesPath = ConvertToResourcesPath(_targetFolderPath);
            if (resourcesPath == null)
            {
                Debug.LogWarning($"[CustomUIDashboard] 기준 폴더가 Resources 폴더 내에 있어야 합니다.\n" +
                    $"현재 경로: {_targetFolderPath}\n" +
                    $"예시: Assets/Resources/UIPrefabs");
                return;
            }
            
            // UIModuleSettings 에셋 찾기 또는 생성
            string settingsAssetPath = "Assets/Resources/UIModuleSettings.asset";
            UIModuleSettings settings = AssetDatabase.LoadAssetAtPath<UIModuleSettings>(settingsAssetPath);
            
            if (settings == null)
            {
                // Resources 폴더 확인 및 생성
                string resourcesFolder = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                
                // ScriptableObject 생성
                settings = ScriptableObject.CreateInstance<UIModuleSettings>();
                AssetDatabase.CreateAsset(settings, settingsAssetPath);
                Debug.Log($"[CustomUIDashboard] UIModuleSettings.asset이 생성되었습니다: {settingsAssetPath}");
            }
            
            // 설정 업데이트
            settings.UpdateSettings(_targetFolderPath, resourcesPath);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            
            // 캐시 초기화 (런타임에서 새로운 값을 읽도록)
            UIModuleSettings.ClearCache();
            
            Debug.Log($"[CustomUIDashboard] UIModuleSettings 업데이트 완료\n" +
                $"Assets 경로: {_targetFolderPath}\n" +
                $"Resources 경로: {resourcesPath}");
        }
        
        /// <summary>
        /// Assets 경로를 Resources 경로로 변환
        /// 예: "Assets/Resources/UIPrefabs" → "UIPrefabs/"
        /// 예: "Assets/Resources" → "" (루트)
        /// </summary>
        private string ConvertToResourcesPath(string assetsPath)
        {
            if (string.IsNullOrEmpty(assetsPath))
            {
                return null;
            }
            
            // "Assets/Resources" 정확히 일치하는 경우 (루트 폴더 선택)
            if (assetsPath == "Assets/Resources" || assetsPath == "Assets/Resources/")
            {
                return ""; // Resources 루트 폴더
            }
            
            // "Assets/Resources/" 이후의 경로 추출
            const string resourcesMarker = "Resources/";
            int resourcesIndex = assetsPath.IndexOf(resourcesMarker);
            
            if (resourcesIndex >= 0)
            {
                string relativePath = assetsPath.Substring(resourcesIndex + resourcesMarker.Length);
                // 빈 문자열이면 루트, 아니면 끝에 슬래시 추가
                if (string.IsNullOrEmpty(relativePath))
                {
                    return ""; // Resources 루트 폴더
                }
                return relativePath.EndsWith("/") ? relativePath : relativePath + "/";
            }
            
            // Resources 폴더 내에 없으면 null 반환
            return null;
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            // 상단: 폴더 선택 및 새로고침
            DrawHeader();
            
            EditorGUILayout.Space(5);
            
            // UI 생성 섹션
            DrawCreateUISection();
            
            EditorGUILayout.Space(5);
            
            // 중앙: 리스트 표시
            DrawUIList();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 헤더 영역 (폴더 선택, 새로고침 버튼)
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("UI Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("기준 폴더:", GUILayout.Width(80));
            
            EditorGUILayout.BeginVertical();
            _folderScrollPosition = EditorGUILayout.BeginScrollView(
                new Vector2(_folderScrollPosition.x, 0),
                GUILayout.Height(18)
            );
            _targetFolderPath = EditorGUILayout.TextField(_targetFolderPath);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("폴더 선택", GUILayout.Width(80)))
            {
                // Assets 폴더부터 시작하도록 설정
                string startPath = Application.dataPath;
                if (!string.IsNullOrEmpty(_targetFolderPath) && _targetFolderPath.StartsWith("Assets/"))
                {
                    // 상대 경로를 절대 경로로 변환
                    string fullPath = _targetFolderPath.Replace("Assets", Application.dataPath);
                    if (System.IO.Directory.Exists(fullPath))
                    {
                        startPath = fullPath;
                    }
                }
                
                string selectedPath = EditorUtility.OpenFolderPanel("UI 프리팹 폴더 선택", startPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 절대 경로를 Assets 상대 경로로 변환
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        _targetFolderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        SaveFolderPaths(); // 즉시 저장
                        // GUI 이벤트 종료 후 지연 호출로 새로고침 (Begin/End 불일치 방지)
                        EditorApplication.delayCall += () =>
                        {
                            RefreshUIList();
                            Repaint();
                        };
                        GUIUtility.ExitGUI();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("오류", "Assets 폴더 내의 경로만 선택할 수 있습니다.", "확인");
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("새로고침", GUILayout.Width(100)))
            {
                // GUI 이벤트 종료 후 지연 호출로 새로고침 (Begin/End 불일치 방지)
                EditorApplication.delayCall += () =>
                {
                    RefreshUIList();
                    Repaint();
                };
                GUIUtility.ExitGUI();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// UI 리스트 표시
        /// </summary>
        private void DrawUIList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField($"UI 목록 ({_uiList.Count}개)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 스크롤뷰는 항상 Begin/End 쌍으로 실행 (GUI 에러 방지)
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_isRefreshing)
            {
                EditorGUILayout.HelpBox("UI 목록을 불러오는 중...", MessageType.Info);
            }
            else if (_uiList.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "UI를 찾을 수 없습니다.\n" +
                    "기준 폴더에 BaseScreen 또는 BasePopup을 상속받은 컴포넌트가 있는 프리팹이 있는지 확인하세요.",
                    MessageType.Warning
                );
            }
            else
            {
                // Screen과 Popup 분리하여 표시
                var screens = _uiList.Where(ui => ui.Type == UIType.Screen).ToList();
                var popups = _uiList.Where(ui => ui.Type == UIType.Popup).ToList();
                
                // Screen 섹션
                if (screens.Count > 0)
                {
                    EditorGUILayout.LabelField($"Screen ({screens.Count}개)", EditorStyles.boldLabel);
                    EditorGUILayout.Space(3);
                    
                    foreach (var screen in screens)
                    {
                        DrawUIItem(screen);
                    }
                    
                    EditorGUILayout.Space(10);
                }
                
                // Popup 섹션
                if (popups.Count > 0)
                {
                    EditorGUILayout.LabelField($"Popup ({popups.Count}개)", EditorStyles.boldLabel);
                    EditorGUILayout.Space(3);
                    
                    foreach (var popup in popups)
                    {
                        DrawUIItem(popup);
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 개별 UI 아이템 표시
        /// </summary>
        private void DrawUIItem(UIInfo uiInfo)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            // 타입 아이콘/라벨
            string typeLabel = uiInfo.Type == UIType.Screen ? "[Screen]" : "[Popup]";
            Color originalColor = GUI.color;
            GUI.color = uiInfo.Type == UIType.Screen ? Color.cyan : Color.yellow;
            EditorGUILayout.LabelField(typeLabel, GUILayout.Width(70));
            GUI.color = originalColor;
            
            // 이름
            EditorGUILayout.LabelField(uiInfo.Name, EditorStyles.boldLabel, GUILayout.Width(200));
            
            GUILayout.FlexibleSpace();
            
            // 경로 (축약)
            string shortPath = uiInfo.Path;
            if (shortPath.Length > 40)
            {
                shortPath = "..." + shortPath.Substring(shortPath.Length - 37);
            }
            EditorGUILayout.LabelField(shortPath, EditorStyles.miniLabel, GUILayout.Width(150));
            
            // 프리팹 선택 버튼
            if (GUILayout.Button("선택", GUILayout.Width(50)))
            {
                Selection.activeObject = uiInfo.Prefab;
                EditorGUIUtility.PingObject(uiInfo.Prefab);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// UI 리스트 새로고침
        /// </summary>
        private void RefreshUIList()
        {
            _isRefreshing = true;
            _uiList.Clear();
            
            if (string.IsNullOrEmpty(_targetFolderPath) || !Directory.Exists(_targetFolderPath))
            {
                _isRefreshing = false;
                return;
            }
            
            // 프리팹 파일 찾기 (기준 폴더만 검색, 하위 폴더 제외)
            // 주의: UIPoolManager에서 "prefabPathPrefix + 클래스이름"으로 로드하므로
            //       프리팹은 기준 폴더에 직접 배치해야 함
            string[] prefabPaths = Directory.GetFiles(_targetFolderPath, "*.prefab", SearchOption.TopDirectoryOnly)
                .Select(path => path.Replace('\\', '/'))
                .Where(path => path.StartsWith("Assets/"))
                .ToArray();
            
            foreach (string prefabPath in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;
                
                // 프리팹에서 BaseUI를 상속받은 컴포넌트 찾기
                BaseUI[] uiComponents = prefab.GetComponentsInChildren<BaseUI>(true);
                
                foreach (BaseUI uiComponent in uiComponents)
                {
                    UIType uiType = DetermineUIType(uiComponent);
                    
                    UIInfo uiInfo = new UIInfo
                    {
                        Name = prefab.name,
                        Path = prefabPath,
                        Type = uiType,
                        Prefab = prefab,
                        UIComponent = uiComponent
                    };
                    
                    _uiList.Add(uiInfo);
                }
            }
            
            // 이름순으로 정렬
            _uiList = _uiList.OrderBy(ui => ui.Type).ThenBy(ui => ui.Name).ToList();
            
            _isRefreshing = false;
            Repaint();
        }
        
        /// <summary>
        /// UI 타입 결정 (Screen인지 Popup인지)
        /// </summary>
        private UIType DetermineUIType(BaseUI uiComponent)
        {
            if (uiComponent is BaseScreen)
            {
                return UIType.Screen;
            }
            else if (uiComponent is BasePopup)
            {
                return UIType.Popup;
            }
            else
            {
                return UIType.Unknown;
            }
        }
        
        /// <summary>
        /// UI 생성 섹션
        /// </summary>
        private void DrawCreateUISection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("새 UI 만들기", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // 이름 입력
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("이름:", GUILayout.Width(80));
            _newUIName = EditorGUILayout.TextField(_newUIName);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(3);
            
            // 타입 선택
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("타입:", GUILayout.Width(80));
            _isCreatingScreen = EditorGUILayout.ToggleLeft("Screen", _isCreatingScreen, GUILayout.Width(80));
            _isCreatingScreen = !EditorGUILayout.ToggleLeft("Popup", !_isCreatingScreen, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Popup 옵션 (팝업 선택 시에만 표시)
            if (!_isCreatingScreen)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("팝업 옵션", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                // 스크린 이동시 닫힘/남아있음 옵션
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("스크린 이동시:", GUILayout.Width(100));
                _popupCloseOnScreenChange = (PopupCloseOnScreenChange)EditorGUILayout.EnumPopup(_popupCloseOnScreenChange);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(3);
                
                // 같은 종류의 팝업이 하나만 존재할 수 있는지 옵션
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("팝업 타입:", GUILayout.Width(100));
                _popupInstanceType = (PopupInstanceType)EditorGUILayout.EnumPopup(_popupInstanceType);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            // 스크립트 폴더 경로 설정
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("스크립트 폴더:", GUILayout.Width(100));
            string scriptFolder = _isCreatingScreen ? _screenScriptFolderPath : _popupScriptFolderPath;
            scriptFolder = EditorGUILayout.TextField(scriptFolder);
            if (_isCreatingScreen)
            {
                _screenScriptFolderPath = scriptFolder;
            }
            else
            {
                _popupScriptFolderPath = scriptFolder;
            }
            if (GUILayout.Button("선택", GUILayout.Width(50)))
            {
                string currentPath = _isCreatingScreen ? _screenScriptFolderPath : _popupScriptFolderPath;
                
                // Assets 폴더부터 시작하도록 설정
                string startPath = Application.dataPath;
                if (!string.IsNullOrEmpty(currentPath) && currentPath.StartsWith("Assets/"))
                {
                    // 상대 경로를 절대 경로로 변환
                    string fullPath = currentPath.Replace("Assets", Application.dataPath);
                    if (System.IO.Directory.Exists(fullPath))
                    {
                        startPath = fullPath;
                    }
                }
                
                string path = EditorUtility.OpenFolderPanel("스크립트 폴더 선택", startPath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    string newPath = "Assets" + path.Substring(Application.dataPath.Length);
                    if (_isCreatingScreen)
                    {
                        _screenScriptFolderPath = newPath;
                    }
                    else
                    {
                        _popupScriptFolderPath = newPath;
                    }
                    SaveFolderPaths(); // 즉시 저장
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 만들기 버튼
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_newUIName));
            if (GUILayout.Button("만들기", GUILayout.Height(30)))
            {
                CreateNewUI();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 새 UI 생성
        /// </summary>
        private void CreateNewUI()
        {
            // 만들기 전에 현재 입력된 경로를 저장
            SaveFolderPaths();
            
            if (string.IsNullOrEmpty(_newUIName))
            {
                EditorUtility.DisplayDialog("오류", "이름을 입력해주세요.", "확인");
                return;
            }
            
            // 이름 검증 (C# 클래스 이름 규칙)
            string className = _newUIName.Trim();
            if (!IsValidClassName(className))
            {
                EditorUtility.DisplayDialog("오류", "유효하지 않은 이름입니다.\n영문자, 숫자, 언더스코어만 사용 가능하며 숫자로 시작할 수 없습니다.", "확인");
                return;
            }
            
            // 현재 입력된 경로 사용 (기준 폴더 = 프리팹 폴더, 스크립트 폴더는 타입에 따라)
            string prefabFolder = _targetFolderPath;
            string scriptFolder = _isCreatingScreen ? _screenScriptFolderPath : _popupScriptFolderPath;
            
            // 폴더 존재 확인 및 생성
            if (!Directory.Exists(prefabFolder))
            {
                Directory.CreateDirectory(prefabFolder);
                AssetDatabase.Refresh();
            }
            
            if (!Directory.Exists(scriptFolder))
            {
                Directory.CreateDirectory(scriptFolder);
                AssetDatabase.Refresh();
            }
            
            // 1. 스크립트 생성
            string scriptPath = CreateScript(className, scriptFolder);
            if (string.IsNullOrEmpty(scriptPath))
            {
                EditorUtility.DisplayDialog("오류", "스크립트 생성에 실패했습니다.", "확인");
                return;
            }
            
            // 스크립트 컴파일 대기 (비동기 처리)
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            // 비동기로 컴파일 완료 대기
            _pendingClassName = className;
            _pendingScriptPath = scriptPath;
            _pendingPrefabFolderPath = prefabFolder;
            _isWaitingForCompilation = true;
            _compilationStartTime = EditorApplication.timeSinceStartup;
            
            if (EditorApplication.isCompiling)
            {
                // 컴파일 중이면 OnUpdate에서 처리
                EditorUtility.DisplayProgressBar("UI 생성 중", "스크립트 컴파일 대기 중...", 0.5f);
            }
            else
            {
                // 컴파일이 완료되어 있으면 바로 처리
                EditorApplication.delayCall += () =>
                {
                    System.Threading.Thread.Sleep(500);
                    EditorUtility.ClearProgressBar();
                    CompletePrefabCreation();
                };
            }
        }
        
        /// <summary>
        /// 프리팹 생성 완료 (비동기 처리)
        /// </summary>
        private void CompletePrefabCreation()
        {
            try
            {
                EditorUtility.ClearProgressBar();
                
                if (string.IsNullOrEmpty(_pendingClassName) || string.IsNullOrEmpty(_pendingScriptPath))
                {
                    return;
                }
                
                string className = _pendingClassName;
                string scriptPath = _pendingScriptPath;
                
                // 컴파일 에러 재확인
                if (EditorUtility.scriptCompilationFailed)
                {
                    EditorUtility.DisplayDialog("컴파일 에러", 
                        $"스크립트 컴파일에 실패했습니다.\n\n" +
                        $"클래스 이름: {className}\n" +
                        $"스크립트 경로: {scriptPath}\n\n" +
                        $"콘솔에서 컴파일 에러를 확인해주세요.\n" +
                        $"특히 패키지 참조가 올바른지 확인하세요.", 
                        "확인");
                    
                    _pendingClassName = null;
                    _pendingScriptPath = null;
                    _pendingPrefabFolderPath = null;
                    return;
                }
                
                // 타입 찾기 (여러 번 시도)
                System.Type scriptType = null;
                for (int i = 0; i < 5; i++)
                {
                    scriptType = FindScriptType(className, scriptPath);
                    if (scriptType != null)
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(200);
                }
                
                if (scriptType == null)
                {
                    EditorUtility.DisplayDialog("오류", 
                        $"스크립트 타입을 찾을 수 없습니다.\n\n" +
                        $"클래스 이름: {className}\n" +
                        $"스크립트 경로: {scriptPath}\n\n" +
                        $"가능한 원인:\n" +
                        $"1. 컴파일 에러가 있습니다 (콘솔 확인)\n" +
                        $"2. 패키지 참조가 올바르지 않습니다\n" +
                        $"3. asmdef 파일이 올바르게 설정되지 않았습니다\n\n" +
                        $"Unity를 재시작하거나 수동으로 스크립트를 확인해주세요.", 
                        "확인");
                    
                    _pendingClassName = null;
                    _pendingScriptPath = null;
                    _pendingPrefabFolderPath = null;
                    return;
                }
                
                // 프리팹 생성
                string prefabPath = CreatePrefab(className, scriptType, _pendingPrefabFolderPath);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    EditorUtility.DisplayDialog("오류", "프리팹 생성에 실패했습니다.", "확인");
                    return;
                }
                
                // 자동 새로고침
                RefreshUIList();
                
                // 생성된 프리팹 선택
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
                
                // 성공 메시지 (콘솔에 로그 출력)
                Debug.Log($"<color=green>✓</color> {className} UI가 성공적으로 생성되었습니다! (프리팹: {prefabPath})");
                
                EditorUtility.DisplayDialog("성공", $"{className} UI가 성공적으로 생성되었습니다!", "확인");
                
                // 입력 필드 초기화
                _newUIName = "";
            }
            finally
            {
                // 대기 상태 초기화
                _pendingClassName = null;
                _pendingScriptPath = null;
                _pendingPrefabFolderPath = null;
                EditorUtility.ClearProgressBar();
            }
        }
        
        /// <summary>
        /// 스크립트 타입 찾기 (간소화된 버전)
        /// </summary>
        private System.Type FindScriptType(string className, string scriptPath)
        {
            // 방법 1: 직접 경로로 MonoScript 로드
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script != null)
            {
                System.Type scriptType = script.GetClass();
                if (scriptType != null && scriptType.Name == className)
                {
                    return scriptType;
                }
            }
            
            // 방법 2: 모든 Assembly에서 검색
            System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    // UIModule 네임스페이스로 시도
                    System.Type scriptType = assembly.GetType($"UIModule.{className}");
                    if (scriptType != null)
                    {
                        return scriptType;
                    }
                    
                    // 네임스페이스 없이도 시도
                    scriptType = assembly.GetType(className);
                    if (scriptType != null && scriptType.Name == className)
                    {
                        return scriptType;
                    }
                }
                catch
                {
                    // 무시
                }
            }
            
            // 방법 3: 모든 타입을 검색
            foreach (var assembly in assemblies)
            {
                try
                {
                    System.Type[] types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.Name == className && 
                            (type.IsSubclassOf(typeof(BaseScreen)) || type.IsSubclassOf(typeof(BasePopup))))
                        {
                            return type;
                        }
                    }
                }
                catch
                {
                    // 무시
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 유효한 클래스 이름인지 확인
        /// </summary>
        private bool IsValidClassName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (char.IsDigit(name[0])) return false;
            
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 스크립트 생성
        /// </summary>
        private string CreateScript(string className, string folderPath)
        {
            string scriptPath = Path.Combine(folderPath, $"{className}.cs").Replace('\\', '/');
            
            if (File.Exists(scriptPath))
            {
                if (!EditorUtility.DisplayDialog("파일 존재", $"{className}.cs 파일이 이미 존재합니다. 덮어쓰시겠습니까?", "예", "아니오"))
                {
                    return null;
                }
            }
            
            string scriptContent = GenerateScriptContent(className, _isCreatingScreen);
            
            try
            {
                File.WriteAllText(scriptPath, scriptContent, Encoding.UTF8);
                return scriptPath;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"스크립트 생성 실패: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 스크립트 내용 생성
        /// </summary>
        private string GenerateScriptContent(string className, bool isScreen)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UIModule;");
            sb.AppendLine();
            sb.AppendLine("namespace UIModule");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {className} {(isScreen ? "Screen" : "Popup")}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {className} : {(isScreen ? "BaseScreen" : "BasePopup")}");
            sb.AppendLine("    {");
            sb.AppendLine("        // UI 요소 참조");
            sb.AppendLine("        [SerializeField] private UIButton _buttonConfirm;");
            if (isScreen)
            {
                sb.AppendLine("        [SerializeField] private UIButton _buttonBack;");
            }
            else
            {
                sb.AppendLine("        [SerializeField] private UIButton _buttonClose;");
            }
            sb.AppendLine();
            
            if (isScreen)
            {
                sb.AppendLine("        protected override void OnScreenInitialize()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 버튼 클릭 이벤트 등록");
                sb.AppendLine("            if (_buttonConfirm != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                _buttonConfirm.OnClick += OnButtonConfirmClicked;");
                sb.AppendLine("            }");
                sb.AppendLine("            ");
                sb.AppendLine("            if (_buttonBack != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                _buttonBack.OnClick += OnButtonBackClicked;");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            // 초기화 로직");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        protected override void OnScreenShow()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 표시 시 로직");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        protected override void OnScreenHide()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 숨김 시 로직");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        protected override void OnScreenDestroy()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 버튼 클릭 이벤트 해제");
                sb.AppendLine("            if (_buttonConfirm != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                _buttonConfirm.OnClick -= OnButtonConfirmClicked;");
                sb.AppendLine("            }");
                sb.AppendLine("            ");
                sb.AppendLine("            if (_buttonBack != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                _buttonBack.OnClick -= OnButtonBackClicked;");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            // 제거 시 로직");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// 확인 버튼 클릭 처리");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        private void OnButtonConfirmClicked()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 확인 버튼 로직");
                sb.AppendLine("            Debug.Log(\"Confirm button clicked\");");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// 뒤로가기 버튼 클릭 처리");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        private void OnButtonBackClicked()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 뒤로가기 버튼 로직");
                sb.AppendLine("            if (UIManager.Instance != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                UIManager.Instance.BackScreen();");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
            else
            {
                sb.AppendLine("        protected override void OnPopupInitialize()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 버튼 클릭 이벤트 등록");
                sb.AppendLine("            if (_buttonConfirm != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                _buttonConfirm.OnClick += OnButtonConfirmClicked;");
                sb.AppendLine("            }");
                sb.AppendLine("            ");
                sb.AppendLine("            if (_buttonClose != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                _buttonClose.OnClick += OnButtonCloseClicked;");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            // 초기화 로직");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        protected override void OnPopupShow()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 표시 시 로직");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        protected override void OnPopupHide()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 숨김 시 로직");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        protected override void OnPopupDestroy()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 버튼 클릭 이벤트 해제");
                sb.AppendLine("            if (_buttonConfirm != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                _buttonConfirm.OnClick -= OnButtonConfirmClicked;");
                sb.AppendLine("            }");
                sb.AppendLine("            ");
                sb.AppendLine("            if (_buttonClose != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                _buttonClose.OnClick -= OnButtonCloseClicked;");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            // 제거 시 로직");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// 확인 버튼 클릭 처리");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        private void OnButtonConfirmClicked()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 확인 버튼 로직");
                sb.AppendLine("            Close();");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// 닫기 버튼 클릭 처리");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        private void OnButtonCloseClicked()");
                sb.AppendLine("        {");
                sb.AppendLine("            // 닫기 버튼 로직");
                sb.AppendLine("            Close();");
                sb.AppendLine("        }");
            }
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
        
        
        /// <summary>
        /// 프리팹 생성
        /// </summary>
        private string CreatePrefab(string className, System.Type scriptType, string prefabFolderPath)
        {
            if (scriptType == null)
            {
                Debug.LogError($"스크립트 타입이 null입니다: {className}");
                return null;
            }
            
            // GameObject 생성 (Canvas는 UIManager의 레이어 Canvas를 사용하므로 포함하지 않음)
            GameObject prefabGO = new GameObject(className);
            
            // RectTransform 설정
            RectTransform rectTransform = prefabGO.AddComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;
            
            // Screen은 Stretch, Popup은 MiddleCenter로 설정
            if (_isCreatingScreen)
            {
                // Screen: 전체 화면으로 설정
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                // Popup: MiddleCenter로 설정
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(400, 300); // 기본 크기
                rectTransform.anchoredPosition = Vector2.zero;
            }
            
            // BG 이미지 추가
            GameObject bgGO = new GameObject("BG");
            bgGO.transform.SetParent(prefabGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            UnityEngine.UI.Image bgImage = bgGO.AddComponent<UnityEngine.UI.Image>();
            
            if (_isCreatingScreen)
            {
                // Screen: 전체 화면, 어두운 그레이
                bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                bgRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                // Popup: 400x300 크기, 밝은 그레이
                bgImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                bgRect.anchoredPosition = Vector2.zero;
            }
            
            // BG를 맨 뒤로 보내기 (SetAsFirstSibling)
            bgGO.transform.SetAsFirstSibling();
            
            // 스크립트 컴포넌트 추가
            Component uiComponent = prefabGO.AddComponent(scriptType);
            
            // 버튼 위치 설정 (Screen은 하단 중앙, Popup은 하단)
            Vector2 confirmButtonPos, secondButtonPos;
            string secondButtonName;
            if (_isCreatingScreen)
            {
                // Screen: 하단 중앙에 배치, 뒤로가기 버튼
                confirmButtonPos = new Vector2(-70, -400);
                secondButtonPos = new Vector2(70, -400);
                secondButtonName = "ButtonBack";
            }
            else
            {
                // Popup: 하단에 배치, 닫기 버튼
                confirmButtonPos = new Vector2(-70, -120);
                secondButtonPos = new Vector2(70, -120);
                secondButtonName = "ButtonClose";
            }
            
            // 확인 버튼 생성 (왼쪽)
            GameObject confirmButtonGO = CreateButton("ButtonConfirm", "Confirm", prefabGO.transform, confirmButtonPos);
            
            // 두 번째 버튼 생성 (오른쪽)
            string secondButtonTextEnglish = _isCreatingScreen ? "Back" : "Close";
            GameObject secondButtonGO = CreateButton(secondButtonName, secondButtonTextEnglish, prefabGO.transform, secondButtonPos);
            
            // 프리팹으로 저장
            string prefabPath = Path.Combine(prefabFolderPath, $"{className}.prefab").Replace('\\', '/');
            
            // 프리팹 생성
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabGO, prefabPath);
            
            // 프리팹 에셋에서 버튼 참조 연결
            if (prefab != null)
            {
                Component prefabComponent = prefab.GetComponent(scriptType);
                if (prefabComponent != null)
                {
                    SerializedObject serializedObject = new SerializedObject(prefabComponent);
                    
                    // 확인 버튼 참조 연결
                    SerializedProperty confirmButtonProp = serializedObject.FindProperty("_buttonConfirm");
                    if (confirmButtonProp != null)
                    {
                        GameObject prefabConfirmButton = prefab.transform.Find("ButtonConfirm")?.gameObject;
                        if (prefabConfirmButton != null)
                        {
                            confirmButtonProp.objectReferenceValue = prefabConfirmButton.GetComponent<UIButton>();
                        }
                    }
                    
                    // 두 번째 버튼 참조 연결 (Screen: _buttonBack, Popup: _buttonClose)
                    string secondButtonPropertyName = _isCreatingScreen ? "_buttonBack" : "_buttonClose";
                    string secondButtonObjectName = _isCreatingScreen ? "ButtonBack" : "ButtonClose";
                    SerializedProperty secondButtonProp = serializedObject.FindProperty(secondButtonPropertyName);
                    if (secondButtonProp != null)
                    {
                        GameObject prefabSecondButton = prefab.transform.Find(secondButtonObjectName)?.gameObject;
                        if (prefabSecondButton != null)
                        {
                            secondButtonProp.objectReferenceValue = prefabSecondButton.GetComponent<UIButton>();
                        }
                    }
                    
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(prefab);
                    AssetDatabase.SaveAssets();
                }
            }
            
            // 임시 GameObject 제거
            DestroyImmediate(prefabGO);
            
            // Popup인 경우 프리팹 에셋에 옵션 값 설정 (프리팹에 Serialize되도록)
            if (!_isCreatingScreen && prefab != null)
            {
                BasePopup popupComponent = prefab.GetComponent<BasePopup>();
                if (popupComponent != null)
                {
                    // 프리팹 에셋의 SerializedObject를 사용하여 값 설정
                    SerializedObject prefabSerializedObject = new SerializedObject(popupComponent);
                    
                    // 옵션 값 설정
                    bool closeOnScreenChange = _popupCloseOnScreenChange == PopupCloseOnScreenChange.닫힘;
                    bool isSingleton = _popupInstanceType == PopupInstanceType.하나만존재;
                    
                    SerializedProperty closeOnScreenChangeProp = prefabSerializedObject.FindProperty("_closeOnScreenChange");
                    SerializedProperty isSingletonProp = prefabSerializedObject.FindProperty("_isSingleton");
                    
                    if (closeOnScreenChangeProp != null)
                    {
                        closeOnScreenChangeProp.boolValue = closeOnScreenChange;
                    }
                    if (isSingletonProp != null)
                    {
                        isSingletonProp.boolValue = isSingleton;
                    }
                    
                    prefabSerializedObject.ApplyModifiedProperties();
                    
                    // 프리팹 에셋 저장
                    EditorUtility.SetDirty(prefab);
                    AssetDatabase.SaveAssets();
                }
            }
            
            AssetDatabase.Refresh();
            
            return prefabPath;
        }
        
        /// <summary>
        /// 버튼 GameObject 생성
        /// </summary>
        private GameObject CreateButton(string buttonName, string buttonText, Transform parent, Vector2 position)
        {
            // 버튼 GameObject 생성
            GameObject buttonGO = new GameObject(buttonName);
            buttonGO.transform.SetParent(parent, false);
            
            // RectTransform 설정
            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(120, 40);
            buttonRect.anchoredPosition = position;
            
            // Image 컴포넌트 추가 (버튼 배경)
            UnityEngine.UI.Image buttonImage = buttonGO.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f); // 파란색 배경
            
            // Button 컴포넌트 추가
            UnityEngine.UI.Button button = buttonGO.AddComponent<UnityEngine.UI.Button>();
            
            // 텍스트용 자식 GameObject 생성
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            // TextMeshProUGUI 컴포넌트 추가
            TMPro.TextMeshProUGUI text = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = buttonText;
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            
            // UIButton 컴포넌트 추가
            UIButton uiButton = buttonGO.AddComponent<UIButton>();
            
            return buttonGO;
        }
    }
}

