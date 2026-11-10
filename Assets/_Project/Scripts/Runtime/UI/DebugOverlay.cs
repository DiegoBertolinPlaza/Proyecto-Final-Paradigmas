using MatriculaDeHonor.Boss;
using MatriculaDeHonor.Core;
using MatriculaDeHonor.Enemies;
using MatriculaDeHonor.GameFlow;
using MatriculaDeHonor.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MatriculaDeHonor.UI
{
    /// <summary>
    /// F1: muestra en pantalla el estado de cada máquina de estados (jugador, profesores, jefe,
    /// partida) y la sospecha de cada profesor. Muy útil para depurar y para enseñar la IA en la defensa.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        private const float RefreshInterval = 1f;

        [SerializeField] private bool visible;

        private EnemyBase[] enemies = new EnemyBase[0];
        private BossController[] bosses = new BossController[0];
        private PlayerController player;
        private float refreshTimer;
        private GUIStyle style;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                visible = !visible;
            }

            if (!visible) return;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = RefreshInterval;
                enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
                bosses = FindObjectsByType<BossController>(FindObjectsSortMode.None);
                player = FindAnyObjectByType<PlayerController>();
            }
        }

        private void OnGUI()
        {
            if (!visible) return;

            style ??= new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };

            string gameState = ServiceLocator.TryGet(out GameManager game) ? game.CurrentState.ToString() : "(sin GameManager)";
            string playerState = player != null ? player.CurrentStateName : "-";
            GUI.Label(new Rect(Screen.width - 360f, 10f, 350f, 60f),
                $"<b>Partida:</b> {gameState}\n<b>Jugador:</b> {playerState}", style);

            var cam = Camera.main;
            if (cam == null) return;

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.isActiveAndEnabled) continue;

                string text = $"{enemy.Config.displayName}\n<b>{enemy.CurrentStateName}</b>  sospecha {enemy.Perception.Suspicion01:0.00}";
                DrawWorldLabel(cam, enemy.transform.position + Vector3.up * 2.4f, text);
            }

            foreach (var boss in bosses)
            {
                if (boss == null || !boss.isActiveAndEnabled) continue;

                var cmd = boss.LastCommand;
                string text = $"{boss.Config.displayName} [{boss.BrainName}]\nvida {boss.Health}  mov ({cmd.Move.x:0.0},{cmd.Move.y:0.0}) " +
                              $"{(cmd.Throw ? "LANZA " : string.Empty)}{(cmd.Charge ? "EMBISTE" : string.Empty)}";
                DrawWorldLabel(cam, boss.transform.position + Vector3.up * 3f, text);
            }
        }

        private void DrawWorldLabel(Camera cam, Vector3 worldPosition, string text)
        {
            Vector3 screen = cam.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f) return;

            var rect = new Rect(screen.x - 150f, Screen.height - screen.y - 40f, 300f, 60f);
            GUI.Label(rect, text, style);
        }
    }
}
