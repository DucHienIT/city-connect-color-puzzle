using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using TTSDK;
#endif

namespace TinyTownRoads
{
    /// <summary>
    /// The only component placed in the scene. Creates the camera, event system,
    /// audio, UI and game manager at runtime so the scene needs no manual wiring.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        // Light "street" gray around the board, matching the toy-city 3D theme.
        static readonly Color CameraBackground = new Color(0.855f, 0.835f, 0.784f);

        void Awake()
        {
            Application.targetFrameRate = 60;

            // Initialize the TikTok/Douyin minigame SDK first thing on the container.
            // Any other TT.* API must only be called after the init callback fires.
            InitTikTokSDK();

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = false;
            cam.fieldOfView = 44f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CameraBackground;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 12f, -4f), Quaternion.Euler(72f, 0f, 0f));

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

        /// <summary>
        /// Boots the TikTok/Douyin minigame container SDK. Runs only in the WebGL
        /// runtime (the SDK has no effect in the Editor or other player targets).
        /// Gameplay does not block on this — it fires and logs; add any TT.* calls
        /// that need the container (login, ads, share, …) inside the callback.
        /// </summary>
        static void InitTikTokSDK()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            TT.InitSDK((code, env) =>
            {
                if (code == 0)
                {
                    Debug.Log($"[TTSDK] initialized (env: {env})");
                    // Safe to call other TT.* APIs from here on.
                }
                else
                {
                    Debug.LogError($"[TTSDK] init failed, code: {code}");
                }
            });
#endif
        }
    }
}
