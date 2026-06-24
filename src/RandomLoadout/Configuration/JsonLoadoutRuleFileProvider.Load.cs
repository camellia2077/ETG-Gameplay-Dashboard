using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed partial class JsonLoadoutRuleFileProvider
    {
        private readonly string _filePath;
        private readonly string _presetsDirectoryPath;
        private string _activePresetName = DefaultPresetId;

        public JsonLoadoutRuleFileProvider(string filePath)
            : this(filePath, DashboardFileLayout.GetPresetsDirectoryPath(Path.GetDirectoryName(filePath) ?? string.Empty))
        {
        }

        public JsonLoadoutRuleFileProvider(string filePath, string presetsDirectoryPath)
        {
            _filePath = filePath ?? string.Empty;
            _presetsDirectoryPath = presetsDirectoryPath ?? string.Empty;
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        public string ActivePresetName
        {
            get { return NormalizePresetId(_activePresetName); }
            set { _activePresetName = NormalizePresetId(value); }
        }

        public LoadoutRuleFileModel LoadEditableModel()
        {
            return new LoadoutRuleFileModel
            {
                Presets = LoadPresetFiles(null, null),
            };
        }

        public void SaveEditableModel(LoadoutRuleFileModel fileModel)
        {
            EnsurePresetsDirectoryExists();

            LoadoutRuleFilePresetModel[] presets = fileModel != null && fileModel.Presets != null
                ? fileModel.Presets
                : new LoadoutRuleFilePresetModel[0];
            HashSet<string> expectedPresetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < presets.Length; i++)
            {
                LoadoutRuleFilePresetModel preset = presets[i] ?? new LoadoutRuleFilePresetModel();
                string targetPath = ResolvePresetFilePath(preset, i + 1);
                preset.SourcePath = targetPath;
                File.WriteAllText(targetPath, SerializePresetFile(preset), Encoding.UTF8);
                expectedPresetPaths.Add(Path.GetFullPath(targetPath));
            }

            string[] existingPresetPaths = Directory.Exists(_presetsDirectoryPath)
                ? Directory.GetFiles(_presetsDirectoryPath, "*.json", SearchOption.TopDirectoryOnly)
                : new string[0];
            for (int i = 0; i < existingPresetPaths.Length; i++)
            {
                string existingPresetPath = Path.GetFullPath(existingPresetPaths[i]);
                if (!expectedPresetPaths.Contains(existingPresetPath))
                {
                    File.Delete(existingPresetPath);
                }
            }
        }

        public LoadoutRuleFileLoadResult Load()
        {
            List<string> messages = new List<string>();
            List<string> warnings = new List<string>();
            LoadoutRuleFileModel fileModel = new LoadoutRuleFileModel
            {
                Presets = LoadPresetFiles(messages, warnings),
            };

            if (fileModel.Presets == null || fileModel.Presets.Length == 0)
            {
                warnings.Add("No Start Items preset files were found in '" + _presetsDirectoryPath + "'.");
            }

            LoadoutRuleFilePresetModel activePreset = GetActivePreset(fileModel);
            if (activePreset == null)
            {
                warnings.Add("Active start-items preset id '" + ActivePresetName + "' was not found.");
            }
            else
            {
                messages.Add(
                    "Using start-items preset '" +
                    StartItemsPresetNames.GetEnglishDisplayName(activePreset) +
                    "' [Id=" +
                    StartItemsPresetNames.NormalizePresetId(activePreset.Id) +
                    "].");
            }

            return new LoadoutRuleFileLoadResult(ConvertToDefinitions(fileModel, messages), messages.ToArray(), warnings.ToArray());
        }

        private LoadoutRuleFilePresetModel[] LoadPresetFiles(List<string> messages, List<string> warnings)
        {
            if (string.IsNullOrEmpty(_presetsDirectoryPath) || !Directory.Exists(_presetsDirectoryPath))
            {
                if (warnings != null)
                {
                    warnings.Add("Preset directory was not found at '" + _presetsDirectoryPath + "'.");
                }

                return new LoadoutRuleFilePresetModel[0];
            }

            string[] presetPaths = Directory.GetFiles(_presetsDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);
            Array.Sort(presetPaths, StringComparer.OrdinalIgnoreCase);

            List<LoadoutRuleFilePresetModel> presets = new List<LoadoutRuleFilePresetModel>();
            for (int i = 0; i < presetPaths.Length; i++)
            {
                string presetPath = presetPaths[i];
                try
                {
                    string presetJson = Json5TextNormalizer.Normalize(File.ReadAllText(presetPath, Encoding.UTF8));
                    LoadoutRuleFilePresetModel preset = ParsePresetFile(presetJson, presetPath, i + 1);
                    if (preset == null)
                    {
                        if (warnings != null)
                        {
                            warnings.Add("Preset file did not contain a valid preset: '" + presetPath + "'.");
                        }

                        continue;
                    }

                    presets.Add(preset);
                    if (messages != null)
                    {
                        messages.Add(
                            "Loaded preset '" +
                            StartItemsPresetNames.GetEnglishDisplayName(preset) +
                            "' [Id=" +
                            preset.Id +
                            "] from '" +
                            presetPath +
                            "'.");
                    }
                }
                catch (Exception exception)
                {
                    if (warnings != null)
                    {
                        warnings.Add("Failed to parse preset file '" + presetPath + "'. " + exception.Message);
                    }
                }
            }

            return presets.ToArray();
        }

        private LoadoutRuleFilePresetModel ParsePresetFile(string rawJson, string presetPath, int fallbackIndex)
        {
            if (string.IsNullOrEmpty(rawJson) || string.IsNullOrEmpty(rawJson.Trim()))
            {
                return null;
            }

            string id = ParseString(rawJson, "id");
            string displayNameKey = ParseString(rawJson, "display_name_key");
            string name = ParseString(rawJson, "name");
            string rulesArrayBody = ExtractPropertyArrayBody(rawJson, "rules");
            if ((string.IsNullOrEmpty(id) && string.IsNullOrEmpty(name)) ||
                (string.IsNullOrEmpty(rulesArrayBody) && !RegexContainsRulesArray(rawJson)))
            {
                return null;
            }

            return new LoadoutRuleFilePresetModel
            {
                Id = StartItemsPresetNames.CreatePresetId(id, name, fallbackIndex),
                DisplayNameKey = StartItemsPresetNames.NormalizePresetName(displayNameKey),
                Name = StartItemsPresetNames.NormalizePresetName(name),
                SourcePath = presetPath ?? string.Empty,
                Rules = ParseRulesFromArrayBody(rulesArrayBody),
            };
        }

        private void EnsurePresetsDirectoryExists()
        {
            if (!string.IsNullOrEmpty(_presetsDirectoryPath) && !Directory.Exists(_presetsDirectoryPath))
            {
                Directory.CreateDirectory(_presetsDirectoryPath);
            }
        }

        private string ResolvePresetFilePath(LoadoutRuleFilePresetModel preset, int fallbackIndex)
        {
            if (preset != null && !string.IsNullOrEmpty(preset.SourcePath))
            {
                return preset.SourcePath;
            }

            string presetId = StartItemsPresetNames.CreatePresetId(
                preset != null ? preset.Id : string.Empty,
                preset != null ? preset.Name : string.Empty,
                fallbackIndex);
            string fileName = BuildPresetFileName(presetId);
            return Path.Combine(_presetsDirectoryPath, fileName);
        }

        private static string BuildPresetFileName(string presetId)
        {
            string normalizedId = StartItemsPresetNames.NormalizePresetId(presetId);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < normalizedId.Length; i++)
            {
                char current = normalizedId[i];
                if ((current >= 'a' && current <= 'z') ||
                    (current >= 'A' && current <= 'Z') ||
                    (current >= '0' && current <= '9') ||
                    current == '-' ||
                    current == '_' ||
                    current == '.')
                {
                    builder.Append(char.ToLowerInvariant(current));
                }
                else
                {
                    builder.Append("u");
                    builder.Append(((int)current).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            string safeId = builder.ToString().Trim('-');
            if (string.IsNullOrEmpty(safeId))
            {
                safeId = "preset";
            }

            return "preset." + safeId + ".json";
        }

        private static bool RegexContainsRulesArray(string rawJson)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(
                rawJson ?? string.Empty,
                GetPropertyPrefixPattern("rules") + "\\[",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private const string DefaultPresetId = StartItemsPresetNames.DefaultPresetId;

        private static string NormalizePresetId(string presetId)
        {
            return StartItemsPresetNames.NormalizePresetId(presetId);
        }
    }
}
