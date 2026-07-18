using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UIModule.Tests
{
    /// <summary>
    /// UI Cancel 정책과 동일 프레임 중복 방지를 검증한다.
    /// </summary>
    public class UICancelRoutingTests
    {
        [TearDown]
        public void TearDown()
        {
            UIManager manager = Object.FindFirstObjectByType<UIManager>();
            if (manager != null)
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        /// <summary>
        /// 선택 항목이 없어도 Default Cancel이 최상위 Popup을 닫는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator CancelWithoutSelectable_ClosesTopPopup()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            manager.ShowScreen<FocusTestScreen>();
            EmptyTestPopup popup = manager.ShowPopup<EmptyTestPopup>();
            yield return null;

            Assert.That(manager.TryRouteCancel(), Is.True);
            yield return null;

            Assert.That(popup.IsActive, Is.False);
        }

        /// <summary>
        /// Ignore 정책은 Cancel을 소비하면서 Popup을 유지하는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator IgnoreCancel_DoesNotClosePopup()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            manager.ShowScreen<FocusTestScreen>();
            EmptyTestPopup popup = manager.ShowPopup<EmptyTestPopup>();
            popup.gameObject.AddComponent<UIFocusScope>().Configure(null, UICancelBehavior.Ignore);
            yield return null;

            Assert.That(manager.TryRouteCancel(), Is.True);
            Assert.That(popup.IsActive, Is.True);
        }

        /// <summary>
        /// 같은 프레임 Cancel은 한 번만 라우팅되는지 검증한다.
        /// </summary>
        [UnityTest]
        public IEnumerator DuplicateCancelInSameFrame_IsSuppressed()
        {
            UIManager manager = UIManager.Instance;
            manager.SetPoolingEnabled(false);
            manager.ShowScreen<FocusTestScreen>();
            manager.ShowPopup<EmptyTestPopup>();
            yield return null;

            Assert.That(manager.TryRouteCancel(), Is.True);
            Assert.That(manager.TryRouteCancel(), Is.False);
        }
    }

    /// <summary>
    /// Selectable 없이 Cancel 라우팅만 검증하는 Popup 대역이다.
    /// </summary>
    public class EmptyTestPopup : BasePopup
    {
        protected override void OnPopupInitialize()
        {
        }

        protected override void OnPopupShow()
        {
        }

        protected override void OnPopupHide()
        {
        }

        protected override void OnPopupDestroy()
        {
        }
    }
}
