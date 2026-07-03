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

            var title = UIFactory.CreateText(screen, "Title", "TINY TOWN", 130, UIFactory.Accent);
            UIFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 0.76f), Vector2.zero, new Vector2(900, 150));

            var title2 = UIFactory.CreateText(screen, "Title2", "ROADS", 130, UIFactory.TextColor);
            UIFactory.Place((RectTransform)title2.transform, new Vector2(0.5f, 0.69f), Vector2.zero, new Vector2(900, 150));

            var subtitle = UIFactory.CreateText(screen, "Subtitle", "connect every building, pave every street",
                40, UIFactory.MutedText, FontStyle.Normal);
            UIFactory.Place((RectTransform)subtitle.transform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(900, 60));

            var play = UIFactory.CreateButton(screen, "Play", "PLAY", new Vector2(560, 130),
                game.ContinueGame, UIFactory.Accent, 54);
            UIFactory.ButtonLabel(play).color = new Color(0.12f, 0.09f, 0.02f);
            UIFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.44f), Vector2.zero, new Vector2(560, 130));

            var levels = UIFactory.CreateButton(screen, "Levels", "LEVELS", new Vector2(560, 120), ui.ShowLevelSelect);
            UIFactory.Place((RectTransform)levels.transform, new Vector2(0.5f, 0.34f), Vector2.zero, new Vector2(560, 120));

            var settings = UIFactory.CreateButton(screen, "Settings", "SETTINGS", new Vector2(560, 120), ui.ShowSettings);
            UIFactory.Place((RectTransform)settings.transform, new Vector2(0.5f, 0.24f), Vector2.zero, new Vector2(560, 120));
        }
    }
}
