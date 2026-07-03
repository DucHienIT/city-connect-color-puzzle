using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
            var pointer = Pointer.current;
            if (pointer == null) return;

            bool isPressed = pointer.press.isPressed;
            var pos = pointer.position.ReadValue();

            if (isPressed && !pressed) PressStarted?.Invoke(pos);
            else if (isPressed) PressMoved?.Invoke(pos);
            else if (!isPressed && pressed) PressReleased?.Invoke();

            pressed = isPressed;
        }
    }
}
