using UnityEngine;

namespace MatriculaDeHonor.Enemies.Perception
{
    /// <summary>
    /// Cono de visión: rango + ángulo horizontal + línea de visión (las paredes tapan).
    /// El ángulo se mide en el plano horizontal para que un plataformas en vertical no deje
    /// "ciego" al profesor cuando el jugador está un piso por encima.
    /// </summary>
    public sealed class VisionSensor
    {
        private readonly LayerMask obstacleMask;

        public VisionSensor(float range, float fullAngleDegrees, LayerMask obstacleMask)
        {
            Range = Mathf.Max(0f, range);
            HalfAngle = Mathf.Clamp(fullAngleDegrees * 0.5f, 0f, 180f);
            this.obstacleMask = obstacleMask;
        }

        public float Range { get; }
        public float HalfAngle { get; }

        /// <summary>Solo geometría (sin física): útil para tests y para dibujar gizmos.</summary>
        public static bool IsInsideCone(Vector3 eye, Vector3 forward, Vector3 point, float range, float halfAngle)
        {
            Vector3 toPoint = point - eye;
            if (toPoint.sqrMagnitude > range * range) return false;

            Vector3 flatToPoint = new Vector3(toPoint.x, 0f, toPoint.z);
            Vector3 flatForward = new Vector3(forward.x, 0f, forward.z);
            if (flatToPoint.sqrMagnitude < 0.0001f || flatForward.sqrMagnitude < 0.0001f) return true;

            return Vector3.Angle(flatForward, flatToPoint) <= halfAngle;
        }

        public bool CanSee(Vector3 eye, Vector3 forward, Transform target, Vector3 targetPoint)
        {
            if (target == null) return false;
            if (!IsInsideCone(eye, forward, targetPoint, Range, HalfAngle)) return false;

            Vector3 toTarget = targetPoint - eye;
            float distance = toTarget.magnitude;
            if (distance < 0.001f) return true;

            if (!Physics.Raycast(eye, toTarget / distance, out var hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            // Lo primero que toca el rayo es el propio jugador: no hay nada en medio.
            return hit.transform == target || hit.transform.IsChildOf(target);
        }
    }
}
