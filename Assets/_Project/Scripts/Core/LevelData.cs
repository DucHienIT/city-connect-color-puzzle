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

    /// <summary>
    /// One color: a city (hub building) plus the houses that must each be connected
    /// to it by a road. The city's demand = houses.Count (shown as roof pins).
    /// </summary>
    [Serializable]
    public class ColorGroup
    {
        public string color;
        public CellCoord city;
        public List<CellCoord> houses = new List<CellCoord>();
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
        public List<ColorGroup> groups = new List<ColorGroup>();
        public List<CellCoord> obstacles = new List<CellCoord>();

        public static LevelData FromJson(string json)
        {
            var data = JsonUtility.FromJson<LevelData>(json);
            if (data.groups == null) data.groups = new List<ColorGroup>();
            if (data.obstacles == null) data.obstacles = new List<CellCoord>();
            foreach (var group in data.groups)
                if (group.houses == null) group.houses = new List<CellCoord>();
            return data;
        }

        public string ToJson() => JsonUtility.ToJson(this, true);
    }
}
