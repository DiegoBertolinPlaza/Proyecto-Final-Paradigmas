using System.IO;
using MatriculaDeHonor.CameraSystem;
using MatriculaDeHonor.Levels;
using MatriculaDeHonor.Player;
using MatriculaDeHonor.Player.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MatriculaDeHonor.Editor
{
    /// <summary>
    /// Patrón Builder: monta una escena paso a paso con una interfaz fluida
    /// (<c>new SceneBuilder().AddSun().AddBox(...).AddPlayer(...).Save(ruta)</c>).
    /// Cada paso sabe CÓMO se construye su pieza; qué piezas lleva cada escena lo decide quien
    /// lo usa (el "director"), por ejemplo <see cref="TestSceneGenerator"/>.
    /// </summary>
    public sealed class SceneBuilder
    {
        private const string MaterialsFolder = "Assets/_Project/Art/Prototype";

        private readonly Scene scene;
        private PlayerController player;

        public SceneBuilder(NewSceneMode mode = NewSceneMode.Single)
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);

            // Los GameObject nuevos van a la escena activa; en modo aditivo hay que activarla.
            SceneManager.SetActiveScene(scene);
        }

        public Scene Scene => scene;
        public PlayerController Player => player;

        public SceneBuilder AddSun()
        {
            var sun = new GameObject("Sol");
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            return this;
        }

        /// <summary>Caja estática con colisión. <paramref name="center"/> es el centro de la caja.</summary>
        public SceneBuilder AddBox(string name, Vector3 center, Vector3 size, PrototypeColor color)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetPositionAndRotation(center, Quaternion.identity);
            box.transform.localScale = size;
            GameObjectUtility.SetStaticEditorFlags(box, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
            ApplyMaterial(box, color);
            return this;
        }

        /// <summary>Volumen invisible que mata al jugador al caer del nivel.</summary>
        public SceneBuilder AddKillZone(Vector3 center, Vector3 size)
        {
            var zone = new GameObject("Zona de muerte");
            zone.transform.position = center;

            var trigger = zone.AddComponent<BoxCollider>();
            trigger.size = size;
            trigger.isTrigger = true;

            zone.AddComponent<KillZone>();
            return this;
        }

        /// <summary>El estudiante con su input de teclado/mando. <paramref name="feetPosition"/> = pies.</summary>
        public SceneBuilder AddPlayer(string name, Vector3 feetPosition, PlayerConfig config)
        {
            var root = new GameObject(name) { tag = "Player" };
            root.transform.position = feetPosition + Vector3.up * 0.05f;

            var body = root.AddComponent<CharacterController>();
            body.height = 1.8f;
            body.radius = 0.35f;
            body.center = new Vector3(0f, 0.9f, 0f);
            body.stepOffset = 0.3f;

            root.AddComponent<PlayerMotor>();
            var input = root.AddComponent<KeyboardGamepadInputSource>();
            player = root.AddComponent<PlayerController>();

            EditorAssets.SetField(player, "config", config);
            EditorAssets.SetField(player, "inputSourceBehaviour", input);

            AddVisual(root.transform, PrimitiveType.Capsule, "Cuerpo",
                new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 0.9f, 0.7f), PrototypeColor.Student);

            // "Nariz": deja ver hacia dónde mira el personaje.
            AddVisual(root.transform, PrimitiveType.Cube, "Frente",
                new Vector3(0f, 1.35f, 0.35f), new Vector3(0.2f, 0.2f, 0.3f), PrototypeColor.Platform);

            // Parpadea mientras es invulnerable; coge solo los Renderer de las piezas de arriba.
            root.AddComponent<PlayerDamageFeedback>();
            return this;
        }

        /// <summary>Raíz del nivel: pools, fábrica de proyectiles y reapariciones.</summary>
        public SceneBuilder AddLevelController()
        {
            var level = new GameObject("[Nivel]").AddComponent<LevelController>();
            if (player != null)
            {
                EditorAssets.SetField(level, "player", player);
            }

            return this;
        }

        /// <summary>Cámara principal semifija que sigue al jugador (si ya se ha añadido).</summary>
        public SceneBuilder AddFollowCamera(string name)
        {
            var cameraObject = new GameObject(name) { tag = "MainCamera" };
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            var rig = cameraObject.AddComponent<PlatformerCameraRig>();

            // Mismo encuadre que el offset por defecto del rig, para que en el editor ya se vea bien.
            Vector3 focus = player != null ? player.transform.position : Vector3.zero;
            var offset = new Vector3(0f, 7f, -9f);
            cameraObject.transform.SetPositionAndRotation(focus + offset, Quaternion.LookRotation(-offset + Vector3.up));

            if (player != null)
            {
                EditorAssets.SetField(rig, "target", player.transform);
                EditorAssets.SetField(player, "cameraTransform", cameraObject.transform);
            }

            return this;
        }

        public Scene Save(string path)
        {
            EditorAssets.EnsureFolder(Path.GetDirectoryName(path));
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new IOException($"No se ha podido guardar la escena en {path}.");
            }

            return scene;
        }

        private static void AddVisual(Transform parent, PrimitiveType shape, string name, Vector3 localPosition,
            Vector3 localScale, PrototypeColor color)
        {
            var visual = GameObject.CreatePrimitive(shape);
            visual.name = name;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localScale = localScale;
            ApplyMaterial(visual, color);
        }

        private static void ApplyMaterial(GameObject target, PrototypeColor color)
        {
            var material = color switch
            {
                PrototypeColor.Ground => EditorAssets.LoadOrCreateMaterial($"{MaterialsFolder}/M_Suelo.mat", new Color(0.55f, 0.6f, 0.55f)),
                PrototypeColor.Platform => EditorAssets.LoadOrCreateMaterial($"{MaterialsFolder}/M_Plataforma.mat", new Color(0.85f, 0.65f, 0.3f)),
                _ => EditorAssets.LoadOrCreateMaterial($"{MaterialsFolder}/M_Estudiante.mat", new Color(0.2f, 0.45f, 0.9f))
            };

            if (material != null)
            {
                target.GetComponent<Renderer>().sharedMaterial = material;
            }
        }
    }

    public enum PrototypeColor
    {
        Ground,
        Platform,
        Student
    }
}
