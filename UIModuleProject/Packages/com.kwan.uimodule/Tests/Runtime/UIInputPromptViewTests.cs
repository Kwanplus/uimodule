using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UIModule.Tests
{
    /// <summary>
    /// 공용 Xbox 입력 프롬프트의 장치 상태 표시와 설정 조회를 검증한다.
    /// </summary>
    public class UIInputPromptViewTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _createdObjects.Count - 1; index >= 0; index--)
            {
                if (_createdObjects[index] != null)
                {
                    Object.DestroyImmediate(_createdObjects[index]);
                }
            }

            _createdObjects.Clear();
            UIModuleSettings.ClearCache();

            UIManager manager = Object.FindFirstObjectByType<UIManager>();
            if (manager != null)
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }

        /// <summary>
        /// 모든 Xbox 버튼 종류가 설정된 Sprite를 반환하는지 검증한다.
        /// </summary>
        [Test]
        public void Configuration_ReturnsSpriteForEveryXboxButtonType()
        {
            UIInputPromptConfiguration configuration = CreateConfiguration();

            foreach (XboxButtonType buttonType in (XboxButtonType[])System.Enum.GetValues(typeof(XboxButtonType)))
            {
                Sprite sprite = CreateSprite();
                SetConfigurationSprite(configuration, buttonType, sprite);

                Assert.That(configuration.GetSprite(buttonType), Is.SameAs(sprite));
            }
        }

        /// <summary>
        /// 활성화 시 현재 장치 상태를 즉시 반영하고 장치 변경에 따라 표시를 전환하는지 검증한다.
        /// </summary>
        [Test]
        public void Prompt_ReflectsCurrentDeviceStateAndDeviceChanges()
        {
            UIManager manager = UIManager.Instance;
            UIInputPromptConfiguration configuration = CreateConfiguration();
            Sprite sprite = CreateSprite();
            SetConfigurationSprite(configuration, XboxButtonType.South, sprite);
            UseConfiguration(configuration);
            SetInputDeviceState(manager, UIInputDeviceType.Gamepad);
            UIInputPromptView view = CreatePromptView(XboxButtonType.South, out GameObject container, out Image iconImage);

            Assert.That(container.activeSelf, Is.True);
            Assert.That(iconImage.sprite, Is.SameAs(sprite));

            SetInputDeviceState(manager, UIInputDeviceType.Keyboard);

            Assert.That(container.activeSelf, Is.False);
            Assert.That(view, Is.Not.Null);
        }

        /// <summary>
        /// Gamepad 이외의 마지막 입력 장치에서는 프롬프트를 숨기는지 검증한다.
        /// </summary>
        [TestCase(UIInputDeviceType.None)]
        [TestCase(UIInputDeviceType.Keyboard)]
        [TestCase(UIInputDeviceType.Pointer)]
        [TestCase(UIInputDeviceType.Touch)]
        [TestCase(UIInputDeviceType.Other)]
        public void Prompt_HidesForNonGamepadDevices(UIInputDeviceType deviceType)
        {
            UIManager manager = UIManager.Instance;
            UIInputPromptConfiguration configuration = CreateConfiguration();
            SetConfigurationSprite(configuration, XboxButtonType.South, CreateSprite());
            UseConfiguration(configuration);
            UIInputPromptView view = CreatePromptView(XboxButtonType.South, out GameObject container, out _);

            SetInputDeviceState(manager, deviceType);

            Assert.That(container.activeSelf, Is.False);
            Assert.That(view, Is.Not.Null);
        }

        /// <summary>
        /// 동일 공용 설정을 사용하는 모든 프롬프트가 다음 장치 변경에서 변경된 Sprite를 반영하는지 검증한다.
        /// </summary>
        [Test]
        public void Prompt_UsesSharedConfigurationForAllViews()
        {
            UIManager manager = UIManager.Instance;
            UIInputPromptConfiguration configuration = CreateConfiguration();
            Sprite firstSprite = CreateSprite();
            Sprite secondSprite = CreateSprite();
            SetConfigurationSprite(configuration, XboxButtonType.South, firstSprite);
            UseConfiguration(configuration);
            SetInputDeviceState(manager, UIInputDeviceType.Gamepad);
            CreatePromptView(XboxButtonType.South, out _, out Image firstIcon);
            CreatePromptView(XboxButtonType.South, out _, out Image secondIcon);

            Assert.That(firstIcon.sprite, Is.SameAs(firstSprite));
            Assert.That(secondIcon.sprite, Is.SameAs(firstSprite));

            SetConfigurationSprite(configuration, XboxButtonType.South, secondSprite);
            SetInputDeviceState(manager, UIInputDeviceType.Keyboard);
            SetInputDeviceState(manager, UIInputDeviceType.Gamepad);

            Assert.That(firstIcon.sprite, Is.SameAs(secondSprite));
            Assert.That(secondIcon.sprite, Is.SameAs(secondSprite));
        }

        /// <summary>
        /// 반복 활성화해도 InputDeviceChanged 구독이 하나만 유지되는지 검증한다.
        /// </summary>
        [Test]
        public void Prompt_RepeatedEnableDisable_DoesNotDuplicateSubscription()
        {
            UIManager manager = UIManager.Instance;
            UIInputPromptConfiguration configuration = CreateConfiguration();
            SetConfigurationSprite(configuration, XboxButtonType.South, CreateSprite());
            UseConfiguration(configuration);
            UIInputPromptView view = CreatePromptView(XboxButtonType.South, out _, out _);

            for (int index = 0; index < 3; index++)
            {
                view.gameObject.SetActive(false);
                view.gameObject.SetActive(true);
            }

            System.Delegate listeners = typeof(UIManager)
                .GetField("InputDeviceChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(manager) as System.Delegate;
            int subscriptionCount = 0;
            if (listeners != null)
            {
                foreach (System.Delegate listener in listeners.GetInvocationList())
                {
                    if (System.Object.ReferenceEquals(listener.Target, view))
                    {
                        subscriptionCount++;
                    }
                }
            }

            Assert.That(subscriptionCount, Is.EqualTo(1));
        }

        /// <summary>
        /// View 자신을 컨테이너로 연결해도 비활성화되지 않고 이후 Gamepad 입력에 다시 표시되는지 검증한다.
        /// </summary>
        [Test]
        public void Prompt_WithSelfContainer_KeepsDeviceSubscriptionAndTogglesIcon()
        {
            UIManager manager = UIManager.Instance;
            UIInputPromptConfiguration configuration = CreateConfiguration();
            SetConfigurationSprite(configuration, XboxButtonType.South, CreateSprite());
            UseConfiguration(configuration);
            SetInputDeviceState(manager, UIInputDeviceType.Keyboard);
            LogAssert.Expect(
                LogType.Error,
                "[UIModule] UIInputPromptView의 Prompt Container는 View 자신이 아닌 별도의 하위 오브젝트여야 합니다. " +
                "자기 자신을 연결한 경우 아이콘만 표시·숨김 처리합니다.");
            UIInputPromptView view = CreateSelfContainerPromptView(out Image iconImage);

            Assert.That(view.gameObject.activeSelf, Is.True);
            Assert.That(iconImage.enabled, Is.False);

            SetInputDeviceState(manager, UIInputDeviceType.Gamepad);

            Assert.That(view.gameObject.activeSelf, Is.True);
            Assert.That(iconImage.enabled, Is.True);

            SetInputDeviceState(manager, UIInputDeviceType.Keyboard);

            Assert.That(view.gameObject.activeSelf, Is.True);
            Assert.That(iconImage.enabled, Is.False);
        }

        /// <summary>
        /// 설정, Sprite 또는 UI 참조가 없더라도 프롬프트가 예외 없이 숨겨지는지 검증한다.
        /// </summary>
        [Test]
        public void Prompt_WithMissingConfigurationSpriteOrReferences_HidesWithoutException()
        {
            UIManager manager = UIManager.Instance;
            UIInputPromptConfiguration configuration = CreateConfiguration();
            UseConfiguration(configuration);
            SetInputDeviceState(manager, UIInputDeviceType.Gamepad);
            UIInputPromptView missingSpriteView = CreatePromptView(
                XboxButtonType.South,
                out GameObject missingSpriteContainer,
                out _);
            UIModuleSettings.ClearCache();
            UIInputPromptView missingConfigurationView = null;
            GameObject missingConfigurationContainer = null;
            Assert.DoesNotThrow(() =>
                missingConfigurationView = CreatePromptView(
                    XboxButtonType.South,
                    out missingConfigurationContainer,
                    out _));
            GameObject missingReferenceObject = new GameObject("MissingReferences");
            _createdObjects.Add(missingReferenceObject);

            Assert.DoesNotThrow(() => missingReferenceObject.AddComponent<UIInputPromptView>());
            Assert.That(missingSpriteContainer.activeSelf, Is.False);
            Assert.That(missingConfigurationContainer.activeSelf, Is.False);
            Assert.That(missingSpriteView, Is.Not.Null);
            Assert.That(missingConfigurationView, Is.Not.Null);
        }

        /// <summary>
        /// 테스트용 공용 입력 프롬프트 설정을 생성한다.
        /// </summary>
        private UIInputPromptConfiguration CreateConfiguration()
        {
            UIInputPromptConfiguration configuration = ScriptableObject.CreateInstance<UIInputPromptConfiguration>();
            _createdObjects.Add(configuration);
            return configuration;
        }

        /// <summary>
        /// 테스트용 UIModuleSettings 인스턴스에 공용 프롬프트 설정을 연결한다.
        /// </summary>
        private void UseConfiguration(UIInputPromptConfiguration configuration)
        {
            UIModuleSettings settings = ScriptableObject.CreateInstance<UIModuleSettings>();
            _createdObjects.Add(settings);
            typeof(UIModuleSettings)
                .GetField("_inputPromptConfiguration", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(settings, configuration);
            typeof(UIModuleSettings)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
                ?.SetValue(null, settings);
        }

        /// <summary>
        /// 지정한 Xbox 버튼 Sprite를 테스트 설정에 연결한다.
        /// </summary>
        private static void SetConfigurationSprite(
            UIInputPromptConfiguration configuration,
            XboxButtonType buttonType,
            Sprite sprite)
        {
            typeof(UIInputPromptConfiguration)
                .GetField(GetSpriteFieldName(buttonType), BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(configuration, sprite);
        }

        /// <summary>
        /// Xbox 버튼 종류에 대응하는 직렬화 Sprite 필드 이름을 반환한다.
        /// </summary>
        private static string GetSpriteFieldName(XboxButtonType buttonType)
        {
            switch (buttonType)
            {
                case XboxButtonType.South:
                    return "_south";
                case XboxButtonType.East:
                    return "_east";
                case XboxButtonType.West:
                    return "_west";
                case XboxButtonType.North:
                    return "_north";
                case XboxButtonType.LB:
                    return "_leftBumper";
                case XboxButtonType.RB:
                    return "_rightBumper";
                case XboxButtonType.LT:
                    return "_leftTrigger";
                case XboxButtonType.RT:
                    return "_rightTrigger";
                case XboxButtonType.LeftStick:
                    return "_leftStick";
                case XboxButtonType.RightStick:
                    return "_rightStick";
                case XboxButtonType.DPadUp:
                    return "_dPadUp";
                case XboxButtonType.DPadDown:
                    return "_dPadDown";
                case XboxButtonType.DPadLeft:
                    return "_dPadLeft";
                case XboxButtonType.DPadRight:
                    return "_dPadRight";
                case XboxButtonType.View:
                    return "_view";
                case XboxButtonType.Menu:
                    return "_menu";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 프롬프트 컨테이너와 아이콘을 포함한 테스트 View를 생성한다.
        /// </summary>
        private UIInputPromptView CreatePromptView(
            XboxButtonType buttonType,
            out GameObject container,
            out Image iconImage)
        {
            GameObject viewObject = new GameObject("PromptView", typeof(RectTransform));
            viewObject.SetActive(false);
            _createdObjects.Add(viewObject);

            container = new GameObject("PromptContainer", typeof(RectTransform));
            container.transform.SetParent(viewObject.transform, false);
            iconImage = container.AddComponent<Image>();
            UIInputPromptView view = viewObject.AddComponent<UIInputPromptView>();
            typeof(UIInputPromptView)
                .GetField("_promptContainer", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(view, container);
            typeof(UIInputPromptView)
                .GetField("_iconImage", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(view, iconImage);
            typeof(UIInputPromptView)
                .GetField("_buttonType", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(view, buttonType);

            viewObject.SetActive(true);
            return view;
        }

        /// <summary>
        /// View 자신을 프롬프트 컨테이너로 연결한 예외 구성의 테스트 대역을 생성한다.
        /// </summary>
        private UIInputPromptView CreateSelfContainerPromptView(out Image iconImage)
        {
            GameObject viewObject = new GameObject("SelfContainerPrompt", typeof(RectTransform));
            viewObject.SetActive(false);
            _createdObjects.Add(viewObject);

            iconImage = viewObject.AddComponent<Image>();
            UIInputPromptView view = viewObject.AddComponent<UIInputPromptView>();
            typeof(UIInputPromptView)
                .GetField("_promptContainer", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(view, viewObject);
            typeof(UIInputPromptView)
                .GetField("_iconImage", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(view, iconImage);
            typeof(UIInputPromptView)
                .GetField("_buttonType", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(view, XboxButtonType.South);

            viewObject.SetActive(true);
            return view;
        }

        /// <summary>
        /// UIManager의 입력 장치 상태를 테스트 조건에 맞게 갱신한다.
        /// </summary>
        private static void SetInputDeviceState(UIManager manager, UIInputDeviceType deviceType)
        {
            typeof(UIManager)
                .GetMethod(
                    "UpdateInputDeviceState",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(UIInputDeviceType?) },
                    null)
                ?.Invoke(manager, new object[] { (UIInputDeviceType?)deviceType });
        }

        /// <summary>
        /// 테스트용 Sprite를 생성한다.
        /// </summary>
        private Sprite CreateSprite()
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
            _createdObjects.Add(sprite);
            _createdObjects.Add(texture);
            return sprite;
        }
    }
}
