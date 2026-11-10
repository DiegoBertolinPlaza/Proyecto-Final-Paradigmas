using System;
using UnityEngine;

namespace MatriculaDeHonor.Boss
{
    /// <summary>
    /// TODO lo que el jefe sabe del mundo en un instante. Es el contrato común entre el cerebro
    /// programado y el agente de ML-Agents: los dos deciden con exactamente la misma información.
    ///
    /// Está expresado en el "marco del jefe mirando al jugador" (eje Z = hacia el jugador,
    /// eje X = a su derecha) y normalizado a [-1, 1] o [0, 1], que es lo que necesita una red
    /// neuronal para aprender bien y además hace la política independiente de dónde esté la arena.
    /// </summary>
    public readonly struct BossObservation
    {
        /// <summary>Número de floats que escribe <see cref="WriteTo"/>. Tiene que coincidir con
        /// "Vector Observation → Space Size" del componente Behavior Parameters.</summary>
        public const int Size = 14;

        public readonly bool HasTarget;
        /// <summary>Distancia horizontal al jugador / diagonal de la arena.</summary>
        public readonly float Distance01;
        /// <summary>Altura relativa del jugador (+1 = 5 m por encima).</summary>
        public readonly float TargetHeight;
        /// <summary>Velocidad del jugador en el marco del jefe / velocidad máxima del jugador.</summary>
        public readonly Vector3 TargetLocalVelocity;
        public readonly bool TargetIsAirborne;
        /// <summary>El jugador está justo encima: amenaza de pisotón.</summary>
        public readonly bool TargetIsAbove;
        public readonly float OwnHealth01;
        public readonly float TargetHealth01;
        public readonly float ThrowCooldown01;
        public readonly float ChargeCooldown01;
        public readonly bool IsCharging;
        /// <summary>Posición del jefe respecto al centro de la arena, en su marco (para no arrinconarse).</summary>
        public readonly Vector2 ArenaPosition;

        public BossObservation(
            bool hasTarget,
            float distance01,
            float targetHeight,
            Vector3 targetLocalVelocity,
            bool targetIsAirborne,
            bool targetIsAbove,
            float ownHealth01,
            float targetHealth01,
            float throwCooldown01,
            float chargeCooldown01,
            bool isCharging,
            Vector2 arenaPosition)
        {
            HasTarget = hasTarget;
            Distance01 = Mathf.Clamp01(distance01);
            TargetHeight = Mathf.Clamp(targetHeight, -1f, 1f);
            TargetLocalVelocity = Vector3.Max(-Vector3.one, Vector3.Min(Vector3.one, targetLocalVelocity));
            TargetIsAirborne = targetIsAirborne;
            TargetIsAbove = targetIsAbove;
            OwnHealth01 = Mathf.Clamp01(ownHealth01);
            TargetHealth01 = Mathf.Clamp01(targetHealth01);
            ThrowCooldown01 = Mathf.Clamp01(throwCooldown01);
            ChargeCooldown01 = Mathf.Clamp01(chargeCooldown01);
            IsCharging = isCharging;
            ArenaPosition = Vector2.Max(-Vector2.one, Vector2.Min(Vector2.one, arenaPosition));
        }

        /// <summary>Vuelca la observación en un buffer en orden fijo (el orden importa para la red).</summary>
        public void WriteTo(float[] buffer, int offset = 0)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length - offset < Size) throw new ArgumentException("Buffer demasiado pequeño.", nameof(buffer));

            int i = offset;
            buffer[i++] = Distance01;
            buffer[i++] = TargetHeight;
            buffer[i++] = TargetLocalVelocity.x;
            buffer[i++] = TargetLocalVelocity.y;
            buffer[i++] = TargetLocalVelocity.z;
            buffer[i++] = TargetIsAirborne ? 1f : 0f;
            buffer[i++] = TargetIsAbove ? 1f : 0f;
            buffer[i++] = OwnHealth01;
            buffer[i++] = TargetHealth01;
            buffer[i++] = ThrowCooldown01;
            buffer[i++] = ChargeCooldown01;
            buffer[i++] = IsCharging ? 1f : 0f;
            buffer[i++] = ArenaPosition.x;
            buffer[i++] = ArenaPosition.y;
        }
    }

    /// <summary>Lo que el jefe decide hacer este frame (en su marco: Y = hacia el jugador).</summary>
    public readonly struct BossCommand
    {
        public readonly Vector2 Move;
        public readonly bool Throw;
        public readonly bool Charge;

        public BossCommand(Vector2 move, bool throwProjectile, bool charge)
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            Throw = throwProjectile;
            Charge = charge;
        }

        public static BossCommand Idle => new BossCommand(Vector2.zero, false, false);
    }

    /// <summary>
    /// Cerebro del jefe (patrón Strategy). Hay dos implementaciones intercambiables:
    /// <see cref="ScriptedBossBrain"/> (reglas) y BossAgent (ML-Agents, en el ensamblado ML).
    /// Si el entrenamiento sale mal, el juego sigue funcionando con el programado.
    /// </summary>
    public interface IBossBrain
    {
        string DisplayName { get; }
        void Bind(BossController boss);
        BossCommand Decide(in BossObservation observation, float deltaTime);
    }
}
