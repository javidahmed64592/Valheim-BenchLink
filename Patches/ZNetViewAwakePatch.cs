using BenchLink.Clusters;

using HarmonyLib;

namespace BenchLink.Patches;

/// <summary>Remembers which objects were made as ghosts before their ZNetView destroys itself.</summary>
[HarmonyPatch(typeof(ZNetView), "Awake")]
internal static class ZNetView_Awake_Patch
{
    private static void Prefix(ZNetView __instance)
    {
        GhostStations.MarkIfDisabled(__instance);
    }
}
