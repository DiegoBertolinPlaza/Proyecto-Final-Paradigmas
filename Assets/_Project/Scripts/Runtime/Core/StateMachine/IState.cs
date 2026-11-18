namespace MatriculaDeHonor.Core
{
    /// <summary>
    /// Estado genérico (patrón State). Lo usan el jugador, los profesores, el jefe y el flujo de partida.
    /// </summary>
    public interface IState
    {
        /// <summary>Nombre legible para depuración (overlay, logs).</summary>
        string Name { get; }

        void Enter();
        void Tick(float deltaTime);
        void FixedTick(float fixedDeltaTime);
        void Exit();
    }
}
