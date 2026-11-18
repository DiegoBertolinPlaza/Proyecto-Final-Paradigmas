using MatriculaDeHonor.Player.Input;
using UnityEngine;

namespace MatriculaDeHonor.Tests.PlayMode
{
    /// <summary>
    /// Fuente de input que maneja el test (otra estrategia de <see cref="IPlayerInputSource"/>):
    /// permite pulsar y soltar botones en frames concretos sin teclado.
    /// </summary>
    public sealed class ScriptedInputSource : MonoBehaviour, IPlayerInputSource
    {
        public Vector2 Move;
        public bool Run;
        public bool JumpHeld;

        private bool jumpPressedThisFrame;
        private bool groundPoundPressedThisFrame;

        /// <summary>Pulsa saltar en el próximo frame. <paramref name="hold"/> = false lo suelta enseguida.</summary>
        public void PressJump(bool hold = true)
        {
            jumpPressedThisFrame = true;
            JumpHeld = hold;
        }

        /// <summary>Pulsa el culetazo en el próximo frame.</summary>
        public void PressGroundPound() => groundPoundPressedThisFrame = true;

        public PlayerInputFrame ReadFrame()
        {
            var frame = new PlayerInputFrame
            {
                Move = Move,
                MoveIsWorldSpace = true,
                RunHeld = Run,
                JumpPressed = jumpPressedThisFrame,
                JumpHeld = JumpHeld || jumpPressedThisFrame,
                GroundPoundPressed = groundPoundPressedThisFrame
            };

            jumpPressedThisFrame = false;
            groundPoundPressedThisFrame = false;
            return frame;
        }
    }
}
