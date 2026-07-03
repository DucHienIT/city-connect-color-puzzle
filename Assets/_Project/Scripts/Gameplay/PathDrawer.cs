using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyTownRoads
{
    /// <summary>
    /// Turns pointer presses into road edits: begins a drag on a house or existing
    /// branch, L-steps the active branch toward the pointer cell on move, and reports
    /// whether the drag changed anything on release (that is what counts as a "move").
    /// </summary>
    public class PathDrawer : MonoBehaviour
    {
        public bool InputEnabled { get; set; } = true;

        /// <summary>(roadsChanged, snapshotBeforeDrag) — raised when a drag ends.</summary>
        public event Action<bool, List<Vector2Int>[][]> DragEnded;

        PathManager paths;
        GridView view;
        Camera cam;
        int activeColor = -1;
        int activeHouse = -1;
        int dragStartVersion;
        List<Vector2Int>[][] preDragSnapshot;

        public void Setup(PathManager pathManager, GridView gridView, Camera camera, InputController input)
        {
            if (paths == null)
            {
                input.PressStarted += OnPressStarted;
                input.PressMoved += OnPressMoved;
                input.PressReleased += OnPressReleased;
            }
            paths = pathManager;
            view = gridView;
            cam = camera;
            activeColor = -1;
            activeHouse = -1;
        }

        void OnPressStarted(Vector2 screenPos)
        {
            if (!InputEnabled || paths == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (!view.ScreenToCell(screenPos, cam, out var cell)) return;

            preDragSnapshot = paths.Snapshot();
            dragStartVersion = paths.Version;
            if (paths.BeginDrag(cell, out activeColor, out activeHouse))
            {
                AudioManager.Instance?.PlayPick();
                view.PulseCity(activeColor); // show which city this house belongs to
            }
        }

        void OnPressMoved(Vector2 screenPos)
        {
            if (!InputEnabled || activeColor < 0) return;
            if (!view.ScreenToCell(screenPos, cam, out var target)) return;
            StepTowards(target);
        }

        void StepTowards(Vector2Int target)
        {
            var branch = paths.GetBranch(activeColor, activeHouse);
            for (int guard = 0; guard < 64 && branch.Count > 0; guard++)
            {
                var head = branch[branch.Count - 1];
                if (head == target) return;

                var delta = target - head;
                var primary = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                    ? new Vector2Int(Math.Sign(delta.x), 0)
                    : new Vector2Int(0, Math.Sign(delta.y));
                var secondary = primary.x != 0
                    ? new Vector2Int(0, Math.Sign(delta.y))
                    : new Vector2Int(Math.Sign(delta.x), 0);

                if (paths.Extend(activeColor, activeHouse, head + primary))
                {
                    AudioManager.Instance?.PlayDraw(branch.Count);
                }
                else if (secondary != Vector2Int.zero && paths.Extend(activeColor, activeHouse, head + secondary))
                {
                    AudioManager.Instance?.PlayDraw(branch.Count);
                }
                else
                {
                    return;
                }
            }
        }

        void OnPressReleased()
        {
            if (activeColor < 0) return;
            bool changed = paths.Version != dragStartVersion;
            var snapshot = preDragSnapshot;
            activeColor = -1;
            activeHouse = -1;
            preDragSnapshot = null;
            DragEnded?.Invoke(changed, snapshot);
        }
    }
}
