using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatriculaDeHonor.Core
{
    /// <summary>
    /// Service Locator mínimo. Lo preferimos a los Singletons clásicos porque:
    /// (1) los servicios se registran y se dan de baja con el ciclo de vida de su dueño
    /// (p. ej. los pools de proyectiles viven lo que vive el nivel), y
    /// (2) en los tests se puede registrar una implementación falsa.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            var key = typeof(T);
            if (Services.TryGetValue(key, out var existing) && !ReferenceEquals(existing, service))
            {
                Debug.LogWarning($"[ServiceLocator] Se reemplaza el servicio {key.Name}.");
            }

            Services[key] = service;
        }

        /// <summary>Da de baja el servicio solo si sigue siendo la misma instancia que se registró.</summary>
        public static void Unregister<T>(T service) where T : class
        {
            var key = typeof(T);
            if (Services.TryGetValue(key, out var existing) && ReferenceEquals(existing, service))
            {
                Services.Remove(key);
            }
        }

        public static T Get<T>() where T : class
        {
            if (TryGet<T>(out var service))
            {
                return service;
            }

            throw new InvalidOperationException(
                $"[ServiceLocator] No hay ningún servicio registrado de tipo {typeof(T).Name}.");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var raw) && raw is T typed && !IsDestroyedUnityObject(typed))
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public static void Clear() => Services.Clear();

        // Un MonoBehaviour destruido sigue siendo "no null" para C#; Unity sobrecarga == para detectarlo.
        private static bool IsDestroyedUnityObject(object service)
        {
            return service is UnityEngine.Object unityObject && unityObject == null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode() => Clear();
    }
}
