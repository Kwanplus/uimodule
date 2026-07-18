# Gamepad UI Sample

## 무설정 흐름

빈 Scene에 `GamepadUISampleBootstrap` 컴포넌트를 추가하고 Play Mode를 시작합니다.

- Input System이 활성화된 프로젝트에서는 EventSystem과 기본 UI 액션이 자동 생성됩니다.
- 방향키 또는 게임패드 D-pad/스틱으로 Button을 이동하고 Submit으로 Popup을 엽니다.
- Popup이 열리면 Cancel로 닫히며 기존 Screen Button의 선택이 복원됩니다.

이 예제는 프리팹 없이 동적 UI를 생성하므로 pooling을 끕니다. 실제 프로젝트에서는 `Resources` 프리팹과 UI Dashboard 워크플로를 그대로 사용해도 됩니다.

## 선택 설정 흐름

`CustomUI.inputactions`를 열어 `MoveSelection`, `ConfirmSelection`, `DismissPanel`을 확인합니다. `UIInputConfiguration` 에셋을 만들고 각각 Navigate, Submit, Cancel에 연결한 뒤, 빈 Scene의 `OptionalInputConfigurationSample`에 이 에셋을 할당합니다.

이 흐름은 표준 `UI/*` 이름을 사용하지 않는 프로젝트에서만 필요합니다. 두 Bootstrap은 각각 UIManager를 만들므로 같은 Scene에 함께 배치하지 마세요.
