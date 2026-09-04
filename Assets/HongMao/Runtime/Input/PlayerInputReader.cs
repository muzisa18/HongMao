using UnityEngine;
using UnityEngine.InputSystem;

namespace HongMao
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool ParryPressed { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool AbsorbPressed { get; private set; }
        public bool MaskPressed { get; private set; }
        public bool CancelPressed { get; private set; }
        public bool RestartPressed { get; private set; }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null)
            {
                ClearFrameButtons();
                return;
            }

            Horizontal = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            Vertical = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            JumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            DodgePressed = keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame;
            AbsorbPressed = keyboard.qKey.wasPressedThisFrame;
            MaskPressed = keyboard.rKey.wasPressedThisFrame;
            CancelPressed = keyboard.escapeKey.wasPressedThisFrame;
            RestartPressed = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            AttackPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
            ParryPressed = mouse != null && mouse.rightButton.wasPressedThisFrame;
        }

        void ClearFrameButtons()
        {
            Horizontal = 0f;
            Vertical = 0f;
            JumpPressed = AttackPressed = ParryPressed = DodgePressed = false;
            AbsorbPressed = MaskPressed = CancelPressed = RestartPressed = false;
        }
    }
}
