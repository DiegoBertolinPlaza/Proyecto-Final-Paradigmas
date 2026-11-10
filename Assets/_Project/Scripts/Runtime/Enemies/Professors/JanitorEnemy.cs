using MatriculaDeHonor.Enemies.Attacks;

namespace MatriculaDeHonor.Enemies.Professors
{
    /// <summary>
    /// Bedel (nivel 1): patrulla la entrada y te pilla por contacto.
    /// Fíjate en lo poco que ocupa: todo el comportamiento común está en EnemyBase y cada
    /// profesor solo rellena los ganchos que le hacen diferente (Template Method).
    /// </summary>
    public sealed class JanitorEnemy : EnemyBase
    {
        protected override IAttackStrategy CreateAttackStrategy()
        {
            return new ContactAttack(config.attackRange, config.attackCooldown, config.damage, hurtsFromAbove: !config.canBeStomped);
        }
    }
}
