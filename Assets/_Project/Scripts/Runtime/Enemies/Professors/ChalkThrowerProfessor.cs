using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Enemies.Attacks;
using UnityEngine;

namespace MatriculaDeHonor.Enemies.Professors
{
    /// <summary>Profe de Cálculo (nivel 2+): te lanza tizas apuntando a donde vas a estar.</summary>
    public sealed class ChalkThrowerProfessor : EnemyBase
    {
        [SerializeField] private Projectile chalkPrefab;

        protected override IAttackStrategy CreateAttackStrategy()
        {
            return new ProjectileAttack(
                chalkPrefab,
                config.attackRange,
                config.attackCooldown,
                config.damage,
                config.projectileSpeed,
                config.aimSkill);
        }
    }
}
