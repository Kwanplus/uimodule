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
            UIInputActionReferenceOwner referenceOwner = eventSystemObject.AddComponent<UIInputActionReferenceOwner>();
            ConfigureOwnedInputModule(inputModule, configuration, referenceOwner);
            ValidateRequiredActions(inputModule);
            return eventSystem;
        }

        /// <summary>
        /// 자동 생성한 모듈에 선택 설정 또는 프로젝트 전역 UI 액션을 연결한다.
        /// </summary>
        private static void ConfigureOwnedInputModule(
            InputSystemUIInputModule inputModule,
            UIInputConfiguration configuration,
            UIInputActionReferenceOwner referenceOwner)
        {
            InputActionAsset projectActions = GetProjectWideActions();
            if (configuration != null)
            {
                ApplyConfiguration(inputModule, configuration, projectActions, referenceOwner);
                return;
            }

            if (projectActions == null)
            {
                return;
            }

            ApplyProjectWideActions(inputModule, projectActions, referenceOwner);
        }

        /// <summary>
        /// 명시 설정에서 할당된 역할만 덮어쓴다.
        /// 기본 모듈 액션은 비어 있는 역할의 fallback으로 유지된다.
        /// </summary>
        private static void ApplyConfiguration(
            InputSystemUIInputModule inputModule,
            UIInputConfiguration configuration,
            InputActionAsset projectActions,
            UIInputActionReferenceOwner referenceOwner)
        {
            inputModule.move = ResolveAction(configuration.Navigate, projectActions, "Navigate", inputModule.move, referenceOwner);
            inputModule.submit = ResolveAction(configuration.Submit, projectActions, "Submit", inputModule.submit, referenceOwner);
            inputModule.cancel = ResolveAction(configuration.Cancel, projectActions, "Cancel", inputModule.cancel, referenceOwner);
            inputModule.point = ResolveAction(configuration.Point, projectActions, "Point", inputModule.point, referenceOwner);
            inputModule.leftClick = ResolveAction(configuration.Click, projectActions, "Click", inputModule.leftClick, referenceOwner);
            inputModule.rightClick = ResolveAction(configuration.RightClick, projectActions, "RightClick", inputModule.rightClick, referenceOwner);
            inputModule.middleClick = ResolveAction(configuration.MiddleClick, projectActions, "MiddleClick", inputModule.middleClick, referenceOwner);
            inputModule.scrollWheel = ResolveAction(configuration.ScrollWheel, projectActions, "ScrollWheel", inputModule.scrollWheel, referenceOwner);
            inputModule.trackedDevicePosition = ResolveAction(configuration.TrackedDevicePosition, projectActions, "TrackedDevicePosition", inputModule.trackedDevicePosition, referenceOwner);
            inputModule.trackedDeviceOrientation = ResolveAction(configuration.TrackedDeviceOrientation, projectActions, "TrackedDeviceOrientation", inputModule.trackedDeviceOrientation, referenceOwner);
        }

        /// <summary>
        /// 명시 설정, 전역 UI 액션, 기본 모듈 액션 순서로 역할을 해석한다.
        /// </summary>
        private static InputActionReference ResolveAction(
            InputActionReference configuredAction,
            InputActionAsset projectActions,
            string actionName,
            InputActionReference defaultAction,
            UIInputActionReferenceOwner referenceOwner)
        {
            return configuredAction ?? FindActionReference(projectActions, actionName, referenceOwner) ?? defaultAction;
        }

        /// <summary>
        /// 프로젝트 전역 액션의 표준 UI 역할을 찾아 연결한다.
        /// </summary>
        private static void ApplyProjectWideActions(
            InputSystemUIInputModule inputModule,
            InputActionAsset projectActions,
            UIInputActionReferenceOwner referenceOwner)
        {
            inputModule.move = FindActionReference(projectActions, "Navigate", referenceOwner) ?? inputModule.move;
            inputModule.submit = FindActionReference(projectActions, "Submit", referenceOwner) ?? inputModule.submit;
            inputModule.cancel = FindActionReference(projectActions, "Cancel", referenceOwner) ?? inputModule.cancel;
            inputModule.point = FindActionReference(projectActions, "Point", referenceOwner) ?? inputModule.point;
            inputModule.leftClick = FindActionReference(projectActions, "Click", referenceOwner) ?? inputModule.leftClick;
            inputModule.rightClick = FindActionReference(projectActions, "RightClick", referenceOwner) ?? inputModule.rightClick;
            inputModule.middleClick = FindActionReference(projectActions, "MiddleClick", referenceOwner) ?? inputModule.middleClick;
            inputModule.scrollWheel = FindActionReference(projectActions, "ScrollWheel", referenceOwner) ?? inputModule.scrollWheel;
            inputModule.trackedDevicePosition = FindActionReference(projectActions, "TrackedDevicePosition", referenceOwner) ?? inputModule.trackedDevicePosition;
            inputModule.trackedDeviceOrientation = FindActionReference(projectActions, "TrackedDeviceOrientation", referenceOwner) ?? inputModule.trackedDeviceOrientation;
        }

        /// <summary>
        /// UI 맵 우선으로 표준 액션을 찾는다.
        /// </summary>
        private static InputActionReference FindActionReference(
            InputActionAsset actions,
            string actionName,
            UIInputActionReferenceOwner referenceOwner)
        {
            if (actions == null)
            {
                return null;
            }

            InputAction action = actions.FindAction($"UI/{actionName}", false) ?? actions.FindAction(actionName, false);
            return action == null ? null : referenceOwner.Own(InputActionReference.Create(action));
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
                return;
            }

            ValidateRequiredActions(inputSystemModule);
        }

        /// <summary>
        /// 기본 UI 조작에 필요한 역할이 비어 있으면 해결 방법을 한 번만 진단한다.
        /// </summary>
        private static void ValidateRequiredActions(InputSystemUIInputModule inputModule)
        {
            ValidateRequiredAction("Navigate", inputModule.move);
            ValidateRequiredAction("Submit", inputModule.submit);
            ValidateRequiredAction("Cancel", inputModule.cancel);
        }

        /// <summary>
        /// 개별 필수 역할의 누락을 진단한다.
        /// </summary>
        private static void ValidateRequiredAction(string role, InputActionReference action)
        {
            if (action != null && action.action != null)
            {
                return;
            }

            ReportOnce(
                $"Missing{role}",
                $"[UIModule] UI {role} 액션이 비어 있습니다. UIInputConfiguration에 할당하거나 프로젝트 전역 UI/{role} 액션을 추가하세요.");
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
