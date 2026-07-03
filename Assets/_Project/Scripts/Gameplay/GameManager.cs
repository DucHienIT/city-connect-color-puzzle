using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// Orchestrates a play session: level lifecycle, move counting, undo, hints,
    /// win detection and progress saving. UI screens call into this; it pushes
    /// state back through UIController.
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
        int moves;
        bool levelWon;
        Tween winPopupDelay;
        readonly Stack<List<Vector2Int>[]> undoStack = new Stack<List<Vector2Int>[]>();

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
            moves = 0;
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
            // Board is centered at origin; leave world-space margins for the HUD bars.
            float halfW = grid.Width * 0.5f + 0.8f;
            float halfH = grid.Height * 0.5f + 2.6f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.orthographicSize = Mathf.Max(halfH, halfW / cam.aspect);
        }

        void OnDragEnded(bool changed, List<Vector2Int>[] preDragSnapshot)
        {
            if (levelWon || !changed) return;
            undoStack.Push(preDragSnapshot);
            moves++;
            RefreshHud();
            CheckWin();
        }

        void CheckWin()
        {
            if (levelWon || !WinChecker.IsWin(grid, paths)) return;

            levelWon = true;
            drawer.InputEnabled = false;
            int stars = WinChecker.CalcStars(grid, moves);
            SaveSystem.RecordResult(grid.Level.levelId, stars);
            AudioManager.Instance?.PlayWin();

            bool hasNext = levelIndex + 1 < levels.Count;
            int shownMoves = moves;
            winPopupDelay = DOVirtual.DelayedCall(0.7f, () => ui.ShowWinPopup(stars, shownMoves, hasNext));
        }

        public void ResetLevel()
        {
            if (paths == null) return;
            winPopupDelay?.Kill();
            paths.ClearAll();
            moves = 0;
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
            moves++;
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
            ui.UpdateHud(grid.Level, moves, SaveSystem.HintsRemainingToday());
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
