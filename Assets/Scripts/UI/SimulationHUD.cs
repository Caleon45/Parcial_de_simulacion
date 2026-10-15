using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyaleSim
{
    public class SimulationHUD : MonoBehaviour
    {
        public static SimulationHUD Instance { get; private set; }

        public PlayerAgent selectedPlayer { get; private set; }

        private GUIStyle titleStyle;
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle victoryStyle;
        private bool stylesInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;
            stylesInitialized = true;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = Color.white;

            boxStyle = new GUIStyle(GUI.skin.box);

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12
            };
            labelStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            victoryStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            victoryStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        }

        public void SelectPlayer(PlayerAgent player)
        {
            selectedPlayer = player;
        }

        private void OnGUI()
        {
            InitStyles();

            DrawTopBar();
            DrawLadder();
            DrawBottomControls();

            // Pantalla de victoria si la partida terminó
            if (Simulate.Instance != null && Simulate.Instance.IsMatchOver)
            {
                DrawVictoryScreen();
            }
        }

        private void DrawTopBar()
        {
            float w = 620f;
            float h = 42f;
            float x = (Screen.width - w) * 0.5f;

            GUI.Box(new Rect(x, 10, w, h), "");

            string timeText = Simulate.Instance != null ? $"⏱ Tiempo: {Simulate.Instance.MatchTime:F1}s | Vel: {Simulate.Instance.simulationSpeed:F1}x" : "";
            string aliveText = Simulate.Instance != null ? $"⚔ Vivos: {Simulate.Instance.AlivePlayersCount}/10" : "";
            string stormText = "";

            if (StormController.Instance != null)
            {
                string stateName = StormController.Instance.currentState == StormState.Waiting ? "Espera" : "Cierre";
                stormText = $"⚡ Tormenta F{StormController.Instance.currentPhaseIndex + 1} ({stateName}): {StormController.Instance.stateTimer:F0}s";
            }

            GUI.Label(new Rect(x + 15, 18, 220, 25), timeText, labelStyle);
            GUI.Label(new Rect(x + 245, 18, 140, 25), aliveText, titleStyle);
            GUI.Label(new Rect(x + 395, 18, 215, 25), stormText, labelStyle);
        }

        // Tabla de clasificación (Ladder) con los 10 jugadores
        private void DrawLadder()
        {
            float w = 260f;
            float h = 265f;
            float x = Screen.width - w - 15f;
            float y = 15f;

            GUI.Box(new Rect(x, y, w, h), "TABLA DE JUGADORES (LADDER)");

            float startY = y + 25f;
            float rowHeight = 23f;

            for (int i = 0; i < PlayerAgent.ActivePlayers.Count; i++)
            {
                PlayerAgent p = PlayerAgent.ActivePlayers[i];
                if (p == null) continue;

                string status = p.IsDead ? "<color=#888888>[ELIMINADO]</color>" : $"<color=#00ff88>HP:{p.currentHealth:F0}</color>";
                string wep = p.HasWeapon ? $"▲{p.equippedWeapon.weaponName.Substring(10)}" : "Desarmado";
                string rowText = $"{p.playerName.Split(' ')[0]}: {status} | {wep}";

                // Botón rápido para seleccionar e inspeccionar al bot
                if (GUI.Button(new Rect(x + 8, startY + (i * rowHeight), w - 16, rowHeight - 2), rowText, labelStyle))
                {
                    SelectPlayer(p);
                    if (SimulationCamera.Instance != null)
                    {
                        SimulationCamera.Instance.FollowTarget(p.transform);
                    }
                }
            }
        }

        private void DrawBottomControls()
        {
            float w = 460f;
            float h = 42f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - h - 12f;

            GUI.Box(new Rect(x, y, w, h), "");

            bool isPaused = Simulate.Instance != null && Simulate.Instance.isPaused;
            if (GUI.Button(new Rect(x + 10, y + 6, 85, 30), isPaused ? "Reanudar" : "Pausar", buttonStyle))
            {
                Simulate.Instance.TogglePause();
            }

            if (GUI.Button(new Rect(x + 105, y + 6, 60, 30), "1x", buttonStyle))
            {
                Simulate.Instance.SetSpeed(1f);
            }

            if (GUI.Button(new Rect(x + 175, y + 6, 60, 30), "2x", buttonStyle))
            {
                Simulate.Instance.SetSpeed(2f);
            }

            if (GUI.Button(new Rect(x + 245, y + 6, 60, 30), "4x", buttonStyle))
            {
                Simulate.Instance.SetSpeed(4f);
            }

            if (GUI.Button(new Rect(x + 315, y + 6, 135, 30), "Reiniciar Partida", buttonStyle))
            {
                Simulate.Instance.RestartSimulation();
            }
        }

        // Mensaje de victoria modal con botón de reiniciar
        private void DrawVictoryScreen()
        {
            float w = 480f;
            float h = 240f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            // Fondo de victoria
            GUI.Box(new Rect(x, y, w, h), "\n¡¡VICTORIA ROYALE!!", victoryStyle);

            string winner = Simulate.Instance != null ? Simulate.Instance.WinnerName : "Jugador";
            string infoText = $"¡El jugador {winner} es el último sobreviviente!\nDuración de la partida: {Simulate.Instance.MatchTime:F1}s";

            GUIStyle centerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            centerStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(x + 20, y + 75, w - 40, 60), infoText, centerStyle);

            // Botón destacado para reiniciar
            if (GUI.Button(new Rect(x + 120, y + 155, 240, 48), "Reiniciar Simulación", buttonStyle))
            {
                Simulate.Instance.RestartSimulation();
            }
        }
    }
}

// Con apoyo de Gemini
