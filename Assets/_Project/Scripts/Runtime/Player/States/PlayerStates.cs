using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Core;
using UnityEngine;

namespace MatriculaDeHonor.Player.States
{
    // Estados del estudiante (patrón State). Cada uno solo decide cómo se mueve mientras está
    // activo; CUÁNDO se cambia de estado está declarado en PlayerController.BuildStateMachine().

    public abstract class PlayerStateBase : StateBase<PlayerController>
    {
        protected PlayerStateBase(PlayerController context) : base(context) { }

        protected PlayerMotor Motor => Context.Motor;
        protected PlayerConfig Config => Context.Config;
    }

    public sealed class GroundedState : PlayerStateBase
    {
        public GroundedState(PlayerController context) : base(context) { }

        public override string Name => Motor.HorizontalVelocity.sqrMagnitude > 0.25f ? "Run" : "Idle";

        protected override void OnEnter()
        {
            Motor.GravityEnabled = true;
            Motor.GravityScale = 1f;
        }

        protected override void OnTick(float deltaTime)
        {
            Context.ApplyHorizontalMovement(1f, deltaTime);
            Context.TickFootsteps(deltaTime);
        }
    }

    public sealed class JumpState : PlayerStateBase
    {
        public JumpState(PlayerController context) : base(context) { }

        protected override void OnEnter()
        {
            float height = Context.TakeJumpHeight();
            Context.ConsumeJumpBuffer();

            Motor.GravityEnabled = true;
            Motor.GravityScale = 1f;
            Motor.SetVerticalVelocity(Config.JumpVelocityFor(height));
        }

        protected override void OnTick(float deltaTime)
        {
            Context.ApplyHorizontalMovement(Config.airControl, deltaTime);

            // Salto de altura variable: si sueltas el botón mientras subes, cae antes.
            bool rising = Motor.VerticalVelocity > 0f;
            Motor.GravityScale = rising && !Context.Frame.JumpHeld ? Config.lowJumpGravityMultiplier : 1f;
        }
    }

    public sealed class FallState : PlayerStateBase
    {
        public FallState(PlayerController context) : base(context) { }

        protected override void OnEnter()
        {
            Motor.GravityEnabled = true;
            Motor.GravityScale = Config.fallGravityMultiplier;
        }

        protected override void OnTick(float deltaTime)
        {
            Context.ApplyHorizontalMovement(Config.airControl, deltaTime);
        }
    }

    /// <summary>Culetazo: se queda un instante quieto en el aire y cae en vertical.</summary>
    public sealed class GroundPoundState : PlayerStateBase
    {
        public GroundPoundState(PlayerController context) : base(context) { }

        public bool HasSlammed { get; private set; }

        protected override void OnEnter()
        {
            HasSlammed = false;
            Motor.SetHorizontalVelocity(Vector3.zero);
            Motor.SetVerticalVelocity(0f);
            Motor.GravityEnabled = false;
        }

        protected override void OnTick(float deltaTime)
        {
            if (HasSlammed || TimeInState < Config.groundPoundHangTime) return;

            // Sin gravedad durante la caída: velocidad constante y fácil de leer para el jugador.
            HasSlammed = true;
            Motor.SetVerticalVelocity(-Config.groundPoundSpeed);
        }

        protected override void OnExit()
        {
            Motor.GravityEnabled = true;

            if (HasSlammed && Motor.IsGrounded)
            {
                Context.EmitNoise(Config.groundPoundNoiseRadius);
            }
        }
    }

    public sealed class HurtState : PlayerStateBase
    {
        public HurtState(PlayerController context) : base(context) { }

        public bool IsFinished => TimeInState >= Config.hurtDuration;

        protected override void OnEnter()
        {
            Context.ConsumeHurt();

            Vector3 away = Context.transform.position - Context.LastDamage.SourcePosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
            {
                away = -Context.transform.forward;
            }

            Motor.GravityEnabled = true;
            Motor.GravityScale = 1f;
            Motor.SetHorizontalVelocity(away.normalized * Config.knockbackSpeed);
            Motor.SetVerticalVelocity(4f);
        }

        protected override void OnTick(float deltaTime)
        {
            Motor.Accelerate(Vector3.zero, Config.deceleration * 0.25f, deltaTime);
        }
    }

    public sealed class DeadState : PlayerStateBase
    {
        public DeadState(PlayerController context) : base(context) { }

        protected override void OnEnter()
        {
            Motor.SetHorizontalVelocity(Vector3.zero);

            // Si ha caído al vacío lo congelamos; si no, que caiga al suelo.
            if (Context.LastDamage.Kind == DamageKind.Fall)
            {
                Motor.SetVerticalVelocity(0f);
                Motor.GravityEnabled = false;
            }
        }

        protected override void OnExit()
        {
            Motor.GravityEnabled = true;
        }
    }
}
