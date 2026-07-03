using UnityEngine;
using UnityEngine.UI;

namespace TinyTownRoads
{
    public class SettingsMenu
    {
        public GameObject Root { get; }

        readonly Text soundLabel;
        readonly Text musicLabel;
        readonly Image soundIcon;
        readonly Image musicIcon;

        public SettingsMenu(Transform parent, UIController ui)
        {
            var screen = UIFactory.CreateScreen(parent, "Settings");
            Root = screen.gameObject;
            var theme = UIFactory.Theme;

            var dim = UIFactory.CreateImage(screen, "Dim", UIFactory.DimColor, rounded: false);
            UIFactory.Stretch((RectTransform)dim.transform);

            var panelImg = theme != null && theme.panel != null
                ? UIFactory.CreateSpriteImage(screen, "Panel", theme.panel)
                : UIFactory.CreateImage(screen, "Panel", UIFactory.PanelColor);
            var panel = (RectTransform)panelImg.transform;
            UIFactory.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 760));

            var title = UIFactory.CreateText(panel, "Title", "SETTINGS", 60,
                theme != null ? UIFactory.Ink : UIFactory.TextColor,
                FontStyle.Bold, shadow: false);
            UIFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(700, 90));

            var sound = UIFactory.CreateButton(panel, "Sound", "", new Vector2(560, 120),
                ToggleSound, theme != null ? theme.buttonBlue : null);
            UIFactory.Place((RectTransform)sound.transform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(560, 120));
            soundLabel = UIFactory.ButtonLabel(sound);
            if (theme != null && theme.iconSoundOn != null)
            {
                UIFactory.AddButtonIcon(sound, theme.iconSoundOn, 52);
                soundIcon = sound.transform.Find("Icon").GetComponent<Image>();
            }

            var music = UIFactory.CreateButton(panel, "Music", "", new Vector2(560, 120),
                ToggleMusic, theme != null ? theme.buttonBlue : null);
            UIFactory.Place((RectTransform)music.transform, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(560, 120));
            musicLabel = UIFactory.ButtonLabel(music);
            if (theme != null && theme.iconMusicOn != null)
            {
                UIFactory.AddButtonIcon(music, theme.iconMusicOn, 52);
                musicIcon = music.transform.Find("Icon").GetComponent<Image>();
            }

            var close = UIFactory.CreateButton(panel, "Close", "CLOSE", new Vector2(560, 120),
                ui.CloseSettings, theme != null ? theme.buttonRed : null, 46);
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

            var theme = UIFactory.Theme;
            if (theme == null) return;
            if (soundIcon != null && theme.iconSoundOff != null)
                soundIcon.sprite = SaveSystem.SoundOn ? theme.iconSoundOn : theme.iconSoundOff;
            if (musicIcon != null && theme.iconMusicOff != null)
                musicIcon.sprite = SaveSystem.MusicOn ? theme.iconMusicOn : theme.iconMusicOff;
        }
    }
}
