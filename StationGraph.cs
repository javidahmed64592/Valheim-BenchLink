using System;

namespace BenchLink;

/// <summary>A crafting station reduced to what the link maths needs, with no Unity types.</summary>
internal readonly struct StationNode(float x, float z, float range, int type)
{
    public float X { get; } = x;
    public float Z { get; } = z;
    public float Range { get; } = range;

    /// <summary>Identifies the kind of station (workbench, forge, ...); stations of one kind share an id.</summary>
    public int Type { get; } = type;
}

/// <summary>
/// Links stations whose ranges reach each other and groups them into clusters.
/// Everything is measured on the horizontal plane, like the game's build range.
/// </summary>
internal sealed class StationGraph
{
    private readonly StationNode[] _nodes;
    private readonly int[] _cluster;
    private readonly int[] _stamp;
    private int _stampId;

    public StationGraph(StationNode[] nodes, float linkSlack)
    {
        _nodes = nodes;
        _cluster = new int[nodes.Length];
        _stamp = new int[nodes.Length];
        ClusterCount = BuildClusters(linkSlack);
    }

    public int Count => _nodes.Length;
    public int ClusterCount { get; }

    public StationNode this[int index] => _nodes[index];

    /// <summary>The cluster a station belongs to, numbered from 0 in order of first appearance.</summary>
    public int ClusterOf(int index)
    {
        return _cluster[index];
    }

    public int ClusterSize(int cluster)
    {
        int size = 0;
        for (int i = 0; i < _cluster.Length; i++)
        {
            if (_cluster[i] == cluster)
            {
                size++;
            }
        }

        return size;
    }

    public static float Distance(in StationNode a, in StationNode b)
    {
        return Distance(a.X, a.Z, b.X, b.Z);
    }

    /// <summary>Two stations link when they are closer than the larger of their two ranges, plus the slack.</summary>
    public static bool AreLinked(in StationNode a, in StationNode b, float linkSlack)
    {
        return Distance(a, b) < Math.Max(a.Range, b.Range) + linkSlack;
    }

    public bool Covers(int index, float x, float z)
    {
        return Distance(_nodes[index].X, _nodes[index].Z, x, z) < _nodes[index].Range;
    }

