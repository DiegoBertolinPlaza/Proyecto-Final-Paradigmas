using System.Collections;
using System.Collections.Generic;
using MatriculaDeHonor.Core;
using MatriculaDeHonor.Levels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MatriculaDeHonor.GameFlow
{
    /// <summary>
    /// Dirige el bucle de partida con una máquina de estados (MainMenu, Loading, Playing, Paused,
    /// LevelComplete, GameOver, GameCompleted). Sobrevive entre escenas y se registra en el
    /// ServiceLocator; la UI solo llama a sus métodos públicos (StartNewGame, TogglePause...).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField, Min(0f)] private float respawnDelay = 1.5f;

        private readonly Dictionary<GameStateId, GameFlowState> states = new Dictionary<GameStateId, GameFlowState>();
        private StateMachine flow;
        private InputAction pauseAction;
        private ISaveService saveService;
        private Coroutine pendingRespawn;
        private Coroutine pendingLoad;
        private bool isDuplicate;

        public GameStateId CurrentState { get; private set; } = GameStateId.Boot;
        public GameSession Session { get; private set; }
        public SaveData Progress { get; private set; }
        public LevelCatalog Catalog => catalog;
        public LevelDefinition CurrentLevel => catalog != null && Session != null ? catalog.Get(Session.CurrentLevelIndex) : null;
        public bool HasNextLevel => catalog != null && Session != null && catalog.Get(Session.CurrentLevelIndex + 1) != null;

        /// <summary>Lo llama el bootstrapper antes del primer Start.</summary>
        public void Initialize(LevelCatalog levelCatalog)
        {
            catalog = levelCatalog;
        }

        private void Awake()
        {
            if (ServiceLocator.TryGet(out GameManager existing) && existing != this)
            {
                isDuplicate = true;
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register(this);

            saveService = new JsonFileSaveService();
            Progress = saveService.Load();
            Session = new GameSession();

            pauseAction = new InputAction("Pause", InputActionType.Button);
            pauseAction.AddBinding("<Keyboard>/escape");
            pauseAction.AddBinding("<Keyboard>/p");
            pauseAction.AddBinding("<Gamepad>/start");

            BuildStates();
        }

        private void OnEnable()
        {
            if (isDuplicate) return;

            pauseAction.Enable();
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            EventBus<LevelGoalReachedEvent>.Subscribe(OnLevelGoalReached);
            EventBus<CreditPickedUpEvent>.Subscribe(OnCreditPickedUp);
            EventBus<HonorPickedUpEvent>.Subscribe(OnHonorPickedUp);
        }

        private void OnDisable()
        {
            if (isDuplicate) return;

            pauseAction.Disable();
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
            EventBus<LevelGoalReachedEvent>.Unsubscribe(OnLevelGoalReached);
            EventBus<CreditPickedUpEvent>.Unsubscribe(OnCreditPickedUp);
            EventBus<HonorPickedUpEvent>.Unsubscribe(OnHonorPickedUp);
        }

        private void OnDestroy()
        {
            if (isDuplicate) return;

            ServiceLocator.Unregister(this);
            pauseAction?.Dispose();
            Time.timeScale = 1f;
        }

        private void Start()
        {
            if (isDuplicate) return;
            EnterInitialState();
        }

        private void Update()
        {
            if (isDuplicate) return;

            if (pauseAction.WasPressedThisFrame())
            {
                TogglePause();
            }

            flow.Tick(Time.unscaledDeltaTime);
        }

        // ---------- API pública (botones de la UI) ----------

        public void StartNewGame()
        {
            Session = new GameSession();
            LoadLevel(0);
        }

        public void LoadLevel(int index)
        {
            var level = catalog != null ? catalog.Get(index) : null;
            if (level == null)
            {
                Debug.LogError($"[GameManager] No existe el nivel {index} en el catálogo.");
                return;
            }

            CancelPendingRoutines();
            pendingLoad = StartCoroutine(LoadLevelRoutine(index, level.sceneName));
        }

        public void RestartLevel()
        {
            if (CurrentLevel != null)
            {
                LoadLevel(Session.CurrentLevelIndex);
                return;
            }

            // Escena suelta (sin catálogo): se recarga tal cual.
            CancelPendingRoutines();
            pendingLoad = StartCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name, () =>
            {
                Session.BeginLevel(-1, 0);
                ChangeState(GameStateId.Playing);
            }));
        }

        public void ContinueToNextLevel()
        {
            if (HasNextLevel)
            {
                LoadLevel(Session.CurrentLevelIndex + 1);
            }
            else
            {
                ReturnToMainMenu();
            }
        }

        public void TogglePause()
        {
            if (CurrentState == GameStateId.Playing) ChangeState(GameStateId.Paused);
            else if (CurrentState == GameStateId.Paused) ChangeState(GameStateId.Playing);
        }

        public void ReturnToMainMenu()
        {
            if (catalog == null || !Application.CanStreamedLevelBeLoaded(catalog.MainMenuScene))
            {
                Debug.LogWarning("[GameManager] No hay escena de menú en Build Settings.");
                return;
            }

            CancelPendingRoutines();
            pendingLoad = StartCoroutine(LoadSceneRoutine(catalog.MainMenuScene, () => ChangeState(GameStateId.MainMenu)));
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- Internos ----------

        private void BuildStates()
        {
            flow = new StateMachine();

            Add(new MainMenuGameState(this));
            Add(new LoadingGameState(this));
            Add(new PlayingGameState(this));
            Add(new PausedGameState(this));
            Add(new LevelCompleteGameState(this));
            Add(new GameOverGameState(this));
            Add(new GameCompletedGameState(this));

            void Add(GameFlowState state) => states[state.Id] = state;
        }

        private void EnterInitialState()
        {
            string activeScene = SceneManager.GetActiveScene().name;

            if (catalog != null && activeScene == catalog.MainMenuScene)
            {
                ChangeState(GameStateId.MainMenu);
                return;
            }

            // Se ha dado a Play directamente en un nivel (o en una escena de pruebas).
            int index = catalog != null && catalog.TryGetIndex(activeScene, out int found) ? found : -1;
            Session = new GameSession();
            Session.BeginLevel(index, CurrentLevel != null ? CurrentLevel.honorCount : 0);
            ChangeState(GameStateId.Playing);
        }

        private void ChangeState(GameStateId id)
        {
            if (!states.TryGetValue(id, out var state))
            {
                Debug.LogError($"[GameManager] Estado {id} no registrado.");
                return;
            }

            var previous = CurrentState;
            CurrentState = id;
            flow.SetState(state);
            EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent(previous, id));
        }

        private IEnumerator LoadLevelRoutine(int index, string sceneName)
        {
            ChangeState(GameStateId.Loading);
            yield return LoadScene(sceneName);

            var level = catalog.Get(index);
            Session.BeginLevel(index, level != null ? level.honorCount : 0);
            pendingLoad = null;
            ChangeState(GameStateId.Playing);
        }

        private IEnumerator LoadSceneRoutine(string sceneName, System.Action onLoaded)
        {
            ChangeState(GameStateId.Loading);
            yield return LoadScene(sceneName);
            pendingLoad = null;
            onLoaded?.Invoke();
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[GameManager] La escena '{sceneName}' no está en Build Settings.");
                yield break;
            }

            var operation = SceneManager.LoadSceneAsync(sceneName);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
        }

        private void CancelPendingRoutines()
        {
            if (pendingRespawn != null) StopCoroutine(pendingRespawn);
            if (pendingLoad != null) StopCoroutine(pendingLoad);
            pendingRespawn = null;
            pendingLoad = null;
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            pendingRespawn = null;

            if (CurrentState == GameStateId.Playing || CurrentState == GameStateId.Paused)
            {
                EventBus<RespawnRequestedEvent>.Raise(new RespawnRequestedEvent());
            }
        }

        private void RecordLevelCompleted()
        {
            int index = Session.CurrentLevelIndex;
            if (index < 0) return;

            var record = Progress.GetOrCreate(index);
            record.completed = true;
            foreach (int honor in Session.HonorIndicesThisLevel)
            {
                if (!record.honors.Contains(honor)) record.honors.Add(honor);
            }

            Progress.highestUnlockedLevel = Mathf.Max(Progress.highestUnlockedLevel, index + 1);
            saveService.Save(Progress);
        }

        // ---------- Eventos ----------

        private void OnPlayerDied(PlayerDiedEvent _)
        {
            if (CurrentState != GameStateId.Playing) return;

            if (Session.LoseLife())
            {
                if (pendingRespawn != null) StopCoroutine(pendingRespawn);
                pendingRespawn = StartCoroutine(RespawnAfterDelay());
            }
            else
            {
                ChangeState(GameStateId.GameOver);
            }
        }

        private void OnLevelGoalReached(LevelGoalReachedEvent _)
        {
            if (CurrentState != GameStateId.Playing) return;

            RecordLevelCompleted();
            bool finishedGame = catalog != null && Session.CurrentLevelIndex >= 0 && catalog.IsLastLevel(Session.CurrentLevelIndex);
            ChangeState(finishedGame ? GameStateId.GameCompleted : GameStateId.LevelComplete);
        }

        private void OnCreditPickedUp(CreditPickedUpEvent evt) => Session.AddCredits(evt.Amount);

        private void OnHonorPickedUp(HonorPickedUpEvent evt) => Session.RegisterHonor(evt.Index);
    }
}
