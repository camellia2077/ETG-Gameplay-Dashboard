// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Diagnostics;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private long _commandPanelPerformanceTraceStartedAt;
        private bool _commandPanelPerformanceTraceActive;
        private int _commandPanelPerformanceTraceId;
        private long _bossSelectionPagePerformanceTraceStartedAt;
        private bool _bossSelectionPagePerformanceTraceActive;
        private int _bossSelectionPagePerformanceTraceId;
        private long _loadoutPagePerformanceTraceStartedAt;
        private bool _loadoutPagePerformanceTraceActive;
        private int _loadoutPagePerformanceTraceId;
        private string _loadoutPagePerformanceTraceMode = string.Empty;
        private long _panelEndToEndTraceStartedAt;
        private bool _panelEndToEndTraceActive;
        private bool _panelEndToEndFirstRepaintLogged;
        private string _panelEndToEndTraceSource = string.Empty;

        private void BeginCommandPanelPerformanceTrace(string source)
        {
            if (!IsPickupBrowserPerformanceLoggingEnabled())
            {
                return;
            }

            if (_panelEndToEndTraceActive)
            {
                LogPanelEndToEndTrace("CancelledBeforeNextOpen", "<none>");
            }

            _commandPanelPerformanceTraceStartedAt = Stopwatch.GetTimestamp();
            _commandPanelPerformanceTraceActive = true;
            _commandPanelPerformanceTraceId++;
            _panelEndToEndTraceStartedAt = _commandPanelPerformanceTraceStartedAt;
            _panelEndToEndTraceActive = true;
            _panelEndToEndFirstRepaintLogged = false;
            _panelEndToEndTraceSource = source ?? string.Empty;
            LogPanelEndToEndTrace("InputDetected", "<none>");
            LogCommandPanelPerformanceMessage(
                "OpenTrace: Toggle.open.begin. TraceId=" +
                _commandPanelPerformanceTraceId +
                ", Page=" +
                _currentPage +
                ".");
        }

        internal void LogPanelEndToEndHostStage(string stageName, string eventType)
        {
            if (!_panelEndToEndTraceActive)
            {
                return;
            }

            LogPanelEndToEndTrace(stageName, eventType);
        }

        internal void CompletePanelEndToEndTraceOnRepaint(string eventType)
        {
            if (!_panelEndToEndTraceActive || !string.Equals(eventType, "Repaint", StringComparison.Ordinal))
            {
                return;
            }

            if (!_panelEndToEndFirstRepaintLogged)
            {
                _panelEndToEndFirstRepaintLogged = true;
                LogPanelEndToEndTrace("FirstRepaint", eventType);
            }

            LogPanelEndToEndTrace("Complete", eventType);
            _panelEndToEndTraceActive = false;
            _panelEndToEndTraceStartedAt = 0L;
            _panelEndToEndTraceSource = string.Empty;
        }

        private void CancelPanelEndToEndTrace(string stageName)
        {
            if (!_panelEndToEndTraceActive)
            {
                return;
            }

            LogPanelEndToEndTrace(stageName, "<none>");
            _panelEndToEndTraceActive = false;
            _panelEndToEndTraceStartedAt = 0L;
            _panelEndToEndTraceSource = string.Empty;
        }

        private void LogPanelEndToEndTrace(string stageName, string eventType)
        {
            if (_performanceLogger == null ||
                (!_panelEndToEndTraceActive && !string.Equals(stageName, "CancelledBeforeNextOpen", StringComparison.Ordinal)))
            {
                return;
            }

            double elapsedMs = _panelEndToEndTraceStartedAt == 0L
                ? 0d
                : GetCommandPanelPerformanceElapsedMilliseconds(_panelEndToEndTraceStartedAt);
            _performanceLogger.LogInfo(
                EtgGameplayDashboardLog.Performance(
                    "PanelTrace: " +
                    (stageName ?? string.Empty) +
                    ". TraceId=" +
                    _commandPanelPerformanceTraceId +
                    ", ElapsedMs=" +
                    elapsedMs.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                    ", Frame=" +
                    Time.frameCount +
                    ", Event=" +
                    (eventType ?? string.Empty) +
                    ", Source=" +
                    _panelEndToEndTraceSource +
                    ", Page=" +
                    _currentPage +
                    "."));
        }

        private long BeginCommandPanelPerformanceStage()
        {
            return _commandPanelPerformanceTraceActive ? Stopwatch.GetTimestamp() : 0L;
        }

        private void LogCommandPanelPerformanceStage(string stageName)
        {
            LogCommandPanelPerformanceStage(stageName, 0L);
        }

        private void LogCommandPanelPerformanceStage(string stageName, long stageStartedAtTimestamp)
        {
            if (!_commandPanelPerformanceTraceActive)
            {
                return;
            }

            double totalMs = GetCommandPanelPerformanceElapsedMilliseconds(_commandPanelPerformanceTraceStartedAt);
            string stageDuration = stageStartedAtTimestamp == 0L
                ? string.Empty
                : ", StageMs=" + GetCommandPanelPerformanceElapsedMilliseconds(stageStartedAtTimestamp).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            LogCommandPanelPerformanceMessage(
                "OpenTrace: Stage=" +
                (stageName ?? string.Empty) +
                ", TraceId=" +
                _commandPanelPerformanceTraceId +
                ", TotalMs=" +
                totalMs.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                stageDuration +
                ", Page=" +
                _currentPage +
                ".");
        }

        private void CompleteCommandPanelPerformanceTrace(string stageName)
        {
            if (!_commandPanelPerformanceTraceActive)
            {
                return;
            }

            LogCommandPanelPerformanceStage(stageName);
            if (string.Equals(stageName, "OnGUI.complete", StringComparison.Ordinal) &&
                Event.current != null &&
                Event.current.type != EventType.Repaint)
            {
                return;
            }

            _commandPanelPerformanceTraceActive = false;
            _commandPanelPerformanceTraceStartedAt = 0L;
        }

        private void LogCommandPanelPerformanceMessage(string message)
        {
            if (_performanceLogger != null && IsPickupBrowserPerformanceLoggingEnabled())
            {
                _performanceLogger.LogInfo(EtgGameplayDashboardLog.Performance(message));
            }
        }

        private static double GetCommandPanelPerformanceElapsedMilliseconds(long startedAtTimestamp)
        {
            if (startedAtTimestamp == 0L)
            {
                return 0d;
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - startedAtTimestamp;
            return elapsedTicks * 1000d / Stopwatch.Frequency;
        }

        private void BeginBossSelectionPagePerformanceTrace()
        {
            if (!IsPickupBrowserPerformanceLoggingEnabled())
            {
                return;
            }

            _bossSelectionPagePerformanceTraceStartedAt = Stopwatch.GetTimestamp();
            _bossSelectionPagePerformanceTraceActive = true;
            _bossSelectionPagePerformanceTraceId++;
            LogCommandPanelPerformanceMessage(
                "BossPage: Selection.begin. TraceId=" +
                _bossSelectionPagePerformanceTraceId + ".");
        }

        private long BeginBossSelectionPagePerformanceStage()
        {
            return _bossSelectionPagePerformanceTraceActive ? Stopwatch.GetTimestamp() : 0L;
        }

        private void LogBossSelectionPagePerformanceStage(string stageName, long stageStartedAtTimestamp, int optionCount)
        {
            if (!_bossSelectionPagePerformanceTraceActive)
            {
                return;
            }

            string stageDuration = stageStartedAtTimestamp == 0L
                ? string.Empty
                : ", StageMs=" + GetCommandPanelPerformanceElapsedMilliseconds(stageStartedAtTimestamp).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            LogCommandPanelPerformanceMessage(
                "BossPage: Stage=" +
                (stageName ?? string.Empty) +
                ", TraceId=" +
                _bossSelectionPagePerformanceTraceId +
                ", TotalMs=" +
                GetCommandPanelPerformanceElapsedMilliseconds(_bossSelectionPagePerformanceTraceStartedAt).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                stageDuration +
                ", OptionCount=" +
                optionCount +
                ".");
        }

        private void CompleteBossSelectionPagePerformanceTrace(string stageName, int optionCount, long stageStartedAtTimestamp)
        {
            if (!_bossSelectionPagePerformanceTraceActive)
            {
                return;
            }

            LogBossSelectionPagePerformanceStage(stageName, stageStartedAtTimestamp, optionCount);
            _bossSelectionPagePerformanceTraceActive = false;
            _bossSelectionPagePerformanceTraceStartedAt = 0L;
        }

        private void CancelBossSelectionPagePerformanceTrace()
        {
            if (!_bossSelectionPagePerformanceTraceActive)
            {
                return;
            }

            LogBossSelectionPagePerformanceStage("Selection.cancelled", 0L, 0);
            _bossSelectionPagePerformanceTraceActive = false;
            _bossSelectionPagePerformanceTraceStartedAt = 0L;
        }

        private void LogBossSelectionActionPerformance(string operationName, long startedAtTimestamp, GrantCommandExecutionResult result)
        {
            if (startedAtTimestamp == 0L)
            {
                return;
            }

            LogCommandPanelPerformanceMessage(
                "BossPage: Operation=" +
                (operationName ?? string.Empty) +
                ", DurationMs=" +
                GetCommandPanelPerformanceElapsedMilliseconds(startedAtTimestamp).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                ", Succeeded=" +
                (result != null && result.Succeeded) +
                ".");
        }

        private void BeginLoadoutPagePerformanceTrace(string mode)
        {
            if (!IsPickupBrowserPerformanceLoggingEnabled())
            {
                return;
            }

            _loadoutPagePerformanceTraceStartedAt = Stopwatch.GetTimestamp();
            _loadoutPagePerformanceTraceActive = true;
            _loadoutPagePerformanceTraceId++;
            _loadoutPagePerformanceTraceMode = mode ?? string.Empty;
            LogLoadoutPagePerformanceStage("Open.begin", 0L);
        }

        private long BeginLoadoutPagePerformanceStage()
        {
            return _loadoutPagePerformanceTraceActive ? Stopwatch.GetTimestamp() : 0L;
        }

        private void LogLoadoutPagePerformanceStage(string stageName, long stageStartedAtTimestamp)
        {
            if (!_loadoutPagePerformanceTraceActive)
            {
                return;
            }

            string stageDuration = stageStartedAtTimestamp == 0L
                ? string.Empty
                : ", StageMs=" + GetCommandPanelPerformanceElapsedMilliseconds(stageStartedAtTimestamp).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            LogCommandPanelPerformanceMessage(
                "LoadoutPage: Stage=" +
                (stageName ?? string.Empty) +
                ", TraceId=" +
                _loadoutPagePerformanceTraceId +
                ", TotalMs=" +
                GetCommandPanelPerformanceElapsedMilliseconds(_loadoutPagePerformanceTraceStartedAt).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                stageDuration +
                ", Mode=" +
                _loadoutPagePerformanceTraceMode +
                ".");
        }

        private void CompleteLoadoutPagePerformanceTrace(string stageName)
        {
            if (!_loadoutPagePerformanceTraceActive)
            {
                return;
            }

            LogLoadoutPagePerformanceStage(stageName, 0L);
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            _loadoutPagePerformanceTraceActive = false;
            _loadoutPagePerformanceTraceStartedAt = 0L;
            _loadoutPagePerformanceTraceMode = string.Empty;
        }
    }
}
