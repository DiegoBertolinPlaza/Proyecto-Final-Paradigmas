using MatriculaDeHonor.Enemies.Attacks;

namespace MatriculaDeHonor.Enemies.Professors
{
    /// <summary>
    /// Bibliotecaria (nivel 5): oído finísimo y poca vista. Cuando te detecta grita
    /// "¡SILENCIO!" y alerta a todos los profesores cercanos (AlarmRaisedEvent).
    /// </summary>
    public sealed class LibrarianProfessor : EnemyBase
    {
        protected override IAttackStrategy CreateAttackStrategy()
        {
            return new ContactAttack(config.attackRange, config.attackCooldown, config.damage, hurtsFromAbove: !config.canBeStomped);
        }

        protected override void OnTargetSpotted()
        {
            RaiseAlarm();
        }
    }
}
