using BepInEx.Configuration;
using HarmonyLib;
using Mirror;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SephiriaQoL;

internal static class JournalSearch
{
    private static readonly FieldInfo IconsField =
        AccessTools.Field(typeof(UI_JournalContent_Item), "icons");

    private static ConfigEntry<bool> _enabled;
    private static string _query = string.Empty;
    private static Rect _windowRect = new Rect(18f, 92f, 300f, 86f);
    private static UI_JournalPanel _journalPanel;
    private static UI_JournalContent_Item _journalItems;
    private static float _nextLookup;

    internal static void Configure(ConfigEntry<bool> enabled) => _enabled = enabled;

    internal static void OnGUI()
    {
        if (_enabled?.Value != true)
            return;

        if (Time.unscaledTime >= _nextLookup)
        {
            _nextLookup = Time.unscaledTime + 0.5f;
            if (_journalPanel == null)
                _journalPanel = UnityEngine.Object.FindFirstObjectByType<UI_JournalPanel>(FindObjectsInactive.Include);
            if (_journalItems == null)
                _journalItems = UnityEngine.Object.FindFirstObjectByType<UI_JournalContent_Item>(FindObjectsInactive.Include);
        }

        if (_journalPanel == null || !_journalPanel.gameObject.activeInHierarchy)
            return;

        _windowRect = GUI.Window(43129, _windowRect, DrawWindow, "Artifact journal search");
    }

    private static void DrawWindow(int id)
    {
        string next = GUI.TextField(new Rect(10f, 28f, 218f, 24f), _query ?? string.Empty);
        if (GUI.Button(new Rect(234f, 28f, 56f, 24f), "Clear"))
            next = string.Empty;

        GUI.Label(new Rect(10f, 55f, 280f, 21f), "Searches localized artifact effect text.");
        if (!string.Equals(next, _query, StringComparison.Ordinal))
        {
            _query = next;
            _journalItems?.RefreshItems((EItemCategory)0);
        }

        GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 24f));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_JournalContent_Item), "RefreshItems")]
    private static void FilterArtifactJournal(UI_JournalContent_Item __instance, EItemCategory category)
    {
        if (_enabled?.Value != true || (int)category != 0 || string.IsNullOrWhiteSpace(_query) ||
            NetworkClient.localPlayer == null || IconsField == null)
            return;

        PlayerAvatar player = NetworkClient.localPlayer.GetComponent<PlayerAvatar>();
        if (player == null || !(IconsField.GetValue(__instance) is List<UI_ItemIcon> icons))
            return;

        var retained = new List<UI_ItemIcon>();
        foreach (UI_ItemIcon icon in icons)
        {
            if (icon == null)
                continue;

            ItemEntity entity = ItemDatabase.FindItemById(icon.Item.entityID);
            if (entity == null || (int)entity.type != 5 || entity.resourcePrefab == null ||
                !entity.resourcePrefab.TryGetComponent(out Charm_Basic charm))
            {
                UnityEngine.Object.Destroy(icon.gameObject);
                continue;
            }

            string effect = charm.BuildEffectString(player, string.Empty, string.Empty,
                charm.maxLevel, 0, false, true);
            if (!string.IsNullOrEmpty(effect) &&
                effect.IndexOf(_query, StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                retained.Add(icon);
            }
            else
            {
                UnityEngine.Object.Destroy(icon.gameObject);
            }
        }

        IconsField.SetValue(__instance, retained);
    }
}
