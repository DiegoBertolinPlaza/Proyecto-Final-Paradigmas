using System.Linq;
using MatriculaDeHonor.IODP.Editor;
using MatriculaDeHonor.Items;
using MatriculaDeHonor.Levels;
using MatriculaDeHonor.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatriculaDeHonor.IODP.Tests
{
    public sealed class CampusSceneTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)]
        public void AmbienteGuardadoTieneRecorridoYReferenciasValidas(int index)
        {
            string path = $"{CampusSceneGenerator.SceneFolder}/IODP_{index + 1:00}_{CampusSceneGenerator.Slugs[index]}.unity";
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null, "Generar los ambientes antes de ejecutar estos tests.");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                T[] All<T>() where T : Component => roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
                var players = All<PlayerController>();
                Assert.That(players.Length, Is.EqualTo(1));
                var playerData = new SerializedObject(players[0]);
                Assert.That(playerData.FindProperty("config").objectReferenceValue, Is.Not.Null);
                Assert.That(playerData.FindProperty("inputSourceBehaviour").objectReferenceValue, Is.Not.Null);
                Assert.That(playerData.FindProperty("cameraTransform").objectReferenceValue, Is.Not.Null);
                Assert.That(All<Camera>().Length, Is.EqualTo(1));
                Assert.That(All<LevelController>().Length, Is.EqualTo(1));
                Assert.That(All<CreditCollectible>().Length, Is.EqualTo(24));
                Assert.That(All<HonorCollectible>().Length, Is.EqualTo(3));
                Assert.That(All<Checkpoint>().Length, Is.EqualTo(1));
                Assert.That(All<LevelGoal>().Length, Is.EqualTo(1));
                Assert.That(All<KillZone>().Length, Is.EqualTo(1));
                var presentation = All<CampusPresentation>().Single();
                Assert.That(presentation.EnvironmentIndex, Is.EqualTo(index));
                Assert.That(presentation.Environments.Length, Is.EqualTo(10));
                Assert.That(presentation.Environments.All(d => d != null), Is.True);
                foreach (var root in roots)
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero,
                            "Script ausente en " + transform.name);
                Assert.That(All<Renderer>().All(r => r.sharedMaterials.All(m => m != null && m.shader != null)), Is.True,
                    "Material o shader ausente.");
                var floor = All<Transform>().Single(t => t.name == "Suelo continuo · recorrido seguro");
                Assert.That(floor.GetComponent<Collider>(), Is.Not.Null);
                Assert.That(floor.GetComponent<Collider>().bounds.Contains(players[0].transform.position + Vector3.down * 0.3f), Is.True,
                    "El punto inicial debe caer sobre el suelo de la sala.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        [Test]
        public void CatalogoYBuildPermitenRecorrerLosDiezAmbientes()
        {
            var catalog = Resources.Load<LevelCatalog>(LevelCatalog.ResourcePath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Count, Is.EqualTo(10));
            Assert.That(catalog.MainMenuScene, Is.EqualTo("IODP_00_Campus"));
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Assert.That(enabled, Does.Contain(CampusSceneGenerator.MenuPath));
            for (int i = 0; i < catalog.Count; i++)
            {
                Assert.That(catalog.Get(i).honorCount, Is.EqualTo(3));
                Assert.That(enabled.Any(p => p.EndsWith("/" + catalog.Get(i).sceneName + ".unity")), Is.True);
            }
        }
    }
}
