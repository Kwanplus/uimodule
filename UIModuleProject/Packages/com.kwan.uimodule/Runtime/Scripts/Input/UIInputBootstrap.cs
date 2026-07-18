using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace UIModule
{
    /// <summary>
    /// EventSystem과 UI 입력 모듈을 생성하거나 안전하게 재사용한다.
    /// </summary>
    internal static class UIInputBootstrap
    {
        private static readonly HashSet<string> ReportedDiagnostics = new HashSet<string>();

        /// <summary>
        /// UI 입력에 사용할 EventSystem을 준비한다.
        /// </summary>
        /// <param name="owner">자동 생성된 EventSystem의 부모다.</param>
        /// <param name="configuration">선택적 액션 역할 override다.</param>
        /// <returns>준비된 EventSystem 또는 null이다.</returns>
        internal static EventSystem Ensure(Transform owner, UIInputConfiguration configuration)
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                ValidateExistingEventSystem(eventSystem);
                return eventSystem;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(owner, false);
            eventSystem = eventSystemObject.AddComponent<EventSystem>();

            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            ConfigureOwnedInputModule(inputModule, configuration);
            return eventSystem;
        }

        /// <summary>
        /// 자동 생성한 모듈에 선택 설정 또는 프로젝트 전역 UI 액션을 연결한다.
        /// </summary>
        private static void ConfigureOwnedInputModule(InputSystemUIInputModule inputModule, UIInputConfiguration configuration)
        {
            if (configuration != null)
            {
                ApplyConfiguration(inputModule, configuration);
                return;
            }

            InputActionAsset projectActions = GetProjectWideActions();
            if (projectActions == null)
            {
                return;
            }

            ApplyProjectWideActions(inputModule, projectActions);
        }

        /// <summary>
        /// 명시 설정에서 할당된 역할만 덮어쓴다.
        /// 기본 모듈 액션은 비어 있는 역할의 fallback으로 유지된다.
        /// </summary>
        private static void ApplyConfiguration(InputSystemUIInputModule inputModule, UIInputConfiguration configuration)
        {
            inputModule.move = configuration.Navigate ?? inputModule.move;
            inputModule.submit = configuration.Submit ?? inputModule.submit;
            inputModule.cancel = configuration.Cancel ?? inputModule.cancel;
            inputModule.point = configuration.Point ?? inputModule.point;
            inputModule.leftClick = configuration.Click ?? inputModule.leftClick;
            inputModule.rightClick = configuration.RightClick ?? inputModule.rightClick;
            inputModule.middleClick = configuration.MiddleClick ?? inputModule.middleClick;
            inputModule.scrollWheel = configuration.ScrollWheel ?? inputModule.scrollWheel;
            inputModule.trackedDevicePosition = configuration.TrackedDevicePosition ?? inputModule.trackedDevicePosition;
            inputModule.trackedDeviceOrientation = configuration.TrackedDeviceOrientation ?? inputModule.trackedDeviceOrientation;
        }

        /// <summary>
        /// 프로젝트 전역 액션의 표준 UI 역할을 찾아 연결한다.
        /// </summary>
        private static void ApplyProjectWideActions(InputSystemUIInputModule inputModule, InputActionAsset projectActions)
        {
            inputModule.move = FindActionReference(projectActions, "Navigate") ?? inputModule.move;
            inputModule.submit = FindActionReference(projectActions, "Submit") ?? inputModule.submit;
            inputModule.cancel = FindActionReference(projectActions, "Cancel") ?? inputModule.cancel;
            inputModule.point = FindActionReference(projectActions, "Point") ?? inputModule.point;
            inputModule.leftClick = FindActionReference(projectActions, "Click") ?? inputModule.leftClick;
            inputModule.rightClick = FindActionReference(projectActions, "RightClick") ?? inputModule.rightClick;
            inputModule.middleClick = FindActionReference(projectActions, "MiddleClick") ?? inputModule.middleClick;
            inputModule.scrollWheel = FindActionReference(projectActions, "ScrollWheel") ?? inputModule.scrollWheel;
            inputModule.trackedDevicePosition = FindActionReference(projectActions, "TrackedDevicePosition") ?? inputModule.trackedDevicePosition;
            inputModule.trackedDeviceOrientation = FindActionReference(projectActions, "TrackedDeviceOrientation") ?? inputModule.trackedDeviceOrientation;
        }

        /// <summary>
        /// UI 맵 우선으로 표준 액션을 찾는다.
        /// </summary>
        private static InputActionReference FindActionReference(InputActionAsset actions, string actionName)
        {
            InputAction action = actions.FindAction($"UI/{actionName}", false) ?? actions.FindAction(actionName, false);
            return action == null ? null : InputActionReference.Create(action);
        }

        /// <summary>
        /// Input System 버전별 조건부 API에 직접 의존하지 않고 프로젝트 전역 액션을 조회한다.
        /// </summary>
        private static InputActionAsset GetProjectWideActions()
        {
            PropertyInfo property = typeof(InputSystem).GetProperty("actions", BindingFlags.Static | BindingFlags.Public);
            return property?.GetValue(null) as InputActionAsset;
        }

        /// <summary>
        /// 기존 EventSystem을 변경하지 않고 충돌 원인만 진단한다.
        /// </summary>
        private static void ValidateExistingEventSystem(EventSystem eventSystem)
        {
            BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
            int enabledModules = 0;
            InputSystemUIInputModule inputSystemModule = null;

            foreach (BaseInputModule module in modules)
            {
                if (!module.enabled)
                {
                    continue;
                }

                enabledModules++;
                if (module is InputSystemUIInputModule candidate)
                {
                    inputSystemModule = candidate;
                }
            }

            if (enabledModules == 0)
            {
                ReportOnce(
                    "NoInputModule",
                    "[UIModule] 기존 EventSystem에 활성 입력 모듈이 없습니다. InputSystemUIInputModule 또는 StandaloneInputModule을 하나 활성화하세요.");
                return;
            }

            if (enabledModules > 1)
            {
                ReportOnce(
                    "MultipleInputModules",
                    "[UIModule] 기존 EventSystem에 활성 입력 모듈이 여러 개입니다. 하나만 활성화해야 Navigate/Submit/Cancel 중복을 피할 수 있습니다.");
            }

            if (inputSystemModule == null)
            {
                ReportOnce(
                    "NonInputSystemModule",
                    "[UIModule] 기존 EventSystem이 InputSystemUIInputModule을 사용하지 않습니다. New Input System 게임패드 UI를 사용하려면 기존 모듈을 교체하거나 별도 EventSystem 구성을 검토하세요.");
            }
        }

        /// <summary>
        /// 동일한 구성 문제를 한 번만 출력한다.
        /// </summary>
        private static void ReportOnce(string key, string message)
        {
            if (ReportedDiagnostics.Add(key))
            {
                Debug.LogWarning(message);
            }
        }
    }
}
