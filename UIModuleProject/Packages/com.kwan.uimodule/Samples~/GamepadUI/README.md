# Gamepad UI Sample

빈 Scene에 `GamepadUISampleBootstrap` 컴포넌트를 추가하고 Play Mode를 시작합니다.

- Input System이 활성화된 프로젝트에서는 EventSystem과 기본 UI 액션이 자동 생성됩니다.
- 방향키 또는 게임패드 D-pad/스틱으로 Button을 이동하고 Submit으로 Popup을 엽니다.
- Popup이 열리면 Cancel로 닫히며 기존 Screen Button의 선택이 복원됩니다.

이 예제는 프리팹 없이 동적 UI를 생성하므로 pooling을 끕니다. 실제 프로젝트에서는 `Resources` 프리팹과 UI Dashboard 워크플로를 그대로 사용해도 됩니다.
