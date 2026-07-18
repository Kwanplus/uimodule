using System;

namespace UIModule
{
    /// <summary>
    /// UI를 마지막으로 조작한 입력 장치 종류다.
    /// </summary>
    public enum UIInputDeviceType
    {
        None,
        Keyboard,
        Pointer,
        Gamepad,
        Touch,
        Other
    }

    /// <summary>
    /// UI 입력 장치의 현재 공개 상태다.
    /// </summary>
    public readonly struct UIInputDeviceState : IEquatable<UIInputDeviceState>
    {
        /// <summary>
        /// 상태를 생성한다.
        /// </summary>
        public UIInputDeviceState(UIInputDeviceType lastInputDevice, bool isGamepadConnected)
        {
            LastInputDevice = lastInputDevice;
            IsGamepadConnected = isGamepadConnected;
        }

        /// <summary>마지막 UI 입력 장치 종류다.</summary>
        public UIInputDeviceType LastInputDevice { get; }

        /// <summary>연결된 Gamepad가 하나 이상인지 반환한다.</summary>
        public bool IsGamepadConnected { get; }

        /// <summary>
        /// 같은 상태인지 비교한다.
        /// </summary>
        public bool Equals(UIInputDeviceState other)
        {
            return LastInputDevice == other.LastInputDevice && IsGamepadConnected == other.IsGamepadConnected;
        }

        /// <summary>
        /// 객체와 비교한다.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is UIInputDeviceState other && Equals(other);
        }

        /// <summary>
        /// 해시 코드를 반환한다.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(LastInputDevice, IsGamepadConnected);
        }
    }
}
