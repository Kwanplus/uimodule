using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule
{
    /// <summary>
    /// 화면상의 상대 위치를 기준으로 Selectable Navigation을 생성한다.
    /// </summary>
    public class UISpatialNavigation : UINavigationGroup
    {
        [Range(0f, 1f)]
        [SerializeField] private float _directionBias = 0.5f;

        /// <summary>
        /// 각 Selectable의 네 방향에서 가장 가까운 후보를 연결한다.
        /// </summary>
        protected override void BuildNavigation(IReadOnlyList<Selectable> selectables)
        {
            foreach (Selectable selectable in selectables)
            {
                SetLinks(
                    selectable,
                    FindClosest(selectable, selectables, Vector2.up),
                    FindClosest(selectable, selectables, Vector2.down),
                    FindClosest(selectable, selectables, Vector2.left),
                    FindClosest(selectable, selectables, Vector2.right));
            }
        }

        /// <summary>
        /// 지정 방향 안에서 방향 정렬과 거리를 함께 고려한 후보를 찾는다.
        /// </summary>
        private Selectable FindClosest(Selectable source, IReadOnlyList<Selectable> selectables, Vector2 direction)
        {
            Vector2 sourcePosition = source.transform.position;
            Selectable result = null;
            float bestScore = float.PositiveInfinity;

            foreach (Selectable candidate in selectables)
            {
                if (candidate == source)
                {
                    continue;
                }

                Vector2 delta = (Vector2)candidate.transform.position - sourcePosition;
                float distance = delta.magnitude;
                if (distance <= Mathf.Epsilon)
                {
                    continue;
                }

                float alignment = Vector2.Dot(delta / distance, direction);
                if (alignment <= 0f)
                {
                    continue;
                }

                float score = distance * (1f + ((1f - alignment) * _directionBias));
                if (score < bestScore)
                {
                    bestScore = score;
                    result = candidate;
                }
            }

            return result;
        }
    }
}
