using UnityEngine;
using UnityEngine.InputSystem;

namespace MatriculaDeHonor.Player.Input
{
    /// <summary>
    /// Input del jugador humano con el Input System. Las acciones se definen en código para que
    /// el esqueleto funcione sin configurar nada; cuando tengáis el menú de controles podéis
    /// cambiarlo por un InputActionAsset sin tocar el PlayerController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KeyboardGamepadInputSource : MonoBehaviour, IPlayerInputSource
    {
        private InputAction move;
        private InputAction jump;
        private InputAction run;
        private InputAction groundPound;

        private void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            jump = new InputAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            run = new InputAction("Run", InputActionType.Button);
            run.AddBinding("<Keyboard>/leftShift");
            run.AddBinding("<Gamepad>/buttonWest");

            groundPound = new InputAction("GroundPound", InputActionType.Button);
            groundPound.AddBinding("<Keyboard>/leftCtrl");
            groundPound.AddBinding("<Keyboard>/c");
            groundPound.AddBinding("<Gamepad>/leftTrigger");
            groundPound.AddBinding("<Gamepad>/rightTrigger");
        }

        private void OnEnable()
        {
            move.Enable();
            jump.Enable();
            run.Enable();
            groundPound.Enable();
        }

        private void OnDisable()
        {
            move.Disable();
            jump.Disable();
            run.Disable();
            groundPound.Disable();
        }

        private void OnDestroy()
        {
            move.Dispose();
            jump.Dispose();
            run.Dispose();
            groundPound.Dispose();
        }

        public PlayerInputFrame ReadFrame()
        {
            return new PlayerInputFrame
            {
                Move = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f),
                MoveIsWorldSpace = false,
                JumpPressed = jump.WasPressedThisFrame(),
                JumpHeld = jump.IsPressed(),
                RunHeld = run.IsPressed(),
                GroundPoundPressed = groundPound.WasPressedThisFrame()
            };
        }
    }
}
