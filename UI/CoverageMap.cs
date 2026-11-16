using System.Collections.Generic;

using BenchLink.Clusters;

using UnityEngine;
using UnityEngine.UI;

namespace BenchLink.UI;

/// <summary>
/// Shades the area each loaded cluster covers on the large map: Ember Gold for linked clusters, white for a station
/// on its own (or a duplicate), with a brighter edge. Each cluster is one small texture laid over the map image, under
/// the pins, rebuilt only when the stations change. Only loaded stations are known, so only clusters near the player
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

    private static RectTransform _root;
    private static int _signature;
    private static float _checkedAt = float.MinValue;

    public static void Tick()
    {
        Minimap map = Minimap.instance;
        if (!BenchLinkConfig.MapCoverage.Value || map == null || map.m_mode != Minimap.MapMode.Large
            || Player.m_localPlayer == null)
        {
            Hide();
            return;
        }

        if (_root == null)
        {
            Create(map.m_mapImageLarge.rectTransform);
        }

        bool opening = !_root.gameObject.activeSelf;
        if (opening || Time.time - _checkedAt >= RefreshInterval || Time.time < _checkedAt)
        {
            _checkedAt = Time.time;
            Refresh(opening);
        }

        _root.gameObject.SetActive(true);
        Position(map);
    }

    public static void Hide()
    {
        if (_root != null)
        {
            _root.gameObject.SetActive(false);
        }
    }

    public static void Destroy()
    {
        Clear();
        if (_root != null)
        {
            Object.Destroy(_root.gameObject);
        }

        _root = null;
    }

    private static void Create(RectTransform mapImage)
    {
        // The map was rebuilt (a new world), taking the old overlays with it; only their textures are left.
        Clear();

        GameObject root = new("BenchLinkCoverage", typeof(RectTransform), typeof(RectMask2D));
        root.SetActive(false);
        _root = (RectTransform)root.transform;
        _root.SetParent(mapImage, false);
        _root.SetAsFirstSibling();
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = Vector2.zero;
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

        GameObject image = new("Cluster" + cluster, typeof(RectTransform), typeof(RawImage));
        RectTransform rect = (RectTransform)image.transform;
        rect.SetParent(_root, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        RawImage raw = image.GetComponent<RawImage>();
        raw.texture = texture;
        raw.raycastTarget = false;

        float worldWidth = width * cellSize;
        float worldHeight = height * cellSize;
        return new Overlay(rect, texture, minX + (worldWidth / 2), minZ + (worldHeight / 2), worldWidth, worldHeight);
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
    private static void Position(Minimap map)
    {
        RawImage image = map.m_mapImageLarge;
        Rect uv = image.uvRect;
        Rect rect = image.rectTransform.rect;
        float worldToGui = rect.width / (map.m_pixelSize * map.m_textureSize * uv.width);
        float worldToGuiY = rect.height / (map.m_pixelSize * map.m_textureSize * uv.height);
        foreach (Overlay overlay in Overlays)
        {
            map.WorldToMapPoint(new Vector3(overlay.CenterX, 0f, overlay.CenterZ), out float mx, out float my);
            overlay.Rect.anchoredPosition = new Vector2(
                (mx - uv.xMin) / uv.width * rect.width,
                (my - uv.yMin) / uv.height * rect.height);
            overlay.Rect.sizeDelta = new Vector2(overlay.Width * worldToGui, overlay.Height * worldToGuiY);
        }
    }

    private static void Clear()
    {
        foreach (Overlay overlay in Overlays)
        {
            Object.Destroy(overlay.Texture);
            if (overlay.Rect != null)
            {
                Object.Destroy(overlay.Rect.gameObject);
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

    private sealed class Overlay(RectTransform rect, Texture2D texture, float centerX, float centerZ, float width, float height)
    {
        public RectTransform Rect { get; } = rect;
        public Texture2D Texture { get; } = texture;
        public float CenterX { get; } = centerX;
        public float CenterZ { get; } = centerZ;
        public float Width { get; } = width;
        public float Height { get; } = height;
    }
}
