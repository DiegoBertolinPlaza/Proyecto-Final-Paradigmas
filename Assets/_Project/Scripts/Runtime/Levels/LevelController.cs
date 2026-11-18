using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Core;
using MatriculaDeHonor.Core.Pooling;
using MatriculaDeHonor.Player;
using UnityEngine;

namespace MatriculaDeHonor.Levels
{
    /// <summary>
    /// Raíz de composición de cada escena de nivel: crea y registra los servicios que viven lo
    /// que dura el nivel (pools, fábrica de proyectiles) y gestiona checkpoints y reapariciones.
    /// Al destruirse la escena lo da todo de baja, así "volver a jugar" empieza limpio.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class LevelController : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform playerSpawn;

        private PoolService pools;
        private ProjectileFactory projectileFactory;
        private Vector3 respawnPosition;
        private Quaternion respawnRotation;

        public PlayerController Player => player;

        private void Awake()
        {
            var poolRoot = new GameObject("[Pools]").transform;
            poolRoot.SetParent(transform, false);

            pools = new PoolService(poolRoot);
            projectileFactory = new ProjectileFactory(pools);

            ServiceLocator.Register(this);
            ServiceLocator.Register(pools);
            ServiceLocator.Register(projectileFactory);

            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }

            Transform spawn = playerSpawn != null ? playerSpawn : player != null ? player.transform : transform;
            respawnPosition = spawn.position;
            respawnRotation = spawn.rotation;
        }

        private void OnEnable()
        {
            EventBus<CheckpointReachedEvent>.Subscribe(OnCheckpointReached);
            EventBus<RespawnRequestedEvent>.Subscribe(OnRespawnRequested);
        }

        private void OnDisable()
        {
            EventBus<CheckpointReachedEvent>.Unsubscribe(OnCheckpointReached);
            EventBus<RespawnRequestedEvent>.Unsubscribe(OnRespawnRequested);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
            ServiceLocator.Unregister(pools);
            ServiceLocator.Unregister(projectileFactory);
            pools.Dispose();
        }

        private void OnCheckpointReached(CheckpointReachedEvent checkpoint)
        {
            respawnPosition = checkpoint.RespawnPosition;
            respawnRotation = checkpoint.RespawnRotation;
        }

        private void OnRespawnRequested(RespawnRequestedEvent _)
        {
            if (player != null)
            {
                player.Respawn(respawnPosition, respawnRotation);
            }
        }
    }
}
