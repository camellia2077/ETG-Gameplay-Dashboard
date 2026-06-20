using System;

namespace RandomLoadout
{
    internal sealed class AmmonomiconAnimationToggleService
    {
        public static bool IsOpenAnimationEnabled { get; private set; }

        static AmmonomiconAnimationToggleService()
        {
            Reset();
        }

        public GrantCommandExecutionResult Toggle()
        {
            IsOpenAnimationEnabled = !IsOpenAnimationEnabled;
            if (IsOpenAnimationEnabled)
            {
                return LocalizedWithFallback(
                    "result.ammonomicon_animation.enable.success",
                    "Ammonomicon animation enabled.",
                    "\u5df2\u5f00\u542f\u67aa\u68b0\u767e\u79d1\u52a8\u753b\u3002");
            }

            return LocalizedWithFallback(
                "result.ammonomicon_animation.disable.success",
                "Ammonomicon animation disabled.",
                "\u5df2\u5173\u95ed\u67aa\u68b0\u767e\u79d1\u52a8\u753b\u3002");
        }

        public static void Reset()
        {
            IsOpenAnimationEnabled = true;
        }

        private static GrantCommandExecutionResult LocalizedWithFallback(string key, string englishFallback, string simplifiedChineseFallback)
        {
            string message = GuiText.Get(key);
            if (string.Equals(message, key, StringComparison.Ordinal))
            {
                message = string.Equals(GuiText.CurrentLanguageCode, "zh-CN", StringComparison.OrdinalIgnoreCase)
                    ? simplifiedChineseFallback
                    : englishFallback;
            }

            string logMessage = GuiText.GetEnglish(key);
            if (string.Equals(logMessage, key, StringComparison.Ordinal))
            {
                logMessage = englishFallback;
            }

            return new GrantCommandExecutionResult(true, message, logMessage);
        }
    }
}
