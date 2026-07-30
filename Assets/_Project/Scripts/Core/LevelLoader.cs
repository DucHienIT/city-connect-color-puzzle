using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Serves LevelData sorted by levelId. Sources, in priority order: assets injected
    /// by the scene bootstrap (survives Luna export, which only bundles scene
    /// dependencies), then Resources/Levels, then — so a playable-ad build can never
    /// boot into nothing — deterministic runtime generation.
    /// </summary>
    public class LevelLoader
    {
        public const string ResourcesFolder = "Levels";

        /// <summary>Set by GameBootstrap before GameManager is created (scene-serialized).</summary>
        public static LevelAsset[] InjectedLevels;

        readonly List<LevelData> levels;

        public LevelLoader()
        {
            IEnumerable<LevelAsset> source = InjectedLevels != null && InjectedLevels.Length > 0
                ? (IEnumerable<LevelAsset>)InjectedLevels
                : Resources.LoadAll<LevelAsset>(ResourcesFolder);

            levels = source
                .Where(asset => asset != null)
                .Select(asset => asset.data)
                .Where(level => level != null && level.groups.Count > 0)
                .OrderBy(level => level.levelId)
                .ToList();

            if (levels.Count == 0)
            {
                Debug.LogError($"No levels found in Resources/{ResourcesFolder} — generating fallback levels");
                for (int i = 0; i < 3; i++)
                {
                    var rng = new System.Random(1000 + i);
                    int colors = i == 0 ? 2 : 3;
                    var level = LevelGenerator.Generate(6, 6, 2, colors, 0, rng, 4);
                    if (level == null) continue;
                    level.levelId = i + 1;
                    levels.Add(level);
                }
            }
        }

        public int Count => levels.Count;

        public LevelData Get(int index)
        {
            if (levels.Count == 0)
                throw new System.InvalidOperationException($"No levels loaded from Resources/{ResourcesFolder}");
            return levels[Mathf.Clamp(index, 0, levels.Count - 1)];
        }
    }
}
