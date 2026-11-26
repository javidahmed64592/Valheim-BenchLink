using BenchLink.Core;
using BenchLink.UI;

using HarmonyLib;

namespace BenchLink.Patches;

/// <summary>Shift+E on a station highlights its cluster instead of opening the crafting menu.</summary>
[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
[Feature(ClusterHighlight.Feature)]
internal static class CraftingStation_Interact_Patch
{
    private static bool Prefix(CraftingStation __instance, Humanoid user, bool repeat, bool alt, ref bool __result)
    {
        if (!alt)
        {
            return true;
        }

        __result = false;
        if (!repeat && user == Player.m_localPlayer && user is Player player)
        {
            ClusterHighlight.Show(__instance, player);
        }

        return false;
    }
}
