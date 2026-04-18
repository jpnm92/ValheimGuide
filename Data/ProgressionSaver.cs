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
        public const int MaxPins = 5;

        private static ManualLogSource _log;
        private static string _saveFolder;
        private static GuideProgress _current;
        private static bool _isDirty;
        private static volatile int _saveGeneration;
        private static readonly object _fileLock = new object();

        public static GuideProgress Current => _current;
        public static IReadOnlyList<string> PinnedRecipeIds
            => (IReadOnlyList<string>)_current?.PinnedRecipes
               ?? System.Array.Empty<string>();
        public static void Initialise(ManualLogSource log)
        {
            _log = log;
            _saveFolder = Path.Combine(Paths.ConfigPath, "ValheimGuide", "progress");
            Directory.CreateDirectory(_saveFolder);
        }
        
        public static void Load(string characterName)
        {
            string path = GetPath(characterName);

            if (!File.Exists(path))
            {
                _current = new GuideProgress { CharacterName = characterName };
                _isDirty = false;
                _log.LogInfo($"[ProgressSaver] No save found for '{characterName}', starting fresh.");
                return;
            }

            try
            {
                string raw = File.ReadAllText(path);
                _current = JsonConvert.DeserializeObject<GuideProgress>(raw)
                           ?? new GuideProgress { CharacterName = characterName };
                _isDirty = false;
                _log.LogInfo($"[ProgressSaver] Loaded progress for '{characterName}'. " +
                             $"Checked items: {_current.CheckedItems.Count}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[ProgressSaver] Failed to load save for '{characterName}': {ex.Message}");
                _current = new GuideProgress { CharacterName = characterName };
            }
        }

        /// <summary>Synchronous save — use on application quit or scene unload.</summary>
        public static void Save()
        {
            if (_current == null || !_isDirty) return;

            try
            {
                File.WriteAllText(GetPath(_current.CharacterName),
                    JsonConvert.SerializeObject(_current, Formatting.Indented));
                _isDirty = false;
            }
            catch (Exception ex)
            {
                _log.LogError($"[ProgressSaver] Failed to save progress: {ex.Message}");
            }
        }

        private static void SaveAsync()
        {
            if (_current == null || !_isDirty) return;

            int generation = ++_saveGeneration;
            string json = JsonConvert.SerializeObject(_current, Formatting.Indented);
            string path = GetPath(_current.CharacterName);

            // Wrap the file write in a lock inside the background thread
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    lock (_fileLock)
                    {
                        File.WriteAllText(path, json);
                    }

                    if (_saveGeneration == generation)
                        _isDirty = false;
                }
                catch (Exception ex)
                {
                    _log.LogError($"[ProgressSaver] Async save failed: {ex.Message}");
                }
            });
        }

        public static bool IsChecked(string itemId)
            => _current?.CheckedItems.Contains(itemId) ?? false;

        public static void SetChecked(string itemId, bool value)
        {
            if (_current == null) return;

            if (value && !_current.CheckedItems.Contains(itemId))
                _current.CheckedItems.Add(itemId);
            else if (!value)
                _current.CheckedItems.Remove(itemId);

            _isDirty = true;
            SaveAsync(); // non-blocking
        }

        private static string GetPath(string characterName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                characterName = characterName.Replace(c, '_');
            return Path.Combine(_saveFolder, characterName + ".json");
        }

        public static void SetSpoilersPreference(bool showFuture)
        {
            if (_current == null) return;
            _current.ShowFutureStages = showFuture;
            _isDirty = true;
            SaveAsync();
        }

        public static void SetPlaystylePreference(string playstyleId)
        {
            if (_current == null) return;
            _current.PlaystyleId = playstyleId;
            _isDirty = true;
            SaveAsync();
        }

        public static void SetLastStage(string stageId, string viewMode)
        {
            if (_current == null) return;
            _current.LastStageId = stageId;
            _current.LastViewMode = viewMode;
            _isDirty = true;
            SaveAsync();
        }
        public static bool SetPinned(string itemId, bool pinned)
        {
            if (_current == null) return true; // no save loaded — silently ignore

            if (pinned)
            {
                if (_current.PinnedRecipes.Contains(itemId)) return true;  // already pinned
                if (_current.PinnedRecipes.Count >= MaxPins) return false; // cap reached
                _current.PinnedRecipes.Add(itemId);
            }
            else
            {
                if (!_current.PinnedRecipes.Remove(itemId)) return true;   // wasn't pinned
            }

            _isDirty = true;
            SaveAsync();
            return true;
        }

        public static bool IsPinned(string itemId)
            => _current?.PinnedRecipes.Contains(itemId) ?? false;
    }
}