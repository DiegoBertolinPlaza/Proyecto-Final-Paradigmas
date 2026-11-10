using UnityEngine;

namespace MatriculaDeHonor.Levels
{
    /// <summary>Un nivel descrito como datos: añadir un nivel nuevo es crear un asset, no programar.</summary>
    [CreateAssetMenu(menuName = "Matrícula de Honor/Niveles/Nivel", fileName = "Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public string displayName = "Entrada";
        [Tooltip("Zona del campus: Hall, Aulas, Cafetería, Baños, Biblioteca...")]
        public string campusZone = "Hall";
        [Tooltip("Nombre exacto de la escena (debe estar en Build Settings).")]
        public string sceneName;
        [Range(1, 10)] public int difficulty = 1;
        [Min(0)] public int honorCount = 3;
        [TextArea] public string introText;
    }
}
