// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed class GameWindowFocusService
    {
        private const int ShowWindowRestore = 9;
        private const int AllowSetForegroundWindowAny = -1;

        private readonly ManualLogSource _logger;
        private readonly Func<bool> _verboseLoggingEnabledProvider;
        private readonly IntPtr _consoleWindowHandle;
        private IntPtr _lastObservedForegroundWindow;
        private bool _hasLoggedForegroundTarget;

        public GameWindowFocusService(ManualLogSource logger, Func<bool> verboseLoggingEnabledProvider)
        {
            _logger = logger;
            _verboseLoggingEnabledProvider = verboseLoggingEnabledProvider;
            _consoleWindowHandle = GetConsoleWindow();
        }

        public IEnumerator FocusWhenReady(float timeoutSeconds, float pollIntervalSeconds, float settleDelaySeconds)
        {
            float startedAt = Time.realtimeSinceStartup;
            LogVerbose(
                "Startup window focus helper is waiting for the ETG window. " +
                "TimeoutSeconds=" + timeoutSeconds.ToString("0.00") +
                ", PollIntervalSeconds=" + pollIntervalSeconds.ToString("0.00") +
                ", SettleDelaySeconds=" + settleDelaySeconds.ToString("0.00") + ".");

            CurrentProcessWindows windowSelection = null;
            while (Time.realtimeSinceStartup - startedAt <= timeoutSeconds)
            {
                windowSelection = FindCurrentProcessWindows();
                if (windowSelection != null && windowSelection.BestWindow != null)
                {
                    break;
                }

                yield return new WaitForSecondsRealtime(pollIntervalSeconds);
            }

            if (windowSelection == null || windowSelection.BestWindow == null)
            {
                LogVerbose("Startup window focus helper timed out before any ETG window was discovered.");
                yield break;
            }

            WindowCandidate targetWindow = windowSelection.BestWindow;
            LogVerbose("Startup window focus helper discovered current-process windows: " + DescribeWindowList(windowSelection.Candidates) + ".");
            LogVerbose("Startup window focus helper matched window: " + DescribeWindow(targetWindow) + ".");
            if (windowSelection.GameWindow != null && windowSelection.GameWindow.WindowHandle != targetWindow.WindowHandle)
            {
                LogVerbose("Startup window focus helper follow-up game window is available: " + DescribeWindow(windowSelection.GameWindow) + ".");
            }

            if (settleDelaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(settleDelaySeconds);
            }

            bool primaryFocusSucceeded = TryFocusWindow(targetWindow, "primary");
            if (primaryFocusSucceeded &&
                windowSelection.GameWindow != null &&
                windowSelection.GameWindow.WindowHandle != targetWindow.WindowHandle)
            {
                yield return new WaitForSecondsRealtime(0.15f);
                bool followUpFocusSucceeded = TryFocusWindow(windowSelection.GameWindow, "game_window_follow_up");
                if (followUpFocusSucceeded)
                {
                    LogVerbose("Startup window focus helper moved the ETG game window to the foreground after the console-window handoff.");
                    yield break;
                }

                LogVerbose("Startup window focus helper foregrounded the BepInEx console window, but the follow-up attempt to foreground the ETG game window did not succeed.");
                yield break;
            }

            if (primaryFocusSucceeded)
            {
                LogVerbose("Startup window focus helper moved the ETG window to the foreground.");
                yield break;
            }

            LogVerbose("Startup window focus helper found the ETG window but could not move it to the foreground.");
        }

        public IEnumerator LogForegroundWindowChanges(float durationSeconds, float pollIntervalSeconds, string reason)
        {
            float startedAt = Time.realtimeSinceStartup;
            LogVerbose(
                "Foreground window monitor started. " +
                "DurationSeconds=" + durationSeconds.ToString("0.00") +
                ", PollIntervalSeconds=" + pollIntervalSeconds.ToString("0.00") +
                ", Reason=" + reason +
                ".");

            while (Time.realtimeSinceStartup - startedAt <= durationSeconds)
            {
                LogCurrentForegroundWindow(reason);
                yield return new WaitForSecondsRealtime(pollIntervalSeconds);
            }

            LogVerbose("Foreground window monitor finished. Reason=" + reason + ".");
        }

        private CurrentProcessWindows FindCurrentProcessWindows()
        {
            int currentProcessId = Process.GetCurrentProcess().Id;
            List<WindowCandidate> candidates = new List<WindowCandidate>();
            EnumWindows(
                delegate(IntPtr windowHandle, IntPtr parameter)
                {
                    if (!IsWindowVisible(windowHandle))
                    {
                        return true;
                    }

                    int processId;
                    GetWindowThreadProcessId(windowHandle, out processId);
                    if (processId != currentProcessId)
                    {
                        return true;
                    }

                    string title = GetWindowTitle(windowHandle);
                    candidates.Add(new WindowCandidate(windowHandle, processId, title));
                    return true;
                },
                IntPtr.Zero);

            WindowCandidate consoleWindow = null;
            WindowCandidate gameWindow = null;
            WindowCandidate fallbackWindow = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                WindowCandidate candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.WindowHandle == _consoleWindowHandle)
                {
                    consoleWindow = candidate;
                    continue;
                }

                if (!string.IsNullOrEmpty(candidate.Title) &&
                    candidate.Title.IndexOf("BepInEx", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (consoleWindow == null)
                    {
                        consoleWindow = candidate;
                    }
                    continue;
                }

                if (!string.IsNullOrEmpty(candidate.Title) &&
                    candidate.Title.IndexOf("Enter the Gungeon", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (gameWindow == null)
                    {
                        gameWindow = candidate;
                    }

                    continue;
                }

                if (fallbackWindow == null)
                {
                    fallbackWindow = candidate;
                }
            }

            return new CurrentProcessWindows(
                candidates,
                consoleWindow,
                gameWindow,
                fallbackWindow);
        }

        private bool TryFocusWindow(WindowCandidate targetWindow, string stage)
        {
            if (targetWindow == null || targetWindow.WindowHandle == IntPtr.Zero)
            {
                return false;
            }

            bool allowSetForegroundWindowSucceeded = AllowSetForegroundWindow(AllowSetForegroundWindowAny);
            LogVerbose(
                "Startup window focus attempt is beginning. " +
                "Stage=" + stage +
                ", Window=" + DescribeWindow(targetWindow) +
                ", AllowSetForegroundWindow=" + allowSetForegroundWindowSucceeded +
                ".");

            if (IsIconic(targetWindow.WindowHandle))
            {
                LogVerbose("Startup window focus attempt is restoring a minimized window. Stage=" + stage + ".");
                ShowWindowAsync(targetWindow.WindowHandle, ShowWindowRestore);
                Thread.Sleep(150);
            }

            IntPtr foregroundWindow = GetForegroundWindow();
            LogVerbose(
                "Foreground window before focus attempt. " +
                "Stage=" + stage +
                ", Window=" + DescribeWindow(CreateWindowCandidate(foregroundWindow)) +
                ".");
            int ignoredForegroundProcessId;
            int ignoredTargetProcessId;
            int foregroundThreadId = foregroundWindow != IntPtr.Zero
                ? GetWindowThreadProcessId(foregroundWindow, out ignoredForegroundProcessId)
                : 0;
            int targetThreadId = GetWindowThreadProcessId(targetWindow.WindowHandle, out ignoredTargetProcessId);
            int currentThreadId = GetCurrentThreadId();
            bool attachedToForeground = false;
            bool attachedToTarget = false;

            try
            {
                if (foregroundThreadId != 0)
                {
                    attachedToForeground = AttachThreadInput(currentThreadId, foregroundThreadId, true);
                    LogVerbose(
                        "AttachThreadInput completed. " +
                        "Stage=" + stage +
                        ", Direction=current->foreground" +
                        ", CurrentThreadId=" + currentThreadId +
                        ", ForegroundThreadId=" + foregroundThreadId +
                        ", Attached=" + attachedToForeground +
                        ".");
                }

                if (targetThreadId != 0)
                {
                    attachedToTarget = AttachThreadInput(currentThreadId, targetThreadId, true);
                    LogVerbose(
                        "AttachThreadInput completed. " +
                        "Stage=" + stage +
                        ", Direction=current->target" +
                        ", CurrentThreadId=" + currentThreadId +
                        ", TargetThreadId=" + targetThreadId +
                        ", Attached=" + attachedToTarget +
                        ".");
                }

                bool bringWindowToTopSucceeded = BringWindowToTop(targetWindow.WindowHandle);
                bool showWindowSucceeded = ShowWindowAsync(targetWindow.WindowHandle, ShowWindowRestore);
                bool setActiveWindowSucceeded = SetActiveWindow(targetWindow.WindowHandle) != IntPtr.Zero;
                bool setFocusSucceeded = SetFocus(targetWindow.WindowHandle) != IntPtr.Zero;
                LogVerbose(
                    "Pre-foreground calls finished. " +
                    "Stage=" + stage +
                    ", BringWindowToTop=" + bringWindowToTopSucceeded +
                    ", ShowWindowAsync=" + showWindowSucceeded +
                    ", SetActiveWindow=" + setActiveWindowSucceeded +
                    ", SetFocus=" + setFocusSucceeded +
                    ".");

                Marshal.GetLastWin32Error();
                bool setForegroundSucceeded = SetForegroundWindow(targetWindow.WindowHandle);
                int setForegroundLastError = Marshal.GetLastWin32Error();
                Thread.Sleep(150);
                bool isForeground = GetForegroundWindow() == targetWindow.WindowHandle;

                LogVerbose(
                    "Startup window focus attempt finished. " +
                    "Stage=" + stage +
                    ", " +
                    "Window=" + DescribeWindow(targetWindow) +
                    ", SetForegroundWindow=" + setForegroundSucceeded +
                    ", LastError=" + setForegroundLastError +
                    ", IsForeground=" + isForeground + ".");
                LogVerbose(
                    "Foreground window after focus attempt. " +
                    "Stage=" + stage +
                    ", Window=" + DescribeWindow(CreateWindowCandidate(GetForegroundWindow())) +
                    ".");

                return isForeground;
            }
            finally
            {
                if (attachedToTarget && targetThreadId != 0)
                {
                    AttachThreadInput(currentThreadId, targetThreadId, false);
                    LogVerbose(
                        "AttachThreadInput released. " +
                        "Stage=" + stage +
                        ", Direction=current->target" +
                        ", CurrentThreadId=" + currentThreadId +
                        ", TargetThreadId=" + targetThreadId +
                        ".");
                }

                if (attachedToForeground && foregroundThreadId != 0)
                {
                    AttachThreadInput(currentThreadId, foregroundThreadId, false);
                    LogVerbose(
                        "AttachThreadInput released. " +
                        "Stage=" + stage +
                        ", Direction=current->foreground" +
                        ", CurrentThreadId=" + currentThreadId +
                        ", ForegroundThreadId=" + foregroundThreadId +
                        ".");
                }
            }
        }

        private static string GetWindowTitle(IntPtr windowHandle)
        {
            int length = GetWindowTextLength(windowHandle);
            if (length <= 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(length + 1);
            GetWindowText(windowHandle, builder, builder.Capacity);
            return builder.ToString();
        }

        private static string DescribeWindow(WindowCandidate candidate)
        {
            return candidate == null
                ? "<none>"
                : "Hwnd=" + candidate.WindowHandle +
                  ", ProcessId=" + candidate.ProcessId +
                  ", Title=\"" + (candidate.Title ?? string.Empty) + "\"";
        }

        private static string DescribeWindowList(IList<WindowCandidate> candidates)
        {
            if (candidates == null || candidates.Count <= 0)
            {
                return "<none>";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append("#");
                builder.Append(i);
                builder.Append(": ");
                builder.Append(DescribeWindow(candidates[i]));
            }

            return builder.ToString();
        }

        private void LogCurrentForegroundWindow(string reason)
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return;
            }

            if (foregroundWindow == _lastObservedForegroundWindow)
            {
                return;
            }

            _lastObservedForegroundWindow = foregroundWindow;
            WindowCandidate foregroundCandidate = CreateWindowCandidate(foregroundWindow);
            if (foregroundCandidate == null)
            {
                return;
            }

            bool isCurrentProcessWindow = foregroundCandidate.ProcessId == Process.GetCurrentProcess().Id;
            if (!isCurrentProcessWindow && _hasLoggedForegroundTarget)
            {
                LogVerbose(
                    "Foreground window changed away from ETG. " +
                    "Reason=" + reason +
                    ", Window=" + DescribeWindow(foregroundCandidate) +
                    ".");
                _hasLoggedForegroundTarget = false;
                return;
            }

            if (!isCurrentProcessWindow)
            {
                LogVerbose(
                    "Foreground window observed. " +
                    "Reason=" + reason +
                    ", Window=" + DescribeWindow(foregroundCandidate) +
                    ".");
                return;
            }

            _hasLoggedForegroundTarget = true;
            LogVerbose(
                "ETG window reached foreground. " +
                "Reason=" + reason +
                ", Window=" + DescribeWindow(foregroundCandidate) +
                ".");
        }

        private static WindowCandidate CreateWindowCandidate(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }

            int processId;
            GetWindowThreadProcessId(windowHandle, out processId);
            return new WindowCandidate(windowHandle, processId, GetWindowTitle(windowHandle));
        }

        private void LogInfo(string message)
        {
            if (_logger != null && !string.IsNullOrEmpty(message))
            {
                _logger.LogInfo(RandomLoadoutLog.Init(message));
            }
        }

        private void LogVerbose(string message)
        {
            if (!IsVerboseLoggingEnabled())
            {
                return;
            }

            if (_logger != null && !string.IsNullOrEmpty(message))
            {
                _logger.LogInfo(RandomLoadoutLog.Init(message));
            }
        }

        private bool IsVerboseLoggingEnabled()
        {
            return _verboseLoggingEnabledProvider != null && _verboseLoggingEnabledProvider();
        }

        private sealed class WindowCandidate
        {
            public WindowCandidate(IntPtr windowHandle, int processId, string title)
            {
                WindowHandle = windowHandle;
                ProcessId = processId;
                Title = title ?? string.Empty;
            }

            public IntPtr WindowHandle { get; private set; }

            public int ProcessId { get; private set; }

            public string Title { get; private set; }
        }

        private sealed class CurrentProcessWindows
        {
            public CurrentProcessWindows(
                IList<WindowCandidate> candidates,
                WindowCandidate consoleWindow,
                WindowCandidate gameWindow,
                WindowCandidate fallbackWindow)
            {
                Candidates = candidates;
                ConsoleWindow = consoleWindow;
                GameWindow = gameWindow;
                FallbackWindow = fallbackWindow;
            }

            public IList<WindowCandidate> Candidates { get; private set; }

            public WindowCandidate ConsoleWindow { get; private set; }

            public WindowCandidate GameWindow { get; private set; }

            public WindowCandidate FallbackWindow { get; private set; }

            public WindowCandidate BestWindow
            {
                get
                {
                    if (ConsoleWindow != null)
                    {
                        return ConsoleWindow;
                    }

                    if (GameWindow != null)
                    {
                        return GameWindow;
                    }

                    return FallbackWindow;
                }
            }
        }

        private delegate bool EnumWindowsCallback(IntPtr windowHandle, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr windowHandle);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr windowHandle, StringBuilder text, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr windowHandle);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr windowHandle, out int processId);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr windowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindowAsync(IntPtr windowHandle, int command);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr windowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool BringWindowToTop(IntPtr windowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetActiveWindow(IntPtr windowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetFocus(IntPtr windowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AllowSetForegroundWindow(int processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AttachThreadInput(int idAttach, int idAttachTo, bool attach);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
    }
}
