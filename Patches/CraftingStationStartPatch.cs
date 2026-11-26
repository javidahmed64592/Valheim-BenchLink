using BenchLink.Clusters;
using BenchLink.UI;

using HarmonyLib;

namespace BenchLink.Patches;

/// <summary>
/// Stations register themselves in Start, so the clusters change when one starts. Ghosts are taken back out
/// of the game's station list so they never count as a station in range (see <see cref="GhostStations"/>).
/// </summary>
[HarmonyPatch(typeof(CraftingStation), "Start")]
internal static class CraftingStation_Start_Patch
{
    private static void Postfix(CraftingStation __instance)
    {
        if (GhostStations.IsGhost(__instance))
        {
            _ = CraftingStation.m_allStations.Remove(__instance);
            return;
        }

        StationClusters.Invalidate();
        PlacementNotice.Started(__instance);
    }
}
