using System;
using System.Collections.Generic;
using UnityEngine;

namespace MatriculaDeHonor.Core
{
    /// <summary>Marca un struct como evento publicable en el <see cref="EventBus{T}"/>.</summary>
    public interface IEvent { }

    /// <summary>
    /// Bus de eventos tipado (patrón Observer). Cada tipo de evento tiene su propia lista de
    /// suscriptores, así que no hay casts ni strings mágicos. Los eventos son structs para no
    /// generar basura en cada publicación.
    /// </summary>
    public static class EventBus<T> where T : struct, IEvent
    {
        private static readonly List<Action<T>> Handlers = new List<Action<T>>();
        private static Action<T>[] snapshot = Array.Empty<Action<T>>();
        private static bool dirty;

        static EventBus()
        {
            EventBusRegistry.Register(typeof(T), Clear);
        }

        public static int SubscriberCount => Handlers.Count;

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (Handlers.Contains(handler)) return;

            Handlers.Add(handler);
            dirty = true;
        }

        public static void Unsubscribe(Action<T> handler)
        {
            if (handler != null && Handlers.Remove(handler))
            {
                dirty = true;
            }
        }

        /// <summary>
        /// Publica el evento. Se itera sobre una copia, así que los suscriptores pueden
        /// (des)suscribirse dentro del propio handler. Una excepción en un handler no
        /// impide que el resto reciba el evento.
        /// </summary>
        public static void Raise(T evt)
        {
            if (dirty)
            {
                snapshot = Handlers.ToArray();
                dirty = false;
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i](evt);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public static void Clear()
        {
            Handlers.Clear();
            snapshot = Array.Empty<Action<T>>();
            dirty = false;
        }
    }

    /// <summary>
    /// Registro de todos los buses creados, para poder limpiarlos a la vez. Esto es clave para
    /// el requisito de "reiniciarse correctamente": si el editor tiene desactivada la recarga de
    /// dominio, los estáticos sobreviven entre partidas y acumularían suscriptores muertos.
    /// </summary>
    public static class EventBusRegistry
    {
        private static readonly Dictionary<Type, Action> Clearers = new Dictionary<Type, Action>();

        public static IReadOnlyCollection<Type> RegisteredEventTypes => Clearers.Keys;

        internal static void Register(Type eventType, Action clear)
        {
            Clearers[eventType] = clear;
        }

        public static void ClearAll()
        {
            foreach (var clear in Clearers.Values)
            {
                clear();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode() => ClearAll();
    }
}
