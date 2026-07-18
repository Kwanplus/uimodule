using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule.Tests
{
    /// <summary>
    /// Grid 및 Spatial Navigation helper의 명시 연결을 검증한다.
    /// </summary>
    public class UINavigationTests
    {
        [Test]
        public void GridNavigation_ConnectsRowsAndColumns()
        {
            GameObject root = new GameObject("Grid", typeof(RectTransform));
            try
            {
                Button first = CreateButton("First", root.transform, new Vector2(-20f, 20f));
                Button second = CreateButton("Second", root.transform, new Vector2(20f, 20f));
                Button third = CreateButton("Third", root.transform, new Vector2(-20f, -20f));
                Button fourth = CreateButton("Fourth", root.transform, new Vector2(20f, -20f));
                UIGridNavigation navigation = root.AddComponent<UIGridNavigation>();
                navigation.Configure(2);

                navigation.RebuildNavigation();

                Assert.That(first.navigation.selectOnRight, Is.EqualTo(second));
                Assert.That(first.navigation.selectOnDown, Is.EqualTo(third));
                Assert.That(fourth.navigation.selectOnLeft, Is.EqualTo(third));
                Assert.That(fourth.navigation.selectOnUp, Is.EqualTo(second));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SpatialNavigation_ConnectsClosestDirectionalTargets()
        {
            GameObject root = new GameObject("Spatial", typeof(RectTransform));
            try
            {
                Button center = CreateButton("Center", root.transform, Vector2.zero);
                Button up = CreateButton("Up", root.transform, Vector2.up * 20f);
                Button right = CreateButton("Right", root.transform, Vector2.right * 20f);
                UISpatialNavigation navigation = root.AddComponent<UISpatialNavigation>();
                Canvas.ForceUpdateCanvases();

                navigation.RebuildNavigation();

                Assert.That(center.navigation.selectOnUp, Is.EqualTo(up));
                Assert.That(center.navigation.selectOnRight, Is.EqualTo(right));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 위치를 가진 테스트 Button을 만든다.
        /// </summary>
        private static Button CreateButton(string name, Transform parent, Vector2 position)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            ((RectTransform)buttonObject.transform).anchoredPosition = position;
            return buttonObject.GetComponent<Button>();
        }
    }
}
