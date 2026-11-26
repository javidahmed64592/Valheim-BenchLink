using UnityEngine;

namespace BenchLink.Clusters;

/// <summary>
/// Placement ghosts (and other previews made with networking switched off) aren't real stations. The game
/// makes them with <see cref="ZNetView.m_forceDisableInit"/>, which destroys their ZNetView in Awake, and
/// <c>CraftingStation.Start</c> registers a station with no ZNetView as a real one. A workbench ghost on the
/// ground then counts as a workbench in range, e.g. for removing pieces with no workbench nearby.
/// </summary>
internal static class GhostStations
{
    /// <summary>Marks an object whose ZNetView was switched off when it was made.</summary>
    internal sealed class Marker : MonoBehaviour;

    internal static void MarkIfDisabled(ZNetView view)
    {
        if (ZNetView.m_forceDisableInit && view.GetComponent<Marker>() == null)
        {
            _ = view.gameObject.AddComponent<Marker>();
        }
    }

    internal static bool IsGhost(CraftingStation station)
    {
        return station.GetComponentInParent<Marker>() != null;
    }
}
