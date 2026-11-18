using UnityEngine;

namespace MatriculaDeHonor.Core.Pooling
{
    /// <summary>Se añade a cada instancia de un pool para saber a cuál debe volver.</summary>
    [DisallowMultipleComponent]
    public sealed class PooledInstance : MonoBehaviour
    {
        private PoolService owner;
        private IPoolable[] poolables;

        internal Component PrefabKey { get; private set; }

        internal void Bind(PoolService pool, Component prefab)
        {
            owner = pool;
            PrefabKey = prefab;
            poolables = GetComponents<IPoolable>();
        }

        internal void NotifySpawned()
        {
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnSpawned();
        }

        internal void NotifyDespawned()
        {
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnDespawned();
        }

        /// <summary>Devuelve el objeto a su pool (o lo destruye si el pool ya no existe).</summary>
        public void Release()
        {
            if (owner != null)
            {
                owner.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
