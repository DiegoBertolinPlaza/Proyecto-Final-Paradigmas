using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MatriculaDeHonor.CameraSystem;
using MatriculaDeHonor.Editor;
using MatriculaDeHonor.Items;
using MatriculaDeHonor.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MatriculaDeHonor.IODP.Editor
{
    /// <summary>
    /// Director del Builder: diez ambientes con un kit modular de prefabs (Factory) y fichas
    /// ScriptableObject. Solo escribe en IODP; nunca sobrescribe escenas existentes por defecto.
    /// </summary>
    public static class CampusSceneGenerator
    {
        public const string Root = CampusGeometry.RootPath;
        public const string SceneFolder = Root + "/Scenes";
        public const string MenuPath = SceneFolder + "/IODP_00_Campus.unity";
        public static readonly string[] Titles = { "Entrada", "Aulas", "Baños", "Cafetería", "La Revisión", "Biblioteca", "Laboratorios", "Ordenadores", "Despachos", "El Tribunal" };
        public static readonly string[] Slugs = { "Entrada", "Aulas", "Banos", "Cafeteria", "Revision", "Biblioteca", "Laboratorios", "Ordenadores", "Despachos", "Tribunal" };
        private static bool rebuildForValidation;
        private static readonly string[] Inspiration =
        {
            "Fotos 2, 3, 7 y 15: fachada pétrea, acceso acristalado, recepción y carteles. Escuela ficticia IODP.",
            "Fotos 1 y 6: pupitres claros, faldones verdes, pizarra blanca y ventanas altas.",
            "Fotos 13, 14 y 21: baldosas cálidas, lavabos alineados, espejos y cabinas de madera.",
            "Fotos 4, 5 y 8: pilares blancos, franja de madera, mesas y mostrador al fondo.",
            "Foto 17: capilla reinterpretada como sala de revisión, atril, bancos y luz cálida. Sin personajes reales.",
            "Inventada con autorización: estanterías, lectura y altillo, usando verde y madera del resto del campus.",
            "Fotos 9, 10, 11 y 20: vitrinas, bloques motor y bancadas amarillas; laboratorio mecánico.",
            "Inventada con autorización: monitores, mesas y sala de servidores, manteniendo la paleta del campus.",
            "Fotos 16 y 18: pasillos largos, zócalo verde, marcos de madera y puertas repetidas.",
            "Aula grande provisional. Pendiente de la foto prometida: grada lateral, tarima y mesa de tribunal."
        };
        private static readonly string[] Ideas =
        {
            "Aprender a moverse: entrada → recepción → tornos abiertos → descansillo → matrícula. Reserva para el bedel.",
            "Pupitres como plataformas; pasillo central libre, pizarra al fondo y coberturas para futuras tizas.",
            "Ruta entre lavabos y cabinas. El suelo mojado solo es visual en esta fase; el deslizamiento queda pendiente.",
            "Salto entre mesas y zona de bandejas. Espacio reservado para el futuro power-up Café.",
            "Arena despejada de minijefe. Las bancadas laterales son coberturas; aún no hay jefe ni bloqueo de meta.",
            "Pasillos entre estanterías y rincón de lectura; escalera inventada hacia altillo. Reserva de la bibliotecaria.",
            "Bancadas, motores y perímetros amarillos. Vitrinas abiertas estilizadas; sin químicos activos todavía.",
            "Mesas de informática y servidores laterales. Canaleta central y plataformas; láseres aún no implementados.",
            "Secuencia de despachos y sala de reuniones, con puertas alternas y plataformas como atajo.",
            "Arena final provisional con tarima y público vacío. Ambientación construida; combate y profesor final pendientes."
        };

        [MenuItem("Tools/IODP/Crear los 10 ambientes (sin sobrescribir)", priority = 10)]
        public static void CreateFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateAll();
        }

        /// <summary>Entrada batch: -executeMethod MatriculaDeHonor.IODP.Editor.CampusSceneGenerator.CreateAll.</summary>
        public static void CreateAll()
        {
            EditorAssets.EnsureFolder(SceneFolder);
            var definitions = new CampusEnvironmentDefinition[10];
            var levels = new List<LevelDefinition>();
            for (int i = 0; i < 10; i++)
            {
                var definition = EditorAssets.LoadOrCreate<CampusEnvironmentDefinition>($"{Root}/Data/Ambiente_{i + 1:00}.asset");
                definition.number = i + 1;
                definition.title = Titles[i];
                definition.sceneName = $"IODP_{i + 1:00}_{Slugs[i]}";
                definition.inspiration = Inspiration[i];
                definition.gameplayIdea = Ideas[i];
                definition.invented = i == 5 || i == 7;
                definition.awaitingReference = i == 9;
                definition.accent = CampusGeometry.Mat(i == 5 ? "verde" : i == 7 ? "azul" : "ambar").color;
                EditorUtility.SetDirty(definition);
                definitions[i] = definition;

                var level = EditorAssets.LoadOrCreate<LevelDefinition>($"{Root}/Data/Nivel_{i + 1:00}.asset");
                level.displayName = Titles[i];
                level.campusZone = "IODP / " + Titles[i];
                level.sceneName = definition.sceneName;
                level.difficulty = i + 1;
                level.honorCount = 3;
                level.introText = Ideas[i];
                EditorUtility.SetDirty(level);
                levels.Add(level);
            }
            AssetDatabase.SaveAssets();

            for (int i = 0; i < definitions.Length; i++)
            {
                string path = $"{SceneFolder}/{definitions[i].sceneName}.unity";
                if (rebuildForValidation || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    CreateEnvironment(i, definitions, path);
            }
            if (rebuildForValidation || AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath) == null) CreateMenu(definitions);

            // Un solo catálogo, localizado dentro de IODP. Si el equipo ya tiene otro, no se pisa.
            var existing = Resources.Load<LevelCatalog>(LevelCatalog.ResourcePath);
            string catalogPath = Root + "/Resources/LevelCatalog.asset";
            if (existing != null && AssetDatabase.GetAssetPath(existing) != catalogPath)
                throw new InvalidOperationException("Ya hay otro Resources/LevelCatalog. Integrar manualmente para no pisarlo.");
            var catalog = EditorAssets.LoadOrCreate<LevelCatalog>(catalogPath);
            // NewScene descarga assets que no usa la escena anterior. Recargar las referencias
            // evita guardar UnityEngine.Object destruidos como {fileID: 0}.
            catalog.EditorSetup("IODP_00_Campus", Enumerable.Range(1, 10)
                .Select(n => AssetDatabase.LoadAssetAtPath<LevelDefinition>($"{Root}/Data/Nivel_{n:00}.asset")));
            EditorUtility.SetDirty(catalog);

            var iodpBuild = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuPath, true) };
            iodpBuild.AddRange(definitions.Select(d => new EditorBuildSettingsScene($"{SceneFolder}/{d.sceneName}.unity", true)));
            iodpBuild.AddRange(EditorBuildSettings.scenes.Where(s => !s.path.StartsWith(SceneFolder + "/", StringComparison.Ordinal)));
            EditorBuildSettings.scenes = iodpBuild.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(MenuPath);
            Debug.Log("[IODP] 10 ambientes, 16 prefabs modulares, catálogo y selector preparados. Abrir IODP_00_Campus y Play.");
        }

        /// <summary>Solo para revisar este prototipo generado; reemplaza sus diez escenas, no sus prefabs.</summary>
        public static void RebuildGeneratedScenesForValidation()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Cerrar Play y guardar la escena antes de reconstruir.");
            rebuildForValidation = true;
            try { CreateAll(); }
            finally { rebuildForValidation = false; }
        }

        private static void CreateEnvironment(int index, CampusEnvironmentDefinition[] definitions, string path)
        {
            var builder = new SceneBuilder().AddSun();
            var root = new GameObject($"IODP / {index + 1:00} {Titles[index]}").transform;
            var g = new CampusGeometry(root);
            var props = new CampusPropFactory();
            g.Room(Titles[index], index == 4 ? "oscuro" : index == 5 ? "madera" : "suelo", index == 4);
            switch (index)
            {
                case 0: Entrance(g, props); break;
                case 1: Classroom(g, props); break;
                case 2: Bathrooms(g, props); break;
                case 3: Cafeteria(g, props); break;
                case 4: Chapel(g, props); break;
                case 5: Library(g, props); break;
                case 6: Laboratory(g, props); break;
                case 7: Computers(g, props); break;
                case 8: Offices(g, props); break;
                case 9: Tribunal(g, props); break;
            }
            AddCommonRoute(g, index);
            var config = EditorAssets.LoadOrCreate<MatriculaDeHonor.Player.PlayerConfig>(Root + "/Data/IODP_PlayerConfig.asset");
            builder.AddPlayer("Estudiante IODP", new Vector3(0, 0.08f, -23), config)
                .AddLevelController().AddFollowCamera("Main Camera")
                .AddKillZone(new Vector3(0, -10, 2), new Vector3(200, 2, 200));

            var camera = Camera.main;
            if (camera != null)
            {
                camera.fieldOfView = 52;
                camera.farClipPlane = 180;
                camera.backgroundColor = CampusGeometry.Hex("CCC8B5");
                camera.clearFlags = CameraClearFlags.SolidColor;
                var rig = camera.GetComponent<PlatformerCameraRig>();
                var serial = new SerializedObject(rig);
                serial.FindProperty("offset").vector3Value = new Vector3(0, 10, -13);
                serial.ApplyModifiedPropertiesWithoutUndo();
                camera.transform.position = builder.Player.transform.position + new Vector3(0, 10, -13);
                camera.transform.rotation = Quaternion.LookRotation(new Vector3(0, -9, 13));
            }
            AddPresentation(definitions, index);
            SetLighting(index);
            CampusArchitecture.Dress(root, index, camera);
            builder.Save(path);
        }

        private static void CreateMenu(CampusEnvironmentDefinition[] definitions)
        {
            var builder = new SceneBuilder().AddSun();
            var root = new GameObject("IODP / Maqueta de bienvenida").transform;
            var g = new CampusGeometry(root);
            g.Room("Escuela IODP");
            Entrance(g, new CampusPropFactory());
            AddPresentation(definitions, -1);
            var camera = new GameObject("Main Camera") { tag = "MainCamera" };
            var lens = camera.AddComponent<Camera>();
            camera.AddComponent<AudioListener>();
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = CampusGeometry.Hex("C9C4B3");
            lens.fieldOfView = 47;
            camera.transform.position = new Vector3(32, 25, -36);
            camera.transform.LookAt(new Vector3(-2, 1, 1));
            SetLighting(0);
            CampusArchitecture.Dress(root, -1, lens);
            builder.Save(MenuPath);
        }

        private static void AddPresentation(CampusEnvironmentDefinition[] definitions, int index)
        {
            var view = new GameObject("[IODP · Selector y ficha]").AddComponent<CampusPresentation>();
            var serial = new SerializedObject(view);
            var list = serial.FindProperty("environments");
            list.arraySize = definitions.Length;
            for (int i = 0; i < definitions.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<CampusEnvironmentDefinition>($"{Root}/Data/Ambiente_{i + 1:00}.asset");
            serial.FindProperty("environmentIndex").intValue = index;
            serial.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLighting(int index)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.74f, 0.77f, 0.76f);
            RenderSettings.fog = false;
            var sun = Object.FindAnyObjectByType<Light>();
            if (sun != null)
            {
                sun.color = index == 4 ? new Color(1, 0.82f, 0.62f) : new Color(1, 0.95f, 0.84f);
                sun.intensity = 1.4f;
                sun.transform.rotation = Quaternion.Euler(48, -32, 0);
            }
        }

        private static void AddCommonRoute(CampusGeometry g, int index)
        {
            // Las flechas y las tres plataformas son una ruta de prueba, no dificultad final equilibrada.
            for (int z = -19; z <= 21; z += 5)
            {
                var arrow = g.Group("Flecha de recorrido", new Vector3(0, 0.02f, z));
                var a = g.Box("Trazo", new Vector3(-0.2f, 0, 0), new Vector3(0.10f, 0.018f, 0.7f), "ambar", false, arrow);
                var b = g.Box("Trazo", new Vector3(0.2f, 0, 0), new Vector3(0.10f, 0.018f, 0.7f), "ambar", false, arrow);
                a.transform.localRotation = Quaternion.Euler(0, -40, 0);
                b.transform.localRotation = Quaternion.Euler(0, 40, 0);
            }
            for (int i = 0; i < 3; i++)
                g.Step("Plataforma de ruta secundaria", new Vector3(-3, 0, -10 + i * 3), new Vector3(2.4f, 0.55f + i * 0.55f, 2));
            for (int i = 0; i < 24; i++)
            {
                var item = new GameObject("ECTS · " + (i + 1)).transform;
                item.SetParent(g.Root, false);
                item.localPosition = new Vector3(Mathf.Sin(i * 0.45f) * 1.8f, 1, -19 + i * 1.72f);
                var trigger = item.gameObject.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.45f;
                item.gameObject.AddComponent<CreditCollectible>();
                var model = g.Cylinder("Crédito", Vector3.zero, new Vector3(0.4f, 0.08f, 0.4f), "ambar", false, item);
                model.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            Vector3[] honors = { new Vector3(-3, 2.65f, -4), new Vector3(8, 1.5f, 7), new Vector3(0, 1.4f, 22) };
            for (int i = 0; i < honors.Length; i++)
            {
                var item = new GameObject("Matrícula de Honor · " + (i + 1)).transform;
                item.SetParent(g.Root, false);
                item.localPosition = honors[i];
                var trigger = item.gameObject.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.55f;
                var component = item.gameObject.AddComponent<HonorCollectible>();
                var serial = new SerializedObject(component);
                serial.FindProperty("index").intValue = i;
                serial.ApplyModifiedPropertiesWithoutUndo();
                var model = g.Box("Diploma", Vector3.zero, new Vector3(0.65f, 0.75f, 0.12f), "turquesa", false, item);
                model.transform.localRotation = Quaternion.Euler(0, 0, 12);
                g.Box("Sello", new Vector3(0, -0.1f, -0.08f), new Vector3(0.2f, 0.2f, 0.06f), "ambar", false, item);
            }
            var checkpoint = g.Group("Checkpoint", new Vector3(0, 0, 1));
            var checkpointTrigger = checkpoint.gameObject.AddComponent<BoxCollider>();
            checkpointTrigger.isTrigger = true;
            checkpointTrigger.center = Vector3.up;
            checkpointTrigger.size = new Vector3(4, 2, 2);
            var disk = g.Cylinder("Sello de checkpoint", new Vector3(0, 0.04f, 0), new Vector3(2.8f, 0.035f, 2.8f), "turquesa", false, checkpoint);
            var cp = checkpoint.gameObject.AddComponent<Checkpoint>();
            EditorAssets.SetField(cp, "indicator", disk.GetComponent<Renderer>());

            var goal = g.Group("Meta · siguiente ambiente", new Vector3(0, 0, 26));
            for (int side = -1; side <= 1; side += 2)
                g.Box("Jamba de meta", new Vector3(side * 2.2f, 1.7f, 0), new Vector3(0.35f, 3.4f, 0.5f), "turquesa", true, goal);
            g.Sign(index == 9 ? "CUM LAUDE" : "SIGUIENTE ZONA", new Vector3(0, 3.5f, 0), 5.3f, 0.65f, "oscuro", goal);
            var triggerGoal = goal.gameObject.AddComponent<BoxCollider>();
            triggerGoal.isTrigger = true;
            triggerGoal.center = new Vector3(0, 1.3f, 0);
            triggerGoal.size = new Vector3(4, 2.6f, 1.5f);
            goal.gameObject.AddComponent<LevelGoal>();
            var future = g.Group("[Reserva IA · no implementada en este ambiente]", new Vector3(0, 0, 14));
            future.gameObject.tag = "Untagged";
        }

        private static void Entrance(CampusGeometry g, CampusPropFactory p)
        {
            var facade = g.Group("Fachada inspirada en la foto", new Vector3(0, 0, -14));
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box("Macizo de fachada", new Vector3(side * 10.6f, 3.6f, 0), new Vector3(6.4f, 7.2f, 1.1f), "piedra", true, facade);
                g.Box("Pilastra", new Vector3(side * 5.5f, 2.9f, -0.2f), new Vector3(0.9f, 5.8f, 1.5f), "piedra", true, facade);
                for (int y = 1; y <= 6; y++)
                    g.Box("Despiece de piedra", new Vector3(side * 10.6f, y, -0.57f), new Vector3(6.4f, 0.025f, 0.04f), "junta", false, facade);
            }
            g.Box("Dintel de acceso", new Vector3(0, 5.3f, 0), new Vector3(11.8f, 0.65f, 1.3f), "piedra", true, facade);
            g.Sign("I O D P", new Vector3(0, 4.65f, -0.72f), 7.5f, 0.8f, "oscuro", facade);
            for (int w = -1; w <= 1; w++)
            {
                g.Box("Ventana superior", new Vector3(w * 2.7f, 6.65f, 0.1f), new Vector3(1.6f, 1.8f, 0.15f), "azul", false, facade);
                for (int a = 0; a < 7; a++)
                {
                    float angle = a * Mathf.PI / 6;
                    var stone = g.Box("Dovela rojiza", new Vector3(w * 2.7f + Mathf.Cos(angle) * 1.05f, 7.3f + Mathf.Sin(angle) * 0.9f, -0.12f), new Vector3(0.5f, 0.33f, 0.35f), a % 2 == 0 ? "terracota" : "crema", false, facade);
                    stone.transform.localRotation = Quaternion.Euler(0, 0, a * 30);
                }
            }
            g.Box("Felpudo exterior", new Vector3(0, 0.015f, -19), new Vector3(7, 0.025f, 3), "oscuro", false);
            p.Place(CampusProp.Reception, new Vector3(-8, 0, -1), 0, g.Root);
            p.Place(CampusProp.Noticeboard, new Vector3(10, 0, -9), 0, g.Root);
            p.Place(CampusProp.Bench, new Vector3(9, 0, 3), -90, g.Root);
            p.Place(CampusProp.Plant, new Vector3(-11, 0, -8), 0, g.Root);
            p.Place(CampusProp.Plant, new Vector3(11, 0, 14), 0, g.Root);
            for (int i = 0; i < 3; i++) p.Place(CampusProp.Locker, new Vector3(-11.6f, 0, 10 + i * 1.1f), -90, g.Root);
            for (int x = -6; x <= 6; x += 6)
            {
                g.Box("Torno abierto · carcasa", new Vector3(x + 2, 0.65f, 7), new Vector3(1, 1.3f, 1.3f), "metal");
                g.Box("Indicador de acceso", new Vector3(x + 2, 1.34f, 7), new Vector3(0.7f, 0.08f, 0.5f), "turquesa", false);
            }
            g.Stairs(new Vector3(8, 0, 14));
            g.Box("Descansillo de entrada", new Vector3(8, 0.75f, 19), new Vector3(5.6f, 1.5f, 5), "piedra");
        }

        private static void Classroom(CampusGeometry g, CampusPropFactory p)
        {
            for (int row = 0; row < 5; row++)
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 5.5f;
                    float z = -15 + row * 6;
                    p.Place(CampusProp.Desk, new Vector3(x, 0, z), 0, g.Root);
                    p.Place(CampusProp.Desk, new Vector3(x + side * 3.1f, 0, z), 0, g.Root);
                    p.Place(CampusProp.Chair, new Vector3(x, 0, z - 1.1f), 0, g.Root);
                    p.Place(CampusProp.Chair, new Vector3(x + side * 3.1f, 0, z - 1.1f), 0, g.Root);
                }
            g.Box("Pizarra blanca", new Vector3(0, 2.7f, 28.9f), new Vector3(11, 2.6f, 0.15f), "blanco", false);
            g.Text("PARADIGMAS / IODP", new Vector3(0, 2.8f, 28.8f), 0.38f, "verde");
            g.Box("Tarima", new Vector3(0, 0.3f, 20), new Vector3(16, 0.6f, 6), "madera");
            p.Place(CampusProp.Desk, new Vector3(5, 0.6f, 21), 180, g.Root);
            p.Place(CampusProp.Lectern, new Vector3(-5, 0.6f, 21), 0, g.Root);
            p.Place(CampusProp.Radiator, new Vector3(12.4f, 0, 5), -90, g.Root);
        }

        private static void Bathrooms(CampusGeometry g, CampusPropFactory p)
        {
            for (int z = -14; z <= 16; z += 10)
            {
                p.Place(CampusProp.Sink, new Vector3(-11, 0, z), -90, g.Root);
                var cabins = g.Group("Cabinas", new Vector3(10, 0, z));
                for (int side = -1; side <= 1; side += 2)
                    g.Box("Panel de cabina", new Vector3(side * 1.8f, 1.3f, 0), new Vector3(0.13f, 2.6f, 3), "madera", true, cabins);
                g.Box("Puerta entreabierta", new Vector3(-0.7f, 1.3f, -1.45f), new Vector3(1.7f, 2.6f, 0.14f), "madera", true, cabins).transform.localRotation = Quaternion.Euler(0, 35, 0);
                p.Place(CampusProp.Toilet, new Vector3(10, 0, z + 0.5f), 0, g.Root);
            }
            for (int i = 0; i < 4; i++)
                g.Box("Charco decorativo · sin deslizamiento", new Vector3(i % 2 == 0 ? 3 : -3, 0.012f, -12 + i * 8), new Vector3(3, 0.018f, 2), "pantalla", false);
            g.Sign("BAÑOS / PISO MOJADO", new Vector3(0, 3.3f, -15), 8, 0.8f);
            p.Place(CampusProp.Noticeboard, new Vector3(8, 0, 23), 0, g.Root);
        }

        private static void Cafeteria(CampusGeometry g, CampusPropFactory p)
        {
            for (int row = 0; row < 4; row++)
                for (int side = -1; side <= 1; side += 2)
                {
                    var pos = new Vector3(side * 6.5f, 0, -14 + row * 8);
                    p.Place(CampusProp.CafeTable, pos, 0, g.Root);
                    p.Place(CampusProp.Chair, pos + new Vector3(-0.8f, 0, -1.15f), 0, g.Root);
                    p.Place(CampusProp.Chair, pos + new Vector3(0.8f, 0, 1.15f), 180, g.Root);
                }
            for (int side = -1; side <= 1; side += 2)
            {
                g.Box("Pilar de cafetería", new Vector3(side * 4, 2.5f, 3), new Vector3(0.75f, 5, 0.75f), "crema");
                g.Box("Franja de madera de pilar", new Vector3(side * 4, 1.2f, 3), new Vector3(0.78f, 0.7f, 0.78f), "madera", false);
                p.Place(CampusProp.Counter, new Vector3(side * 6, 0, 22), 0, g.Root);
            }
            g.Sign("CAFETERÍA / NOS VEMOS EN EL RECREO", new Vector3(0, 4, 18), 13, 0.9f);
        }

        private static void Chapel(CampusGeometry g, CampusPropFactory p)
        {
            for (int row = 0; row < 5; row++)
                for (int side = -1; side <= 1; side += 2)
                    p.Place(CampusProp.Bench, new Vector3(side * 8.8f, 0, -16 + row * 4), 0, g.Root);
            g.Cylinder("Arena del minijefe · combate pendiente", new Vector3(0, 0.025f, 11), new Vector3(15, 0.02f, 15), "terracota");
            g.Box("Estrado de revisión", new Vector3(0, 0.45f, 23), new Vector3(17, 0.9f, 8), "madera");
            p.Place(CampusProp.Lectern, new Vector3(-5, 0.9f, 23), 0, g.Root);
            g.Stairs(new Vector3(6, 0, 17), 3, 4);
            for (int side = -1; side <= 1; side += 2)
                for (int z = -12; z <= 24; z += 9)
                {
                    g.Box("Ventana alta de capilla", new Vector3(side * 13.7f, 3.6f, z), new Vector3(0.08f, 3, 2.4f), z % 2 == 0 ? "ambar" : "rosa", false);
                    g.Box("Lama de madera", new Vector3(side * 13.5f, 3.6f, z), new Vector3(0.12f, 3.4f, 0.13f), "madera", false);
                }
            g.Sign("LA REVISIÓN", new Vector3(0, 3.3f, 28.8f), 10, 0.9f);
        }

        private static void Library(CampusGeometry g, CampusPropFactory p)
        {
            for (int row = 0; row < 4; row++)
                for (int side = -1; side <= 1; side += 2)
                {
                    p.Place(CampusProp.Shelf, new Vector3(side * 7.5f, 0, -16 + row * 9), side == 1 ? 90 : -90, g.Root);
                    p.Place(CampusProp.Shelf, new Vector3(side * 11.7f, 0, -16 + row * 9), side == 1 ? 90 : -90, g.Root);
                }
            for (int x = -7; x <= 7; x += 7)
            {
                p.Place(CampusProp.Desk, new Vector3(x, 0, 19), 0, g.Root);
                p.Place(CampusProp.Chair, new Vector3(x, 0, 17.8f), 0, g.Root);
            }
            g.Stairs(new Vector3(10, 0, 16), 3, 8);
            g.Box("Altillo de lectura", new Vector3(9, 1.87f, 24), new Vector3(7, 0.26f, 7), "madera");
            g.Sign("BIBLIOTECA / SILENCIO", new Vector3(0, 3.8f, -16), 10, 0.9f);
            p.Place(CampusProp.Plant, new Vector3(-11, 0, 24), 0, g.Root);
        }

        private static void Laboratory(CampusGeometry g, CampusPropFactory p)
        {
            for (int row = 0; row < 4; row++)
                for (int side = -1; side <= 1; side += 2)
                {
                    var pos = new Vector3(side * 7.2f, 0, -14 + row * 10);
                    p.Place(CampusProp.Engine, pos, side == 1 ? -30 : 30, g.Root);
                    g.Box("Perímetro de seguridad", pos + new Vector3(0, 0.015f, 1.6f), new Vector3(4.6f, 0.02f, 0.12f), "ambar", false);
                    g.Box("Perímetro de seguridad", pos + new Vector3(0, 0.015f, -1.6f), new Vector3(4.6f, 0.02f, 0.12f), "ambar", false);
                    // Vitrina sin vidrio transparente: marco abierto estilizado, sin ocultar los motores.
                    for (int s = -1; s <= 1; s += 2)
                        g.Box("Montante de vitrina", pos + new Vector3(side * 4.1f, 1.6f, s * 1.3f), new Vector3(0.12f, 3.2f, 0.12f), "metal", false);
                }
            p.Place(CampusProp.Sink, new Vector3(-8, 0, 24), 0, g.Root);
            g.Sign("LAB / MOTORES Y MÁQUINAS", new Vector3(0, 3.8f, -17), 11, 0.85f);
            g.Box("Canal técnico", new Vector3(4.1f, 0.015f, 2), new Vector3(0.5f, 0.02f, 45), "oscuro", false);
        }

        private static void Computers(CampusGeometry g, CampusPropFactory p)
        {
            for (int row = 0; row < 5; row++)
                for (int side = -1; side <= 1; side += 2)
                {
                    float z = -16 + row * 7;
                    p.Place(CampusProp.Computer, new Vector3(side * 5.7f, 0, z), 0, g.Root);
                    p.Place(CampusProp.Computer, new Vector3(side * 9.2f, 0, z), 0, g.Root);
                    p.Place(CampusProp.Chair, new Vector3(side * 5.7f, 0, z - 1.2f), 0, g.Root);
                }
            for (int i = 0; i < 5; i++)
            {
                g.Box("Rack servidor", new Vector3(-10 + i * 5, 1.5f, 24), new Vector3(1.5f, 3, 1.3f), "oscuro");
                for (int unit = 0; unit < 5; unit++)
                    g.Box("Unidad y piloto", new Vector3(-10 + i * 5, 0.4f + unit * 0.5f, 23.33f), new Vector3(1.2f, 0.22f, 0.05f), unit % 2 == 0 ? "azul" : "pantalla", false);
            }
            g.Sign("INFORMÁTICA / IODP", new Vector3(0, 4, -17), 10, 0.9f);
        }

        private static void Offices(CampusGeometry g, CampusPropFactory p)
        {
            for (int row = 0; row < 5; row++)
                for (int side = -1; side <= 1; side += 2)
                {
                    float z = -17 + row * 9;
                    g.Box("Tabique de despacho", new Vector3(side * 8.3f, 1.7f, z), new Vector3(0.2f, 3.4f, 5.5f), "crema");
                    g.Box("Puerta de despacho", new Vector3(side * 8.15f, 1.35f, z - 0.6f), new Vector3(0.16f, 2.7f, 1.7f), "madera");
                    g.Box("Ventana sobre zócalo", new Vector3(side * 8.1f, 2.65f, z + 1.7f), new Vector3(0.07f, 0.9f, 1.5f), "azul", false);
                    p.Place(CampusProp.Desk, new Vector3(side * 11, 0, z), side == 1 ? 90 : -90, g.Root);
                    p.Place(CampusProp.Plant, new Vector3(side * 6.8f, 0, z + 3), 0, g.Root);
                }
            p.Place(CampusProp.Bench, new Vector3(5, 0, 9), 90, g.Root);
            g.Sign("DESPACHOS / TUTORÍAS", new Vector3(0, 3.6f, -17), 10, 0.9f);
        }

        private static void Tribunal(CampusGeometry g, CampusPropFactory p)
        {
            for (int row = 0; row < 5; row++)
                for (int side = -1; side <= 1; side += 2)
                {
                    float height = row * 0.25f;
                    float x = side * (7.4f + row * 0.85f);
                    g.Box("Grada lateral", new Vector3(x, height * 0.5f, -8 + row * 6), new Vector3(3.6f, Mathf.Max(0.1f, height), 4), "piedra");
                    for (int i = 0; i < 3; i++) p.Place(CampusProp.Chair, new Vector3(x - 1 + i, height, -8 + row * 6), 0, g.Root);
                }
            g.Cylinder("Arena del Tribunal · IA pendiente", new Vector3(0, 0.035f, 6), new Vector3(15, 0.03f, 15), "azul");
            g.Box("Tarima del Tribunal", new Vector3(0, 0.5f, 23), new Vector3(18, 1, 9), "madera");
            g.Stairs(new Vector3(0, 0, 17.8f), 4.2f, 4);
            p.Place(CampusProp.Reception, new Vector3(-5.5f, 1, 24), 0, g.Root);
            p.Place(CampusProp.Reception, new Vector3(5.5f, 1, 24), 0, g.Root);
            g.Box("Pantalla de aula magna", new Vector3(0, 4.7f, 28.9f), new Vector3(12, 2.8f, 0.1f), "oscuro", false);
            g.Text("EL TRIBUNAL\nIODP", new Vector3(0, 4.6f, 28.8f), 0.47f, "ambar");
            g.Sign("AULA GRANDE / PROPUESTA PROVISIONAL", new Vector3(0, 3.6f, -17), 13, 0.85f);
        }

        [MenuItem("Tools/IODP/Exportar 10 vistas de ambiente", priority = 11)]
        public static void ExportPreviews()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de exportar.");
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Guardar la escena actual antes de exportar.");
            string initial = SceneManager.GetActiveScene().path;
            string previewFolder = Root + "/Previews";
            EditorAssets.EnsureFolder(previewFolder);
            // Inicializar URP antes de consultar SupportsRenderRequest en un proceso batch nuevo.
            EditorSceneManager.OpenScene(MenuPath);
            var warmCamera = Camera.main;
            var warmTarget = RenderTexture.GetTemporary(128, 80, 24);
            var warmPrevious = RenderTexture.active;
            try
            {
                warmCamera.targetTexture = warmTarget;
                warmCamera.Render();
            }
            finally
            {
                warmCamera.targetTexture = null;
                RenderTexture.active = warmPrevious;
                RenderTexture.ReleaseTemporary(warmTarget);
            }
            for (int i = 0; i < 10; i++)
            {
                string path = $"{SceneFolder}/IODP_{i + 1:00}_{Slugs[i]}.unity";
                EditorSceneManager.OpenScene(path);
                var camera = Camera.main;
                if (camera == null) throw new InvalidOperationException("Escena sin cámara: " + path);
                // Encuadres desde el interior para revisar escala, materiales y fondos.
                Vector3[] eyes = {
                    new(6.5f, 3.9f, -26.4f), new(8.3f, 2.38f, -18.8f), new(-5.8f, 2.2f, -17.6f), new(3.4f, 2.42f, 14.6f),
                    new(5, 4.4f, -6), new(-3.2f, 2.7f, -17.5f), new(9, 2.65f, -18),
                    new(-6.8f, 2.24f, -19.8f), new(3.1f, 2.9f, -23), new(8, 5.2f, -16)
                };
                Vector3[] targets = {
                    new(0, 4.9f, -8), new(2.6f, 1.55f, -1.5f), new(-10.7f, 1.47f, -8.2f), new(8.3f, 1.62f, 23.9f),
                    new(0, 4.3f, 24), new(7.8f, 2.8f, -5), new(-3, 2.0f, -4),
                    new(2.4f, 1.6f, -7), new(-1.2f, 1.6f, 3), new(-1.4f, 3, 21)
                };
                camera.transform.position = eyes[i];
                camera.transform.LookAt(targets[i]);
                camera.orthographic = false;
                camera.fieldOfView = 55;
                var texture = new RenderTexture(1280, 800, 24);
                texture.Create();
                var previous = RenderTexture.active;
                bool asyncShaders = ShaderUtil.allowAsyncCompilation;
                try
                {
                    ShaderUtil.allowAsyncCompilation = false;
                    var request = new RenderPipeline.StandardRequest { destination = texture };
                    if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
                    else { camera.targetTexture = texture; camera.Render(); }
                    RenderTexture.active = texture;
                    var capture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
                    capture.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0);
                    capture.Apply();
                    File.WriteAllBytes($"{previewFolder}/{i + 1:00}_{Slugs[i]}.png", capture.EncodeToPNG());
                    Object.DestroyImmediate(capture);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = asyncShaders;
                    camera.targetTexture = null;
                    RenderTexture.active = previous;
                    texture.Release();
                    Object.DestroyImmediate(texture);
                    // Reabrir evita guardar la cámara de ilustración encima de la cámara jugable.
                    EditorSceneManager.OpenScene(path);
                }
            }
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(string.IsNullOrEmpty(initial) ? MenuPath : initial);
            Debug.Log("[IODP] Diez vistas exportadas a " + previewFolder);
        }
    }
}
