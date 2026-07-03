using System;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Progress and settings persistence via PlayerPrefs: stars per level,
    /// last played level, sound toggles and the daily free-hint allowance.
    /// </summary>
    public static class SaveSystem
    {
        const string StarsKey = "ttr_stars_";       // + levelId
        const string LastLevelKey = "ttr_last_level";
        const string SoundKey = "ttr_sound";
        const string MusicKey = "ttr_music";
        const string HintDateKey = "ttr_hint_date";
        const string HintCountKey = "ttr_hint_count";

        public const int FreeHintsPerDay = 3;

        public static int GetStars(int levelId) => PlayerPrefs.GetInt(StarsKey + levelId, 0);

        public static void RecordResult(int levelId, int stars)
        {
            if (stars > GetStars(levelId))
            {
                PlayerPrefs.SetInt(StarsKey + levelId, stars);
                PlayerPrefs.Save();
            }
        }

        public static int GetLastLevelIndex() => PlayerPrefs.GetInt(LastLevelKey, 0);

        public static void SetLastLevelIndex(int index)
        {
            PlayerPrefs.SetInt(LastLevelKey, index);
            PlayerPrefs.Save();
        }

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set { PlayerPrefs.SetInt(SoundKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static int HintsRemainingToday()
        {
            return FreeHintsPerDay - HintsUsedToday();
        }

        public static void ConsumeHint()
        {
            PlayerPrefs.SetString(HintDateKey, Today());
            PlayerPrefs.SetInt(HintCountKey, HintsUsedToday() + 1);
            PlayerPrefs.Save();
        }

        static int HintsUsedToday()
        {
            return PlayerPrefs.GetString(HintDateKey, "") == Today()
                ? PlayerPrefs.GetInt(HintCountKey, 0)
                : 0;
        }

        static string Today() => DateTime.Now.ToString("yyyyMMdd");
    }
}
