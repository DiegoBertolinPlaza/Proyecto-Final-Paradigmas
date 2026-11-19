using System.IO;
using MatriculaDeHonor.Boss;
using MatriculaDeHonor.Combat;
using MatriculaDeHonor.GameFlow;
using MatriculaDeHonor.Levels;
using MatriculaDeHonor.Player;
using MatriculaDeHonor.Player.Input;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatriculaDeHonor.ML.Editor
{
    /// <summary>
    /// Tools/MDH/ML/Crear escena de entrenamiento del jefe (P0.6–7).
    /// Monta <see cref="ArenaCount"/> arenas idénticas (jefe + jugador bot) en rejilla para que
    /// ML-Agents recoja experiencia en paralelo. Todas salen del mismo método (Factory Method),
    /// así son iguales y el agente no aprende manías de una arena concreta.
    /// </summary>
    public static class BossTrainingSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/ML/Scenes/BossTraining.unity";
        public const string PlayerConfigPath = "Assets/_Project/ML/Data/TrainingPlayerConfig.asset";
        public const string BossConfigPath = "Assets/_Project/ML/Data/TrainingBossConfig.asset";
        public const string ChalkPrefabPath = "Assets/_Project/ML/Prefabs/TrainingChalk.prefab";

        public const int ArenaCount = 8;
        public const int AgentMaxStep = 3000;
        public const int DecisionPeriod = 5;

        private const float ArenaSpacing = 40f;
        private const float ArenaHalfSize = 10f;

        [MenuItem("Tools/MDH/ML/Crear escena de entrenamiento del jefe", priority = 100)]
        private static void CreateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Create(ScenePath);
            Debug.Log($"[MDH] Escena de entrenamiento en {ScenePath} con {ArenaCount} arenas.");
        }

        /// <summary>Para -executeMethod en modo batch.</summary>
        public static void CreateFromCommandLine() => Create(ScenePath);

        public static Scene Create(string path, NewSceneMode mode = NewSceneMode.Single)
        {
            // Primero la escena y luego los assets: abrir una escena nueva descarga los assets que no
            // usa nadie, y lo cargado antes llegaría ya destruido.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            SceneManager.SetActiveScene(scene);

            var playerConfig = LoadOrCreate<PlayerConfig>(PlayerConfigPath);
            var bossConfig = LoadOrCreate<BossConfig>(BossConfigPath);
            var chalk = LoadOrCreateChalkPrefab(ChalkPrefabPath);

            var sun = new GameObject("Sol").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var root = new GameObject("[Entrenamiento]");
            root.AddComponent<GameFlowOptOut>();  // sin GameManager, menús ni vidas
            root.AddComponent<LevelController>(); // pools y fábrica de proyectiles para las tizas

            int columns = Mathf.CeilToInt(Mathf.Sqrt(ArenaCount));
            for (int i = 0; i < ArenaCount; i++)
            {
                var origin = new Vector3(i % columns * ArenaSpacing, 0f, i / columns * ArenaSpacing);
                CreateArena(i, origin, playerConfig, bossConfig, chalk);
            }

            CreateOverviewCamera(columns);

            EnsureFolder(Path.GetDirectoryName(path));
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException($"No se ha podido guardar la escena en {path}.");
            }

            return scene;
        }

        private static void CreateArena(int index, Vector3 origin, PlayerConfig playerConfig, BossConfig bossConfig, Projectile chalk)
        {
            var arenaRoot = new GameObject($"Arena {index + 1:00}");
            arenaRoot.transform.position = origin;

            var area = arenaRoot.AddComponent<BossArena>();
            SetVector2(area, "halfExtents", new Vector2(ArenaHalfSize, ArenaHalfSize));
            var training = arenaRoot.AddComponent<BossTrainingArena>();

            const float h = ArenaHalfSize;
            const float side = 2f * h + 2f;
            AddBox(arenaRoot.transform, "Suelo", new Vector3(0f, -0.5f, 0f), new Vector3(side, 1f, side));
            AddBox(arenaRoot.transform, "Pared norte", new Vector3(0f, 1.5f, h + 0.5f), new Vector3(side, 3f, 1f));
            AddBox(arenaRoot.transform, "Pared sur", new Vector3(0f, 1.5f, -h - 0.5f), new Vector3(side, 3f, 1f));
            AddBox(arenaRoot.transform, "Pared este", new Vector3(h + 0.5f, 1.5f, 0f), new Vector3(1f, 3f, side));
            AddBox(arenaRoot.transform, "Pared oeste", new Vector3(-h - 0.5f, 1.5f, 0f), new Vector3(1f, 3f, side));

            // Jugador bot: el MISMO PlayerController que el humano, movido por PlayerBotInputSource.
            var botObject = new GameObject("Jugador bot");
            botObject.transform.SetParent(arenaRoot.transform, false);
            botObject.transform.localPosition = new Vector3(0f, 0.05f, -5f);

            var botBody = botObject.AddComponent<CharacterController>();
            botBody.height = 1.8f;
            botBody.radius = 0.35f;
            botBody.center = new Vector3(0f, 0.9f, 0f);
            botBody.stepOffset = 0.3f;

            botObject.AddComponent<PlayerMotor>();
            var botInput = botObject.AddComponent<PlayerBotInputSource>();
            var player = botObject.AddComponent<PlayerController>();
            SetObject(player, "config", playerConfig);
            SetObject(player, "inputSourceBehaviour", botInput);
            SetBool(player, "isMainPlayer", false);
            AddVisual(botObject.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 0.9f, 0.7f));

            // Jefe: cuerpo (BossController) + cerebro (BossAgent) + lo que pide ML-Agents.
            var bossObject = new GameObject("El Tribunal");
            bossObject.transform.SetParent(arenaRoot.transform, false);
            bossObject.transform.SetLocalPositionAndRotation(new Vector3(0f, 0.05f, 5f), Quaternion.LookRotation(Vector3.back));

            var bossBody = bossObject.AddComponent<CharacterController>();
            bossBody.height = 2.2f;
            bossBody.radius = 0.6f;
            bossBody.center = new Vector3(0f, 1.1f, 0f);

            var boss = bossObject.AddComponent<BossController>();

            var parameters = bossObject.AddComponent<BehaviorParameters>();
            parameters.BehaviorName = BossAgent.BehaviorName;
            parameters.BehaviorType = BehaviorType.Default; // entrena si hay Python conectado; si no, heurística
            parameters.BrainParameters.VectorObservationSize = BossObservation.Size;
            parameters.BrainParameters.NumStackedVectorObservations = 1;
            parameters.BrainParameters.ActionSpec = new ActionSpec(BossAgent.ContinuousActionCount, new[] { BossAgent.AttackBranchSize });

            // BossAgent exige un DecisionRequester, y este a su vez un Agent: al resolver esa cadena
            // Unity puede colar un Agent genérico vacío, que sería el que recibe las decisiones y
            // dejaría al BossAgent sin hacer nada. Si aparece, se quita.
            var agent = bossObject.AddComponent<BossAgent>();
            agent.MaxStep = AgentMaxStep;
            foreach (var other in bossObject.GetComponents<Agent>())
            {
                if (other != agent) Object.DestroyImmediate(other);
            }

            var requester = bossObject.GetComponent<DecisionRequester>();
            if (requester == null) requester = bossObject.AddComponent<DecisionRequester>();
            requester.DecisionPeriod = DecisionPeriod;
            requester.TakeActionsBetweenDecisions = true;

            var throwOrigin = new GameObject("Origen del lanzamiento").transform;
            throwOrigin.SetParent(bossObject.transform, false);
            throwOrigin.localPosition = new Vector3(0f, 1.6f, 0.7f);

            SetObject(boss, "config", bossConfig);
            SetObject(boss, "arena", area);
            SetObject(boss, "projectilePrefab", chalk);
            SetObject(boss, "throwOrigin", throwOrigin);
            SetObject(boss, "brainBehaviour", agent);
            SetObject(boss, "explicitTargetBehaviour", player);
            SetBool(boss, "isMainBoss", false);
            AddVisual(bossObject.transform, new Vector3(0f, 1.1f, 0f), new Vector3(1.2f, 1.1f, 1.2f));

            SetObject(training, "area", area);
            SetObject(training, "boss", boss);
            SetObject(training, "agent", agent);
            SetObject(training, "playerBot", player);
            SetObject(training, "botInput", botInput);
        }

        private static void CreateOverviewCamera(int columns)
        {
            int rows = Mathf.CeilToInt((float)ArenaCount / columns);
            var center = new Vector3((columns - 1) * ArenaSpacing * 0.5f, 0f, (rows - 1) * ArenaSpacing * 0.5f);

            var cameraObject = new GameObject("Cámara general") { tag = "MainCamera" };
            cameraObject.AddComponent<Camera>();
            cameraObject.transform.SetPositionAndRotation(center + new Vector3(0f, 90f, -45f),
                Quaternion.LookRotation(new Vector3(0f, -90f, 45f)));
        }

        // ---------- Utilidades ----------

        private static void AddBox(Transform parent, string name, Vector3 localCenter, Vector3 size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localCenter;
            box.transform.localScale = size;
        }

        private static void AddVisual(Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Cuerpo";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
        }

        private static Projectile LoadOrCreateChalkPrefab(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Projectile>(path);
            if (existing != null) return existing;

            EnsureFolder(Path.GetDirectoryName(path));

            // El proyectil no necesita collider: avanza con un SphereCast por frame.
            var chalk = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chalk.name = "Tiza";
            Object.DestroyImmediate(chalk.GetComponent<Collider>());
            chalk.transform.localScale = Vector3.one * 0.3f;
            chalk.AddComponent<Projectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(chalk, path);
            Object.DestroyImmediate(chalk);
            return prefab.GetComponent<Projectile>();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureFolder(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static SerializedProperty Find(SerializedObject serialized, string field)
        {
            return serialized.FindProperty(field) ??
                   throw new System.ArgumentException($"{serialized.targetObject.GetType().Name} no tiene el campo '{field}'.");
        }

        private static void SetObject(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = Find(serialized, field);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Unity deja el campo a null sin avisar si el objeto no encaja o ya está destruido.
            if (value != null && property.objectReferenceValue != value)
            {
                throw new System.ArgumentException($"{target.GetType().Name}.{field} no ha aceptado '{value.name}'.");
            }
        }

        private static void SetBool(Object target, string field, bool value)
        {
            var serialized = new SerializedObject(target);
            Find(serialized, field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector2(Object target, string field, Vector2 value)
        {
            var serialized = new SerializedObject(target);
            Find(serialized, field).vector2Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
