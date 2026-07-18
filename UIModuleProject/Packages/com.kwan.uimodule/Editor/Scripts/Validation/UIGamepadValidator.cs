using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule.Editor
{
    /// <summary>
    /// 게임패드 선택과 Navigation 구성에서 자주 발생하는 프리팹 오류를 검사한다.
    /// </summary>
    public static class UIGamepadValidator
    {
        /// <summary>
        /// 프로젝트의 BaseUI 프리팹을 검사한다.
        /// </summary>
        [MenuItem("Tools/UIModule/Validate Gamepad UI")]
        public static void ValidateProject()
        {
            int warningCount = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string prefabGuid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponentInChildren<BaseUI>(true) == null)
                {
                    continue;
                }

                foreach (UIGamepadValidationIssue issue in ValidatePrefab(path))
                {
                    warningCount++;
                    Debug.LogWarning($"[UIModule] {path}: {issue.Message}", prefab);
                }
            }

            Debug.Log($"[UIModule] Gamepad UI 검증 완료: 경고 {warningCount}건");
        }

        /// <summary>
        /// 하나의 프리팹을 검사해 경고 목록을 반환한다.
        /// </summary>
        public static IReadOnlyList<UIGamepadValidationIssue> ValidatePrefab(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                return ValidateGameObject(root);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 로드된 UI 루트를 검사해 경고 목록을 반환한다.
        /// </summary>
        public static IReadOnlyList<UIGamepadValidationIssue> ValidateGameObject(GameObject root)
        {
            List<UIGamepadValidationIssue> issues = new List<UIGamepadValidationIssue>();
            BaseUI[] uiRoots = root.GetComponentsInChildren<BaseUI>(true);
            foreach (BaseUI uiRoot in uiRoots)
            {
                ValidateFocusScope(uiRoot, issues);
                ValidateSelectables(uiRoot, issues);
                ValidateEnsureVisible(uiRoot, issues);
            }

            return issues;
        }

        /// <summary>
        /// 명시 기본 대상이 UI 범위에서 선택 가능한지 검사한다.
        /// </summary>
        private static void ValidateFocusScope(BaseUI uiRoot, List<UIGamepadValidationIssue> issues)
        {
            UIFocusScope scope = uiRoot.GetComponent<UIFocusScope>();
            if (scope == null || scope.DefaultSelection == null)
            {
                return;
            }

            if (!scope.DefaultSelection.transform.IsChildOf(uiRoot.transform)
                || !scope.DefaultSelection.gameObject.activeSelf
                || !scope.DefaultSelection.IsInteractable())
            {
                issues.Add(new UIGamepadValidationIssue(
                    uiRoot.name,
                    "UIFocusScope의 Default Selection은 활성화된 상호작용 가능 Selectable이어야 하며 같은 UI 범위 안에 있어야 합니다."));
            }
        }

        /// <summary>
        /// Navigation.None과 유효하지 않은 Explicit 링크를 검사한다.
        /// </summary>
        private static void ValidateSelectables(BaseUI uiRoot, List<UIGamepadValidationIssue> issues)
        {
            Selectable[] selectables = uiRoot.GetComponentsInChildren<Selectable>(true);
            foreach (Selectable selectable in selectables)
            {
                if (!selectable.gameObject.activeSelf || !selectable.IsInteractable())
                {
                    continue;
                }

                Navigation navigation = selectable.navigation;
                if (navigation.mode == Navigation.Mode.None)
                {
                    issues.Add(new UIGamepadValidationIssue(
                        selectable.name,
                        "Navigation이 None입니다. 게임패드로 이동해야 하는 항목이면 Automatic, Explicit 또는 Navigation helper를 사용하세요."));
                    continue;
                }

                if (navigation.mode == Navigation.Mode.Explicit)
                {
                    ValidateExplicitLink(uiRoot, selectable, navigation.selectOnUp, "Up", issues);
                    ValidateExplicitLink(uiRoot, selectable, navigation.selectOnDown, "Down", issues);
                    ValidateExplicitLink(uiRoot, selectable, navigation.selectOnLeft, "Left", issues);
                    ValidateExplicitLink(uiRoot, selectable, navigation.selectOnRight, "Right", issues);
                }
            }
        }

        /// <summary>
        /// Explicit 링크가 활성 상태의 같은 UI 범위를 가리키는지 검사한다.
        /// </summary>
        private static void ValidateExplicitLink(BaseUI uiRoot, Selectable source, Selectable target, string direction, List<UIGamepadValidationIssue> issues)
        {
            if (target == null)
            {
                return;
            }

            if (!target.transform.IsChildOf(uiRoot.transform) || !target.gameObject.activeSelf)
            {
                issues.Add(new UIGamepadValidationIssue(
                    source.name,
                    $"Explicit {direction} 링크가 비활성 또는 다른 UI 범위의 Selectable을 가리킵니다."));
            }
        }

        /// <summary>
        /// ScrollRect 자동 노출 컴포넌트의 부모 구성을 검사한다.
        /// </summary>
        private static void ValidateEnsureVisible(BaseUI uiRoot, List<UIGamepadValidationIssue> issues)
        {
            UIEnsureVisibleInScrollRect[] helpers = uiRoot.GetComponentsInChildren<UIEnsureVisibleInScrollRect>(true);
            foreach (UIEnsureVisibleInScrollRect helper in helpers)
            {
                if (helper.GetComponentInParent<ScrollRect>() == null)
                {
                    issues.Add(new UIGamepadValidationIssue(
                        helper.name,
                        "UIEnsureVisibleInScrollRect는 ScrollRect 하위 Selectable에 배치해야 합니다."));
                }
            }
        }
    }

    /// <summary>
    /// 프리팹 검증에서 발견한 경고 정보다.
    /// </summary>
    public readonly struct UIGamepadValidationIssue
    {
        /// <summary>
        /// 검증 경고를 생성한다.
        /// </summary>
        public UIGamepadValidationIssue(string objectName, string message)
        {
            ObjectName = objectName;
            Message = message;
        }

        /// <summary>경고가 발생한 오브젝트 이름이다.</summary>
        public string ObjectName { get; }

        /// <summary>수정 방법을 포함한 경고 내용이다.</summary>
        public string Message { get; }
    }
}
