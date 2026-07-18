using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UIModule
{
    /// <summary>
    /// 전역 InputAction에서 생성한 임시 참조의 수명을 EventSystem과 함께 관리한다.
    /// </summary>
    internal sealed class UIInputActionReferenceOwner : MonoBehaviour
    {
        private readonly List<InputActionReference> _ownedReferences = new List<InputActionReference>();

        /// <summary>
        /// 임시 InputActionReference의 소유권을 등록한다.
        /// </summary>
        internal InputActionReference Own(InputActionReference reference)
        {
            if (reference != null)
            {
                _ownedReferences.Add(reference);
            }

            return reference;
        }

        private void OnDestroy()
        {
            foreach (InputActionReference reference in _ownedReferences)
            {
                if (reference != null)
                {
                    Destroy(reference);
                }
            }

            _ownedReferences.Clear();
        }
    }
}
