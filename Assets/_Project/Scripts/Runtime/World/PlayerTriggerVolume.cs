using MatriculaDeHonor.Player;
using UnityEngine;

namespace MatriculaDeHonor.World
{
    /// <summary>
    /// Base de todo lo que reacciona cuando el estudiante entra en una zona: monedas, power-ups,
    /// checkpoints, la meta, el vacío, los charcos... (Template Method con ganchos OnPlayerX).
    /// Se asegura de que el collider sea trigger y añade un Rigidbody cinemático para que Unity
    /// envíe los mensajes de trigger con el CharacterController.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class PlayerTriggerVolume : MonoBehaviour
    {
        protected virtual void Awake()
        {
            GetComponent<Collider>().isTrigger = true;

            if (!TryGetComponent(out Rigidbody body))
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.isKinematic = true;
            body.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (TryGetPlayer(other, out var player)) OnPlayerEnter(player);
        }

        private void OnTriggerStay(Collider other)
        {
            if (TryGetPlayer(other, out var player)) OnPlayerStay(player);
        }

        private void OnTriggerExit(Collider other)
        {
            if (TryGetPlayer(other, out var player)) OnPlayerExit(player);
        }

        protected virtual void OnPlayerEnter(PlayerController player) { }
        protected virtual void OnPlayerStay(PlayerController player) { }
        protected virtual void OnPlayerExit(PlayerController player) { }

        private static bool TryGetPlayer(Collider other, out PlayerController player)
        {
            player = other.GetComponentInParent<PlayerController>();
            return player != null;
        }
    }
}
