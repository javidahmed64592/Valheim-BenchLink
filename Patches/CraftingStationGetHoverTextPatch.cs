using BenchLink.Core;
using BenchLink.UI;

using HarmonyLib;

namespace BenchLink.Patches;

[HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetHoverText))]
[Feature(ClusterHighlight.Feature)]
internal static class CraftingStation_GetHoverText_Patch
{
    private static void Postfix(CraftingStation __instance, ref string __result)
    {
        Player player = Player.m_localPlayer;
        if (player != null && __instance.InUseDistance(player))
        {
            __result += Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $benchlink_show_linked");
        }
    }
}
