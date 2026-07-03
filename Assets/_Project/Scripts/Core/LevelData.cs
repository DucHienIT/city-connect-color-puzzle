using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    [Serializable]
    public class CellCoord
    {
        public int x;
        public int y;

        public CellCoord() { }
        public CellCoord(int x, int y) { this.x = x; this.y = y; }

        public Vector2Int ToVector() => new Vector2Int(x, y);
    }

    [Serializable]
    public class ColorPair
    {
        public string color;
        public CellCoord start;
        public CellCoord end;
    }

    /// <summary>
    /// One puzzle level, deserialized from JSON (see docs/GAME_SPEC.md section 3).
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int levelId;
        public int gridWidth;
        public int gridHeight;
        public bool requireFullCoverage;
        public List<ColorPair> pairs = new List<ColorPair>();
        public List<CellCoord> obstacles = new List<CellCoord>();

        public static LevelData FromJson(string json)
        {
            var data = JsonUtility.FromJson<LevelData>(json);
            if (data.pairs == null) data.pairs = new List<ColorPair>();
            if (data.obstacles == null) data.obstacles = new List<CellCoord>();
            return data;
        }

        public string ToJson() => JsonUtility.ToJson(this, true);
    }
}
