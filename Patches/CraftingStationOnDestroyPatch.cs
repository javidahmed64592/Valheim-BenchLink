using BenchLink.Clusters;

using HarmonyLib;

namespace BenchLink.Patches;

[HarmonyPatch(typeof(CraftingStation), "OnDestroy")]
internal static class CraftingStation_OnDestroy_Patch
{
    private static void Postfix()
    {
        StationClusters.Invalidate();
    }
}
