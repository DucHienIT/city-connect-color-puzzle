using System;
using UnityEngine;
#if !UNITY_LUNA
using UnityEngine.InputSystem;
#endif

namespace TinyTownRoads
{
    /// <summary>
    /// Thin wrapper over the Input System pointer (mouse + touch) exposing press events
    /// in screen coordinates. Gameplay consumption happens in PathDrawer.
    /// </summary>
    public class InputController : MonoBehaviour
    {
        public event Action<Vector2> PressStarted;
        public event Action<Vector2> PressMoved;
        public event Action PressReleased;

        bool pressed;

        void Update()
        {
#if UNITY_LUNA
            // Luna playables don't support the Input System package; its engine maps
            // touch to legacy mouse events, so this covers mobile too.
            bool isPressed = Input.GetMouseButton(0);
            Vector2 pos = Input.mousePosition;
#else
            var pointer = Pointer.current;
            if (pointer == null) return;

            bool isPressed = pointer.press.isPressed;
            var pos = pointer.position.ReadValue();
#endif

            if (isPressed && !pressed) PressStarted?.Invoke(pos);
            else if (isPressed) PressMoved?.Invoke(pos);
            else if (!isPressed && pressed) PressReleased?.Invoke();

            pressed = isPressed;
        }
    }
}
