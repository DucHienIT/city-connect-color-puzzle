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

            var title = UIFactory.CreateText(screen, "Title", "SELECT LEVEL", 72, UIFactory.TextColor);
            UIFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 0.93f), Vector2.zero, new Vector2(900, 100));

            gridRoot = UIFactory.CreateRect(screen, "Grid");
            var layout = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(Cell, Cell);
            layout.spacing = new Vector2(Spacing, Spacing);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Columns;

            float width = Columns * Cell + (Columns - 1) * Spacing;
            UIFactory.Place(gridRoot, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(width, 1500));

            var back = UIFactory.CreateButton(screen, "Back", "BACK", new Vector2(360, 110), ui.ShowMainMenu);
            UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(360, 110));
        }

        public void Rebuild()
        {
            for (int i = gridRoot.childCount - 1; i >= 0; i--)
                Object.Destroy(gridRoot.GetChild(i).gameObject);

            for (int i = 0; i < game.LevelCount; i++)
            {
                var level = game.GetLevel(i);
                bool unlocked = game.IsUnlocked(i);
                int stars = SaveSystem.GetStars(level.levelId);
                int index = i;

                var button = UIFactory.CreateButton(gridRoot, $"Level_{level.levelId}", "",
                    new Vector2(Cell, Cell),
                    () => game.StartLevel(index),
                    unlocked ? UIFactory.ButtonColor : new Color(0.11f, 0.13f, 0.19f));
                button.interactable = unlocked;

                var number = UIFactory.ButtonLabel(button);
                number.text = level.levelId.ToString();
                number.fontSize = 68;
                number.color = unlocked ? UIFactory.TextColor : UIFactory.MutedText;
                var numberRt = (RectTransform)number.transform;
                numberRt.anchoredPosition = new Vector2(0, 22);

                if (unlocked)
                {
                    var rating = new StarRatingUI(button.transform, 42f, 50f);
                    UIFactory.Place(rating.Root, new Vector2(0.5f, 0.22f), Vector2.zero, Vector2.zero);
                    rating.Set(stars, false);
                }
                else
                {
                    var size = UIFactory.CreateText(button.transform, "Locked", "LOCKED", 26, UIFactory.MutedText);
                    UIFactory.Place((RectTransform)size.transform, new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(180, 40));
                }
            }
        }
    }
}
