using BenchLink.Clusters;

using HarmonyLib;

using UnityEngine;

namespace BenchLink.Patches;

/// <summary>
/// Every build-range check for a piece's required station (placing, removing and the hammer menu's
/// requirement list) goes through this method, so one postfix covers them all. Vanilla runs first and
/// we only step in when it found nothing, or when it picked a duplicate that is not linked while the
/// linked station of that type is also in range (the hammer menu highlights the station returned).
/// </summary>
[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.HaveBuildStationInRange))]
internal static class CraftingStation_HaveBuildStationInRange_Patch
{
    private static void Postfix(string name, Vector3 point, ref CraftingStation __result)
    {
        if (__result != null)
        {
            __result = StationClusters.PreferLinked(__result, point);
            return;
        }

        __result = StationClusters.FindLinked(name, point);
    }
}
