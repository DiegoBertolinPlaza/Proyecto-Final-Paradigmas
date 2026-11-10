using MatriculaDeHonor.Core;
using MatriculaDeHonor.GameFlow;
using UnityEngine;

namespace MatriculaDeHonor.UI
{
    /// <summary>
    /// UI PROVISIONAL con IMGUI para que el bucle completo (menú → jugar → aprobar/suspender →
    /// volver a jugar) funcione desde el primer día. Cuando hagáis la UI de verdad (UGUI o
    /// UI Toolkit), implementad IHudView y llamad a los mismos métodos del GameManager.
    /// </summary>
    public sealed class PlaceholderGameUI : MonoBehaviour, IHudView
    {
        private const float ReferenceHeight = 1080f;

        private HudPresenter presenter;
        private GameManager game;

        private int lives;
        private int credits;
        private int health;
        private int maxHealth = 1;
        private int shield;
        private int honors;
        private int honorsTotal;
        private string powerUpName;
        private float powerUpEndsAt;

        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private float stylesScale = -1f;

        private void Awake()
        {
            presenter = new HudPresenter(this);
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
        }

        // ---------- IHudView ----------
        public void SetLives(int value) => lives = value;
        public void SetCredits(int value) => credits = value;

        public void SetHealth(int current, int max, int shieldCharges)
        {
            health = current;
            maxHealth = Mathf.Max(1, max);
            shield = shieldCharges;
        }

        public void SetHonors(int collected, int total)
        {
            honors = collected;
            honorsTotal = total;
        }

        public void SetPowerUp(string displayName, bool active, float duration)
        {
            powerUpName = active ? displayName : null;
            powerUpEndsAt = duration > 0f ? Time.time + duration : float.PositiveInfinity;
        }

        // ---------- Dibujo ----------
        private void OnGUI()
        {
            if (game == null && !ServiceLocator.TryGet(out game)) return;

            float scale = Screen.height / ReferenceHeight;
            EnsureStyles(scale);

            switch (game.CurrentState)
            {
                case GameStateId.MainMenu:
                    DrawPanel("MATRÍCULA DE HONOR", "Plataformas 3D en el campus de ICAI",
                        ("Nueva partida", game.StartNewGame),
                        ("Salir", game.QuitGame));
                    break;

                case GameStateId.Loading:
                    DrawCenteredLabel("Cargando...");
                    break;

                case GameStateId.Playing:
                    DrawHud(scale);
                    break;

                case GameStateId.Paused:
                    DrawHud(scale);
                    DrawPanel("PAUSA", null,
                        ("Continuar", game.TogglePause),
                        ("Reiniciar nivel", game.RestartLevel),
                        ("Menú principal", game.ReturnToMainMenu));
                    break;

                case GameStateId.LevelComplete:
                    DrawPanel("¡APROBADO!", $"Matrículas de Honor: {honors}/{honorsTotal}   ·   ECTS: {credits}",
                        (game.HasNextLevel ? "Siguiente nivel" : "Menú principal", game.ContinueToNextLevel),
                        ("Repetir nivel", game.RestartLevel));
                    break;

                case GameStateId.GameOver:
                    DrawPanel("SUSPENSO", "Te has quedado sin convocatorias... toca la extraordinaria.",
                        ("Volver a jugar", game.StartNewGame),
                        ("Menú principal", game.ReturnToMainMenu));
                    break;

                case GameStateId.GameCompleted:
                    DrawPanel("¡MATRÍCULA DE HONOR!", $"Has terminado la carrera con {credits} ECTS.",
                        ("Volver a jugar", game.StartNewGame),
                        ("Menú principal", game.ReturnToMainMenu));
                    break;
            }
        }

        private void DrawHud(float scale)
        {
            float pad = 24f * scale;
            float lineHeight = 44f * scale;
            var rect = new Rect(pad, pad, 900f * scale, lineHeight);

            string hearts = new string('■', Mathf.Max(0, health)) + new string('□', Mathf.Max(0, maxHealth - health));
            string shieldText = shield > 0 ? "  + chuleta" : string.Empty;

            GUI.Label(rect, $"Convocatorias: {lives}", labelStyle);
            rect.y += lineHeight;
            GUI.Label(rect, $"Nota: {hearts}{shieldText}", labelStyle);
            rect.y += lineHeight;
            GUI.Label(rect, $"ECTS: {credits}", labelStyle);
            rect.y += lineHeight;
            GUI.Label(rect, $"Matrículas de Honor: {honors}/{honorsTotal}", labelStyle);

            if (!string.IsNullOrEmpty(powerUpName))
            {
                rect.y += lineHeight;
                float remaining = powerUpEndsAt - Time.time;
                string timer = float.IsInfinity(remaining) ? string.Empty : $" ({Mathf.Max(0f, remaining):0}s)";
                GUI.Label(rect, $"Power-up: {powerUpName}{timer}", labelStyle);
            }
        }

        private void DrawPanel(string title, string subtitle, params (string label, System.Action action)[] buttons)
        {
            float scale = Screen.height / ReferenceHeight;
            float width = 720f * scale;
            float buttonHeight = 72f * scale;
            float spacing = 18f * scale;
            float height = 260f * scale + buttons.Length * (buttonHeight + spacing);
            var area = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 32f * scale, area.y + 24f * scale, width - 64f * scale, height - 48f * scale));
            GUILayout.Label(title, titleStyle);

            if (!string.IsNullOrEmpty(subtitle))
            {
                GUILayout.Label(subtitle, labelStyle);
            }

            GUILayout.Space(spacing * 2f);
            foreach (var (label, action) in buttons)
            {
                if (GUILayout.Button(label, buttonStyle, GUILayout.Height(buttonHeight)))
                {
                    action?.Invoke();
                }

                GUILayout.Space(spacing);
            }

            GUILayout.EndArea();
        }

        private void DrawCenteredLabel(string text)
        {
            GUI.Label(new Rect(0f, Screen.height * 0.45f, Screen.width, 80f), text, titleStyle);
        }

        private void EnsureStyles(float scale)
        {
            if (Mathf.Approximately(scale, stylesScale) && titleStyle != null) return;
            stylesScale = scale;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(56f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            titleStyle.normal.textColor = Color.white;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(30f * scale),
                wordWrap = true
            };
            labelStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(32f * scale)
            };
        }
    }
}
