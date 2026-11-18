using UnityEngine;

namespace MatriculaDeHonor.Combat
{
    public enum Faction
    {
        Student,
        Professor,
        Environment
    }

    public enum DamageKind
    {
        Contact,
        Projectile,
        Stomp,
        Hazard,
        Chemical,
        Fall
    }

    public readonly struct DamageInfo
    {
        public readonly int Amount;
        public readonly DamageKind Kind;
        public readonly Faction Faction;
        public readonly Vector3 SourcePosition;
        public readonly GameObject Source;

        public DamageInfo(int amount, DamageKind kind, Faction faction, Vector3 sourcePosition, GameObject source)
        {
            Amount = amount;
            Kind = kind;
            Faction = faction;
            SourcePosition = sourcePosition;
            Source = source;
        }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(in DamageInfo damage);
    }

    /// <summary>Algo que se puede pisar desde arriba (los profesores, el jefe...).</summary>
    public interface IStompable
    {
        /// <summary>Devuelve true si acepta el pisotón (y por tanto el jugador debe rebotar).</summary>
        bool TryStomp(GameObject stomper, bool isGroundPound);
    }

    /// <summary>
    /// Lo que la IA necesita saber del jugador. Vive en Combat (no en Player) para que los
    /// enemigos y el jefe no dependan de la implementación concreta del jugador.
    /// </summary>
    public interface IPlayerTarget : IDamageable
    {
        Transform Transform { get; }
        Vector3 Position { get; }

        /// <summary>Punto al que apuntar (el pecho, no los pies).</summary>
        Vector3 AimPoint { get; }

        Vector3 Velocity { get; }
        bool IsAirborne { get; }
        float Health01 { get; }

        /// <summary>Se lanza cuando el jugador recibe daño de verdad (permite al jefe saber que ha acertado).</summary>
        event System.Action<DamageInfo> Damaged;
    }
}
