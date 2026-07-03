using UnityEngine;
using UnityEngine.UI;

namespace TinyTownRoads
{
    /// <summary>Top info bar (level, moves, coverage rule) and bottom action bar.</summary>
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

            levelText = UIFactory.CreateText(screen, "Level", "LEVEL 1", 64, UIFactory.Ink);
            UIFactory.Place((RectTransform)levelText.transform, new Vector2(0.5f, 0.955f), Vector2.zero, new Vector2(600, 80));

            movesText = UIFactory.CreateText(screen, "Moves", "MOVES 0", 42, UIFactory.InkMuted);
            UIFactory.Place((RectTransform)movesText.transform, new Vector2(0.5f, 0.915f), Vector2.zero, new Vector2(600, 60));

            var bar = UIFactory.CreateRect(screen, "Actions");
            UIFactory.Place(bar, new Vector2(0.5f, 0.065f), Vector2.zero, new Vector2(1020, 120));
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddAction(bar, "MENU", game.QuitToLevelSelect);
            AddAction(bar, "RESET", game.ResetLevel);
            AddAction(bar, "UNDO", game.UndoMove);
            hintLabel = UIFactory.ButtonLabel(AddAction(bar, "HINT", game.UseHint, UIFactory.AccentDark));
        }

        Button AddAction(Transform parent, string label, UnityEngine.Events.UnityAction onClick, Color? bg = null)
        {
            var button = UIFactory.CreateButton(parent, label, label, new Vector2(238, 112), () => onClick(), bg, 40);
            var le = button.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 238;
            le.preferredHeight = 112;
            return button;
        }

        public void UpdateHud(LevelData level, int moves, int hintsLeft)
        {
            levelText.text = $"LEVEL {level.levelId}";
            movesText.text = $"MOVES  {moves}";
            hintLabel.text = $"HINT ({Mathf.Max(hintsLeft, 0)})";
        }
    }
}
