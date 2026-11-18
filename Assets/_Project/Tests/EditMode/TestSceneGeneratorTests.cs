using System.Linq;
using MatriculaDeHonor.CameraSystem;
using MatriculaDeHonor.Editor;
using MatriculaDeHonor.Levels;
using MatriculaDeHonor.Player;
using MatriculaDeHonor.Player.Input;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatriculaDeHonor.Tests
{
    /// <summary>P0.8: el menú Tools/MDH/Escena de prueba crea suelo, jugador y cámara bien conectados.</summary>
    public sealed class TestSceneGeneratorTests
    {
        private const string TempScenePath = "Assets/_Project/Tests/EditMode/Temp_EscenaGenerada.unity";

        private Scene scene;

        [SetUp]
        public void SetUp()
        {
            // Aditiva, para no cerrar la escena que tengas abierta en el editor. Pero Unity no deja
            // añadir una escena nueva junto a otra sin título (la que abre el Test Runner en modo
            // batch); en ese caso la escena generada sustituye a la vacía.
            var mode = HasUntitledScene() ? NewSceneMode.Single : NewSceneMode.Additive;
            scene = TestSceneGenerator.Create(TempScenePath, mode);
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                if (SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
                else
                {
                    // No se puede cerrar la última escena: se cambia por una vacía.
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }

            AssetDatabase.DeleteAsset(TempScenePath);
        }

        private static bool HasUntitledScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)) return true;
            }

            return false;
        }

        [Test]
        public void GuardaLaEscenaEnLaRutaPedida()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(TempScenePath), Is.Not.Null);
        }

        [Test]
        public void ElSueloQuedaJustoBajoLosPiesDelJugador()
        {
            var ground = Root(TestSceneGenerator.GroundName);
            Assert.That(ground.GetComponent<BoxCollider>(), Is.Not.Null, "El suelo necesita colisión.");

            float groundTop = ground.transform.position.y + ground.transform.localScale.y * 0.5f;
            Assert.That(groundTop, Is.EqualTo(0f).Within(0.001f));

            var player = Single<PlayerController>();
            Assert.That(player.transform.position.y, Is.InRange(0f, 0.2f));
        }

        [Test]
        public void ElJugadorTieneConfigYFuenteDeInput()
        {
            var player = Single<PlayerController>();

            Assert.That(player.CompareTag("Player"));
            Assert.That(player.Config, Is.Not.Null);
            Assert.That(player.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(player.GetComponent<PlayerMotor>(), Is.Not.Null);

            var input = player.GetComponent<KeyboardGamepadInputSource>();
            Assert.That(input, Is.Not.Null);
            Assert.That(new SerializedObject(player).FindProperty("inputSourceBehaviour").objectReferenceValue, Is.SameAs(input));
        }

        [Test]
        public void ElJugadorParpadeaTrasUnGolpe()
        {
            var player = Single<PlayerController>();
            Assert.That(player.GetComponent<PlayerDamageFeedback>(), Is.Not.Null);
        }

        [Test]
        public void LaCamaraPrincipalSigueAlJugador()
        {
            var player = Single<PlayerController>();
            var rig = Single<PlatformerCameraRig>();

            Assert.That(rig.CompareTag("MainCamera"));
            Assert.That(rig.GetComponent<Camera>(), Is.Not.Null);
            Assert.That(new SerializedObject(rig).FindProperty("target").objectReferenceValue, Is.SameAs(player.transform));
        }

        [Test]
        public void HayNivelYZonaDeMuerteParaReaparecer()
        {
            var level = Single<LevelController>();
            Assert.That(level.Player, Is.SameAs(Single<PlayerController>()));

            var killZone = Single<KillZone>();
            Assert.That(killZone.transform.position.y, Is.LessThan(-5f));
        }

        private GameObject Root(string name)
        {
            var root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
            Assert.That(root, Is.Not.Null, $"Falta '{name}' en la escena.");
            return root;
        }

        private T Single<T>() where T : Component
        {
            var found = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).ToArray();
            Assert.That(found, Has.Length.EqualTo(1), $"Se esperaba un único {typeof(T).Name}.");
            return found[0];
        }
    }
}
