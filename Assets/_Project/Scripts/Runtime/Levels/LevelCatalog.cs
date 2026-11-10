using System.Collections.Generic;
using UnityEngine;

namespace MatriculaDeHonor.Levels
{
    /// <summary>
    /// Lista ordenada de niveles (de fácil a difícil) y la escena del menú. Vive en Resources
    /// para que el GameManager la encuentre al arrancar desde cualquier escena.
    /// </summary>
    [CreateAssetMenu(menuName = "Matrícula de Honor/Niveles/Catálogo", fileName = "LevelCatalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public const string ResourcePath = "LevelCatalog";

        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

        public string MainMenuScene => mainMenuScene;
        public int Count => levels.Count;
        public IReadOnlyList<LevelDefinition> Levels => levels;

        public LevelDefinition Get(int index)
        {
            return index >= 0 && index < levels.Count ? levels[index] : null;
        }

        public bool TryGetIndex(string sceneName, out int index)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null && levels[i].sceneName == sceneName)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public bool IsLastLevel(int index) => index >= levels.Count - 1;

#if UNITY_EDITOR
        public void EditorSetup(string menuScene, IEnumerable<LevelDefinition> definitions)
        {
            mainMenuScene = menuScene;
            levels = new List<LevelDefinition>(definitions);
        }
#endif
    }
}
