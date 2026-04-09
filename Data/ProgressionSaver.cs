using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json;
using ValheimGuide.Data;

namespace ValheimGuide.Data
{
    public static class ProgressSaver
    {
        private static ManualLogSource _log;
        private static string _saveFolder;
        private static GuideProgress _current;

        public static GuideProgress Current => _current;

        public static void Initialise(ManualLogSource log)
        {
            _log = log;
            _saveFolder = Path.Combine(Paths.ConfigPath, "ValheimGuide", "progress");
            Directory.CreateDirectory(_saveFolder);
        }

        // Call when player spawns — pass Player.m_localPlayer.GetPlayerName()
        public static void Load(string characterName)
        {
            string path = GetPath(characterName);

            if (!File.Exists(path))
            {
                _current = new GuideProgress { CharacterName = characterName };
                _log.LogInfo($"[ProgressSaver] No save found for '{characterName}', starting fresh.");
                return;
            }

            try
            {
                string raw = File.ReadAllText(path);
                _current = JsonConvert.DeserializeObject<GuideProgress>(raw)
                           ?? new GuideProgress { CharacterName = characterName };
                _log.LogInfo($"[ProgressSaver] Loaded progress for '{characterName}'. " +
                             $"Checked items: {_current.CheckedItems.Count}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[ProgressSaver] Failed to load save for '{characterName}': {ex.Message}");
                _current = new GuideProgress { CharacterName = characterName };
            }
        }

        public static void Save()
        {
            if (_current == null) return;

            try
            {
                string json = JsonConvert.SerializeObject(_current, Formatting.Indented);
                File.WriteAllText(GetPath(_current.CharacterName), json);
            }
            catch (Exception ex)
            {
                _log.LogError($"[ProgressSaver] Failed to save progress: {ex.Message}");
            }
        }

        public static bool IsChecked(string itemId)
        {
            return _current?.CheckedItems.Contains(itemId) ?? false;
        }

        public static void SetChecked(string itemId, bool value)
        {
            if (_current == null) return;

            if (value && !_current.CheckedItems.Contains(itemId))
                _current.CheckedItems.Add(itemId);
            else if (!value)
                _current.CheckedItems.Remove(itemId);

            Save();
        }

        private static string GetPath(string characterName)
        {
            // Sanitise character name for use as filename
            foreach (char c in Path.GetInvalidFileNameChars())
                characterName = characterName.Replace(c, '_');

            return Path.Combine(_saveFolder, characterName + ".json");
        }
    }
}