using UnityEngine;
using UnityEngine.InputSystem;

namespace UIModule
{
    /// <summary>
    /// 기본 UI 입력 역할을 소비 프로젝트의 Input Action으로 선택적으로 치환한다.
    /// </summary>
    [CreateAssetMenu(fileName = "UIInputConfiguration", menuName = "UIModule/UI Input Configuration")]
    public class UIInputConfiguration : ScriptableObject
    {
        [Header("UI Action Overrides")]
        [SerializeField] private InputActionReference _navigate;
        [SerializeField] private InputActionReference _submit;
        [SerializeField] private InputActionReference _cancel;
        [SerializeField] private InputActionReference _point;
        [SerializeField] private InputActionReference _click;
        [SerializeField] private InputActionReference _rightClick;
        [SerializeField] private InputActionReference _middleClick;
        [SerializeField] private InputActionReference _scrollWheel;
        [SerializeField] private InputActionReference _trackedDevicePosition;
        [SerializeField] private InputActionReference _trackedDeviceOrientation;

        /// <summary>Navigate 액션 override를 반환한다.</summary>
        public InputActionReference Navigate => _navigate;

        /// <summary>Submit 액션 override를 반환한다.</summary>
        public InputActionReference Submit => _submit;

        /// <summary>Cancel 액션 override를 반환한다.</summary>
        public InputActionReference Cancel => _cancel;

        /// <summary>Point 액션 override를 반환한다.</summary>
        public InputActionReference Point => _point;

        /// <summary>Click 액션 override를 반환한다.</summary>
        public InputActionReference Click => _click;

        /// <summary>Right Click 액션 override를 반환한다.</summary>
        public InputActionReference RightClick => _rightClick;

        /// <summary>Middle Click 액션 override를 반환한다.</summary>
        public InputActionReference MiddleClick => _middleClick;

        /// <summary>Scroll Wheel 액션 override를 반환한다.</summary>
        public InputActionReference ScrollWheel => _scrollWheel;

        /// <summary>Tracked Device Position 액션 override를 반환한다.</summary>
        public InputActionReference TrackedDevicePosition => _trackedDevicePosition;

        /// <summary>Tracked Device Orientation 액션 override를 반환한다.</summary>
        public InputActionReference TrackedDeviceOrientation => _trackedDeviceOrientation;
    }
}
