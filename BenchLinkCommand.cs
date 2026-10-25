using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Jotunn.Entities;

using UnityEngine;

namespace BenchLink;

/// <summary>Debugging aid: prints the station clusters near the player.</summary>
internal sealed class BenchLinkCommand : ConsoleCommand
{
    private const float NearbyDistance = 100f;
    private const int MaxAreaCells = 100000;

    public override string Name => "benchlink";

    public override string Help => "Lists the linked crafting station clusters near you: members, ranges, " +
        "link distances, and the area covered with and without linking.";

    public override void Run(string[] args)
    {
        Run(args, Console.instance);
    }

    public override void Run(string[] args, Terminal context)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            context.AddString("benchlink: no player in the world.");
            return;
        }

        StationGraph graph = StationClusters.Graph;
        IReadOnlyList<CraftingStation> stations = StationClusters.Stations;
        Vector3 origin = player.transform.position;

        context.AddString("BenchLink: LinkSlack " + BenchLinkConfig.LinkSlack.Value.ToString("0.#", CultureInfo.InvariantCulture) +
            ", " + graph.Count + " stations loaded");

        int shown = 0;
        for (int cluster = 0; cluster < graph.ClusterCount; cluster++)
        {
            if (!IsNear(graph, cluster, origin))
            {
                continue;
            }

            shown++;
            Describe(context, graph, stations, cluster, origin);
        }

        if (shown == 0)
        {
            context.AddString("No stations within " + NearbyDistance + " m of you.");
        }
    }

    private static bool IsNear(StationGraph graph, int cluster, Vector3 origin)
    {
        for (int i = 0; i < graph.Count; i++)
        {
            if (graph.ClusterOf(i) == cluster
                && StationGraph.Distance(graph[i], new StationNode(origin.x, origin.z, 0f, -1)) < NearbyDistance)
            {
                return true;
            }
        }

        return false;
    }

    private static void Describe(
        Terminal context,
        StationGraph graph,
        IReadOnlyList<CraftingStation> stations,
        int cluster,
        Vector3 origin)
    {
        context.AddString("Cluster " + cluster + " (" + graph.ClusterSize(cluster) + " stations)");

        StationNode originNode = new(origin.x, origin.z, 0f, -1);
        for (int i = 0; i < graph.Count; i++)
        {
            if (graph.ClusterOf(i) != cluster)
            {
                continue;
            }

            StationNode node = graph[i];
            string inRange = graph.Covers(i, origin.x, origin.z) ? ", you are in range" : string.Empty;
            context.AddString("  [" + i + "] " + Localization.instance.Localize(stations[i].m_name) +
                " level " + stations[i].GetLevel() +
                ", range " + node.Range.ToString("0.#", CultureInfo.InvariantCulture) + " m" +
                ", " + StationGraph.Distance(node, originNode).ToString("0.#", CultureInfo.InvariantCulture) + " m from you" + inRange);
        }

        StringBuilder links = new();
        for (int i = 0; i < graph.Count; i++)
        {
            for (int j = i + 1; j < graph.Count; j++)
            {
                if (graph.ClusterOf(i) == cluster && graph.ClusterOf(j) == cluster
                    && StationGraph.AreLinked(graph[i], graph[j], BenchLinkConfig.LinkSlack.Value))
                {
                    _ = links.Append("  [").Append(i).Append("]-[").Append(j).Append("] ")
                        .Append(StationGraph.Distance(graph[i], graph[j]).ToString("0.#", CultureInfo.InvariantCulture)).Append(" m\n");
                }
            }
        }

        if (links.Length > 0)
        {
            context.AddString("  links:\n" + links.ToString().TrimEnd('\n'));
        }

        (double vanilla, double linked) = graph.EstimateAreas(cluster, MaxAreaCells);
        context.AddString("  area with every type in range: vanilla " + vanilla.ToString("N0", CultureInfo.InvariantCulture) +
            " m2, linked " + linked.ToString("N0", CultureInfo.InvariantCulture) + " m2 (any bench in range)");
    }
}
