using MatriculaDeHonor.Core.Pooling;
using UnityEngine;

namespace MatriculaDeHonor.Combat
{
    /// <summary>
    /// Proyectil en línea recta (tizas, matraces...). No usa Rigidbody: avanza con un SphereCast
    /// por frame, así no atraviesa paredes finas aunque vaya rápido.
    /// </summary>
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.01f)] private float radius = 0.15f;
        [SerializeField, Min(0.1f)] private float maxLifetime = 5f;
        [SerializeField] private LayerMask hitMask = ~0;

        private Vector3 velocity;
        private DamageInfo damageTemplate;
        private GameObject owner;
        private float age;
        private bool inFlight;
        private PooledInstance pooledInstance;

        private void Awake()
        {
            TryGetComponent(out pooledInstance);
        }

        public void Launch(Vector3 launchVelocity, int damage, DamageKind kind, Faction faction, GameObject source)
        {
            velocity = launchVelocity;
            owner = source;
            damageTemplate = new DamageInfo(damage, kind, faction, transform.position, source);
            age = 0f;
            inFlight = true;

            if (velocity.sqrMagnitude > 0.0001f)
            {
                transform.forward = velocity.normalized;
            }
        }

        private void Update()
        {
            if (!inFlight) return;

            age += Time.deltaTime;
            if (age >= maxLifetime)
            {
                Despawn();
                return;
            }

            Vector3 step = velocity * Time.deltaTime;
            float distance = step.magnitude;

            if (distance > 0f &&
                Physics.SphereCast(transform.position, radius, step / distance, out var hit, distance, hitMask, QueryTriggerInteraction.Ignore) &&
                !IsOwner(hit.collider))
            {
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    var damage = new DamageInfo(damageTemplate.Amount, damageTemplate.Kind, damageTemplate.Faction, hit.point, owner);
                    damageable.TakeDamage(damage);
                }

                Despawn();
                return;
            }

            transform.position += step;
        }

        private bool IsOwner(Collider other)
        {
            return owner != null && other.transform.IsChildOf(owner.transform);
        }

        private void Despawn()
        {
            if (!inFlight) return;
            inFlight = false;

            if (pooledInstance == null) TryGetComponent(out pooledInstance);

            if (pooledInstance != null)
            {
                pooledInstance.Release();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void IPoolable.OnSpawned()
        {
            age = 0f;
        }

        void IPoolable.OnDespawned()
        {
            inFlight = false;
            owner = null;
        }
    }
}
