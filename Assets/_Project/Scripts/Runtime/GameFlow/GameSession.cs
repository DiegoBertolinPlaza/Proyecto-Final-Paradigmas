using System;
using System.Collections.Generic;
using MatriculaDeHonor.Core;

namespace MatriculaDeHonor.GameFlow
{
    /// <summary>
    /// Estado de UNA partida: convocatorias (vidas), créditos ECTS y Matrículas de Honor del
    /// nivel actual. "Volver a jugar" crea una sesión nueva: no hay nada que reiniciar a mano.
    /// </summary>
    public sealed class GameSession
    {
        public const int DefaultStartingLives = 4;
        public const int CreditsPerExtraLife = 100;

        private readonly HashSet<int> honorsThisLevel = new HashSet<int>();

        public GameSession(int startingLives = DefaultStartingLives)
        {
            if (startingLives <= 0) throw new ArgumentOutOfRangeException(nameof(startingLives));
            Lives = startingLives;
            CurrentLevelIndex = -1;
        }

        public int Lives { get; private set; }
        public int Credits { get; private set; }
        public int CurrentLevelIndex { get; private set; }
        public int HonorsTotalInLevel { get; private set; }
        public int HonorsThisLevel => honorsThisLevel.Count;
        public IReadOnlyCollection<int> HonorIndicesThisLevel => honorsThisLevel;

        public void BeginLevel(int levelIndex, int honorsInLevel)
        {
            CurrentLevelIndex = levelIndex;
            HonorsTotalInLevel = Math.Max(0, honorsInLevel);
            honorsThisLevel.Clear();
            PublishSnapshot();
        }

        public void AddCredits(int amount)
        {
            if (amount <= 0) return;

            int livesBefore = Credits / CreditsPerExtraLife;
            Credits += amount;
            EventBus<CreditsChangedEvent>.Raise(new CreditsChangedEvent(Credits, amount));

            int extraLives = Credits / CreditsPerExtraLife - livesBefore;
            if (extraLives > 0)
            {
                AddLives(extraLives);
            }
        }

        public void AddLives(int amount)
        {
            if (amount <= 0) return;

            Lives += amount;
            EventBus<LivesChangedEvent>.Raise(new LivesChangedEvent(Lives, amount));
        }

        /// <summary>Gasta una convocatoria. Devuelve true si aún quedan.</summary>
        public bool LoseLife()
        {
            if (Lives > 0)
            {
                Lives--;
                EventBus<LivesChangedEvent>.Raise(new LivesChangedEvent(Lives, -1));
            }

            return Lives > 0;
        }

        /// <summary>Registra una Matrícula de Honor. Devuelve false si ya estaba cogida.</summary>
        public bool RegisterHonor(int index)
        {
            if (!honorsThisLevel.Add(index)) return false;

            EventBus<HonorsChangedEvent>.Raise(new HonorsChangedEvent(honorsThisLevel.Count, HonorsTotalInLevel));
            return true;
        }

        /// <summary>Publica todos los valores (para que el HUD se pinte al empezar un nivel).</summary>
        public void PublishSnapshot()
        {
            EventBus<LivesChangedEvent>.Raise(new LivesChangedEvent(Lives, 0));
            EventBus<CreditsChangedEvent>.Raise(new CreditsChangedEvent(Credits, 0));
            EventBus<HonorsChangedEvent>.Raise(new HonorsChangedEvent(honorsThisLevel.Count, HonorsTotalInLevel));
        }
    }
}
