using System.Reflection;
using HarmonyLib;

namespace MonoSandbox.Patches
{
    [HarmonyPatch]
    public class PlayerInitializePatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(GorillaTagger), "Start");
        }

        public static void Postfix(GorillaTagger __instance)
        {
            RefCache.LHand = __instance.offlineVRRig.leftHandTransform.parent.Find("palm.01.L").gameObject;
            RefCache.RHand = __instance.offlineVRRig.rightHandTransform.parent.Find("palm.01.R").gameObject;
        }
    }
}
