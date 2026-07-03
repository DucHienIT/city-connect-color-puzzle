using System.Collections.Generic;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Maps level-JSON color names to display colors. Names are the shared vocabulary
    /// between the level generator, level JSON files and the runtime.
    /// </summary>
    public static class ColorPalette
    {
        // Order matters: the level generator assigns colors by index.
        public static readonly string[] Names =
        {
            "red", "blue", "yellow", "green", "orange", "purple",
            "cyan", "pink", "brown", "lime", "teal", "indigo"
        };

        static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
        {
            { "red",    FromHex(0xE5484D) },
            { "blue",   FromHex(0x3E63DD) },
            { "yellow", FromHex(0xF5C518) },
            { "green",  FromHex(0x46A758) },
            { "orange", FromHex(0xF76B15) },
            { "purple", FromHex(0x8E4EC6) },
            { "cyan",   FromHex(0x00A2C7) },
            { "pink",   FromHex(0xE64BA6) },
            { "brown",  FromHex(0xAD7F58) },
            { "lime",   FromHex(0x99D52A) },
            { "teal",   FromHex(0x12A594) },
            { "indigo", FromHex(0x6E56CF) },
        };

        public static Color Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return Color.white;
            if (Colors.TryGetValue(name.ToLowerInvariant(), out var c)) return c;
            // Fallback: allow "#RRGGBB" values in level files.
            if (name[0] == '#' && ColorUtility.TryParseHtmlString(name, out var parsed)) return parsed;
            return Color.white;
        }

        static Color FromHex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f);
        }
    }
}
