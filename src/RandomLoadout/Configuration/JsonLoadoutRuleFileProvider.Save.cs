using System.Globalization;
using System.Text;

namespace RandomLoadout
{
    internal sealed partial class JsonLoadoutRuleFileProvider
    {
        private static string SerializeRuleFile(LoadoutRuleFileModel fileModel)
        {
            StringBuilder builder = new StringBuilder();
            LoadoutRuleFilePresetModel[] presets = fileModel != null && fileModel.Presets != null && fileModel.Presets.Length > 0
                ? fileModel.Presets
                : new[]
                {
                    new LoadoutRuleFilePresetModel
                    {
                        Name = DefaultPresetName,
                        Rules = fileModel != null && fileModel.Rules != null ? fileModel.Rules : new LoadoutRuleFileRuleModel[0],
                    },
                };

            builder.AppendLine("{");
            builder.AppendLine("  presets: [");
            for (int i = 0; i < presets.Length; i++)
            {
                AppendPreset(builder, presets[i] ?? new LoadoutRuleFilePresetModel());
                builder.AppendLine(i < presets.Length - 1 ? "," : string.Empty);
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendPreset(StringBuilder builder, LoadoutRuleFilePresetModel preset)
        {
            LoadoutRuleFileRuleModel[] rules = preset != null && preset.Rules != null
                ? preset.Rules
                : new LoadoutRuleFileRuleModel[0];

            builder.AppendLine("    {");
            builder.AppendLine("      name: \"" + EscapeJsonString(NormalizePresetName(preset != null ? preset.Name : string.Empty)) + "\",");
            builder.AppendLine("      rules: [");
            for (int i = 0; i < rules.Length; i++)
            {
                AppendRule(builder, rules[i] ?? new LoadoutRuleFileRuleModel(), "        ");
                builder.AppendLine(i < rules.Length - 1 ? "," : string.Empty);
            }

            builder.AppendLine("      ]");
            builder.Append("    }");
        }

        private static void AppendRule(StringBuilder builder, LoadoutRuleFileRuleModel rule, string indent)
        {
            builder.AppendLine(indent + "{");
            builder.AppendLine(indent + "  enabled: " + (rule.Enabled ? "true" : "false") + ",");
            builder.AppendLine(indent + "  mode: \"" + EscapeJsonString(rule.Mode) + "\",");
            builder.AppendLine(indent + "  category: \"" + EscapeJsonString(rule.Category) + "\",");
            builder.AppendLine(indent + "  count: " + rule.Count.ToString(CultureInfo.InvariantCulture) + ",");

            if (rule.Id.HasValue)
            {
                builder.AppendLine(indent + "  id: " + rule.Id.Value.ToString(CultureInfo.InvariantCulture));
            }
            else if (!string.IsNullOrEmpty(rule.Alias))
            {
                builder.AppendLine(indent + "  alias: \"" + EscapeJsonString(rule.Alias) + "\"");
            }
            else if (!string.IsNullOrEmpty(rule.Name))
            {
                builder.AppendLine(indent + "  name: \"" + EscapeJsonString(rule.Name) + "\"");
            }
            else
            {
                builder.AppendLine(indent + "  poolIds: " + FormatIntArray(rule.PoolIds) + ",");
                builder.AppendLine(indent + "  poolAliases: " + FormatStringArray(rule.PoolAliases) + ",");
                builder.AppendLine(indent + "  pool: " + FormatStringArray(rule.Pool));
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
