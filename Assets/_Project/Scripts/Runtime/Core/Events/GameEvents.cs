using UnityEngine;

namespace MatriculaDeHonor.Core
{
    // Catálogo de eventos globales del juego. Solo van aquí los eventos de "nivel de partida";
    // la comunicación entre un objeto y sus propios componentes usa eventos C# de instancia
    // (importante en el entrenamiento ML, donde hay muchas arenas en paralelo en la misma escena).

    public enum GameStateId
    {
        Boot,
        Loading,
        MainMenu,
        Playing,
        Paused,
        LevelComplete,
        GameOver,
        GameCompleted
    }

    public readonly struct GameStateChangedEvent : IEvent
    {
        public readonly GameStateId Previous;
        public readonly GameStateId Current;

        public GameStateChangedEvent(GameStateId previous, GameStateId current)
        {
            Previous = previous;
            Current = current;
        }
    }

    // ---------- Jugador ----------

    public readonly struct PlayerHealthChangedEvent : IEvent
    {
        public readonly int Current;
        public readonly int Max;
        public readonly int Shield;

        public PlayerHealthChangedEvent(int current, int max, int shield)
        {
            Current = current;
            Max = max;
            Shield = shield;
        }
    }

    public readonly struct PlayerDiedEvent : IEvent
    {
        public readonly Vector3 Position;

        public PlayerDiedEvent(Vector3 position)
        {
            Position = position;
        }
    }

    /// <summary>El GameManager pide al nivel que reaparezca el jugador en el último checkpoint.</summary>
    public readonly struct RespawnRequestedEvent : IEvent { }

    public readonly struct PowerUpChangedEvent : IEvent
    {
        public readonly string DisplayName;
        public readonly bool Active;
        public readonly float Duration;

        public PowerUpChangedEvent(string displayName, bool active, float duration)
        {
            DisplayName = displayName;
            Active = active;
            Duration = duration;
        }
    }

    // ---------- Sesión (vidas = convocatorias, monedas = créditos ECTS) ----------

    public readonly struct LivesChangedEvent : IEvent
    {
        public readonly int Lives;
        public readonly int Delta;

        public LivesChangedEvent(int lives, int delta)
        {
            Lives = lives;
            Delta = delta;
        }
    }

    public readonly struct CreditsChangedEvent : IEvent
    {
        public readonly int Total;
        public readonly int Delta;

        public CreditsChangedEvent(int total, int delta)
        {
            Total = total;
            Delta = delta;
        }
    }

    public readonly struct HonorsChangedEvent : IEvent
    {
        public readonly int Collected;
        public readonly int TotalInLevel;

        public HonorsChangedEvent(int collected, int totalInLevel)
        {
            Collected = collected;
            TotalInLevel = totalInLevel;
        }
    }

    // ---------- Objetos del nivel ----------

    public readonly struct CreditPickedUpEvent : IEvent
    {
        public readonly int Amount;

        public CreditPickedUpEvent(int amount)
        {
            Amount = amount;
        }
    }

    public readonly struct HonorPickedUpEvent : IEvent
    {
        public readonly int Index;

        public HonorPickedUpEvent(int index)
        {
            Index = index;
        }
    }

    public readonly struct CheckpointReachedEvent : IEvent
    {
        public readonly Vector3 RespawnPosition;
        public readonly Quaternion RespawnRotation;

        public CheckpointReachedEvent(Vector3 respawnPosition, Quaternion respawnRotation)
        {
            RespawnPosition = respawnPosition;
            RespawnRotation = respawnRotation;
        }
    }

    public readonly struct LevelGoalReachedEvent : IEvent { }

    public readonly struct BossDefeatedEvent : IEvent
    {
        public readonly string BossName;

        public BossDefeatedEvent(string bossName)
        {
            BossName = bossName;
        }
    }

    // ---------- Percepción de la IA ----------

    /// <summary>Ruido emitido en el mundo (correr, aterrizar, culetazo). Lo escuchan los profesores.</summary>
    public readonly struct NoiseEvent : IEvent
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly GameObject Source;

        public NoiseEvent(Vector3 position, float radius, GameObject source)
        {
            Position = position;
            Radius = radius;
            Source = source;
        }
    }

    /// <summary>Un profesor da la voz de alarma: los compañeros cercanos pasan a perseguir.</summary>
    public readonly struct AlarmRaisedEvent : IEvent
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly GameObject Source;

        public AlarmRaisedEvent(Vector3 position, float radius, GameObject source)
        {
            Position = position;
            Radius = radius;
            Source = source;
        }
    }
}
