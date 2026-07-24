// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Diagnostics;
using BepInEx.Logging;

namespace EtgGameplayDashboard
{
    internal sealed class PerformanceDiagnostics
    {
        private const float SummaryIntervalSeconds = 5f;
        private const double LongFrameThresholdMs = 40d;
        private const double SlowStepThresholdMs = 8d;

        private readonly ManualLogSource _logger;
        private readonly Func<bool> _enabledProvider;

        private float _summaryWindowStartedAt = -1f;
        private int _summaryFrameCount;
        private double _summaryTotalFrameMs;
        private double _summaryWorstFrameMs;
        private int _summaryLongFrameCount;
        private int _summarySlowStepCount;
        private string _summarySlowestStepName = string.Empty;
        private double _summarySlowestStepMs;
        private string _summarySlowestStepDetail = string.Empty;
        private string _lastEvent = "startup";

        public PerformanceDiagnostics(ManualLogSource logger, Func<bool> enabledProvider)
        {
            _logger = logger;
            _enabledProvider = enabledProvider;
        }

        public long BeginSample()
        {
            return IsEnabled() ? Stopwatch.GetTimestamp() : 0L;
        }

        public void MarkEvent(string eventSummary)
        {
            if (!IsEnabled() || string.IsNullOrEmpty(eventSummary))
            {
                return;
            }

            _lastEvent = eventSummary;
        }

        public void RecordFrame(float unscaledDeltaTime, float realtimeSinceStartup, string detail)
        {
            if (!IsEnabled())
            {
                ResetSummary();
                return;
            }

            if (_summaryWindowStartedAt < 0f)
            {
                _summaryWindowStartedAt = realtimeSinceStartup;
            }

            double frameMs = unscaledDeltaTime * 1000d;
            _summaryFrameCount++;
            _summaryTotalFrameMs += frameMs;
            if (frameMs > _summaryWorstFrameMs)
            {
                _summaryWorstFrameMs = frameMs;
            }

            if (frameMs >= LongFrameThresholdMs)
            {
                _summaryLongFrameCount++;
                LogWarning(
                    "Long frame captured. FrameMs=" +
                    frameMs.ToString("0.00") +
                    ", ThresholdMs=" +
                    LongFrameThresholdMs.ToString("0.00") +
                    ", LastEvent=" +
                    _lastEvent +
                    FormatDetail(detail) +
                    ".");
            }

            if (realtimeSinceStartup - _summaryWindowStartedAt < SummaryIntervalSeconds)
            {
                return;
            }

            double averageFrameMs = _summaryFrameCount > 0 ? _summaryTotalFrameMs / _summaryFrameCount : 0d;
            double averageFps = averageFrameMs > 0d ? 1000d / averageFrameMs : 0d;
            LogInfo(
                "FPS summary. WindowSeconds=" +
                (realtimeSinceStartup - _summaryWindowStartedAt).ToString("0.00") +
                ", AverageFps=" +
                averageFps.ToString("0.0") +
                ", AverageFrameMs=" +
                averageFrameMs.ToString("0.00") +
                ", WorstFrameMs=" +
                _summaryWorstFrameMs.ToString("0.00") +
                ", LongFrameCount=" +
                _summaryLongFrameCount +
                ", SlowStepCount=" +
                _summarySlowStepCount +
                ", SlowestStep=" +
                GetSlowestStepSummary() +
                ", LastEvent=" +
                _lastEvent +
                FormatDetail(detail) +
                ".");
            ResetSummary();
            _summaryWindowStartedAt = realtimeSinceStartup;
        }

        public void RecordStep(string stepName, long startedAtTimestamp, string detail)
        {
            if (!IsEnabled() || startedAtTimestamp == 0L)
            {
                return;
            }

            double elapsedMs = GetElapsedMilliseconds(startedAtTimestamp);
            if (elapsedMs < SlowStepThresholdMs)
            {
                return;
            }

            _summarySlowStepCount++;
            if (elapsedMs > _summarySlowestStepMs)
            {
                _summarySlowestStepMs = elapsedMs;
                _summarySlowestStepName = stepName ?? string.Empty;
                _summarySlowestStepDetail = detail ?? string.Empty;
            }

            LogInfo(
                "Slow Update step. Step=" +
                (stepName ?? string.Empty) +
                ", StepMs=" +
                elapsedMs.ToString("0.00") +
                ", ThresholdMs=" +
                SlowStepThresholdMs.ToString("0.00") +
                ", LastEvent=" +
                _lastEvent +
                FormatDetail(detail) +
                ".");
        }

        public void RecordOperation(string operationName, long startedAtTimestamp, string detail)
        {
            if (!IsEnabled() || startedAtTimestamp == 0L)
            {
                return;
            }

            LogInfo(
                "Operation timing. Operation=" +
                (operationName ?? string.Empty) +
                ", DurationMs=" +
                GetElapsedMilliseconds(startedAtTimestamp).ToString("0.00") +
                ", LastEvent=" +
                _lastEvent +
                FormatDetail(detail) +
                ".");
        }

        private string GetSlowestStepSummary()
        {
            if (string.IsNullOrEmpty(_summarySlowestStepName))
            {
                return "<none>";
            }

            return _summarySlowestStepName +
                   "@" +
                   _summarySlowestStepMs.ToString("0.00") +
                   "ms" +
                   FormatDetail(_summarySlowestStepDetail);
        }

        private void ResetSummary()
        {
            _summaryFrameCount = 0;
            _summaryTotalFrameMs = 0d;
            _summaryWorstFrameMs = 0d;
            _summaryLongFrameCount = 0;
            _summarySlowStepCount = 0;
            _summarySlowestStepName = string.Empty;
            _summarySlowestStepMs = 0d;
            _summarySlowestStepDetail = string.Empty;
        }

        private bool IsEnabled()
        {
            return _enabledProvider != null && _enabledProvider();
        }

        private static double GetElapsedMilliseconds(long startedAtTimestamp)
        {
            long elapsedTicks = Stopwatch.GetTimestamp() - startedAtTimestamp;
            return elapsedTicks * 1000d / Stopwatch.Frequency;
        }

        private static string FormatDetail(string detail)
        {
            return string.IsNullOrEmpty(detail) ? string.Empty : ", " + detail;
        }

        private void LogInfo(string message)
        {
            if (_logger != null && !string.IsNullOrEmpty(message))
            {
                _logger.LogInfo(EtgGameplayDashboardLog.Performance(message));
            }
        }

        private void LogWarning(string message)
        {
            if (_logger != null && !string.IsNullOrEmpty(message))
            {
                _logger.LogWarning(EtgGameplayDashboardLog.Performance(message));
            }
        }
    }
}
