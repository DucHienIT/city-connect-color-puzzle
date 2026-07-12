using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Orchestrates a play session: level lifecycle, undo, hints, win detection and
    /// progress saving. Scoring rewards how much of the grid the roads cover. UI
    /// screens call into this; it pushes state back through UIController.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        LevelLoader levels;
        UIController ui;
        Camera cam;
        InputController input;
        PathDrawer drawer;

        GridModel grid;
        PathManager paths;
        GridView view;
        HintController hints;

        int levelIndex;
        bool levelWon;
        Tween winPopupDelay;
        readonly Stack<List<Vector2Int>[][]> undoStack = new Stack<List<Vector2Int>[][]>();

        public int LevelCount => levels.Count;
        public LevelData CurrentLevel => grid?.Level;
        public int CurrentLevelIndex => levelIndex;

        public LevelData GetLevel(int index) => levels.Get(index);

        /// <summary>A level unlocks once the previous one has been completed.</summary>
        public bool IsUnlocked(int index)
        {
            return index == 0 || SaveSystem.GetStars(levels.Get(index - 1).levelId) > 0;
        }

        void Awake()
        {
            Instance = this;
            levels = new LevelLoader();
        }

        public void Init(UIController uiController, Camera camera, InputController inputController, PathDrawer pathDrawer)
        {
            ui = uiController;
            cam = camera;
            input = inputController;
            drawer = pathDrawer;
            drawer.DragEnded += OnDragEnded;
        }

        /// <summary>Starts the first not-yet-completed unlocked level (main menu "Play").</summary>
        public void ContinueGame()
        {
            int index = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                if (!IsUnlocked(i)) break;
                index = i;
                if (SaveSystem.GetStars(levels.Get(i).levelId) == 0) break;
            }
            StartLevel(index);
        }

        public void StartLevel(int index)
        {
            ClearBoard();

            levelIndex = index;
            levelWon = false;
            undoStack.Clear();

            grid = new GridModel(levels.Get(index));
            paths = new PathManager(grid);
            hints = new HintController(grid, paths);
            view = GridView.Create(grid, paths);
            drawer.Setup(paths, view, cam, input);
            drawer.InputEnabled = true;
            FitCamera();

            SaveSystem.SetLastLevelIndex(index);
            ui.ShowGameplay();
            RefreshHud();
        }

        void FitCamera()
        {
            // Tilted top-down perspective view. Push the camera back along its forward
            // axis until the board's extremes project inside a viewport band that
            // leaves room for the HUD bars at the top and bottom of the screen.
            var rot = Quaternion.Euler(72f, 0f, 0f);
            var fwd = rot * Vector3.forward;
            cam.transform.rotation = rot;

            float halfW = grid.Width * 0.5f + 0.55f;
            float halfH = grid.Height * 0.5f + 0.55f;
            var extremes = new[]
            {
                new Vector3(-halfW, 0f, -halfH), new Vector3(halfW, 0f, -halfH),
                new Vector3(-halfW, 0f, halfH), new Vector3(halfW, 0f, halfH),
                new Vector3(-halfW, 0.8f, halfH), new Vector3(halfW, 0.8f, halfH),
                new Vector3(-halfW, 0.8f, -halfH), new Vector3(halfW, 0.8f, -halfH),
            };

            float dist = 4f;
            for (int i = 0; i < 60 && !BoardFits(dist, fwd, extremes); i++)
                dist *= 1.06f;
            cam.transform.position = -fwd * dist;
        }

        bool BoardFits(float dist, Vector3 fwd, Vector3[] points)
        {
            cam.transform.position = -fwd * dist;
            foreach (var p in points)
            {
                var v = cam.WorldToViewportPoint(p);
                if (v.z < 0f || v.x < 0.05f || v.x > 0.95f || v.y < 0.16f || v.y > 0.84f)
                    return false;
            }
            return true;
        }

        void OnDragEnded(bool changed, List<Vector2Int>[][] preDragSnapshot)
        {
            if (levelWon || !changed) return;
            undoStack.Push(preDragSnapshot);
            RefreshHud();
            CheckWin();
        }

        void CheckWin()
        {
            if (levelWon || !WinChecker.IsWin(grid, paths)) return;

            levelWon = true;
            drawer.InputEnabled = false;
            int stars = WinChecker.CalcStars(grid, paths);
            SaveSystem.RecordResult(grid.Level.levelId, stars);
            AudioManager.Instance?.PlayWin();

            bool hasNext = levelIndex + 1 < levels.Count;
            int shownCoverage = WinChecker.CoveragePercent(grid, paths);
            winPopupDelay = DOVirtual.DelayedCall(0.7f, () => ui.ShowWinPopup(stars, shownCoverage, hasNext));
        }

        public void ResetLevel()
        {
            if (paths == null) return;
            winPopupDelay?.Kill();
            paths.ClearAll();
            levelWon = false;
            undoStack.Clear();
            drawer.InputEnabled = true;
            AudioManager.Instance?.PlayClick();
            RefreshHud();
        }

        public void UndoMove()
        {
            if (levelWon || undoStack.Count == 0) return;
            paths.Restore(undoStack.Pop());
            AudioManager.Instance?.PlayClick();
            RefreshHud();
        }

        public void UseHint()
        {
            if (levelWon || hints == null) return;
            if (SaveSystem.HintsRemainingToday() <= 0)
            {
                ui.ShowToast("No free hints left today");
                return;
            }

            var snapshot = paths.Snapshot();
            if (!hints.TryApplyHint())
            {
                ui.ShowToast("Nothing to hint — looking good!");
                return;
            }

            undoStack.Push(snapshot);
            SaveSystem.ConsumeHint();
            if (hints.LastHintedColor >= 0) view.PulsePair(hints.LastHintedColor);
            AudioManager.Instance?.PlayPairComplete();
            RefreshHud();
            CheckWin();
        }

        public void NextLevel()
        {
            if (levelIndex + 1 < levels.Count) StartLevel(levelIndex + 1);
            else QuitToLevelSelect();
        }

        public void QuitToLevelSelect()
        {
            ClearBoard();
            ui.ShowLevelSelect();
        }

        public void QuitToMenu()
        {
            ClearBoard();
            ui.ShowMainMenu();
        }

        void RefreshHud()
        {
            ui.UpdateHud(grid.Level, WinChecker.CoveragePercent(grid, paths), SaveSystem.HintsRemainingToday());
        }

        void ClearBoard()
        {
            winPopupDelay?.Kill();
            if (view != null)
            {
                Destroy(view.gameObject);
                view = null;
            }
            grid = null;
            paths = null;
            hints = null;
        }
    }
}
