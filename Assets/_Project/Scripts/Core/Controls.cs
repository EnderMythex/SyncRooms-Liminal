using UnityEngine;
using UnityEngine.InputSystem;

namespace SyncRooms.Core
{
    /// <summary>Thin wrapper over Input System devices (keyboard/mouse + gamepad) so no action asset is needed.</summary>
    public static class Controls
    {
        static Keyboard K => Keyboard.current;
        static Mouse M => Mouse.current;
        static Gamepad G => Gamepad.current;

        public static Vector2 Move
        {
            get
            {
                Vector2 v = Vector2.zero;
                if (K != null)
                {
                    if (K.wKey.isPressed || K.zKey.isPressed || K.upArrowKey.isPressed) v.y += 1;   // AZERTY-friendly
                    if (K.sKey.isPressed || K.downArrowKey.isPressed) v.y -= 1;
                    if (K.dKey.isPressed || K.rightArrowKey.isPressed) v.x += 1;
                    if (K.aKey.isPressed || K.qKey.isPressed || K.leftArrowKey.isPressed) v.x -= 1;
                }
                if (G != null) v += G.leftStick.ReadValue();
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public static Vector2 Look
        {
            get
            {
                Vector2 v = Vector2.zero;
                if (M != null) v += M.delta.ReadValue() * 0.05f;
                if (G != null) v += G.rightStick.ReadValue() * (180f * Time.unscaledDeltaTime) * 0.1f;
                return v;
            }
        }

        public static bool Sprint => (K != null && K.leftShiftKey.isPressed) || (G != null && G.leftStickButton.isPressed);
        public static bool Crouch => (K != null && (K.leftCtrlKey.isPressed || K.cKey.isPressed)) || (G != null && G.rightStickButton.isPressed);
        public static bool JumpPressed => (K != null && K.spaceKey.wasPressedThisFrame) || (G != null && G.buttonSouth.wasPressedThisFrame);
        public static bool InteractPressed => (K != null && K.eKey.wasPressedThisFrame) || (G != null && G.buttonWest.wasPressedThisFrame);
        public static bool FlashlightPressed => (K != null && K.fKey.wasPressedThisFrame) || (G != null && G.dpad.up.wasPressedThisFrame);
        public static bool InventoryPressed => (K != null && (K.tabKey.wasPressedThisFrame || K.iKey.wasPressedThisFrame)) || (G != null && G.buttonNorth.wasPressedThisFrame);
        public static bool PausePressed => (K != null && K.escapeKey.wasPressedThisFrame) || (G != null && G.startButton.wasPressedThisFrame);
        public static bool DropPressed => K != null && K.gKey.wasPressedThisFrame;
        public static bool UsePressed => (M != null && M.leftButton.wasPressedThisFrame) || (G != null && G.rightTrigger.wasPressedThisFrame);
        /// <summary>-1, 0 or +1 from the mouse wheel / shoulder buttons.</summary>
        public static int Scroll
        {
            get
            {
                if (M != null) { float y = M.scroll.ReadValue().y; if (y > 0.1f) return -1; if (y < -0.1f) return 1; }
                if (G != null) { if (G.leftShoulder.wasPressedThisFrame) return -1; if (G.rightShoulder.wasPressedThisFrame) return 1; }
                return 0;
            }
        }

        /// <summary>Returns 0..4 when a hotbar key (1-5) was pressed this frame, else -1.</summary>
        public static int HotbarPressed
        {
            get
            {
                if (K == null) return -1;
                if (K.digit1Key.wasPressedThisFrame) return 0;
                if (K.digit2Key.wasPressedThisFrame) return 1;
                if (K.digit3Key.wasPressedThisFrame) return 2;
                if (K.digit4Key.wasPressedThisFrame) return 3;
                if (K.digit5Key.wasPressedThisFrame) return 4;
                return -1;
            }
        }
    }
}
