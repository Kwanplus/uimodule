using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule
{
    /// <summary>
    /// Selectable 목록을 가로 또는 세로 한 줄 Navigation으로 연결한다.
    /// </summary>
    public class UILinearNavigation : UINavigationGroup
    {
        [SerializeField] private UILinearNavigationDirection _direction = UILinearNavigationDirection.Vertical;
        [SerializeField] private bool _wrapAround;

        /// <summary>
        /// 대상 목록에 선형 Navigation을 설정한다.
        /// </summary>
        protected override void BuildNavigation(IReadOnlyList<Selectable> selectables)
        {
            for (int index = 0; index < selectables.Count; index++)
            {
                Selectable previous = index > 0 ? selectables[index - 1] : (_wrapAround && selectables.Count > 1 ? selectables[selectables.Count - 1] : null);
                Selectable next = index < selectables.Count - 1 ? selectables[index + 1] : (_wrapAround && selectables.Count > 1 ? selectables[0] : null);

                if (_direction == UILinearNavigationDirection.Vertical)
                {
                    SetLinks(selectables[index], previous, next, null, null);
                }
                else
                {
                    SetLinks(selectables[index], null, null, previous, next);
                }
            }
        }
    }

    /// <summary>
    /// 선형 Navigation의 진행 방향이다.
    /// </summary>
    public enum UILinearNavigationDirection
    {
        Vertical,
        Horizontal
    }
}
