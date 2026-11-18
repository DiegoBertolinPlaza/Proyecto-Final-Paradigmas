using MatriculaDeHonor.Core.Pooling;
using UnityEngine;

namespace MatriculaDeHonor.Combat
{
    /// <summary>
    /// Factory de proyectiles: oculta a quien dispara cómo se crean (pool) y cómo se inicializan.
    /// Los profesores y el jefe solo piden "lánzame esto hacia allí".
    /// </summary>
    public sealed class ProjectileFactory
    {
        private readonly PoolService pools;

        public ProjectileFactory(PoolService pools)
        {
            this.pools = pools;
        }

        public Projectile Launch(
            Projectile prefab,
            Vector3 origin,
            Vector3 direction,
            float speed,
            int damage,
            DamageKind kind,
            Faction faction,
            GameObject owner)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[ProjectileFactory] Se ha pedido lanzar un proyectil sin prefab.");
                return null;
            }

            var rotation = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            var projectile = pools.Spawn(prefab, origin, rotation);
            projectile.Launch(direction.normalized * speed, damage, kind, faction, owner);
            return projectile;
        }
    }
}
