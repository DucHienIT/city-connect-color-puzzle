using UnityEngine;
using UnityEngine.EventSystems;
#if !UNITY_LUNA
using UnityEngine.InputSystem.UI;
#endif

namespace TinyTownRoads
{
    /// <summary>
    /// The only component placed in the scene. Creates the camera, event system,
    /// audio, UI and game manager at runtime so the scene needs no manual wiring.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        // Scene-serialized asset references. Luna's export only bundles assets a scene
        // depends on, so the playable build gets its theme and levels through these
        // instead of Resources.Load (which finds nothing there).
        [SerializeField] UITheme theme;
        [SerializeField] LevelAsset[] levels;
        [SerializeField] Material meshMaterial;

        // Light "street" gray around the board, matching the toy-city 3D theme.
        static readonly Color CameraBackground = new Color(0.855f, 0.835f, 0.784f);

        void Awake()
        {
#if UNITY_LUNA
            // Luna playable: a silent boot failure just shows the clear color, so
            // surface any exception on screen (Playground has no visible console).
            try { Boot(); }
            catch (System.Exception e) { ShowBootError(e); }
#else
            Boot();
#endif
        }

        void Boot()
        {
            Application.targetFrameRate = 60;

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

#if UNITY_LUNA
            if (FindObjectOfType<EventSystem>() == null)
#else
            if (FindFirstObjectByType<EventSystem>() == null)
#endif
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
#if UNITY_LUNA
                esGo.AddComponent<StandaloneInputModule>();
#else
                esGo.AddComponent<InputSystemUIInputModule>();
#endif
            }

            AudioManager.Ensure();

            UIFactory.SetTheme(theme);
            LevelLoader.InjectedLevels = levels;
            MeshFactory.SetMaterial(meshMaterial);

            var input = gameObject.AddComponent<InputController>();
            var drawer = gameObject.AddComponent<PathDrawer>();
            var gm = gameObject.AddComponent<GameManager>();
            var ui = UIController.Create(gm);
            gm.Init(ui, cam, input, drawer);

            PlayableAds.NotifyLoaded();
            if (PlayableAds.Enabled)
                gm.StartLevel(PlayableAds.StartLevelIndex);
            else
                ui.ShowMainMenu();
        }

#if UNITY_LUNA
        static void ShowBootError(System.Exception e)
        {
            Debug.LogError("Boot failed: " + e);

            // Channel 1: Playground's Events panel — works even when rendering is broken.
            try
            {
                LogErrorEvent("ERR " + e.GetType().Name + ": " + e.Message);
                var stack = e.StackTrace;
                if (stack != null)
                {
                    var lines = stack.Split('\n');
                    for (int i = 0; i < lines.Length && i < 5; i++)
                        LogErrorEvent("ST" + i + " " + lines[i].Trim());
                }
            }
            catch { }

            // Channel 2: on-screen overlay; every step shielded so a partial
            // failure still shows as much as possible.
            try
            {
                var canvasGo = new GameObject("BootError");
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;

                var bgGo = new GameObject("Bg");
                bgGo.transform.SetParent(canvasGo.transform, false);
                var bg = bgGo.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(0.45f, 0f, 0f, 0.65f);
                var bgRt = bg.rectTransform;
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;

                Font font = null;
                try { var t = UIFactory.Theme; if (t != null) font = t.font; } catch { }
                if (font == null)
                    try { font = (Font)Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf"); } catch { }
                if (font == null)
                    try { font = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf"); } catch { }
                if (font == null) return;

                var textGo = new GameObject("Text");
                textGo.transform.SetParent(canvasGo.transform, false);
                var text = textGo.AddComponent<UnityEngine.UI.Text>();
                text.font = font;
                text.fontSize = 22;
                text.color = Color.white;
                text.text = e.GetType().Name + ": " + e.Message + "\n\n" + e.StackTrace;
                var rt = text.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(20f, 20f);
                rt.offsetMax = new Vector2(-20f, -20f);
            }
            catch (System.Exception overlayError)
            {
                Debug.LogError("Boot error overlay failed too: " + overlayError);
                try { LogErrorEvent("OVERLAY-ERR " + overlayError.GetType().Name + ": " + overlayError.Message); } catch { }
            }
        }

        static void LogErrorEvent(string message)
        {
            if (message.Length > 90) message = message.Substring(0, 90);
            Luna.Unity.Analytics.LogEvent(message, 0);
        }
#endif
    }
}
