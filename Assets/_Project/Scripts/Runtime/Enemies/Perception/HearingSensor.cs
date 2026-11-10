using System;
using MatriculaDeHonor.Core;
using UnityEngine;

namespace MatriculaDeHonor.Enemies.Perception
{
    /// <summary>
    /// Oído: se suscribe a los <see cref="NoiseEvent"/> del bus (Observer). Correr hace mucho
    /// ruido, andar poco y la sudadera con capucha casi nada.
    /// </summary>
    public sealed class HearingSensor : IDisposable
    {
        private readonly Transform owner;
        private readonly float sensitivity;
        private bool disposed;

        public HearingSensor(Transform owner, float sensitivity)
        {
            this.owner = owner;
            this.sensitivity = Mathf.Max(0f, sensitivity);
            EventBus<NoiseEvent>.Subscribe(OnNoise);
        }

        public bool HasPendingNoise { get; private set; }
        public Vector3 LastNoisePosition { get; private set; }

        /// <summary>1 = el ruido ha sonado justo al lado; 0 = en el límite del radio.</summary>
        public float LastNoiseIntensity01 { get; private set; }

        public void ConsumeNoise() => HasPendingNoise = false;

        private void OnNoise(NoiseEvent noise)
        {
            if (owner == null) return;
            if (noise.Source != null && noise.Source.transform.IsChildOf(owner)) return;

            float effectiveRadius = noise.Radius * sensitivity;
            if (effectiveRadius <= 0f) return;

            float distance = Vector3.Distance(owner.position, noise.Position);
            if (distance > effectiveRadius) return;

            float intensity = 1f - distance / effectiveRadius;
            if (HasPendingNoise && intensity < LastNoiseIntensity01) return; // nos quedamos con el más fuerte

            HasPendingNoise = true;
            LastNoisePosition = noise.Position;
            LastNoiseIntensity01 = intensity;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            EventBus<NoiseEvent>.Unsubscribe(OnNoise);
        }
    }
}
