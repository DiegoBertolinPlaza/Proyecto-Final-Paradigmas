using System;
using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Core;
using MatriculaDeHonor.Player.Input;
using MatriculaDeHonor.Player.States;
using MatriculaDeHonor.Player.Stats;
using UnityEngine;

namespace MatriculaDeHonor.Player
{
    /// <summary>
    /// Contexto del estudiante: lee el input, mantiene vida/estadísticas y delega el
    /// comportamiento en su máquina de estados (Grounded, Jump, Fall, GroundPound, Hurt, Dead).
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour, IPlayerTarget
    {
        [SerializeField] private PlayerConfig config;

        [Tooltip("Componente que implementa IPlayerInputSource. Vacío = se busca en este GameObject.")]
        [SerializeField] private MonoBehaviour inputSourceBehaviour;

        [Tooltip("Cámara para el movimiento relativo. Vacío = Camera.main.")]
        [SerializeField] private Transform cameraTransform;

        [Tooltip("Desmarcar en las arenas de entrenamiento: así no se registra como 'el' jugador ni lanza eventos globales.")]
        [SerializeField] private bool isMainPlayer = true;

        private IPlayerInputSource inputSource;
        private StateMachine stateMachine;
        private GroundedState groundedState;
        private JumpState jumpState;
        private FallState fallState;
        private GroundPoundState groundPoundState;
        private HurtState hurtState;
        private DeadState deadState;

        private float jumpBufferTimer;
        private bool jumpUsedSinceGrounded;
        private bool hurtPending;
        private float footstepTimer;
        private float pendingJumpHeight = -1f;

        public PlayerConfig Config => config;
        public PlayerMotor Motor { get; private set; }
        public PlayerHealth Health { get; private set; }
        public PlayerStats Stats { get; private set; }
        public PlayerInputFrame Frame { get; private set; }
        public DamageInfo LastDamage { get; private set; }
        public bool IsMainPlayer => isMainPlayer;
        public string CurrentStateName => stateMachine?.Current?.Name ?? "-";

        public bool HasBufferedJump => jumpBufferTimer > 0f;
        public bool CanUseCoyoteTime => !jumpUsedSinceGrounded && Time.time - Motor.LastGroundedTime <= config.coyoteTime;
        public bool IsGroundPounding => stateMachine?.Current == groundPoundState;

        public float CurrentMaxSpeed => Frame.RunHeld ? Stats.Get(StatType.RunSpeed) : Stats.Get(StatType.WalkSpeed);

        // Eventos de instancia (no globales): los usa, p. ej., la arena de entrenamiento.
        public event Action<PlayerController> Died;
        public event Action<PlayerController> Respawned;
        public event Action<IStompable> Stomped;
        public event Action<DamageInfo> Damaged;

        // ---------- IPlayerTarget ----------
        public Transform Transform => transform;
        public Vector3 Position => transform.position;
        public Vector3 AimPoint => transform.position + Vector3.up;
        public Vector3 Velocity => Motor != null ? Motor.Velocity : Vector3.zero;
        public bool IsAirborne => Motor != null && !Motor.IsGrounded;
        public bool IsAlive => Health != null && !Health.IsDead;
        public float Health01 => Health?.Health01 ?? 0f;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[PlayerController] Falta PlayerConfig; se usan valores por defecto.", this);
                config = ScriptableObject.CreateInstance<PlayerConfig>();
            }

            Motor = GetComponent<PlayerMotor>();
            inputSource = ResolveInputSource();
            Health = new PlayerHealth(config.maxHealth, config.maxShield);
            Stats = new PlayerStats(new BaseStatProvider(config.BuildBaseStats()));

