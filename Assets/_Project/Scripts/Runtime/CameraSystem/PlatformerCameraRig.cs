using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Core;
using UnityEngine;

namespace MatriculaDeHonor.CameraSystem
{
    /// <summary>
    /// Cámara semifija típica de plataformas 3D por niveles: ángulo constante, sigue al jugador
    /// con suavizado y se adelanta un poco hacia donde corre. El ángulo fijo hace que el control
    /// relativo a cámara sea estable. Más adelante se puede cambiar por Cinemachine.
    /// </summary>
    public sealed class PlatformerCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 7f, -9f);
        [SerializeField, Min(0f)] private float lookHeight = 1f;
        [SerializeField, Min(0f)] private float smoothTime = 0.18f;
        [SerializeField, Min(0f)] private float lookAheadSeconds = 0.25f;
        [Tooltip("Si el objetivo se aleja más que esto (respawn), la cámara salta en vez de deslizarse.")]
        [SerializeField, Min(1f)] private float snapDistance = 15f;

        private IPlayerTarget playerTarget;
        private Vector3 velocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            playerTarget = newTarget != null ? newTarget.GetComponent<IPlayerTarget>() : null;
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null && ServiceLocator.TryGet(out IPlayerTarget located))
            {
                playerTarget = located;
                target = located.Transform;
                SnapToTarget();
            }

            if (target == null) return;

            Vector3 lookAhead = Vector3.zero;
            if (playerTarget != null)
            {
                Vector3 flatVelocity = playerTarget.Velocity;
                flatVelocity.y = 0f;
                lookAhead = flatVelocity * lookAheadSeconds;
            }

            Vector3 desired = target.position + offset + lookAhead;
            if ((desired - transform.position).sqrMagnitude > snapDistance * snapDistance)
            {
                SnapToTarget();
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.rotation = FixedRotation();
        }

        private void SnapToTarget()
        {
            if (target == null) return;

            velocity = Vector3.zero;
            transform.SetPositionAndRotation(target.position + offset, FixedRotation());
        }

        private Quaternion FixedRotation()
        {
            Vector3 lookDirection = -offset + Vector3.up * lookHeight;
            return lookDirection.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(lookDirection) : Quaternion.identity;
        }
    }
}
