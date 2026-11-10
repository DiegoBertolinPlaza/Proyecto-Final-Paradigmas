using MatriculaDeHonor.Player;
using UnityEngine;

namespace MatriculaDeHonor.Items
{
    public sealed class PowerUpPickup : Collectible
    {
        [SerializeField] private PowerUpDefinition powerUp;

        protected override void OnCollected(PlayerController player)
        {
            if (powerUp == null)
            {
                Debug.LogWarning("[PowerUpPickup] No tiene PowerUpDefinition asignado.", this);
                return;
            }

            player.ApplyPowerUp(powerUp.HasStatModifiers ? powerUp : null, powerUp.ShieldCharges);
        }
    }
}
