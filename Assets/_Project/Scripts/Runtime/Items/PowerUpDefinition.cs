using System;
using System.Collections.Generic;
using MatriculaDeHonor.Player.Stats;
using UnityEngine;

namespace MatriculaDeHonor.Items
{
    public enum ModifierMode
    {
        Multiply,
        Add,
        Override
    }

    [Serializable]
    public struct StatModifierEntry
    {
        public StatType stat;
        public ModifierMode mode;
        public float value;
    }

    /// <summary>
    /// Power-up definido como datos. Al aplicarse envuelve las estadísticas del jugador con un
    /// decorador por cada entrada (patrón Decorator). Ejemplos:
    ///  · Café: RunSpeed ×1.4, WalkSpeed ×1.3 durante 10 s.
    ///  · Sudadera con capucha: NoiseMultiplier ×0.2 (sigilo en la biblioteca).
    ///  · Bata de laboratorio: ChemicalResistance = 1 (inmune a charcos).
    ///  · Chuleta: sin modificadores, 1 carga de escudo.
    /// </summary>
    [CreateAssetMenu(menuName = "Matrícula de Honor/Objetos/Power-up", fileName = "PowerUp")]
    public sealed class PowerUpDefinition : ScriptableObject, IStatModifierSource
    {
        [SerializeField] private string displayName = "Café";
        [Tooltip("Segundos. 0 = dura hasta que pierdas una convocatoria.")]
        [SerializeField, Min(0f)] private float duration = 10f;
        [Tooltip("Golpes que absorbe (la chuleta).")]
        [SerializeField, Min(0)] private int shieldCharges;
        [SerializeField] private Color color = new Color(0.55f, 0.35f, 0.2f);
        [SerializeField] private List<StatModifierEntry> modifiers = new List<StatModifierEntry>();

        public string DisplayName => displayName;
        public float Duration => duration;
        public int ShieldCharges => shieldCharges;
        public Color Color => color;
        public bool HasStatModifiers => modifiers != null && modifiers.Count > 0;

        public IStatProvider Decorate(IStatProvider inner)
        {
            IStatProvider current = inner;
            if (modifiers == null) return current;

            foreach (var entry in modifiers)
            {
                current = entry.mode switch
                {
                    ModifierMode.Multiply => new StatMultiplierDecorator(current, entry.stat, entry.value),
                    ModifierMode.Add => new StatAdditiveDecorator(current, entry.stat, entry.value),
                    ModifierMode.Override => new StatOverrideDecorator(current, entry.stat, entry.value),
                    _ => current
                };
            }

            return current;
        }

#if UNITY_EDITOR
        /// <summary>Solo para el generador de escenas del editor.</summary>
        public void EditorSetup(string name, float seconds, int shield, Color tint, params StatModifierEntry[] entries)
        {
            displayName = name;
            duration = seconds;
            shieldCharges = shield;
            color = tint;
            modifiers = new List<StatModifierEntry>(entries);
        }
#endif
    }
}
