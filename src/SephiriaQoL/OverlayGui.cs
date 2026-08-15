using BepInEx.Configuration;
using UnityEngine;

namespace SephiriaQoL;

internal static class OverlayGui
{
    internal const float MinScale = 0.75f;
    internal const float MaxScale = 2f;

    internal static readonly Color Panel = new Color(0.115f, 0.068f, 0.052f, 0.97f);
    internal static readonly Color PanelRaised = new Color(0.29f, 0.17f, 0.12f, 0.99f);
    internal static readonly Color Header = new Color(0.34f, 0.18f, 0.11f, 1f);
    internal static readonly Color Track = new Color(0.055f, 0.032f, 0.027f, 0.94f);
    internal static readonly Color Border = new Color(0.36f, 0.2f, 0.13f, 1f);
    internal static readonly Color BorderLight = new Color(0.66f, 0.44f, 0.25f, 1f);
    internal static readonly Color FrameShadow = new Color(0.045f, 0.024f, 0.018f, 1f);
    internal static readonly Color Text = new Color(0.97f, 0.9f, 0.76f, 1f);
    internal static readonly Color MutedText = new Color(0.84f, 0.71f, 0.57f, 1f);
    internal static readonly Color Accent = new Color(0.91f, 0.67f, 0.28f, 1f);
    internal static readonly Color Success = new Color(0.57f, 0.82f, 0.43f, 1f);
    internal static readonly Color Danger = new Color(0.82f, 0.33f, 0.38f, 1f);
    private static readonly Color WoodGrainLight = new Color(0.82f, 0.53f, 0.29f, 0.2f);
    private static readonly Color WoodGrainDark = new Color(0.08f, 0.035f, 0.02f, 0.3f);
    private static readonly Color Peg = new Color(0.72f, 0.56f, 0.35f, 1f);

    internal static readonly Color[] PlayerColors =
    {
        new Color(0.93f, 0.43f, 0.72f, 1f),
        new Color(0.31f, 0.75f, 0.86f, 1f),
        new Color(0.65f, 0.82f, 0.33f, 1f),
        new Color(0.37f, 0.62f, 0.91f, 1f),
        new Color(0.89f, 0.61f, 0.28f, 1f),
        new Color(0.69f, 0.51f, 0.84f, 1f),
        new Color(0.86f, 0.38f, 0.4f, 1f),
        new Color(0.43f, 0.76f, 0.53f, 1f)
    };

    private static Texture2D _windowTexture;
    private static Texture2D _buttonTexture;
    private static Texture2D _buttonHoverTexture;
    private static Texture2D _buttonActiveTexture;
    private static Texture2D _selectedButtonTexture;
    private static Texture2D _selectedButtonHoverTexture;
    private static Texture2D _dangerButtonTexture;
    private static Texture2D _textFieldTexture;
    private static GUIStyle _windowStyle;
    private static GUIStyle _titleStyle;
    private static GUIStyle _labelStyle;
    private static GUIStyle _mutedStyle;
    private static GUIStyle _rightStyle;
    private static GUIStyle _smallRightStyle;
    private static GUIStyle _buttonStyle;
    private static GUIStyle _selectedButtonStyle;
    private static GUIStyle _dangerButtonStyle;
    private static GUIStyle _toggleStyle;
    private static GUIStyle _textFieldStyle;
    private static GUIStyle _scaleButtonStyle;

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

    internal static GUIStyle SelectedButtonStyle
    {
        get
        {
            EnsureStyles();
            return _selectedButtonStyle;
        }
    }

    internal static GUIStyle DangerButtonStyle
    {
        get
        {
            EnsureStyles();
            return _dangerButtonStyle;
        }
    }

    internal static GUIStyle ToggleStyle
    {
        get
        {
            EnsureStyles();
            return _toggleStyle;
        }
    }

    internal static GUIStyle TextFieldStyle
    {
        get
        {
            EnsureStyles();
            return _textFieldStyle;
        }
    }

