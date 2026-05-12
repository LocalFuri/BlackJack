using System.IO;
using UnityEngine;

namespace Blackjack
{
    /// <summary>
    /// Reads and writes <see cref="OptionsSettings"/> as a JSON file
    /// stored in <see cref="Application.persistentDataPath"/>.
    /// The file survives builds and editor restarts.
    /// </summary>
    public static class SettingsRepository
    {
        private const string FileName = "options.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>
        /// Loads settings from disk. Returns a default instance when no file exists yet.
        /// </summary>
        public static OptionsSettings Load()
        {
            if (!File.Exists(FilePath))
            {
                Debug.Log($"[SettingsRepository] No settings file found at '{FilePath}'. Using defaults.");
                return new OptionsSettings();
            }

            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonUtility.FromJson<OptionsSettings>(json) ?? new OptionsSettings();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SettingsRepository] Failed to load settings: {ex.Message}. Using defaults.");
                return new OptionsSettings();
            }
        }

        /// <summary>
        /// Saves the given settings to disk, overwriting any existing file.
        /// </summary>
        public static void Save(OptionsSettings settings)
        {
            try
            {
                string json = JsonUtility.ToJson(settings, prettyPrint: true);
                File.WriteAllText(FilePath, json);
                Debug.Log($"[SettingsRepository] Settings saved to '{FilePath}'.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SettingsRepository] Failed to save settings: {ex.Message}");
            }
        }
    }
}
