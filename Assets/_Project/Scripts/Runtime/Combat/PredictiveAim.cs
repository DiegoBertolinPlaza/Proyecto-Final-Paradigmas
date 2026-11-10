using UnityEngine;

namespace MatriculaDeHonor.Combat
{
    /// <summary>
    /// Puntería predictiva: calcula hacia dónde disparar para interceptar a un objetivo que se
    /// mueve con velocidad constante. Es la "decisión" que hace que el profe de Cálculo apunte
    /// a donde VAS a estar y no a donde estás.
    ///
    /// Se resuelve |D + V·t| = s·t, con D = objetivo − tirador, V = velocidad del objetivo y
    /// s = velocidad del proyectil, que da la cuadrática (V·V − s²)t² + 2(V·D)t + D·D = 0.
    /// </summary>
    public static class PredictiveAim
    {
        private const float Epsilon = 1e-5f;

        public static bool TrySolveIntercept(
            Vector3 shooterPosition,
            Vector3 targetPosition,
            Vector3 targetVelocity,
            float projectileSpeed,
            out Vector3 aimPoint,
            out float timeToImpact)
        {
            aimPoint = targetPosition;
            timeToImpact = 0f;

            if (projectileSpeed <= Epsilon)
            {
                return false;
            }

            Vector3 toTarget = targetPosition - shooterPosition;
            float a = Vector3.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
            float b = 2f * Vector3.Dot(targetVelocity, toTarget);
            float c = Vector3.Dot(toTarget, toTarget);

            float t;
            if (Mathf.Abs(a) < Epsilon)
            {
                // Proyectil y objetivo igual de rápidos: la ecuación se vuelve lineal.
                if (Mathf.Abs(b) < Epsilon) return false;
                t = -c / b;
            }
            else
            {
                float discriminant = b * b - 4f * a * c;
                if (discriminant < 0f) return false;

                float root = Mathf.Sqrt(discriminant);
                float t1 = (-b - root) / (2f * a);
                float t2 = (-b + root) / (2f * a);
                t = SmallestPositive(t1, t2);
            }

            if (t <= 0f || float.IsNaN(t) || float.IsInfinity(t))
            {
                return false;
            }

            timeToImpact = t;
            aimPoint = targetPosition + targetVelocity * t;
            return true;
        }

        /// <summary>
        /// Mezcla entre apuntar directo (skill = 0) y la predicción perfecta (skill = 1).
        /// Es el parámetro que sube con la dificultad del nivel.
        /// </summary>
        public static Vector3 AimDirection(
            Vector3 shooterPosition,
            Vector3 targetPosition,
            Vector3 targetVelocity,
            float projectileSpeed,
            float skill01)
        {
            Vector3 aimPoint = targetPosition;
            if (TrySolveIntercept(shooterPosition, targetPosition, targetVelocity, projectileSpeed, out var predicted, out _))
            {
                aimPoint = Vector3.Lerp(targetPosition, predicted, Mathf.Clamp01(skill01));
            }

            Vector3 direction = aimPoint - shooterPosition;
            return direction.sqrMagnitude > Epsilon ? direction.normalized : Vector3.forward;
        }

        private static float SmallestPositive(float a, float b)
        {
            if (a > 0f && b > 0f) return Mathf.Min(a, b);
            if (a > 0f) return a;
            return b;
        }
    }
}
