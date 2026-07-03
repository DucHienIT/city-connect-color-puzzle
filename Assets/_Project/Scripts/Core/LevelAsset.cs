using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// ScriptableObject wrapper for one level, stored as .asset files in
    /// Resources/Levels. The payload stays the plain LevelData class so the
    /// pure-logic core (solver, generator, tests) never depends on ScriptableObject
    /// and can still run outside Unity.
    /// </summary>
    public class LevelAsset : ScriptableObject
    {
        public LevelData data = new LevelData();
    }
}
