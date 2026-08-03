using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule.Tests
{
    /// <summary>
    /// 선형 Navigation helper를 검증한다.
    /// </summary>
    public class UILinearNavigationTests
    {
        /// <summary>
        /// 세 버튼을 세로로 연결하는지 검증한다.
        /// </summary>
        [Test]
        public void RebuildNavigation_ConnectsAdjacentSelectables()
        {
            GameObject root = new GameObject("Root", typeof(RectTransform));
            try
            {
                Button first = CreateButton("First", root.transform);
                Button second = CreateButton("Second", root.transform);
                Button third = CreateButton("Third", root.transform);
                UILinearNavigation navigation = root.AddComponent<UILinearNavigation>();

                navigation.RebuildNavigation();

                Assert.That(first.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                Assert.That(first.navigation.selectOnDown, Is.EqualTo(second));
                Assert.That(second.navigation.selectOnUp, Is.EqualTo(first));
                Assert.That(second.navigation.selectOnDown, Is.EqualTo(third));
                Assert.That(third.navigation.selectOnUp, Is.EqualTo(second));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// helper 비활성화 시 기존 Automatic Navigation을 복원하는지 검증한다.
        /// </summary>
        [Test]
        public void Disable_RestoresOriginalAutomaticNavigation()
        {
            GameObject root = new GameObject("Root", typeof(RectTransform));
            try
            {
                Button first = CreateButton("First", root.transform);
                Button second = CreateButton("Second", root.transform);
                UILinearNavigation navigation = root.AddComponent<UILinearNavigation>();

                Assert.That(first.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                Assert.That(first.navigation.selectOnDown, Is.EqualTo(second));

                navigation.enabled = false;

                Assert.That(first.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));
                Assert.That(first.navigation.selectOnDown, Is.Null);
                Assert.That(second.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 런타임 대상 목록이 하위 자동 탐색보다 우선하고 목록 밖 대상은 연결하지 않는지 검증한다.
        /// </summary>
        [Test]
        public void RuntimeSelectables_OverrideAutoCollectedChildren()
        {
            GameObject root = new GameObject("Root", typeof(RectTransform));
            try
            {
                Button first = CreateButton("First", root.transform);
                Button excluded = CreateButton("Excluded", root.transform);
                Button third = CreateButton("Third", root.transform);
                UILinearNavigation navigation = root.AddComponent<UILinearNavigation>();

                navigation.SetRuntimeSelectables(new Selectable[] { first, third });
                navigation.RebuildNavigation();

                Assert.That(first.navigation.selectOnDown, Is.EqualTo(third));
                Assert.That(third.navigation.selectOnUp, Is.EqualTo(first));
                Assert.That(excluded.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 런타임 목록을 복사하고 null 및 중복 대상을 제거하는지 검증한다.
        /// </summary>
        [Test]
        public void RuntimeSelectables_CopiesAndNormalizesInput()
        {
            GameObject root = new GameObject("Root", typeof(RectTransform));
            try
            {
                Button first = CreateButton("First", root.transform);
                Button second = CreateButton("Second", root.transform);
                Button third = CreateButton("Third", root.transform);
                UILinearNavigation navigation = root.AddComponent<UILinearNavigation>();
                List<Selectable> source = new List<Selectable>
                {
                    first,
                    null,
                    second,
                    second,
                    third
                };

                navigation.SetRuntimeSelectables(source);
                source.Clear();
                navigation.RebuildNavigation();

                Assert.That(first.navigation.selectOnDown, Is.EqualTo(second));
                Assert.That(second.navigation.selectOnUp, Is.EqualTo(first));
                Assert.That(second.navigation.selectOnDown, Is.EqualTo(third));
                Assert.That(third.navigation.selectOnUp, Is.EqualTo(second));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 명시적 빈 런타임 목록이 하위 자동 탐색으로 fallback하지 않는지 검증한다.
        /// </summary>
        [Test]
        public void RuntimeSelectables_WithEmptyOrNullList_DoesNotAutoCollect()
        {
            GameObject root = new GameObject("Root", typeof(RectTransform));
            try
            {
                Button first = CreateButton("First", root.transform);
                Button second = CreateButton("Second", root.transform);
                UILinearNavigation navigation = root.AddComponent<UILinearNavigation>();

                navigation.SetRuntimeSelectables(new Selectable[0]);
                navigation.RebuildNavigation();

                Assert.That(first.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));
                Assert.That(second.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));

                navigation.SetRuntimeSelectables(null);
                navigation.RebuildNavigation();

                Assert.That(first.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));
                Assert.That(second.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 런타임 목록 교체 시 제거된 대상의 기존 Navigation을 즉시 복원하는지 검증한다.
        /// </summary>
        [Test]
        public void RuntimeSelectables_ReplacementRestoresRemovedTargets()
        {
            GameObject root = new GameObject("Root", typeof(RectTransform));
            try
            {
                Button first = CreateButton("First", root.transform);
                Button second = CreateButton("Second", root.transform);
                Button third = CreateButton("Third", root.transform);
                UILinearNavigation navigation = root.AddComponent<UILinearNavigation>();

                navigation.SetRuntimeSelectables(new Selectable[] { first, second });
                navigation.RebuildNavigation();
                Assert.That(first.navigation.selectOnDown, Is.EqualTo(second));

                navigation.SetRuntimeSelectables(new Selectable[] { third });

                Assert.That(first.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));
                Assert.That(second.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));

                navigation.RebuildNavigation();

                Assert.That(third.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                Assert.That(first.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));
                Assert.That(second.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 런타임 목록 해제 후 Inspector 목록과 하위 자동 탐색으로 순서대로 복귀하는지 검증한다.
        /// </summary>
        [Test]
        public void ClearRuntimeSelectables_RestoresInspectorAndAutoCollection()
        {
            GameObject root = new GameObject("Root", typeof(RectTransform));
            try
            {
                Button first = CreateButton("First", root.transform);
                Button second = CreateButton("Second", root.transform);
                Button third = CreateButton("Third", root.transform);
                UILinearNavigation navigation = root.AddComponent<UILinearNavigation>();
                SetInspectorSelectables(navigation, new Selectable[] { first, third });

                navigation.SetRuntimeSelectables(new Selectable[] { second });
                navigation.RebuildNavigation();
                Assert.That(second.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));

                navigation.ClearRuntimeSelectables();
                Assert.That(second.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));
                navigation.RebuildNavigation();

                Assert.That(first.navigation.selectOnDown, Is.EqualTo(third));
                Assert.That(second.navigation.mode, Is.EqualTo(Navigation.Mode.Automatic));

                SetInspectorSelectables(navigation, null);
                navigation.RebuildNavigation();

                Assert.That(first.navigation.selectOnDown, Is.EqualTo(second));
                Assert.That(second.navigation.selectOnDown, Is.EqualTo(third));
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
        /// Inspector Selectable 목록을 테스트 조건에 맞게 설정한다.
        /// </summary>
        private static void SetInspectorSelectables(UILinearNavigation navigation, Selectable[] selectables)
        {
            typeof(UINavigationGroup)
                .GetField("_selectables", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(navigation, selectables);
        }
    }
}
