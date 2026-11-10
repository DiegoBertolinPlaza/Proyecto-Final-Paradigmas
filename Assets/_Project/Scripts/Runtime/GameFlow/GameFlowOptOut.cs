using UnityEngine;

namespace MatriculaDeHonor.GameFlow
{
    /// <summary>
    /// Marcador: si está en la escena inicial, NO se crea el GameManager. Para arenas de
    /// entrenamiento de ML-Agents y escenas de pruebas donde no queremos vidas ni menús.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameFlowOptOut : MonoBehaviour { }
}
