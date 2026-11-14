using BenchLink.UI;

using HarmonyLib;

namespace BenchLink.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
internal static class Player_PlacePiece_Patch
{
    private static void Postfix(Piece piece)
    {
        PlacementNotice.Placed(piece);
    }
}
