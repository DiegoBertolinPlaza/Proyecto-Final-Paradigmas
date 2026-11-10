using MatriculaDeHonor.GameFlow;
using MatriculaDeHonor.Levels;
using MatriculaDeHonor.UI;
using UnityEngine;

namespace MatriculaDeHonor.Bootstrap
{
    /// <summary>
    /// Raíz de composición global: al arrancar CUALQUIER escena crea el objeto persistente
    /// [Systems] con el GameManager y la UI. Así se puede dar a Play directamente en un nivel
    /// sin pasar por el menú, y en la build todo funciona igual.
    /// </summary>
    public static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindAnyObjectByType<GameFlowOptOut>() != null) return;
            if (Object.FindAnyObjectByType<GameManager>() != null) return;

            var catalog = Resources.Load<LevelCatalog>(LevelCatalog.ResourcePath);
            if (catalog == null)
            {
                Debug.LogWarning("[Bootstrap] No se encuentra Resources/LevelCatalog. Usa el menú " +
                                 "'Matrícula de Honor > Generar contenido base' del editor.");
            }

            var root = new GameObject("[Systems]");
            var manager = root.AddComponent<GameManager>();
            manager.Initialize(catalog);
            root.AddComponent<PlaceholderGameUI>();
            root.AddComponent<DebugOverlay>();
        }
    }
}
