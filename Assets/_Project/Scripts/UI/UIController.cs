using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTownRoads
{
    /// <summary>
    /// Owns the canvas and all screens (built once, toggled on demand), the settings
    /// overlay and a small toast label. Screens call GameManager; GameManager calls back
    /// through the Show*/UpdateHud methods here.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        GameManager game;
        MainMenuUI mainMenu;
        LevelSelectUI levelSelect;
        GameplayHUD hud;
        WinPopup winPopup;
        SettingsMenu settings;
        CanvasGroup toastGroup;
        Text toastText;

        public static UIController Create(GameManager game)
        {
            var canvas = UIFactory.CreateCanvas("UI");
            var ui = canvas.gameObject.AddComponent<UIController>();
            ui.game = game;
            ui.Build(canvas.transform);
            return ui;
        }

        void Build(Transform root)
        {
            mainMenu = new MainMenuUI(root, game, this);
            levelSelect = new LevelSelectUI(root, game, this);
            hud = new GameplayHUD(root, game);
            winPopup = new WinPopup(root, game);
            settings = new SettingsMenu(root, this);

            var toastRt = UIFactory.CreateRect(root, "Toast");
            UIFactory.Place(toastRt, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(900, 90));
            var bg = UIFactory.CreateImage(toastRt, "Bg", new Color(0f, 0f, 0f, 0.75f));
            UIFactory.Stretch((RectTransform)bg.transform);
            toastText = UIFactory.CreateText(toastRt, "Text", "", 40, UIFactory.TextColor, FontStyle.Normal);
            UIFactory.Stretch((RectTransform)toastText.transform);
            toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastGroup.alpha = 0f;

            HideAll();
        }

        void HideAll()
        {
            mainMenu.Root.SetActive(false);
            levelSelect.Root.SetActive(false);
            hud.Root.SetActive(false);
            winPopup.Hide();
            settings.Root.SetActive(false);
        }

        public void ShowMainMenu()
        {
            HideAll();
            mainMenu.Root.SetActive(true);
        }

        public void ShowLevelSelect()
        {
            HideAll();
            levelSelect.Rebuild();
            levelSelect.Root.SetActive(true);
        }

        public void ShowGameplay()
        {
            HideAll();
            hud.Root.SetActive(true);
        }

        public void ShowWinPopup(int stars, int moves, bool hasNext)
        {
            winPopup.Show(stars, moves, hasNext);
        }

        public void ShowSettings()
        {
            settings.Root.SetActive(true);
        }

        public void CloseSettings()
        {
            settings.Root.SetActive(false);
        }

        public void UpdateHud(LevelData level, int moves, int hintsLeft)
        {
            hud.UpdateHud(level, moves, hintsLeft);
        }

        public void ShowToast(string message)
        {
            toastText.text = message;
            toastGroup.DOKill();
            toastGroup.alpha = 1f;
            toastGroup.DOFade(0f, 0.5f).SetDelay(1.6f);
        }
    }
}
