namespace MatriculaDeHonor.Core
{
    /// <summary>
    /// Base para estados con un contexto (el "dueño" del estado: jugador, enemigo, GameManager...).
    /// Implementa IState de forma explícita y expone ganchos protegidos (Template Method), así
    /// ningún estado concreto puede olvidarse de llevar la cuenta del tiempo en el estado.
    /// </summary>
    public abstract class StateBase<TContext> : IState
    {
        protected readonly TContext Context;

        protected StateBase(TContext context)
        {
            Context = context;
        }

        public virtual string Name => GetType().Name.Replace("State", string.Empty);

        /// <summary>Segundos transcurridos desde que se entró en el estado.</summary>
        public float TimeInState { get; private set; }

        void IState.Enter()
        {
            TimeInState = 0f;
            OnEnter();
        }

        void IState.Tick(float deltaTime)
        {
            TimeInState += deltaTime;
            OnTick(deltaTime);
        }

        void IState.FixedTick(float fixedDeltaTime) => OnFixedTick(fixedDeltaTime);

        void IState.Exit() => OnExit();

        protected virtual void OnEnter() { }
        protected virtual void OnTick(float deltaTime) { }
        protected virtual void OnFixedTick(float fixedDeltaTime) { }
        protected virtual void OnExit() { }
    }
}
