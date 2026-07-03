using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Sprite/font hookup for the Layer Lab "GUI Pro-CasualGame" kit, stored as a
    /// ScriptableObject in Resources/UI so the code-built UI can reference the
    /// third-party assets without prefabs or scene wiring. When the asset (or a
    /// field) is missing, every screen falls back to the old flat procedural look
    /// (SpriteFactory sprites + LegacyRuntime font).
    /// </summary>
    public class UITheme : ScriptableObject
    {
        [Header("Font")]
        public Font font;

        [Header("Wide buttons")]
        public Sprite buttonGreen;
        public Sprite buttonBlue;
        public Sprite buttonYellow;
        public Sprite buttonOrange;
        public Sprite buttonRed;
        public Sprite buttonGray;

        [Header("Square buttons")]
        public Sprite squareBlue;
        public Sprite squareYellow;

        [Header("Frames & labels")]
        public Sprite cellFrame;
        public Sprite panel;
        public Sprite titleFlag;
        public Sprite titleFlagAlt;
        public Sprite label;

        [Header("Icons")]
        public Sprite iconHome;
        public Sprite iconRefresh;
        public Sprite iconUndo;
        public Sprite iconBulb;
        public Sprite iconSetting;
        public Sprite iconPlay;
        public Sprite iconLock;
        public Sprite iconSoundOn;
        public Sprite iconSoundOff;
        public Sprite iconMusicOn;
        public Sprite iconMusicOff;
        public Sprite iconClose;
        public Sprite star;
    }
}
