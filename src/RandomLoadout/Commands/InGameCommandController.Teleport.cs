using System;
using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void DrawTeleportPanelIfEnabled(Rect mainPanelRect, ManualLogSource logger)
        {
            if (!_showTeleportPanel)
            {
                return;
            }

            float panelTop = Mathf.Max(8f, Mathf.Min(mainPanelRect.y, Screen.height - TeleportPanelHeight - 8f));
            Rect panelRect = new Rect(
                mainPanelRect.x - TeleportPanelWidth - ButtonGap,
                panelTop,
                TeleportPanelWidth,
                TeleportPanelHeight);
            GUI.Box(panelRect, GUIContent.none, _panelStyle);

            Rect closeButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(closeButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                _showTeleportPanel = false;
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - ButtonWidth - 32f, 24f),
                GuiText.Get("gui.teleport.title"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 42f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.teleport.hint"),
                _hintStyle);

            float rowY = panelRect.y + 72f;
            DrawTeleportSectionLabel(panelRect, ref rowY, "gui.teleport.section.start");
            DrawTeleportOptionButton(panelRect, ref rowY, TeleportOptions[0], logger);
            DrawTeleportOptionButton(panelRect, ref rowY, TeleportOptions[1], logger);

            DrawTeleportSectionLabel(panelRect, ref rowY, "gui.teleport.section.continue");
            DrawTeleportOptionButton(panelRect, ref rowY, TeleportOptions[2], logger);
            DrawTeleportOptionButton(panelRect, ref rowY, TeleportOptions[3], logger);
            DrawTeleportOptionButton(panelRect, ref rowY, TeleportOptions[4], logger);
            DrawTeleportOptionButton(panelRect, ref rowY, TeleportOptions[5], logger);
            DrawTeleportOptionButton(panelRect, ref rowY, TeleportOptions[6], logger);

            DrawTeleportSectionLabel(panelRect, ref rowY, "gui.teleport.section.option");
            DrawTeleportOptionButton(panelRect, ref rowY, TeleportOptions[7], logger);
            DrawTeleportOptionButton(panelRect, ref rowY, TeleportOptions[8], logger);

            DrawTeleportSectionLabel(panelRect, ref rowY, "gui.teleport.section.quick_restart");
            DrawTeleportOptionButton(panelRect, ref rowY, TeleportOptions[9], logger);
        }

        private void DrawTeleportSectionLabel(Rect panelRect, ref float rowY, string labelKey)
        {
            GUI.Label(
                new Rect(panelRect.x + 14f, rowY, panelRect.width - 28f, 18f),
                GuiText.Get(labelKey),
                _hintStyle);
            rowY += 20f;
        }

        private void DrawTeleportOptionButton(Rect panelRect, ref float rowY, TeleportOption option, ManualLogSource logger)
        {
            Rect buttonRect = new Rect(panelRect.x + 14f, rowY, panelRect.width - 28f, 28f);
            if (GUI.Button(buttonRect, GuiText.Get(option.LabelKey), _buttonStyle))
            {
                ExecuteTeleport(option, logger);
            }

            rowY += 32f;
        }

        private void ExecuteTeleport(TeleportOption option, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = TryTeleport(option);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                _inputText = option.CommandText;
                _showTeleportPanel = false;
                _focusInputField = true;
            }

            if (logger == null)
            {
                return;
            }

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private GrantCommandExecutionResult TryTeleport(TeleportOption option)
        {
            if (option == null || string.IsNullOrEmpty(option.SceneName))
            {
                return GrantCommandExecutionResult.Localized(false, "result.teleport.unavailable");
            }

            if (_bossRushService != null && _bossRushService.IsActive)
            {
                return GrantCommandExecutionResult.Localized(false, "result.teleport.boss_rush_active");
            }

            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.teleport.unavailable");
            }

            try
            {
                if (gameManager.IsFoyer && (object)Foyer.Instance != null)
                {
                    Foyer.Instance.OnDepartedFoyer();
                }

                gameManager.LoadCustomLevel(option.SceneName);
                return new GrantCommandExecutionResult(
                    true,
                    GuiText.Get("result.teleport.success", GuiText.Get(option.LabelKey)),
                    GuiText.GetEnglish("result.teleport.success", GuiText.GetEnglish(option.LabelKey)) + " Command=" + option.CommandText + ".");
            }
            catch (Exception exception)
            {
                return new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("result.teleport.failure", GuiText.Get(option.LabelKey)),
                    GuiText.GetEnglish("result.teleport.failure", GuiText.GetEnglish(option.LabelKey)) + " " + exception.GetType().Name + ".");
            }
        }
    }
}
