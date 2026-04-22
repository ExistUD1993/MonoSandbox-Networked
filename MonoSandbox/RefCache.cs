using UnityEngine;

namespace MonoSandbox
{
    public static class RefCache
    {
        public static GameObject LHand;
        public static GameObject RHand;
        public static GameObject SandboxContainer;

        public static bool HitExists;
        public static RaycastHit Hit;

        public static Material Default;
        public static Material Selection;
        public static AudioClip PageSelection;
        public static AudioClip ItemSelection;
    }
}
