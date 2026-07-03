using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TinyTownRoads
{
    /// <summary>
    /// The only component placed in the scene. Creates the camera, event system,
    /// audio, UI and game manager at runtime so the scene needs no manual wiring.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        static readonly Color CameraBackground = new Color(0.055f, 0.07f, 0.11f);

        void Awake()
        {
            Application.targetFrameRate = 60;

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CameraBackground;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
            }

            AudioManager.Ensure();

            var input = gameObject.AddComponent<InputController>();
            var drawer = gameObject.AddComponent<PathDrawer>();
            var gm = gameObject.AddComponent<GameManager>();
            var ui = UIController.Create(gm);
            gm.Init(ui, cam, input, drawer);

            ui.ShowMainMenu();
        }
    }
}
