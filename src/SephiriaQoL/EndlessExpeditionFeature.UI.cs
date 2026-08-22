using Mirror;
using UnityEngine;

namespace SephiriaQoL;

internal sealed partial class EndlessExpeditionFeature
{
    internal void OnGUI()
    {
        if (!_pendingChoice && !_active)
            return;

        float scale = OverlayGui.ResolveScale(_panelScale);
        if (_pendingChoice)
        {
            if (_choiceWindow.x <= 0f)
                _choiceWindow.position = new Vector2(
                    Mathf.Max(12f, (Screen.width - 430f * scale) * 0.5f),
                    Mathf.Max(12f, (Screen.height - 280f * scale) * 0.5f));
            _choiceWindow = OverlayGui.BeginScaledWindow(
                43161, _choiceWindow, 430f, 280f, scale, DrawChoiceWindow, out _);
        }
        else
        {
            _statusWindow.x = Mathf.Max(12f, Screen.width - 350f * scale);
            _statusWindow = OverlayGui.BeginScaledWindow(
                43162, _statusWindow, 330f, 168f, scale, DrawStatusWindow, out _);
        }
    }

    private void DrawChoiceWindow(int id)
    {
        OverlayGui.DrawHeader(new Rect(4f, 4f, 422f, 38f));
        GUI.Label(new Rect(18f, 10f, 280f, 25f), "The descent can continue", OverlayGui.TitleStyle);
        OverlayGui.DrawScaleControls(_panelScale, 292f, 11f);

        GUI.Label(new Rect(22f, 58f, 386f, 58f),
            "The final foe has fallen. Begin an Endless Expedition with increasingly dangerous procedural floors, or finish this run normally.",
            OverlayGui.LabelStyle);
        OverlayGui.DrawPanel(new Rect(18f, 126f, 394f, 76f));
        int interval = NetworkServer.active
            ? Mathf.Clamp(_minibossInterval.Value, 2, 10)
            : _displayMinibossInterval;
        GUI.Label(new Rect(32f, 134f, 364f, 20f),
            $"Every {interval} stages, a native room becomes a miniboss milestone.", OverlayGui.LabelStyle);
        int missingPlayers = 0;
        bool canContinue = !NetworkServer.active || CanHostContinue(out missingPlayers);
        GUI.Label(new Rect(32f, 157f, 364f, 38f),
            NetworkServer.active && !canContinue
                ? $"Waiting for {missingPlayers} player(s) to enable the same QoL version."
                : "Expedition floors are temporary and are not written to the run save.",
            NetworkServer.active && !canContinue ? OverlayGui.TitleStyle : OverlayGui.MutedStyle);

        if (NetworkServer.active)
        {
            GUI.enabled = canContinue;
            if (GUI.Button(new Rect(20f, 222f, 242f, 38f), "Continue endlessly", OverlayGui.SelectedButtonStyle))
                StartOnHost();
            GUI.enabled = true;
            if (GUI.Button(new Rect(272f, 222f, 138f, 38f), "Finish run", OverlayGui.ButtonStyle))
                FinishOnHost(victory: true);
        }
        else
        {
            GUI.Label(new Rect(22f, 222f, 386f, 38f),
                "Waiting for the host to choose…", OverlayGui.MutedStyle);
        }

        GUI.DragWindow(new Rect(0f, 0f, 260f, 44f));
    }

    private void DrawStatusWindow(int id)
    {
        OverlayGui.DrawHeader(new Rect(4f, 4f, 322f, 36f));
        GUI.Label(new Rect(16f, 9f, 190f, 24f), $"Endless Stage {_currentStage}", OverlayGui.TitleStyle);
        OverlayGui.DrawScaleControls(_panelScale, 192f, 9f);

        int interval = _displayMinibossInterval;
        int untilMilestone = interval - ((_currentStage - 1) % interval);
        OverlayGui.DrawPanel(new Rect(14f, 50f, 302f, 62f));
        GUI.Label(new Rect(28f, 58f, 274f, 20f),
            $"Enemy HP {_displayHealthMultiplier:0.00}×   •   Count {_displaySpawnMultiplier:0.00}×",
            OverlayGui.LabelStyle);
        GUI.Label(new Rect(28f, 82f, 274f, 18f),
            untilMilestone == 1 ? "Miniboss milestone on this stage" : $"Miniboss milestone in {untilMilestone - 1} stages",
            untilMilestone == 1 ? OverlayGui.TitleStyle : OverlayGui.MutedStyle);

        if (NetworkServer.active)
        {
            if (!_confirmFinish)
            {
                if (GUI.Button(new Rect(174f, 124f, 142f, 28f), "Finish expedition", OverlayGui.ButtonStyle))
                    _confirmFinish = true;
            }
            else
            {
                if (GUI.Button(new Rect(14f, 124f, 142f, 28f), "Cancel", OverlayGui.ButtonStyle))
                    _confirmFinish = false;
                if (GUI.Button(new Rect(164f, 124f, 152f, 28f), "Confirm finish", OverlayGui.DangerButtonStyle))
                    FinishOnHost(victory: true);
            }
        }
        else
        {
            GUI.Label(new Rect(18f, 128f, 296f, 20f), "The host controls expedition progress.", OverlayGui.MutedStyle);
        }
    }
}
