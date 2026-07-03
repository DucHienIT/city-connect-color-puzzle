using DG.Tweening;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// The little vehicle riding each road: parked at the drawing head while the
    /// player drags, and — once its branch connects — driving house → city on a
    /// loop forever (short pause at the city, then back at the house), keeping
    /// finished roads alive. Body is tinted with the color; windows/roof/wheels
    /// are baked into the details mesh.
    /// </summary>
    public class CarView : MonoBehaviour
    {
        const float SecondsPerCell = 0.18f;
        const float CityPause = 0.5f;

        Tween drive;

        /// <summary>True while the looping drive tween is alive.</summary>
        public bool IsDriving => drive != null && drive.IsActive();

        public static CarView Create(Transform parent, Color color)
        {
            var go = new GameObject("Car");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<CarView>();
            MeshFactory.CreatePart(go.transform, "Body", MeshFactory.CarBody(), color);
            MeshFactory.CreatePart(go.transform, "Details", MeshFactory.CarDetails(), Color.white);
            go.SetActive(false);
            return view;
        }

        /// <summary>Snap to a position facing <paramref name="direction"/> (no animation).</summary>
        public void Park(Vector3 position, Vector3 direction)
        {
            KillDrive();
            gameObject.SetActive(true);
            transform.position = position;
            if (direction.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        /// <summary>Drive the route on an endless loop: house → city, brief stop, repeat.</summary>
        public void Drive(Vector3[] waypoints)
        {
            if (waypoints == null || waypoints.Length < 2) return;
            KillDrive();
            gameObject.SetActive(true);
            transform.position = waypoints[0];
            transform.rotation = Quaternion.LookRotation(waypoints[1] - waypoints[0], Vector3.up);
            drive = DOTween.Sequence()
                .Append(transform.DOPath(waypoints, SecondsPerCell * waypoints.Length, PathType.Linear)
                    .SetLookAt(0.02f)
                    .SetEase(Ease.Linear))
                .AppendInterval(CityPause)
                .SetLoops(-1, LoopType.Restart);
        }

        public void Hide()
        {
            KillDrive();
            gameObject.SetActive(false);
        }

        void KillDrive()
        {
            drive?.Kill();
            drive = null;
        }

        void OnDestroy()
        {
            KillDrive();
        }
    }
}
