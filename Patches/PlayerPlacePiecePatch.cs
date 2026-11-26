using BenchLink.Core;
using BenchLink.UI;

using HarmonyLib;

namespace BenchLink.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
[Feature(Feature)]
internal static class Player_PlacePiece_Patch
{
    /// <summary>Without it, there is no notice when a new station repeats a type in its cluster.</summary>
    public const string Feature = "Placement notice";

    private static void Postfix(Piece piece)
    {
        PlacementNotice.Placed(piece);
    }
}
