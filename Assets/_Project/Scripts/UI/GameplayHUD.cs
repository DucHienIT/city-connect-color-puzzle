using UnityEngine;
using UnityEngine.UI;

namespace TinyTownRoads
{
    /// <summary>
    /// Top info (level flag + move counter) and the bottom action bar of square
    /// icon buttons (menu / reset / undo / hint with its daily count).
    /// </summary>
    public class GameplayHUD
    {
        public GameObject Root { get; }

        readonly Text levelText;
        readonly Text movesText;
        readonly Text hintLabel;

        public GameplayHUD(Transform parent, GameManager game)
        {
            var screen = UIFactory.CreateScreen(parent, "GameplayHUD");
            Root = screen.gameObject;
            var theme = UIFactory.Theme;

            if (theme != null && theme.titleFlag != null)
            {
                var flag = UIFactory.CreateIcon(screen, "TitleFlag", theme.titleFlag, new Vector2(560, 190));
                UIFactory.Place((RectTransform)flag.transform, new Vector2(0.5f, 0.945f), Vector2.zero, new Vector2(560, 190));
                levelText = UIFactory.CreateText(screen, "Level", "LEVEL 1", 54, Color.white);
                UIFactory.Place((RectTransform)levelText.transform, new Vector2(0.5f, 0.952f), Vector2.zero, new Vector2(600, 80));
            }
            else
            {
                levelText = UIFactory.CreateText(screen, "Level", "LEVEL 1", 64, UIFactory.Ink);
                UIFactory.Place((RectTransform)levelText.transform, new Vector2(0.5f, 0.955f), Vector2.zero, new Vector2(600, 80));
            }

            movesText = UIFactory.CreateText(screen, "Moves", "MOVES 0", 40, UIFactory.InkMuted, FontStyle.Bold, shadow: false);
            UIFactory.Place((RectTransform)movesText.transform, new Vector2(0.5f, 0.905f), Vector2.zero, new Vector2(600, 60));

            var bar = UIFactory.CreateRect(screen, "Actions");
            UIFactory.Place(bar, new Vector2(0.5f, 0.07f), Vector2.zero, new Vector2(1020, 160));
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddAction(bar, theme?.iconHome, "MENU", game.QuitToLevelSelect, theme?.squareBlue);
            AddAction(bar, theme?.iconRefresh, "RESET", game.ResetLevel, theme?.squareBlue);
            AddAction(bar, theme?.iconUndo, "UNDO", game.UndoMove, theme?.squareBlue);
            var hint = AddAction(bar, theme?.iconBulb, "HINT", game.UseHint, theme?.squareYellow);

            if (theme != null && theme.iconBulb != null)
            {
                // Icon buttons: the hint count rides at the bottom of its button.
                hintLabel = UIFactory.CreateText(hint.transform, "Count", "x3", 32, Color.white);
                UIFactory.Place((RectTransform)hintLabel.transform, new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(120, 40));
            }
            else
            {
                hintLabel = UIFactory.ButtonLabel(hint);
            }
        }

        Button AddAction(Transform parent, Sprite icon, string fallbackLabel,
            UnityEngine.Events.UnityAction onClick, Sprite bg)
        {
            Button button;
            if (icon != null)
            {
                button = UIFactory.CreateIconButton(parent, fallbackLabel, bg, icon, 150f, () => onClick());
                var iconRt = (RectTransform)button.transform.Find("Icon").transform;
                iconRt.anchoredPosition = new Vector2(0f, 10f); // leave room for badges below
            }
            else
            {
                button = UIFactory.CreateButton(parent, fallbackLabel, fallbackLabel, new Vector2(238, 112), () => onClick(), null, 40);
            }
            var le = button.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = icon != null ? 150 : 238;
            le.preferredHeight = icon != null ? 150 : 112;
            return button;
        }

        public void UpdateHud(LevelData level, int moves, int hintsLeft)
        {
            levelText.text = $"LEVEL {level.levelId}";
            movesText.text = $"MOVES  {moves}";
            int hints = Mathf.Max(hintsLeft, 0);
            hintLabel.text = UIFactory.Theme != null && UIFactory.Theme.iconBulb != null
                ? $"x{hints}"
                : $"HINT ({hints})";
        }
    }
}
