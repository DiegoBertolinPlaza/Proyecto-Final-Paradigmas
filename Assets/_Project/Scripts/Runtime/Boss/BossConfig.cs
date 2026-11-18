using UnityEngine;

namespace MatriculaDeHonor.Boss
{
    [CreateAssetMenu(menuName = "Matrícula de Honor/Jefe/Configuración", fileName = "BossConfig")]
    public sealed class BossConfig : ScriptableObject
    {
        public string displayName = "El Tribunal";

        [Header("Vida")]
        [Min(1)] public int maxHealth = 3;
        [Min(0f)] public float hitInvulnerability = 1f;
        [Tooltip("Por debajo de esta fracción de vida se enfada: ataques más frecuentes.")]
        [Range(0f, 1f)] public float enragedHealthFraction = 0.34f;
        [Range(0.1f, 1f)] public float enragedCooldownMultiplier = 0.65f;

        [Header("Movimiento")]
        [Min(0f)] public float moveSpeed = 5.5f;
        [Min(0f)] public float turnSpeedDegrees = 540f;
        [Min(0f)] public float gravity = 30f;

        [Header("Lanzamiento")]
        [Min(0f)] public float throwCooldown = 1.6f;
        [Min(0.1f)] public float projectileSpeed = 14f;
        [Range(0f, 1f)] public float aimSkill = 0.8f;
        [Min(0)] public int projectileDamage = 1;

        [Header("Embestida")]
        [Min(0f)] public float chargeCooldown = 4f;
        [Min(0f)] public float chargeSpeed = 15f;
        [Min(0f)] public float chargeDuration = 0.6f;
        [Min(0)] public int contactDamage = 1;
        [Min(0f)] public float contactRadius = 1.3f;

        [Header("Normalización de observaciones")]
        [Tooltip("Velocidad máxima esperada del jugador; se usa para normalizar su velocidad.")]
        [Min(0.1f)] public float maxPlayerSpeed = 12f;
    }
}
