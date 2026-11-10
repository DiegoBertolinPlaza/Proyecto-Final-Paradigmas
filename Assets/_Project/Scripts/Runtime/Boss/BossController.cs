using System;
using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Core;
using UnityEngine;

namespace MatriculaDeHonor.Boss
{
    /// <summary>
    /// "Cuerpo" del jefe: vida, movimiento y ataques. No decide nada: cada frame construye una
    /// <see cref="BossObservation"/>, se la pasa a su <see cref="IBossBrain"/> y ejecuta el
    /// <see cref="BossCommand"/> que le devuelve. Separar cuerpo y cerebro es lo que permite
    /// cambiar entre IA programada y red neuronal sin tocar nada más.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class BossController : MonoBehaviour, IDamageable, IStompable
    {
        private const float DefaultArenaDiagonal = 28f;
        private const float ContactCooldown = 0.75f;

        [SerializeField] private BossConfig config;
        [SerializeField] private BossArena arena;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform throwOrigin;

        [Tooltip("Cerebro alternativo que implemente IBossBrain (p. ej. BossAgent de ML-Agents). Vacío = programado.")]
        [SerializeField] private MonoBehaviour brainBehaviour;

        [Tooltip("Objetivo fijo (solo para arenas de entrenamiento). Vacío = el jugador registrado.")]
        [SerializeField] private MonoBehaviour explicitTargetBehaviour;

        [Tooltip("Desmarcar en entrenamiento: no lanza eventos globales ni se desactiva al morir.")]
        [SerializeField] private bool isMainBoss = true;

        private CharacterController characterController;
        private IBossBrain brain;
        private IPlayerTarget target;
        private int health;
        private float throwTimer;
        private float chargeTimer;
        private float chargeRemaining;
        private float contactTimer;
        private float invulnerableTimer;
        private float verticalVelocity;
        private Vector3 chargeDirection;
        private bool chargeHitThisDash;

        public BossConfig Config => config;
        public BossArena Arena => arena;
        public IBossBrain Brain => brain;
        public string BrainName => brain?.DisplayName ?? "-";
        public BossCommand LastCommand { get; private set; }
        public IPlayerTarget Target => target;
        public int Health => health;
        public float Health01 => config != null ? (float)health / config.maxHealth : 0f;
        public bool IsAlive => health > 0;
        public bool IsCharging => chargeRemaining > 0f;
        public bool IsEnraged => Health01 <= config.enragedHealthFraction;
        public float ArenaDiagonal => arena != null ? arena.Diagonal : DefaultArenaDiagonal;

        /// <summary>El jefe ha hecho daño al jugador (proyectil o embestida). Recompensa para el agente ML.</summary>
        public event Action HitTarget;
        public event Action<DamageInfo> Damaged;
        public event Action Defeated;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[BossController] Falta BossConfig; se usan valores por defecto.", this);
                config = ScriptableObject.CreateInstance<BossConfig>();
            }

            characterController = GetComponent<CharacterController>();
            health = config.maxHealth;

            brain = brainBehaviour as IBossBrain;
            if (brainBehaviour != null && brain == null)
            {
                Debug.LogWarning($"[BossController] {brainBehaviour.GetType().Name} no implementa IBossBrain; se usa el programado.", this);
            }

