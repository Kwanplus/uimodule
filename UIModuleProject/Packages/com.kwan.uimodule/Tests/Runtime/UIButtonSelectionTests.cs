using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIModule.Tests
{
    /// <summary>
    /// UIButton의 EventSystem 선택 상태 통지와 기존 입력 처리를 검증한다.
    /// </summary>
    public class UIButtonSelectionTests
    {
        [SetUp]
        public void SetUp()
        {
            DestroyAllEventSystems();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyAllEventSystems();
        }

        /// <summary>
        /// EventSystem 선택 전환이 이전 버튼의 선택 해제와 신규 버튼의 선택을 한 번씩 통지하는지 검증한다.
        /// </summary>
        [Test]
        public void EventSystemSelection_NotifiesSelectedAndDeselectedOnce()
        {
            EventSystem eventSystem = CreateEventSystem();
            UIButton first = CreateButton("First");
            UIButton second = CreateButton("Second");
            List<string> notifications = new List<string>();
            BaseEventData selectedEventData = null;
            BaseEventData deselectedEventData = null;

            try
            {
                first.OnSelected += eventData =>
                {
                    notifications.Add("First.Selected");
                    selectedEventData = eventData;
                };
                first.OnDeselected += eventData =>
                {
                    notifications.Add("First.Deselected");
                    deselectedEventData = eventData;
                };
                second.OnSelected += eventData => notifications.Add("Second.Selected");

                eventSystem.SetSelectedGameObject(first.gameObject);
                eventSystem.SetSelectedGameObject(second.gameObject);

                Assert.That(notifications, Is.EqualTo(new[]
                {
                    "First.Selected",
                    "First.Deselected",
                    "Second.Selected"
                }));
                Assert.That(selectedEventData, Is.Not.Null);
                Assert.That(deselectedEventData, Is.Not.Null);
                Assert.That(first.IsSelected, Is.False);
                Assert.That(second.IsSelected, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
            }
        }

        /// <summary>
        /// 스케일 효과를 끈 경우에도 선택 상태와 이벤트 통지는 유지하는지 검증한다.
        /// </summary>
        [Test]
        public void Selection_WhenScaleIsDisabled_NotifiesAndUpdatesStateWithoutScaling()
        {
            EventSystem eventSystem = CreateEventSystem();
            UIButton button = CreateButton("Button");
            int selectedCount = 0;
            int deselectedCount = 0;
            SetSelectScaleEnabled(button, false);

            try
            {
                button.OnSelected += _ => selectedCount++;
                button.OnDeselected += _ => deselectedCount++;

                eventSystem.SetSelectedGameObject(button.gameObject);

                Assert.That(selectedCount, Is.EqualTo(1));
                Assert.That(button.IsSelected, Is.True);
                Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one));

                eventSystem.SetSelectedGameObject(null);

                Assert.That(deselectedCount, Is.EqualTo(1));
                Assert.That(button.IsSelected, Is.False);
                Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(button.gameObject);
            }
        }

        /// <summary>
        /// 포인터 Hover는 스케일 효과만 적용하고 선택 이벤트를 발생시키지 않는지 검증한다.
        /// </summary>
        [Test]
        public void PointerHover_DoesNotNotifySelectionAndPreservesScaleFeedback()
        {
            EventSystem eventSystem = CreateEventSystem();
            UIButton button = CreateButton("Button");
            PointerEventData pointerEventData = new PointerEventData(eventSystem);
            int selectedCount = 0;
            int deselectedCount = 0;

            try
            {
                button.OnSelected += _ => selectedCount++;
                button.OnDeselected += _ => deselectedCount++;

                button.OnPointerEnter(pointerEventData);

                Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one * 1.05f));
                Assert.That(selectedCount, Is.Zero);
                Assert.That(deselectedCount, Is.Zero);
                Assert.That(button.IsSelected, Is.False);

                button.OnPointerExit(pointerEventData);

                Assert.That(button.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(selectedCount, Is.Zero);
                Assert.That(deselectedCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(button.gameObject);
            }
        }

        /// <summary>
        /// 포인터 클릭과 Submit이 기존 UIButton 클릭 이벤트를 계속 전달하고 Navigation 전역 이벤트는 발생시키지 않는지 검증한다.
        /// </summary>
        [Test]
        public void PointerClickAndSubmit_ForwardToExistingClickEvents()
        {
            EventSystem eventSystem = CreateEventSystem();
            UIButton button = CreateButton("Button");
            PointerEventData pointerEventData = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left
            };
            BaseEventData submitEventData = new BaseEventData(eventSystem);
            int clickCount = 0;
            int anyClickCount = 0;
            int navigationCount = 0;

            try
            {
                button.OnClick += () => clickCount++;
                UIButton.OnAnyClicked += HandleAnyClicked;
                UIButton.OnAnyNavigationSelected += HandleNavigationSelected;

                ExecuteEvents.Execute<IPointerClickHandler>(
                    button.gameObject,
                    pointerEventData,
                    ExecuteEvents.pointerClickHandler);
                ExecuteEvents.Execute<ISubmitHandler>(
                    button.gameObject,
                    submitEventData,
                    ExecuteEvents.submitHandler);

                Assert.That(clickCount, Is.EqualTo(2));
                Assert.That(anyClickCount, Is.EqualTo(2));
                Assert.That(navigationCount, Is.Zero);
            }
            finally
            {
                UIButton.OnAnyClicked -= HandleAnyClicked;
                UIButton.OnAnyNavigationSelected -= HandleNavigationSelected;
                Object.DestroyImmediate(button.gameObject);
            }

            void HandleAnyClicked()
            {
                anyClickCount++;
            }

            void HandleNavigationSelected()
            {
                navigationCount++;
            }
        }

        /// <summary>
        /// 선택 이벤트 구독자가 없거나 버튼이 비활성화 및 파괴돼도 추가 선택 통지가 없는지 검증한다.
        /// </summary>
        [Test]
        public void SelectionLifecycle_DoesNotRequireSubscribersOrNotifyOnDisableAndDestroy()
        {
            EventSystem eventSystem = CreateEventSystem();
            UIButton buttonWithoutSubscribers = CreateButton("WithoutSubscribers");
            UIButton button = CreateButton("Button");
            int selectedCount = 0;
            int deselectedCount = 0;

            try
            {
                Assert.DoesNotThrow(() => eventSystem.SetSelectedGameObject(buttonWithoutSubscribers.gameObject));
                Assert.DoesNotThrow(() => eventSystem.SetSelectedGameObject(null));

                button.OnSelected += _ => selectedCount++;
                button.OnDeselected += _ => deselectedCount++;
                eventSystem.SetSelectedGameObject(button.gameObject);
                selectedCount = 0;
                deselectedCount = 0;

                button.gameObject.SetActive(false);
                Object.DestroyImmediate(button.gameObject);

                Assert.That(selectedCount, Is.Zero);
                Assert.That(deselectedCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(buttonWithoutSubscribers.gameObject);
            }
        }

        /// <summary>
        /// AxisEventData 기반 방향 Navigation 선택만 전역 이벤트를 1회 통지하는지 검증한다.
        /// </summary>
        [Test]
        public void NavigationSelection_WithAxisEventData_NotifiesGlobalOnce()
        {
            EventSystem eventSystem = CreateEventSystem();
            UIButton first = CreateButton("First");
            UIButton second = CreateButton("Second");
            Navigation firstNavigation = first.Button.navigation;
            firstNavigation.mode = Navigation.Mode.Explicit;
            firstNavigation.selectOnDown = second.Button;
            first.Button.navigation = firstNavigation;
            int navigationCount = 0;
            int selectedCount = 0;

            try
            {
                UIButton.OnAnyNavigationSelected += HandleNavigationSelected;
                second.OnSelected += _ => selectedCount++;

                eventSystem.SetSelectedGameObject(first.gameObject);
                Assert.That(navigationCount, Is.Zero);

                AxisEventData axisEventData = new AxisEventData(eventSystem)
                {
                    moveDir = MoveDirection.Down
                };
                ExecuteEvents.Execute(
                    first.gameObject,
                    axisEventData,
                    ExecuteEvents.moveHandler);

                Assert.That(eventSystem.currentSelectedGameObject, Is.EqualTo(second.gameObject));
                Assert.That(navigationCount, Is.EqualTo(1));
                Assert.That(selectedCount, Is.EqualTo(1));
                Assert.That(second.IsSelected, Is.True);

                // 이동 대상이 없으면 추가 통지하지 않는다.
                AxisEventData noTargetEventData = new AxisEventData(eventSystem)
                {
                    moveDir = MoveDirection.Down
                };
                ExecuteEvents.Execute(
                    second.gameObject,
                    noTargetEventData,
                    ExecuteEvents.moveHandler);
                Assert.That(navigationCount, Is.EqualTo(1));

                // 같은 Button을 AxisEventData로 재선택해도 EventSystem이 early return 하므로 중복 통지하지 않는다.
                eventSystem.SetSelectedGameObject(second.gameObject, new AxisEventData(eventSystem));
                Assert.That(navigationCount, Is.EqualTo(1));
            }
            finally
            {
                UIButton.OnAnyNavigationSelected -= HandleNavigationSelected;
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
            }

            void HandleNavigationSelected()
            {
                navigationCount++;
            }
        }

        /// <summary>
        /// PointerEventData로 선택하면 전역 Navigation 이벤트가 발생하지 않는지 검증한다.
        /// </summary>
        [Test]
        public void NavigationSelection_WithPointerEventData_DoesNotNotifyGlobal()
        {
            EventSystem eventSystem = CreateEventSystem();
            UIButton button = CreateButton("Button");
            int navigationCount = 0;
            int selectedCount = 0;

            try
            {
                UIButton.OnAnyNavigationSelected += HandleNavigationSelected;
                button.OnSelected += _ => selectedCount++;

                eventSystem.SetSelectedGameObject(button.gameObject, new PointerEventData(eventSystem));

                Assert.That(selectedCount, Is.EqualTo(1));
                Assert.That(navigationCount, Is.Zero);
                Assert.That(button.IsSelected, Is.True);
            }
            finally
            {
                UIButton.OnAnyNavigationSelected -= HandleNavigationSelected;
                Object.DestroyImmediate(button.gameObject);
            }

            void HandleNavigationSelected()
            {
                navigationCount++;
            }
        }

        /// <summary>
        /// 일반 BaseEventData 기반 프로그램 선택은 전역 Navigation 이벤트를 발생시키지 않는지 검증한다.
        /// </summary>
        [Test]
        public void NavigationSelection_WithBaseEventData_DoesNotNotifyGlobal()
        {
            EventSystem eventSystem = CreateEventSystem();
            UIButton button = CreateButton("Button");
            int navigationCount = 0;
            int selectedCount = 0;

            try
            {
                UIButton.OnAnyNavigationSelected += HandleNavigationSelected;
                button.OnSelected += _ => selectedCount++;

                eventSystem.SetSelectedGameObject(button.gameObject);

                Assert.That(selectedCount, Is.EqualTo(1));
                Assert.That(navigationCount, Is.Zero);
            }
            finally
            {
                UIButton.OnAnyNavigationSelected -= HandleNavigationSelected;
                Object.DestroyImmediate(button.gameObject);
            }

            void HandleNavigationSelected()
            {
                navigationCount++;
            }
        }

        /// <summary>
        /// 전역 Navigation 이벤트 구독 해제 후 추가 통지가 없고, 구독자가 없어도 예외가 없는지 검증한다.
        /// </summary>
        [Test]
        public void NavigationSelection_AfterUnsubscribe_DoesNotNotifyGlobal()
        {
            EventSystem eventSystem = CreateEventSystem();
            UIButton button = CreateButton("Button");
            int navigationCount = 0;

            try
            {
                UIButton.OnAnyNavigationSelected += HandleNavigationSelected;

                eventSystem.SetSelectedGameObject(
                    button.gameObject,
                    new AxisEventData(eventSystem) { moveDir = MoveDirection.Right });
                Assert.That(navigationCount, Is.EqualTo(1));

                UIButton.OnAnyNavigationSelected -= HandleNavigationSelected;
                navigationCount = 0;

                eventSystem.SetSelectedGameObject(null);
                Assert.DoesNotThrow(() =>
                    eventSystem.SetSelectedGameObject(
                        button.gameObject,
                        new AxisEventData(eventSystem) { moveDir = MoveDirection.Left }));
                Assert.That(navigationCount, Is.Zero);
            }
            finally
            {
                UIButton.OnAnyNavigationSelected -= HandleNavigationSelected;
                Object.DestroyImmediate(button.gameObject);
            }

            void HandleNavigationSelected()
            {
                navigationCount++;
            }
        }

        /// <summary>
        /// EventSystem을 생성한다.
        /// </summary>
        private static EventSystem CreateEventSystem()
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            return eventSystemObject.GetComponent<EventSystem>();
        }

        /// <summary>
        /// UIButton을 포함한 테스트용 Button 오브젝트를 생성한다.
        /// </summary>
        private static UIButton CreateButton(string name)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            return buttonObject.AddComponent<UIButton>();
        }

        /// <summary>
        /// 선택 스케일 옵션을 테스트 조건에 맞게 변경한다.
        /// </summary>
        private static void SetSelectScaleEnabled(UIButton button, bool enabled)
        {
            typeof(UIButton)
                .GetField("enableSelectScale", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(button, enabled);
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
