using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule
{
    /// <summary>
    /// 동적 Selectable 집합의 Navigation을 명시적으로 생성하는 기반 컴포넌트다.
    /// </summary>
    public abstract class UINavigationGroup : MonoBehaviour
    {
        [SerializeField] private bool _includeInactive;
        [SerializeField] private Selectable[] _selectables;

        private readonly Dictionary<Selectable, Navigation> _originalNavigation = new Dictionary<Selectable, Navigation>();

        /// <summary>
        /// 현재 대상 목록을 사용해 Navigation을 생성한다.
        /// </summary>
        public void RebuildNavigation()
        {
            List<Selectable> selectables = GetValidSelectables();
            RestoreNavigation();
            foreach (Selectable selectable in selectables)
            {
                _originalNavigation[selectable] = selectable.navigation;
            }

            BuildNavigation(selectables);
        }

        /// <summary>
        /// 이 컴포넌트가 바꾼 Navigation을 원래 값으로 되돌린다.
        /// </summary>
        public void RestoreNavigation()
        {
            foreach (KeyValuePair<Selectable, Navigation> pair in _originalNavigation)
            {
                if (pair.Key != null)
                {
                    pair.Key.navigation = pair.Value;
                }
            }

            _originalNavigation.Clear();
        }

        protected virtual void OnEnable()
        {
            RebuildNavigation();
        }

        protected virtual void OnDisable()
        {
            RestoreNavigation();
        }

        /// <summary>
        /// 파생 클래스가 대상 목록의 연결을 설정한다.
        /// </summary>
        protected abstract void BuildNavigation(IReadOnlyList<Selectable> selectables);

        /// <summary>
        /// Explicit Navigation 링크를 적용한다.
        /// </summary>
        protected static void SetLinks(Selectable selectable, Selectable up, Selectable down, Selectable left, Selectable right)
        {
            Navigation navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            navigation.selectOnLeft = left;
            navigation.selectOnRight = right;
            selectable.navigation = navigation;
        }

        /// <summary>
        /// 직렬화 목록 또는 하위 Selectable에서 유효 대상 목록을 만든다.
        /// </summary>
        private List<Selectable> GetValidSelectables()
        {
            Selectable[] source = _selectables == null || _selectables.Length == 0
                ? GetComponentsInChildren<Selectable>(_includeInactive)
                : _selectables;
            List<Selectable> result = new List<Selectable>();

            foreach (Selectable selectable in source)
            {
                if (selectable != null
                    && (_includeInactive || selectable.gameObject.activeInHierarchy)
                    && selectable.IsInteractable())
                {
                    result.Add(selectable);
                }
            }

            return result;
        }
    }
}
