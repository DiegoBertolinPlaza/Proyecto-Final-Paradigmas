using System.Collections.Generic;
using MatriculaDeHonor.Player.Stats;
using UnityEngine;

namespace MatriculaDeHonor.Player
{
    /// <summary>
    /// Todos los números del control del estudiante. Están en un ScriptableObject para poder
    /// ajustar el "game feel" en caliente sin tocar código y tener varios perfiles si hace falta.
    /// </summary>
    [CreateAssetMenu(menuName = "Matrícula de Honor/Jugador/Configuración", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Header("Movimiento en suelo")]
        [Min(0f)] public float walkSpeed = 5f;
        [Min(0f)] public float runSpeed = 8.5f;
        [Min(0f)] public float acceleration = 45f;
        [Min(0f)] public float deceleration = 60f;
        [Min(0f)] public float turnSpeedDegrees = 900f;

        [Header("Salto")]
        [Min(0f)] public float jumpHeight = 2.4f;
        [Range(0f, 1f)] public float airControl = 0.65f;
        [Min(0f)] public float gravity = 32f;
        [Tooltip("Gravedad extra al caer: hace el salto menos 'flotante'.")]
        [Min(1f)] public float fallGravityMultiplier = 1.6f;
        [Tooltip("Gravedad extra si se suelta el botón subiendo: salto de altura variable.")]
        [Min(1f)] public float lowJumpGravityMultiplier = 2.4f;
        [Min(0f)] public float maxFallSpeed = 28f;

        [Header("Asistencias (lo que hace que el control se sienta justo)")]
        [Tooltip("Margen para saltar justo después de salir de una plataforma.")]
        [Min(0f)] public float coyoteTime = 0.12f;
        [Tooltip("Margen para pulsar salto justo antes de tocar el suelo.")]
        [Min(0f)] public float jumpBufferTime = 0.12f;

        [Header("Pisotón y culetazo")]
        [Min(0f)] public float stompBounceHeight = 1.6f;
        [Min(0f)] public float stompBounceHeightHeld = 3.2f;
        [Min(0f)] public float groundPoundHangTime = 0.18f;
        [Min(0f)] public float groundPoundSpeed = 30f;

        [Header("Vida")]
        [Min(1)] public int maxHealth = 3;
        [Min(0)] public int maxShield = 1;
        [Min(0f)] public float invulnerabilityTime = 1.5f;
        [Min(0f)] public float hurtDuration = 0.35f;
        [Min(0f)] public float knockbackSpeed = 7f;

        [Header("Ruido (lo oyen los profesores)")]
        [Min(0f)] public float walkNoiseRadius = 2f;
        [Min(0f)] public float runNoiseRadius = 9f;
        [Min(0f)] public float landNoiseRadius = 6f;
        [Min(0f)] public float groundPoundNoiseRadius = 14f;
        [Min(0.05f)] public float footstepInterval = 0.35f;

        public float JumpVelocityFor(float height) => Mathf.Sqrt(2f * gravity * Mathf.Max(0f, height));

        public Dictionary<StatType, float> BuildBaseStats()
        {
            return new Dictionary<StatType, float>
            {
                { StatType.WalkSpeed, walkSpeed },
                { StatType.RunSpeed, runSpeed },
                { StatType.JumpHeight, jumpHeight },
                { StatType.NoiseMultiplier, 1f },
                { StatType.ChemicalResistance, 0f }
            };
        }
    }
}
