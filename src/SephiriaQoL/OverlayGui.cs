using BepInEx.Configuration;
using UnityEngine;

namespace SephiriaQoL;

internal static class OverlayGui
{
    internal const float MinScale = 0.75f;
    internal const float MaxScale = 2f;

    internal static readonly Color Panel = new Color(0.055f, 0.071f, 0.082f, 0.96f);
    internal static readonly Color PanelRaised = new Color(0.086f, 0.11f, 0.125f, 0.98f);
    internal static readonly Color Track = new Color(0.018f, 0.025f, 0.03f, 0.9f);
    internal static readonly Color Border = new Color(0.22f, 0.29f, 0.32f, 0.9f);
    internal static readonly Color Text = new Color(0.91f, 0.94f, 0.95f, 1f);
    internal static readonly Color MutedText = new Color(0.58f, 0.65f, 0.68f, 1f);
    internal static readonly Color Accent = new Color(0.2f, 0.82f, 0.78f, 1f);
    internal static readonly Color Danger = new Color(0.96f, 0.34f, 0.28f, 1f);

    internal static readonly Color[] PlayerColors =
    {
        new Color(0.18f, 0.78f, 0.91f, 1f),
        new Color(0.98f, 0.68f, 0.22f, 1f),
        new Color(0.93f, 0.36f, 0.32f, 1f),
        new Color(0.38f, 0.82f, 0.47f, 1f),
        new Color(0.67f, 0.45f, 0.94f, 1f),
        new Color(0.27f, 0.55f, 0.96f, 1f),
        new Color(0.94f, 0.46f, 0.72f, 1f),
        new Color(0.63f, 0.76f, 0.24f, 1f)
    };

    private static Texture2D _windowTexture;
    private static GUIStyle _windowStyle;
    private static GUIStyle _titleStyle;
    private static GUIStyle _labelStyle;
    private static GUIStyle _mutedStyle;
    private static GUIStyle _rightStyle;
    private static GUIStyle _smallRightStyle;
    private static GUIStyle _buttonStyle;

    internal static GUIStyle WindowStyle
    {
        get
        {
            EnsureStyles();
            return _windowStyle;
        }
    }

    internal static GUIStyle TitleStyle
    {
        get
        {
            EnsureStyles();
            return _titleStyle;
        }
    }

    internal static GUIStyle LabelStyle
    {
        get
        {
            EnsureStyles();
            return _labelStyle;
        }
    }

    internal static GUIStyle MutedStyle
    {
        get
        {
            EnsureStyles();
            return _mutedStyle;
        }
    }

    internal static GUIStyle RightStyle
    {
        get
        {
            EnsureStyles();
            return _rightStyle;
        }
    }

    internal static GUIStyle SmallRightStyle
    {
        get
        {
            EnsureStyles();
            return _smallRightStyle;
        }
    }

    internal static GUIStyle ButtonStyle
    {
        get
        {
            EnsureStyles();
            return _buttonStyle;
        }
    }

    internal static float ResolveScale(ConfigEntry<float> configuredScale)
    {
        if (configuredScale != null && configuredScale.Value > 0f)
            return Mathf.Clamp(configuredScale.Value, MinScale, MaxScale);

        if (Application.platform != RuntimePlatform.OSXPlayer || Screen.height < 1600)
            return 1f;

        float retinaScale = Mathf.Clamp(Screen.height / 1350f, 1f, 1.6f);
        return Mathf.Round(retinaScale * 10f) / 10f;
    }

    internal static void AdjustScale(ConfigEntry<float> configuredScale, float delta)
    {
        if (configuredScale == null)
            return;

        float next = ResolveScale(configuredScale) + delta;
        configuredScale.Value = Mathf.Round(Mathf.Clamp(next, MinScale, MaxScale) * 10f) / 10f;
    }

    internal static void ResetScale(ConfigEntry<float> configuredScale)
    {
        if (configuredScale != null)
            configuredScale.Value = 0f;
    }

