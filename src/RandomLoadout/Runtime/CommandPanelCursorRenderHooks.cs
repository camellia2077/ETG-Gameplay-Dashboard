// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using RandomLoadout.Core.Cursor;
using UnityEngine;

namespace RandomLoadout
{
    internal static class CommandPanelCursorRenderHooks
    {
        private const int SampleFrameInterval = 30;
        private const string CursorOverrideKey = "RandomLoadout.CommandPanelCursorAbovePanel";
        private static Func<bool> s_enabledProvider;
        private static Func<bool> s_probeEnabledProvider;
        private static Func<bool> s_abovePanelEnabledProvider;
        private static Func<Color> s_cursorColorProvider;
        private static ManualLogSource s_logger;
        private static readonly CursorRenderOwnershipService s_cursorRenderOwnershipService = new CursorRenderOwnershipService();
        private static GameCursorController s_lastCursorController;
        private static bool s_cursorOverrideApplied;
        private static string s_lastRenderedColorDescription = string.Empty;
        private static string s_lastInputAndColorStateDescription = string.Empty;
        private static string s_lastPanelCursorStateDescription = string.Empty;
        private static bool s_hasLastUnityCursorState;
        private static bool s_lastUnityCursorVisible;
        private static CursorLockMode s_lastUnityCursorLockMode;
        private static Material s_cursorColorMaterial;
        private static bool s_cursorColorMaterialInitializationAttempted;

        public static void Configure(
            Func<bool> enabledProvider,
            Func<bool> probeEnabledProvider,
            Func<bool> abovePanelEnabledProvider,
            Func<Color> cursorColorProvider,
            ManualLogSource logger)
        {
            s_enabledProvider = enabledProvider;
            s_probeEnabledProvider = probeEnabledProvider;
            s_abovePanelEnabledProvider = abovePanelEnabledProvider;
            s_cursorColorProvider = cursorColorProvider;
            s_logger = logger;
            s_lastCursorController = null;
            s_cursorOverrideApplied = false;
            s_lastRenderedColorDescription = string.Empty;
            s_lastInputAndColorStateDescription = string.Empty;
            s_lastPanelCursorStateDescription = string.Empty;
            s_hasLastUnityCursorState = false;
            ClearCursorColorMaterial();
        }

        public static void UpdateCursorOverride(bool panelVisible)
        {
            CursorRenderDecision ownership = EvaluateCursorOwnership(panelVisible);
            bool customColorEnabled = ownership.ColorMode == CursorColorMode.Custom;
            BraveInput cursorInput = GetMouseInput();
            bool hasMouse = cursorInput != null;
            LogPanelCursorState(panelVisible, cursorInput);
            LogInputAndColorState(panelVisible, customColorEnabled, hasMouse);
            LogUnityCursorStateTransition(panelVisible, ownership, cursorInput);
            bool shouldApply = ownership.PluginOwnsRender;
            if (shouldApply == s_cursorOverrideApplied)
            {
                return;
            }

            if (shouldApply)
            {
                GameCursorController.CursorOverride.SetOverride(CursorOverrideKey, true);
            }
            else
            {
                if (GameCursorController.CursorOverride != null)
                {
                    GameCursorController.CursorOverride.RemoveOverride(CursorOverrideKey);
                }
            }

            s_cursorOverrideApplied = shouldApply;
            if (s_logger != null)
            {
                s_logger.LogInfo(
                    RandomLoadoutLog.CursorRender(
                        "ETG cursor override " + (shouldApply ? "applied" : "removed") +
                        ". PanelVisible=" + panelVisible + ", MouseMode=" + shouldApply + "."));
            }
        }

        public static void ClearCursorOverride()
        {
            if (GameCursorController.CursorOverride != null)
            {
                GameCursorController.CursorOverride.RemoveOverride(CursorOverrideKey);
            }

            s_cursorOverrideApplied = false;
        }

        private static void ClearCursorColorMaterial()
        {
            if (s_cursorColorMaterial != null)
            {
                UnityEngine.Object.Destroy(s_cursorColorMaterial);
                s_cursorColorMaterial = null;
            }

            s_cursorColorMaterialInitializationAttempted = false;
        }

