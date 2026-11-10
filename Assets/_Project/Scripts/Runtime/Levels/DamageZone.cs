using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Player;
using MatriculaDeHonor.World;
using UnityEngine;

namespace MatriculaDeHonor.Levels
{
    /// <summary>Zona que hace daño mientras estás dentro (charcos del laboratorio de Química...).</summary>
    public sealed class DamageZone : PlayerTriggerVolume
    {
        [SerializeField] private DamageKind kind = DamageKind.Chemical;
        [SerializeField, Min(1)] private int damage = 1;

        protected override void OnPlayerStay(PlayerController player)
        {
            // La invulnerabilidad del jugador tras un golpe ya limita la frecuencia del daño.
            player.TakeDamage(new DamageInfo(damage, kind, Faction.Environment, transform.position, gameObject));
        }
    }
}
