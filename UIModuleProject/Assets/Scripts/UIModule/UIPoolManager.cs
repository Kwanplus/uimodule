using System.Collections.Generic;
using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// UI Pooling 시스템 매니저
    /// 모든 UI 풀을 통합 관리
    /// </summary>
    public class UIPoolManager : MonoBehaviour
    {
        private static UIPoolManager _instance;
        
        /// <summary>
        /// UIPoolManager 싱글톤 인스턴스
        /// </summary>
        public static UIPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("UIPoolManager");
                    _instance = go.AddComponent<UIPoolManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        // 타입별 풀 관리
        private Dictionary<System.Type, UIPool> _pools = new Dictionary<System.Type, UIPool>();
        
        // 풀 부모 Transform
        private Transform _poolRoot;
        
        // 풀 설정
        [Header("풀 설정")]
        [SerializeField] private int _defaultInitialSize = 1;
        [SerializeField] private int _defaultMaxSize = 10;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 초기화
        /// </summary>
        private void Initialize()
        {
            // 풀 루트 생성
            GameObject poolRootGO = new GameObject("PoolRoot");
            poolRootGO.transform.SetParent(transform);
            _poolRoot = poolRootGO.transform;
        }
        
        /// <summary>
        /// 풀 가져오기 또는 생성
        /// </summary>
        public UIPool GetOrCreatePool<T>(UILayer targetLayer) where T : BaseUI
        {
            System.Type uiType = typeof(T);
            
            // 이미 풀이 있으면 반환
            if (_pools.TryGetValue(uiType, out UIPool existingPool))
            {
                return existingPool;
            }
            
            // 프리팹 로드
            string prefabName = uiType.Name;
            string prefabPath = "UIPrefabs/" + prefabName;
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                Debug.LogWarning($"풀을 생성할 수 없습니다. 프리팹을 찾을 수 없습니다: {prefabPath}");
                return null;
            }
            
            // 레이어별 풀 부모 생성
            Canvas layerCanvas = UIManager.Instance.GetLayerCanvas(targetLayer);
            if (layerCanvas == null)
            {
                Debug.LogError($"레이어 Canvas를 찾을 수 없습니다: {targetLayer}");
                return null;
            }
            
            // 풀 부모 생성
            string poolParentName = $"{targetLayer}Pool";
            Transform poolParent = _poolRoot.Find(poolParentName);
            if (poolParent == null)
            {
                GameObject poolParentGO = new GameObject(poolParentName);
                poolParentGO.transform.SetParent(_poolRoot);
                poolParent = poolParentGO.transform;
            }
            
            // 새 풀 생성
            UIPool newPool = new UIPool(prefab, poolParent, _defaultInitialSize, _defaultMaxSize);
            _pools[uiType] = newPool;
            
            return newPool;
        }
        
        /// <summary>
        /// 풀에서 UI 인스턴스 가져오기
        /// </summary>
        public T GetFromPool<T>(UILayer targetLayer) where T : BaseUI
        {
            UIPool pool = GetOrCreatePool<T>(targetLayer);
            if (pool == null)
            {
                return null;
            }
            
            T instance = pool.Get<T>();
            if (instance != null && instance.gameObject != null)
            {
                // 레이어 Canvas 가져오기
                Canvas layerCanvas = UIManager.Instance.GetLayerCanvas(targetLayer);
                if (layerCanvas != null)
                {
                    // PoolRoot에서 레이어 Canvas로 강제 이동
                    if (instance.transform.parent != layerCanvas.transform)
                    {
                        instance.transform.SetParent(layerCanvas.transform, false);
                    }
                    
                    // RectTransform 설정 (Screen은 Stretch, Popup은 MiddleCenter)
                    RectTransform rectTransform = instance.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        if (targetLayer == UILayer.Screen)
                        {
                            // Screen: 전체 화면으로 설정
                            rectTransform.anchorMin = Vector2.zero;
                            rectTransform.anchorMax = Vector2.one;
                            rectTransform.sizeDelta = Vector2.zero;
                            rectTransform.anchoredPosition = Vector2.zero;
                        }
                        else if (targetLayer == UILayer.Popup)
                        {
                            // Popup: MiddleCenter로 설정 (프리팹에 설정이 없을 경우에만)
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
                        
                        // Scale 확인 및 수정
                        if (rectTransform.localScale == Vector3.zero)
                        {
                            rectTransform.localScale = Vector3.one;
                        }
                    }
                }
                else
                {
                    Debug.LogError($"레이어 Canvas를 찾을 수 없습니다: {targetLayer}");
                }
            }
            
            return instance;
        }
        
        /// <summary>
        /// UI 인스턴스를 풀로 반환
        /// </summary>
        public void ReturnToPool<T>(T instance) where T : BaseUI
        {
            if (instance == null || instance.gameObject == null)
            {
                return;
            }
            
            // 실제 인스턴스의 타입 사용 (제네릭 타입이 아닌)
            System.Type uiType = instance.GetType();
            if (_pools.TryGetValue(uiType, out UIPool pool))
            {
                pool.Return(instance);
            }
            else
            {
                // 풀이 없으면 제거 (풀이 생성되지 않았을 수 있음 - Pooling 미사용 시 등)
                Destroy(instance.gameObject);
            }
        }
        
        /// <summary>
        /// 특정 타입의 풀 제거
        /// </summary>
        public void ClearPool<T>() where T : BaseUI
        {
            System.Type uiType = typeof(T);
            if (_pools.TryGetValue(uiType, out UIPool pool))
            {
                pool.Clear();
                _pools.Remove(uiType);
            }
        }
        
        /// <summary>
        /// 모든 풀 제거
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
        }
        
        /// <summary>
        /// 풀 정보 가져오기 (디버그용)
        /// </summary>
        public Dictionary<System.Type, UIPool> GetAllPools()
        {
            return new Dictionary<System.Type, UIPool>(_pools);
        }
    }
}

