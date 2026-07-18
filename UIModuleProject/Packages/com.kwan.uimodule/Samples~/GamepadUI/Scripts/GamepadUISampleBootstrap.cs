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
            CreateButton("Open Popup", new Vector2(0f, 110f), () => UIManager.Instance.ShowPopup<GamepadUISamplePopup>());
            CreateSlider(new Vector2(0f, 25f));
            CreateButton("Back", new Vector2(0f, -70f), () => UIManager.Instance.BackScreen());
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

        /// <summary>
        /// 기본 Input System Navigate에서 좌우 값 변경을 확인할 Slider를 만든다.
        /// </summary>
        private void CreateSlider(Vector2 position)
        {
            GameObject sliderObject = new GameObject("Sample Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderObject.transform.SetParent(transform, false);
            RectTransform rectTransform = sliderObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(260f, 28f);
            rectTransform.anchoredPosition = position;
            sliderObject.GetComponent<Slider>().value = 0.5f;
        }
    }

    /// <summary>
    /// Cancel로 닫히며 이전 Screen의 선택을 복원하는 샘플 Popup이다.
    /// </summary>
    public class GamepadUISamplePopup : BasePopup
    {
        protected override void OnPopupInitialize()
        {
            CreateButton("Open Nested Popup", new Vector2(0f, 36f), () => UIManager.Instance.ShowPopup<GamepadUINestedPopup>());
            CreateButton("Close", new Vector2(0f, -36f), Close);
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

    /// <summary>
    /// 중첩 Popup의 단계별 Cancel과 포커스 복원을 확인하는 샘플이다.
    /// </summary>
    public class GamepadUINestedPopup : BasePopup
    {
        protected override void OnPopupInitialize()
        {
            GameObject buttonObject = new GameObject("Close Nested", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(240f, 56f);
            buttonObject.GetComponent<Button>().onClick.AddListener(Close);
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
    }
}
