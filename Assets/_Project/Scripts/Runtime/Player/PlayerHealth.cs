using System;
using MatriculaDeHonor.Combat;

namespace MatriculaDeHonor.Player
{
    public enum DamageResult
    {
        /// <summary>No ha hecho nada (invulnerable o ya muerto).</summary>
        Ignored,
        /// <summary>Lo ha parado la chuleta.</summary>
        Absorbed,
        Damaged,
        Killed
    }

    /// <summary>
    /// Vida del estudiante, en C# puro (sin MonoBehaviour) para poder testearla en EditMode.
    /// </summary>
    public sealed class PlayerHealth
    {
        private float invulnerableTimer;

        public PlayerHealth(int max, int maxShield)
        {
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max));

            Max = max;
            MaxShield = Math.Max(0, maxShield);
            Current = max;
        }

        public int Max { get; }
        public int MaxShield { get; }
        public int Current { get; private set; }
        public int Shield { get; private set; }
        public bool IsDead => Current <= 0;
        public bool IsInvulnerable => invulnerableTimer > 0f;
        public float Health01 => (float)Current / Max;

        public event Action<PlayerHealth> Changed;
        public event Action Died;

        public DamageResult ApplyDamage(in DamageInfo damage, float invulnerabilitySeconds)
        {
            if (IsDead || damage.Amount <= 0) return DamageResult.Ignored;

            // Caer al vacío mata siempre, aunque seas invulnerable.
            if (damage.Kind == DamageKind.Fall)
            {
                Kill();
                return DamageResult.Killed;
            }

            if (IsInvulnerable) return DamageResult.Ignored;

            invulnerableTimer = invulnerabilitySeconds;

            if (Shield > 0)
            {
                Shield--;
                Changed?.Invoke(this);
                return DamageResult.Absorbed;
            }

            Current = Math.Max(0, Current - damage.Amount);
            Changed?.Invoke(this);

            if (Current == 0)
            {
                Died?.Invoke();
                return DamageResult.Killed;
            }

            return DamageResult.Damaged;
        }

        public void AddShield(int amount)
        {
            if (amount <= 0 || IsDead) return;

            Shield = Math.Min(MaxShield, Shield + amount);
            Changed?.Invoke(this);
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead) return;

            Current = Math.Min(Max, Current + amount);
            Changed?.Invoke(this);
        }

        public void Kill()
        {
            if (IsDead) return;

            Current = 0;
            Shield = 0;
            Changed?.Invoke(this);
            Died?.Invoke();
        }

        public void ResetToFull()
        {
            Current = Max;
            Shield = 0;
            invulnerableTimer = 0f;
            Changed?.Invoke(this);
        }

        public void Tick(float deltaTime)
        {
            if (invulnerableTimer > 0f)
            {
                invulnerableTimer = Math.Max(0f, invulnerableTimer - deltaTime);
            }
        }
    }
}
