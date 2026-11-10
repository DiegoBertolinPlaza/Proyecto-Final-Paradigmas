using MatriculaDeHonor.Boss;
using MatriculaDeHonor.Combat;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace MatriculaDeHonor.ML
{
    /// <summary>
    /// Cerebro del jefe entrenado con ML-Agents (PPO). Implementa la MISMA interfaz que el
    /// cerebro programado, así que el BossController no sabe cuál está usando.
    ///
    /// Observaciones (14 floats, ver BossObservation): distancia, altura relativa, velocidad del
    /// jugador en el marco del jefe, si el jugador está en el aire / encima, vidas de ambos,
    /// enfriamientos, si está embistiendo y su posición en la arena.
    ///
    /// Acciones: 2 continuas (moverse lateral / adelante-atrás) + 1 rama discreta de 3
    /// (nada / lanzar tiza / embestir).
    ///
    /// Recompensas: +1 al golpear al jugador, +1 si el jugador pierde toda la vida,
    /// −0.5 al recibir un pisotón, −1 al ser derrotado, y pequeñas penalizaciones por tiempo
    /// y por quedarse arrinconado (el "exploit" típico).
    ///
    /// Sin modelo asignado y sin entrenador conectado, ML-Agents usa Heuristic(), que delega en
    /// el cerebro programado: el juego funciona siempre.
    /// </summary>
    [RequireComponent(typeof(BossController))]
    [RequireComponent(typeof(DecisionRequester))]
    public sealed class BossAgent : Agent, IBossBrain
    {
        public const string BehaviorName = "TribunalBoss";
        public const int ContinuousActionCount = 2;
        public const int AttackBranchSize = 3;

        [Header("Recompensas")]
        [SerializeField] private float hitTargetReward = 1f;
        [SerializeField] private float targetDefeatedReward = 1f;
        [SerializeField] private float damagedPenalty = -0.5f;
        [SerializeField] private float defeatedPenalty = -1f;
        [SerializeField] private float timePenaltyPerStep = -0.0005f;
        [SerializeField] private float cornerPenaltyPerStep = -0.001f;
        [SerializeField, Range(0f, 1f)] private float cornerThreshold = 0.85f;

        private readonly float[] observationBuffer = new float[BossObservation.Size];
        private BossController boss;
        private ScriptedBossBrain heuristicBrain;
        private BossTrainingArena trainingArena;
        private BossCommand lastCommand = BossCommand.Idle;

        public string DisplayName => "ML-Agents";

        // ---------- IBossBrain ----------

        public void Bind(BossController controller)
        {
            boss = controller;
        }

        public BossCommand Decide(in BossObservation observation, float deltaTime) => lastCommand;

        // ---------- Agent ----------

        public override void Initialize()
        {
            if (boss == null) boss = GetComponent<BossController>();

            heuristicBrain = new ScriptedBossBrain();
            heuristicBrain.Bind(boss);
            trainingArena = GetComponentInParent<BossTrainingArena>();

            boss.HitTarget += OnHitTarget;
            boss.Damaged += OnDamaged;
            boss.Defeated += OnDefeated;
        }

        public override void OnEpisodeBegin()
        {
            lastCommand = BossCommand.Idle;

            if (trainingArena != null)
            {
                trainingArena.ResetEpisode();
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            boss.BuildObservation().WriteTo(observationBuffer);
            for (int i = 0; i < observationBuffer.Length; i++)
            {
                sensor.AddObservation(observationBuffer[i]);
            }
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var continuous = actions.ContinuousActions;
            var discrete = actions.DiscreteActions;

            var move = new Vector2(Mathf.Clamp(continuous[0], -1f, 1f), Mathf.Clamp(continuous[1], -1f, 1f));
            int attack = discrete[0];
            lastCommand = new BossCommand(move, attack == 1, attack == 2);

            AddReward(timePenaltyPerStep);

            var observation = boss.BuildObservation();
            if (Mathf.Abs(observation.ArenaPosition.x) > cornerThreshold ||
                Mathf.Abs(observation.ArenaPosition.y) > cornerThreshold)
            {
                AddReward(cornerPenaltyPerStep);
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var command = heuristicBrain.Decide(boss.BuildObservation(), Time.fixedDeltaTime);

            var continuous = actionsOut.ContinuousActions;
            continuous[0] = command.Move.x;
            continuous[1] = command.Move.y;

            var discrete = actionsOut.DiscreteActions;
            discrete[0] = command.Throw ? 1 : command.Charge ? 2 : 0;
        }

        /// <summary>Lo llama la arena cuando el jugador bot pierde toda la vida.</summary>
        public void NotifyTargetDefeated()
        {
            AddReward(targetDefeatedReward);
            EndEpisode();
        }

        private void OnHitTarget() => AddReward(hitTargetReward);

        private void OnDamaged(DamageInfo _) => AddReward(damagedPenalty);

        private void OnDefeated()
        {
            AddReward(defeatedPenalty);
            EndEpisode();
        }

        // Al añadir el componente en el editor, deja configurado Behavior Parameters.
        private void Reset()
        {
            var parameters = GetComponent<BehaviorParameters>();
            if (parameters == null) return;

            parameters.BehaviorName = BehaviorName;
            parameters.BrainParameters.VectorObservationSize = BossObservation.Size;
            parameters.BrainParameters.NumStackedVectorObservations = 1;
            parameters.BrainParameters.ActionSpec = new ActionSpec(ContinuousActionCount, new[] { AttackBranchSize });
        }
    }
}