        public static void Install(Harmony harmony, ManualLogSource logger)
        {
            if (harmony == null)
            {
                return;
            }

            MethodInfo targetMethod = AccessTools.Method(typeof(GameCursorController), "OnGUI");
            MethodInfo prefixMethod = AccessTools.Method(typeof(CommandPanelCursorRenderHooks), "GameCursorControllerOnGUIPrefix");
            MethodInfo postfixMethod = AccessTools.Method(typeof(CommandPanelCursorRenderHooks), "GameCursorControllerOnGUIPostfix");
            if (targetMethod == null || prefixMethod == null || postfixMethod == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Cursor render diagnostics hook skipped: GameCursorController.OnGUI target or patch method was not found."));
                }

                return;
            }

            try
            {
                harmony.Patch(
                    targetMethod,
                    prefix: new HarmonyMethod(prefixMethod),
                    postfix: new HarmonyMethod(postfixMethod));
                if (logger != null)
                {
                    logger.LogInfo(RandomLoadoutLog.Init("Cursor render diagnostics hook ready: GameCursorController.OnGUI prefix/postfix."));
                }
            }
            catch (Exception exception)
            {
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Cursor render diagnostics hook failed: " + exception.GetType().Name + ": " + exception.Message));
                }
            }
        }

        public static void LogPluginStage(string stage, bool panelVisible)
        {
            if (!ShouldLog())
            {
                return;
            }

            Log(
                stage +
                ", PanelVisible=" + panelVisible +
                ", Screen=" + Screen.width + "x" + Screen.height +
                ", Mouse=" + FormatVector(Input.mousePosition));
        }

        public static void DrawRenderProbeAfterPanel(bool panelVisible)
        {
            if (!panelVisible ||
                s_probeEnabledProvider == null ||
                !s_probeEnabledProvider() ||
                Event.current == null ||
                Event.current.type != EventType.Repaint)
            {
                return;
            }

            GameCursorController controller = s_lastCursorController;
            if (controller == null)
            {
                controller = UnityEngine.Object.FindObjectOfType<GameCursorController>();
            }

            if (controller == null || !GameManager.HasInstance || GameManager.Instance.IsLoadingLevel || GameManager.IsReturningToBreach)
            {
                return;
            }

            BraveInput cursorInput = GetMouseInput();
            if (cursorInput == null)
            {
                return;
            }

            Texture2D texture = controller.normalCursor;
            int cursorIndex = GameManager.Options.CurrentCursorIndex;
            if (controller.cursors != null && cursorIndex >= 0 && cursorIndex < controller.cursors.Length)
            {
                texture = controller.cursors[cursorIndex];
            }

            if (texture == null)
            {
                return;
            }

            Vector2 mousePosition = cursorInput.MousePosition;
            mousePosition.y = Screen.height - mousePosition.y;
            int scaleTileScale = Pixelator.Instance != null ? (int)Pixelator.Instance.ScaleTileScale : 3;
            Vector2 size = new Vector2(texture.width, texture.height) * scaleTileScale;
            Rect screenRect = new Rect(
                mousePosition.x - size.x / 2f,
                mousePosition.y - size.y / 2f,
                size.x,
                size.y);

            Graphics.DrawTexture(
                screenRect,
                texture,
                new Rect(0f, 0f, 1f, 1f),
                0,
                0,
                0,
                0,
                Color.white);

            if (s_enabledProvider != null && s_enabledProvider() && s_logger != null && Time.frameCount % SampleFrameInterval == 0)
            {
                Log("Cursor render probe drawn after Control Panel, OffsetX=0.0, Tint=white.");
            }
        }

        public static void DrawCursorAfterPanel(bool panelVisible)
        {
            // Keep ownership of the ETG cursor while a mouse-capable player exists.
            // Otherwise closing the panel releases the native cursor, which can make
            // ETG immediately redraw the P2 cursor with its purple tint even though
            // the plugin color is disabled and configured as white.
            // While the Control Panel is open, keep drawing even before BraveInput
            // recognizes a mouse owner. Keyboard/controller handoff can briefly leave
            // both players at HasMouse=false; waiting for that transition made the
            // in-game cursor appear only after the first physical mouse click.
            if (EvaluateCursorOwnership(panelVisible).PluginOwnsRender || panelVisible)
            {
                DrawEtgCursors(panelVisible);
                return;
            }

            DrawRenderProbeAfterPanel(panelVisible);
        }

        private static bool ShouldDrawCursorAbovePanel(bool panelVisible)
        {
            // The ETG cursor must remain visible above the panel even when
            // Combat Cursor coloring is disabled. Panel layering is not a
            // color setting, so do not gate it on either color state or the
            // legacy optional toggle.
            return panelVisible &&
                GameManager.HasInstance &&
                GetMouseInput() != null;
        }

        private static bool IsCustomCursorColorEnabled()
        {
            // Combat Cursor OFF means "use the original white tint" only. It
            // intentionally does not disable the separate panel-layer pass.
            if (s_cursorColorProvider == null)
            {
                return false;
            }

            Color color = s_cursorColorProvider();
            return color.r != 1f || color.g != 1f || color.b != 1f || color.a != 1f;
        }

        private static void LogInputAndColorState(bool panelVisible, bool customColorEnabled, bool playerOneHasMouse)
        {
            if (!ShouldLog())
            {
                s_lastInputAndColorStateDescription = string.Empty;
                return;
            }

            GameManager gameManager = GameManager.HasInstance ? GameManager.Instance : null;
            BraveInput playerOneInput = gameManager != null ? BraveInput.GetInstanceForPlayer(0) : null;
            BraveInput playerTwoInput = gameManager != null ? BraveInput.GetInstanceForPlayer(1) : null;
            BraveInput cursorInput = GetMouseInput();
            string state =
                "PanelVisible=" + panelVisible +
                ", GameType=" + (gameManager != null ? gameManager.CurrentGameType.ToString() : "<null>") +
                ", P1=" + DescribePlayer(gameManager != null ? gameManager.PrimaryPlayer : null) +
                ", P2=" + DescribePlayer(gameManager != null ? gameManager.SecondaryPlayer : null) +
                ", P1Input=" + DescribeInput(playerOneInput) +
                ", P2Input=" + DescribeInput(playerTwoInput) +
                ", CursorInput=" + DescribeInput(cursorInput) +
                ", PlayerOneHasMouse=" + playerOneHasMouse +
                ", CustomColorEnabled=" + customColorEnabled +
                ", CursorColor=" + DescribeColor(s_cursorColorProvider != null ? s_cursorColorProvider() : Color.white);
            if (string.Equals(state, s_lastInputAndColorStateDescription, StringComparison.Ordinal))
            {
                return;
            }

            s_lastInputAndColorStateDescription = state;
            Log("Cursor/input state changed. " + state + ".");
        }

        private static void LogPanelCursorState(bool panelVisible, BraveInput cursorInput)
        {
            if (!IsVerboseLoggingEnabled())
            {
                s_lastPanelCursorStateDescription = string.Empty;
                return;
            }

            GameManager gameManager = GameManager.HasInstance ? GameManager.Instance : null;
            BraveInput playerOneInput = gameManager != null ? BraveInput.GetInstanceForPlayer(0) : null;
            BraveInput playerTwoInput = gameManager != null ? BraveInput.GetInstanceForPlayer(1) : null;
            Color configuredColor = s_cursorColorProvider != null ? s_cursorColorProvider() : Color.white;
            string state =
                "PanelVisible=" + panelVisible +
                ", CursorInput=" + DescribeInput(cursorInput) +
                ", P1Input=" + DescribeInput(playerOneInput) +
                ", P2Input=" + DescribeInput(playerTwoInput) +
                ", CustomColorEnabled=" + IsCustomCursorColorEnabled() +
                ", ConfiguredColor=" + DescribeColor(configuredColor) +
                ", CursorOverrideApplied=" + s_cursorOverrideApplied +
                ", CursorOverrideAvailable=" + (GameCursorController.CursorOverride != null) +
                ", CursorIndex=" + (GameManager.HasInstance && GameManager.Options != null ? GameManager.Options.CurrentCursorIndex.ToString() : "<null>") +
                ", UnityCursorVisible=" + Cursor.visible +
                ", UnityCursorLockMode=" + Cursor.lockState;
            if (string.Equals(state, s_lastPanelCursorStateDescription, StringComparison.Ordinal))
            {
                return;
            }

            s_lastPanelCursorStateDescription = state;
            s_logger.LogInfo(RandomLoadoutLog.CursorRender("Panel cursor state transition. " + state + "."));
        }

        private static void LogUnityCursorStateTransition(bool panelVisible, CursorRenderDecision ownership, BraveInput cursorInput)
        {
            if (!IsVerboseLoggingEnabled())
            {
                s_hasLastUnityCursorState = false;
                return;
            }

            bool cursorVisible = Cursor.visible;
            CursorLockMode cursorLockMode = Cursor.lockState;
            if (s_hasLastUnityCursorState &&
                s_lastUnityCursorVisible == cursorVisible &&
                s_lastUnityCursorLockMode == cursorLockMode)
            {
                return;
            }

            string previousState = s_hasLastUnityCursorState
                ? "Visible=" + s_lastUnityCursorVisible + ",LockMode=" + s_lastUnityCursorLockMode
                : "<unknown>";
            s_hasLastUnityCursorState = true;
            s_lastUnityCursorVisible = cursorVisible;
            s_lastUnityCursorLockMode = cursorLockMode;
            Log(
                "Unity cursor state changed. Previous=" + previousState +
                ", Current=Visible=" + cursorVisible + ",LockMode=" + cursorLockMode +
                ", PanelVisible=" + panelVisible +
                ", PluginOwnsRender=" + ownership.PluginOwnsRender +
                ", CursorInput=" + DescribeInput(cursorInput) +
                ", Mouse=" + FormatVector(Input.mousePosition));
        }

        private static string DescribePlayer(PlayerController player)
        {
            if ((object)player == null)
            {
                return "<null>";
            }

            try
            {
                return "Id=" + player.GetInstanceID() +
                       ",Name=" + player.name +
                       ",Index=" + player.PlayerIDX +
                       ",Character=" + player.characterIdentity +
                       ",Active=" + player.gameObject.activeInHierarchy +
                       ",InputOverridden=" + player.IsInputOverridden;
            }
            catch (Exception exception)
            {
                return "StateReadFailed=" + exception.GetType().Name;
            }
        }

        private static string DescribeInput(BraveInput input)
        {
            if (input == null)
            {
                return "<null>";
            }

            try
            {
                InControl.InputDevice device = input.ActiveActions != null ? input.ActiveActions.Device : null;
                return "HasMouse=" + input.HasMouse() +
                       ",Device=" + (device != null ? device.Name + "/" + device.DeviceClass : "<none>");
            }
            catch (Exception exception)
            {
                return "StateReadFailed=" + exception.GetType().Name;
            }
        }

        private static void DrawEtgCursors(bool panelVisible)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (s_lastCursorController == null)
            {
                LogCursorRedrawSkip(panelVisible, "NoCachedGameCursorController");
                return;
            }

            if (!GameManager.HasInstance)
            {
                LogCursorRedrawSkip(panelVisible, "GameManagerUnavailable");
                return;
            }

            if (GameManager.Instance.IsLoadingLevel)
            {
                LogCursorRedrawSkip(panelVisible, "LevelLoading");
                return;
            }

            if (GameManager.IsReturningToBreach)
            {
                LogCursorRedrawSkip(panelVisible, "ReturningToCharacterSelect");
                return;
            }

            Texture2D texture = GetSelectedCursorTexture(s_lastCursorController);
            if (texture == null)
            {
                LogCursorRedrawSkip(panelVisible, "SelectedCursorTextureUnavailable");
                return;
            }

            BraveInput cursorInput = GetMouseInput();
            if (cursorInput == null && !panelVisible)
            {
                LogCursorRedrawSkip(panelVisible, "MouseInputUnavailable");
                return;
            }

            bool usingUnityMousePositionFallback = cursorInput == null;
        Vector2 mousePosition;
        if (usingUnityMousePositionFallback)
        {
            Vector3 unityMousePosition = Input.mousePosition;
            mousePosition = new Vector2(unityMousePosition.x, unityMousePosition.y);
        }
        else
        {
            mousePosition = cursorInput.MousePosition;
        }
            mousePosition.y = Screen.height - mousePosition.y;
            Color cursorColor = s_cursorColorProvider != null ? s_cursorColorProvider() : Color.white;
            string colorDescription = DescribeColor(cursorColor);
            if (s_logger != null && !string.Equals(colorDescription, s_lastRenderedColorDescription, StringComparison.Ordinal))
            {
                Log("Custom cursor render color: " + colorDescription + ".");
                s_lastRenderedColorDescription = colorDescription;
            }

            Rect cursorRect = DrawCursorTexture(texture, mousePosition, cursorColor);
            if (ShouldLog())
            {
                Log(
                    "Game cursor texture draw submitted after Control Panel. PanelVisible=" + panelVisible +
                    ", Texture=" + texture.width + "x" + texture.height +
                    ", DrawRect=" + FormatRect(cursorRect) +
                    ", PositionSource=" + (usingUnityMousePositionFallback ? "UnityInput.mousePosition" : "BraveInput.MousePosition") +
                    ", Color=" + colorDescription +
                    ", GUIEnabled=" + GUI.enabled +
                    ", GUIColor=" + DescribeColor(GUI.color) +
                    ", " + DescribeCursor(s_lastCursorController));
            }
        }

        private static BraveInput GetMouseInput()
        {
            if (!GameManager.HasInstance)
            {
                return null;
            }

            BraveInput playerOneInput = BraveInput.GetInstanceForPlayer(0);
            if (playerOneInput != null && playerOneInput.HasMouse())
            {
                return playerOneInput;
            }

            BraveInput playerTwoInput = BraveInput.GetInstanceForPlayer(1);
            if (playerTwoInput != null && playerTwoInput.HasMouse())
            {
                return playerTwoInput;
            }

            return null;
        }

        private static CursorRenderDecision EvaluateCursorOwnership(bool panelVisible)
        {
            if (!GameManager.HasInstance)
            {
                return s_cursorRenderOwnershipService.Evaluate(
                    panelVisible,
                    false,
                    false,
                    IsCustomCursorColorEnabled(),
                    false);
            }

            BraveInput playerOneInput = BraveInput.GetInstanceForPlayer(0);
            BraveInput playerTwoInput = BraveInput.GetInstanceForPlayer(1);
            return s_cursorRenderOwnershipService.Evaluate(
                panelVisible,
                playerOneInput != null && playerOneInput.HasMouse(),
                playerTwoInput != null && playerTwoInput.HasMouse(),
                IsCustomCursorColorEnabled(),
                GameCursorController.CursorOverride != null);
        }

        private static void LogCursorRedrawSkip(bool panelVisible, string reason)
        {
            if (ShouldLog())
            {
                Log("Cursor redraw skipped after Control Panel. PanelVisible=" + panelVisible + ", Reason=" + reason + ".");
            }
        }

        private static Texture2D GetSelectedCursorTexture(GameCursorController controller)
        {
            Texture2D texture = controller.normalCursor;
            int cursorIndex = GameManager.Options.CurrentCursorIndex;
            if (controller.cursors != null && cursorIndex >= 0 && cursorIndex < controller.cursors.Length)
            {
                texture = controller.cursors[cursorIndex];
            }

            return texture;
        }

        private static Rect DrawCursorTexture(Texture2D texture, Vector2 position, Color color)
        {
            int scaleTileScale = Pixelator.Instance != null ? (int)Pixelator.Instance.ScaleTileScale : 3;
            Vector2 size = new Vector2(texture.width, texture.height) * scaleTileScale;
            Rect screenRect = new Rect(
                position.x + 0.5f - size.x / 2f,
                position.y + 0.5f - size.y / 2f,
                size.x,
                size.y);
            // Graphics.DrawTexture is submitted to Unity's graphics pipeline. Boss UI can
            // render after that submission and hide the cursor even though this call ran.
            // The Control Panel itself is IMGUI, so draw the cursor in the same pass after
            // the panel and restore the prior tint for unrelated IMGUI users.
            Color previousColor = GUI.color;
            try
            {
                GUI.color = color;
                GUI.DrawTexture(screenRect, texture);
            }
            finally
            {
                GUI.color = previousColor;
            }

            return screenRect;
        }

        private static bool EnsureCursorColorMaterial()
        {
            if (s_cursorColorMaterial != null)
            {
                return true;
            }

            if (s_cursorColorMaterialInitializationAttempted)
            {
                return false;
            }

            s_cursorColorMaterialInitializationAttempted = true;
            Shader shader = Shader.Find("UI/Default");
            if (shader == null)
            {
                LogCursorShaderFailure("UI/Default was not found.");
                return false;
            }

            try
            {
                s_cursorColorMaterial = new Material(shader);
                s_cursorColorMaterial.name = "RandomLoadout.CursorColorOverride";
                s_cursorColorMaterial.hideFlags = HideFlags.HideAndDontSave;
                s_cursorColorMaterial.SetInt("_ColorMask", 15);
                s_cursorColorMaterial.SetInt("_StencilComp", 8);
                s_cursorColorMaterial.SetFloat("_UseUIAlphaClip", 0f);
                if (!s_cursorColorMaterial.HasProperty("_Color"))
                {
                    ClearCursorColorMaterial();
                    LogCursorShaderFailure("UI/Default does not expose _Color.");
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                ClearCursorColorMaterial();
                LogCursorShaderFailure(exception.GetType().Name + ": " + exception.Message);
                return false;
            }
        }

        private static void LogCursorShaderFailure(string message)
        {
            if (s_logger != null)
            {
                s_logger.LogWarning(RandomLoadoutLog.CursorRender("Cursor color shader unavailable: " + message));
            }
        }

        private static string DescribeColor(Color color)
        {
            CombatCursorColorOption[] options = CombatCursorColorCatalog.GetOptions();
            for (int index = 0; index < options.Length; index++)
            {
                Color optionColor = options[index].Color;
                if (Mathf.Abs(optionColor.r - color.r) < 0.002f &&
                    Mathf.Abs(optionColor.g - color.g) < 0.002f &&
                    Mathf.Abs(optionColor.b - color.b) < 0.002f &&
                    Mathf.Abs(optionColor.a - color.a) < 0.002f)
                {
                    return options[index].Id + " " + options[index].Hex +
                        " RGB(" + color.r.ToString("F3") + "," + color.g.ToString("F3") + "," + color.b.ToString("F3") + ")";
                }
            }

            return "unknown RGB(" + color.r.ToString("F3") + "," + color.g.ToString("F3") + "," + color.b.ToString("F3") + ")";
        }

        private static bool GameCursorControllerOnGUIPrefix(GameCursorController __instance)
        {
            s_lastCursorController = __instance;
            bool suppressVanillaCursor = IsCustomCursorColorEnabled();
            if (ShouldLog())
            {
                Log("GameCursorController.OnGUI.prefix, SuppressVanillaCursor=" + suppressVanillaCursor + ", " + DescribeCursor(__instance));
            }

            // A colored cursor is drawn by DrawCursorAfterPanel. Suppressing
            // the vanilla pass prevents ETG's original texture from being
            // rendered over it and making the selected color look incorrect.
            return !suppressVanillaCursor;
        }

        private static void GameCursorControllerOnGUIPostfix(GameCursorController __instance)
        {
            if (!ShouldLog())
            {
                return;
            }

            Log("GameCursorController.OnGUI.postfix, " + DescribeCursor(__instance));
        }

        private static bool ShouldLog()
        {
            return IsVerboseLoggingEnabled() &&
                Event.current != null &&
                Event.current.type == EventType.Repaint &&
                Time.frameCount % SampleFrameInterval == 0;
        }

        private static bool IsVerboseLoggingEnabled()
        {
            return s_enabledProvider != null &&
                s_enabledProvider() &&
                s_logger != null;
        }

        private static string DescribeCursor(GameCursorController controller)
        {
            int normalWidth = controller != null && controller.normalCursor != null ? controller.normalCursor.width : 0;
            int normalHeight = controller != null && controller.normalCursor != null ? controller.normalCursor.height : 0;
            int cursorCount = controller != null && controller.cursors != null ? controller.cursors.Length : 0;
            return "UnityOSCursorVisible=" + Cursor.visible +
                ", UnityCursorLockMode=" + Cursor.lockState +
                ", NormalCursor=" + normalWidth + "x" + normalHeight +
                ", CursorCount=" + cursorCount +
                ", Mouse=" + FormatVector(Input.mousePosition) +
                ", Screen=" + Screen.width + "x" + Screen.height;
        }

        private static string FormatVector(Vector3 value)
        {
            return "(" + value.x.ToString("0.0") + "," + value.y.ToString("0.0") + "," + value.z.ToString("0.0") + ")";
        }

        private static string FormatRect(Rect value)
        {
            return "(" + value.x.ToString("0.0") + "," + value.y.ToString("0.0") + "," +
                value.width.ToString("0.0") + "x" + value.height.ToString("0.0") + ")";
        }

        private static void Log(string message)
        {
            s_logger.LogInfo(
                RandomLoadoutLog.CursorRender(
                    "Frame=" + Time.frameCount +
                    ", Event=" + (Event.current != null ? Event.current.type.ToString() : "<null>") +
                    ", GUIDepth=" + GUI.depth +
                    ", " + message + "."));
        }
    }
}
