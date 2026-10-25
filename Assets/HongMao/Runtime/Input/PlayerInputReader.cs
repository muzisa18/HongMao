using UnityEngine;
using UnityEngine.InputSystem;

namespace HongMao
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
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
        public bool InteractPressed { get; private set; }
        public bool GameplayBlocked { get; private set; }

        public void SetGameplayBlocked(bool blocked)
        {
            GameplayBlocked = blocked;
            // Also consume gameplay buttons on the closing frame.
            ClearGameplayButtons();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null)
            {
                ClearGameplayButtons();
                CancelPressed = RestartPressed = InteractPressed = false;
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
            InteractPressed = keyboard.eKey.wasPressedThisFrame;
            if (GameplayBlocked) ClearGameplayButtons();
        }

        void ClearGameplayButtons()
        {
            Horizontal = 0f;
            Vertical = 0f;
            JumpPressed = AttackPressed = ParryPressed = DodgePressed = false;
            AbsorbPressed = MaskPressed = false;
        }
    }
}
