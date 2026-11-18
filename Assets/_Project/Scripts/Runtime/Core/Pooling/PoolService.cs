using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace MatriculaDeHonor.Core.Pooling
{
    /// <summary>Componentes que quieren enterarse de cuándo salen y vuelven al pool.</summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

    /// <summary>
    /// Pools de objetos indexados por prefab (patrón Object Pool sobre <see cref="ObjectPool{T}"/>).
    /// Vive lo que vive el nivel: el <c>LevelController</c> lo crea, lo registra y lo libera,
    /// así que al cambiar de escena no queda ninguna instancia colgando.
    /// </summary>
    public sealed class PoolService
    {
        private const int DefaultCapacity = 16;
        private const int MaxSize = 256;

        private readonly Dictionary<Component, ObjectPool<PooledInstance>> pools =
            new Dictionary<Component, ObjectPool<PooledInstance>>();

        private readonly Transform root;
        private bool disposed;

        public PoolService(Transform root)
        {
            this.root = root;
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get();

            // Colocamos antes de activar para que OnEnable ya vea la posición correcta.
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            instance.NotifySpawned();

            return instance.GetComponent<T>();
        }

        internal void Release(PooledInstance instance)
        {
            if (disposed || !pools.TryGetValue(instance.PrefabKey, out var pool))
            {
                Object.Destroy(instance.gameObject);
                return;
            }

            pool.Release(instance);
        }

        public void Dispose()
        {
            disposed = true;
            foreach (var pool in pools.Values)
            {
                pool.Clear();
            }

            pools.Clear();
        }

        private ObjectPool<PooledInstance> GetOrCreatePool(Component prefab)
        {
            if (pools.TryGetValue(prefab, out var pool))
            {
                return pool;
            }

            pool = new ObjectPool<PooledInstance>(
                createFunc: () => CreateInstance(prefab),
                actionOnGet: null,
                actionOnRelease: OnRelease,
                actionOnDestroy: instance =>
                {
                    if (instance != null) Object.Destroy(instance.gameObject);
                },
                collectionCheck: true,
                defaultCapacity: DefaultCapacity,
                maxSize: MaxSize);

            pools.Add(prefab, pool);
            return pool;
        }

        private PooledInstance CreateInstance(Component prefab)
        {
            var go = Object.Instantiate(prefab.gameObject, root);
            go.SetActive(false);

            if (!go.TryGetComponent(out PooledInstance instance))
            {
                instance = go.AddComponent<PooledInstance>();
            }

            instance.Bind(this, prefab);
            return instance;
        }

        private static void OnRelease(PooledInstance instance)
        {
            instance.NotifyDespawned();
            instance.gameObject.SetActive(false);
        }
    }
}
