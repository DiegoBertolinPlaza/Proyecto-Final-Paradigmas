using MatriculaDeHonor.Player;
using MatriculaDeHonor.World;
using UnityEngine;

namespace MatriculaDeHonor.Items
{
    /// <summary>
    /// Base de los coleccionables (Template Method): se encarga de girar, detectar al jugador
    /// y desaparecer; cada subclase solo dice qué pasa al recogerlo.
    /// </summary>
    public abstract class Collectible : PlayerTriggerVolume
    {
        [SerializeField] private float spinDegreesPerSecond = 120f;

        private bool collected;

        protected virtual void Update()
        {
            if (spinDegreesPerSecond != 0f)
            {
                transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
            }
        }

        protected sealed override void OnPlayerEnter(PlayerController player)
        {
            if (collected || !player.IsAlive || !player.IsMainPlayer) return;

            collected = true;
            OnCollected(player);
            gameObject.SetActive(false);
        }

        protected abstract void OnCollected(PlayerController player);
    }
}
