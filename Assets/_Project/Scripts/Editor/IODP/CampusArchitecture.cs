using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MatriculaDeHonor.IODP.Editor
{
    // Las mallas, materiales y prefabs se entregan horneados. Esta clase solo monta el decorado.
    public static class CampusArchitecture
    {
        public const string Folder = "Assets/_Project/IODP/Prefabs/Arquitectura";
        static readonly string[] Buildings = {
            "Portico", "Seminarios", "Vestuarios", "Cantina", "Capilla",
            "Hemeroteca", "Nave", "Calculo", "Claustro", "Paraninfo"
        };
        static readonly string[] FormerShell = {
            "Junta de baldosa", "Remate de madera", "Pared crema", "Pilar crema", "Base de pilar",
            "Ventana azul", "Testero crema", "Zócalo de testero", "Viga abierta", "Panel de luz", "Cartel",
            "Rótulo ·", "Flecha de recorrido", "Fachada inspirada", "Ventana alta de capilla", "Lama de madera",
            "Pizarra blanca", "Grada lateral", "Pantalla de aula magna", "Charco decorativo"
        };

        public static void FinishDetails(Transform root, int index)
        {
            string art = "Assets/_Project/IODP/Art/Arquitectura/";
            int zone = Mathf.Max(0, index);
            var finish = AssetDatabase.LoadAssetAtPath<Material>(art + new[] {
                "stone", "sage", "tile", "oak", "stone", "oak", "steel", "iron", "oak", "cloth"
            }[zone] + ".mat");
            var brass = AssetDatabase.LoadAssetAtPath<Material>(art + "bronze.mat");
            foreach (var t in root.Cast<Transform>())
            {
                if (t.name == "Plataforma de ruta secundaria") t.GetComponent<Renderer>().sharedMaterial = finish;
                if (t.name == "Borde de salto") t.GetComponent<Renderer>().sharedMaterial = brass;
                var source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                if (!source) continue;
                var p = t.localPosition;
                if (zone == 1 && (source.name == "IODP_Chair" || source.name == "IODP_Desk") && p.z < 13)
                {
                    // Agrupar mesas y sillas por parejas en torno al pasillo del seminario.
                    var pivot = new Vector3(Mathf.Sign(p.x) * 7.05f, 0, -15 + Mathf.Round((p.z + 15) / 6) * 6);
                    var tilt = Quaternion.Euler(0, Mathf.Sign(p.x) * (p.z > -4 ? 19 : -13), 0);
                    t.localPosition = pivot + tilt * (p - pivot);
                    t.localRotation = tilt * t.localRotation;
                }
                if (zone == 7 && (source.name == "IODP_Computer" || source.name == "IODP_Chair") && p.x > 0)
                {
                    var pivot = new Vector3(7.45f, 0, -16 + Mathf.Round((p.z + 16) / 7) * 7);
                    var rotate = Quaternion.Euler(0, -63, 0);
                    t.localPosition = pivot + rotate * (p - pivot);
                    t.localRotation = rotate * t.localRotation;
                }
            }
            foreach (var label in root.GetComponentsInChildren<TextMesh>(true))
            {
                // Dimensionar los rÃ³tulos antiguos en metros sobre su soporte.
                label.characterSize *= .24f;
            }
            var goal = root.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Meta Â·"));
            if (goal != null)
                foreach (var jamb in goal.Cast<Transform>().Where(t => t.name == "Jamba de meta"))
                    jamb.GetComponent<Renderer>().sharedMaterial = brass;
            if (index < 0) return;
            var lights = new GameObject("IluminaciÃ³n local").transform;
            lights.SetParent(root, false);
            Color tint = CampusGeometry.Hex(zone == 4 || zone == 9 ? "FFD29B" : zone == 7 ? "BCE9FF" : "FFF0DB");
            foreach (int z in new[] { -13, 11 })
            {
                var mount = new GameObject("Luz de sala").transform;
                mount.SetParent(lights, false);
                mount.localPosition = new Vector3(z < 0 ? -5 : 5, zone == 4 || zone == 6 ? 5.7f : 3.7f, z);
                var light = mount.gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = tint;
                light.intensity = 1.6f;
                light.range = 10;
                light.shadows = LightShadows.None;
            }
        }


        public static void Dress(Transform root, int index, Camera camera)
        {
            int zone = Mathf.Max(index, 0);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/{Buildings[zone]}.prefab");
            if (prefab == null) throw new InvalidOperationException("Falta el decorado " + Buildings[zone]);
            if (root.Find(prefab.name)) return;
            var original = root.Cast<Transform>().ToArray();
            foreach (var t in original)
            {
                if (FormerShell.Any(p => t.name.StartsWith(p, StringComparison.Ordinal)) ||
                    (zone == 9 && PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)?.name == "IODP_Chair"))
                    Object.DestroyImmediate(t.gameObject);
                else if (t.name.StartsWith("Zócalo verde") || t.name.StartsWith("Suelo continuo"))
                    t.GetComponent<Renderer>().enabled = false; // conservar los límites físicos del prototipo
            }
            var building = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            building.name = prefab.name;
            var sun = root.gameObject.scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Light>()).First();
            bool warm = zone == 4 || zone == 5 || zone == 9;
            bool technical = zone == 6 || zone == 7;
            sun.color = CampusGeometry.Hex(warm ? "FFDDB2" : technical ? "DCEBF5" : "FFF1DC");
            sun.intensity = warm ? 1.42f : 1.65f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .66f;
            sun.transform.rotation = Quaternion.Euler(zone == 0 ? 40 : 53, -32 + zone * 7, 0);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = CampusGeometry.Hex(technical ? "CDDDE8" : "E7E6D9");
            RenderSettings.ambientEquatorColor = CampusGeometry.Hex(warm ? "BCA58E" : "AAADA5");
            RenderSettings.ambientGroundColor = CampusGeometry.Hex("817970");
            FinishDetails(root, index);
            if (!camera) return;
            camera.backgroundColor = CampusGeometry.Hex(technical ? "A9BDC7" : warm ? "CABBA6" : "CBDAD8");
            if (index < 0)
            {
                camera.fieldOfView = 49;
                camera.transform.position = new Vector3(8, 6.2f, -29);
                camera.transform.LookAt(new Vector3(-4.2f, 3.45f, -5.3f));
            }
        }
    }
}
