using UnityEngine;

namespace TinyTownRoads
{
    public class MainMenuUI
    {
        public GameObject Root { get; }

        public MainMenuUI(Transform parent, GameManager game, UIController ui)
        {
            var screen = UIFactory.CreateScreen(parent, "MainMenu");
            Root = screen.gameObject;
            var theme = UIFactory.Theme;

            // Deep navy ink outline so the title reads crisply over the light 3D town.
            var titleInk = new Color(0.11f, 0.10f, 0.20f);

            var title = UIFactory.CreateText(screen, "Title", "TINY TOWN", 130, UIFactory.Accent,
                FontStyle.Bold, shadow: false);
            UIFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 0.76f), Vector2.zero, new Vector2(900, 150));
            UIFactory.AddOutline(title, titleInk, 5f);
            var titleMotion = title.gameObject.AddComponent<UIMotion>();
            titleMotion.amp = 7f; titleMotion.speed = 1.7f; titleMotion.phase = 0f; titleMotion.rotAmp = 1.2f;

            var title2 = UIFactory.CreateText(screen, "Title2", "ROADS", 130, Color.white,
                FontStyle.Bold, shadow: false);
            UIFactory.Place((RectTransform)title2.transform, new Vector2(0.5f, 0.69f), Vector2.zero, new Vector2(900, 150));
            UIFactory.AddOutline(title2, titleInk, 5f);
            var title2Motion = title2.gameObject.AddComponent<UIMotion>();
            title2Motion.amp = 7f; title2Motion.speed = 1.7f; title2Motion.phase = 0.9f; title2Motion.rotAmp = 1.2f;

            var subtitle = UIFactory.CreateText(screen, "Subtitle", "connect every house to its city",
                40, UIFactory.InkMuted, FontStyle.Normal, shadow: false);
            UIFactory.Place((RectTransform)subtitle.transform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(900, 60));

            var play = UIFactory.CreateButton(screen, "Play", "PLAY", new Vector2(560, 135),
                game.ContinueGame, theme != null ? theme.buttonGreen : null, 56);
            UIFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.44f), Vector2.zero, new Vector2(560, 135));
            if (theme != null) UIFactory.AddButtonIcon(play, theme.iconPlay, 52);
            var playBob = play.gameObject.AddComponent<UIMotion>();
            playBob.amp = 5f; playBob.speed = 2.2f;

            var levels = UIFactory.CreateButton(screen, "Levels", "LEVELS", new Vector2(560, 120),
                ui.ShowLevelSelect, theme != null ? theme.buttonBlue : null);
            UIFactory.Place((RectTransform)levels.transform, new Vector2(0.5f, 0.335f), Vector2.zero, new Vector2(560, 120));

            var settings = UIFactory.CreateButton(screen, "Settings", "SETTINGS", new Vector2(560, 120),
                ui.ShowSettings, theme != null ? theme.buttonGray : null);
            UIFactory.Place((RectTransform)settings.transform, new Vector2(0.5f, 0.235f), Vector2.zero, new Vector2(560, 120));
            if (theme != null) UIFactory.AddButtonIcon(settings, theme.iconSetting, 48);
        }
    }
}
