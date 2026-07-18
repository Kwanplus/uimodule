using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace UIModule.Tests
{
    /// <summary>
    /// UI Input bootstrap의 EventSystem 생성과 재사용 계약을 검증한다.
    /// </summary>
    public class UIInputBootstrapTests
    {
        [SetUp]
        public void SetUp()
        {
            DestroyAllEventSystems();
            UIInputBootstrap.ResetDiagnosticsForTests();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyAllEventSystems();
            UIInputBootstrap.ResetDiagnosticsForTests();
        }

        /// <summary>
        /// EventSystem이 없으면 Input System UI 모듈과 함께 생성하는지 검증한다.
        /// </summary>
        [Test]
        public void Ensure_WithoutEventSystem_CreatesInputSystemModule()
        {
            GameObject owner = new GameObject("Owner");
            try
            {
                EventSystem eventSystem = UIInputBootstrap.Ensure(owner.transform, null);

                Assert.That(eventSystem, Is.Not.Null);
                Assert.That(eventSystem.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
                Assert.That(eventSystem.transform.parent, Is.EqualTo(owner.transform));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        /// <summary>
        /// 기존 EventSystem을 변경하지 않고 재사용하는지 검증한다.
        /// </summary>
        [Test]
        public void Ensure_WithExistingEventSystem_ReusesExistingInstance()
        {
            GameObject existingObject = new GameObject("ExistingEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            GameObject owner = new GameObject("Owner");
            try
            {
                EventSystem existing = existingObject.GetComponent<EventSystem>();
                EventSystem resolved = UIInputBootstrap.Ensure(owner.transform, null);

                Assert.That(resolved, Is.SameAs(existing));
                Assert.That(owner.GetComponentInChildren<EventSystem>(), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(existingObject);
            }
        }

        /// <summary>
        /// 테스트 간 EventSystem 상태를 제거한다.
        /// </summary>
        private static void DestroyAllEventSystems()
        {
            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (EventSystem eventSystem in eventSystems)
            {
                Object.DestroyImmediate(eventSystem.gameObject);
            }
        }
    }
}
