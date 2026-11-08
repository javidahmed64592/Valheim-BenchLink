using HarmonyLib;

using UnityEngine;

namespace BenchLink;

/// <summary>
/// <para>
/// Lays a crafting station's range ring on the floor around it rather than on the terrain underneath. The game
/// places each segment of the ring by casting down from far above onto the ground only, so on a raised floor or
/// platform the ring hides beneath it. This is the same placement, but the cast also stops at building pieces and
/// starts just below head height above the station: floors at the station's level (and steps up from it) are
/// found, while roofs and upper storeys are passed over. Where nothing is found that way (a hill rising higher than
/// that), the game's own placement is kept.
/// </para>
/// <para>
/// The station's own ring is the one both the game (when you hover with a hammer) and the range indicator show, so
/// this covers both. Wards and other rings are left alone.
/// </para>
/// </summary>
[HarmonyPatch(typeof(CircleProjector), "Update")]
internal static class FloorRings
{
    /// <summary>
    /// How far above the station the downward cast starts. Below a roof or an upper floor, and below the top of a
    /// two-metre wall: a cast that starts inside a collider passes through it, so walls do not catch the ring.
    /// </summary>
    private const float Headroom = 1.8f;

    /// <summary>How far below the station the cast reaches, for a floor laid lower down a slope.</summary>
    private const float Depth = 30f;

    private static readonly int PieceMask = LayerMask.GetMask("piece");

    private static bool Prefix(CircleProjector __instance)
    {
        if (__instance.GetComponentInParent<CraftingStation>() == null)
        {
            return true;
        }

        Place(__instance);
        return false;
    }

    /// <summary>The game's segment layout and orientation, with the height found by <see cref="SurfaceHeight"/>.</summary>
    private static void Place(CircleProjector ring)
    {
        ring.CreateSegments();
        int count = ring.m_nrOfSegments;
        Vector3 centre = ring.transform.position;
        int mask = ring.m_mask.value;

        bool full = ring.m_turns == 1f;
        float step = Mathf.PI * 2f * ring.m_turns / (count - (full ? 0 : 1));
        float spin = full && !ring.m_sliceLines ? Time.time * ring.m_speed : 0f;
        for (int i = 0; i < count; i++)
        {
            float angle = (Mathf.PI / 180f * ring.m_start) + (i * step) + spin;
            Vector3 position = centre + new Vector3(Mathf.Sin(angle) * ring.m_radius, 0f, Mathf.Cos(angle) * ring.m_radius);
            position.y = SurfaceHeight(position, centre.y, mask);
            ring.m_segments[i].transform.position = position;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject segment = ring.m_segments[i];
            GameObject previous;
            GameObject next;
            if (full)
            {
                previous = ring.m_segments[i == 0 ? count - 1 : i - 1];
                next = ring.m_segments[i == count - 1 ? 0 : i + 1];
            }
            else
            {
                previous = i == 0 ? segment : ring.m_segments[i - 1];
                next = i == count - 1 ? segment : ring.m_segments[i + 1];
            }

            Vector3 along = (next.transform.position - previous.transform.position).normalized;
            segment.transform.rotation = Quaternion.LookRotation(along, Vector3.up);
        }

        for (int i = count; i < ring.m_segments.Count; i++)
        {
            Transform slice = ring.m_segments[i].transform;
            Vector3 position = slice.position;
            position.y = SurfaceHeight(position, centre.y, mask);
            slice.position = position;
        }
    }

    /// <summary>
    /// The top of the floor or ground at the point, looking down from just below head height above the station,
    /// or the ground as the game finds it if there is nothing there.
    /// </summary>
    private static float SurfaceHeight(Vector3 point, float stationHeight, int groundMask)
    {
        Vector3 from = new(point.x, stationHeight + Headroom, point.z);
        bool found = Physics.Raycast(from, Vector3.down, out RaycastHit hit, Headroom + Depth, groundMask | PieceMask, QueryTriggerInteraction.Ignore)
            || Physics.Raycast(point + (Vector3.up * 500f), Vector3.down, out hit, 1000f, groundMask);
        return found ? hit.point.y : point.y;
    }
}
