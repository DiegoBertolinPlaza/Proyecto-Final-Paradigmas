using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MatriculaDeHonor.GameFlow
{
    [Serializable]
    public sealed class LevelRecord
    {
        public int levelIndex;
        public bool completed;
        public List<int> honors = new List<int>();
    }

    [Serializable]
    public sealed class SaveData
    {
        public int highestUnlockedLevel;
        public List<LevelRecord> levels = new List<LevelRecord>();

        public LevelRecord GetOrCreate(int levelIndex)
        {
            foreach (var record in levels)
            {
                if (record.levelIndex == levelIndex) return record;
            }

            var created = new LevelRecord { levelIndex = levelIndex };
            levels.Add(created);
            return created;
        }
    }

    public interface ISaveService
    {
        SaveData Load();
        void Save(SaveData data);
        void DeleteAll();
    }

    /// <summary>Guarda el progreso en JSON en la carpeta de datos persistentes del sistema.</summary>
    public sealed class JsonFileSaveService : ISaveService
    {
        private readonly string path;

        public JsonFileSaveService(string fileName = "progreso.json")
        {
            path = Path.Combine(Application.persistentDataPath, fileName);
        }

        public SaveData Load()
        {
            try
            {
                if (!File.Exists(path)) return new SaveData();
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(path)) ?? new SaveData();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Save] No se pudo leer {path}; se empieza de cero. {exception.Message}");
                return new SaveData();
            }
        }

        public void Save(SaveData data)
        {
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Save] No se pudo guardar en {path}: {exception.Message}");
            }
        }

        public void DeleteAll()
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
