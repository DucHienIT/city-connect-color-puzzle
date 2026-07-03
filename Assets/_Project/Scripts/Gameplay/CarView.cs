using DG.Tweening;
using UnityEngine;

namespace TinyTownRoads
{
    /// <summary>
    /// The little vehicle riding each road: parked at the drawing head while the player
    /// drags, and driving the whole route once its pair connects. Body is tinted with
    /// the path color; windows/roof/wheels are baked into the details mesh.
    /// </summary>
    public class CarView : MonoBehaviour
    {
        Tween drive;

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

        /// <summary>Drive the full route once, then vanish (delivered into the city).</summary>
        public void Drive(Vector3[] waypoints)
        {
            if (waypoints == null || waypoints.Length < 2) return;
            KillDrive();
            gameObject.SetActive(true);
            transform.position = waypoints[0];
            transform.rotation = Quaternion.LookRotation(waypoints[1] - waypoints[0], Vector3.up);
            drive = transform.DOPath(waypoints, 0.12f * waypoints.Length, PathType.Linear)
                .SetLookAt(0.02f)
                .SetEase(Ease.InOutSine)
                .OnComplete(() => gameObject.SetActive(false));
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
