using UnityEngine;
using UnityEngine.InputSystem;
namespace MirrorTrip
{
    // Opt-in control for an unfinished level. A full WorldSession always takes priority.
    [RequireComponent(typeof(WorldPlayerMotor))]
    public sealed class LevelTestController : MonoBehaviour
    {
        public WorldPlayerMotor motor;
        private WorldSession owner;
        private Vector2 spawn, movement;
        private bool jumpHeld;
        private void Awake()
        {
            if (!motor) motor = GetComponent<WorldPlayerMotor>();
            foreach (var s in FindObjectsByType<WorldSession>(FindObjectsSortMode.None))
                if (s.surfacePlayer == motor || s.mirrorPlayer == motor) { owner = s; break; }
            spawn = motor.LocalPosition;
        }
        private void Update()
        {
            if (owner && owner.isActiveAndEnabled) return;
            var k = Keyboard.current;
            movement = Vector2.zero; jumpHeld = false; motor.SprintHeld = false;
            if (k == null || !Application.isFocused) return;
            movement.x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0)
                       - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
            if (!motor.sideView)
                movement.y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0)
                           - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
            jumpHeld = k.spaceKey.isPressed;
            if (k.spaceKey.wasPressedThisFrame) motor.QueueJump();
            motor.SprintHeld = k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed;
            if (k.rKey.wasPressedThisFrame) ResetToStart();
        }
        private void FixedUpdate()
        {
            if (owner && owner.isActiveAndEnabled) return;
            motor.Tick(movement, jumpHeld, Time.fixedDeltaTime);
        }
        public void ResetToStart()
        {
            movement = Vector2.zero; jumpHeld = false;
            motor.ResetTransient(); motor.SetLocalPosition(spawn);
            foreach (var camera in FindObjectsByType<PlayerFollowCamera>(FindObjectsSortMode.None))
                if (camera.target == motor) camera.SnapToTarget();
        }
        private void OnDisable() { if (motor) motor.SprintHeld = false; movement = Vector2.zero; jumpHeld = false; }
    }
}

