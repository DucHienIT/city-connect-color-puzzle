using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyTownRoads
{
    /// <summary>
    /// Squash-on-press feedback for uGUI buttons. Pure Update + unscaled time so it
    /// keeps working while the game is paused (timeScale = 0, e.g. the pause popup)
    /// and never depends on DOTween. Attach after the target's base scale is set.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] float pressedScale = 0.92f;
        [SerializeField] float speed = 22f;

        Vector3 baseScale = Vector3.one;
        bool held;

        void Awake() => baseScale = transform.localScale;

        public void OnPointerDown(PointerEventData e) => held = true;
        public void OnPointerUp(PointerEventData e) => held = false;
        public void OnPointerExit(PointerEventData e) => held = false;

        void Update()
        {
            var target = baseScale * (held ? pressedScale : 1f);
            float t = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, target, t);
        }

        void OnDisable()
        {
            held = false;
            transform.localScale = baseScale;
        }
    }

    /// <summary>
    /// Gentle idle bob + sway for decorative UI (titles, ornaments). Captures its rest
    /// pose in Start — so the caller must place the element first — and uses unscaled
    /// time so it animates on menus regardless of timeScale. Update-based on purpose:
    /// survives any DOTween.Kill sweep the gameplay might do.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIMotion : MonoBehaviour
    {
        public float amp = 8f;
        public float speed = 2f;
        public float phase = 0f;
        public float rotAmp = 0f;

        RectTransform rt;
        Vector2 basePos;
        Quaternion baseRot;
        bool ready;

        void Start()
        {
            rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
            baseRot = rt.localRotation;
            ready = true;
        }

        void Update()
        {
            if (!ready) return;
            float t = Time.unscaledTime;
            rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Sin(t * speed + phase) * amp);
            if (rotAmp != 0f)
                rt.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.7f + phase) * rotAmp);
        }
    }
}
