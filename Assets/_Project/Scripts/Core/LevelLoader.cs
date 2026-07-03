using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Loads every level JSON from Resources/Levels once and serves them sorted by levelId.
    /// </summary>
    public class LevelLoader
    {
        public const string ResourcesFolder = "Levels";

        readonly List<LevelData> levels;

        public LevelLoader()
        {
            levels = Resources.LoadAll<TextAsset>(ResourcesFolder)
                .Select(asset => LevelData.FromJson(asset.text))
                .Where(level => level.pairs.Count > 0)
                .OrderBy(level => level.levelId)
                .ToList();

            if (levels.Count == 0)
                Debug.LogError($"No levels found in Resources/{ResourcesFolder}");
        }

        public int Count => levels.Count;

        public LevelData Get(int index) => levels[Mathf.Clamp(index, 0, levels.Count - 1)];
    }
}
