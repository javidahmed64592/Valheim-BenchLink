using HarmonyLib;

using UnityEngine;

namespace BenchLink;

/// <summary>
/// Tells the player when a station they have just built repeats a type in the cluster it stands in,
/// so it will not extend that cluster. A station only joins the link graph once it has started, a frame
/// after it is placed, so the check waits for that.
/// </summary>
internal static class PlacementNotice
{
    private const float PlacementWindow = 1f;

    private static float _placedAt = float.NegativeInfinity;
    private static CraftingStation _justPlaced;

    public static void Tick()
    {
        if (_justPlaced == null)
        {
            return;
        }

        CraftingStation station = _justPlaced;
        _justPlaced = null;

        int index = StationClusters.IndexOf(station);
        Player player = Player.m_localPlayer;
        if (index < 0 || player == null || !StationClusters.Graph.IsDuplicate(index))
        {
            return;
        }

        string name = Localization.instance.Localize(station.m_name);
        player.Message(MessageHud.MessageType.Center,
            "This " + name + " is not linked: the cluster it is in already has one, so it will not extend it.");
    }

    internal static void Placed(Piece piece)
    {
        if (piece.GetComponentInChildren<CraftingStation>() != null)
        {
            _placedAt = Time.time;
        }
    }

    internal static void Started(CraftingStation station)
    {
        if (Time.time - _placedAt < PlacementWindow)
        {
            _placedAt = float.NegativeInfinity;
            _justPlaced = station;
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
internal static class PlacePiecePatch
{
    private static void Postfix(Piece piece)
    {
        PlacementNotice.Placed(piece);
    }
}