    /// <summary>
    /// Finds the station of the wanted type that serves a point: one whose cluster has a station
    /// covering the point. Returns the one nearest the point, or -1 if there is none.
    /// </summary>
    public int FindLinked(float x, float z, int type)
    {
        _stampId++;
        bool anyCovered = false;
        for (int i = 0; i < _nodes.Length; i++)
        {
            if (Covers(i, x, z))
            {
                _stamp[_cluster[i]] = _stampId;
                anyCovered = true;
            }
        }

        if (!anyCovered)
        {
            return -1;
        }

        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i].Type != type || _stamp[_cluster[i]] != _stampId)
            {
                continue;
            }

            float distance = Distance(_nodes[i].X, _nodes[i].Z, x, z);
            if (distance < bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>True if some station of the type in the cluster covers the point: vanilla reach for that type.</summary>
    public bool TypeCoversVanilla(int cluster, int type, float x, float z)
    {
        for (int i = 0; i < _nodes.Length; i++)
        {
            if (_cluster[i] == cluster && _nodes[i].Type == type && Covers(i, x, z))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True if any station in the cluster covers the point: reach once the cluster is linked.</summary>
    public bool ClusterCovers(int cluster, float x, float z)
    {
        for (int i = 0; i < _nodes.Length; i++)
        {
            if (_cluster[i] == cluster && Covers(i, x, z))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Estimates, by sampling a grid, the area covered by every type in the cluster without linking (vanilla)
    /// and the area covered by any station in it (linked). Metres squared. The grid is coarsened
    /// until it has at most maxCells cells, which keeps big clusters cheap.
    /// </summary>
    public (double vanilla, double linked) EstimateAreas(int cluster, int maxCells)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;
        for (int i = 0; i < _nodes.Length; i++)
        {
            if (_cluster[i] != cluster)
            {
                continue;
            }

            minX = Math.Min(minX, _nodes[i].X - _nodes[i].Range);
            maxX = Math.Max(maxX, _nodes[i].X + _nodes[i].Range);
            minZ = Math.Min(minZ, _nodes[i].Z - _nodes[i].Range);
            maxZ = Math.Max(maxZ, _nodes[i].Z + _nodes[i].Range);
        }

        float cellSize = 1f;
        while ((maxX - minX) / cellSize * ((maxZ - minZ) / cellSize) > maxCells)
        {
            cellSize *= 2f;
        }

        int[] types = TypesIn(cluster);
        long vanillaCells = 0;
        long linkedCells = 0;
        for (float x = minX + (cellSize / 2); x < maxX; x += cellSize)
        {
            for (float z = minZ + (cellSize / 2); z < maxZ; z += cellSize)
            {
                if (!ClusterCovers(cluster, x, z))
                {
                    continue;
                }

                linkedCells++;
                if (CoversEveryType(cluster, types, x, z))
                {
                    vanillaCells++;
                }
            }
        }

        double cellArea = (double)cellSize * cellSize;
        return (vanillaCells * cellArea, linkedCells * cellArea);
    }

    /// <summary>Area of the overlap of two circles of radii r1 and r2 whose centres are d apart.</summary>
    public static double LensArea(double r1, double r2, double d)
    {
        if (d >= r1 + r2)
        {
            return 0;
        }

        if (d <= Math.Abs(r1 - r2))
        {
            double smaller = Math.Min(r1, r2);
            return Math.PI * smaller * smaller;
        }

        double a1 = r1 * r1 * Math.Acos(((d * d) + (r1 * r1) - (r2 * r2)) / (2 * d * r1));
        double a2 = r2 * r2 * Math.Acos(((d * d) + (r2 * r2) - (r1 * r1)) / (2 * d * r2));
        double kite = 0.5 * Math.Sqrt((-d + r1 + r2) * (d + r1 - r2) * (d - r1 + r2) * (d + r1 + r2));
        return a1 + a2 - kite;
    }

    /// <summary>Area covered by at least one of two circles.</summary>
    public static double UnionArea(double r1, double r2, double d)
    {
        return (Math.PI * r1 * r1) + (Math.PI * r2 * r2) - LensArea(r1, r2, d);
    }

    private static float Distance(float ax, float az, float bx, float bz)
    {
        float dx = ax - bx;
        float dz = az - bz;
        return (float)Math.Sqrt((dx * dx) + (dz * dz));
    }

    private bool CoversEveryType(int cluster, int[] types, float x, float z)
    {
        foreach (int type in types)
        {
            if (!TypeCoversVanilla(cluster, type, x, z))
            {
                return false;
            }
        }

        return true;
    }

    private int[] TypesIn(int cluster)
    {
        int[] types = new int[_nodes.Length];
        int count = 0;
        for (int i = 0; i < _nodes.Length; i++)
        {
            if (_cluster[i] == cluster && Array.IndexOf(types, _nodes[i].Type, 0, count) < 0)
            {
                types[count++] = _nodes[i].Type;
            }
        }

        Array.Resize(ref types, count);
        return types;
    }

    /// <summary>Union-find over the link graph; returns the number of clusters.</summary>
    private int BuildClusters(float linkSlack)
    {
        int[] parent = new int[_nodes.Length];
        for (int i = 0; i < parent.Length; i++)
        {
            parent[i] = i;
        }

        for (int i = 0; i < _nodes.Length; i++)
        {
            for (int j = i + 1; j < _nodes.Length; j++)
            {
                if (AreLinked(_nodes[i], _nodes[j], linkSlack))
                {
                    parent[Find(parent, i)] = Find(parent, j);
                }
            }
        }

        int[] clusterOfRoot = new int[_nodes.Length];
        for (int i = 0; i < clusterOfRoot.Length; i++)
        {
            clusterOfRoot[i] = -1;
        }

        int clusters = 0;
        for (int i = 0; i < _nodes.Length; i++)
        {
            int root = Find(parent, i);
            if (clusterOfRoot[root] < 0)
            {
                clusterOfRoot[root] = clusters++;
            }

            _cluster[i] = clusterOfRoot[root];
        }

        return clusters;
    }

    private static int Find(int[] parent, int i)
    {
        while (parent[i] != i)
        {
            parent[i] = parent[parent[i]];
            i = parent[i];
        }

        return i;
    }
}
