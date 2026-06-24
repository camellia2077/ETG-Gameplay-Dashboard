using System;

namespace RandomLoadout
{
    internal static class SceneNameNormalizer
    {
        private static readonly string RuntimeScenePrefix = new string(new[] { 't', 't', '_' });
        private static readonly string RuntimeSceneRoot = RuntimeScenePrefix.Substring(0, 2);

        public static string Normalize(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return string.Empty;
            }

            string normalizedSceneName = sceneName.Trim();
            if (string.Equals(normalizedSceneName, RuntimeSceneRoot + "5", StringComparison.Ordinal))
            {
                return "proper";
            }

            if (normalizedSceneName.StartsWith(RuntimeScenePrefix, StringComparison.Ordinal))
            {
                return normalizedSceneName.Substring(RuntimeScenePrefix.Length);
            }

            return normalizedSceneName;
        }
    }
}
