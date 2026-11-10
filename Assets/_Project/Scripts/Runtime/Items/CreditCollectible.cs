using MatriculaDeHonor.Core;
using MatriculaDeHonor.Player;
using UnityEngine;

namespace MatriculaDeHonor.Items
{
    /// <summary>Crédito ECTS (la "moneda"). Cada 100 créditos, una convocatoria extra.</summary>
    public sealed class CreditCollectible : Collectible
    {
        [SerializeField, Min(1)] private int amount = 1;

        protected override void OnCollected(PlayerController player)
        {
            EventBus<CreditPickedUpEvent>.Raise(new CreditPickedUpEvent(amount));
        }
    }
}
