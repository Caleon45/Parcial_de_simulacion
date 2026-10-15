using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyaleSim
{
    public enum PlayerAIState
    {
        Spawning,           // Aterrizando al mapa
        Looting,            // Buscando botín activamente (Prioridad inicial absoluta)
        FleeingStorm,       // Huyendo hacia la zona segura para evitar daño
        EvaluatingEnemy,    // Analizando posición geográfica y conveniencia de combate
        Combat,             // En combate activo contra un enemigo
        ThirdPartying,      // Emboscando a dos enemigos que ya están peleando
        InAlliance,         // Cooperando temporalmente con otro jugador
        Dead                // Eliminado de la partida
    }

    /// <summary>
    /// Entidad de Jugador (Entidad Simulable requerida #1).
    /// Implementa ISimulatable. Toda su lógica de IA, combate, alianzas, percepción
    /// y supervivencia es orquestada desde Simulate.cs.
    /// </summary>
    public class PlayerAgent : MonoBehaviour, ISimulatable
    {
        public static List<PlayerAgent> ActivePlayers = new List<PlayerAgent>();

        [Header("Identidad y Atributos")]
        public string playerName = "Jugador";
        public Color playerColor = Color.white;
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float maxShield = 50f;
        public float currentShield = 0f;
        public float moveSpeed = 6.5f;

        [Header("Estado de Inteligencia Artificial")]
        public PlayerAIState currentState = PlayerAIState.Spawning;
        public string currentBehaviorDescription = "Iniciando...";

        [Header("Equipamiento")]
        public WeaponStats equippedWeapon = null; // Al inicio NO TIENE NADA (cumple especificación)
        private GameObject weaponVisualInstance = null;

        [Header("Sistema de Alianzas")]
        public PlayerAgent currentAlly = null;
        public float allianceTimer = 0f;
        public float maxAllianceDuration = 35f;

        [Header("Combate y Oportunismo")]
        public PlayerAgent currentTarget = null;
        private float attackTimer = 0f;
        private float fleeTimer = 0f;
        private Vector3 fleeDirection = Vector3.zero;

        [Header("Percepción")]
        public float visionRange = 22f;
        public float hearingRange = 30f;

        [Header("Estadísticas de Partida")]
        public int killsCount = 0;
        public int damageDealt = 0;
        public float survivalTime = 0f;

        // Referencias internas
        private CharacterController charController;
        private MeshRenderer bodyRenderer;
        private Vector3 moveVelocity;

        public string EntityName => playerName;
        public bool IsActive => gameObject.activeInHierarchy && currentState != PlayerAIState.Dead;
        public bool IsDead => currentState == PlayerAIState.Dead;
        public bool HasWeapon => equippedWeapon != null;

        private void Awake()
        {
            charController = GetComponent<CharacterController>();
            if (charController == null)
            {
                charController = gameObject.AddComponent<CharacterController>();
                charController.radius = 0.5f;
                charController.height = 1.8f;
                charController.center = new Vector3(0, 0.9f, 0);
            }
        }

        private void OnEnable()
        {
            if (!ActivePlayers.Contains(this))
                ActivePlayers.Add(this);

            if (BattleRoyaleSim.Simulate.Instance != null)
                BattleRoyaleSim.Simulate.Instance.RegisterEntity(this);
        }

        private void OnDisable()
        {
            ActivePlayers.Remove(this);
            if (BattleRoyaleSim.Simulate.Instance != null)
                BattleRoyaleSim.Simulate.Instance.UnregisterEntity(this);
        }

        public void Initialize(string name, Color color, Vector3 spawnPosition)
        {
            playerName = name;
            playerColor = color;
            transform.position = spawnPosition;

            currentHealth = maxHealth;
            currentShield = 0f;
            equippedWeapon = null;
            killsCount = 0;
            damageDealt = 0;
            survivalTime = 0f;
            currentAlly = null;
            currentTarget = null;

            BuildPlayerVisuals();
            UpdateWeaponVisual();

            currentState = PlayerAIState.Looting;
            currentBehaviorDescription = "Buscando botín inicial...";
        }

        private void BuildPlayerVisuals()
        {
            // Cápsula o cilindro estilizado
            Transform visual = transform.Find("BodyVisual");
            if (visual == null)
            {
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "BodyVisual";
                body.transform.SetParent(transform, false);
                body.transform.localPosition = new Vector3(0, 0.9f, 0);
                body.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                Destroy(body.GetComponent<Collider>());

                bodyRenderer = body.GetComponent<MeshRenderer>();
                bodyRenderer.material = MeshBuilder.CreateColoredMaterial(playerColor, 0.6f, false);
            }
        }

        /// <summary>
        /// Actualiza la figura 3D visible del arma triangular equipada y su color de rareza
        /// </summary>
        public void UpdateWeaponVisual()
        {
            if (weaponVisualInstance != null)
            {
                Destroy(weaponVisualInstance);
                weaponVisualInstance = null;
            }

            if (equippedWeapon != null)
            {
                // Instanciar Prisma Triangular 3D flotando sobre su hombro derecho
                weaponVisualInstance = new GameObject("Equipped_WeaponTriangle");
                weaponVisualInstance.transform.SetParent(transform, false);
                weaponVisualInstance.transform.localPosition = new Vector3(0.55f, 1.35f, 0.35f);
                weaponVisualInstance.transform.localRotation = Quaternion.Euler(0, 45f, 25f);
                weaponVisualInstance.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

                MeshFilter filter = weaponVisualInstance.AddComponent<MeshFilter>();
                filter.mesh = MeshBuilder.GetTrianglePrismMesh();

                MeshRenderer ren = weaponVisualInstance.AddComponent<MeshRenderer>();
                ren.material = MeshBuilder.CreateColoredMaterial(equippedWeapon.rarityColor, 0.7f, true);
            }
        }

        /// <summary>
        /// CICLO DE SIMULACIÓN CENTRALIZADO: Llamado exclusivamente desde Simulate.cs
        /// </summary>
        public void Simulate(float deltaTime)
        {
            if (IsDead) return;

            survivalTime += deltaTime;
            if (attackTimer > 0f) attackTimer -= deltaTime;
            if (fleeTimer > 0f) fleeTimer -= deltaTime;

            // Manejo de alianzas
            SimulateAlliance(deltaTime);

            // TOMA DE DECISIONES DE INTELIGENCIA ARTIFICIAL
            MakeAIDecision(deltaTime);

            // APLICAR FÍSICA Y MOVIMIENTO
            ApplyMovement(deltaTime);
        }

        private void SimulateAlliance(float deltaTime)
        {
            if (currentAlly != null)
            {
                allianceTimer += deltaTime;

                // CONDICIONES DE RUPTURA / TRAICIÓN DE ALIANZA:
                // 1. Duración agotada
                // 2. Círculo de la tormenta muy reducido (fase avanzada: solo puede quedar uno!)
                // 3. El aliado está críticamente herido y la tentación de eliminarlo surge
                bool stormTooSmall = StormController.Instance != null && StormController.Instance.currentRadius < 25f;
                bool allyLowHealth = currentAlly.currentHealth < 25f && currentHealth > 50f;
                bool timerExpired = allianceTimer >= maxAllianceDuration;

                if (stormTooSmall || allyLowHealth || timerExpired || currentAlly.IsDead)
                {
                    BreakAlliance(allyLowHealth ? "por oportunidad de eliminación" : "el espacio se agota");
                }
            }
        }

        public void FormAllianceWith(PlayerAgent partner)
        {
            if (partner == null || partner == this || partner.IsDead) return;

            currentAlly = partner;
            partner.currentAlly = this;
            allianceTimer = 0f;
            partner.allianceTimer = 0f;

            if (BattleRoyaleSim.Simulate.Instance != null)
            {
                BattleRoyaleSim.Simulate.Instance.LogEvent($"¡Pacto temporal! {playerName} y {partner.playerName} formaron una alianza.");
            }
        }

        public void BreakAlliance(string reason)
        {
            if (currentAlly != null)
            {
                string allyName = currentAlly.playerName;
                currentAlly.currentAlly = null;
                currentAlly = null;

                if (BattleRoyaleSim.Simulate.Instance != null)
                {
                    BattleRoyaleSim.Simulate.Instance.LogEvent($"¡Alianza rota! {playerName} rompió la alianza con {allyName} ({reason}).");
                }
            }
        }

        #region Inteligencia Artificial y Toma de Decisiones

        private void MakeAIDecision(float deltaTime)
        {
            // Huir a la zona segura si esta cerca del borde de la tormenta
            bool inStormHazard = StormController.Instance != null && StormController.Instance.IsNearStormHazard(transform.position, 12f);
            if (inStormHazard)
            {
                currentState = PlayerAIState.FleeingStorm;
                currentBehaviorDescription = "Huyendo de la tormenta";
                Vector3 safeDir = StormController.Instance.GetDirectionToSafeZone(transform.position);
                MoveTowards(transform.position + safeDir * 10f, 1.15f);
                return;
            }

            // Prioridad inicial: buscar botin si no tiene arma
            if (!HasWeapon)
            {
                currentState = PlayerAIState.Looting;
                currentBehaviorDescription = "Buscando botin";
                ExecuteLootingBehavior();
                return;
            }

            ScanEnvironmentAndAct();
        }

        private void ScanEnvironmentAndAct()
        {
            // Oportunismo (third-party) si hay dos enemigos peleando
            PlayerAgent thirdPartyTarget = DetectThirdPartyOpportunity();
            if (thirdPartyTarget != null)
            {
                currentState = PlayerAIState.ThirdPartying;
                currentTarget = thirdPartyTarget;
                currentBehaviorDescription = $"Tercereando a {currentTarget.playerName}";
                ExecuteCombatBehavior(currentTarget);
                return;
            }

            // 2. Detectar enemigo más cercano
            PlayerAgent nearestEnemy = GetNearestEnemy();

            if (nearestEnemy != null)
            {
                // Evaluar posición del enemigo y conveniencia de la pelea
                // "Evitan las peleas que no les convienen"
                bool shouldEngage = EvaluateCombatAdvantage(nearestEnemy);

                // Posibilidad de alianza si ambos no tienen ventaja abrumadora y están cerca
                if (!shouldEngage && currentAlly == null && nearestEnemy.currentAlly == null && Random.value < 0.25f && nearestEnemy.HasWeapon)
                {
                    FormAllianceWith(nearestEnemy);
                    currentState = PlayerAIState.InAlliance;
                    currentBehaviorDescription = $"En alianza con {nearestEnemy.playerName}";
                    return;
                }

                if (shouldEngage)
                {
                    currentState = PlayerAIState.Combat;
                    currentTarget = nearestEnemy;
                    currentBehaviorDescription = $"Combatiendo contra {currentTarget.playerName} con {equippedWeapon.weaponName}";
                    ExecuteCombatBehavior(currentTarget);
                }
                else
                {
                    // Pelea no conveniente: HUIR y buscar mejor botín o reposicionarse
                    currentState = PlayerAIState.EvaluatingEnemy;
                    currentBehaviorDescription = $"Evitando pelea desfavorable contra {nearestEnemy.playerName}! Reubicándose...";
                    Vector3 awayDir = (transform.position - nearestEnemy.transform.position).normalized;
                    awayDir.y = 0;
                    MoveTowards(transform.position + awayDir * 15f, 1.1f);
                }
                return;
            }

            // Si no hay enemigos a la vista, continuar looteando mejores armas o dirigirse al centro seguro
            if (equippedWeapon.rarity < LootRarity.Legendary)
            {
                currentState = PlayerAIState.Looting;
                currentBehaviorDescription = "Explorando en busca de mejor botín (mejor rareza)...";
                ExecuteLootingBehavior();
            }
            else
            {
                // Ya tiene botín legendario: patrullar centro de zona segura
                currentState = PlayerAIState.EvaluatingEnemy;
                currentBehaviorDescription = "Armado al máximo. Patrullando zona segura...";
                if (StormController.Instance != null)
                {
                    MoveTowards(StormController.Instance.nextCenter, 0.75f);
                }
            }
        }

        private bool EvaluateCombatAdvantage(PlayerAgent enemy)
        {
            if (enemy == null) return false;

            // Si el jugador está cerca de la tormenta, NUNCA le conviene pelear aquí
            if (StormController.Instance != null && StormController.Instance.IsNearStormHazard(transform.position, 14f))
            {
                return false;
            }

            // Comparar poder de fuego (rareza del arma triangular) y vida
            int myTier = (int)equippedWeapon.rarity;
            int enemyTier = enemy.HasWeapon ? (int)enemy.equippedWeapon.rarity : -1;

            float myPower = (currentHealth + currentShield) * (1f + myTier * 0.35f);
            float enemyPower = (enemy.currentHealth + enemy.currentShield) * (1f + enemyTier * 0.35f);

            // Si la ventaja es decente o enemiga es desarmada, atacar; si el enemigo es muy superior, retirarse
            return myPower >= (enemyPower * 0.75f);
        }

        private PlayerAgent DetectThirdPartyOpportunity()
        {
            // Revisa si hay enemigos en combate entre sí dentro del rango de audición/visión
            for (int i = 0; i < ActivePlayers.Count; i++)
            {
                PlayerAgent p1 = ActivePlayers[i];
                if (p1 == this || p1 == currentAlly || p1.IsDead) continue;

                float dist = Vector3.Distance(transform.position, p1.transform.position);
                if (dist <= visionRange && p1.currentState == PlayerAIState.Combat && p1.currentTarget != null)
                {
                    // Si p1 está combatiendo contra otro objetivo p2 que no soy yo, aprovecha la oportunidad!
                    PlayerAgent p2 = p1.currentTarget;
                    if (p2 != this && p2 != currentAlly && !p2.IsDead)
                    {
                        // Seleccionar al que tenga menos vida para asegurar la eliminación
                        return p1.currentHealth <= p2.currentHealth ? p1 : p2;
                    }
                }
            }
            return null;
        }

        private PlayerAgent GetNearestEnemy()
        {
            PlayerAgent bestTarget = null;
            float closestDist = visionRange;

            for (int i = 0; i < ActivePlayers.Count; i++)
            {
                PlayerAgent other = ActivePlayers[i];
                if (other == this || other == currentAlly || other.IsDead) continue;

                float dist = Vector3.Distance(transform.position, other.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = other;
                }
            }
            return bestTarget;
        }

        private void ExecuteLootingBehavior()
        {
            // 1. Buscar armas o consumibles en el piso
            LootItem nearestItem = GetNearestLootItem();
            if (nearestItem != null)
            {
                float dist = Vector3.Distance(transform.position, nearestItem.transform.position);
                if (dist <= 1.4f)
                {
                    CollectLootItem(nearestItem);
                    return;
                }
                MoveTowards(nearestItem.transform.position, 1.0f);
                return;
            }

            // 2. Buscar cofres cerrados
            LootChest nearestChest = GetNearestClosedChest();
            if (nearestChest != null)
            {
                float dist = Vector3.Distance(transform.position, nearestChest.transform.position);
                if (dist <= 1.6f)
                {
                    if (nearestChest.currentState == ChestState.Closed)
                    {
                        nearestChest.StartOpening(this);
                        currentBehaviorDescription = $"Abriendo {nearestChest.EntityName}...";
                    }
                    return;
                }
                MoveTowards(nearestChest.transform.position, 1.0f);
                return;
            }

            // 3. Si no hay ítems ni cofres a la vista, explorar hacia el centro de la zona segura
            if (StormController.Instance != null)
            {
                MoveTowards(StormController.Instance.nextCenter, 0.85f);
            }
        }

        private LootItem GetNearestLootItem()
        {
            LootItem best = null;
            float minDist = visionRange * 1.5f;

            for (int i = 0; i < LootItem.ActiveLootItems.Count; i++)
            {
                LootItem item = LootItem.ActiveLootItems[i];
                if (item == null || !item.IsActive) continue;

                // Solo recoger armas si no tiene una o si la del piso es de mayor rareza
                if (item.lootType == LootType.WeaponTriangle && item.weaponStats != null)
                {
                    if (equippedWeapon != null && item.weaponStats.rarity <= equippedWeapon.rarity)
                        continue;
                }

                float dist = Vector3.Distance(transform.position, item.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    best = item;
                }
            }
            return best;
        }

        private LootChest GetNearestClosedChest()
        {
            LootChest best = null;
            float minDist = visionRange * 1.8f;

            for (int i = 0; i < LootChest.ActiveChests.Count; i++)
            {
                LootChest chest = LootChest.ActiveChests[i];
                if (chest == null || !chest.CanBeOpened) continue;

                float dist = Vector3.Distance(transform.position, chest.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    best = chest;
                }
            }
            return best;
        }

        private void CollectLootItem(LootItem item)
        {
            if (item == null) return;

            if (item.lootType == LootType.WeaponTriangle && item.weaponStats != null)
            {
                LootRarity oldRarity = equippedWeapon != null ? equippedWeapon.rarity : LootRarity.Common;
                equippedWeapon = item.weaponStats;
                UpdateWeaponVisual();

                if (BattleRoyaleSim.Simulate.Instance != null)
                {
                    BattleRoyaleSim.Simulate.Instance.LogEvent($"{playerName} equipó un {equippedWeapon.weaponName} [{equippedWeapon.rarity}]!");
                }
            }
            else if (item.lootType == LootType.Medkit)
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + item.healAmount);
                if (BattleRoyaleSim.Simulate.Instance != null)
                {
                    BattleRoyaleSim.Simulate.Instance.LogEvent($"{playerName} usó un Botiquín (+{item.healAmount:F0} Salud).");
                }
            }
            else if (item.lootType == LootType.ShieldPotion)
            {
                currentShield = Mathf.Min(maxShield, currentShield + 25f);
                if (BattleRoyaleSim.Simulate.Instance != null)
                {
                    BattleRoyaleSim.Simulate.Instance.LogEvent($"{playerName} consumió Poción de Escudo (+25 Escudo).");
                }
            }

            item.OnCollectedBy(this);
        }

        private void ExecuteCombatBehavior(PlayerAgent target)
        {
            if (target == null || target.IsDead)
            {
                currentTarget = null;
                return;
            }

            float dist = Vector3.Distance(transform.position, target.transform.position);
            float combatRange = equippedWeapon != null ? equippedWeapon.range : 2f;

            // Mantener distancia óptima
            if (dist > combatRange * 0.85f)
            {
                MoveTowards(target.transform.position, 1.05f);
            }
            else if (dist < combatRange * 0.4f)
            {
                // Muy cerca: dar pasos laterales o hacia atrás para mantener ventaja de rango
                Vector3 retreatDir = (transform.position - target.transform.position).normalized;
                retreatDir.y = 0;
                MoveTowards(transform.position + retreatDir * 5f, 0.9f);
            }
            else
            {
                // Órbita / Strafing de combate alrededor del objetivo
                Vector3 strafeDir = Vector3.Cross(Vector3.up, (target.transform.position - transform.position).normalized);
                MoveTowards(transform.position + strafeDir * 4f, 0.9f);
            }

            // Mirar hacia el objetivo
            Vector3 lookDir = (target.transform.position - transform.position);
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 12f);
            }

            // Atacar si el cooldown lo permite
            if (dist <= combatRange && attackTimer <= 0f && equippedWeapon != null)
            {
                attackTimer = equippedWeapon.attackCooldown;
                PerformAttack(target);
            }
        }

        private void PerformAttack(PlayerAgent target)
        {
            float damage = equippedWeapon.damage * Random.Range(0.9f, 1.15f);
            damageDealt += Mathf.RoundToInt(damage);

            // Crear efecto visual de disparo / rayo hacia el objetivo
            CreateAttackTracer(transform.position + Vector3.up * 1.2f, target.transform.position + Vector3.up * 1f, equippedWeapon.rarityColor);

            target.TakeDamage(damage, this, $"arma {equippedWeapon.weaponName}");
        }

        private void CreateAttackTracer(Vector3 from, Vector3 to, Color color)
        {
            GameObject tracerObj = new GameObject("Tracer");
            LineRenderer lr = tracerObj.AddComponent<LineRenderer>();
            lr.startWidth = 0.12f;
            lr.endWidth = 0.05f;
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.material = MeshBuilder.CreateColoredMaterial(color, 0.5f, true);
            Destroy(tracerObj, 0.18f);
        }

        #endregion

        #region Daño, Muerte y Desecho de Botín

        public void TakeDamage(float amount, PlayerAgent attacker, string sourceDesc)
        {
            if (IsDead) return;

            // Absorción por escudo primero
            if (currentShield > 0f)
            {
                if (currentShield >= amount)
                {
                    currentShield -= amount;
                    amount = 0f;
                }
                else
                {
                    amount -= currentShield;
                    currentShield = 0f;
                }
            }

            currentHealth -= amount;

            // Si es atacado mientras abría un cofre, reacciona
            for (int i = 0; i < LootChest.ActiveChests.Count; i++)
            {
                if (LootChest.ActiveChests[i].currentState == ChestState.Opening)
                {
                    LootChest.ActiveChests[i].CancelOpening();
                }
            }

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die(attacker, sourceDesc);
            }
        }

        private void Die(PlayerAgent killer, string cause)
        {
            currentState = PlayerAIState.Dead;
            currentBehaviorDescription = "Eliminado";

            if (currentAlly != null)
            {
                currentAlly.currentAlly = null;
                currentAlly = null;
            }

            // Registrar eliminación en el log global de la simulación
            if (BattleRoyaleSim.Simulate.Instance != null)
            {
                if (killer != null)
                {
                    killer.killsCount++;
                    BattleRoyaleSim.Simulate.Instance.LogEvent($"[K.O.] {killer.playerName} eliminó a {playerName} con {cause}!");
                }
                else
                {
                    BattleRoyaleSim.Simulate.Instance.LogEvent($"[K.O.] {playerName} fue eliminado por {cause}!");
                }
            }

            // ESPECIFICACIÓN: "Lo que un jugador suelta al morir queda para otros"
            DropLootOnDeath();

            // Desactivar visuales y collider
            if (weaponVisualInstance != null)
            {
                Destroy(weaponVisualInstance);
            }
            if (charController != null)
            {
                charController.enabled = false;
            }

            gameObject.SetActive(false);
        }

        private void DropLootOnDeath()
        {
            // Soltar el arma triangular equipada
            if (equippedWeapon != null)
            {
                GameObject dropObj = new GameObject($"Dropped_{equippedWeapon.weaponName}");
                LootItem item = dropObj.AddComponent<LootItem>();
                item.InitializeWeapon(equippedWeapon, transform.position + Vector3.up * 0.2f);
            }

            // Soltar consumible de recompensa
            GameObject medObj = new GameObject("Dropped_Medkit");
            LootItem med = medObj.AddComponent<LootItem>();
            med.InitializeConsumable(LootType.Medkit, transform.position + new Vector3(0.8f, 0.2f, 0.8f));
        }

        #endregion

        #region Movimiento y Navegación

        private void MoveTowards(Vector3 targetPos, float speedMultiplier = 1f)
        {
            Vector3 direction = (targetPos - transform.position);
            direction.y = 0;

            if (direction.sqrMagnitude > 0.05f)
            {
                direction.Normalize();

                // Evitación de obstáculos simple con Raycast
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, 2.0f))
                {
                    // Si choca con un muro o árbol, desviar 45 grados
                    direction = Quaternion.Euler(0, 45f, 0) * direction;
                }

                moveVelocity = direction * (moveSpeed * speedMultiplier);

                // Rotar hacia la dirección de movimiento
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
            else
            {
                moveVelocity = Vector3.zero;
            }
        }

        private void ApplyMovement(float deltaTime)
        {
            if (charController != null && charController.enabled)
            {
                // Gravedad básica para mantener al jugador en el suelo
                Vector3 motion = moveVelocity * deltaTime;
                motion.y -= 9.8f * deltaTime;
                charController.Move(motion);
            }
            else
            {
                transform.position += moveVelocity * deltaTime;
            }
        }

        #endregion

        public void ResetEntity()
        {
            if (charController != null) charController.enabled = true;
            gameObject.SetActive(true);
            currentHealth = maxHealth;
            currentShield = 0f;
            equippedWeapon = null;
            killsCount = 0;
            damageDealt = 0;
            survivalTime = 0f;
            currentAlly = null;
            currentTarget = null;
            currentState = PlayerAIState.Looting;
            UpdateWeaponVisual();
        }
    }
}

// Con apoyo de Gemini
