using MatriculaDeHonor.Core;
using MatriculaDeHonor.Player;
using UnityEngine;

namespace MatriculaDeHonor.Items
{
    /// <summary>Matrícula de Honor: el coleccionable escondido (3 por nivel).</summary>
    public sealed class HonorCollectible : Collectible
    {
        [Tooltip("Índice dentro del nivel (0, 1, 2) para guardar cuáles se han conseguido.")]
        [SerializeField, Min(0)] private int index;

        protected override void OnCollected(PlayerController player)
        {
            EventBus<HonorPickedUpEvent>.Raise(new HonorPickedUpEvent(index));
        }
    }
}
