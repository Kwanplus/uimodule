using System.Collections.Generic;
using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// 개별 UI 타입별 풀 관리 클래스
    /// </summary>
    public class UIPool
    {
        private GameObject _prefab;
        private Transform _parent;
        private Queue<BaseUI> _availablePool = new Queue<BaseUI>();
        private HashSet<BaseUI> _activeInstances = new HashSet<BaseUI>();
        private int _initialSize;
        private int _maxSize;
        
        /// <summary>
        /// 풀 이름 (프리팹 이름)
        /// </summary>
        public string PoolName => _prefab != null ? _prefab.name : "Unknown";
        
        /// <summary>
        /// 사용 가능한 풀 크기
        /// </summary>
        public int AvailableCount => _availablePool.Count;
        
        /// <summary>
        /// 활성화된 인스턴스 개수
        /// </summary>
        public int ActiveCount => _activeInstances.Count;
        
        /// <summary>
        /// 전체 인스턴스 개수
        /// </summary>
        public int TotalCount => _availablePool.Count + _activeInstances.Count;
        
        public UIPool(GameObject prefab, Transform parent, int initialSize = 1, int maxSize = 10)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;
            _maxSize = maxSize;
            
            // 초기 풀 생성 (초기화 시에만 _availablePool에 추가)
            for (int i = 0; i < _initialSize; i++)
            {
                BaseUI instance = CreateNewInstance();
                if (instance != null)
                {
                    _availablePool.Enqueue(instance);
                }
            }
        }
        
        /// <summary>
        /// 풀에서 UI 인스턴스 가져오기
        /// </summary>
        public T Get<T>() where T : BaseUI
        {
            BaseUI instance = null;
            
            // 사용 가능한 인스턴스가 있으면 재사용
            if (_availablePool.Count > 0)
            {
                instance = _availablePool.Dequeue();
            }
            // 풀이 최대 크기보다 작으면 새로 생성
            else if (TotalCount < _maxSize)
            {
                instance = CreateNewInstance();
            }
            // 최대 크기에 도달했으면 null 반환
            else
            {
                Debug.LogWarning($"풀 {PoolName}이 최대 크기에 도달했습니다. ({_maxSize})");
                return null;
            }
            
            if (instance != null && instance.gameObject != null)
            {
            // PoolRoot에 위치하도록 설정 (GetFromPool에서 레이어 Canvas로 이동됨)
            if (instance.transform.parent != _parent)
            {
                instance.transform.SetParent(_parent, false);
            }
                
                _activeInstances.Add(instance);
                instance.gameObject.SetActive(true);
                return instance as T;
            }
            
            return null;
        }
        
        /// <summary>
        /// UI 인스턴스를 풀로 반환
        /// </summary>
        public void Return(BaseUI instance)
        {
            if (instance == null || instance.gameObject == null)
            {
                return;
            }
            
            // 중복 반환 방지
            if (_availablePool.Contains(instance))
            {
                return;
            }
            
            // 활성화된 인스턴스 목록에서 제거
            bool isInActiveInstances = _activeInstances.Contains(instance);
            if (isInActiveInstances)
            {
                _activeInstances.Remove(instance);
            }
            
            // 비활성화
            if (instance.gameObject.activeSelf)
            {
                instance.gameObject.SetActive(false);
            }
            
            // PoolRoot로 이동
            if (_parent == null)
            {
                Debug.LogError($"풀 {PoolName}의 부모가 null입니다!");
                return;
            }
            
            instance.transform.SetParent(_parent, false);
            
            // 초기 상태로 리셋
            ResetInstance(instance);
            
            // 풀에 추가
            _availablePool.Enqueue(instance);
        }
        
        /// <summary>
        /// 새 인스턴스 생성 (풀에 추가하지 않음 - 호출자가 처리)
        /// </summary>
        private BaseUI CreateNewInstance()
        {
            GameObject instance = Object.Instantiate(_prefab, _parent);
            instance.name = _prefab.name; // (Clone) 제거
            instance.SetActive(false);
            
            BaseUI uiComponent = instance.GetComponent<BaseUI>();
            if (uiComponent == null)
            {
                Debug.LogError($"프리팹 {_prefab.name}에 BaseUI 컴포넌트가 없습니다.");
                Object.Destroy(instance);
                return null;
            }
            
            // 초기화 시에는 생성자에서 풀에 추가, Get() 호출 시에는 바로 활성화됨
            return uiComponent;
        }
        
        /// <summary>
        /// 인스턴스를 초기 상태로 리셋
        /// </summary>
        private void ResetInstance(BaseUI instance)
        {
            // RectTransform 리셋
            RectTransform rectTransform = instance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }
        }
        
        /// <summary>
        /// 풀 정리 (모든 인스턴스 제거)
        /// </summary>
        public void Clear()
        {
            // 활성화된 인스턴스 모두 반환
            var activeList = new List<BaseUI>(_activeInstances);
            foreach (var instance in activeList)
            {
                Return(instance);
            }
            
            // 모든 인스턴스 제거
            while (_availablePool.Count > 0)
            {
                BaseUI instance = _availablePool.Dequeue();
                if (instance != null && instance.gameObject != null)
                {
                    Object.Destroy(instance.gameObject);
                }
            }
            
            _activeInstances.Clear();
        }
    }
}

