using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// UI 모듈 설정을 저장하는 ScriptableObject
    /// Resources 폴더에 저장되어 런타임에서 로드 가능
    /// </summary>
    public class UIModuleSettings : ScriptableObject
    {
        private const string SETTINGS_PATH = "UIModuleSettings";
        private static UIModuleSettings _instance;
        
        /// <summary>
        /// 싱글톤 인스턴스 (Resources에서 로드)
        /// </summary>
        public static UIModuleSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<UIModuleSettings>(SETTINGS_PATH);
                    
                    if (_instance == null)
                    {
                        // UI Dashboard에서 폴더 설정을 변경하면 자동으로 생성됨
                    }
                }
                return _instance;
            }
        }
        
        [Header("프리팹 설정")]
        [Tooltip("Resources 폴더 기준 프리팹 경로 (예: UIPrefabs/)")]
        [SerializeField] private string _prefabPathPrefix = "UIPrefabs/";
        
        [Header("원본 경로 (참고용)")]
        [Tooltip("Assets 기준 전체 경로 (에디터 전용)")]
        [SerializeField] private string _assetsFolderPath = "Assets/Resources/UIPrefabs";
        
        /// <summary>
        /// Resources 폴더 기준 프리팹 경로 접두사
        /// 빈 문자열("")은 Resources 루트 폴더를 의미
        /// </summary>
        public string PrefabPathPrefix
        {
            get
            {
                // null인 경우에만 기본값 반환 (빈 문자열은 유효한 값 - Resources 루트)
                if (_prefabPathPrefix == null)
                {
                    return "UIPrefabs/";
                }
                // 빈 문자열이면 그대로 반환 (Resources 루트 폴더)
                if (_prefabPathPrefix == "")
                {
                    return "";
                }
                return _prefabPathPrefix.EndsWith("/") ? _prefabPathPrefix : _prefabPathPrefix + "/";
            }
        }
        
        /// <summary>
        /// Assets 기준 전체 폴더 경로
        /// </summary>
        public string AssetsFolderPath => _assetsFolderPath;
        
        /// <summary>
        /// 설정 업데이트 (에디터에서만 호출)
        /// </summary>
        public void UpdateSettings(string assetsFolderPath, string prefabPathPrefix)
        {
            _assetsFolderPath = assetsFolderPath;
            _prefabPathPrefix = prefabPathPrefix;
        }
        
        /// <summary>
        /// 캐시된 인스턴스 초기화 (에디터에서 재로드 시 사용)
        /// </summary>
        public static void ClearCache()
        {
            _instance = null;
        }
    }
}

