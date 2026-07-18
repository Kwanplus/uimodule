using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule.Editor.Tests
{
    /// <summary>
    /// Gamepad UI 검증 결과 모델을 검증한다.
    /// </summary>
    public class UIGamepadValidatorTests
    {
        /// <summary>
        /// 검증 이슈가 대상 이름과 안내 메시지를 보존하는지 검증한다.
        /// </summary>
        [Test]
        public void ValidationIssue_PreservesDiagnosticContext()
        {
            UIGamepadValidationIssue issue = new UIGamepadValidationIssue("ConfirmButton", "Navigation이 None입니다.");

            Assert.That(issue.ObjectName, Is.EqualTo("ConfirmButton"));
            Assert.That(issue.Message, Does.Contain("Navigation"));
        }

        /// <summary>
        /// Navigation.None Selectable이 경고되는지 검증한다.
        /// </summary>
        [Test]
        public void ValidatePrefab_ReportsNavigationNone()
        {
            GameObject root = new GameObject("ValidationRoot", typeof(RectTransform), typeof(TestValidationUI));
            try
            {
                GameObject buttonObject = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(root.transform, false);
                Button button = buttonObject.GetComponent<Button>();
                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
                var issues = UIGamepadValidator.ValidateGameObject(root);
                Assert.That(issues.Count, Is.GreaterThan(0));
                Assert.That(issues[0].Message, Does.Contain("Navigation이 None"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Explicit Navigation이 상호작용 불가 대상을 가리킬 때 경고하는지 검증한다.
        /// </summary>
        [Test]
        public void ValidateGameObject_ReportsExplicitLinkToNonInteractableTarget()
        {
            GameObject root = new GameObject("ValidationRoot", typeof(RectTransform), typeof(TestValidationUI));
            try
            {
                Button source = CreateButton("Source", root.transform);
                Button target = CreateButton("Target", root.transform);
                target.interactable = false;
                Navigation navigation = source.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnDown = target;
                source.navigation = navigation;

                var issues = UIGamepadValidator.ValidateGameObject(root);

                Assert.That(ContainsMessage(issues, "Explicit Down"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 대상 없는 Navigation Group을 경고하는지 검증한다.
        /// </summary>
        [Test]
        public void ValidateGameObject_ReportsEmptyNavigationGroup()
        {
            GameObject root = new GameObject("ValidationRoot", typeof(RectTransform), typeof(TestValidationUI), typeof(UIGridNavigation));
            try
            {
                var issues = UIGamepadValidator.ValidateGameObject(root);

                Assert.That(ContainsMessage(issues, "Navigation Group에 유효한 Selectable"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 명시 Navigation Group 배열 밖의 하위 Selectable은 Navigation.None 경고 대상인지 검증한다.
        /// </summary>
        [Test]
        public void ValidateGameObject_ReportsNavigationNoneOutsideExplicitGroupList()
        {
            GameObject root = new GameObject("ValidationRoot", typeof(RectTransform), typeof(TestValidationUI), typeof(UILinearNavigation));
            try
            {
                Button managedButton = CreateButton("Managed", root.transform);
                Button unmanagedButton = CreateButton("Unmanaged", root.transform);
                Navigation navigation = unmanagedButton.navigation;
                navigation.mode = Navigation.Mode.None;
                unmanagedButton.navigation = navigation;

                SerializedObject serializedGroup = new SerializedObject(root.GetComponent<UILinearNavigation>());
                SerializedProperty selectables = serializedGroup.FindProperty("_selectables");
                selectables.arraySize = 1;
                selectables.GetArrayElementAtIndex(0).objectReferenceValue = managedButton;
                serializedGroup.ApplyModifiedPropertiesWithoutUndo();

                var issues = UIGamepadValidator.ValidateGameObject(root);

                Assert.That(ContainsMessage(issues, "Navigation이 None"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 명시 Navigation Group 배열 안의 Selectable은 Navigation.None 경고에서 제외되는지 검증한다.
        /// </summary>
        [Test]
        public void ValidateGameObject_SkipsNavigationNoneForExplicitGroupMember()
        {
            GameObject root = new GameObject("ValidationRoot", typeof(RectTransform), typeof(TestValidationUI), typeof(UILinearNavigation));
            try
            {
                Button managedButton = CreateButton("Managed", root.transform);
                Navigation navigation = managedButton.navigation;
                navigation.mode = Navigation.Mode.None;
                managedButton.navigation = navigation;

                SerializedObject serializedGroup = new SerializedObject(root.GetComponent<UILinearNavigation>());
                SerializedProperty selectables = serializedGroup.FindProperty("_selectables");
                selectables.arraySize = 1;
                selectables.GetArrayElementAtIndex(0).objectReferenceValue = managedButton;
                serializedGroup.ApplyModifiedPropertiesWithoutUndo();

                var issues = UIGamepadValidator.ValidateGameObject(root);

                Assert.That(ContainsMessage(issues, "Navigation이 None"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 테스트용 Button을 생성한다.
        /// </summary>
        private static Button CreateButton(string name, Transform parent)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            return buttonObject.GetComponent<Button>();
        }

        /// <summary>
        /// 검증 결과에 지정한 메시지가 있는지 반환한다.
        /// </summary>
        private static bool ContainsMessage(System.Collections.Generic.IReadOnlyList<UIGamepadValidationIssue> issues, string message)
        {
            foreach (UIGamepadValidationIssue issue in issues)
            {
                if (issue.Message.Contains(message))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Editor validator 테스트용 최소 BaseUI 구현이다.
    /// </summary>
    public class TestValidationUI : BaseUI
    {
        protected override void OnInitialize()
        {
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnDestroy()
        {
        }
    }
}
