using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Loads every LevelAsset from Resources/Levels once and serves the wrapped
    /// LevelData sorted by levelId.
    /// </summary>
    public class LevelLoader
    {
        public const string ResourcesFolder = "Levels";

        readonly List<LevelData> levels;

        public LevelLoader()
        {
            levels = Resources.LoadAll<LevelAsset>(ResourcesFolder)
                .Select(asset => asset.data)
                .Where(level => level != null && level.groups.Count > 0)
                .OrderBy(level => level.levelId)
                .ToList();

            if (levels.Count == 0)
                Debug.LogError($"No levels found in Resources/{ResourcesFolder}");
        }

        public int Count => levels.Count;

        public LevelData Get(int index) => levels[Mathf.Clamp(index, 0, levels.Count - 1)];
    }
}
