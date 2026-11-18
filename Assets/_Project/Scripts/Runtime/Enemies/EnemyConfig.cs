using UnityEngine;

namespace MatriculaDeHonor.Enemies
{
    /// <summary>
    /// Parámetros de un tipo de profesor. La dificultad de cada nivel se ajusta aquí
    /// (más visión, más oído, mejor puntería...) sin tocar el código de la IA.
    /// </summary>
    [CreateAssetMenu(menuName = "Matrícula de Honor/Enemigos/Configuración de profesor", fileName = "EnemyConfig")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [Header("Identidad")]
        public string displayName = "Profesor";

        [Header("Movimiento")]
        [Min(0f)] public float patrolSpeed = 2f;
        [Min(0f)] public float chaseSpeed = 4.5f;
        [Min(0f)] public float waypointWaitTime = 1f;
        [Min(0f)] public float turnSpeedDegrees = 360f;

        [Header("Vista")]
        [Min(0f)] public float visionRange = 10f;
        [Range(1f, 360f)] public float visionAngle = 110f;

        [Header("Oído")]
        [Tooltip("Multiplica el radio de los ruidos. La bibliotecaria tiene mucho.")]
        [Min(0f)] public float hearingSensitivity = 1f;

        [Header("Sospecha")]
        [Min(0f)] public float suspicionGainPerSecond = 1.2f;
        [Min(0f)] public float suspicionDecayPerSecond = 0.35f;
        [Tooltip("Cuánta sospecha suma un ruido justo al lado (escala con la distancia).")]
        [Range(0f, 1f)] public float noiseSuspicion = 0.6f;

        [Header("Persecución y búsqueda")]
        [Min(0f)] public float loseTargetAfter = 2.5f;
        [Min(0f)] public float investigateDuration = 3f;
        [Min(0f)] public float searchDuration = 4f;
        [Min(0f)] public float searchRadius = 4f;

        [Header("Ataque")]
        [Min(0f)] public float attackRange = 1.4f;
        [Min(0f)] public float attackCooldown = 1.2f;
        [Min(0)] public int damage = 1;
        [Min(0.1f)] public float projectileSpeed = 12f;
        [Tooltip("0 = apunta a donde estás, 1 = a donde vas a estar.")]
        [Range(0f, 1f)] public float aimSkill = 0.7f;

        [Header("Vida")]
        [Min(1)] public int maxHealth = 1;
        public bool canBeStomped = true;
        [Min(0f)] public float stunDuration = 1.2f;
        [Min(0f)] public float stompImmunityTime = 0.25f;

        [Header("Alarma")]
        [Tooltip("Radio en el que avisa a otros profesores al verte. 0 = no avisa.")]
        [Min(0f)] public float alarmRadius;
    }
}
