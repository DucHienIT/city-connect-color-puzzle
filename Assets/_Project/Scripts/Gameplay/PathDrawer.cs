using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyTownRoads
{
    /// <summary>
    /// Turns pointer presses into path edits: begins a drag on a node or existing path,
    /// L-steps toward the pointer cell on move, and reports whether the drag changed
    /// anything on release (that is what counts as a "move").
    /// </summary>
    public class PathDrawer : MonoBehaviour
    {
        public bool InputEnabled { get; set; } = true;

        /// <summary>(pathChanged, snapshotBeforeDrag) — raised when a drag ends.</summary>
        public event Action<bool, List<Vector2Int>[]> DragEnded;

        PathManager paths;
        GridView view;
        Camera cam;
        int activeColor = -1;
        int dragStartVersion;
        List<Vector2Int>[] preDragSnapshot;

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
        }

        void OnPressStarted(Vector2 screenPos)
        {
            if (!InputEnabled || paths == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (!view.ScreenToCell(screenPos, cam, out var cell)) return;

            preDragSnapshot = paths.Snapshot();
            dragStartVersion = paths.Version;
            activeColor = paths.BeginDrag(cell);
            if (activeColor >= 0)
                AudioManager.Instance?.PlayPick();
        }

        void OnPressMoved(Vector2 screenPos)
        {
            if (!InputEnabled || activeColor < 0) return;
            if (!view.ScreenToCell(screenPos, cam, out var target)) return;
            StepTowards(target);
        }

        void StepTowards(Vector2Int target)
        {
            var path = paths.GetPath(activeColor);
            for (int guard = 0; guard < 64 && path.Count > 0; guard++)
            {
                var head = path[path.Count - 1];
                if (head == target) return;

                var delta = target - head;
                var primary = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                    ? new Vector2Int(Math.Sign(delta.x), 0)
                    : new Vector2Int(0, Math.Sign(delta.y));
                var secondary = primary.x != 0
                    ? new Vector2Int(0, Math.Sign(delta.y))
                    : new Vector2Int(Math.Sign(delta.x), 0);

                if (paths.Extend(activeColor, head + primary))
                {
                    AudioManager.Instance?.PlayDraw(path.Count);
                }
                else if (secondary != Vector2Int.zero && paths.Extend(activeColor, head + secondary))
                {
                    AudioManager.Instance?.PlayDraw(path.Count);
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
            preDragSnapshot = null;
            DragEnded?.Invoke(changed, snapshot);
        }
    }
}
