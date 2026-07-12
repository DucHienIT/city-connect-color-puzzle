using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTownRoads
{
    public class WinPopup
    {
        public GameObject Root { get; }

        readonly RectTransform panel;
        readonly StarRatingUI rating;
        readonly Text coverageText;
        readonly Button nextButton;

        public WinPopup(Transform parent, GameManager game)
        {
            var screen = UIFactory.CreateScreen(parent, "WinPopup");
            Root = screen.gameObject;
            var theme = UIFactory.Theme;

            var dim = UIFactory.CreateImage(screen, "Dim", UIFactory.DimColor, rounded: false);
            UIFactory.Stretch((RectTransform)dim.transform);

            var panelImg = theme != null && theme.panel != null
                ? UIFactory.CreateSpriteImage(screen, "Panel", theme.panel)
                : UIFactory.CreateImage(screen, "Panel", UIFactory.PanelColor);
            panel = (RectTransform)panelImg.transform;
            UIFactory.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 1040));

            if (theme != null && theme.titleFlagAlt != null)
            {
                var flag = UIFactory.CreateIcon(panel, "TitleFlag", theme.titleFlagAlt, new Vector2(640, 210));
                UIFactory.Place((RectTransform)flag.transform, new Vector2(0.5f, 0.97f), Vector2.zero, new Vector2(640, 210));
            }
            var title = UIFactory.CreateText(panel, "Title", "DISTRICT COMPLETE!", 50,
                theme != null ? Color.white : UIFactory.Accent);
            UIFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 0.985f), Vector2.zero, new Vector2(760, 90));

            rating = new StarRatingUI(panel, 170f, 210f);
            UIFactory.Place(rating.Root, new Vector2(0.5f, 0.66f), Vector2.zero, Vector2.zero);

            coverageText = UIFactory.CreateText(panel, "Coverage", "",  42,
                theme != null ? Color.white : UIFactory.MutedText, FontStyle.Normal, shadow: false);
            UIFactory.Place((RectTransform)coverageText.transform, new Vector2(0.5f, 0.47f), Vector2.zero, new Vector2(700, 60));
            // Light text on the blue panel body; subtle dark outline for legibility.
            if (theme != null) UIFactory.AddOutline(coverageText, new Color(0.06f, 0.13f, 0.28f, 0.85f), 2f, drop: false);

            nextButton = UIFactory.CreateButton(panel, "Next", "NEXT LEVEL", new Vector2(560, 125),
                game.NextLevel, theme != null ? theme.buttonGreen : null, 48);
            UIFactory.Place((RectTransform)nextButton.transform, new Vector2(0.5f, 0.34f), Vector2.zero, new Vector2(560, 125));

            var replay = UIFactory.CreateButton(panel, "Replay", "REPLAY", new Vector2(560, 115),
                () => game.StartLevel(game.CurrentLevelIndex), theme != null ? theme.buttonYellow : null);
            UIFactory.Place((RectTransform)replay.transform, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(560, 115));

            var levels = UIFactory.CreateButton(panel, "Levels", "LEVELS", new Vector2(560, 115),
                game.QuitToLevelSelect, theme != null ? theme.buttonBlue : null);
            UIFactory.Place((RectTransform)levels.transform, new Vector2(0.5f, 0.1f), Vector2.zero, new Vector2(560, 115));
        }

        public void Show(int stars, int coverage, bool hasNext)
        {
            Root.SetActive(true);
            nextButton.gameObject.SetActive(hasNext);
            coverageText.text = $"Town filled: {coverage}%";
            rating.Set(stars, animate: true);

            panel.DOKill();
            panel.localScale = Vector3.one * 0.7f;
            panel.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        }

        public void Hide()
        {
            panel.DOKill();
            Root.SetActive(false);
        }
    }
}
