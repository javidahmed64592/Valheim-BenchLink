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
    private const float HoldSeconds = 5f;
    private const float FadeSeconds = 1.5f;

    private static float _placedAt = float.NegativeInfinity;
    private static CraftingStation _justPlaced;
    private static float _fadeAt = float.PositiveInfinity;
    private static string _shownText;

    public static void Tick()
    {
        FadeWhenDue();
        StationClusters.Poll();

        if (StationClusters.TryTakeChanged(out string changed))
        {
            Show("Cluster " + Localization.instance.Localize(changed) + " changed!");
        }

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

        Show("Duplicate " + Localization.instance.Localize(station.m_name) + ": it won't extend the cluster");
    }

    /// <summary>
    /// Vanilla starts fading a centre message at once and takes 4 s, which is too quick to read, so drop
    /// that fade and run our own after a hold.
    /// </summary>
    private static void Show(string text)
    {
        MessageHud hud = MessageHud.instance;
        if (hud == null)
        {
            return;
        }

        int before = hud._crossFadeTextBuffer.Count;
        hud.ShowMessage(MessageHud.MessageType.Center, text);
        if (hud._crossFadeTextBuffer.Count != before + 2)
        {
            return;
        }

        hud._crossFadeTextBuffer.RemoveAt(before + 1);
        _shownText = hud.m_messageCenterText.text;
        _fadeAt = Time.unscaledTime + HoldSeconds;
    }

    private static void FadeWhenDue()
    {
        if (Time.unscaledTime < _fadeAt)
        {
            return;
        }

        _fadeAt = float.PositiveInfinity;
        MessageHud hud = MessageHud.instance;

        // Another centre message may have replaced ours; it has its own fade.
        if (hud != null && hud.m_messageCenterText.text == _shownText)
        {
            hud.m_messageCenterText.CrossFadeAlpha(0f, FadeSeconds, ignoreTimeScale: true);
        }
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
