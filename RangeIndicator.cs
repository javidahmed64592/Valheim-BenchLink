using System.Collections.Generic;

using UnityEngine;

namespace BenchLink;

/// <summary>
/// While building, shows the game's own bench rings for every station in the cluster of each bench
/// the player is in range of, and tints the rings of linked stations Ember Gold. It borrows each station's
/// existing ring rather than creating any, so there is nothing to pool or leak: rings are switched off again
/// when the hammer is put away, the indicator is turned off, or the station unloads.
/// </summary>
internal sealed class RangeIndicator
{
    /// <summary>Runs a little faster than the rings' own 0.5 s hide timer so a ring never blinks off.</summary>
    private const float TickInterval = 0.25f;

    private static readonly Color LinkedTint = new(0.941f, 0.690f, 0.290f);
    private static readonly string[] ColorProperties = ["_Color", "_TintColor", "_BaseColor"];

    private readonly List<CraftingStation> _shown = [];
    private readonly List<CraftingStation> _next = [];
    private readonly HashSet<CraftingStation> _tinted = [];
    private readonly Dictionary<CraftingStation, MarkerRenderers> _cache = [];
    private readonly MaterialPropertyBlock _block = new();

    private int[] _colorIds;
    private bool[] _clusterActive = [];
    private float _timer;
    private int _cacheVersion;

    public void Tick(float deltaTime)
    {
        _timer += deltaTime;
        if (_timer < TickInterval)
        {
            return;
        }

        _timer = 0f;

        Player player = Player.m_localPlayer;
        if (BenchLinkConfig.Indicator.Value == IndicatorMode.Off || player == null || !player.InPlaceMode())
        {
            Hide();
            return;
        }

        if (_cacheVersion != StationClusters.Version)
        {
            _cache.Clear();
            _cacheVersion = StationClusters.Version;
        }

        Update(player.transform.position);
    }

    public void Hide()
    {
        foreach (CraftingStation station in _shown)
        {
            Hide(station);
        }

        _shown.Clear();
        _tinted.Clear();
    }

    private void Update(Vector3 playerPosition)
    {
        StationGraph graph = StationClusters.Graph;
        IReadOnlyList<CraftingStation> stations = StationClusters.Stations;

        if (_clusterActive.Length < graph.ClusterCount)
        {
            _clusterActive = new bool[graph.ClusterCount];
        }

        for (int i = 0; i < graph.ClusterCount; i++)
        {
            _clusterActive[i] = false;
        }

        for (int i = 0; i < graph.Count; i++)
        {
            if (graph.Covers(i, playerPosition.x, playerPosition.z))
            {
                _clusterActive[graph.ClusterOf(i)] = true;
            }
        }

        _next.Clear();
        for (int i = 0; i < graph.Count; i++)
        {
            int cluster = graph.ClusterOf(i);
            if (_clusterActive[cluster] && stations[i].m_areaMarker != null)
            {
                _next.Add(stations[i]);
                Show(stations[i], graph.ClusterSize(cluster) > 1);
            }
        }

        foreach (CraftingStation station in _shown)
        {
            if (!_next.Contains(station))
            {
                Hide(station);
            }
        }

        _shown.Clear();
        _shown.AddRange(_next);
    }

    private void Show(CraftingStation station, bool linked)
    {
        station.m_areaMarker.SetActive(true);

        // The hammer menu's own showing of the ring would switch it off half a second later.
        station.CancelInvoke("HideMarker");

        if (linked)
        {
            Tint(station);
        }
        else
        {
            Untint(station);
        }
    }

    private void Hide(CraftingStation station)
    {
        if (station == null)
        {
            return;
        }

        Untint(station);
        if (station.m_areaMarker != null)
        {
            station.m_areaMarker.SetActive(false);
        }
    }

    private void Tint(CraftingStation station)
    {
        foreach (Renderer renderer in RenderersOf(station))
        {
            int id = ColorId(renderer);
            if (id < 0)
            {
                continue;
            }

            // Keep the ring's own transparency; only the hue changes.
            float alpha = renderer.sharedMaterial.GetColor(id).a;
            _block.Clear();
            _block.SetColor(id, new Color(LinkedTint.r, LinkedTint.g, LinkedTint.b, alpha));
            renderer.SetPropertyBlock(_block);
        }

        _ = _tinted.Add(station);
    }

    private void Untint(CraftingStation station)
    {
        if (!_tinted.Remove(station))
        {
            return;
        }

        foreach (Renderer renderer in RenderersOf(station))
        {
            renderer.SetPropertyBlock(null);
        }
    }

    private int ColorId(Renderer renderer)
    {
        Material material = renderer.sharedMaterial;
        if (material == null)
        {
            return -1;
        }

        if (_colorIds == null)
        {
            _colorIds = new int[ColorProperties.Length];
            for (int i = 0; i < ColorProperties.Length; i++)
            {
                _colorIds[i] = Shader.PropertyToID(ColorProperties[i]);
            }
        }

        foreach (int id in _colorIds)
        {
            if (material.HasProperty(id))
            {
                return id;
            }
        }

        return -1;
    }

    /// <summary>The ring's segments are created lazily by the game, so the cache refreshes when their number changes.</summary>
    private Renderer[] RenderersOf(CraftingStation station)
    {
        Transform marker = station.m_areaMarker.transform;
        if (!_cache.TryGetValue(station, out MarkerRenderers cached) || cached.ChildCount != marker.childCount)
        {
            cached = new MarkerRenderers(marker.GetComponentsInChildren<Renderer>(true), marker.childCount);
            _cache[station] = cached;
        }

        return cached.Items;
    }

    private readonly struct MarkerRenderers(Renderer[] items, int childCount)
    {
        public Renderer[] Items { get; } = items;
        public int ChildCount { get; } = childCount;
    }
}
