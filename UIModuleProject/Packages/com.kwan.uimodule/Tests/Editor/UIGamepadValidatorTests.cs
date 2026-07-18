using NUnit.Framework;
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
