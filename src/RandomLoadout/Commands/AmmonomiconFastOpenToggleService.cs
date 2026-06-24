using System;

namespace RandomLoadout
{
    internal sealed class AmmonomiconFastOpenToggleService
    {
        public static bool IsFastOpenEnabled { get; private set; }

        static AmmonomiconFastOpenToggleService()
        {
            IsFastOpenEnabled = false;
        }

        public GrantCommandExecutionResult Toggle()
        {
            return SetIsFastOpenEnabled(!IsFastOpenEnabled);
        }

        public GrantCommandExecutionResult SetIsFastOpenEnabled(bool isEnabled)
        {
            IsFastOpenEnabled = isEnabled;
            if (IsFastOpenEnabled)
            {
                return LocalizedWithFallback(
                    "result.ammonomicon_fast_open.enable.success",
                    "Ammonomicon fast open enabled.",
                    "\u5df2\u5f00\u542f\u67aa\u68b0\u767e\u79d1\u5feb\u901f\u6253\u5f00\u3002");
            }

            return LocalizedWithFallback(
                "result.ammonomicon_fast_open.disable.success",
                "Ammonomicon fast open disabled.",
                "\u5df2\u5173\u95ed\u67aa\u68b0\u767e\u79d1\u5feb\u901f\u6253\u5f00\u3002");
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
