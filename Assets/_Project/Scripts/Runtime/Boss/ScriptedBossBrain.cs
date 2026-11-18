using UnityEngine;

namespace MatriculaDeHonor.Boss
{
    /// <summary>
    /// Cerebro por reglas del jefe. Es el "plan B" si el agente de ML no está entrenado, sirve
    /// como heurística para probar el agente a mano y como profesor para imitation learning.
    /// Solo usa la <see cref="BossObservation"/>: la misma información que la red neuronal.
    /// </summary>
    public sealed class ScriptedBossBrain : IBossBrain
    {
        private const float PreferredMinDistance = 5f;
        private const float PreferredMaxDistance = 9f;
        private const float WallThreshold = 0.8f;

        private float arenaDiagonal = 28f;
        private float strafeTimer;
        private float strafeSign = 1f;

        public string DisplayName => "Programado";

        public void Bind(BossController boss)
        {
            if (boss != null)
            {
                arenaDiagonal = boss.ArenaDiagonal;
            }
        }

        public BossCommand Decide(in BossObservation observation, float deltaTime)
        {
            if (!observation.HasTarget || observation.IsCharging)
            {
                return BossCommand.Idle;
            }

            float distance = observation.Distance01 * arenaDiagonal;
            Vector2 move = Vector2.zero;

            // Avanzar / retroceder para mantenerse a media distancia.
            if (observation.TargetIsAbove) move.y = -1f;
            else if (distance > PreferredMaxDistance) move.y = 1f;
            else if (distance < PreferredMinDistance) move.y = -0.8f;

            // Movimiento lateral cambiando de lado de vez en cuando (más difícil de pisar).
            strafeTimer -= deltaTime;
            if (strafeTimer <= 0f)
            {
                strafeTimer = Random.Range(1.2f, 2.8f);
                if (Random.value < 0.5f) strafeSign = -strafeSign;
            }

            move.x = strafeSign * 0.7f;

            // No arrinconarse: si una pared queda a un lado, se aleja de ella; si queda detrás, no retrocede.
            if (Mathf.Abs(observation.ArenaPosition.x) > WallThreshold)
            {
                strafeSign = -Mathf.Sign(observation.ArenaPosition.x);
                move.x = strafeSign;
            }

            if (observation.ArenaPosition.y < -WallThreshold)
            {
                move.y = Mathf.Max(move.y, 0.3f);
            }

            bool throwNow = observation.ThrowCooldown01 <= 0f && distance > 3f;
            bool chargeNow = observation.ChargeCooldown01 <= 0f &&
                             !observation.TargetIsAirborne &&
                             distance > 3f && distance < 10f;

            return new BossCommand(move, throwNow, chargeNow);
        }
    }
}
