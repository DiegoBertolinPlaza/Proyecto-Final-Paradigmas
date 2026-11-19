using System.IO;
using UnityEditor;
using UnityEngine;

namespace MatriculaDeHonor.Editor
{
    /// <summary>
    /// Utilidades de assets para las herramientas de editor: crear carpetas, ScriptableObjects y
    /// materiales solo si no existen. Así repetir una herramienta nunca pisa lo que ya habéis ajustado.
    /// </summary>
    public static class EditorAssets
    {
        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureFolder(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>Material liso de prototipo (URP Lit). Devuelve null si no encuentra el shader.</summary>
        public static Material LoadOrCreateMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return null;

            EnsureFolder(Path.GetDirectoryName(path));
            material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>Crea la carpeta (y las intermedias) dentro de Assets si no existen.</summary>
        public static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>
        /// Asigna un campo privado [SerializeField] como lo haría el Inspector. Falla en alto si el
        /// campo no existe, para enterarnos enseguida si alguien lo renombra.
        /// </summary>
        public static void SetField(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new System.ArgumentException($"{target.GetType().Name} no tiene el campo serializado '{fieldName}'.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Unity deja el campo a null sin avisar si el objeto no encaja o ya está destruido.
            if (value != null && property.objectReferenceValue != value)
            {
                throw new System.ArgumentException(
                    $"{target.GetType().Name}.{fieldName} no ha aceptado '{value.name}' ({value.GetType().Name}).");
            }
        }
    }
}
