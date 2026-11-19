using MatriculaDeHonor.Core;
using MatriculaDeHonor.GameFlow;
using MatriculaDeHonor.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MatriculaDeHonor.IODP
{
    /// <summary>
    /// Selector de ambientes y ficha de prototipo. Reutiliza GameManager: no añade un segundo
    /// sistema de vida, pausa, carga ni progreso. El menú IODP sustituye solo la presentación del menú.
    /// </summary>
    public sealed class CampusPresentation : MonoBehaviour
    {
        [SerializeField] private CampusEnvironmentDefinition[] environments;
        [SerializeField] private int environmentIndex = -1;
        private GameManager game;
        private PlaceholderGameUI stockUi;
        private bool showSelector;
        private bool loading;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;

        public int EnvironmentIndex => environmentIndex;
        public CampusEnvironmentDefinition[] Environments => environments;

        private void Update()
        {
            if (game == null) ServiceLocator.TryGet(out game);
            if (environmentIndex < 0 && stockUi == null)
            {
                stockUi = FindAnyObjectByType<PlaceholderGameUI>();
                if (stockUi != null) stockUi.enabled = false;
            }

            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
                showSelector = !showSelector;
        }

        private void OnDestroy()
        {
            if (stockUi != null) stockUi.enabled = true;
        }

        static readonly Color Ink = new Color32(30, 61, 58, 255);
        static readonly Color Paper = new Color32(246, 241, 226, 250);
        static readonly Color Rule = new Color32(192, 193, 171, 255);
        static readonly Color Brass = new Color32(205, 164, 99, 255);
        static readonly Color Hover = new Color32(220, 227, 207, 255);

        private void OnGUI()
        {
            if (environments == null || environments.Length == 0) return;
            EnsureStyles();
            var previous = GUI.matrix;
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 1280f, Screen.height / 800f));
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            if (environmentIndex < 0)
            {
                float y = (height - 716) * .5f;
                Fill(new Rect(38, y + 5, 406, 716), new Color(0, 0, 0, .14f));
                Fill(new Rect(34, y, 406, 716), Paper);
                Fill(new Rect(34, y, 406, 9), Ink);
                Label(62, y + 30, 350, 25, "I O D P     /     ESCUELA POLITÉCNICA", smallStyle, Ink);
                Label(59, y + 60, 365, 76, "Cum Laude", titleStyle, Ink);
                Label(63, y + 148, 350, 29, "Directorio del campus", bodyStyle, Ink);
                Rows(62, y + 197, 350, 42);
                Fill(new Rect(62, y + 642, 350, 1), Rule);
                Label(62, y + 654, 350, 25, "WASD   Moverse       Espacio   Saltar", smallStyle, Ink);
                Label(62, y + 678, 350, 24, "F2   Directorio       Esc   Pausa", smallStyle, Ink);
            }
            else if (environmentIndex < environments.Length && environments[environmentIndex] != null)
            {
                var item = environments[environmentIndex];
                float x = width - 288;
                Fill(new Rect(x, 19, 265, 54), Ink);
                Fill(new Rect(x, 19, 4, 54), Brass);
                Label(x + 14, 24, 188, 44, $"{item.number:00}   {item.title}", bodyStyle, Paper);
                Label(x + 221, 28, 34, 36, "F2", smallStyle, Brass);
                if (showSelector)
                {
                    Fill(new Rect(x, 78, 265, 386), Paper);
                    Rows(x + 14, 88, 237, 36);
                }
            }
            if (loading)
            {
                Fill(new Rect(width - 252, height - 62, 228, 40), Ink);
                Label(width - 237, height - 60, 210, 35, "Cargando ambiente…", smallStyle, Paper);
            }
            GUI.matrix = previous;
        }

        private void Rows(float x, float y, float width, float pitch)
        {
            for (int i = 0; i < environments.Length; i++)
            {
                var item = environments[i];
                if (item == null) continue;
                var rect = new Rect(x, y + i * pitch, width, pitch);
                bool over = rect.Contains(Event.current.mousePosition);
                if (over || i == environmentIndex) Fill(rect, Hover);
                Fill(new Rect(rect.x, rect.y, width, 1), Rule);
                Label(x + 7, rect.y, 36, pitch, $"{item.number:00}", smallStyle, Ink);
                Label(x + 50, rect.y, width - 85, pitch, item.title, buttonStyle, Ink);
                Label(x + width - 23, rect.y, 22, pitch, "›", bodyStyle, Ink);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) Load(i);
            }
        }

        static void Fill(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        static void Label(float x, float y, float width, float height, string text, GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            GUI.Label(new Rect(x, y, width, height), text, style);
        }


        private void Load(int index)
        {
            if (loading || game == null || game.Catalog == null || index >= game.Catalog.Count) return;
            loading = true;
            game.LoadLevel(index);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 56, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleLeft };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            buttonStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleLeft };
        }
    }
}
