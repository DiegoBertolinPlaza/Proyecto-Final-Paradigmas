using MatriculaDeHonor.Editor;
using UnityEditor;
using UnityEngine;

namespace MatriculaDeHonor.IODP.Editor
{
    public enum CampusProp { Desk, Chair, Bench, Plant, Reception, Noticeboard, Shelf, Computer, Engine, Sink, Toilet, Locker, CafeTable, Counter, Lectern, Radiator }

    /// <summary>Factory de prefabs modulares. Cada escena reutiliza los mismos muebles y materiales.</summary>
    public sealed class CampusPropFactory
    {
        public const string Folder = CampusGeometry.RootPath + "/Prefabs";

        public GameObject Place(CampusProp kind, Vector3 position, float angle, Transform parent)
        {
            string path = $"{Folder}/IODP_{kind}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                EditorAssets.EnsureFolder(Folder);
                var temporary = new GameObject("IODP_" + kind);
                Build(kind, new CampusGeometry(temporary.transform));
                prefab = PrefabUtility.SaveAsPrefabAsset(temporary, path);
                Object.DestroyImmediate(temporary);
                if (prefab == null) throw new System.InvalidOperationException("No se ha creado " + path);
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0, angle, 0);
            return instance;
        }

        private static void Build(CampusProp kind, CampusGeometry g)
        {
            switch (kind)
            {
                case CampusProp.Desk:
                    g.Box("Tablero crema", new Vector3(0, 0.86f, 0), new Vector3(2.2f, 0.14f, 1.15f), "crema");
                    g.Box("Faldón verde", new Vector3(0, 0.48f, 0.37f), new Vector3(2.08f, 0.65f, 0.1f), "verde", false);
                    Legs(g, 0.94f, 0.44f, 0.8f);
                    g.Box("Cuaderno", new Vector3(-0.45f, 0.96f, 0), new Vector3(0.45f, 0.05f, 0.55f), "papel", false);
                    break;
                case CampusProp.Chair:
                    g.Box("Asiento", new Vector3(0, 0.48f, 0), new Vector3(0.62f, 0.12f, 0.62f), "crema", false);
                    g.Box("Respaldo", new Vector3(0, 0.85f, 0.27f), new Vector3(0.62f, 0.58f, 0.1f), "verde", false);
                    Legs(g, 0.24f, 0.24f, 0.43f);
                    break;
                case CampusProp.Bench:
                    g.Box("Banco · asiento", new Vector3(0, 0.55f, 0), new Vector3(3.2f, 0.18f, 0.85f), "madera");
                    g.Box("Banco · respaldo", new Vector3(0, 1.0f, 0.38f), new Vector3(3.2f, 0.65f, 0.14f), "madera", false);
                    Legs(g, 1.3f, 0.25f, 0.47f);
                    break;
                case CampusProp.Plant:
                    g.Cylinder("Maceta", new Vector3(0, 0.35f, 0), new Vector3(0.75f, 0.35f, 0.75f), "terracota", true);
                    g.Cylinder("Tallo", new Vector3(0, 1, 0), new Vector3(0.1f, 0.6f, 0.1f), "madera");
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * Mathf.PI * 0.4f;
                        var leaf = g.Shape(PrimitiveType.Sphere, "Hoja", new Vector3(Mathf.Cos(a) * 0.3f, 1.35f + i * 0.13f, Mathf.Sin(a) * 0.3f), new Vector3(0.7f, 0.33f, 0.42f), "hojas");
                        leaf.transform.localRotation = Quaternion.Euler(0, -i * 72, 25);
                    }
                    break;
                case CampusProp.Reception:
                    g.Box("Mostrador de seguridad", new Vector3(0, 0.7f, 0), new Vector3(6, 1.4f, 1.5f), "madera");
                    g.Box("Encimera de recepción", new Vector3(0, 1.46f, 0), new Vector3(6.3f, 0.16f, 1.7f), "crema");
                    g.Sign("INFORMACIÓN / IODP", new Vector3(0, 0.85f, -0.78f), 4.4f, 0.55f);
                    Monitor(g, new Vector3(-1.2f, 1.55f, 0.1f));
                    break;
                case CampusProp.Noticeboard:
                    g.Box("Marco del tablón", new Vector3(0, 1.8f, 0), new Vector3(2.9f, 2.1f, 0.15f), "madera", false);
                    g.Box("Corcho", new Vector3(0, 1.8f, -0.1f), new Vector3(2.7f, 1.9f, 0.05f), "terracota", false);
                    for (int i = 0; i < 5; i++)
                        g.Box("Aviso", new Vector3(-0.95f + (i % 3) * 0.85f, 1.45f + (i / 3) * 0.8f, -0.15f), new Vector3(0.54f, 0.64f, 0.02f), i % 2 == 0 ? "papel" : "ambar", false);
                    break;
                case CampusProp.Shelf:
                    g.Box("Librería · fondo", new Vector3(0, 1.7f, 0.38f), new Vector3(3, 3.4f, 0.16f), "madera");
                    for (int side = -1; side <= 1; side += 2)
                        g.Box("Librería · lateral", new Vector3(side * 1.45f, 1.7f, 0), new Vector3(0.13f, 3.4f, 0.85f), "madera");
                    for (int row = 0; row < 4; row++)
                    {
                        float h = 0.15f + row * 0.84f;
                        g.Box("Balda", new Vector3(0, h, 0), new Vector3(2.9f, 0.12f, 0.85f), "madera");
                        for (int book = 0; book < 10; book++)
                        {
                            string color = new[] { "verde", "rojo", "azul", "ambar", "crema" }[(book + row) % 5];
                            float bh = 0.48f + (book % 3) * 0.07f;
                            g.Box("Libro", new Vector3(-1.2f + book * 0.26f, h + bh * 0.5f + 0.07f, -0.05f), new Vector3(0.19f, bh, 0.45f), color, false);
                        }
                    }
                    break;
                case CampusProp.Computer:
                    g.Box("Mesa de informática", new Vector3(0, 0.85f, 0), new Vector3(2.3f, 0.14f, 1.35f), "madera");
                    Legs(g, 1, 0.5f, 0.8f);
                    Monitor(g, new Vector3(0, 0.94f, 0.25f));
                    g.Box("Teclado", new Vector3(0, 0.96f, -0.35f), new Vector3(0.85f, 0.07f, 0.28f), "oscuro", false);
                    g.Box("Torre", new Vector3(0.8f, 0.35f, 0), new Vector3(0.35f, 0.7f, 0.7f), "oscuro", false);
                    break;
                case CampusProp.Engine:
                    g.Box("Bancada amarilla", new Vector3(0, 0.16f, 0), new Vector3(2.6f, 0.32f, 1.7f), "ambar");
                    g.Box("Bloque motor", new Vector3(0, 0.85f, 0), new Vector3(1.4f, 1.1f, 0.9f), "metal");
                    for (int i = -1; i <= 1; i++)
                    {
                        g.Cylinder("Cilindro", new Vector3(i * 0.43f, 1.5f, 0), new Vector3(0.35f, 0.2f, 0.35f), "oscuro");
                        var gear = g.Cylinder("Volante motor", new Vector3(i * 0.43f, 0.9f, -0.56f), new Vector3(0.58f, 0.1f, 0.58f), "metal");
                        gear.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    }
                    g.Sign("MOTOR / BANCO DE PRUEBAS", new Vector3(0, 0.22f, -0.88f), 2.5f, 0.35f);
                    break;
                case CampusProp.Sink:
                    g.Box("Encimera", new Vector3(0, 1, 0), new Vector3(2.7f, 0.23f, 1.05f), "crema");
                    g.Box("Mueble", new Vector3(0, 0.46f, 0.2f), new Vector3(2.5f, 0.92f, 0.6f), "madera");
                    for (int i = -1; i <= 1; i++)
                    {
                        g.Shape(PrimitiveType.Sphere, "Lavabo", new Vector3(i * 0.83f, 1.11f, -0.05f), new Vector3(0.7f, 0.13f, 0.7f), "blanco");
                        g.Cylinder("Grifo", new Vector3(i * 0.83f, 1.3f, 0.3f), new Vector3(0.08f, 0.18f, 0.08f), "metal");
                    }
                    g.Box("Espejo estilizado", new Vector3(0, 2, 0.55f), new Vector3(2.7f, 1.4f, 0.08f), "azul", false);
                    break;
                case CampusProp.Toilet:
                    g.Box("Cisterna", new Vector3(0, 0.8f, 0.35f), new Vector3(0.65f, 0.9f, 0.3f), "blanco");
                    g.Shape(PrimitiveType.Sphere, "Taza", new Vector3(0, 0.45f, -0.1f), new Vector3(0.65f, 0.75f, 0.9f), "blanco", true);
                    break;
                case CampusProp.Locker:
                    g.Box("Taquilla", new Vector3(0, 1.1f, 0), new Vector3(0.9f, 2.2f, 0.7f), "verde");
                    g.Box("Puerta", new Vector3(0, 1.1f, -0.37f), new Vector3(0.83f, 2.08f, 0.06f), "azul", false);
                    g.Box("Tirador", new Vector3(0.26f, 1.1f, -0.43f), new Vector3(0.07f, 0.26f, 0.07f), "ambar", false);
                    for (int i = 0; i < 3; i++) g.Box("Ventilación", new Vector3(0, 1.83f - i * 0.1f, -0.41f), new Vector3(0.5f, 0.035f, 0.02f), "oscuro", false);
                    break;
                case CampusProp.CafeTable:
                    g.Box("Mesa de cafetería", new Vector3(0, 0.95f, 0), new Vector3(2.8f, 0.18f, 1.6f), "madera");
                    Legs(g, 1.1f, 0.55f, 0.86f);
                    g.Box("Bandeja", new Vector3(0.45f, 1.07f, 0.1f), new Vector3(0.6f, 0.055f, 0.4f), "ambar", false);
                    g.Cylinder("Vaso de café", new Vector3(0.45f, 1.21f, 0.1f), new Vector3(0.19f, 0.13f, 0.19f), "papel");
                    break;
                case CampusProp.Counter:
                    g.Box("Mostrador", new Vector3(0, 0.7f, 0), new Vector3(5.4f, 1.4f, 1.5f), "madera");
                    g.Box("Encimera", new Vector3(0, 1.45f, 0), new Vector3(5.6f, 0.14f, 1.7f), "metal");
                    g.Box("Cafetera", new Vector3(-1.2f, 1.88f, 0), new Vector3(1.2f, 0.75f, 0.8f), "oscuro");
                    g.Box("Panel de cafetera", new Vector3(-1.2f, 2, -0.43f), new Vector3(0.9f, 0.3f, 0.04f), "pantalla", false);
                    g.Sign("CAFÉ / MENÚ DEL DÍA", new Vector3(0, 0.8f, -0.78f), 4.3f, 0.65f);
                    break;
                case CampusProp.Lectern:
                    g.Box("Atril · base", new Vector3(0, 0.1f, 0), new Vector3(1.4f, 0.2f, 1.1f), "madera");
                    g.Box("Atril · pie", new Vector3(0, 0.65f, 0.1f), new Vector3(0.6f, 1.2f, 0.6f), "madera");
                    var top = g.Box("Atril · tablero", new Vector3(0, 1.35f, 0), new Vector3(1.5f, 0.14f, 1.1f), "madera");
                    top.transform.localRotation = Quaternion.Euler(15, 0, 0);
                    break;
                case CampusProp.Radiator:
                    for (int i = 0; i < 9; i++) g.Box("Elemento de radiador", new Vector3(-0.8f + i * 0.2f, 0.55f, 0), new Vector3(0.13f, 0.85f, 0.3f), "blanco", false);
                    break;
            }
        }

        private static void Legs(CampusGeometry g, float x, float z, float height)
        {
            // Patas decorativas: el tablero concentra la colisión para evitar enganchones.
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    g.Box("Pata", new Vector3(sx * x, height * 0.5f, sz * z), new Vector3(0.09f, height, 0.09f), "metal", false);
        }

        private static void Monitor(CampusGeometry g, Vector3 start)
        {
            g.Box("Base monitor", start, new Vector3(0.65f, 0.05f, 0.4f), "oscuro", false);
            g.Box("Pie monitor", start + Vector3.up * 0.2f, new Vector3(0.12f, 0.4f, 0.12f), "metal", false);
            g.Box("Marco monitor", start + Vector3.up * 0.6f, new Vector3(1.2f, 0.75f, 0.12f), "oscuro", false);
            g.Box("Pantalla", start + new Vector3(0, 0.6f, -0.071f), new Vector3(1.07f, 0.62f, 0.025f), "pantalla", false);
            for (int i = 0; i < 3; i++)
                g.Box("Línea en pantalla", start + new Vector3(-0.1f, 0.75f - i * 0.12f, -0.09f), new Vector3(0.62f - i * 0.1f, 0.04f, 0.012f), "papel", false);
        }
    }
}
