using System.Collections.Generic;
using UnityEngine;

namespace MatriculaDeHonor.Enemies
{
    /// <summary>Ruta de patrulla. Si no se asignan puntos, usa los hijos de este GameObject.</summary>
    public sealed class PatrolRoute : MonoBehaviour
    {
        [SerializeField] private Transform[] points = new Transform[0];
        [SerializeField] private bool pingPong;

        private readonly List<Transform> resolved = new List<Transform>();
        private bool resolvedOnce;

        public int Count
        {
            get
            {
                EnsureResolved();
                return resolved.Count;
            }
        }

        public Vector3 GetPoint(int index)
        {
            EnsureResolved();
            return resolved[Mathf.Clamp(index, 0, resolved.Count - 1)].position;
        }

        public int GetNearestIndex(Vector3 position)
        {
            EnsureResolved();

            int best = 0;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < resolved.Count; i++)
            {
                float distance = (resolved[i].position - position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>Siguiente punto; <paramref name="direction"/> se invierte en los extremos si es ping-pong.</summary>
        public int GetNextIndex(int current, ref int direction)
        {
            EnsureResolved();
            if (resolved.Count <= 1) return 0;

            if (!pingPong)
            {
                return (current + 1) % resolved.Count;
            }

            int next = current + direction;
            if (next < 0 || next >= resolved.Count)
            {
                direction = -direction;
                next = current + direction;
            }

            return next;
        }

        private void EnsureResolved()
        {
            if (resolvedOnce) return;
            resolvedOnce = true;

            resolved.Clear();
            if (points != null)
            {
                foreach (var point in points)
                {
                    if (point != null) resolved.Add(point);
                }
            }

            if (resolved.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    resolved.Add(child);
                }
            }
        }

        private void OnDrawGizmos()
        {
            var list = new List<Transform>();
            if (points != null && points.Length > 0)
            {
                foreach (var point in points) if (point != null) list.Add(point);
            }
            else
            {
                foreach (Transform child in transform) list.Add(child);
            }

            Gizmos.color = new Color(1f, 0.8f, 0.2f);
            for (int i = 0; i < list.Count; i++)
            {
                Gizmos.DrawWireSphere(list[i].position, 0.25f);
                if (i + 1 < list.Count) Gizmos.DrawLine(list[i].position, list[i + 1].position);
                else if (!pingPong && list.Count > 2) Gizmos.DrawLine(list[i].position, list[0].position);
            }
        }
    }
}
