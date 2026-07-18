using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace UIModule.Editor.Tests
{
    /// <summary>
    /// 프로젝트 전역 Input Actions를 사용하는 UI bootstrap 경로를 검증한다.
    /// </summary>
    public class UIInputBootstrapEditorTests
    {
        private const string TemporaryAssetPath = "Assets/UIModuleInputBootstrapTests.asset";

        private InputActionAsset _previousProjectActions;

        [SetUp]
        public void SetUp()
        {
            _previousProjectActions = InputSystem.actions;
            InputSystem.actions = null;
            DestroyAllEventSystems();
            UIInputBootstrap.ResetDiagnosticsForTests();
            AssetDatabase.DeleteAsset(TemporaryAssetPath);
        }

        [TearDown]
        public void TearDown()
        {
            InputSystem.actions = _previousProjectActions;
            DestroyAllEventSystems();
            UIInputBootstrap.ResetDiagnosticsForTests();
            AssetDatabase.DeleteAsset(TemporaryAssetPath);
        }

        /// <summary>
        /// 전역 UI 맵의 표준 역할을 자동 생성 EventSystem에 연결하는지 검증한다.
        /// </summary>
        [Test]
        public void Ensure_WithProjectWideActions_UsesStandardUiMap()
        {
            InputActionAsset actions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap uiMap = actions.AddActionMap("UI");
            InputAction navigate = uiMap.AddAction("Navigate", InputActionType.Value);
            InputAction submit = uiMap.AddAction("Submit", InputActionType.Button);
            InputAction cancel = uiMap.AddAction("Cancel", InputActionType.Button);
            AssetDatabase.CreateAsset(actions, TemporaryAssetPath);
            AssetDatabase.SaveAssets();
            InputSystem.actions = actions;

            GameObject owner = new GameObject("Owner");
            try
            {
                EventSystem eventSystem = UIInputBootstrap.Ensure(owner.transform, null);
                InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();

                Assert.That(module.move.action, Is.SameAs(navigate));
                Assert.That(module.submit.action, Is.SameAs(submit));
                Assert.That(module.cancel.action, Is.SameAs(cancel));
            }
            finally
            {
                Object.DestroyImmediate(owner);
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