    internal static Rect BeginScaledWindow(
        int id,
        Rect screenRect,
        float logicalWidth,
        float logicalHeight,
        float scale,
        GUI.WindowFunction drawWindow,
        out Matrix4x4 previousMatrix)
    {
        float physicalWidth = logicalWidth * scale;
        float physicalHeight = logicalHeight * scale;
        screenRect.x = Mathf.Clamp(screenRect.x, 0f, Mathf.Max(0f, Screen.width - physicalWidth));
        screenRect.y = Mathf.Clamp(screenRect.y, 0f, Mathf.Max(0f, Screen.height - 32f * scale));

        previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        Rect logicalRect = new Rect(screenRect.x / scale, screenRect.y / scale, logicalWidth, logicalHeight);
        logicalRect = GUI.Window(id, logicalRect, drawWindow, GUIContent.none, WindowStyle);
        GUI.matrix = previousMatrix;

        return new Rect(logicalRect.x * scale, logicalRect.y * scale, physicalWidth, physicalHeight);
    }

    internal static void DrawScaleControls(ConfigEntry<float> scaleEntry, float x, float y)
    {
        if (GUI.Button(new Rect(x, y, 24f, 22f), "−", ButtonStyle))
            AdjustScale(scaleEntry, -0.1f);

        float scale = ResolveScale(scaleEntry);
        string scaleLabel = scaleEntry != null && scaleEntry.Value <= 0f
            ? $"AUTO {scale * 100f:0}%"
            : $"{scale * 100f:0}%";
        if (GUI.Button(new Rect(x + 27f, y, 72f, 22f), scaleLabel, ButtonStyle))
            ResetScale(scaleEntry);

        if (GUI.Button(new Rect(x + 102f, y, 24f, 22f), "+", ButtonStyle))
            AdjustScale(scaleEntry, 0.1f);
    }

    internal static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    internal static void Outline(Rect rect, Color color, float thickness = 1f)
    {
        Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
        Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    internal static Color ElementColor(EDamageElementalType elementalType)
    {
        switch (elementalType)
        {
            case EDamageElementalType.Fire:
            case EDamageElementalType.FireAndIce:
            case EDamageElementalType.FireAndLightning:
                return new Color(0.96f, 0.32f, 0.2f, 1f);
            case EDamageElementalType.Ice:
            case EDamageElementalType.IceAndLightning:
                return new Color(0.25f, 0.72f, 0.96f, 1f);
            case EDamageElementalType.Lightning:
                return new Color(0.97f, 0.78f, 0.18f, 1f);
            case EDamageElementalType.Chaos:
                return new Color(0.72f, 0.33f, 0.92f, 1f);
            case EDamageElementalType.Physical:
                return new Color(0.83f, 0.84f, 0.8f, 1f);
            default:
                return new Color(0.55f, 0.62f, 0.65f, 1f);
        }
    }

    private static void EnsureStyles()
    {
        if (_windowStyle != null)
            return;

        _windowTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        _windowTexture.SetPixel(0, 0, Panel);
        _windowTexture.Apply();

        _windowStyle = new GUIStyle(GUI.skin.window);
        _windowStyle.normal.background = _windowTexture;
        _windowStyle.onNormal.background = _windowTexture;
        _windowStyle.border = new RectOffset(1, 1, 1, 1);
        _windowStyle.padding = new RectOffset(0, 0, 0, 0);

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        _titleStyle.normal.textColor = Text;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft
        };
        _labelStyle.normal.textColor = Text;

        _mutedStyle = new GUIStyle(_labelStyle);
        _mutedStyle.normal.textColor = MutedText;
        _mutedStyle.fontSize = 11;

        _rightStyle = new GUIStyle(_labelStyle)
        {
            alignment = TextAnchor.MiddleRight,
            fontStyle = FontStyle.Bold
        };

        _smallRightStyle = new GUIStyle(_mutedStyle)
        {
            alignment = TextAnchor.MiddleRight
        };

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(4, 4, 2, 2)
        };
        _buttonStyle.normal.textColor = Text;
        _buttonStyle.hover.textColor = Color.white;
        _buttonStyle.active.textColor = Color.white;
    }
}
