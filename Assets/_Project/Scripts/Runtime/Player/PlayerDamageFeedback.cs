using MatriculaDeHonor.Combat;
using UnityEngine;

namespace MatriculaDeHonor.Player
{
    /// <summary>
    /// Hace parpadear al estudiante mientras es invulnerable tras un golpe, para que se vea que
    /// durante ese rato no le pueden volver a dar. Escucha el evento Damaged del PlayerController
    /// (Observer): el controlador no sabe que existe.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [DisallowMultipleComponent]
    public sealed class PlayerDamageFeedback : MonoBehaviour
    {
        [Tooltip("Lo que parpadea. Vacío = todos los Renderer del jugador y sus hijos.")]
        [SerializeField] private Renderer[] renderers;

        [SerializeField, Min(0.02f)] private float blinkInterval = 0.1f;

        private PlayerController player;
        private float blinkTimer;
        private bool blinking;

        public bool IsBlinking => blinking;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void OnEnable()
        {
            player.Damaged += OnDamaged;
            player.Respawned += OnRespawned;
        }

        private void OnDisable()
        {
            player.Damaged -= OnDamaged;
            player.Respawned -= OnRespawned;
            StopBlinking();
        }

        private void Update()
        {
            if (!blinking) return;

            if (!player.IsAlive || !player.Health.IsInvulnerable)
            {
                StopBlinking();
                return;
            }

            blinkTimer -= Time.deltaTime;
            if (blinkTimer > 0f) return;

            blinkTimer = blinkInterval;
            SetVisible(!IsVisible());
        }

        private void OnDamaged(DamageInfo damage)
        {
            if (!player.IsAlive || !player.Health.IsInvulnerable) return;

            blinking = true;
            blinkTimer = blinkInterval;
            SetVisible(false);
        }

        private void OnRespawned(PlayerController _) => StopBlinking();

        private void StopBlinking()
        {
            blinking = false;
            SetVisible(true);
        }

        private bool IsVisible() => renderers.Length == 0 || renderers[0] == null || renderers[0].enabled;

        private void SetVisible(bool visible)
        {
            foreach (var item in renderers)
            {
                if (item != null) item.enabled = visible;
            }
        }
    }
}
