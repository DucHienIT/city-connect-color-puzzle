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

        // Tuned for small 3D buildings viewed from above: neighbors in this list are
        // deliberately far apart in hue/value so red/orange/pink never read as the
        // same color once roofs are darkened and roads are tinted.
        static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
        {
            { "red",    FromHex(0xE23333) },
            { "blue",   FromHex(0x2F6BFF) },
            { "yellow", FromHex(0xFFD400) },
            { "green",  FromHex(0x2FA84F) },
            { "orange", FromHex(0xFF8A00) },
            { "purple", FromHex(0x9046E0) },
            { "cyan",   FromHex(0x00C2E0) },
            { "pink",   FromHex(0xFF64B8) },
            { "brown",  FromHex(0x8A5A3B) },
            { "lime",   FromHex(0xA8D521) },
            { "teal",   FromHex(0x0E9888) },
            { "indigo", FromHex(0x4A3FB5) },
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
