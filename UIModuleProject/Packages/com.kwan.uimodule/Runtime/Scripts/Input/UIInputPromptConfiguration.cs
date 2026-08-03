using UnityEngine;

namespace UIModule
{
    /// <summary>
    /// 공용 Xbox 입력 프롬프트 Sprite를 관리한다.
    /// </summary>
    [CreateAssetMenu(fileName = "UIInputPromptConfiguration", menuName = "UIModule/UI Input Prompt Configuration")]
    public class UIInputPromptConfiguration : ScriptableObject
    {
        [Header("Face Buttons")]
        [SerializeField] private Sprite _south;
        [SerializeField] private Sprite _east;
        [SerializeField] private Sprite _west;
        [SerializeField] private Sprite _north;

        [Header("Shoulder And Trigger")]
        [SerializeField] private Sprite _leftBumper;
        [SerializeField] private Sprite _rightBumper;
        [SerializeField] private Sprite _leftTrigger;
        [SerializeField] private Sprite _rightTrigger;

        [Header("Sticks And DPad")]
        [SerializeField] private Sprite _leftStick;
        [SerializeField] private Sprite _rightStick;
        [SerializeField] private Sprite _dPadUp;
        [SerializeField] private Sprite _dPadDown;
        [SerializeField] private Sprite _dPadLeft;
        [SerializeField] private Sprite _dPadRight;

        [Header("System Buttons")]
        [SerializeField] private Sprite _view;
        [SerializeField] private Sprite _menu;

        /// <summary>
        /// 지정한 Xbox 버튼에 대응하는 Sprite를 반환한다.
        /// </summary>
        /// <param name="buttonType">조회할 Xbox 버튼 종류다.</param>
        public Sprite GetSprite(XboxButtonType buttonType)
        {
            switch (buttonType)
            {
                case XboxButtonType.South:
                    return _south;
                case XboxButtonType.East:
                    return _east;
                case XboxButtonType.West:
                    return _west;
                case XboxButtonType.North:
                    return _north;
                case XboxButtonType.LB:
                    return _leftBumper;
                case XboxButtonType.RB:
                    return _rightBumper;
                case XboxButtonType.LT:
                    return _leftTrigger;
                case XboxButtonType.RT:
                    return _rightTrigger;
                case XboxButtonType.LeftStick:
                    return _leftStick;
                case XboxButtonType.RightStick:
                    return _rightStick;
                case XboxButtonType.DPadUp:
                    return _dPadUp;
                case XboxButtonType.DPadDown:
                    return _dPadDown;
                case XboxButtonType.DPadLeft:
                    return _dPadLeft;
                case XboxButtonType.DPadRight:
                    return _dPadRight;
                case XboxButtonType.View:
                    return _view;
                case XboxButtonType.Menu:
                    return _menu;
                default:
                    return null;
            }
        }
    }
}
