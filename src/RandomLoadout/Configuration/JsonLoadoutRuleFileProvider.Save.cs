using System.Globalization;
using System.Text;

namespace RandomLoadout
{
    internal sealed partial class JsonLoadoutRuleFileProvider
    {
        private static string SerializePresetFile(LoadoutRuleFilePresetModel preset)
        {
            LoadoutRuleFilePresetModel safePreset = preset ?? new LoadoutRuleFilePresetModel();
            LoadoutRuleFileRuleModel[] rules = safePreset.Rules ?? new LoadoutRuleFileRuleModel[0];

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"id\": \"" + EscapeJsonString(StartItemsPresetNames.NormalizePresetId(safePreset.Id)) + "\",");
            if (!string.IsNullOrEmpty(safePreset.DisplayNameKey))
            {
                builder.AppendLine("  \"display_name_key\": \"" + EscapeJsonString(safePreset.DisplayNameKey) + "\",");
            }

            if (!string.IsNullOrEmpty(safePreset.Name))
            {
                builder.AppendLine("  \"name\": \"" + EscapeJsonString(safePreset.Name) + "\",");
            }

            builder.AppendLine("  \"rules\": [");
            for (int i = 0; i < rules.Length; i++)
            {
                AppendRule(builder, rules[i] ?? new LoadoutRuleFileRuleModel(), "    ");
                builder.AppendLine(i < rules.Length - 1 ? "," : string.Empty);
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendRule(StringBuilder builder, LoadoutRuleFileRuleModel rule, string indent)
        {
            builder.AppendLine(indent + "{");
            builder.AppendLine(indent + "  \"enabled\": " + (rule.Enabled ? "true" : "false") + ",");
            builder.AppendLine(indent + "  \"mode\": \"" + EscapeJsonString(rule.Mode) + "\",");
            builder.AppendLine(indent + "  \"category\": \"" + EscapeJsonString(rule.Category) + "\",");
            builder.AppendLine(indent + "  \"count\": " + rule.Count.ToString(CultureInfo.InvariantCulture) + ",");

            if (string.Equals(rule.Mode, "random", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(rule.Name))
                {
                    builder.AppendLine(indent + "  \"name\": \"" + EscapeJsonString(rule.Name) + "\",");
                }

                builder.AppendLine(indent + "  \"poolIds\": " + FormatIntArray(rule.PoolIds) + ",");
                builder.AppendLine(indent + "  \"poolAliases\": " + FormatStringArray(rule.PoolAliases) + ",");
                builder.AppendLine(indent + "  \"pool\": " + FormatStringArray(rule.Pool));
            }
            else if (rule.Id.HasValue)
            {
                builder.AppendLine(indent + "  \"id\": " + rule.Id.Value.ToString(CultureInfo.InvariantCulture));
            }
            else if (!string.IsNullOrEmpty(rule.Alias))
            {
                builder.AppendLine(indent + "  \"alias\": \"" + EscapeJsonString(rule.Alias) + "\"");
            }
            else if (!string.IsNullOrEmpty(rule.Name))
            {
                builder.AppendLine(indent + "  \"name\": \"" + EscapeJsonString(rule.Name) + "\"");
            }
            builder.Append(indent + "}");
        }

        private static string FormatIntArray(int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return "[]";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(values[i].ToString(CultureInfo.InvariantCulture));
            }

            builder.Append("]");
            return builder.ToString();
        }

        private static string FormatStringArray(string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return "[]";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("[");
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append("\"");
                builder.Append(EscapeJsonString(values[i]));
                builder.Append("\"");
            }

            builder.Append("]");
            return builder.ToString();
        }

        private static string EscapeJsonString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
