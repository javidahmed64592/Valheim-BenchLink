using System.Collections.Generic;

using UnityEngine;

namespace BenchLink;

/// <summary>
/// Builds the link graph over the loaded crafting stations and caches it. The cache is rebuilt when a station
/// appears or disappears, and at most once a second otherwise so upgrades and downgrades (which change a
/// station's range) are picked up without doing any work every frame.
/// </summary>
internal static class StationClusters
{
    private const float RefreshInterval = 1f;

    private static readonly Dictionary<string, int> TypeIds = [];
    private static readonly List<CraftingStation> StationList = [];

    private static StationGraph _graph;
    private static float _builtAt;
    private static float _builtSlack;
    private static int _builtVersion = -1;

    /// <summary>Bumped whenever a station is added or removed.</summary>
    public static int Version { get; private set; }

    /// <summary>The loaded stations, in the same order as the nodes of <see cref="Graph"/>.</summary>
    public static IReadOnlyList<CraftingStation> Stations
    {
        get
        {
            Refresh();
            return StationList;
        }
    }

    public static StationGraph Graph
    {
        get
        {
            Refresh();
            return _graph;
        }
    }

    public static void Invalidate()
    {
        Version++;
    }

    /// <summary>Finds a station of the named type that serves the point through a link, if any.</summary>
    public static CraftingStation FindLinked(string name, Vector3 point)
    {
        Refresh();
        if (!TypeIds.TryGetValue(name, out int type))
        {
            return null;
        }

        int index = _graph.FindLinked(point.x, point.z, type);
        return index < 0 ? null : StationList[index];
    }

    public static string NameOf(int type)
    {
        foreach (KeyValuePair<string, int> entry in TypeIds)
        {
            if (entry.Value == type)
            {
                return entry.Key;
            }
        }

        return string.Empty;
    }

    private static void Refresh()
    {
        float age = Time.time - _builtAt;
        if (_graph != null && _builtVersion == Version && _builtSlack == BenchLinkConfig.LinkSlack.Value
            && age >= 0f && age < RefreshInterval)
        {
            return;
        }

        Rebuild();
    }

    private static void Rebuild()
    {
        StationList.Clear();
        foreach (CraftingStation station in CraftingStation.m_allStations)
        {
            if (station != null)
            {
                StationList.Add(station);
            }
        }

        StationNode[] nodes = new StationNode[StationList.Count];
        for (int i = 0; i < nodes.Length; i++)
        {
            CraftingStation station = StationList[i];
            Vector3 position = station.transform.position;
            nodes[i] = new StationNode(position.x, position.z, station.GetStationBuildRange(), TypeOf(station.m_name));
        }

        _builtSlack = BenchLinkConfig.LinkSlack.Value;
        _graph = new StationGraph(nodes, _builtSlack);
        _builtAt = Time.time;
        _builtVersion = Version;
    }

    private static int TypeOf(string name)
    {
        if (!TypeIds.TryGetValue(name, out int type))
        {
            type = TypeIds.Count;
            TypeIds[name] = type;
        }

        return type;
    }
}
