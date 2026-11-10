using MatriculaDeHonor.Core;
using MatriculaDeHonor.Player;
using MatriculaDeHonor.World;
using UnityEngine;

namespace MatriculaDeHonor.Levels
{
    /// <summary>Meta del nivel ("entregar el examen"). Opcionalmente bloqueada hasta vencer al jefe.</summary>
    public sealed class LevelGoal : PlayerTriggerVolume
    {
        [SerializeField] private bool lockedUntilBossDefeated;
        [SerializeField] private GameObject visual;

        private bool locked;
        private bool reached;

        protected override void Awake()
        {
            base.Awake();
            locked = lockedUntilBossDefeated;
            if (visual != null) visual.SetActive(!locked);
        }

        private void OnEnable() => EventBus<BossDefeatedEvent>.Subscribe(OnBossDefeated);
        private void OnDisable() => EventBus<BossDefeatedEvent>.Unsubscribe(OnBossDefeated);

        protected override void OnPlayerEnter(PlayerController player)
        {
            if (locked || reached || !player.IsMainPlayer || !player.IsAlive) return;

            reached = true;
            EventBus<LevelGoalReachedEvent>.Raise(new LevelGoalReachedEvent());
        }

        private void OnBossDefeated(BossDefeatedEvent _)
        {
            locked = false;
            if (visual != null) visual.SetActive(true);
        }
    }
}
