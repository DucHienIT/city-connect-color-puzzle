namespace TinyTownRoads
{
    /// <summary>
    /// Luna Playworks integration for the playable-ad build. The Luna.Unity calls are
    /// no-op stubs inside the editor and normal WebGL builds; they only do real work
    /// when the project is exported through the Playworks plugin. Flip
    /// <see cref="Enabled"/> off to restore the full-game flow (main menu, level select).
    /// </summary>
    public static class PlayableAds
    {
        /// <summary>Playable-ad mode: boot straight into a level and show the install CTA.</summary>
        public const bool Enabled = true;

        /// <summary>Level shown in the ad (index into the shipped level list).</summary>
        public const int StartLevelIndex = 0;

        // Store links opened by the CTA. Leave empty to use the URLs configured on the
        // Luna project dashboard (per-network overrides live there too).
        const string GooglePlayUrl = "https://play.google.com/store/apps/details?id=com.rotatelab.cubeblast&hl=vi";
        const string AppStoreUrl = "https://apps.apple.com/us/app/cube-land-puzzle-game/id6757154335";

        static bool started;

        /// <summary>Ad networks need to know the playable finished loading.</summary>
        public static void NotifyLoaded()
        {
            if (Enabled) Luna.Unity.LifeCycle.GameLoaded();
        }

        /// <summary>Gameplay became interactive (fires once).</summary>
        public static void NotifyStarted()
        {
            if (!Enabled || started) return;
            started = true;
            Luna.Unity.LifeCycle.GameStarted();
            Luna.Unity.Analytics.LogEvent("level_start", 0);
        }

        /// <summary>The ad's win moment — after this every network expects the CTA.</summary>
        public static void NotifyEnded()
        {
            if (!Enabled) return;
            Luna.Unity.LifeCycle.GameEnded();
            Luna.Unity.Analytics.LogEvent("level_complete", 0);
        }

        /// <summary>Install CTA — opens the store page for the current platform.</summary>
        public static void InstallFullGame()
        {
            if (!Enabled) return;
            Luna.Unity.Analytics.LogEvent("cta_clicked", 0);
            Luna.Unity.Playable.InstallFullGame(GooglePlayUrl, AppStoreUrl);
        }
    }
}
