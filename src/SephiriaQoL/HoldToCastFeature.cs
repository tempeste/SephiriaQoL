using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaQoL;

internal static class HoldToCastFeature
{
    private const float ReadyThreshold = 0.001f;

    private static readonly FieldInfo ActionControllerField =
        AccessTools.Field(typeof(PlayerInputController), "integratedActionController");
    private static readonly FieldInfo AvatarField =
        AccessTools.Field(typeof(PlayerInputController), "avatar");
    private static readonly FieldInfo SkillControllerField =
        AccessTools.Field(typeof(PlayerInputController), "skillController");
    private static readonly MethodInfo GetAimedPositionMethod =
        AccessTools.Method(typeof(PlayerInputController), "GetAimedPosition");

    private static readonly InputAction[] HeldActions = new InputAction[8];
    private static readonly bool[] AttemptedWhileReady = new bool[8];

    private static ConfigEntry<bool> _enabled;
    private static ManualLogSource _logger;
    private static InputAction _heldCastModeAction;
    private static bool _castModeAttemptedWhileReady;
    private static bool _loggedFailure;

    internal static bool IsHolding =>
        _enabled?.Value == true && (AnyQuickCastHeld() || IsActionHeld(_heldCastModeAction));

    internal static void Configure(ConfigEntry<bool> enabled, ManualLogSource logger)
    {
        _enabled = enabled;
        _logger = logger;
    }

    internal static void Update()
    {
        if (_enabled?.Value != true)
        {
            ClearHeldActions();
            return;
        }

        PlayerInputController inputController = PlayerInputController.Instance;
        if (!CanAcceptHeldInput(inputController))
        {
            ClearHeldActions();
            return;
        }

        try
        {
            IntegratedActionController actionController =
                (IntegratedActionController)ActionControllerField.GetValue(inputController);
            PlayerAvatar avatar = (PlayerAvatar)AvatarField.GetValue(inputController);
            SkillController skillController = (SkillController)SkillControllerField.GetValue(inputController);
            if (actionController == null || avatar == null || skillController == null)
                return;

            for (int slotIndex = 0; slotIndex < HeldActions.Length; slotIndex++)
                UpdateHeldSlot(inputController, actionController, avatar, skillController, slotIndex);

            UpdateCastModeSlot(inputController, actionController, avatar, skillController);
        }
        catch (Exception exception)
        {
            if (_loggedFailure)
                return;

            _loggedFailure = true;
            _logger?.LogWarning($"Hold-to-cast paused after a runtime API mismatch: {exception.Message}");
        }
    }

    private static void UpdateHeldSlot(
        PlayerInputController inputController,
        IntegratedActionController actionController,
        PlayerAvatar avatar,
        SkillController skillController,
        int slotIndex)
    {
        InputAction action = HeldActions[slotIndex];
        if (!IsActionHeld(action))
        {
            HeldActions[slotIndex] = null;
            AttemptedWhileReady[slotIndex] = false;
            return;
        }

        bool ready = IsSlotReady(actionController, avatar, skillController, slotIndex);
        if (!ready)
        {
            AttemptedWhileReady[slotIndex] = false;
            return;
        }

        if (AttemptedWhileReady[slotIndex])
            return;

        Cast(inputController, actionController, slotIndex);
        AttemptedWhileReady[slotIndex] = true;
    }

    private static void UpdateCastModeSlot(
        PlayerInputController inputController,
        IntegratedActionController actionController,
        PlayerAvatar avatar,
        SkillController skillController)
    {
        if (!IsActionHeld(_heldCastModeAction) || !avatar.activeMagicCastModeClientside)
        {
            _heldCastModeAction = null;
            _castModeAttemptedWhileReady = false;
            return;
        }

        int slotIndex = inputController.currentQuickSlotIdx;
        bool ready = IsSlotReady(actionController, avatar, skillController, slotIndex);
        if (!ready)
        {
            _castModeAttemptedWhileReady = false;
            return;
        }

        if (_castModeAttemptedWhileReady)
            return;

        Cast(inputController, actionController, slotIndex);
        _castModeAttemptedWhileReady = true;
    }

