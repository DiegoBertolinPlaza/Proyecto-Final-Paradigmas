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

        /// <summary>Pulsa saltar en el próximo frame. <paramref name="hold"/> = false lo suelta enseguida.</summary>
        public void PressJump(bool hold = true)
        {
            jumpPressedThisFrame = true;
            JumpHeld = hold;
        }

        public PlayerInputFrame ReadFrame()
        {
            var frame = new PlayerInputFrame
            {
                Move = Move,
                MoveIsWorldSpace = true,
                RunHeld = Run,
                JumpPressed = jumpPressedThisFrame,
                JumpHeld = JumpHeld || jumpPressedThisFrame
            };

            jumpPressedThisFrame = false;
            return frame;
        }
    }
}
