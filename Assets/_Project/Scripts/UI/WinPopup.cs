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
        readonly Text movesText;
        readonly Button nextButton;

        public WinPopup(Transform parent, GameManager game)
        {
            var screen = UIFactory.CreateScreen(parent, "WinPopup");
            Root = screen.gameObject;

            var dim = UIFactory.CreateImage(screen, "Dim", UIFactory.DimColor, rounded: false);
            UIFactory.Stretch((RectTransform)dim.transform);

            var panelImg = UIFactory.CreateImage(screen, "Panel", UIFactory.PanelColor);
            panel = (RectTransform)panelImg.transform;
            UIFactory.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 1020));

            var title = UIFactory.CreateText(panel, "Title", "DISTRICT COMPLETE!", 62, UIFactory.Accent);
            UIFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(760, 90));

            rating = new StarRatingUI(panel, 170f, 210f);
            UIFactory.Place(rating.Root, new Vector2(0.5f, 0.66f), Vector2.zero, Vector2.zero);

            movesText = UIFactory.CreateText(panel, "Moves", "", 44, UIFactory.MutedText, FontStyle.Normal);
            UIFactory.Place((RectTransform)movesText.transform, new Vector2(0.5f, 0.47f), Vector2.zero, new Vector2(700, 60));

            nextButton = UIFactory.CreateButton(panel, "Next", "NEXT LEVEL", new Vector2(560, 125),
                game.NextLevel, UIFactory.Accent, 48);
            UIFactory.ButtonLabel(nextButton).color = new Color(0.12f, 0.09f, 0.02f);
            UIFactory.Place((RectTransform)nextButton.transform, new Vector2(0.5f, 0.33f), Vector2.zero, new Vector2(560, 125));

            var replay = UIFactory.CreateButton(panel, "Replay", "REPLAY", new Vector2(560, 115),
                () => game.StartLevel(game.CurrentLevelIndex));
            UIFactory.Place((RectTransform)replay.transform, new Vector2(0.5f, 0.21f), Vector2.zero, new Vector2(560, 115));

            var levels = UIFactory.CreateButton(panel, "Levels", "LEVELS", new Vector2(560, 115),
                game.QuitToLevelSelect);
            UIFactory.Place((RectTransform)levels.transform, new Vector2(0.5f, 0.09f), Vector2.zero, new Vector2(560, 115));
        }

        public void Show(int stars, int moves, bool hasNext)
        {
            Root.SetActive(true);
            nextButton.gameObject.SetActive(hasNext);
            movesText.text = $"Solved in {moves} move{(moves == 1 ? "" : "s")}";
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
