using System;
using UnityEngine;

namespace MatriculaDeHonor.Player
{
    /// <summary>
    /// Física del personaje sobre un CharacterController: integra velocidad y gravedad, detecta
    /// suelo y lleva la cuenta del "coyote time". No sabe nada de input ni de estados.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        private const float GroundStickVelocity = -2f;

        private CharacterController characterController;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        public bool IsGrounded { get; private set; }
        public float LastGroundedTime { get; private set; } = float.NegativeInfinity;
        public float VerticalVelocity => verticalVelocity;
        public Vector3 HorizontalVelocity => horizontalVelocity;
        public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalVelocity;

        /// <summary>Multiplicador de gravedad que fijan los estados (caída, salto corto...).</summary>
        public float GravityScale { get; set; } = 1f;

        public bool GravityEnabled { get; set; } = true;

        /// <summary>Velocidad vertical en el momento de aterrizar (negativa).</summary>
        public event Action<float> Landed;

        /// <summary>Choques del CharacterController (para pisotones).</summary>
        public event Action<ControllerColliderHit> Hit;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        /// <summary>Acelera la velocidad horizontal hacia la deseada.</summary>
        public void Accelerate(Vector3 desiredHorizontalVelocity, float acceleration, float deltaTime)
        {
            desiredHorizontalVelocity.y = 0f;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredHorizontalVelocity, acceleration * deltaTime);
        }

        public void SetHorizontalVelocity(Vector3 velocity)
        {
            velocity.y = 0f;
            horizontalVelocity = velocity;
        }

        public void SetVerticalVelocity(float velocity)
        {
            verticalVelocity = velocity;
            if (velocity > 0f)
            {
                IsGrounded = false;
            }
        }

        public void FaceDirection(Vector3 direction, float degreesPerSecond, float deltaTime)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            var target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * deltaTime);
        }

        /// <summary>Integra gravedad y mueve. Lo llama el PlayerController una vez por frame.</summary>
        public void Step(float deltaTime, float gravity, float maxFallSpeed)
        {
            if (GravityEnabled)
            {
                verticalVelocity -= gravity * GravityScale * deltaTime;
                verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
            }

            bool wasGrounded = IsGrounded;
            float impactVelocity = verticalVelocity;

            characterController.Move(Velocity * deltaTime);

            IsGrounded = characterController.isGrounded && verticalVelocity <= 0f;

            if (IsGrounded)
            {
                LastGroundedTime = Time.time;
                verticalVelocity = GroundStickVelocity;

                if (!wasGrounded)
                {
                    Landed?.Invoke(impactVelocity);
                }
            }
            else if ((characterController.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            {
                // Cabezazo contra el techo: cortamos la subida.
                verticalVelocity = 0f;
            }
        }

        /// <summary>Mueve al personaje sin física (respawn, inicio de episodio de entrenamiento...).</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = true;

            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            GravityScale = 1f;
            GravityEnabled = true;
            IsGrounded = false;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Hit?.Invoke(hit);
        }
    }
}
