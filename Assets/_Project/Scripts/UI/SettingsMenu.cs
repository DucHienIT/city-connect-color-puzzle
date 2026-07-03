using UnityEngine;
using UnityEngine.UI;

namespace TinyTownRoads
{
    public class SettingsMenu
    {
        public GameObject Root { get; }

        readonly Text soundLabel;
        readonly Text musicLabel;

        public SettingsMenu(Transform parent, UIController ui)
        {
            var screen = UIFactory.CreateScreen(parent, "Settings");
            Root = screen.gameObject;

            var dim = UIFactory.CreateImage(screen, "Dim", UIFactory.DimColor, rounded: false);
            UIFactory.Stretch((RectTransform)dim.transform);

            var panelImg = UIFactory.CreateImage(screen, "Panel", UIFactory.PanelColor);
            var panel = (RectTransform)panelImg.transform;
            UIFactory.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 720));

            var title = UIFactory.CreateText(panel, "Title", "SETTINGS", 64, UIFactory.TextColor);
            UIFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(700, 90));

            var sound = UIFactory.CreateButton(panel, "Sound", "", new Vector2(560, 120), ToggleSound);
            UIFactory.Place((RectTransform)sound.transform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(560, 120));
            soundLabel = UIFactory.ButtonLabel(sound);

            var music = UIFactory.CreateButton(panel, "Music", "", new Vector2(560, 120), ToggleMusic);
            UIFactory.Place((RectTransform)music.transform, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(560, 120));
            musicLabel = UIFactory.ButtonLabel(music);

            var close = UIFactory.CreateButton(panel, "Close", "CLOSE", new Vector2(560, 120),
                ui.CloseSettings, UIFactory.Accent, 46);
            UIFactory.ButtonLabel(close).color = new Color(0.12f, 0.09f, 0.02f);
            UIFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(560, 120));

            RefreshLabels();
        }

        void ToggleSound()
        {
            SaveSystem.SoundOn = !SaveSystem.SoundOn;
            RefreshLabels();
        }

        void ToggleMusic()
        {
            SaveSystem.MusicOn = !SaveSystem.MusicOn;
            AudioManager.Instance?.ApplyMusicSetting();
            RefreshLabels();
        }

        void RefreshLabels()
        {
            soundLabel.text = $"SOUND: {(SaveSystem.SoundOn ? "ON" : "OFF")}";
            musicLabel.text = $"MUSIC: {(SaveSystem.MusicOn ? "ON" : "OFF")}";
        }
    }
}
