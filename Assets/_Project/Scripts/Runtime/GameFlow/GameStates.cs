using MatriculaDeHonor.Core;
using UnityEngine;

namespace MatriculaDeHonor.GameFlow
{
    // Estados del bucle de partida exigido por el enunciado:
    // MainMenu → Loading → Playing ⇄ Paused → LevelComplete / GameOver / GameCompleted → (otra vez)

    public abstract class GameFlowState : StateBase<GameManager>
    {
        protected GameFlowState(GameManager context) : base(context) { }

        public abstract GameStateId Id { get; }

        protected static void SetCursor(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    public sealed class MainMenuGameState : GameFlowState
    {
        public MainMenuGameState(GameManager context) : base(context) { }

        public override GameStateId Id => GameStateId.MainMenu;

        protected override void OnEnter()
        {
            Time.timeScale = 1f;
            SetCursor(true);
        }
    }

    public sealed class LoadingGameState : GameFlowState
    {
        public LoadingGameState(GameManager context) : base(context) { }

        public override GameStateId Id => GameStateId.Loading;

        protected override void OnEnter()
        {
            Time.timeScale = 1f;
        }
    }

    public sealed class PlayingGameState : GameFlowState
    {
        public PlayingGameState(GameManager context) : base(context) { }

        public override GameStateId Id => GameStateId.Playing;

        protected override void OnEnter()
        {
            Time.timeScale = 1f;
            SetCursor(false);
        }
    }

    public sealed class PausedGameState : GameFlowState
    {
        public PausedGameState(GameManager context) : base(context) { }

        public override GameStateId Id => GameStateId.Paused;

        protected override void OnEnter()
        {
            Time.timeScale = 0f;
            SetCursor(true);
        }
    }

    /// <summary>Base de las pantallas de resultado: congelan el mundo y muestran el ratón.</summary>
    public abstract class ResultGameState : GameFlowState
    {
        protected ResultGameState(GameManager context) : base(context) { }

        protected override void OnEnter()
        {
            Time.timeScale = 0f;
            SetCursor(true);
        }
    }

    /// <summary>"¡Aprobado!"</summary>
    public sealed class LevelCompleteGameState : ResultGameState
    {
        public LevelCompleteGameState(GameManager context) : base(context) { }

        public override GameStateId Id => GameStateId.LevelComplete;
    }

    /// <summary>"Suspenso: convocatoria extraordinaria".</summary>
    public sealed class GameOverGameState : ResultGameState
    {
        public GameOverGameState(GameManager context) : base(context) { }

        public override GameStateId Id => GameStateId.GameOver;
    }

    /// <summary>"¡Matrícula de Honor!": último nivel superado.</summary>
    public sealed class GameCompletedGameState : ResultGameState
    {
        public GameCompletedGameState(GameManager context) : base(context) { }

        public override GameStateId Id => GameStateId.GameCompleted;
    }
}