    private static GUIStyle ScaleButtonStyle
    {
        get
        {
            EnsureStyles();
            return _scaleButtonStyle;
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
        if (GUI.Button(new Rect(x, y, 22f, 22f), "−", ScaleButtonStyle))
            AdjustScale(scaleEntry, -0.1f);

        float scale = ResolveScale(scaleEntry);
        string scaleLabel = scaleEntry != null && scaleEntry.Value <= 0f
            ? $"Auto {scale * 100f:0}%"
            : $"{scale * 100f:0}%";
        if (GUI.Button(new Rect(x + 25f, y, 76f, 22f), scaleLabel, ScaleButtonStyle))
            ResetScale(scaleEntry);

        if (GUI.Button(new Rect(x + 104f, y, 22f, 22f), "+", ScaleButtonStyle))
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

    internal static void DrawHeader(Rect rect)
    {
        Fill(rect, FrameShadow);
        Fill(Inset(rect, 2f), Border);
        Fill(Inset(rect, 4f), Header);
        DrawWoodGrain(Inset(rect, 4f), true);
        DrawPeg(new Rect(rect.x + 7f, rect.y + 7f, 5f, 5f));
        DrawPeg(new Rect(rect.xMax - 12f, rect.y + 7f, 5f, 5f));
    }

    internal static void DrawPanel(Rect rect)
    {
        DrawPanel(rect, PanelRaised, Border);
    }

    internal static void DrawPanel(Rect rect, Color fill, Color outline)
    {
        Fill(rect, FrameShadow);
        Fill(Inset(rect, 2f), outline);
        Fill(Inset(rect, 3f), fill);
        DrawWoodGrain(Inset(rect, 3f), false);
        Fill(new Rect(rect.x + 5f, rect.y + 3f, Mathf.Max(0f, rect.width * 0.32f), 1f),
            new Color(BorderLight.r, BorderLight.g, BorderLight.b, 0.42f));
        Fill(new Rect(rect.xMax - 5f, rect.yMax - 5f, 2f, 2f), FrameShadow);
    }

    internal static void DrawInset(Rect rect)
    {
        Fill(rect, Border);
        Fill(Inset(rect, 1f), Track);
    }

    internal static void DrawPip(Rect rect, Color color)
    {
        Fill(rect, FrameShadow);
        Fill(Inset(rect, 1f), color);
        Fill(new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(1f, rect.width - 4f), 1f),
            new Color(1f, 1f, 1f, 0.38f));
    }

    private static void DrawWoodGrain(Rect rect, bool strong)
    {
        if (rect.width < 28f || rect.height < 10f)
            return;

        Color light = strong
            ? new Color(WoodGrainLight.r, WoodGrainLight.g, WoodGrainLight.b, 0.3f)
            : WoodGrainLight;
        Fill(new Rect(rect.x + 8f, rect.y + 6f, rect.width * 0.22f, 1f), light);
        Fill(new Rect(rect.x + rect.width * 0.38f, rect.y + 11f, rect.width * 0.18f, 1f), WoodGrainDark);
        Fill(new Rect(rect.x + rect.width * 0.67f, rect.y + 5f, rect.width * 0.21f, 1f), WoodGrainDark);
        if (rect.height >= 28f)
            Fill(new Rect(rect.x + rect.width * 0.17f, rect.yMax - 8f, rect.width * 0.27f, 1f), WoodGrainDark);
    }

    private static void DrawPeg(Rect rect)
    {
        Fill(rect, FrameShadow);
        Fill(Inset(rect, 1f), Peg);
        Fill(new Rect(rect.x + 1f, rect.y + 1f, 1f, 1f), Text);
    }

    internal static Rect Inset(Rect rect, float amount)
    {
        return new Rect(
            rect.x + amount,
            rect.y + amount,
            Mathf.Max(0f, rect.width - amount * 2f),
            Mathf.Max(0f, rect.height - amount * 2f));
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

        _windowTexture = CreateFrameTexture(Panel, Border, BorderLight, true);
        _buttonTexture = CreateFrameTexture(new Color(0.35f, 0.2f, 0.13f, 1f), Border, BorderLight, true);
        _buttonHoverTexture = CreateFrameTexture(new Color(0.43f, 0.25f, 0.15f, 1f), BorderLight, Text, true);
        _buttonActiveTexture = CreateFrameTexture(new Color(0.25f, 0.13f, 0.085f, 1f), Border, BorderLight, true);
        _selectedButtonTexture = CreateFrameTexture(new Color(0.44f, 0.26f, 0.14f, 1f), Accent, BorderLight, true);
        _selectedButtonHoverTexture = CreateFrameTexture(new Color(0.52f, 0.31f, 0.16f, 1f), Accent, Text, true);
        _dangerButtonTexture = CreateFrameTexture(new Color(0.43f, 0.16f, 0.12f, 1f), Danger, BorderLight, true);
        _textFieldTexture = CreateFrameTexture(Track, Border, BorderLight, false);

        _windowStyle = new GUIStyle(GUI.skin.window);
        _windowStyle.normal.background = _windowTexture;
        _windowStyle.onNormal.background = _windowTexture;
        _windowStyle.border = new RectOffset(4, 4, 4, 4);
        _windowStyle.padding = new RectOffset(0, 0, 0, 0);

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 15,
            fontStyle = FontStyle.Normal
        };
        _titleStyle.normal.textColor = Text;

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
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
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(6, 6, 3, 3),
            border = new RectOffset(3, 3, 3, 3)
        };
        _buttonStyle.normal.background = _buttonTexture;
        _buttonStyle.hover.background = _buttonHoverTexture;
        _buttonStyle.active.background = _buttonActiveTexture;
        _buttonStyle.focused.background = _buttonHoverTexture;
        _buttonStyle.normal.textColor = Text;
        _buttonStyle.hover.textColor = Text;
        _buttonStyle.active.textColor = Text;
        _buttonStyle.focused.textColor = Text;

