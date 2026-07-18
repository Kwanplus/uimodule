using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule
{
    /// <summary>
    /// 행 우선 Selectable 목록을 격자 Navigation으로 연결한다.
    /// </summary>
    public class UIGridNavigation : UINavigationGroup
    {
        [Min(1)]
        [SerializeField] private int _columnCount = 1;
        [SerializeField] private bool _wrapHorizontal;
        [SerializeField] private bool _wrapVertical;

        /// <summary>
        /// 런타임 생성 Grid Navigation의 열 수와 순환 여부를 설정한다.
        /// </summary>
        public void Configure(int columnCount, bool wrapHorizontal = false, bool wrapVertical = false)
        {
            _columnCount = Mathf.Max(1, columnCount);
            _wrapHorizontal = wrapHorizontal;
            _wrapVertical = wrapVertical;
        }

        /// <summary>
        /// 대상 목록에 격자 Navigation을 설정한다.
        /// </summary>
        protected override void BuildNavigation(IReadOnlyList<Selectable> selectables)
        {
            int columns = Mathf.Max(1, _columnCount);
            for (int index = 0; index < selectables.Count; index++)
            {
                int row = index / columns;
                int column = index % columns;
                int rowStart = row * columns;
                int rowEnd = Mathf.Min(rowStart + columns, selectables.Count) - 1;

                Selectable left = FindHorizontal(selectables, index, rowStart, rowEnd, column, -1);
                Selectable right = FindHorizontal(selectables, index, rowStart, rowEnd, column, 1);
                Selectable up = FindVertical(selectables, index, columns, -1);
                Selectable down = FindVertical(selectables, index, columns, 1);
                SetLinks(selectables[index], up, down, left, right);
            }
        }

        /// <summary>
        /// 같은 행의 좌우 대상을 찾는다.
        /// </summary>
        private Selectable FindHorizontal(IReadOnlyList<Selectable> selectables, int index, int rowStart, int rowEnd, int column, int direction)
        {
            int candidate = index + direction;
            if (candidate >= rowStart && candidate <= rowEnd)
            {
                return selectables[candidate];
            }

            if (!_wrapHorizontal)
            {
                return null;
            }

            return direction < 0 ? selectables[rowEnd] : selectables[rowStart];
        }

        /// <summary>
        /// 같은 열의 상하 대상을 찾는다.
        /// </summary>
        private Selectable FindVertical(IReadOnlyList<Selectable> selectables, int index, int columns, int direction)
        {
            int candidate = index + (columns * direction);
            if (candidate >= 0 && candidate < selectables.Count)
            {
                return selectables[candidate];
            }

            if (!_wrapVertical)
            {
                return null;
            }

            int column = index % columns;
            int lastRowStart = ((selectables.Count - 1) / columns) * columns;
            int wrapped = direction < 0 ? lastRowStart + column : column;
            return wrapped >= 0 && wrapped < selectables.Count ? selectables[wrapped] : null;
        }
    }
}
