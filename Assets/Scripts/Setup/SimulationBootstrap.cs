using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace BattleRoyaleSim
{
    // Inicializador del ecosistema
    public class SimulationBootstrap : MonoBehaviour
    {
        [Header("Configuración de Partida")]
        public int playerCount = 10;
        public float mapDimension = 180f;

        private static readonly string[] PlayerNames = new string[]
        {
            "Alex (Sombra)", "Bruno (Titán)", "Carla (Viper)", "Diego (Fénix)", "Elena (Lobo)",
            "Fabián (Espectro)", "Gabi (Torbellino)", "Helena (Águila)", "Iván (Ranger)", "Julia (Cazadora)"
        };

        private static readonly Color[] PlayerColors = new Color[]
        {
            new Color(0.9f, 0.2f, 0.2f),   // Rojo
            new Color(0.2f, 0.5f, 1.0f),   // Azul
            new Color(0.1f, 0.85f, 0.35f), // Verde
            new Color(0.95f, 0.7f, 0.1f),  // Amarillo / Dorado
            new Color(0.8f, 0.3f, 0.95f),  // Púrpura
            new Color(1.0f, 0.45f, 0.1f),  // Naranja
            new Color(0.2f, 0.85f, 0.85f), // Cian
            new Color(0.95f, 0.4f, 0.65f), // Rosa
            new Color(0.6f, 0.8f, 0.2f),   // Lima
            new Color(0.85f, 0.85f, 0.9f)  // Blanco / Plata
        };

        private void Awake()
        {
            EnsureEventSystem();
            SetupCoreSimulation();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }
        }

        private void SetupCoreSimulation()
        {
            // 1. Núcleo Simulate.cs
            if (Simulate.Instance == null)
            {
                GameObject simObj = new GameObject("Simulation_Manager");
                simObj.AddComponent<Simulate>();
            }

            // 2. Entorno y Mapa
            MapEnvironment map = MapEnvironment.Instance;
            if (map == null)
            {
                GameObject mapObj = new GameObject("Environment");
                map = mapObj.AddComponent<MapEnvironment>();
                map.mapSize = mapDimension;
            }
            map.BuildCompleteMap();

            // 3. Entidad Tormenta
            StormController storm = StormController.Instance;
            if (storm == null)
            {
                GameObject stormObj = new GameObject("Storm_Entity");
                storm = stormObj.AddComponent<StormController>();
            }
            storm.SetupInitialState(Vector3.zero, mapDimension * 0.48f);

            // 4. Entidades Jugadores (10 jugadores con IA avanzada)
            SpawnPlayers(map);

            // 5. Configurar HUD y Ladder
            if (SimulationHUD.Instance == null)
            {
                GameObject hudObj = new GameObject("Simulation_HUD");
                hudObj.AddComponent<SimulationHUD>();
            }

            // 6. Configurar Cámara y deshabilitar sombras direccionales
            SetupCamera();
            Light dirLight = Object.FindAnyObjectByType<Light>();
            if (dirLight != null) dirLight.shadows = LightShadows.None;

            if (Simulate.Instance != null)
            {
                Simulate.Instance.LogEvent("¡Simulación Caso 2.2 iniciada con 10 jugadores en el mapa!");
            }
        }

        private void SpawnPlayers(MapEnvironment map)
        {
            List<Vector3> spawns = map.GetPlayerSpawnPositions(playerCount);

            for (int i = 0; i < playerCount; i++)
            {
                string pName = i < PlayerNames.Length ? PlayerNames[i] : $"Jugador {i + 1}";
                Color pColor = i < PlayerColors.Length ? PlayerColors[i] : Color.white;
                Vector3 spawnPos = spawns[i];

                GameObject pObj = new GameObject($"Player_{i + 1}_{pName}");
                PlayerAgent agent = pObj.AddComponent<PlayerAgent>();
                agent.Initialize(pName, pColor, spawnPos);

                // Agregar UI sobre la cabeza del jugador
                PlayerWorldUI worldUI = pObj.AddComponent<PlayerWorldUI>();
                worldUI.Initialize(agent);
            }
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            SimulationCamera simCam = cam.GetComponent<SimulationCamera>();
            if (simCam == null)
            {
                simCam = cam.gameObject.AddComponent<SimulationCamera>();
            }
            simCam.SetInitialOverview(Vector3.zero);
        }
    }
}

// Con apoyo de Gemini
