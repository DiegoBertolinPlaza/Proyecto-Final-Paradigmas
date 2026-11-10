using UnityEngine;

namespace MatriculaDeHonor.Boss
{
    /// <summary>Zona rectangular de combate. Sirve para normalizar observaciones y para colocar
    /// al jefe y al jugador al empezar cada episodio de entrenamiento.</summary>
    public sealed class BossArena : MonoBehaviour
    {
        [SerializeField] private Vector2 halfExtents = new Vector2(10f, 10f);

        public Vector3 Center => transform.position;
        public Vector2 HalfExtents => halfExtents;
        public float Diagonal => 2f * halfExtents.magnitude;

        public Vector3 RandomPointInside(float margin)
        {
            float x = Random.Range(-halfExtents.x + margin, halfExtents.x - margin);
            float z = Random.Range(-halfExtents.y + margin, halfExtents.y - margin);
            return transform.TransformPoint(new Vector3(x, 0f, z));
        }

        /// <summary>Posición relativa al centro normalizada a [-1, 1] en cada eje.</summary>
        public Vector2 Normalize(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            return new Vector2(
                halfExtents.x > 0f ? local.x / halfExtents.x : 0f,
                halfExtents.y > 0f ? local.z / halfExtents.y : 0f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.2f, 0.9f, 0.8f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(halfExtents.x * 2f, 0.1f, halfExtents.y * 2f));
        }
    }
}
