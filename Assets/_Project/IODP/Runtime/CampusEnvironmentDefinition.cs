using UnityEngine;

namespace MatriculaDeHonor.IODP
{
    /// <summary>Datos del ambiente; el mismo asset alimenta el selector y la ficha de cada escena.</summary>
    [CreateAssetMenu(menuName = "Cum Laude/IODP/Ambiente")]
    public sealed class CampusEnvironmentDefinition : ScriptableObject
    {
        [Range(1, 10)] public int number = 1;
        public string title;
        public string sceneName;
        public Color accent = new Color(0.9f, 0.6f, 0.2f);
        [TextArea(2, 5)] public string inspiration;
        [TextArea(2, 5)] public string gameplayIdea;
        public bool invented;
        public bool awaitingReference;
    }
}
