using System.Collections.Generic;
using System.Globalization;

using BenchLink.Clusters;

using UnityEngine;

namespace BenchLink.UI;

/// <summary>
/// Lights up every station in a cluster for a few seconds so the player can find them: each glows Ember Gold and
/// gets a marker on the HUD, through walls, with its name, upgrade level and distance. Started with Shift+E on any station in the
/// cluster. Only the player who asked sees it.
/// </summary>
internal static class ClusterHighlight
{
    /// <summary>The same Ember Gold as the linked stations' range rings.</summary>
    internal static readonly Color Gold = new(0.941f, 0.690f, 0.290f);

    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private static readonly List<CraftingStation> Lit = [];
    private static readonly List<float> Heights = [];
    private static readonly List<string> Levels = [];
    private static readonly Dictionary<string, int> MaxLevels = [];
    private static readonly List<StationMarker> Markers = [];

    private static float _until;

    public static void Show(CraftingStation station, Player player)
    {
        Stop();

        int index = StationClusters.IndexOf(station);
        if (index < 0)
        {
            return;
        }

        StationGraph graph = StationClusters.Graph;
        IReadOnlyList<CraftingStation> stations = StationClusters.Stations;
        int cluster = graph.ClusterOf(index);
        for (int i = 0; i < stations.Count; i++)
        {
            if (graph.ClusterOf(i) == cluster && stations[i] != null)
            {
                Lit.Add(stations[i]);
                Heights.Add(HeightOf(stations[i]));
                Levels.Add(LevelOf(stations[i]));
            }
        }

        _until = Time.time + BenchLinkConfig.HighlightDuration.Value;

        player.Message(MessageHud.MessageType.TopLeft, Describe(station, graph.IsDuplicate(index)));
    }

    private static string Describe(CraftingStation station, bool duplicate)
    {
        if (Lit.Count > 1)
        {
            return Localization.instance.Localize("$benchlink_showing", Lit.Count.ToString(CultureInfo.InvariantCulture));
        }

        string name = Localization.instance.Localize(station.m_name);
        return Localization.instance.Localize(duplicate ? "$benchlink_duplicate_unlinked" : "$benchlink_alone", name);
    }

    public static void Tick()
    {
        if (Lit.Count == 0)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        Camera camera = Utils.GetMainCamera();
        if (player == null || camera == null || Time.time >= _until)
        {
            Stop();
            return;
        }

        float pulse = Mathf.Lerp(0.3f, 0.7f, (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f);
        Vector3 from = player.transform.position;
        for (int i = 0; i < Lit.Count; i++)
        {
            StationMarker marker = MarkerAt(i);
            CraftingStation station = Lit[i];
            if (station == null)
            {
                marker.Hide();
                continue;
            }

            MaterialMan.instance.SetValue(station.gameObject, EmissionColor, Gold * pulse);

            Vector3 position = station.transform.position;
            Vector3 above = new(position.x, Heights[i], position.z);
            marker.Show(camera, above, Localization.instance.Localize(station.m_name), Levels[i], Vector3.Distance(from, position));
        }
    }

    public static void Stop()
    {
        if (MaterialMan.instance != null)
        {
            foreach (CraftingStation station in Lit)
            {
                if (station != null)
                {
                    MaterialMan.instance.ResetValue(station.gameObject, EmissionColor);
                }
            }
        }

        foreach (StationMarker marker in Markers)
        {
            marker.Hide();
        }

        Lit.Clear();
        Heights.Clear();
        Levels.Clear();
    }

    private static StationMarker MarkerAt(int index)
    {
        while (Markers.Count <= index)
        {
            Markers.Add(new StationMarker());
        }

        return Markers[index];
    }

    /// <summary>"Level 3/5", or just "Level 3" when the highest level is not known.</summary>
    private static string LevelOf(CraftingStation station)
    {
        string level = station.GetLevel().ToString(CultureInfo.InvariantCulture);
        int max = MaxLevelOf(station.m_name);
        return max > 0
            ? Localization.instance.Localize("$benchlink_level_of", level, max.ToString(CultureInfo.InvariantCulture))
            : Localization.instance.Localize("$benchlink_level", level);
    }

    /// <summary>
    /// One more than the number of upgrade types the game has for the station, found from the prefabs once per
    /// station type, so upgrades added by other mods count too. 0 if any of them stack, as there is then no limit.
    /// </summary>
    private static int MaxLevelOf(string name)
    {
        if (MaxLevels.TryGetValue(name, out int max))
        {
            return max;
        }

        ZNetScene scene = ZNetScene.instance;
        if (scene == null)
        {
            return 0;
        }

        max = 1;
        foreach (GameObject prefab in scene.m_prefabs)
        {
            StationExtension extension = prefab != null ? prefab.GetComponent<StationExtension>() : null;
            if (extension == null || extension.m_craftingStation == null || extension.m_craftingStation.m_name != name)
            {
                continue;
            }

            if (extension.m_stack)
            {
                max = 0;
                break;
            }

            max++;
        }

        MaxLevels[name] = max;
        return max;
    }

    /// <summary>Just above the top of the station's model, where its marker floats.</summary>
    private static float HeightOf(CraftingStation station)
    {
        float top = station.transform.position.y + 1f;
        foreach (Renderer renderer in station.GetComponentsInChildren<Renderer>())
        {
            if (renderer.enabled && renderer is MeshRenderer or SkinnedMeshRenderer)
            {
                top = Mathf.Max(top, renderer.bounds.max.y);
            }
        }

        return top + 0.4f;
    }
}
