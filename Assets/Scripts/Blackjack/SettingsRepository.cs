using System.IO;
using UnityEngine;

namespace Blackjack
{
    /// <summary>
    /// Reads and writes <see cref="OptionsSettings"/> as a JSON file.
    /// In builds, tries the folder that contains the .exe first; if that folder is not
    /// writable (for example under Program Files), falls back to
    /// <see cref="Application.persistentDataPath"/>.
    /// In the Editor, the project root is used first.
    /// </summary>
    public static class SettingsRepository
    {
        private const string FileName = "options.json";

        private static string? _resolvedPath;

        /// <summary>Folder that contains the built .exe (or the project root in the Editor).</summary>
        public static string GameDirectoryPath =>
            Directory.GetParent(Application.dataPath)!.FullName;

        /// <summary>Primary settings path: <c>options.json</c> next to the .exe / project root.</summary>
        public static string GameDirectoryFilePath =>
            Path.Combine(GameDirectoryPath, FileName);

        /// <summary>Fallback path used when the game directory is read-only.</summary>
        public static string FallbackFilePath =>
            Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Path used for the most recent successful load or save.</summary>
        public static string FilePath => _resolvedPath ?? GameDirectoryFilePath;

        /// <summary>True when a settings file exists in either location.</summary>
        public static bool Exists() => FindExistingFilePath() != null;

        /// <summary>
        /// Loads settings from disk. Returns a default instance when no file exists yet.
        /// </summary>
        public static OptionsSettings Load()
        {
            string? path = FindExistingFilePath();
            if (path == null)
            {
                Debug.Log("[SettingsRepository] No settings file found. Using defaults.");
                return new OptionsSettings();
            }

            try
            {
                string json = File.ReadAllText(path);
                _resolvedPath = path;
                Debug.Log($"[SettingsRepository] Settings loaded from '{path}'.");
                return JsonUtility.FromJson<OptionsSettings>(json) ?? new OptionsSettings();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SettingsRepository] Failed to load settings from '{path}': {ex.Message}. Using defaults.");
                return new OptionsSettings();
            }
        }

        /// <summary>
        /// Saves the given settings to disk, overwriting any existing file.
        /// </summary>
        public static void Save(OptionsSettings settings)
        {
            string json = JsonUtility.ToJson(settings, prettyPrint: true);

            if (TryWrite(GameDirectoryFilePath, json))
            {
                _resolvedPath = GameDirectoryFilePath;
                Debug.Log($"[SettingsRepository] Settings saved to '{GameDirectoryFilePath}'.");
                return;
            }

            if (TryWrite(FallbackFilePath, json))
            {
                _resolvedPath = FallbackFilePath;
                Debug.LogWarning(
                    $"[SettingsRepository] Could not write next to the game executable. Settings saved to '{FallbackFilePath}'.");
                return;
            }

            Debug.LogError("[SettingsRepository] Failed to save settings — no writable location found.");
        }

        private static string? FindExistingFilePath()
        {
            if (File.Exists(GameDirectoryFilePath))
                return GameDirectoryFilePath;

            if (File.Exists(FallbackFilePath))
                return FallbackFilePath;

            return null;
        }

        private static bool TryWrite(string path, string json)
        {
            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(path, json);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SettingsRepository] Could not write to '{path}': {ex.Message}");
                return false;
            }
        }
    }
}
