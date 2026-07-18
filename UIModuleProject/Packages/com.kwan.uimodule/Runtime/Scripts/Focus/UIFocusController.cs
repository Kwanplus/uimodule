using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UIModule
{
    /// <summary>
    /// UIManager의 스택 수명주기에 맞춰 EventSystem 선택을 기록하고 복원한다.
    /// </summary>
    internal sealed class UIFocusController
    {
        private readonly UIManager _manager;
        private readonly Dictionary<BaseUI, GameObject> _lastSelectedByUi = new Dictionary<BaseUI, GameObject>();
        private readonly Dictionary<BasePopup, List<Selectable>> _blockedSelectablesByPopup = new Dictionary<BasePopup, List<Selectable>>();
        private readonly Dictionary<Selectable, bool> _originalInteractableBySelectable = new Dictionary<Selectable, bool>();
        private readonly Dictionary<Selectable, int> _modalBlockCounts = new Dictionary<Selectable, int>();
        private bool _isDisposed;

        /// <summary>
        /// 포커스 controller를 생성한다.
        /// </summary>
        internal UIFocusController(UIManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// UI가 표시된 직후 Popup 모달 상태를 준비한다.
        /// </summary>
        internal void HandleShown(BaseUI ui)
        {
            AttachLifetimeRelay(ui);
            if (ui is BasePopup popup)
            {
                BlockLowerSelectables(popup);
                ScheduleFocus(popup);
            }
            else if (!(ui is BaseScreen))
            {
                ScheduleFocus(ui);
            }
        }

        /// <summary>
        /// 새 Popup이 최상위가 되기 전 아래 UI의 마지막 선택을 기록한다.
        /// </summary>
        internal void HandleBeforePopupShown(BasePopup popup)
        {
            BaseUI lowerUi = _manager.GetTopInputUI();
            if (lowerUi != null && lowerUi != popup)
            {
                RememberSelection(lowerUi);
            }
        }

        /// <summary>
        /// Screen의 동적 콘텐츠 생성이 끝난 뒤 최초 선택을 예약한다.
        /// </summary>
        internal void HandleScreenBegan(BaseScreen screen)
        {
            ScheduleFocus(screen);
        }

        /// <summary>
        /// UI가 숨겨지기 전에 현재 선택을 저장하고 stale selection을 제거한다.
        /// </summary>
        internal void HandleHiding(BaseUI ui)
        {
            EventSystem eventSystem = _manager.EventSystem;
            if (eventSystem != null && IsWithin(ui, eventSystem.currentSelectedGameObject))
            {
                _lastSelectedByUi[ui] = eventSystem.currentSelectedGameObject;
                eventSystem.SetSelectedGameObject(null);
            }

            if (ui is BasePopup popup)
            {
                RestoreLowerSelectables(popup);
            }
        }

        /// <summary>
        /// UI가 숨겨진 뒤 새 최상위 UI의 선택을 복원한다.
        /// </summary>
        internal void HandleHidden(BaseUI ui)
        {
            if (ui is BasePopup)
            {
                _lastSelectedByUi.Remove(ui);
            }

            BaseUI topUi = _manager.GetTopInputUI();
            if (topUi != null)
            {
                ScheduleFocus(topUi);
            }
        }

        /// <summary>
        /// Navigate 입력 중 선택이 없거나 무효하면 현재 범위의 유효한 선택으로 복구한다.
        /// </summary>
        internal void EnsureSelectionForNavigation()
        {
            BaseUI topUi = _manager.GetTopInputUI();
            if (topUi == null)
            {
                return;
            }

            EventSystem eventSystem = _manager.EventSystem;
            if (eventSystem == null || !IsSelectableInUi(topUi, eventSystem.currentSelectedGameObject))
            {
                SelectBestTarget(topUi);
            }
        }

        /// <summary>
        /// Scope가 요청한 경우 포인터 클릭 뒤 게임패드 선택 표시를 해제한다.
        /// </summary>
        internal void ApplyPointerSelectionPolicy()
        {
            if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame)
            {
                return;
            }

            BaseUI topUi = _manager.GetTopInputUI();
            UIFocusScope scope = topUi == null ? null : topUi.GetComponent<UIFocusScope>();
            if (scope != null && !scope.KeepSelectionOnPointerInput)
            {
                _manager.EventSystem?.SetSelectedGameObject(null);
            }
        }

        /// <summary>
        /// UI의 현재 유효 선택을 저장한다.
        /// </summary>
        internal void RememberSelection(BaseUI ui)
        {
            EventSystem eventSystem = _manager.EventSystem;
            if (eventSystem != null && IsSelectableInUi(ui, eventSystem.currentSelectedGameObject))
            {
                _lastSelectedByUi[ui] = eventSystem.currentSelectedGameObject;
            }
        }

        /// <summary>
        /// UI가 파괴되거나 풀로 반환될 때 기록을 제거한다.
        /// </summary>
        internal void Forget(BaseUI ui)
        {
            _lastSelectedByUi.Remove(ui);
            if (ui is BasePopup popup)
            {
                RestoreLowerSelectables(popup);
            }
        }

        /// <summary>
        /// 외부 Destroy 또는 Pool clear로 UI가 제거될 때 호출한다.
        /// </summary>
        internal void HandleDestroyed(BaseUI ui)
        {
            if (_isDisposed)
            {
                return;
            }

            Forget(ui);
            _manager.HandleExternallyDestroyedUi(ui);
            BaseUI topUi = _manager.GetTopInputUI();
            if (topUi != null)
            {
                ScheduleFocus(topUi);
            }
        }

        /// <summary>
        /// UIManager 종료 시 남은 모달 차단을 원상 복구한다.
        /// </summary>
        internal void Dispose()
        {
            _isDisposed = true;
            foreach (KeyValuePair<Selectable, bool> pair in _originalInteractableBySelectable)
            {
                if (pair.Key != null)
                {
                    pair.Key.interactable = pair.Value;
                }
            }

            _blockedSelectablesByPopup.Clear();
            _originalInteractableBySelectable.Clear();
            _modalBlockCounts.Clear();
            _lastSelectedByUi.Clear();
        }

        /// <summary>
        /// 레이아웃이 반영된 다음 프레임 말미에 포커스를 적용한다.
        /// </summary>
        private void ScheduleFocus(BaseUI ui)
        {
            _manager.StartCoroutine(FocusAfterLayout(ui));
        }

        /// <summary>
        /// LayoutGroup이 만든 동적 Selectable까지 찾을 수 있게 렌더 전까지 대기한다.
        /// </summary>
        private IEnumerator FocusAfterLayout(BaseUI expectedUi)
        {
            // Batchmode PlayMode 테스트와 timeScale=0에서도 같은 프레임 경계를 사용하기 위해
            // WaitForEndOfFrame 대신 다음 Update까지 대기한 뒤 Canvas를 강제 갱신한다.
            yield return null;
            Canvas.ForceUpdateCanvases();

            if (expectedUi == null || !expectedUi.IsActive || _manager.GetTopInputUI() != expectedUi)
            {
                yield break;
            }

            SelectBestTarget(expectedUi);
        }

        /// <summary>
        /// 명시 대상, 저장 대상, 첫 Selectable 순으로 선택한다.
        /// </summary>
        private void SelectBestTarget(BaseUI ui)
        {
            EventSystem eventSystem = _manager.EventSystem;
            if (eventSystem == null)
            {
                return;
            }

            UIFocusScope scope = ui.GetComponent<UIFocusScope>();
            Selectable explicitTarget = scope == null ? null : scope.DefaultSelection;
            if (IsSelectableValid(explicitTarget))
            {
                eventSystem.SetSelectedGameObject(explicitTarget.gameObject);
                return;
            }

            if (_lastSelectedByUi.TryGetValue(ui, out GameObject rememberedTarget) && IsSelectableInUi(ui, rememberedTarget))
            {
                eventSystem.SetSelectedGameObject(rememberedTarget);
                return;
            }

            Selectable firstSelectable = FindFirstSelectable(ui);
            eventSystem.SetSelectedGameObject(firstSelectable == null ? null : firstSelectable.gameObject);
        }

        /// <summary>
        /// Popup이 표시된 동안 아래 범위의 Selectable을 잠시 비활성화한다.
        /// </summary>
        private void BlockLowerSelectables(BasePopup popup)
        {
            UIFocusScope scope = popup.GetComponent<UIFocusScope>();
            if (scope != null && !scope.BlocksLowerScopes)
            {
                return;
            }

            List<Selectable> blockedSelectables = new List<Selectable>();
            foreach (Selectable selectable in Selectable.allSelectablesArray)
            {
                if (selectable == null
                    || !selectable.gameObject.activeInHierarchy
                    || IsWithin(popup, selectable.gameObject)
                    || !_manager.IsManagedUiObject(selectable.gameObject))
                {
                    continue;
                }

                if (!_originalInteractableBySelectable.ContainsKey(selectable))
                {
                    _originalInteractableBySelectable.Add(selectable, selectable.interactable);
                    _modalBlockCounts.Add(selectable, 0);
                }

                _modalBlockCounts[selectable]++;
                blockedSelectables.Add(selectable);
                selectable.interactable = false;
            }

            _blockedSelectablesByPopup[popup] = blockedSelectables;
        }

        /// <summary>
        /// Popup이 닫힐 때 이전 Selectable의 원래 상태를 복구한다.
        /// </summary>
        private void RestoreLowerSelectables(BasePopup popup)
        {
            if (!_blockedSelectablesByPopup.TryGetValue(popup, out List<Selectable> blockedSelectables))
            {
                return;
            }

            foreach (Selectable selectable in blockedSelectables)
            {
                if (selectable == null || !_modalBlockCounts.TryGetValue(selectable, out int count))
                {
                    continue;
                }

                count--;
                if (count > 0)
                {
                    _modalBlockCounts[selectable] = count;
                    continue;
                }

                if (_originalInteractableBySelectable.TryGetValue(selectable, out bool wasInteractable))
                {
                    selectable.interactable = wasInteractable;
                }

                _modalBlockCounts.Remove(selectable);
                _originalInteractableBySelectable.Remove(selectable);
            }

            _blockedSelectablesByPopup.Remove(popup);
        }

        /// <summary>
        /// 외부 Destroy에서도 상태를 회수할 수 있도록 UI에 수명주기 relay를 붙인다.
        /// </summary>
        private void AttachLifetimeRelay(BaseUI ui)
        {
            UIFocusLifetimeRelay relay = ui.GetComponent<UIFocusLifetimeRelay>();
            if (relay == null)
            {
                relay = ui.gameObject.AddComponent<UIFocusLifetimeRelay>();
            }

            relay.Initialize(this, ui);
        }

        /// <summary>
        /// UI 범위의 첫 활성 Selectable을 반환한다.
        /// </summary>
        private static Selectable FindFirstSelectable(BaseUI ui)
        {
            Selectable[] selectables = ui.GetComponentsInChildren<Selectable>(true);
            foreach (Selectable selectable in selectables)
            {
                if (IsSelectableValid(selectable))
                {
                    return selectable;
                }
            }

            return null;
        }

        /// <summary>
        /// Selectable이 선택 가능한지 반환한다.
        /// </summary>
        private static bool IsSelectableValid(Selectable selectable)
        {
            return selectable != null
                && selectable.gameObject.activeInHierarchy
                && selectable.IsInteractable();
        }

        /// <summary>
        /// 선택 오브젝트가 UI 범위에 속하고 유효한 Selectable인지 반환한다.
        /// </summary>
        private static bool IsSelectableInUi(BaseUI ui, GameObject target)
        {
            if (!IsWithin(ui, target))
            {
                return false;
            }

            return IsSelectableValid(target.GetComponent<Selectable>());
        }

        /// <summary>
        /// 대상이 UI 하위인지 반환한다.
        /// </summary>
        private static bool IsWithin(BaseUI ui, GameObject target)
        {
            return ui != null && target != null && target.transform.IsChildOf(ui.transform);
        }
    }
}