            BuildStateMachine();
        }

        private void OnEnable()
        {
            Motor.Hit += OnMotorHit;
            Motor.Landed += OnLanded;
            Health.Changed += OnHealthChanged;
            Health.Died += OnHealthDied;
            Stats.ModifierChanged += OnModifierChanged;

            if (isMainPlayer)
            {
                ServiceLocator.Register<IPlayerTarget>(this);
            }
        }

        private void Start()
        {
            OnHealthChanged(Health);
        }

        private void OnDisable()
        {
            Motor.Hit -= OnMotorHit;
            Motor.Landed -= OnLanded;
            Health.Changed -= OnHealthChanged;
            Health.Died -= OnHealthDied;
            Stats.ModifierChanged -= OnModifierChanged;

            if (isMainPlayer)
            {
                ServiceLocator.Unregister<IPlayerTarget>(this);
            }
        }

        private void BuildStateMachine()
        {
            groundedState = new GroundedState(this);
            jumpState = new JumpState(this);
            fallState = new FallState(this);
            groundPoundState = new GroundPoundState(this);
            hurtState = new HurtState(this);
            deadState = new DeadState(this);

            stateMachine = new StateMachine();

            // Prioritarias: se evalúan desde cualquier estado.
            stateMachine.AddAnyTransition(deadState, () => Health.IsDead);
            stateMachine.AddAnyTransition(hurtState, () => hurtPending && !Health.IsDead);

            stateMachine.AddTransition(groundedState, jumpState, () => HasBufferedJump);
            stateMachine.AddTransition(groundedState, fallState, () => !Motor.IsGrounded);

            stateMachine.AddTransition(jumpState, groundPoundState, () => Frame.GroundPoundPressed);
            stateMachine.AddTransition(jumpState, fallState, () => Motor.VerticalVelocity <= 0f);

            stateMachine.AddTransition(fallState, jumpState, () => HasBufferedJump && CanUseCoyoteTime);
            stateMachine.AddTransition(fallState, groundPoundState, () => Frame.GroundPoundPressed);
            stateMachine.AddTransition(fallState, groundedState, () => Motor.IsGrounded);

            stateMachine.AddTransition(groundPoundState, groundedState, () => groundPoundState.HasSlammed && Motor.IsGrounded);

            stateMachine.AddTransition(hurtState, groundedState, () => hurtState.IsFinished && Motor.IsGrounded);
            stateMachine.AddTransition(hurtState, fallState, () => hurtState.IsFinished && !Motor.IsGrounded);

            stateMachine.SetState(fallState);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return; // juego en pausa

            Frame = inputSource != null ? inputSource.ReadFrame() : default;

            jumpBufferTimer = Frame.JumpPressed
                ? config.jumpBufferTime
                : Mathf.Max(0f, jumpBufferTimer - deltaTime);

            if (Motor.IsGrounded)
            {
                jumpUsedSinceGrounded = false;
            }

            Health.Tick(deltaTime);
            Stats.Tick(deltaTime);

            stateMachine.Tick(deltaTime);
            Motor.Step(deltaTime, config.gravity, config.maxFallSpeed);
        }

        // ---------- API para los estados ----------

        public Vector3 GetDesiredMoveDirection()
        {
            Vector2 input = Frame.Move;
            if (input.sqrMagnitude < 0.0001f) return Vector3.zero;

            if (Frame.MoveIsWorldSpace)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            var cam = ResolveCamera();
            if (cam == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(cam.up, Vector3.up);
            }

            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return forward * input.y + right * input.x;
        }

        public void ApplyHorizontalMovement(float control, float deltaTime)
        {
            Vector3 direction = GetDesiredMoveDirection();
            bool hasInput = direction.sqrMagnitude > 0.0001f;
            float acceleration = (hasInput ? config.acceleration : config.deceleration) * control;

            Motor.Accelerate(direction * CurrentMaxSpeed, acceleration, deltaTime);
            Motor.FaceDirection(direction, config.turnSpeedDegrees, deltaTime);
        }

        public void ConsumeJumpBuffer()
        {
            jumpBufferTimer = 0f;
            jumpUsedSinceGrounded = true;
        }

        /// <summary>Altura del próximo salto: la de un rebote si hay uno pendiente, si no la normal.</summary>
        public float TakeJumpHeight()
        {
            if (pendingJumpHeight >= 0f)
            {
                float height = pendingJumpHeight;
                pendingJumpHeight = -1f;
                return height;
            }

            return Stats.Get(StatType.JumpHeight);
        }

        public void ConsumeHurt() => hurtPending = false;

        public void TickFootsteps(float deltaTime)
        {
            if (Motor.HorizontalVelocity.sqrMagnitude < 0.25f)
            {
                footstepTimer = 0f;
                return;
            }

            footstepTimer -= deltaTime;
            if (footstepTimer > 0f) return;

            footstepTimer = config.footstepInterval;
            EmitNoise(Frame.RunHeld ? config.runNoiseRadius : config.walkNoiseRadius);
        }

        public void EmitNoise(float radius)
        {
            if (!isMainPlayer || radius <= 0f) return;

            float finalRadius = radius * Stats.Get(StatType.NoiseMultiplier);
            EventBus<NoiseEvent>.Raise(new NoiseEvent(transform.position, finalRadius, gameObject));
        }

        public void Bounce(float height)
        {
            pendingJumpHeight = height;
            stateMachine.SetState(jumpState, forceReenter: true);
        }

        public void ApplyPowerUp(IStatModifierSource modifier, int shieldCharges)
        {
            if (shieldCharges > 0)
            {
                Health.AddShield(shieldCharges);
            }

            if (modifier != null)
            {
                Stats.Apply(modifier);
            }
        }

        // ---------- Vida ----------

        public void TakeDamage(in DamageInfo damage)
        {
            if (damage.Faction == Faction.Student) return;
            if (damage.Kind == DamageKind.Chemical && Stats.Get(StatType.ChemicalResistance) >= 1f) return;

            var result = Health.ApplyDamage(damage, config.invulnerabilityTime);
            if (result == DamageResult.Ignored) return;

            LastDamage = damage;
            if (result != DamageResult.Killed)
            {
                hurtPending = true;
            }

            Damaged?.Invoke(damage);
        }

        /// <summary>Muerte instantánea (caer al vacío).</summary>
        public void Kill()
        {
            TakeDamage(new DamageInfo(int.MaxValue, DamageKind.Fall, Faction.Environment, transform.position, null));
        }

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            Motor.Teleport(position, rotation);
            Health.ResetToFull();
            Stats.ClearAll();

            jumpBufferTimer = 0f;
            hurtPending = false;
            pendingJumpHeight = -1f;
            footstepTimer = 0f;

            stateMachine.SetState(fallState, forceReenter: true);
            Respawned?.Invoke(this);
        }

        public void SetInputSource(IPlayerInputSource source)
        {
            inputSource = source;
        }

        // ---------- Callbacks ----------

        private void OnMotorHit(ControllerColliderHit hit)
        {
            if (Health.IsDead) return;

            bool landingOnTop = hit.normal.y > 0.6f && Motor.VerticalVelocity <= 0.1f;
            if (!landingOnTop) return;

            var stompable = hit.collider.GetComponentInParent<IStompable>();
            if (stompable == null) return;

            if (!stompable.TryStomp(gameObject, IsGroundPounding)) return;

            Stomped?.Invoke(stompable);
            Bounce(Frame.JumpHeld ? config.stompBounceHeightHeld : config.stompBounceHeight);
        }

        private void OnLanded(float impactVelocity)
        {
            if (impactVelocity < -12f)
            {
                EmitNoise(config.landNoiseRadius);
            }
        }

        private void OnHealthChanged(PlayerHealth health)
        {
            if (!isMainPlayer) return;
            EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(health.Current, health.Max, health.Shield));
        }

        private void OnHealthDied()
        {
            Died?.Invoke(this);

            if (isMainPlayer)
            {
                EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent(transform.position));
            }
        }

        private void OnModifierChanged(IStatModifierSource source, bool active)
        {
            if (!isMainPlayer) return;
            EventBus<PowerUpChangedEvent>.Raise(new PowerUpChangedEvent(source.DisplayName, active, source.Duration));
        }

        private IPlayerInputSource ResolveInputSource()
        {
            if (inputSourceBehaviour is IPlayerInputSource explicitSource)
            {
                return explicitSource;
            }

            if (inputSourceBehaviour != null)
            {
                Debug.LogWarning($"[PlayerController] {inputSourceBehaviour.GetType().Name} no implementa IPlayerInputSource.", this);
            }

            var found = GetComponent<IPlayerInputSource>();
            if (found == null)
            {
                Debug.LogWarning("[PlayerController] No hay ninguna fuente de input en el jugador.", this);
            }

            return found;
        }

        private Transform ResolveCamera()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            return cameraTransform;
        }
    }
}
