// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Diagnostics;

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

        private void BeginCommandPanelPerformanceTrace()
        {
            if (!IsPickupBrowserPerformanceLoggingEnabled())
            {
                return;
            }

            _commandPanelPerformanceTraceStartedAt = Stopwatch.GetTimestamp();
            _commandPanelPerformanceTraceActive = true;
            _commandPanelPerformanceTraceId++;
            LogCommandPanelPerformanceMessage(
                "OpenTrace: Toggle.open.begin. TraceId=" +
                _commandPanelPerformanceTraceId +
                ", Page=" +
                _currentPage +
                ".");
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
                : ", StageMs=" + GetCommandPanelPerformanceElapsedMilliseconds(stageStartedAtTimestamp).ToString("0.00");
            LogCommandPanelPerformanceMessage(
                "OpenTrace: Stage=" +
                (stageName ?? string.Empty) +
                ", TraceId=" +
                _commandPanelPerformanceTraceId +
                ", TotalMs=" +
                totalMs.ToString("0.00") +
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
                : ", StageMs=" + GetCommandPanelPerformanceElapsedMilliseconds(stageStartedAtTimestamp).ToString("0.00");
            LogCommandPanelPerformanceMessage(
                "BossPage: Stage=" +
                (stageName ?? string.Empty) +
                ", TraceId=" +
                _bossSelectionPagePerformanceTraceId +
                ", TotalMs=" +
                GetCommandPanelPerformanceElapsedMilliseconds(_bossSelectionPagePerformanceTraceStartedAt).ToString("0.00") +
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
                GetCommandPanelPerformanceElapsedMilliseconds(startedAtTimestamp).ToString("0.00") +
                ", Succeeded=" +
                (result != null && result.Succeeded) +
                ".");
        }
    }
}
