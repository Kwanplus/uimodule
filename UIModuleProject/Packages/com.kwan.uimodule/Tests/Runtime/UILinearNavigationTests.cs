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
        /// 테스트용 Button을 생성한다.
        /// </summary>
        private static Button CreateButton(string name, Transform parent)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            return buttonObject.GetComponent<Button>();
        }
    }
}
