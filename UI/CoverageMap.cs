using System.Collections.Generic;

using BenchLink.Clusters;

using UnityEngine;
using UnityEngine.UI;

namespace BenchLink.UI;

/// <summary>
/// Shades the area each loaded cluster covers on the minimap and the large map: Ember Gold for linked clusters, white for a station
/// on its own (or a duplicate), with a brighter edge. Each cluster is one small texture shared by both maps and laid over
/// each map image, under the pins, rebuilt only when the stations change. Only loaded stations are known, so only clusters near the player
/// show.
/// </summary>
internal static class CoverageMap
{
    private const float RefreshInterval = 1f;
    private const float CellSize = 0.5f;
    private const int MaxTextureSize = 512;
    private const int EdgeCells = 2;
    private const byte FillAlpha = 60;
    private const byte EdgeAlpha = 220;

    private static readonly List<Overlay> Overlays = [];

    private static RectTransform _largeRoot;
    private static RectTransform _smallRoot;
    private static bool _visible;
    private static int _signature;
    private static float _checkedAt = float.MinValue;

    public static void Tick()
    {
        Minimap map = Minimap.instance;
        if (!BenchLinkConfig.MapCoverage.Value || map == null || map.m_mode == Minimap.MapMode.None
            || Player.m_localPlayer == null)
        {
            Hide();
            return;
        }

        bool force = !_visible;
        if (_largeRoot == null || _smallRoot == null)
        {
            // The map was rebuilt (a new world), taking the old overlays with it; only their textures are left.
            Clear();
            DestroyRoots();
            _largeRoot = CreateRoot(map.m_mapImageLarge.rectTransform);
            _smallRoot = CreateRoot(map.m_mapImageSmall.rectTransform);
            force = true;
        }

        if (force || Time.time - _checkedAt >= RefreshInterval || Time.time < _checkedAt)
        {
            _checkedAt = Time.time;
            Refresh(force);
        }

        bool large = map.m_mode == Minimap.MapMode.Large;
        _largeRoot.gameObject.SetActive(large);
        _smallRoot.gameObject.SetActive(!large);
        _visible = true;
        Position(map, large);
    }

    public static void Hide()
    {
        if (_largeRoot != null)
        {
            _largeRoot.gameObject.SetActive(false);
        }

        if (_smallRoot != null)
        {
            _smallRoot.gameObject.SetActive(false);
        }

        _visible = false;
    }

    public static void Destroy()
    {
        Clear();
        DestroyRoots();
        _visible = false;
    }

    private static void DestroyRoots()
    {
        if (_largeRoot != null)
        {
            Object.Destroy(_largeRoot.gameObject);
        }

        if (_smallRoot != null)
        {
            Object.Destroy(_smallRoot.gameObject);
        }

        _largeRoot = null;
        _smallRoot = null;
    }

    /// <summary>A layer filling the map image and clipped to it, drawn under the image's other children.</summary>
    private static RectTransform CreateRoot(RectTransform mapImage)
    {
        GameObject root = new("BenchLinkCoverage", typeof(RectTransform), typeof(RectMask2D));
        root.SetActive(false);
        RectTransform rect = (RectTransform)root.transform;
        rect.SetParent(mapImage, false);
        rect.SetAsFirstSibling();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void Refresh(bool force)
    {
        StationGraph graph = StationClusters.Graph;
        int signature = SignatureOf(graph);
        if (!force && signature == _signature)
        {
            return;
        }

        _signature = signature;
        Clear();
        for (int cluster = 0; cluster < graph.ClusterCount; cluster++)
        {
            Overlays.Add(Build(graph, cluster));
        }
    }

    private static Overlay Build(StationGraph graph, int cluster)
    {
        (float minX, float minZ, float maxX, float maxZ) = graph.ClusterBounds(cluster);
        float cellSize = Mathf.Max(CellSize, Mathf.Max(maxX - minX, maxZ - minZ) / (MaxTextureSize - 2));

        // One empty cell all round, so the edge fades out cleanly when the texture is scaled.
        minX -= cellSize;
        minZ -= cellSize;
        int width = Mathf.CeilToInt((maxX - minX) / cellSize) + 1;
        int height = Mathf.CeilToInt((maxZ - minZ) / cellSize) + 1;

        bool[] mask = graph.CoverageMask(cluster, minX, minZ, cellSize, width, height);
        Color32 color = graph.ClusterSize(cluster) > 1 ? (Color32)ClusterHighlight.Gold : new Color32(255, 255, 255, 255);
        Color32[] pixels = new Color32[mask.Length];
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                int at = (row * width) + column;
                if (mask[at])
                {
                    color.a = IsEdge(mask, width, height, column, row) ? EdgeAlpha : FillAlpha;
                    pixels[at] = color;
                }
            }
        }

        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        {
            name = "BenchLinkCoverage",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        float worldWidth = width * cellSize;
        float worldHeight = height * cellSize;
        return new Overlay(
            CreateImage(_largeRoot, texture, cluster),
            CreateImage(_smallRoot, texture, cluster),
            texture,
            minX + (worldWidth / 2),
            minZ + (worldHeight / 2),
            worldWidth,
            worldHeight);
    }

