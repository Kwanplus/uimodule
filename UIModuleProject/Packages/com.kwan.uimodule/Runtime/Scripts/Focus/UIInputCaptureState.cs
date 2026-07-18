using System;

namespace UIModule
{
    /// <summary>
    /// UI가 게임플레이 입력을 점유하고 있음을 소비 프로젝트에 알리는 상태다.
    /// </summary>
    public readonly struct UIInputCaptureState : IEquatable<UIInputCaptureState>
    {
        /// <summary>
        /// 입력 점유 상태를 생성한다.
        /// </summary>
        public UIInputCaptureState(bool isCaptured, UIInputCaptureReason reason, int screenDepth, int popupDepth)
        {
            IsCaptured = isCaptured;
            Reason = reason;
            ScreenDepth = screenDepth;
            PopupDepth = popupDepth;
        }

        /// <summary>게임플레이 입력을 억제해야 하는 UI가 활성인지 반환한다.</summary>
        public bool IsCaptured { get; }

        /// <summary>현재 최상위 점유 원인을 반환한다.</summary>
        public UIInputCaptureReason Reason { get; }

        /// <summary>Screen 스택 깊이를 반환한다.</summary>
        public int ScreenDepth { get; }

        /// <summary>Popup 스택 깊이를 반환한다.</summary>
        public int PopupDepth { get; }

        /// <summary>
        /// 다른 상태와 동일한지 비교한다.
        /// </summary>
        public bool Equals(UIInputCaptureState other)
        {
            return IsCaptured == other.IsCaptured
                && Reason == other.Reason
                && ScreenDepth == other.ScreenDepth
                && PopupDepth == other.PopupDepth;
        }

        /// <summary>
        /// 객체와 동일한지 비교한다.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is UIInputCaptureState other && Equals(other);
        }

        /// <summary>
        /// 상태의 해시 코드를 반환한다.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(IsCaptured, Reason, ScreenDepth, PopupDepth);
        }

        /// <summary>동일한지 비교한다.</summary>
        public static bool operator ==(UIInputCaptureState left, UIInputCaptureState right)
        {
            return left.Equals(right);
        }

        /// <summary>다른지 비교한다.</summary>
        public static bool operator !=(UIInputCaptureState left, UIInputCaptureState right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// 현재 UI 입력 점유의 최상위 원인이다.
    /// </summary>
    public enum UIInputCaptureReason
    {
        None,
        Background,
        Screen,
        Popup,
        Overlay,
        System
    }
}
