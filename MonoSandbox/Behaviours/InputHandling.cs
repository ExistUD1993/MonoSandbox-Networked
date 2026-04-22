using UnityEngine;

namespace MonoSandbox.Behaviours
{
    public class InputHandling : MonoBehaviour
    {
        public static float LeftTrigger;
        public static float RightTrigger;
        public static float LeftGrip;
        public static float RightGrip;

        public static bool LeftPrimary;
        public static bool RightPrimary;
        public static bool LeftSecondary;
        public static bool RightSecondary;

        public void Update()
        {
            ControllerInputPoller input = ControllerInputPoller.instance;

            LeftTrigger = input.leftControllerIndexFloat;
            LeftGrip = input.leftControllerGripFloat;
            RightTrigger = input.rightControllerIndexFloat;
            RightGrip = input.rightControllerGripFloat;
            LeftPrimary = input.leftControllerPrimaryButton;
            LeftSecondary = input.leftControllerSecondaryButton;
            RightPrimary = input.rightControllerPrimaryButton;
            RightSecondary = input.rightControllerSecondaryButton;
        }
    }
}
