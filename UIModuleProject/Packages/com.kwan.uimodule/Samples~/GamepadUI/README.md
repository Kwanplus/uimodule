# Gamepad UI Sample

Package Manager에서 이 Sample을 import한 뒤 아래 Scene 중 하나를 엽니다.

## 무설정 흐름

`Scenes/GamepadUIZeroConfig.unity`는 `GamepadUISampleBootstrap`을 포함합니다.

- EventSystem과 기본 UI Action이 자동 생성됩니다.
- 방향키 또는 Gamepad D-pad/스틱으로 Button과 Slider를 이동하고 South/Submit으로 Popup을 엽니다.
- 첫 Popup의 **Open Nested Popup**을 선택하고 East/Cancel을 누르면 한 단계씩 닫히며 포커스가 복원됩니다.

## 선택 설정 흐름

`Scenes/GamepadUICustomInput.unity`는 `OptionalInputConfigurationSample`과 `CustomUIInputConfiguration.asset`을 포함합니다.

- `CustomUI.inputactions`의 비표준 `MoveSelection`, `ConfirmSelection`, `DismissPanel`이 Navigate, Submit, Cancel에 연결돼 있습니다.
- Gamepad left stick, South, East로 각각 Navigate, Submit, Cancel을 확인합니다.
- 이 Scene은 3×3 동적 Grid와 runtime `UIFocusScope` Custom Cancel을 사용하며, 표준 `UI/*` Action을 사용하지 않는 프로젝트의 선택 설정 예제입니다.

두 Scene은 모두 프리팹 없이 UI를 동적으로 만들므로 pooling을 끕니다. 같은 Scene에 두 Bootstrap을 함께 배치하지 마세요.
