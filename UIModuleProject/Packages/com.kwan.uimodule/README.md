# UI Module

Unity 6000.3 기반 UGUI Screen/Popup 모듈입니다. New Input System과 함께 설치하면 단순 `Button`, `Slider` 기반 Screen/Popup은 별도 Input Action 설정 없이 키보드·마우스·게임패드 Navigate, Submit, Cancel, 포커스 복원을 지원합니다.

## 설치와 기본 사용

패키지는 `com.unity.inputsystem`, `com.unity.ugui`를 의존성으로 설치합니다. Scene에 `UIManager`를 두거나 `UIManager.Instance`를 호출하면 EventSystem이 없을 때 자동 생성됩니다.

```csharp
UIManager.Instance.ShowScreen<MainScreen>();
UIManager.Instance.ShowPopup<SettingsPopup>();
```

`UIInputBootstrap`이 생성한 `InputSystemUIInputModule`은 역할마다 **명시 설정 → 프로젝트 전역 `UI/*` 액션 → Input System 내장 UI 액션** 순으로 해석합니다. 기존 EventSystem과 유효한 입력 모듈이 있으면 이를 변경하지 않습니다. 활성 입력 모듈이 없거나 여러 개인 구성, 비호환 모듈, 필수 UI 역할 누락은 해결 방법과 함께 한 번만 Console에 진단됩니다.

## 선택 설정

프로젝트의 UI Action 이름이나 바인딩이 표준과 다르면 `UIInputConfiguration` 에셋을 만들고 `UIManager`의 **Input Configuration**에 할당합니다. 필요한 역할만 지정할 수 있고, 비어 있는 역할은 프로젝트 전역 `UI/*`, 그 다음 내장 기본 액션으로 fallback합니다.

설정이 없을 때 프로젝트 전역 Input Actions에 `UI/Navigate`, `UI/Submit`, `UI/Cancel`, `UI/Point`, `UI/Click` 등이 있으면 해당 액션을 우선 사용합니다.

## 포커스와 Cancel

- 최초 선택: `UIFocusScope` 기본 대상 → 이전 유효 선택 → 첫 활성 Selectable 순서입니다. Popup/Screen 복귀는 이전 유효 선택을 기본 대상보다 우선합니다.
- Popup 중첩: 아래 UI의 선택을 저장하고 닫힐 때 복원합니다. 기본 Popup은 아래 Selectable을 잠시 비활성화하여 Automatic Navigation 누수를 막습니다.
- 동적 Screen: `OnScreenBegin()` 이후 다음 프레임 말미에 포커스를 적용합니다.
- Cancel: 선택 항목이 없으면 최상위 활성 UI로 한 번 라우팅됩니다. Popup은 기존 `OnBackKeyPressed()`, Screen은 `BackScreen()`을 기본 동작으로 사용합니다.
- Default Cancel은 Popup과 Screen에서만 제공합니다. Background/Overlay/System은 `UIFocusScope`의 Ignore/Custom 정책 또는 선택된 `ICancelHandler`로 명시 처리하세요.
- 복합 UI는 `UIFocusScope`의 Default/Ignore/Custom Cancel 정책과 `UILinearNavigation`, `UIGridNavigation`, `UISpatialNavigation`, `UIEnsureVisibleInScrollRect`를 선택적으로 사용합니다. 런타임 Custom Cancel은 `AddCancelListener()`로 등록할 수 있습니다.

## 게임플레이 입력 경계

UI Module은 `PlayerInput`이나 게임플레이 Action Map을 참조·Enable·Disable하지 않습니다.

```csharp
uiManager.InputCaptureChanged += state =>
{
    // 소비 프로젝트가 게임플레이 입력 억제 정책을 결정합니다.
    // state.IsCaptured, state.Reason, state.PopupDepth를 사용합니다.
};
```

```csharp
uiManager.InputDeviceChanged += state =>
{
    // LastInputDevice: Keyboard, Pointer, Gamepad, Touch, Other
    // IsGamepadConnected: Gamepad 연결 여부
};
```

`LastInputDevice`는 실제 UI 액션을 수행한 장치만 반영합니다. Gamepad 연결·재연결은 `IsGamepadConnected`와 포커스만 갱신합니다.

## 검증과 샘플

- `Tools > UIModule > Validate Gamepad UI`에서 Default Selection, Navigation.None, Explicit 링크, Navigation Group(Grid/Spatial 포함), ScrollRect 구성을 검사합니다. Navigation Group은 배열이 비어 있으면 하위 Selectable 전체를, 배열이 있으면 그 배열의 항목만 관리 대상으로 판단합니다.
- Package Manager에서 **Gamepad UI** Sample을 import한 뒤 `Scenes/GamepadUIZeroConfig.unity` 또는 `Scenes/GamepadUICustomInput.unity`를 열어 무설정과 비표준 Action 연결 흐름을 확인할 수 있습니다. 외부 Popup 인스턴스는 `UIManager.ShowPopup(popup)`으로 등록하세요. 직접 `Show()`는 호환을 위해 자동 등록되지만 1회 경고를 냅니다.