    private static bool IsSlotReady(
        IntegratedActionController actionController,
        PlayerAvatar avatar,
        SkillController skillController,
        int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= actionController.quickSlots.Length)
            return false;

        QuickSlotData slot = actionController.quickSlots[slotIndex];
        if (slot == null || slot.IsEmpty)
            return false;

        switch (slot.Type)
        {
            case QuickSlotType.Magic:
                return slot.magic != null &&
                       skillController.CanCast &&
                       !skillController.IsInGlobalCooldown &&
                       slot.magic.CooldownRatio <= ReadyThreshold &&
                       slot.magic.CanCast(avatar, true, true) == ECanUseSkillResult.Succeeded;
            case QuickSlotType.Active:
                return slot.active != null && slot.active.GetCooldownRatio() <= ReadyThreshold;
            default:
                return false;
        }
    }

    private static void Cast(
        PlayerInputController inputController,
        IntegratedActionController actionController,
        int slotIndex)
    {
        Vector2 aimedPosition = (Vector2)GetAimedPositionMethod.Invoke(inputController, null);
        actionController.Cast(slotIndex, aimedPosition, inputController.autoAimedTarget);
    }

    private static bool CanAcceptHeldInput(PlayerInputController controller)
    {
        if (controller == null || !controller.enabled || !controller.HasAvatar || controller.BlockAvatarInput)
            return false;

        return UIManager.Instance == null || UIManager.Instance.CurrentControlStack == null;
    }

    private static bool IsActionHeld(InputAction action) => action != null && action.enabled && action.IsPressed();

    private static bool AnyQuickCastHeld()
    {
        for (int index = 0; index < HeldActions.Length; index++)
        {
            if (IsActionHeld(HeldActions[index]))
                return true;
        }

        return false;
    }

    private static void ClearHeldActions()
    {
        Array.Clear(HeldActions, 0, HeldActions.Length);
        Array.Clear(AttemptedWhileReady, 0, AttemptedWhileReady.Length);
        _heldCastModeAction = null;
        _castModeAttemptedWhileReady = false;
    }

    private static void ObserveQuickCast(int slotIndex, InputAction action)
    {
        if (_enabled?.Value != true || slotIndex < 0 || slotIndex >= HeldActions.Length)
            return;

        HeldActions[slotIndex] = action;
        AttemptedWhileReady[slotIndex] = action != null && action.IsPressed();
    }

    private static void ObserveCastMode(PlayerInputController controller, InputAction.CallbackContext input)
    {
        if (_enabled?.Value != true || controller == null || !input.action.IsPressed())
            return;

        string controlPath = input.control?.path;
        PlayerAvatar avatar = (PlayerAvatar)AvatarField.GetValue(controller);
        if (avatar == null || !avatar.activeMagicCastModeClientside ||
            string.IsNullOrEmpty(controlPath) || !controlPath.Contains("Mouse/left"))
            return;

        _heldCastModeAction = input.action;
        _castModeAttemptedWhileReady = true;
    }

    [HarmonyPatch]
    private static class QuickCastInputPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            for (int slot = 1; slot <= 8; slot++)
            {
                MethodInfo method = AccessTools.Method(
                    typeof(PlayerInputController), $"HandleOnQuickCast{slot}");
                if (method != null)
                    yield return method;
            }
        }

        private static void Prefix(MethodBase __originalMethod, InputAction.CallbackContext input)
        {
            int slotIndex = __originalMethod.Name[__originalMethod.Name.Length - 1] - '1';
            ObserveQuickCast(slotIndex, input.action);
        }
    }

    [HarmonyPatch(typeof(PlayerInputController), nameof(PlayerInputController.HandleOnFire))]
    private static class CastModeInputPatch
    {
        private static void Prefix(PlayerInputController __instance, InputAction.CallbackContext input) =>
            ObserveCastMode(__instance, input);
    }

    [HarmonyPatch(typeof(SkillController), "UserCode_RpcUseMagic__ECanUseSkillResult")]
    private static class CooldownFeedbackPatch
    {
        private static bool Prefix(ECanUseSkillResult res) =>
            res != ECanUseSkillResult.Failed_NotYet || !IsHolding;
    }
}
