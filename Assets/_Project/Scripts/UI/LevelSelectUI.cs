using UnityEngine;
using UnityEngine.UI;

namespace TinyTownRoads
{
    /// <summary>Level grid with earned stars; rebuilt every time it is shown.</summary>
    public class LevelSelectUI
    {
        public GameObject Root { get; }

        readonly GameManager game;
        readonly RectTransform gridRoot;

        const int Columns = 4;
        const float Cell = 210f;
        const float Spacing = 28f;

        public LevelSelectUI(Transform parent, GameManager game, UIController ui)
        {
            this.game = game;

            var screen = UIFactory.CreateScreen(parent, "LevelSelect");
            Root = screen.gameObject;
            var theme = UIFactory.Theme;

            if (theme != null && theme.titleFlag != null)
            {
                var flag = UIFactory.CreateIcon(screen, "TitleFlag", theme.titleFlag, new Vector2(620, 200));
                UIFactory.Place((RectTransform)flag.transform, new Vector2(0.5f, 0.93f), Vector2.zero, new Vector2(620, 200));
            }
            var title = UIFactory.CreateText(screen, "Title", "SELECT LEVEL", 58, Color.white);
            UIFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 0.937f), Vector2.zero, new Vector2(900, 100));

            gridRoot = UIFactory.CreateRect(screen, "Grid");
            var layout = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(Cell, Cell);
            layout.spacing = new Vector2(Spacing, Spacing);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Columns;

            float width = Columns * Cell + (Columns - 1) * Spacing;
            UIFactory.Place(gridRoot, new Vector2(0.5f, 0.51f), Vector2.zero, new Vector2(width, 1500));

            var back = UIFactory.CreateButton(screen, "Back", "BACK", new Vector2(360, 110),
                ui.ShowMainMenu, theme != null ? theme.buttonRed : null);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(360, 110));
        }

        public void Rebuild()
        {
            for (int i = gridRoot.childCount - 1; i >= 0; i--)
                Object.Destroy(gridRoot.GetChild(i).gameObject);

            var theme = UIFactory.Theme;
            for (int i = 0; i < game.LevelCount; i++)
            {
                var level = game.GetLevel(i);
                bool unlocked = game.IsUnlocked(i);
                int stars = SaveSystem.GetStars(level.levelId);
                int index = i;

                var button = UIFactory.CreateButton(gridRoot, $"Level_{level.levelId}", "",
                    new Vector2(Cell, Cell),
                    () => game.StartLevel(index),
                    theme != null ? theme.cellFrame : null);
                button.interactable = unlocked;
                if (theme == null)
                    button.image.color = unlocked ? UIFactory.ButtonColor : new Color(0.11f, 0.13f, 0.19f);
                else if (!unlocked)
                    button.image.color = new Color(0.72f, 0.70f, 0.66f);

                if (unlocked)
                {
                    var number = UIFactory.ButtonLabel(button);
                    number.text = level.levelId.ToString();
                    number.fontSize = 68;
                    number.color = theme != null ? UIFactory.Ink : UIFactory.TextColor;
                    if (theme != null) Object.Destroy(number.GetComponent<Shadow>());
                    var numberRt = (RectTransform)number.transform;
                    numberRt.anchoredPosition = new Vector2(0, 24);

                    var rating = new StarRatingUI(button.transform, 44f, 52f);
                    UIFactory.Place(rating.Root, new Vector2(0.5f, 0.24f), Vector2.zero, Vector2.zero);
                    rating.Set(stars, false);
                }
                else if (theme != null && theme.iconLock != null)
                {
                    var lockIcon = UIFactory.CreateIcon(button.transform, "Lock", theme.iconLock,
                        new Vector2(84, 84), new Color(0.42f, 0.40f, 0.37f));
                    UIFactory.Place((RectTransform)lockIcon.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84, 84));
                }
                else
                {
                    var label = UIFactory.ButtonLabel(button);
                    label.text = "LOCKED";
                    label.fontSize = 26;
                    label.color = UIFactory.MutedText;
                }
            }
        }
    }
}
