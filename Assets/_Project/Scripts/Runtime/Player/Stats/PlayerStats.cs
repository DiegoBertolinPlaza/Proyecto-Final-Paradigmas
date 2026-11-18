using System;
using System.Collections.Generic;

namespace MatriculaDeHonor.Player.Stats
{
    public enum StatType
    {
        WalkSpeed,
        RunSpeed,
        JumpHeight,
        /// <summary>Multiplica el radio de ruido (la sudadera lo baja: sigilo en la biblioteca).</summary>
        NoiseMultiplier,
        /// <summary>1 = inmune a charcos químicos (bata de laboratorio).</summary>
        ChemicalResistance
    }

    public interface IStatProvider
    {
        float Get(StatType stat);
    }

    /// <summary>Algo que modifica estadísticas durante un tiempo (los power-ups).</summary>
    public interface IStatModifierSource
    {
        string DisplayName { get; }

        /// <summary>Duración en segundos; 0 o menos = permanente hasta que se limpie.</summary>
        float Duration { get; }

        /// <summary>Envuelve al proveedor recibido con sus decoradores.</summary>
        IStatProvider Decorate(IStatProvider inner);
    }

    /// <summary>Valores base, sacados de la configuración del jugador.</summary>
    public sealed class BaseStatProvider : IStatProvider
    {
        private readonly Dictionary<StatType, float> values;

        public BaseStatProvider(IDictionary<StatType, float> values)
        {
            this.values = new Dictionary<StatType, float>(values);
        }

        public float Get(StatType stat) => values.TryGetValue(stat, out var value) ? value : 0f;
    }

    /// <summary>
    /// Base del patrón Decorator: cada power-up envuelve al proveedor anterior y solo altera
    /// las estadísticas que le interesan. Así se pueden apilar café + sudadera sin ifs.
    /// </summary>
    public abstract class StatDecorator : IStatProvider
    {
        protected readonly IStatProvider Inner;

        protected StatDecorator(IStatProvider inner)
        {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public virtual float Get(StatType stat) => Inner.Get(stat);
    }

    public sealed class StatMultiplierDecorator : StatDecorator
    {
        private readonly StatType target;
        private readonly float factor;

        public StatMultiplierDecorator(IStatProvider inner, StatType target, float factor) : base(inner)
        {
            this.target = target;
            this.factor = factor;
        }

        public override float Get(StatType stat) => stat == target ? Inner.Get(stat) * factor : Inner.Get(stat);
    }

    public sealed class StatAdditiveDecorator : StatDecorator
    {
        private readonly StatType target;
        private readonly float amount;

        public StatAdditiveDecorator(IStatProvider inner, StatType target, float amount) : base(inner)
        {
            this.target = target;
            this.amount = amount;
        }

        public override float Get(StatType stat) => stat == target ? Inner.Get(stat) + amount : Inner.Get(stat);
    }

    public sealed class StatOverrideDecorator : StatDecorator
    {
        private readonly StatType target;
        private readonly float value;

        public StatOverrideDecorator(IStatProvider inner, StatType target, float value) : base(inner)
        {
            this.target = target;
            this.value = value;
        }

        public override float Get(StatType stat) => stat == target ? value : Inner.Get(stat);
    }

    /// <summary>
    /// Mantiene la cadena de decoradores activa. Cuando un modificador caduca se reconstruye la
    /// cadena desde la base, en el mismo orden en que se aplicaron los que quedan.
    /// </summary>
    public sealed class PlayerStats : IStatProvider
    {
        private sealed class ActiveModifier
        {
            public readonly IStatModifierSource Source;
            public float Remaining;

            public ActiveModifier(IStatModifierSource source)
            {
                Source = source;
                Remaining = source.Duration;
            }

            public bool IsTimed => Source.Duration > 0f;
        }

        private readonly IStatProvider baseProvider;
        private readonly List<ActiveModifier> active = new List<ActiveModifier>();
        private IStatProvider chain;

        /// <summary>(fuente, activo)</summary>
        public event Action<IStatModifierSource, bool> ModifierChanged;

        public PlayerStats(IStatProvider baseProvider)
        {
            this.baseProvider = baseProvider ?? throw new ArgumentNullException(nameof(baseProvider));
            chain = baseProvider;
        }

        public int ActiveCount => active.Count;

        public float Get(StatType stat) => chain.Get(stat);

        public bool IsActive(IStatModifierSource source) => IndexOf(source) >= 0;

        /// <summary>Aplica un modificador. Si ya estaba activo, solo refresca su duración.</summary>
        public void Apply(IStatModifierSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            int index = IndexOf(source);
            if (index >= 0)
            {
                active[index].Remaining = source.Duration;
                ModifierChanged?.Invoke(source, true);
                return;
            }

            active.Add(new ActiveModifier(source));
            Rebuild();
            ModifierChanged?.Invoke(source, true);
        }

        public void Remove(IStatModifierSource source)
        {
            int index = IndexOf(source);
            if (index < 0) return;

            active.RemoveAt(index);
            Rebuild();
            ModifierChanged?.Invoke(source, false);
        }

        public void Tick(float deltaTime)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var modifier = active[i];
                if (!modifier.IsTimed) continue;

                modifier.Remaining -= deltaTime;
                if (modifier.Remaining <= 0f)
                {
                    Remove(modifier.Source);
                }
            }
        }

        public void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Remove(active[i].Source);
            }
        }

        private int IndexOf(IStatModifierSource source)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (ReferenceEquals(active[i].Source, source)) return i;
            }

            return -1;
        }

        private void Rebuild()
        {
            IStatProvider current = baseProvider;
            for (int i = 0; i < active.Count; i++)
            {
                current = active[i].Source.Decorate(current);
            }

            chain = current;
        }
    }
}
