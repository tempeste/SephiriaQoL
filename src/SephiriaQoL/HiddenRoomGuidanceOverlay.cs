using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SephiriaQoL;

internal sealed class HiddenRoomGuidanceOverlay
{
    private static HiddenRoomGuidanceOverlay _instance;

    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<float> _scale;
    private readonly List<Component> _entrances = new List<Component>();
    private Component _nearest;
    private float _nextRefresh;
    private GUIStyle _arrowStyle;
    private GUIStyle _markerLabelStyle;

    internal HiddenRoomGuidanceOverlay(ConfigEntry<bool> enabled, ConfigEntry<float> scale)
    {
        _enabled = enabled;
        _scale = scale;
        _instance = this;
    }

    internal void Update()
    {
        if (Time.unscaledTime < _nextRefresh)
            return;

        _nextRefresh = Time.unscaledTime + 0.5f;
        _entrances.RemoveAll(target => !IsUndiscovered(target));
        PlayerAvatar observer = GameCamera.Instance?.Observer;
        _nearest = observer == null
            ? null
            : _entrances.OrderBy(target =>
                Vector2.SqrMagnitude((Vector2)target.transform.position - (Vector2)observer.transform.position))
                .FirstOrDefault();
    }

    internal void OnGUI()
    {
        if (_enabled.Value != true || _nearest == null || Camera.main == null)
            return;

        Vector3 screen = Camera.main.WorldToScreenPoint(_nearest.transform.position);
        if (screen.z < 0f)
            return;

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 target = new Vector2(screen.x, Screen.height - screen.y);
        Vector2 direction = (target - center).normalized;
        const float margin = 58f;
        Vector2 marker = new Vector2(
            Mathf.Clamp(target.x, margin, Screen.width - margin),
            Mathf.Clamp(target.y, margin, Screen.height - margin));

        float scale = OverlayGui.ResolveScale(_scale);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        marker /= scale;

        string arrow = DirectionArrow(direction);
        Rect panel = new Rect(marker.x - 55f, marker.y - 22f, 110f, 44f);
        OverlayGui.Fill(panel, new Color(OverlayGui.Panel.r, OverlayGui.Panel.g, OverlayGui.Panel.b, 0.92f));
        OverlayGui.Outline(panel, OverlayGui.Accent);
        _arrowStyle ??= CenterStyle(17);
        _markerLabelStyle ??= CenterStyle(10);
        GUI.Label(new Rect(panel.x, panel.y + 1f, panel.width, 22f), arrow, _arrowStyle);
        GUI.Label(new Rect(panel.x, panel.y + 22f, panel.width, 18f), "HIDDEN ROOM", _markerLabelStyle);
        GUI.matrix = previous;
    }

    internal void Dispose()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Register(Component entrance)
    {
        if (entrance != null && !_entrances.Contains(entrance))
            _entrances.Add(entrance);
    }

    private static bool IsUndiscovered(Component target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;
        if (target is HiddenRoomTriggerCollider collider)
            return collider.hp > 0f;
        if (target is BreakableProp_HiddenPortal portal)
            return !portal.IsBroken && portal.PassageDir == HiddenPortalSide.Entrance;
        return false;
    }

    private static string DirectionArrow(Vector2 direction)
    {
        float angle = Mathf.Atan2(-direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < -157.5f || angle >= 157.5f) return "←";
        if (angle < -112.5f) return "↙";
        if (angle < -67.5f) return "↓";
        if (angle < -22.5f) return "↘";
        if (angle < 22.5f) return "→";
        if (angle < 67.5f) return "↗";
        if (angle < 112.5f) return "↑";
        return "↖";
    }

    private static GUIStyle CenterStyle(int size)
    {
        GUIStyle style = new GUIStyle(OverlayGui.LabelStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = size,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = OverlayGui.Text;
        return style;
    }

    [HarmonyPatch(typeof(HiddenRoomTriggerCollider), nameof(HiddenRoomTriggerCollider.SetPassageDir))]
    private static class DiggingEntrancePatch
    {
        private static void Postfix(HiddenRoomTriggerCollider __instance) => _instance?.Register(__instance);
    }

    [HarmonyPatch(typeof(BreakableProp_HiddenPortal), nameof(BreakableProp_HiddenPortal.Connect),
        new[] { typeof(IHiddenPortal), typeof(Action) })]
    private static class PortalEntrancePatch
    {
        private static void Postfix(BreakableProp_HiddenPortal __instance)
        {
            if (__instance.PassageDir == HiddenPortalSide.Entrance)
                _instance?.Register(__instance);
        }
    }
}
