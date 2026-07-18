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
                ValidateNavigationGroups(uiRoot, issues);
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
                || !scope.DefaultSelection.gameObject.activeInHierarchy
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
                if (navigation.mode == Navigation.Mode.None && !IsManagedByNavigationGroup(uiRoot, selectable))
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

            if (!target.transform.IsChildOf(uiRoot.transform)
                || !target.gameObject.activeInHierarchy
                || !target.IsInteractable())
            {
                issues.Add(new UIGamepadValidationIssue(
                    source.name,
                    $"Explicit {direction} 링크가 비활성 또는 다른 UI 범위의 Selectable을 가리킵니다."));
            }
        }

        /// <summary>
        /// Navigation Group 대상과 그룹별 구성을 검사한다.
        /// </summary>
        private static void ValidateNavigationGroups(BaseUI uiRoot, List<UIGamepadValidationIssue> issues)
        {
            UINavigationGroup[] groups = uiRoot.GetComponentsInChildren<UINavigationGroup>(true);
            foreach (UINavigationGroup group in groups)
            {
                SerializedObject serializedGroup = new SerializedObject(group);
                SerializedProperty selectablesProperty = serializedGroup.FindProperty("_selectables");
                List<Selectable> targets = new List<Selectable>();
                if (selectablesProperty != null && selectablesProperty.arraySize > 0)
                {
                    HashSet<Selectable> uniqueTargets = new HashSet<Selectable>();
                    for (int index = 0; index < selectablesProperty.arraySize; index++)
                    {
                        Selectable target = selectablesProperty.GetArrayElementAtIndex(index).objectReferenceValue as Selectable;
                        if (target == null)
                        {
                            AddGroupIssue(group, issues, "Navigation Group 대상에 null 항목이 있습니다.");
                            continue;
                        }

                        if (!uniqueTargets.Add(target))
                        {
                            AddGroupIssue(group, issues, "Navigation Group 대상에 중복 Selectable이 있습니다.");
                            continue;
                        }

                        if (!target.transform.IsChildOf(uiRoot.transform)
                            || !target.gameObject.activeInHierarchy
                            || !target.IsInteractable())
                        {
                            AddGroupIssue(group, issues, "Navigation Group 대상은 같은 UI 범위의 활성 상호작용 가능 Selectable이어야 합니다.");
                            continue;
                        }

                        targets.Add(target);
                    }
                }
                else
                {
                    foreach (Selectable selectable in group.GetComponentsInChildren<Selectable>(true))
                    {
                        if (selectable.gameObject.activeInHierarchy && selectable.IsInteractable())
                        {
                            targets.Add(selectable);
                        }
                    }
                }

                if (targets.Count == 0)
                {
                    AddGroupIssue(group, issues, "Navigation Group에 유효한 Selectable이 없습니다.");
                }

                ValidateGridNavigation(group, targets.Count, serializedGroup, issues);
                ValidateSpatialNavigation(group, targets, issues);
            }
        }

        /// <summary>
        /// Grid Navigation의 열 수와 불완전 행을 검사한다.
        /// </summary>
        private static void ValidateGridNavigation(
            UINavigationGroup group,
            int targetCount,
            SerializedObject serializedGroup,
            List<UIGamepadValidationIssue> issues)
        {
            if (!(group is UIGridNavigation))
            {
                return;
            }

            SerializedProperty columnsProperty = serializedGroup.FindProperty("_columnCount");
            int columns = columnsProperty == null ? 0 : columnsProperty.intValue;
            if (columns <= 0)
            {
                AddGroupIssue(group, issues, "Grid Navigation의 Column Count는 1 이상이어야 합니다.");
                return;
            }

            if (targetCount > columns && targetCount % columns != 0)
            {
                AddGroupIssue(group, issues, "Grid Navigation의 마지막 행이 불완전합니다. Vertical Wrap 동작을 확인하세요.");
            }
        }

        /// <summary>
        /// Spatial Navigation에서 같은 좌표에 있는 대상을 검사한다.
        /// </summary>
        private static void ValidateSpatialNavigation(
            UINavigationGroup group,
            List<Selectable> targets,
            List<UIGamepadValidationIssue> issues)
        {
            if (!(group is UISpatialNavigation))
            {
                return;
            }

            for (int left = 0; left < targets.Count; left++)
            {
                for (int right = left + 1; right < targets.Count; right++)
                {
                    if (Vector2.Distance(targets[left].transform.position, targets[right].transform.position) <= Mathf.Epsilon)
                    {
                        AddGroupIssue(group, issues, "Spatial Navigation 대상에 같은 위치의 Selectable이 있습니다.");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Navigation.None을 런타임에 재구성하는 대상인지 반환한다.
        /// </summary>
        private static bool IsManagedByNavigationGroup(BaseUI uiRoot, Selectable selectable)
        {
            foreach (UINavigationGroup group in uiRoot.GetComponentsInChildren<UINavigationGroup>(true))
            {
                if (selectable.transform.IsChildOf(group.transform))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Navigation Group 관련 경고를 추가한다.
        /// </summary>
        private static void AddGroupIssue(UINavigationGroup group, List<UIGamepadValidationIssue> issues, string message)
        {
            issues.Add(new UIGamepadValidationIssue(group.name, message));
        }

        /// <summary>
        /// ScrollRect 자동 노출 컴포넌트의 부모 구성을 검사한다.
        /// </summary>
        private static void ValidateEnsureVisible(BaseUI uiRoot, List<UIGamepadValidationIssue> issues)
        {
            UIEnsureVisibleInScrollRect[] helpers = uiRoot.GetComponentsInChildren<UIEnsureVisibleInScrollRect>(true);
            foreach (UIEnsureVisibleInScrollRect helper in helpers)
            {
                SerializedObject serializedHelper = new SerializedObject(helper);
                ScrollRect scrollRect = serializedHelper.FindProperty("_scrollRect")?.objectReferenceValue as ScrollRect;
                if (scrollRect == null)
                {
                    issues.Add(new UIGamepadValidationIssue(
                        helper.name,
                        "UIEnsureVisibleInScrollRect의 ScrollRect 참조를 지정하세요."));
                    continue;
                }

                if (scrollRect.viewport == null || scrollRect.content == null)
                {
                    issues.Add(new UIGamepadValidationIssue(
                        helper.name,
                        "ScrollRect에는 Viewport와 Content가 모두 지정되어야 합니다."));
                    continue;
                }

                if (helper.GetComponent<Selectable>() == null
                    || !helper.transform.IsChildOf(scrollRect.content))
                {
                    issues.Add(new UIGamepadValidationIssue(
                        helper.name,
                        "UIEnsureVisibleInScrollRect는 ScrollRect Content 아래의 Selectable에 배치해야 합니다."));
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
