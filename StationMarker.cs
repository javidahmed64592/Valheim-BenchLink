using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace BenchLink;

/// <summary>
/// A gold arrow and label on the HUD that sit over a station wherever it is, walls or not. When the station is off
/// screen (or behind the player) the marker clings to the edge of the screen and points the way to turn. It hides
/// along with the rest of the HUD.
/// </summary>
internal sealed class StationMarker
{
    /// <summary>How far in from the screen's edge the marker stops, in HUD units.</summary>
    private const float EdgeMargin = 70f;

    private GameObject _root;
    private RectTransform _arrow;
    private Image _arrowImage;
    private RectTransform _label;
    private TextMeshProUGUI _text;
    private Canvas _canvas;

    public void Show(Camera camera, Vector3 above, string name, float distance)
    {
        if (!EnsureCreated())
        {
            return;
        }

        Vector3 screen = camera.WorldToScreenPointScaled(above);
        float width = Screen.width;
        float height = Screen.height;
        float margin = EdgeMargin * (_canvas != null ? _canvas.scaleFactor : 1f);
        bool behind = screen.z < 0f;
        bool onScreen = !behind && screen.x >= margin && screen.x <= width - margin && screen.y >= margin && screen.y <= height - margin;

        _root.SetActive(true);
        Vector2 position = screen;
        float bob = Mathf.Sin(Time.time * 3f) * 4f;

        if (onScreen)
        {
            _arrow.localEulerAngles = Vector3.zero;
            _arrow.localPosition = new Vector3(0f, bob, 0f);
            _label.localPosition = new Vector3(0f, 34f, 0f);
        }
        else
        {
            // Behind the camera the projection is mirrored through the centre of the screen.
            Vector2 centre = new(width * 0.5f, height * 0.5f);
            Vector2 direction = (Vector2)screen - centre;
            if (behind)
            {
                direction = -direction;
            }

            if (direction.sqrMagnitude < 1f)
            {
                direction = Vector2.down;
            }

            float scale = Mathf.Min(
                Mathf.Abs(direction.x) > 0.01f ? (centre.x - margin) / Mathf.Abs(direction.x) : float.MaxValue,
                Mathf.Abs(direction.y) > 0.01f ? (centre.y - margin) / Mathf.Abs(direction.y) : float.MaxValue);
            position = centre + (direction * scale);

            // The arrow image points down (-y); turn it to point along the direction, nudged outward as it bobs.
            Vector2 unit = direction.normalized;
            float angle = (Mathf.Atan2(unit.y, unit.x) * Mathf.Rad2Deg) + 90f;
            _arrow.localEulerAngles = new Vector3(0f, 0f, angle);
            _arrow.localPosition = unit * bob;
            _label.localPosition = -unit * 46f;
        }

        _root.transform.position = position;
        _text.text = "<color=#f0b04a>" + name + "</color>\n" + Mathf.RoundToInt(distance) + " m";

        float pulse = Mathf.Lerp(0.75f, 1f, (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f);
        Color gold = ClusterHighlight.Gold;
        _arrowImage.color = new Color(gold.r, gold.g, gold.b, pulse);
    }

    public void Hide()
    {
        if (_root != null)
        {
            _root.SetActive(false);
        }
    }

    /// <summary>The HUD is rebuilt every time a world is entered, taking the marker with it; this builds a new one when needed.</summary>
    private bool EnsureCreated()
    {
        if (_root != null)
        {
            return true;
        }

        // Under the same root as the game's creature name tags, which are placed with the same screen projection
        // and hidden along with the rest of the HUD.
        Hud hud = Hud.instance;
        EnemyHud enemyHud = EnemyHud.instance;
        if (hud == null || enemyHud == null || enemyHud.m_hudRoot == null)
        {
            return false;
        }

        _root = new GameObject("BenchLink_StationMarker", typeof(RectTransform));
        _root.transform.SetParent(enemyHud.m_hudRoot.transform, worldPositionStays: false);
        _canvas = _root.GetComponentInParent<Canvas>();

        GameObject arrow = new("Arrow", typeof(RectTransform), typeof(Image));
        _arrow = arrow.GetComponent<RectTransform>();
        _arrow.SetParent(_root.transform, worldPositionStays: false);
        _arrow.sizeDelta = new Vector2(34f, 34f);
        _arrowImage = arrow.GetComponent<Image>();
        _arrowImage.sprite = MarkerArrow.Sprite;
        _arrowImage.raycastTarget = false;

        GameObject label = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        _label = label.GetComponent<RectTransform>();
        _label.SetParent(_root.transform, worldPositionStays: false);
        _label.sizeDelta = new Vector2(260f, 60f);
        _text = label.GetComponent<TextMeshProUGUI>();

        // The same font and outlined material as the crosshair's hover text.
        TextMeshProUGUI hover = hud.m_hoverName;
        if (hover != null)
        {
            _text.font = hover.font;
            _text.fontSharedMaterial = hover.fontSharedMaterial;
        }

        _text.fontSize = 18f;
        _text.alignment = TextAlignmentOptions.Center;
        _text.textWrappingMode = TextWrappingModes.NoWrap;
        _text.raycastTarget = false;
        _text.richText = true;

        _root.SetActive(false);
        return true;
    }
}

/// <summary>The marker's arrow, drawn pixel by pixel when first needed so the mod ships no image files.</summary>
internal static class MarkerArrow
{
    /// <summary>A downward-pointing triangle, white with a dark rim; the marker colours it.</summary>
    public static Sprite Sprite
    {
        get
        {
            // Compared with Unity's == null rather than ??, as Unity objects can be destroyed behind our back.
            if (field == null)
            {
                field = Make();
            }

            return field;
        }
    }

    private static Sprite Make()
    {
        const int size = 64;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, mipChain: false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,

            // Kept for the whole session: the game unloads unreferenced assets on every scene change.
            hideFlags = HideFlags.DontUnloadUnusedAsset,
        };
        Color[] pixels = new Color[size * size];

        // In [-1, 1] on both axes, y up: the flat top edge at y = 0.55, the tip at y = -0.75.
        const float top = 0.55f;
        const float tip = -0.75f;
        const float halfWidth = 0.8f;
        const float slope = halfWidth / (top - tip);
        const float rim = 0.07f;
        float edgeScale = 1f / Mathf.Sqrt(1f + (slope * slope));

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = ((x + 0.5f) / size * 2f) - 1f;
                float v = ((y + 0.5f) / size * 2f) - 1f;

                // Distance inside the triangle (negative outside), the nearer of the top edge and the slanted sides.
                float inside = Mathf.Min(top - v, ((slope * (v - tip)) - Mathf.Abs(u)) * edgeScale);

                // White fill fading through a dark rim to transparent.
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-rim, -rim * 0.3f, inside));
                float light = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, rim * 0.6f, inside));
                float shade = Mathf.Lerp(0.12f, 1f, light);
                pixels[(y * size) + x] = new Color(shade, shade, shade, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }
}
