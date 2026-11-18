using MatriculaDeHonor.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatriculaDeHonor.Editor
{
    /// <summary>
    /// Tools/MDH/Escena de prueba (P0.8): suelo, escalones, una pasarela con hueco, el estudiante y
    /// la cámara. Es el "director" del Builder: aquí se decide QUÉ lleva la escena y
    /// <see cref="SceneBuilder"/> sabe CÓMO montar cada pieza.
    /// </summary>
    public static class TestSceneGenerator
    {
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox/EscenaDePrueba.unity";
        public const string PlayerConfigPath = "Assets/_Project/Data/Player/PlayerConfig.asset";

        public const string GroundName = "Suelo";
        public const string PlayerName = "Estudiante";
        public const string CameraName = "Main Camera";

        [MenuItem("Tools/MDH/Escena de prueba", priority = 0)]
        private static void CreateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            if (exists && !EditorUtility.DisplayDialog("Escena de prueba",
                    $"Ya existe {ScenePath}.\n¿La rehago desde cero?", "Rehacer", "Cancelar"))
            {
                return;
            }

            Create(ScenePath);
            Selection.activeGameObject = GameObject.Find(PlayerName);
            Debug.Log($"[MDH] Escena de prueba en {ScenePath}. Play: WASD/flechas mover, Espacio saltar, " +
                      "Shift correr, Ctrl/C culetazo, F1 depuración.");
        }

        /// <summary>Entrada para modo batch: <c>-executeMethod MatriculaDeHonor.Editor.TestSceneGenerator.CreateFromCommandLine</c>.</summary>
        public static void CreateFromCommandLine() => Create(ScenePath);

        public static Scene Create(string path, NewSceneMode mode = NewSceneMode.Single)
        {
            // Primero la escena y luego los assets: abrir una escena nueva descarga los assets que no
            // usa nadie, y un PlayerConfig cargado antes llegaría ya destruido (config = null).
            var builder = new SceneBuilder(mode);
            var config = EditorAssets.LoadOrCreate<PlayerConfig>(PlayerConfigPath);

            return builder
                .AddSun()
                .AddBox(GroundName, new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), PrototypeColor.Ground)
                // Escalones de 0.6, 1.4 y 2.2 m: el último roza el salto máximo (2.4 m por defecto).
                .AddBox("Escalón bajo", new Vector3(4f, 0.3f, 6f), new Vector3(3f, 0.6f, 3f), PrototypeColor.Platform)
                .AddBox("Escalón medio", new Vector3(8f, 0.7f, 6f), new Vector3(3f, 1.4f, 3f), PrototypeColor.Platform)
                .AddBox("Escalón alto", new Vector3(12f, 1.1f, 6f), new Vector3(3f, 2.2f, 3f), PrototypeColor.Platform)
                // Dos pasarelas de 1.4 m con un hueco de 3 m: coyote time y salto corriendo.
                .AddBox("Pasarela A", new Vector3(-8f, 0.7f, 8f), new Vector3(3f, 1.4f, 8f), PrototypeColor.Platform)
                .AddBox("Pasarela B", new Vector3(-8f, 0.7f, 19f), new Vector3(3f, 1.4f, 8f), PrototypeColor.Platform)
                .AddKillZone(new Vector3(0f, -15f, 0f), new Vector3(300f, 2f, 300f))
                .AddPlayer(PlayerName, Vector3.zero, config)
                .AddLevelController()
                .AddFollowCamera(CameraName)
                .Save(path);
        }
    }
}
