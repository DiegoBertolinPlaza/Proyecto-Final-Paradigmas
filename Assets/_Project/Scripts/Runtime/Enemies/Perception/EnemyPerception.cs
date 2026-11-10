using System;
using MatriculaDeHonor.Combat;
using MatriculaDeHonor.Core;
using UnityEngine;

namespace MatriculaDeHonor.Enemies.Perception
{
    /// <summary>
    /// Une vista + oído en un "medidor de sospecha" (0..1). Es la información que usa la
    /// máquina de estados del profesor para decidir: patrullar, investigar, perseguir o buscar.
    /// </summary>
    public sealed class EnemyPerception : IDisposable
    {
        private readonly Transform body;
        private readonly Transform eye;
        private readonly EnemyConfig config;
        private readonly HearingSensor hearing;
        private IPlayerTarget explicitTarget;

        public EnemyPerception(Transform body, Transform eye, EnemyConfig config, LayerMask visionMask)
        {
            this.body = body;
            this.eye = eye != null ? eye : body;
            this.config = config;

            Vision = new VisionSensor(config.visionRange, config.visionAngle, visionMask);
            hearing = new HearingSensor(body, config.hearingSensitivity);
        }

        public VisionSensor Vision { get; }
        public IPlayerTarget Target { get; private set; }
        public bool CanSeeTarget { get; private set; }
        public float Suspicion01 { get; private set; }
        public bool IsAlerted => Suspicion01 >= 1f;
        public Vector3 LastKnownTargetPosition { get; private set; }
        public float TimeSinceTargetSeen { get; private set; } = float.PositiveInfinity;
        public bool HasLivingTarget => IsValid(Target) && Target.IsAlive;

        /// <summary>Algo raro que merece la pena ir a mirar (un ruido, un vistazo fugaz).</summary>
        public bool HasPointOfInterest { get; private set; }
        public Vector3 PointOfInterest { get; private set; }

        public void SetTarget(IPlayerTarget target) => explicitTarget = target;

        public void Tick(float deltaTime)
        {
            AcquireTarget();

            CanSeeTarget = HasLivingTarget &&
                           Vision.CanSee(eye.position, body.forward, Target.Transform, Target.AimPoint);

            if (CanSeeTarget)
            {
                TimeSinceTargetSeen = 0f;
                LastKnownTargetPosition = Target.Position;
                SetPointOfInterest(Target.Position);

                // Cuanto más cerca, antes se da cuenta (de x3 pegado a x1 en el límite de la vista).
                float distance01 = Vision.Range > 0f
                    ? Vector3.Distance(body.position, Target.Position) / Vision.Range
                    : 1f;
                float proximityBoost = Mathf.Lerp(3f, 1f, Mathf.Clamp01(distance01));
                Suspicion01 = Mathf.Min(1f, Suspicion01 + config.suspicionGainPerSecond * proximityBoost * deltaTime);
            }
            else
            {
                TimeSinceTargetSeen += deltaTime;
                Suspicion01 = Mathf.Max(0f, Suspicion01 - config.suspicionDecayPerSecond * deltaTime);
            }

            if (hearing.HasPendingNoise)
            {
                SetPointOfInterest(hearing.LastNoisePosition);
                Suspicion01 = Mathf.Min(1f, Suspicion01 + config.noiseSuspicion * hearing.LastNoiseIntensity01);
                hearing.ConsumeNoise();
            }

            if (IsValid(Target) && !Target.IsAlive)
            {
                ResetAwareness();
            }
        }

        public Vector3 ConsumePointOfInterest()
        {
            HasPointOfInterest = false;
            return PointOfInterest;
        }

        /// <summary>Alerta inmediata (le han dado, o un compañero ha dado la voz de alarma).</summary>
        public void ForceAlert(Vector3 position)
        {
            Suspicion01 = 1f;
            LastKnownTargetPosition = position;
            TimeSinceTargetSeen = 0f;
            SetPointOfInterest(position);
        }

        public void ResetAwareness()
        {
            Suspicion01 = 0f;
            HasPointOfInterest = false;
            TimeSinceTargetSeen = float.PositiveInfinity;
        }

        public void Dispose() => hearing.Dispose();

        private void SetPointOfInterest(Vector3 position)
        {
            PointOfInterest = position;
            HasPointOfInterest = true;
        }

        private void AcquireTarget()
        {
            if (IsValid(explicitTarget))
            {
                Target = explicitTarget;
                return;
            }

            if (!IsValid(Target) && ServiceLocator.TryGet(out IPlayerTarget located))
            {
                Target = located;
            }
        }

        private static bool IsValid(IPlayerTarget target)
        {
            if (target == null) return false;
            return !(target is UnityEngine.Object unityObject) || unityObject != null;
        }
    }
}
