using NUnit.Framework;

namespace UIModule.Tests
{
    /// <summary>
    /// 입력 점유 상태 값 객체를 검증한다.
    /// </summary>
    public class UIInputCaptureStateTests
    {
        /// <summary>
        /// 같은 상태는 동등하고 다른 스택 깊이는 구분되는지 검증한다.
        /// </summary>
        [Test]
        public void Equality_UsesReasonAndStackDepth()
        {
            UIInputCaptureState first = new UIInputCaptureState(true, UIInputCaptureReason.Popup, 1, 2);
            UIInputCaptureState same = new UIInputCaptureState(true, UIInputCaptureReason.Popup, 1, 2);
            UIInputCaptureState different = new UIInputCaptureState(true, UIInputCaptureReason.Popup, 1, 1);

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first, Is.Not.EqualTo(different));
        }

        /// <summary>
        /// 입력 장치 상태가 마지막 장치와 연결 상태를 함께 비교하는지 검증한다.
        /// </summary>
        [Test]
        public void DeviceStateEquality_UsesLastDeviceAndConnection()
        {
            UIInputDeviceState first = new UIInputDeviceState(UIInputDeviceType.Gamepad, true);
            UIInputDeviceState same = new UIInputDeviceState(UIInputDeviceType.Gamepad, true);
            UIInputDeviceState disconnected = new UIInputDeviceState(UIInputDeviceType.Gamepad, false);

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first, Is.Not.EqualTo(disconnected));
        }
    }
}
