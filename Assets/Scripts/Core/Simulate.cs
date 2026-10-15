using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyaleSim
{
    [DefaultExecutionOrder(-100)]
    public class Simulate : MonoBehaviour
    {
        public static Simulate Instance { get; private set; }

        [Header("Configuración de Simulación")]
        [Tooltip("Velocidad de reproducción de la simulación (1x, 2x, 4x...)")]
        [Range(0f, 10f)]
        public float simulationSpeed = 1f;

        [Tooltip("Si la simulación está actualmente pausada")]
        public bool isPaused = false;

        [Header("Telemetría y Estado Global")]
        [SerializeField] private float matchTime = 0f;
        [SerializeField] private int alivePlayersCount = 0;
        [SerializeField] private string winnerName = "Ninguno";
        [SerializeField] private bool matchOver = false;

        // Lista de todas las entidades activas que implementan ISimulatable
        private readonly List<ISimulatable> entities = new List<ISimulatable>();
        private readonly List<ISimulatable> entitiesToAdd = new List<ISimulatable>();
        private readonly List<ISimulatable> entitiesToRemove = new List<ISimulatable>();

        // Eventos para la interfaz y sistema de telemetría
        public event Action<string> OnEventLogged;
        public event Action<float> OnSimulationTick;
        public event Action<string> OnGameOver;

        public float MatchTime => matchTime;
        public int AlivePlayersCount => alivePlayersCount;
        public string WinnerName => winnerName;
        public bool IsMatchOver => matchOver;
        public IReadOnlyList<ISimulatable> Entities => entities;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Registra una entidad para que participe en el ciclo Simulate().
        /// </summary>
        public void RegisterEntity(ISimulatable entity)
        {
            if (entity != null && !entities.Contains(entity) && !entitiesToAdd.Contains(entity))
            {
                entitiesToAdd.Add(entity);
            }
        }

        /// <summary>
        /// Da de baja una entidad del ciclo de simulación.
        /// </summary>
        public void UnregisterEntity(ISimulatable entity)
        {
            if (entity != null && !entitiesToRemove.Contains(entity))
            {
                entitiesToRemove.Add(entity);
            }
        }

        /// <summary>
        /// Limpia todas las entidades registradas.
        /// </summary>
        public void ClearEntities()
        {
            entities.Clear();
            entitiesToAdd.Clear();
            entitiesToRemove.Clear();
        }

        private void Update()
        {
            if (isPaused) return;

            // Procesar altas y bajas pendientes de manera segura
            if (entitiesToAdd.Count > 0)
            {
                entities.AddRange(entitiesToAdd);
                entitiesToAdd.Clear();
            }

            if (entitiesToRemove.Count > 0)
            {
                for (int i = 0; i < entitiesToRemove.Count; i++)
                {
                    entities.Remove(entitiesToRemove[i]);
                }
                entitiesToRemove.Clear();
            }

            float dt = Time.deltaTime * simulationSpeed;
            matchTime += dt;

            // INVOCACIÓN CENTRALIZADA: Llama a Simulate() en cada entidad registrada
            for (int i = 0; i < entities.Count; i++)
            {
                ISimulatable entity = entities[i];
                if (entity != null && entity.IsActive)
                {
                    entity.Simulate(dt);
                }
            }

            // Notificar tick
            OnSimulationTick?.Invoke(dt);

            // Comprobar estado de victoria
            CheckMatchStatus();
        }

        private void CheckMatchStatus()
        {
            if (matchOver) return;

            int alive = 0;
            string lastAliveName = "";

            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i] is PlayerAgent player && player.IsActive && !player.IsDead)
                {
                    alive++;
                    lastAliveName = player.EntityName;
                }
            }

            alivePlayersCount = alive;

            // Si solo queda 1 jugador con vida (o ninguno), la partida concluye
            if (alive == 1 && matchTime > 5f)
            {
                matchOver = true;
                winnerName = lastAliveName;
                string victoryMsg = $"¡VICTORIA ROYALE! {winnerName} ha ganado la simulación tras {matchTime:F1}s.";
                LogEvent(victoryMsg);
                OnGameOver?.Invoke(winnerName);
            }
            else if (alive == 0 && matchTime > 5f)
            {
                matchOver = true;
                winnerName = "Nadie (Empate / Tormenta)";
                string tieMsg = $"¡FIN DE PARTIDA! Todos los jugadores han caído.";
                LogEvent(tieMsg);
                OnGameOver?.Invoke(winnerName);
            }
        }

        /// <summary>
        /// Registra un evento en el feed global y notifica suscriptores.
        /// </summary>
        public void LogEvent(string message)
        {
            string timeStamped = $"[{matchTime:F1}s] {message}";
            Debug.Log($"[SIMULATE] {timeStamped}");
            OnEventLogged?.Invoke(timeStamped);
        }

        // Reinicia la simulacion recargando la escena
        public void RestartSimulation()
        {
            matchTime = 0f;
            matchOver = false;
            winnerName = "Ninguno";
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public void SetSpeed(float speed)
        {
            simulationSpeed = Mathf.Max(0f, speed);
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
        }
    }
}

// Con apoyo de Gemini