            brain ??= new ScriptedBossBrain();
            brain.Bind(this);
        }

        private void OnDisable()
        {
            SetTarget(null);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            AcquireTarget();
            TickTimers(deltaTime);

            if (!IsAlive) return;

            var observation = BuildObservation();
            LastCommand = brain.Decide(observation, deltaTime);
            Execute(LastCommand, deltaTime);
        }

        // ---------- Observación ----------

        public BossObservation BuildObservation()
        {
            Vector2 arenaPosition = arena != null ? arena.Normalize(transform.position) : Vector2.zero;
            float throwCooldown01 = config.throwCooldown > 0f ? throwTimer / config.throwCooldown : 0f;
            float chargeCooldown01 = config.chargeCooldown > 0f ? chargeTimer / config.chargeCooldown : 0f;

            if (!HasLivingTarget())
            {
                return new BossObservation(false, 1f, 0f, Vector3.zero, false, false, Health01, 0f,
                    throwCooldown01, chargeCooldown01, IsCharging, arenaPosition);
            }

            Vector3 toTarget = target.Position - transform.position;
            float heightDifference = toTarget.y;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            Quaternion inverseFrame = Quaternion.Inverse(GetFrame(toTarget, distance));
            Vector3 localVelocity = inverseFrame * target.Velocity / config.maxPlayerSpeed;

            // La posición en la arena también se expresa en el marco del jefe.
            Vector3 arenaLocal = inverseFrame * new Vector3(arenaPosition.x, 0f, arenaPosition.y);

            bool targetAbove = heightDifference > 1f && distance < 2.5f;

            return new BossObservation(
                hasTarget: true,
                distance01: distance / ArenaDiagonal,
                targetHeight: heightDifference / 5f,
                targetLocalVelocity: localVelocity,
                targetIsAirborne: target.IsAirborne,
                targetIsAbove: targetAbove,
                ownHealth01: Health01,
                targetHealth01: target.Health01,
                throwCooldown01: throwCooldown01,
                chargeCooldown01: chargeCooldown01,
                isCharging: IsCharging,
                arenaPosition: new Vector2(arenaLocal.x, arenaLocal.z));
        }

        // ---------- Ejecución del comando ----------

        private void Execute(BossCommand command, float deltaTime)
        {
            Vector3 horizontal;

            if (IsCharging)
            {
                horizontal = chargeDirection * config.chargeSpeed;
                chargeRemaining -= deltaTime;
                TryContactDamage(DamageKind.Contact, duringCharge: true);
            }
            else
            {
                Vector3 toTarget = HasLivingTarget() ? target.Position - transform.position : transform.forward;
                toTarget.y = 0f;
                Quaternion frame = GetFrame(toTarget, toTarget.magnitude);

                transform.rotation = Quaternion.RotateTowards(transform.rotation, frame, config.turnSpeedDegrees * deltaTime);

                Vector3 local = new Vector3(command.Move.x, 0f, command.Move.y);
                horizontal = frame * local * config.moveSpeed;

                if (command.Throw && throwTimer <= 0f) Throw();
                if (command.Charge && chargeTimer <= 0f) StartCharge(frame * Vector3.forward);

                TryContactDamage(DamageKind.Contact, duringCharge: false);
            }

            verticalVelocity = characterController.isGrounded ? -2f : verticalVelocity - config.gravity * deltaTime;
            characterController.Move((horizontal + Vector3.up * verticalVelocity) * deltaTime);
        }

        private void Throw()
        {
            if (!HasLivingTarget() || projectilePrefab == null) return;
            if (!ServiceLocator.TryGet(out ProjectileFactory factory)) return;

            throwTimer = CooldownFor(config.throwCooldown);

            Vector3 origin = throwOrigin != null ? throwOrigin.position : transform.position + Vector3.up * 1.5f;
            Vector3 direction = PredictiveAim.AimDirection(origin, target.AimPoint, target.Velocity, config.projectileSpeed, config.aimSkill);
            factory.Launch(projectilePrefab, origin + direction * 0.6f, direction, config.projectileSpeed,
                config.projectileDamage, DamageKind.Projectile, Faction.Professor, gameObject);
        }

        private void StartCharge(Vector3 direction)
        {
            chargeTimer = CooldownFor(config.chargeCooldown);
            chargeRemaining = config.chargeDuration;
            chargeDirection = direction.normalized;
            chargeHitThisDash = false;
        }

        private void TryContactDamage(DamageKind kind, bool duringCharge)
        {
            if (!HasLivingTarget() || contactTimer > 0f) return;
            if (duringCharge && chargeHitThisDash) return;

            Vector3 offset = target.Position - transform.position;
            if (offset.y > 1f) return; // si está encima, eso es un pisotón, no un golpe
            offset.y = 0f;

            if (offset.sqrMagnitude > config.contactRadius * config.contactRadius) return;

            contactTimer = ContactCooldown;
            if (duringCharge) chargeHitThisDash = true;

            target.TakeDamage(new DamageInfo(config.contactDamage, kind, Faction.Professor, transform.position, gameObject));
        }

        private float CooldownFor(float baseCooldown)
        {
            return IsEnraged ? baseCooldown * config.enragedCooldownMultiplier : baseCooldown;
        }

        private void TickTimers(float deltaTime)
        {
            if (throwTimer > 0f) throwTimer -= deltaTime;
            if (chargeTimer > 0f) chargeTimer -= deltaTime;
            if (contactTimer > 0f) contactTimer -= deltaTime;
            if (invulnerableTimer > 0f) invulnerableTimer -= deltaTime;
        }

        // ---------- Vida ----------

        public void TakeDamage(in DamageInfo damage)
        {
            if (!IsAlive || invulnerableTimer > 0f || damage.Amount <= 0 || damage.Faction == Faction.Professor) return;

            health = Mathf.Max(0, health - damage.Amount);
            invulnerableTimer = config.hitInvulnerability;
            chargeRemaining = 0f;
            Damaged?.Invoke(damage);

            if (health == 0)
            {
                Defeat();
            }
        }

        public bool TryStomp(GameObject stomper, bool isGroundPound)
        {
            // Mientras embiste no se le puede pisar: hay que esquivarle primero.
            if (!IsAlive || IsCharging || invulnerableTimer > 0f) return false;

            TakeDamage(new DamageInfo(1, DamageKind.Stomp, Faction.Student, stomper.transform.position, stomper));
            return true;
        }

        private void Defeat()
        {
            Defeated?.Invoke();

            if (isMainBoss)
            {
                EventBus<BossDefeatedEvent>.Raise(new BossDefeatedEvent(config.displayName));
                gameObject.SetActive(false);
            }
        }

        /// <summary>Reinicia el jefe (nuevo intento o nuevo episodio de entrenamiento).</summary>
        public void ResetBoss(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = true;

            health = config.maxHealth;
            throwTimer = chargeTimer = chargeRemaining = contactTimer = invulnerableTimer = 0f;
            verticalVelocity = 0f;
            LastCommand = BossCommand.Idle;
        }

        // ---------- Objetivo ----------

        public void SetTarget(IPlayerTarget newTarget)
        {
            if (ReferenceEquals(target, newTarget)) return;

            if (target != null) target.Damaged -= OnTargetDamaged;
            target = newTarget;
            if (target != null) target.Damaged += OnTargetDamaged;
        }

        private void AcquireTarget()
        {
            if (HasValidTarget()) return;

            if (explicitTargetBehaviour is IPlayerTarget explicitTarget)
            {
                SetTarget(explicitTarget);
            }
            else if (ServiceLocator.TryGet(out IPlayerTarget located))
            {
                SetTarget(located);
            }
        }

        private void OnTargetDamaged(DamageInfo damage)
        {
            if (damage.Source == gameObject)
            {
                HitTarget?.Invoke();
            }
        }

        private bool HasValidTarget()
        {
            if (target == null) return false;
            return !(target is UnityEngine.Object unityObject) || unityObject != null;
        }

        private bool HasLivingTarget() => HasValidTarget() && target.IsAlive;

        private Quaternion GetFrame(Vector3 flatDirection, float magnitude)
        {
            if (magnitude > 0.01f)
            {
                return Quaternion.LookRotation(flatDirection / magnitude);
            }

            Vector3 forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward.normalized) : Quaternion.identity;
        }
    }
}