    private static RectTransform CreateImage(RectTransform root, Texture2D texture, int cluster)
    {
        GameObject image = new("Cluster" + cluster, typeof(RectTransform), typeof(RawImage));
        RectTransform rect = (RectTransform)image.transform;
        rect.SetParent(root, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        RawImage raw = image.GetComponent<RawImage>();
        raw.texture = texture;
        raw.raycastTarget = false;
        return rect;
    }

    /// <summary>A covered cell is on the edge if an uncovered cell, or the end of the grid, is within EdgeCells of it.</summary>
    private static bool IsEdge(bool[] mask, int width, int height, int column, int row)
    {
        for (int dz = -EdgeCells; dz <= EdgeCells; dz++)
        {
            for (int dx = -EdgeCells; dx <= EdgeCells; dx++)
            {
                if ((dx * dx) + (dz * dz) > EdgeCells * EdgeCells)
                {
                    continue;
                }

                int x = column + dx;
                int z = row + dz;
                if (x < 0 || z < 0 || x >= width || z >= height || !mask[(z * width) + x])
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Lays each overlay over the map the same way the game places area pins, so they pan and zoom with it.</summary>
    private static void Position(Minimap map, bool large)
    {
        RawImage image = large ? map.m_mapImageLarge : map.m_mapImageSmall;
        Rect uv = image.uvRect;
        Rect rect = image.rectTransform.rect;
        float worldToGui = rect.width / (map.m_pixelSize * map.m_textureSize * uv.width);
        float worldToGuiY = rect.height / (map.m_pixelSize * map.m_textureSize * uv.height);
        foreach (Overlay overlay in Overlays)
        {
            map.WorldToMapPoint(new Vector3(overlay.CenterX, 0f, overlay.CenterZ), out float mx, out float my);
            RectTransform target = large ? overlay.Large : overlay.Small;
            target.anchoredPosition = new Vector2(
                (mx - uv.xMin) / uv.width * rect.width,
                (my - uv.yMin) / uv.height * rect.height);
            target.sizeDelta = new Vector2(overlay.Width * worldToGui, overlay.Height * worldToGuiY);
        }
    }

    private static void Clear()
    {
        foreach (Overlay overlay in Overlays)
        {
            Object.Destroy(overlay.Texture);
            if (overlay.Large != null)
            {
                Object.Destroy(overlay.Large.gameObject);
            }

            if (overlay.Small != null)
            {
                Object.Destroy(overlay.Small.gameObject);
            }
        }

        Overlays.Clear();
    }

    /// <summary>Changes whenever a station moves, changes range, or changes cluster or duplicate state.</summary>
    private static int SignatureOf(StationGraph graph)
    {
        unchecked
        {
            int hash = graph.Count;
            for (int i = 0; i < graph.Count; i++)
            {
                StationNode node = graph[i];
                hash = (hash * 31) + Mathf.RoundToInt(node.X * 10f);
                hash = (hash * 31) + Mathf.RoundToInt(node.Z * 10f);
                hash = (hash * 31) + Mathf.RoundToInt(node.Range * 10f);
                hash = (hash * 31) + graph.ClusterOf(i);
                hash = (hash * 31) + (graph.IsDuplicate(i) ? 1 : 0);
            }

            return hash;
        }
    }

    private sealed class Overlay(
        RectTransform large,
        RectTransform small,
        Texture2D texture,
        float centerX,
        float centerZ,
        float width,
        float height)
    {
        public RectTransform Large { get; } = large;
        public RectTransform Small { get; } = small;
        public Texture2D Texture { get; } = texture;
        public float CenterX { get; } = centerX;
        public float CenterZ { get; } = centerZ;
        public float Width { get; } = width;
        public float Height { get; } = height;
    }
}
