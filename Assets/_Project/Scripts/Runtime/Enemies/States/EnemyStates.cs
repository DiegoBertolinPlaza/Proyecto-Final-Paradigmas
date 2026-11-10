using MatriculaDeHonor.Core;
using MatriculaDeHonor.Enemies.Perception;
using UnityEngine;
using UnityEngine.AI;

namespace MatriculaDeHonor.Enemies.States
{
    // Estados de la IA de los profesores. Cada estado decide QUÉ hacer mientras está activo;
    // CUÁNDO se cambia está declarado en EnemyBase.BuildStateMachine().

    public abstract class EnemyStateBase : StateBase<EnemyBase>
    {
        protected EnemyStateBase(EnemyBase context) : base(context) { }

        protected EnemyPerception Perception => Context.Perception;
        protected EnemyConfig Config => Context.Config;
    }

    public sealed class PatrolState : EnemyStateBase
    {
        private int waypointIndex;
        private int direction = 1;
        private float waitTimer;
        private bool waiting;

        public PatrolState(EnemyBase context) : base(context) { }

        protected override void OnEnter()
        {
            waiting = false;
            var route = Context.PatrolRoute;

            if (route != null && route.Count > 0)
            {
                waypointIndex = route.GetNearestIndex(Context.transform.position);
                Context.MoveTo(route.GetPoint(waypointIndex), Config.patrolSpeed);
            }
            else
            {
                Context.MoveTo(Context.SpawnPosition, Config.patrolSpeed);
            }
        }

        protected override void OnTick(float deltaTime)
        {
            var route = Context.PatrolRoute;
            if (route == null || route.Count == 0)
            {
                if (Context.HasArrived) Context.StopMoving();
                return;
            }

            if (waiting)
            {
                waitTimer -= deltaTime;
                if (waitTimer > 0f) return;

                waiting = false;
                waypointIndex = route.GetNextIndex(waypointIndex, ref direction);
                Context.MoveTo(route.GetPoint(waypointIndex), Config.patrolSpeed);
            }
            else if (Context.HasArrived)
            {
                waiting = true;
                waitTimer = Config.waypointWaitTime;
            }
        }
    }

    /// <summary>"¿Qué ha sido eso?": va al punto de interés y mira alrededor.</summary>
    public sealed class InvestigateState : EnemyStateBase
    {
        private const float GiveUpAfter = 10f;

        private bool arrived;
        private float lookTimer;
        private float lookDirection = 1f;

        public InvestigateState(EnemyBase context) : base(context) { }

        public bool IsFinished => (arrived && lookTimer >= Config.investigateDuration) || TimeInState > GiveUpAfter;

        protected override void OnEnter()
        {
            GoTo(Perception.ConsumePointOfInterest());
            lookDirection = Random.value < 0.5f ? -1f : 1f;
        }

        protected override void OnTick(float deltaTime)
        {
            // Si oye algo nuevo mientras investiga, cambia de objetivo.
            if (Perception.HasPointOfInterest)
            {
                GoTo(Perception.ConsumePointOfInterest());
            }

            if (!arrived && Context.HasArrived)
            {
                arrived = true;
                lookTimer = 0f;
                Context.StopMoving();
            }

            if (arrived)
            {
                lookTimer += deltaTime;
                Context.transform.Rotate(0f, 90f * lookDirection * deltaTime, 0f);
            }
        }

        private void GoTo(Vector3 point)
        {
            arrived = false;
            lookTimer = 0f;
            Context.MoveTo(point, Config.patrolSpeed * 1.25f);
        }
    }

    public sealed class ChaseState : EnemyStateBase
    {
        private const float RepathInterval = 0.2f;
        private float repathTimer;

        public ChaseState(EnemyBase context) : base(context) { }

        protected override void OnEnter()
        {
            repathTimer = 0f;
        }

        protected override void OnTick(float deltaTime)
        {
            repathTimer -= deltaTime;
            if (repathTimer > 0f) return;

            repathTimer = RepathInterval;
            Vector3 destination = Perception.CanSeeTarget
                ? Perception.Target.Position
                : Perception.LastKnownTargetPosition;

            Context.MoveTo(destination, Config.chaseSpeed);
        }
    }

    public sealed class AttackState : EnemyStateBase
    {
        public AttackState(EnemyBase context) : base(context) { }

        protected override void OnEnter()
        {
            Context.StopMoving();
        }

        protected override void OnTick(float deltaTime)
        {
            if (!Perception.HasLivingTarget) return;

            Context.FaceTowards(Perception.Target.Position, deltaTime);

            var attackContext = Context.BuildAttackContext();
            if (Context.Attack.CanExecute(attackContext))
            {
                Context.Attack.Execute(attackContext);
            }
        }
    }

    /// <summary>Va a la última posición conocida y da vueltas por la zona.</summary>
    public sealed class SearchState : EnemyStateBase
    {
        private const float WanderPause = 0.8f;
        private const float GiveUpExtra = 8f;

        private bool reachedLastKnown;
        private float searchTimer;
        private float wanderTimer;
        private Vector3 center;

        public SearchState(EnemyBase context) : base(context) { }

        public bool IsFinished =>
            (reachedLastKnown && searchTimer >= Config.searchDuration) ||
            TimeInState > Config.searchDuration + GiveUpExtra;

        protected override void OnEnter()
        {
            reachedLastKnown = false;
            searchTimer = 0f;
            wanderTimer = 0f;
            center = Perception.LastKnownTargetPosition;
            Context.MoveTo(center, Config.chaseSpeed * 0.8f);
        }

        protected override void OnTick(float deltaTime)
        {
            if (!reachedLastKnown)
            {
                if (Context.HasArrived) reachedLastKnown = true;
                return;
            }

            searchTimer += deltaTime;
            if (!Context.HasArrived) return;

            wanderTimer -= deltaTime;
            if (wanderTimer > 0f) return;

            wanderTimer = WanderPause;
            Vector3 candidate = center + Random.insideUnitSphere * Config.searchRadius;
            if (NavMesh.SamplePosition(candidate, out var hit, Config.searchRadius, NavMesh.AllAreas))
            {
                Context.MoveTo(hit.position, Config.patrolSpeed * 1.5f);
            }
        }
    }

    public sealed class StunnedState : EnemyStateBase
    {
        private float duration;

        public StunnedState(EnemyBase context) : base(context) { }

        public bool IsFinished => TimeInState >= duration;

        protected override void OnEnter()
        {
            duration = Context.ConsumeStunRequest();
            Context.StopMoving();
        }
    }
}
