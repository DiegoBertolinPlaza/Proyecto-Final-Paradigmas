using System;
using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Core;
using MatriculaDeHonor.Enemies.Attacks;
using MatriculaDeHonor.Enemies.Perception;
using MatriculaDeHonor.Enemies.States;
using UnityEngine;
using UnityEngine.AI;

namespace MatriculaDeHonor.Enemies
{
    /// <summary>
    /// Base de todos los profesores (Template Method). Define el esqueleto común:
    /// percepción → estrategia de ataque → máquina de estados, y deja ganchos para que cada
    /// profesor concreto cambie solo lo que le hace distinto.
    ///
    /// IA por defecto (FSM):
    ///   Patrol ─(ruido / vistazo)→ Investigate ─(sospecha = 1)→ Chase ─(a tiro)→ Attack
    ///   Chase ─(lo pierde de vista)→ Search ─(se acaba el tiempo)→ Patrol
    ///   Cualquiera ─(golpe / pisotón)→ Stunned → Search
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable, IStompable
    {
        [SerializeField] protected EnemyConfig config;
        [SerializeField] private PatrolRoute patrolRoute;
        [Tooltip("Desde dónde mira. Vacío = el propio transform.")]
        [SerializeField] private Transform eye;
        [Tooltip("Desde dónde ataca / lanza. Vacío = un metro por encima de los pies.")]
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private LayerMask visionMask = Physics.DefaultRaycastLayers;

        private int health;
        private float stompImmunityTimer;
        private bool stunRequested;
        private float requestedStunDuration;

        public EnemyConfig Config => config;
        public NavMeshAgent Agent { get; private set; }
        public EnemyPerception Perception { get; private set; }
        public IAttackStrategy Attack { get; private set; }
        public StateMachine StateMachine { get; private set; }
        public PatrolRoute PatrolRoute => patrolRoute;
        public Vector3 SpawnPosition { get; private set; }
        public int Health => health;
        public bool IsDead => health <= 0;
        public bool IsAlive => !IsDead;
        public string CurrentStateName => IsDead ? "Dead" : StateMachine?.Current?.Name ?? "-";

        public Vector3 AttackOriginPosition =>
            attackOrigin != null ? attackOrigin.position : transform.position + Vector3.up;

        // Estados por defecto: protegidos para que las subclases puedan reutilizarlos.
        protected PatrolState StatePatrol { get; private set; }
        protected InvestigateState StateInvestigate { get; private set; }
        protected ChaseState StateChase { get; private set; }
        protected AttackState StateAttack { get; private set; }
        protected SearchState StateSearch { get; private set; }
        protected StunnedState StateStunned { get; private set; }

        public event Action<EnemyBase> Defeated;

        protected virtual void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"[{name}] Falta EnemyConfig; se usan valores por defecto.", this);
                config = ScriptableObject.CreateInstance<EnemyConfig>();
            }

            Agent = GetComponent<NavMeshAgent>();
            Agent.speed = config.patrolSpeed;
            Agent.angularSpeed = config.turnSpeedDegrees;

            Perception = new EnemyPerception(transform, eye, config, visionMask);
            Attack = CreateAttackStrategy();
            health = config.maxHealth;
            SpawnPosition = transform.position;

            CreateDefaultStates();
            StateMachine = new StateMachine();
            BuildStateMachine(StateMachine);
        }

        protected virtual void OnEnable()
        {
            EventBus<AlarmRaisedEvent>.Subscribe(OnAlarmRaised);
        }

        protected virtual void OnDisable()
        {
            EventBus<AlarmRaisedEvent>.Unsubscribe(OnAlarmRaised);
        }

        protected virtual void OnDestroy()
        {
            Perception?.Dispose();
        }

        protected virtual void Update()
        {
            if (IsDead) return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            if (stompImmunityTimer > 0f) stompImmunityTimer -= deltaTime;

            bool wasAlerted = Perception.IsAlerted;
            Perception.Tick(deltaTime);
            if (!wasAlerted && Perception.IsAlerted)
            {
                OnTargetSpotted();
            }

            Attack.Tick(deltaTime);
            StateMachine.Tick(deltaTime);
        }

        // ---------- Ganchos (Template Method) ----------

        /// <summary>Qué estrategia de ataque usa este profesor.</summary>
        protected abstract IAttackStrategy CreateAttackStrategy();

        /// <summary>Transiciones de la IA. Se puede sobrescribir para profesores con comportamiento propio.</summary>
        protected virtual void BuildStateMachine(StateMachine fsm)
        {
            fsm.AddAnyTransition(StateStunned, () => stunRequested);
            fsm.AddTransition(StateStunned, StateSearch, () => StateStunned.IsFinished);

            fsm.AddTransition(StatePatrol, StateChase, () => Perception.IsAlerted);
            fsm.AddTransition(StatePatrol, StateInvestigate, () => Perception.HasPointOfInterest);

            fsm.AddTransition(StateInvestigate, StateChase, () => Perception.IsAlerted);
            fsm.AddTransition(StateInvestigate, StatePatrol, () => StateInvestigate.IsFinished);

            fsm.AddTransition(StateChase, StatePatrol, () => !Perception.HasLivingTarget);
            fsm.AddTransition(StateChase, StateAttack, () => Perception.CanSeeTarget && IsTargetInAttackRange(1f));
            fsm.AddTransition(StateChase, StateSearch, () => Perception.TimeSinceTargetSeen > config.loseTargetAfter);

            fsm.AddTransition(StateAttack, StatePatrol, () => !Perception.HasLivingTarget);
            fsm.AddTransition(StateAttack, StateChase, () => !Perception.CanSeeTarget || !IsTargetInAttackRange(1.25f));

            fsm.AddTransition(StateSearch, StateChase, () => Perception.IsAlerted && Perception.CanSeeTarget);
            fsm.AddTransition(StateSearch, StatePatrol, () => StateSearch.IsFinished);

            fsm.SetState(StatePatrol);
        }

        /// <summary>Se llama al pasar a "alerta" (sospecha llena).</summary>
        protected virtual void OnTargetSpotted() { }

        /// <summary>Qué pasa al derrotarlo. Por defecto desaparece.</summary>
        protected virtual void OnDefeated()
        {
            gameObject.SetActive(false);
        }

        // ---------- API para los estados ----------

        public bool CanNavigate => Agent != null && Agent.enabled && Agent.isOnNavMesh;

        public bool HasArrived =>
            CanNavigate && !Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance + 0.2f;

        public void MoveTo(Vector3 destination, float speed)
        {
            if (!CanNavigate) return;

            Agent.isStopped = false;
            Agent.speed = speed;
            Agent.SetDestination(destination);
        }

        public void StopMoving()
        {
            if (!CanNavigate) return;

            Agent.isStopped = true;
            Agent.ResetPath();
        }

        public void FaceTowards(Vector3 point, float deltaTime)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            var target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, config.turnSpeedDegrees * deltaTime);
        }

        public bool IsTargetInAttackRange(float tolerance)
        {
            if (!Perception.HasLivingTarget) return false;

            float range = Attack.Range * tolerance;
            return (Perception.Target.Position - AttackOriginPosition).sqrMagnitude <= range * range;
        }

        public AttackContext BuildAttackContext()
        {
            return new AttackContext(gameObject, AttackOriginPosition, Perception.Target);
        }

        public void Stun(float seconds)
        {
            stunRequested = true;
            requestedStunDuration = seconds;
        }

        public float ConsumeStunRequest()
        {
            stunRequested = false;
            return requestedStunDuration;
        }

        // ---------- IDamageable / IStompable ----------

        public void TakeDamage(in DamageInfo damage)
        {
            if (IsDead || damage.Amount <= 0 || damage.Faction == Faction.Professor) return;

            health = Mathf.Max(0, health - damage.Amount);
            if (health == 0)
            {
                Die();
                return;
            }

            Perception.ForceAlert(damage.SourcePosition);
            Stun(config.stunDuration);
        }

        public virtual bool TryStomp(GameObject stomper, bool isGroundPound)
        {
            if (IsDead || stompImmunityTimer > 0f) return false;
            if (!config.canBeStomped && !isGroundPound) return false;

            stompImmunityTimer = config.stompImmunityTime;
            int amount = isGroundPound ? 2 : 1;
            TakeDamage(new DamageInfo(amount, DamageKind.Stomp, Faction.Student, stomper.transform.position, stomper));
            return true;
        }

        /// <summary>Da la voz de alarma a los profesores cercanos (si el config lo permite).</summary>
        protected void RaiseAlarm()
        {
            if (config.alarmRadius <= 0f) return;
            EventBus<AlarmRaisedEvent>.Raise(new AlarmRaisedEvent(transform.position, config.alarmRadius, gameObject));
        }

        private void CreateDefaultStates()
        {
            StatePatrol = new PatrolState(this);
            StateInvestigate = new InvestigateState(this);
            StateChase = new ChaseState(this);
            StateAttack = new AttackState(this);
            StateSearch = new SearchState(this);
            StateStunned = new StunnedState(this);
        }

        private void Die()
        {
            StopMoving();
            Defeated?.Invoke(this);
            OnDefeated();
        }

        private void OnAlarmRaised(AlarmRaisedEvent alarm)
        {
            if (IsDead || alarm.Source == gameObject) return;

            if ((alarm.Position - transform.position).sqrMagnitude <= alarm.Radius * alarm.Radius)
            {
                Perception.ForceAlert(alarm.Position);
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            var cfg = config;
            if (cfg == null) return;

            Vector3 origin = eye != null ? eye.position : transform.position + Vector3.up * 1.6f;
            float half = cfg.visionAngle * 0.5f;

            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Vector3 left = Quaternion.Euler(0f, -half, 0f) * transform.forward * cfg.visionRange;
            Vector3 right = Quaternion.Euler(0f, half, 0f) * transform.forward * cfg.visionRange;
            Gizmos.DrawLine(origin, origin + left);
            Gizmos.DrawLine(origin, origin + right);
            Gizmos.DrawLine(origin + left, origin + transform.forward * cfg.visionRange);
            Gizmos.DrawLine(origin + right, origin + transform.forward * cfg.visionRange);

            Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, cfg.attackRange);
        }
    }
}
