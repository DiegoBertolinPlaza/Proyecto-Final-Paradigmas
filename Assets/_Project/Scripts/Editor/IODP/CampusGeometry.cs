using System.Collections.Generic;
using MatriculaDeHonor.Editor;
using UnityEditor;
using UnityEngine;

namespace MatriculaDeHonor.IODP.Editor
{
    /// <summary>Builder de arquitectura y paleta compartida. Geometría en metros, Y arriba, avance +Z.</summary>
    public sealed class CampusGeometry
    {
        public const string RootPath = "Assets/_Project/IODP";
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public Transform Root { get; }

        public CampusGeometry(Transform root) => Root = root;

        public static Material Mat(string key)
        {
            if (Materials.TryGetValue(key, out var found) && found != null) return found;
            Color color = key switch
            {
                "verde" => Hex("667D65"), "crema" => Hex("EEE6D6"), "piedra" => Hex("A9A69E"),
                "terracota" => Hex("9F6550"), "suelo" => Hex("D6D0BD"), "junta" => Hex("ABA68E"),
                "madera" => Hex("B1814F"), "oscuro" => Hex("243C42"), "azul" => Hex("426B83"),
                "ambar" => Hex("E9AE49"), "turquesa" => Hex("4FA99E"), "metal" => Hex("69777B"),
                "papel" => Hex("FAF2D9"), "rosa" => Hex("B5747D"), "hojas" => Hex("4D6B4B"),
                "pantalla" => Hex("65BED0"), "blanco" => Hex("F1EEE3"), "rojo" => Hex("B95D4B"),
                _ => Hex("C9B891")
            };
            var material = EditorAssets.LoadOrCreateMaterial($"{RootPath}/Art/Materials/IODP_{key}.mat", color);
            if (material == null) throw new System.InvalidOperationException("Falta el shader URP Lit.");
            material.SetFloat("_Smoothness", key == "metal" ? 0.38f : 0.12f);
            if (key == "pantalla")
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.5f);
            }
            Materials[key] = material;
            return material;
        }

        public GameObject Box(string name, Vector3 position, Vector3 size, string material,
            bool collision = true, Transform parent = null) =>
            Shape(PrimitiveType.Cube, name, position, size, material, collision, parent);

        public GameObject Cylinder(string name, Vector3 position, Vector3 size, string material,
            bool collision = false, Transform parent = null) =>
            Shape(PrimitiveType.Cylinder, name, position, size, material, collision, parent);

        public GameObject Shape(PrimitiveType type, string name, Vector3 position, Vector3 size,
            string material, bool collision = false, Transform parent = null)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent != null ? parent : Root, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = Mat(material);
            if (!collision) Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }

        public Transform Group(string name, Vector3 position, float angle = 0f, Transform parent = null)
        {
            var obj = new GameObject(name).transform;
            obj.SetParent(parent != null ? parent : Root, false);
            obj.localPosition = position;
            obj.localRotation = Quaternion.Euler(0, angle, 0);
            return obj;
        }

        public void Text(string text, Vector3 position, float size = 0.35f, string material = "crema",
            Transform parent = null, float angle = 0f)
        {
            var label = new GameObject("Rótulo · " + text).AddComponent<TextMesh>();
            label.transform.SetParent(parent != null ? parent : Root, false);
            label.transform.localPosition = position;
            label.transform.localRotation = Quaternion.Euler(0, angle, 0);
            label.text = text;
            label.fontSize = 64;
            label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Mat(material).color;
        }

        public void Sign(string text, Vector3 position, float width = 5f, float height = 1f,
            string material = "oscuro", Transform parent = null)
        {
            Box("Cartel", position, new Vector3(width, height, 0.12f), material, false, parent);
            Text(text, position + Vector3.back * 0.071f, Mathf.Min(0.29f, width / Mathf.Max(1, text.Length) * 0.8f), "crema", parent);
        }

        public void Room(string title, string floor = "suelo", bool chapel = false)
        {
            Box("Suelo continuo · recorrido seguro", new Vector3(0, -0.3f, 2), new Vector3(28, 0.6f, 56), floor);
            // Junta dibujada, no miles de baldosas ni colliders diminutos.
            for (int x = -12; x <= 12; x += 3)
                Box("Junta de baldosa", new Vector3(x, 0.005f, 2), new Vector3(0.018f, 0.009f, 56), "junta", false);
            for (int z = -25; z <= 29; z += 3)
                Box("Junta de baldosa", new Vector3(0, 0.006f, z), new Vector3(28, 0.009f, 0.018f), "junta", false);

            for (int side = -1; side <= 1; side += 2)
            {
                Box("Zócalo verde · lateral", new Vector3(side * 14, 0.72f, 2), new Vector3(0.35f, 1.44f, 56), "verde");
                Box("Remate de madera", new Vector3(side * 13.98f, 1.49f, 2), new Vector3(0.4f, 0.10f, 56), "madera", false);
                // Laterales superiores abiertos delante: corte de maqueta para no tapar al jugador.
                Box("Pared crema · fondo", new Vector3(side * 14, 3.5f, 22), new Vector3(0.35f, 4f, 16), "crema");
                for (int z = -18; z <= 24; z += 10)
                {
                    Box("Pilar crema", new Vector3(side * 13.3f, 2.7f, z), new Vector3(0.7f, 5.4f, 0.7f), "crema");
                    Box("Base de pilar", new Vector3(side * 13.3f, 0.3f, z), new Vector3(0.9f, 0.6f, 0.9f), "piedra");
                    Box("Ventana azul", new Vector3(side * 13.77f, 3.3f, z), new Vector3(0.08f, 2.2f, 3.5f), "azul", false);
                }
            }
            Box("Testero crema", new Vector3(0, 2.8f, 30), new Vector3(28, 5.6f, 0.4f), "crema");
            Box("Zócalo de testero", new Vector3(0, 0.72f, 29.76f), new Vector3(28, 1.44f, 0.12f), "verde", false);
            Sign($"IODP / {title.ToUpperInvariant()}", new Vector3(0, 4.1f, 29.5f), 11, 1.05f);
            for (int z = -14; z <= 24; z += 10)
            {
                Box("Viga abierta · sin techo", new Vector3(0, chapel ? 6.6f : 5.6f, z), new Vector3(27, 0.18f, 0.24f), chapel ? "madera" : "crema", false);
                Box("Panel de luz", new Vector3(0, 5.42f, z), new Vector3(2, 0.08f, 1.1f), "papel", false);
            }
        }

        public void Step(string name, Vector3 feet, Vector3 size)
        {
            Box(name, feet + Vector3.up * size.y * 0.5f, size, "ambar");
            Box("Borde de salto", feet + Vector3.up * (size.y + 0.016f), new Vector3(size.x, 0.025f, 0.12f), "papel", false);
        }

        public void Stairs(Vector3 start, float width = 3.6f, int steps = 6, Transform parent = null)
        {
            for (int i = 0; i < steps; i++)
            {
                float height = (i + 1) * 0.25f;
                Box("Peldaño", start + new Vector3(0, height * 0.5f, i * 0.65f), new Vector3(width, height, 0.65f), "piedra", true, parent);
                Box("Nariz de peldaño", start + new Vector3(0, height + 0.02f, i * 0.65f - 0.22f), new Vector3(width, 0.035f, 0.09f), "ambar", false, parent);
            }
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }
    }
}
