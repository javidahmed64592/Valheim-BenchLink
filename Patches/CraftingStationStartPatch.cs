using BenchLink.Clusters;
using BenchLink.UI;

using HarmonyLib;

namespace BenchLink.Patches;

/// <summary>Stations register themselves in Start, so the clusters change when one starts.</summary>
[HarmonyPatch(typeof(CraftingStation), "Start")]
internal static class CraftingStation_Start_Patch
{
    private static void Postfix(CraftingStation __instance)
    {
        StationClusters.Invalidate();
        PlacementNotice.Started(__instance);
    }
}
