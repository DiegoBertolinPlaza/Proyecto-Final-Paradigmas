using UnityEngine;

namespace MatriculaDeHonor.Player.Input
{
    /// <summary>
    /// "Jugador bot" para entrenar al jefe con ML-Agents. Maneja el MISMO PlayerController que un
    /// humano (por eso el jefe aprende contra las físicas reales del juego). Sus parámetros se
    /// aleatorizan en cada episodio para que el jefe no se aprenda un único patrón.
    /// </summary>
    public sealed class PlayerBotInputSource : MonoBehaviour, IPlayerInputSource
    {
        [SerializeField] private Transform target;

        [Tooltip("Probabilidad de ir a por el jefe en vez de esquivar.")]
        [SerializeField, Range(0f, 1f)] private float aggression = 0.6f;

        [Tooltip("Probabilidad de saltar cuando está cerca.")]
        [SerializeField, Range(0f, 1f)] private float jumpiness = 0.5f;

        [SerializeField, Min(0.05f)] private float decisionInterval = 0.25f;

        private Vector2 currentMove;
        private bool pendingJump;
        private bool holdJump;
        private bool pendingGroundPound;
        private float decisionTimer;
        private float strafeSign = 1f;

        public Transform Target => target;

        public void SetTarget(Transform newTarget) => target = newTarget;

        public void Randomize()
        {
            aggression = Random.Range(0.2f, 0.95f);
            jumpiness = Random.Range(0.1f, 0.8f);
            decisionInterval = Random.Range(0.15f, 0.4f);
            strafeSign = Random.value < 0.5f ? -1f : 1f;
        }

        private void Update()
        {
            decisionTimer -= Time.deltaTime;
            if (decisionTimer > 0f) return;

            decisionTimer = decisionInterval;
            Decide();
        }

        private void Decide()
        {
            if (target == null)
            {
                currentMove = Vector2.zero;
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            float heightAboveTarget = -toTarget.y;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            Vector3 direction = distance > 0.01f ? toTarget / distance : transform.forward;

            Vector3 move;
            if (Random.value < aggression)
            {
                move = direction;
            }
            else
            {
                if (Random.value < 0.2f) strafeSign = -strafeSign;
                move = Vector3.Cross(Vector3.up, direction) * strafeSign;
            }

            currentMove = new Vector2(move.x, move.z);

            if (distance < 3.5f && Random.value < jumpiness)
            {
                pendingJump = true;
                holdJump = Random.value < 0.7f;
            }
            else if (Random.value < 0.1f)
            {
                holdJump = false;
            }

            if (distance < 1.2f && heightAboveTarget > 1.5f)
            {
                pendingGroundPound = true;
            }
        }

        public PlayerInputFrame ReadFrame()
        {
            var frame = new PlayerInputFrame
            {
                Move = currentMove,
                MoveIsWorldSpace = true,
                JumpPressed = pendingJump,
                JumpHeld = holdJump,
                RunHeld = true,
                GroundPoundPressed = pendingGroundPound
            };

            pendingJump = false;
            pendingGroundPound = false;
            return frame;
        }
    }
}
