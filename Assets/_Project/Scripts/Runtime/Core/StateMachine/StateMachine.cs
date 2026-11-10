using System;
using System.Collections.Generic;

namespace MatriculaDeHonor.Core
{
    /// <summary>
    /// Máquina de estados finita con transiciones declarativas (condición → estado destino).
    /// Las transiciones "desde cualquier estado" se evalúan primero, en orden de registro,
    /// para que eventos prioritarios (muerte, daño, aturdimiento) se impongan siempre.
    /// </summary>
    public sealed class StateMachine
    {
        private sealed class Transition
        {
            public readonly IState To;
            public readonly Func<bool> Condition;

            public Transition(IState to, Func<bool> condition)
            {
                To = to;
                Condition = condition;
            }
        }

        private static readonly List<Transition> NoTransitions = new List<Transition>(0);

        private readonly Dictionary<IState, List<Transition>> transitionsByState = new Dictionary<IState, List<Transition>>();
        private readonly List<Transition> anyStateTransitions = new List<Transition>();
        private List<Transition> currentTransitions = NoTransitions;

        public IState Current { get; private set; }
        public IState Previous { get; private set; }

        /// <summary>Se lanza tras cada cambio: (anterior, nuevo).</summary>
        public event Action<IState, IState> StateChanged;

        public void AddTransition(IState from, IState to, Func<bool> condition)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            if (!transitionsByState.TryGetValue(from, out var list))
            {
                list = new List<Transition>();
                transitionsByState[from] = list;
            }

            list.Add(new Transition(to, condition));

            if (from == Current)
            {
                currentTransitions = list;
            }
        }

        public void AddAnyTransition(IState to, Func<bool> condition)
        {
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            anyStateTransitions.Add(new Transition(to, condition));
        }

        /// <summary>
        /// Cambia de estado inmediatamente. Si <paramref name="forceReenter"/> es true y el destino
        /// es el estado actual, se ejecutan Exit y Enter de nuevo (útil para rebotes o reinicios).
        /// </summary>
        public void SetState(IState next, bool forceReenter = false)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (next == Current && !forceReenter) return;

            Current?.Exit();
            Previous = Current;
            Current = next;
            currentTransitions = transitionsByState.TryGetValue(next, out var list) ? list : NoTransitions;
            Current.Enter();

            StateChanged?.Invoke(Previous, Current);
        }

        public void Tick(float deltaTime)
        {
            var transition = FindTransition();
            if (transition != null)
            {
                SetState(transition.To);
            }

            Current?.Tick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            Current?.FixedTick(fixedDeltaTime);
        }

        private Transition FindTransition()
        {
            for (int i = 0; i < anyStateTransitions.Count; i++)
            {
                var transition = anyStateTransitions[i];
                if (transition.To != Current && transition.Condition())
                {
                    return transition;
                }
            }

            for (int i = 0; i < currentTransitions.Count; i++)
            {
                var transition = currentTransitions[i];
                if (transition.Condition())
                {
                    return transition;
                }
            }

            return null;
        }
    }
}
