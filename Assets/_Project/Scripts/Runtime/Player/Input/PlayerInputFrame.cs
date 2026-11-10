using UnityEngine;

namespace MatriculaDeHonor.Player.Input
{
    /// <summary>
    /// Lo que el jugador "quiere hacer" en un frame. El controlador no sabe si viene de un
    /// teclado, un mando o de un bot: eso permite entrenar al jefe con ML-Agents contra un
    /// bot que maneja exactamente el mismo personaje que tú.
    /// </summary>
    public struct PlayerInputFrame
    {
        public Vector2 Move;

        /// <summary>true si Move ya está en ejes del mundo (XZ). Si es false, es relativo a la cámara.</summary>
        public bool MoveIsWorldSpace;

        public bool JumpPressed;
        public bool JumpHeld;
        public bool RunHeld;
        public bool GroundPoundPressed;
    }

    /// <summary>Fuente de input (Strategy): teclado/mando, bot de entrenamiento, repetición...</summary>
    public interface IPlayerInputSource
    {
        PlayerInputFrame ReadFrame();
    }
}
