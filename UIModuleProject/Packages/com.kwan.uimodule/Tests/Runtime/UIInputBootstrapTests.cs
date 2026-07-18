using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

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
        /// 명시 Navigate 설정과 기본 Submit/Cancel fallback을 역할별로 조합하는지 검증한다.
        /// </summary>
        [Test]
        public void Ensure_WithPartialConfiguration_UsesConfiguredAndDefaultActions()
        {
            GameObject owner = new GameObject("Owner");
            UIInputConfiguration configuration = ScriptableObject.CreateInstance<UIInputConfiguration>();
            InputActionAsset actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputAction navigateAction = actionAsset.AddActionMap("CustomUI").AddAction("CustomNavigate", InputActionType.Value);
            InputActionReference navigateReference = InputActionReference.Create(navigateAction);
            SetConfigurationAction(configuration, "_navigate", navigateReference);

            try
            {
                EventSystem eventSystem = UIInputBootstrap.Ensure(owner.transform, configuration);
                InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();

                Assert.That(module.move.action, Is.SameAs(navigateAction));
                Assert.That(module.submit.action, Is.Not.Null);
                Assert.That(module.cancel.action, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(configuration);
                Object.DestroyImmediate(navigateReference);
                Object.DestroyImmediate(actionAsset);
            }
        }

        /// <summary>
        /// 기존 비호환 입력 모듈을 변경하지 않고 한 번만 진단하는지 검증한다.
        /// </summary>
        [Test]
        public void Ensure_WithNonInputSystemModule_ReportsOnce()
        {
            GameObject existingObject = new GameObject("ExistingEventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            GameObject owner = new GameObject("Owner");
            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[UIModule] 기존 EventSystem이 InputSystemUIInputModule을 사용하지 않습니다. New Input System 게임패드 UI를 사용하려면 기존 모듈을 교체하거나 별도 EventSystem 구성을 검토하세요.");

                UIInputBootstrap.Ensure(owner.transform, null);
                UIInputBootstrap.Ensure(owner.transform, null);

                Assert.That(existingObject.GetComponent<StandaloneInputModule>(), Is.Not.Null);
                Assert.That(existingObject.GetComponent<InputSystemUIInputModule>(), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(existingObject);
            }
        }

        /// <summary>
        /// 기존 EventSystem의 복수 활성 입력 모듈을 한 번만 진단하는지 검증한다.
        /// </summary>
        [Test]
        public void Ensure_WithMultipleInputModules_ReportsOnce()
        {
            GameObject existingObject = new GameObject(
                "ExistingEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule),
                typeof(StandaloneInputModule));
            GameObject owner = new GameObject("Owner");
            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[UIModule] 기존 EventSystem에 활성 입력 모듈이 여러 개입니다. 하나만 활성화해야 Navigate/Submit/Cancel 중복을 피할 수 있습니다.");

                UIInputBootstrap.Ensure(owner.transform, null);
                UIInputBootstrap.Ensure(owner.transform, null);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(existingObject);
            }
        }

        /// <summary>
        /// 활성 EventSystem이 여러 개면 한 번만 진단하는지 검증한다.
        /// </summary>
        [Test]
        public void Ensure_WithMultipleEventSystems_ReportsOnce()
        {
            GameObject firstObject = new GameObject(
                "FirstEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            GameObject secondObject = new GameObject(
                "SecondEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            GameObject owner = new GameObject("Owner");
            try
            {
                LogAssert.Expect(
                    LogType.Warning,
                    "[UIModule] 활성 EventSystem이 여러 개입니다. UI 입력을 처리할 EventSystem을 하나만 유지하세요.");

                UIInputBootstrap.Ensure(owner.transform, null);
                UIInputBootstrap.Ensure(owner.transform, null);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
            }
        }

        /// <summary>
        /// 필수 역할 누락을 역할별로 한 번만 진단하는지 검증한다.
        /// </summary>
        [Test]
        public void Ensure_WithMissingNavigate_ReportsRoleOnce()
        {
            GameObject existingObject = new GameObject("ExistingEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            GameObject owner = new GameObject("Owner");
            InputActionAsset actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap uiMap = actionAsset.AddActionMap("UI");
            InputAction submitAction = uiMap.AddAction("Submit", InputActionType.Button);
            InputAction cancelAction = uiMap.AddAction("Cancel", InputActionType.Button);
            InputActionReference submitReference = InputActionReference.Create(submitAction);
            InputActionReference cancelReference = InputActionReference.Create(cancelAction);
            try
            {
                InputSystemUIInputModule module = existingObject.GetComponent<InputSystemUIInputModule>();
                module.move = null;
                module.submit = submitReference;
                module.cancel = cancelReference;
                LogAssert.Expect(
                    LogType.Warning,
                    "[UIModule] UI Navigate 액션이 비어 있습니다. UIInputConfiguration에 할당하거나 프로젝트 전역 UI/Navigate 액션을 추가하세요.");

                UIInputBootstrap.Ensure(owner.transform, null);
                UIInputBootstrap.Ensure(owner.transform, null);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(existingObject);
                Object.DestroyImmediate(submitReference);
                Object.DestroyImmediate(cancelReference);
                Object.DestroyImmediate(actionAsset);
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

        /// <summary>
        /// 테스트용 InputActionReference를 선택 설정에 주입한다.
        /// </summary>
        private static void SetConfigurationAction(
            UIInputConfiguration configuration,
            string fieldName,
            InputActionReference actionReference)
        {
            typeof(UIInputConfiguration)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(configuration, actionReference);
        }
    }
}
