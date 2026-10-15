using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyaleSim
{
    public enum StormState
    {
        Waiting,
        Shrinking,
        Finished
    }

    [System.Serializable]
    public class StormPhaseConfig
    {
        public float waitDuration = 20f;
        public float shrinkDuration = 15f;
        public float targetRadius = 50f;
        public float damagePerSecond = 4f;

        public StormPhaseConfig(float wait, float shrink, float radius, float dps)
        {
            waitDuration = wait;
            shrinkDuration = shrink;
            targetRadius = radius;
            damagePerSecond = dps;
        }
    }

    // Entidad de Tormenta
    public class StormController : MonoBehaviour, ISimulatable
    {
        public static StormController Instance { get; private set; }

        [Header("Estado de la Tormenta")]
        public int currentPhaseIndex = 0;
        public StormState currentState = StormState.Waiting;
        public float stateTimer = 0f;

        [Header("Dimensiones de Zona")]
        public Vector3 currentCenter = Vector3.zero;
        public float currentRadius = 90f;

        public Vector3 nextCenter = Vector3.zero;
        public float nextRadius = 60f;

        [Header("Parámetros de Daño")]
        public float currentDamagePerSecond = 3f;
        private float damageTickAccumulator = 0f;

        [Header("Fases Predefinidas")]
        public List<StormPhaseConfig> phases = new List<StormPhaseConfig>();

        [Header("Visuales Procedurales")]
        private Transform stormVisualCylinder;
        private LineRenderer currentBorderLine;
        private LineRenderer nextBorderLine;

        public string EntityName => "Tormenta";
        public bool IsActive => gameObject.activeInHierarchy;
        public float InitialMapRadius { get; private set; } = 95f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePhases();
            BuildVisuals();
        }

        private void OnEnable()
        {
            if (BattleRoyaleSim.Simulate.Instance != null)
                BattleRoyaleSim.Simulate.Instance.RegisterEntity(this);
        }

        private void OnDisable()
        {
            if (BattleRoyaleSim.Simulate.Instance != null)
                BattleRoyaleSim.Simulate.Instance.UnregisterEntity(this);
        }

        private void InitializePhases()
        {
            phases.Clear();
            phases.Add(new StormPhaseConfig(18f, 16f, 60f, 3f));   // Fase 1
            phases.Add(new StormPhaseConfig(15f, 14f, 38f, 6f));   // Fase 2
            phases.Add(new StormPhaseConfig(12f, 12f, 20f, 10f));  // Fase 3
            phases.Add(new StormPhaseConfig(10f, 10f, 8f, 16f));   // Fase 4
            phases.Add(new StormPhaseConfig(8f, 8f, 0.5f, 28f));   // Muerte súbita final
        }

        public void SetupInitialState(Vector3 center, float startRadius)
        {
            currentCenter = center;
            currentRadius = startRadius;
            InitialMapRadius = startRadius;

            currentPhaseIndex = 0;
            currentState = StormState.Waiting;
            stateTimer = 0f;

            CalculateNextSafeZone();
            UpdateVisualRings();
        }

        private void CalculateNextSafeZone()
        {
            if (currentPhaseIndex < phases.Count)
            {
                StormPhaseConfig cfg = phases[currentPhaseIndex];
                nextRadius = cfg.targetRadius;

                // El nuevo centro se sitúa aleatoriamente dentro del círculo actual para dar pistas
                float maxOffset = Mathf.Max(0f, currentRadius - nextRadius);
                Vector2 randomCircle = Random.insideUnitCircle * (maxOffset * 0.75f);
                nextCenter = currentCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
                currentDamagePerSecond = cfg.damagePerSecond;
            }
            else
            {
                nextRadius = 0.5f;
                nextCenter = currentCenter;
                currentDamagePerSecond = 35f;
            }
        }

        private void BuildVisuals()
        {
            // Crear cilindro visual de la tormenta (muro de energía translúcido)
            GameObject cylinderObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinderObj.name = "Storm_EnergyWall";
            cylinderObj.transform.SetParent(transform, false);
            Destroy(cylinderObj.GetComponent<Collider>());

            MeshRenderer ren = cylinderObj.GetComponent<MeshRenderer>();
            ren.material = CreateStormMaterial();
            stormVisualCylinder = cylinderObj.transform;

            // Línea del borde actual (Borde morado brillante)
            GameObject curLineObj = new GameObject("Storm_CurrentRingLine");
            curLineObj.transform.SetParent(transform, false);
            currentBorderLine = curLineObj.AddComponent<LineRenderer>();
            SetupLineRenderer(currentBorderLine, new Color(0.85f, 0.2f, 0.95f, 0.9f), 0.45f);

            // Línea del borde siguiente (Pistas de zona segura futura - Blanco punteado / cian)
            GameObject nextLineObj = new GameObject("Storm_NextSafeRingLine");
            nextLineObj.transform.SetParent(transform, false);
            nextBorderLine = nextLineObj.AddComponent<LineRenderer>();
            SetupLineRenderer(nextBorderLine, new Color(0.2f, 0.95f, 1f, 0.7f), 0.3f);
        }

        private Material CreateStormMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            Color stormColor = new Color(0.6f, 0.1f, 0.85f, 0.28f);
            mat.color = stormColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", stormColor);

            // Configurar modo transparente si es URP/Estándar
            mat.SetFloat("_Surface", 1); // Transparent en URP
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;

            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", new Color(0.8f, 0.15f, 1f) * 0.8f);
            }

            return mat;
        }

        private void SetupLineRenderer(LineRenderer line, Color color, float width)
        {
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 60;
            line.startWidth = width;
            line.endWidth = width;
            line.material = MeshBuilder.CreateColoredMaterial(color, 0.5f, true);
        }

        /// <summary>
        /// Ciclo de simulación orquestado por Simulate.cs
        /// </summary>
        public void Simulate(float deltaTime)
        {
            stateTimer += deltaTime;

            if (currentPhaseIndex >= phases.Count)
            {
                currentState = StormState.Finished;
            }
            else
            {
                StormPhaseConfig cfg = phases[currentPhaseIndex];

                if (currentState == StormState.Waiting)
                {
                    if (stateTimer >= cfg.waitDuration)
                    {
                        currentState = StormState.Shrinking;
                        stateTimer = 0f;
                        if (BattleRoyaleSim.Simulate.Instance != null)
                        {
                            BattleRoyaleSim.Simulate.Instance.LogEvent($"¡La Tormenta Fase {currentPhaseIndex + 1} está encogiéndose!");
                        }
                    }
                }
                else if (currentState == StormState.Shrinking)
                {
                    float t = Mathf.Clamp01(stateTimer / cfg.shrinkDuration);
                    float prevRadius = currentPhaseIndex > 0 ? phases[currentPhaseIndex - 1].targetRadius : InitialMapRadius;
                    currentRadius = Mathf.Lerp(prevRadius, nextRadius, t);
                    currentCenter = Vector3.Lerp(currentCenter, nextCenter, t);

                    if (stateTimer >= cfg.shrinkDuration)
                    {
                        currentRadius = nextRadius;
                        currentCenter = nextCenter;
                        currentPhaseIndex++;
                        currentState = StormState.Waiting;
                        stateTimer = 0f;

                        CalculateNextSafeZone();

                        if (BattleRoyaleSim.Simulate.Instance != null)
                        {
                            BattleRoyaleSim.Simulate.Instance.LogEvent($"La Tormenta se ha detenido. Fase {currentPhaseIndex + 1} preparada.");
                        }
                    }
                }
            }

            // Aplicar daño continuo a todos los jugadores que estén fuera de la zona segura
            ApplyStormDamage(deltaTime);

            // Actualizar representación visual
            UpdateVisualRings();
        }

        private void ApplyStormDamage(float deltaTime)
        {
            damageTickAccumulator += deltaTime;
            if (damageTickAccumulator >= 0.5f) // Ticks cada 0.5 segundos
            {
                float damageToDeal = currentDamagePerSecond * damageTickAccumulator;
                damageTickAccumulator = 0f;

                for (int i = 0; i < PlayerAgent.ActivePlayers.Count; i++)
                {
                    PlayerAgent player = PlayerAgent.ActivePlayers[i];
                    if (player != null && !player.IsDead)
                    {
                        if (!IsInsideSafeZone(player.transform.position))
                        {
                            player.TakeDamage(damageToDeal, null, "la Tormenta");
                        }
                    }
                }
            }
        }

        private void UpdateVisualRings()
        {
            // Ajustar cilindro
            if (stormVisualCylinder != null)
            {
                stormVisualCylinder.position = new Vector3(currentCenter.x, 10f, currentCenter.z);
                stormVisualCylinder.localScale = new Vector3(currentRadius * 2f, 20f, currentRadius * 2f);
            }

            // Dibujar círculo actual
            DrawCircle(currentBorderLine, currentCenter, currentRadius, 0.4f);

            // Dibujar círculo de pista de siguiente zona segura
            DrawCircle(nextBorderLine, nextCenter, nextRadius, 0.45f);
        }

        private void DrawCircle(LineRenderer line, Vector3 center, float radius, float yPos)
        {
            if (line == null) return;
            int count = line.positionCount;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;
                line.SetPosition(i, new Vector3(x, yPos, z));
            }
        }

        #region Consultas para la Inteligencia Artificial de los Jugadores

        /// <summary>
        /// Comprueba si una posición está dentro de la zona segura actual
        /// </summary>
        public bool IsInsideSafeZone(Vector3 position)
        {
            Vector2 pos2D = new Vector2(position.x, position.z);
            Vector2 center2D = new Vector2(currentCenter.x, currentCenter.z);
            return Vector2.Distance(pos2D, center2D) <= currentRadius;
        }

        /// <summary>
        /// Distancia al borde de la tormenta. Positivo si está adentro, negativo si ya está en la tormenta.
        /// </summary>
        public float GetDistanceToStormEdge(Vector3 position)
        {
            Vector2 pos2D = new Vector2(position.x, position.z);
            Vector2 center2D = new Vector2(currentCenter.x, currentCenter.z);
            float distToCenter = Vector2.Distance(pos2D, center2D);
            return currentRadius - distToCenter;
        }

        /// <summary>
        /// Requisito clave del usuario: Verifica si el jugador está en peligro inminente de tormenta
        /// (muy cerca del borde o afuera).
        /// </summary>
        public bool IsNearStormHazard(Vector3 position, float safetyMargin = 14f)
        {
            return GetDistanceToStormEdge(position) < safetyMargin;
        }

        /// <summary>
        /// Vector unitario de dirección hacia el centro de la zona segura
        /// </summary>
        public Vector3 GetDirectionToSafeZone(Vector3 position)
        {
            Vector3 dir = (nextCenter - position);
            dir.y = 0;
            if (dir.sqrMagnitude < 0.001f) return Vector3.forward;
            return dir.normalized;
        }

        #endregion

        public void ResetEntity()
        {
            InitializePhases();
            SetupInitialState(Vector3.zero, InitialMapRadius);
        }
    }
}

// Con apoyo de Gemini
