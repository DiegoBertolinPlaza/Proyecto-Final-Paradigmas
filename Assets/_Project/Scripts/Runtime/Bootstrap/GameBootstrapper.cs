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
                Debug.LogWarning("[Bootstrap] Aún no hay Resources/LevelCatalog: el GameManager arranca sin " +
                                 "lista de niveles (normal en la escena de prueba).");
            }

            var root = new GameObject("[Systems]");
            var manager = root.AddComponent<GameManager>();
            manager.Initialize(catalog);
            root.AddComponent<PlaceholderGameUI>();
            root.AddComponent<DebugOverlay>();
        }
    }
}
