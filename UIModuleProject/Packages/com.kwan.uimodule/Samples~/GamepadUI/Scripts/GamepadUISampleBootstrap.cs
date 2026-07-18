using UnityEngine;
using UnityEngine.UI;
using UIModule;

namespace UIModule.GamepadSample
{
    /// <summary>
    /// 프리팹 없이도 무설정 Navigate, Submit, Cancel과 Popup 포커스 복원을 확인하는 샘플 진입점이다.
    /// </summary>
    public class GamepadUISampleBootstrap : MonoBehaviour
    {
        private void Start()
        {
            UIManager.Instance.SetPoolingEnabled(false);
            UIManager.Instance.ShowScreen<GamepadUISampleScreen>();
        }
    }

    /// <summary>
    /// 동적으로 Button을 구성하는 기본 샘플 Screen이다.
    /// </summary>
    public class GamepadUISampleScreen : BaseScreen
    {
        protected override void OnScreenInitialize()
        {
            CreateButton("Open Popup", new Vector2(0f, 80f), () => UIManager.Instance.ShowPopup<GamepadUISamplePopup>());
            CreateButton("Back", new Vector2(0f, 0f), () => UIManager.Instance.BackScreen());
        }

        protected override void OnScreenBegin()
        {
        }

        protected override void OnScreenHide()
        {
        }

        protected override void OnScreenDestroy()
        {
        }

        /// <summary>
        /// 샘플 Button을 생성한다.
        /// </summary>
        private void CreateButton(string buttonName, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(260f, 56f);
            rectTransform.anchoredPosition = position;
            buttonObject.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.85f, 1f);
            buttonObject.GetComponent<Button>().onClick.AddListener(action);
        }
    }

    /// <summary>
    /// Cancel로 닫히며 이전 Screen의 선택을 복원하는 샘플 Popup이다.
    /// </summary>
    public class GamepadUISamplePopup : BasePopup
    {
        protected override void OnPopupInitialize()
        {
            CreateButton("Close", new Vector2(0f, 0f), Close);
        }

        protected override void OnPopupShow()
        {
        }

        protected override void OnPopupHide()
        {
        }

        protected override void OnPopupDestroy()
        {
        }

        /// <summary>
        /// Popup 닫기 Button을 생성한다.
        /// </summary>
        private void CreateButton(string buttonName, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(240f, 56f);
            rectTransform.anchoredPosition = position;
            buttonObject.GetComponent<Image>().color = new Color(0.65f, 0.3f, 0.2f, 1f);
            buttonObject.GetComponent<Button>().onClick.AddListener(action);
        }
    }
}