        _scaleButtonStyle = new GUIStyle(_buttonStyle)
        {
            fontSize = 10,
            padding = new RectOffset(2, 2, 2, 2)
        };

        _selectedButtonStyle = new GUIStyle(_buttonStyle);
        _selectedButtonStyle.normal.background = _selectedButtonTexture;
        _selectedButtonStyle.hover.background = _selectedButtonHoverTexture;
        _selectedButtonStyle.focused.background = _selectedButtonHoverTexture;
        _selectedButtonStyle.normal.textColor = new Color(1f, 0.92f, 0.69f, 1f);

        _dangerButtonStyle = new GUIStyle(_buttonStyle);
        _dangerButtonStyle.normal.background = _dangerButtonTexture;
        _dangerButtonStyle.hover.background = _dangerButtonTexture;

        _toggleStyle = new GUIStyle(_buttonStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 6, 3, 3)
        };
        _toggleStyle.onNormal.background = _selectedButtonTexture;
        _toggleStyle.onHover.background = _selectedButtonHoverTexture;
        _toggleStyle.onActive.background = _selectedButtonTexture;
        _toggleStyle.onFocused.background = _selectedButtonHoverTexture;
        _toggleStyle.onNormal.textColor = new Color(1f, 0.92f, 0.69f, 1f);
        _toggleStyle.onHover.textColor = Text;

        _textFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(7, 7, 4, 4),
            border = new RectOffset(3, 3, 3, 3)
        };
        _textFieldStyle.normal.background = _textFieldTexture;
        _textFieldStyle.focused.background = _textFieldTexture;
        _textFieldStyle.hover.background = _textFieldTexture;
        _textFieldStyle.active.background = _textFieldTexture;
        _textFieldStyle.normal.textColor = Text;
        _textFieldStyle.focused.textColor = Text;
        _textFieldStyle.hover.textColor = Text;
        _textFieldStyle.active.textColor = Text;
    }

    private static Texture2D CreateFrameTexture(Color fill, Color border, Color highlight, bool woodGrain)
    {
        const int size = 12;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool outer = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                bool topHighlight = !outer && (x == 1 || y == 1);
                bool lowerShadow = !outer && (x == size - 2 || y == size - 2);
                bool lightGrain = woodGrain && !outer && !topHighlight && !lowerShadow &&
                    y == 4 && x >= 3 && x <= 7;
                bool darkGrain = woodGrain && !outer && !topHighlight && !lowerShadow &&
                    y == 8 && x >= 5 && x <= 9;
                Color color = outer
                    ? FrameShadow
                    : topHighlight
                        ? highlight
                        : lowerShadow
                            ? border
                            : lightGrain
                                ? Color.Lerp(fill, highlight, 0.24f)
                                : darkGrain
                                    ? Color.Lerp(fill, FrameShadow, 0.32f)
                                    : fill;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }
}
