using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Core;
using UnityEngine;

namespace MatriculaDeHonor.Enemies.Attacks
{
    public readonly struct AttackContext
    {
        public readonly GameObject Attacker;
        public readonly Vector3 Origin;
        public readonly IPlayerTarget Target;

        public AttackContext(GameObject attacker, Vector3 origin, IPlayerTarget target)
        {
            Attacker = attacker;
            Origin = origin;
            Target = target;
        }
    }

    /// <summary>
    /// Cómo ataca un profesor (patrón Strategy). La máquina de estados es la misma para todos;
    /// lo que cambia entre el bedel, el de Cálculo o la de Química es la estrategia.
    /// </summary>
    public interface IAttackStrategy
    {
        string Name { get; }
        float Range { get; }
        bool IsReady { get; }

        /// <summary>0 = listo, 1 = recién usado.</summary>
        float Cooldown01 { get; }

        void Tick(float deltaTime);
        bool CanExecute(in AttackContext context);
        void Execute(in AttackContext context);
    }

    /// <summary>Base con enfriamiento (Template Method: las subclases solo implementan Perform).</summary>
    public abstract class CooldownAttack : IAttackStrategy
    {
        private float cooldownTimer;

        protected CooldownAttack(float range, float cooldown)
        {
            Range = Mathf.Max(0f, range);
            Cooldown = Mathf.Max(0f, cooldown);
        }

        public abstract string Name { get; }
        public float Range { get; }
        public float Cooldown { get; }
        public bool IsReady => cooldownTimer <= 0f;
        public float Cooldown01 => Cooldown <= 0f ? 0f : Mathf.Clamp01(cooldownTimer / Cooldown);

        public void Tick(float deltaTime)
        {
            if (cooldownTimer > 0f) cooldownTimer -= deltaTime;
        }

        public bool CanExecute(in AttackContext context)
        {
            return IsReady &&
                   context.Target != null &&
                   context.Target.IsAlive &&
                   IsInRange(context) &&
                   CanExecuteInternal(context);
        }

        public void Execute(in AttackContext context)
        {
            if (!CanExecute(context)) return;

            cooldownTimer = Cooldown;
            Perform(context);
        }

        public void ResetCooldown() => cooldownTimer = 0f;

        protected virtual bool IsInRange(in AttackContext context)
        {
            return (context.Target.Position - context.Origin).sqrMagnitude <= Range * Range;
        }

        protected virtual bool CanExecuteInternal(in AttackContext context) => true;

        protected abstract void Perform(in AttackContext context);
    }

    /// <summary>Daño por contacto (el bedel te "pilla").</summary>
    public sealed class ContactAttack : CooldownAttack
    {
        private readonly int damage;
        private readonly bool hurtsFromAbove;

        public ContactAttack(float range, float cooldown, int damage, bool hurtsFromAbove)
            : base(range, cooldown)
        {
            this.damage = damage;
            this.hurtsFromAbove = hurtsFromAbove;
        }

        public override string Name => "Contacto";

        protected override bool IsInRange(in AttackContext context)
        {
            Vector3 offset = context.Target.Position - context.Origin;
            offset.y = 0f;
            return offset.sqrMagnitude <= Range * Range;
        }

        protected override bool CanExecuteInternal(in AttackContext context)
        {
            // Si el jugador le está cayendo encima, gana el pisotón, no el golpe.
            bool targetAbove = context.Target.Position.y > context.Origin.y + 1f;
            return hurtsFromAbove || !targetAbove;
        }

        protected override void Perform(in AttackContext context)
        {
            context.Target.TakeDamage(new DamageInfo(damage, DamageKind.Contact, Faction.Professor, context.Origin, context.Attacker));
        }
    }

    /// <summary>Lanza proyectiles con puntería predictiva (el de Cálculo y sus tizas).</summary>
    public sealed class ProjectileAttack : CooldownAttack
    {
        private readonly Projectile prefab;
        private readonly int damage;
        private readonly float projectileSpeed;
        private readonly float aimSkill01;
        private readonly DamageKind kind;
        private bool warnedMissingFactory;

        public ProjectileAttack(
            Projectile prefab,
            float range,
            float cooldown,
            int damage,
            float projectileSpeed,
            float aimSkill01,
            DamageKind kind = DamageKind.Projectile)
            : base(range, cooldown)
        {
            this.prefab = prefab;
            this.damage = damage;
            this.projectileSpeed = projectileSpeed;
            this.aimSkill01 = aimSkill01;
            this.kind = kind;
        }

        public override string Name => "Proyectil";

        protected override bool CanExecuteInternal(in AttackContext context) => prefab != null;

        protected override void Perform(in AttackContext context)
        {
            if (!ServiceLocator.TryGet(out ProjectileFactory factory))
            {
                if (!warnedMissingFactory)
                {
                    Debug.LogWarning("[ProjectileAttack] No hay ProjectileFactory: ¿falta un LevelController en la escena?");
                    warnedMissingFactory = true;
                }

                return;
            }

            Vector3 direction = PredictiveAim.AimDirection(
                context.Origin,
                context.Target.AimPoint,
                context.Target.Velocity,
                projectileSpeed,
                aimSkill01);

            factory.Launch(prefab, context.Origin + direction * 0.5f, direction, projectileSpeed, damage, kind, Faction.Professor, context.Attacker);
        }
    }
}
