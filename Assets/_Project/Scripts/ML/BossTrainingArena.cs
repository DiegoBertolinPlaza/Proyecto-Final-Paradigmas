using MatriculaDeHonor.Boss;
using MatriculaDeHonor.Player;
using MatriculaDeHonor.Player.Input;
using UnityEngine;

namespace MatriculaDeHonor.ML
{
    /// <summary>
    /// Una arena de entrenamiento: jefe + jugador bot. Se duplica muchas veces en la escena de
    /// entrenamiento para que ML-Agents recoja experiencia en paralelo. Todo se comunica con
    /// eventos de instancia (no globales) para que las arenas no se pisen entre sí.
    /// </summary>
    public sealed class BossTrainingArena : MonoBehaviour
    {
        [SerializeField] private BossArena area;
        [SerializeField] private BossController boss;
        [SerializeField] private BossAgent agent;
        [SerializeField] private PlayerController playerBot;
        [SerializeField] private PlayerBotInputSource botInput;
        [SerializeField, Min(0f)] private float spawnMargin = 2f;
        [SerializeField, Min(0f)] private float minSpawnSeparation = 6f;

        private void Start()
        {
            if (botInput != null)
            {
                botInput.SetTarget(boss.transform);
                playerBot.SetInputSource(botInput);
            }

            playerBot.Died += OnPlayerBotDied;
        }

        private void OnDestroy()
        {
            if (playerBot != null) playerBot.Died -= OnPlayerBotDied;
        }

        public void ResetEpisode()
        {
            Vector3 bossPosition = area.RandomPointInside(spawnMargin);
            Vector3 playerPosition = bossPosition;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                playerPosition = area.RandomPointInside(spawnMargin);
                if ((playerPosition - bossPosition).sqrMagnitude >= minSpawnSeparation * minSpawnSeparation) break;
            }

            Vector3 lift = Vector3.up * 0.1f;
            boss.ResetBoss(bossPosition + lift, Quaternion.LookRotation(Flat(playerPosition - bossPosition)));
            playerBot.Respawn(playerPosition + lift, Quaternion.LookRotation(Flat(bossPosition - playerPosition)));

            if (botInput != null) botInput.Randomize();
        }

        private void OnPlayerBotDied(PlayerController _)
        {
            agent.NotifyTargetDefeated();
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }
    }
}
