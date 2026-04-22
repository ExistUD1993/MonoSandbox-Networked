using UnityEngine;

namespace MonoSandbox.Behaviours
{
    public class InputHandling : MonoBehaviour
    {
        public static float LeftTrigger, RightTrigger, LeftGrip, RightGrip;
        public static bool LeftPrimary, RightPrimary, LeftSecondary, RightSecondary;

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
