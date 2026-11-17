using System.Collections.Generic;
using System.Linq;
using MatriculaDeHonor.Boss;
using MatriculaDeHonor.ML;
using MatriculaDeHonor.ML.Editor;
using MatriculaDeHonor.Player;
using NUnit.Framework;
using Unity.InferenceEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MatriculaDeHonor.Tests.ML
{
    /// <summary>
    /// P0.6–7: la escena de entrenamiento está bien conectada y el modelo que sale del
    /// entrenamiento de humo lo puede usar el BossAgent (mismas entradas y salidas).
    /// </summary>
    public sealed class BossTrainingTests
    {
        public const string SmokeModelPath = "Assets/_Project/ML/Models/TribunalBoss_smoke.onnx";

        [Test]
        public void LaEscenaDeEntrenamientoTieneTodasLasArenasConectadas()
        {
            var scene = EditorSceneManager.OpenScene(BossTrainingSceneBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var arenas = scene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<BossTrainingArena>(true))
                    .ToArray();
                Assert.That(arenas, Has.Length.EqualTo(BossTrainingSceneBuilder.ArenaCount));

                foreach (var arena in arenas)
                {
                    var serialized = new SerializedObject(arena);
                    foreach (string field in new[] { "area", "boss", "agent", "playerBot", "botInput" })
                    {
                        Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null,
                            $"{arena.name}: falta '{field}'.");
                    }

                    var boss = (BossController)serialized.FindProperty("boss").objectReferenceValue;
                    var bossSerialized = new SerializedObject(boss);
                    Assert.That(bossSerialized.FindProperty("isMainBoss").boolValue, Is.False, $"{arena.name}: el jefe lanzaría eventos globales.");
                    Assert.That(bossSerialized.FindProperty("brainBehaviour").objectReferenceValue, Is.InstanceOf<BossAgent>());

                    var bot = (PlayerController)serialized.FindProperty("playerBot").objectReferenceValue;
                    Assert.That(bot.IsMainPlayer, Is.False, $"{arena.name}: el bot se registraría como el jugador.");
                    Assert.That(bot.Config, Is.Not.Null);

                    var agents = boss.GetComponents<Agent>();
                    Assert.That(agents, Has.Length.EqualTo(1), $"{arena.name}: el jefe debe tener un único Agent.");
                    Assert.That(agents[0], Is.InstanceOf<BossAgent>(), $"{arena.name}: las decisiones irían a un Agent genérico.");
                    Assert.That(agents[0].MaxStep, Is.EqualTo(BossTrainingSceneBuilder.AgentMaxStep));

                    var parameters = boss.GetComponent<BehaviorParameters>();
                    Assert.That(parameters.BehaviorName, Is.EqualTo(BossAgent.BehaviorName));
                    Assert.That(parameters.BrainParameters.VectorObservationSize, Is.EqualTo(BossObservation.Size));
                    Assert.That(parameters.BrainParameters.ActionSpec.NumContinuousActions, Is.EqualTo(BossAgent.ContinuousActionCount));
                    Assert.That(parameters.BrainParameters.ActionSpec.BranchSizes, Is.EqualTo(new[] { BossAgent.AttackBranchSize }));
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void ElModeloDeHumoEncajaConElBossAgent()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ModelAsset>(SmokeModelPath);
            Assert.That(asset, Is.Not.Null, $"Falta {SmokeModelPath}: copia ahí el .onnx del entrenamiento de humo.");

            var model = ModelLoader.Load(asset);

            var observations = model.inputs.Single(input => input.name == "obs_0");
            Assert.That(observations.shape.Get(observations.shape.rank - 1), Is.EqualTo(BossObservation.Size),
                "El modelo espera otro número de observaciones que el BossAgent.");

            var outputNames = model.outputs.Select(output => output.name).ToArray();
            Assert.That(outputNames, Does.Contain("continuous_actions"));
            Assert.That(outputNames, Does.Contain("discrete_actions"));

            // Inferencia de verdad con una observación vacía: debe devolver 2 acciones continuas y 1 discreta.
            var inputs = new List<Tensor>();
            using var worker = new Worker(model, BackendType.CPU);
            try
            {
                foreach (var input in model.inputs)
                {
                    int width = input.shape.Get(input.shape.rank - 1);
                    var shape = new TensorShape(1, width);
                    inputs.Add(input.name == "action_masks"
                        ? new Tensor<float>(shape, Enumerable.Repeat(1f, width).ToArray())
                        : new Tensor<float>(shape));
                }

                worker.Schedule(inputs.ToArray());

                var continuous = (Tensor<float>)worker.PeekOutput("continuous_actions");
                var discrete = worker.PeekOutput("discrete_actions");
                Assert.That(continuous.shape[-1], Is.EqualTo(BossAgent.ContinuousActionCount));
                Assert.That(discrete.shape[-1], Is.EqualTo(1), "Una rama discreta: nada / lanzar / embestir.");
            }
            finally
            {
                foreach (var tensor in inputs) tensor.Dispose();
            }
        }
    }
}
