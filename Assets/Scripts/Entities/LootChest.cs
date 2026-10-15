using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyaleSim
{
    public enum ChestState
    {
        Closed,
        Opening,
        Opened
    }

    // Entidad de Cofre de Botin
    public class LootChest : MonoBehaviour, ISimulatable
    {
        [Header("Configuración de Cofre")]
        public string chestId = "Cofre_01";
        public bool isHotspotChest = false;
        public ChestState currentState = ChestState.Closed;

        [Header("Tiempo de Apertura")]
        public float openDuration = 1.0f;
        private float currentOpenTimer = 0f;
        private PlayerAgent openingPlayer = null;

        [Header("Visuales")]
        private Transform lidTransform;
        private MeshRenderer boxRenderer;
        private MeshRenderer lidRenderer;
        private bool visualsBuilt = false;

        public static List<LootChest> ActiveChests = new List<LootChest>();

        public string EntityName => chestId;
        public bool IsActive => gameObject.activeInHierarchy;
        public bool CanBeOpened => currentState == ChestState.Closed;

        private void OnEnable()
        {
            if (!ActiveChests.Contains(this))
                ActiveChests.Add(this);

            if (BattleRoyaleSim.Simulate.Instance != null)
                BattleRoyaleSim.Simulate.Instance.RegisterEntity(this);
        }

        private void OnDisable()
        {
            ActiveChests.Remove(this);
            if (BattleRoyaleSim.Simulate.Instance != null)
                BattleRoyaleSim.Simulate.Instance.UnregisterEntity(this);
        }

        private void Awake()
        {
            BuildChestVisuals();
        }

        public void Initialize(string id, Vector3 position, bool hotspot)
        {
            chestId = id;
            isHotspotChest = hotspot;
            transform.position = position;
            BuildChestVisuals();
        }

        private void BuildChestVisuals()
        {
            if (visualsBuilt) return;
            visualsBuilt = true;

            // Base del cofre (Cubo)
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "Chest_Base";
            baseObj.transform.SetParent(transform, false);
            baseObj.transform.localPosition = new Vector3(0, 0.4f, 0);
            baseObj.transform.localScale = new Vector3(1.1f, 0.7f, 0.8f);

            // Eliminar collider predeterminado del hijo para no solapar
            Destroy(baseObj.GetComponent<Collider>());

            Color baseColor = isHotspotChest ? new Color(0.95f, 0.65f, 0.1f) : new Color(0.6f, 0.4f, 0.2f);
            boxRenderer = baseObj.GetComponent<MeshRenderer>();
            boxRenderer.material = MeshBuilder.CreateColoredMaterial(baseColor, 0.6f, isHotspotChest);

            // Tapa del cofre (Articulable)
            GameObject lidObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lidObj.name = "Chest_Lid";
            lidObj.transform.SetParent(transform, false);
            lidObj.transform.localPosition = new Vector3(0, 0.75f, 0.35f);
            lidObj.transform.localScale = new Vector3(1.15f, 0.25f, 0.85f);
            Destroy(lidObj.GetComponent<Collider>());

            Color lidColor = isHotspotChest ? new Color(1f, 0.8f, 0.2f) : new Color(0.75f, 0.5f, 0.25f);
            lidRenderer = lidObj.GetComponent<MeshRenderer>();
            lidRenderer.material = MeshBuilder.CreateColoredMaterial(lidColor, 0.7f, isHotspotChest);
            lidTransform = lidObj.transform;

            // Collider principal de interacción
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.5f, 0);
            col.size = new Vector3(1.6f, 1.2f, 1.6f);
            col.isTrigger = true;
        }

        /// <summary>
        /// Comienza el proceso de apertura iniciado por un PlayerAgent
        /// </summary>
        public bool StartOpening(PlayerAgent player)
        {
            if (currentState != ChestState.Closed) return false;

            currentState = ChestState.Opening;
            openingPlayer = player;
            currentOpenTimer = 0f;
            return true;
        }

        /// <summary>
        /// Cancela la apertura si el jugador huye o es atacado
        /// </summary>
        public void CancelOpening()
        {
            if (currentState == ChestState.Opening)
            {
                currentState = ChestState.Closed;
                openingPlayer = null;
                currentOpenTimer = 0f;
            }
        }

        /// <summary>
        /// Ciclo de simulación orquestado por Simulate.cs
        /// </summary>
        public void Simulate(float deltaTime)
        {
            if (currentState == ChestState.Opening)
            {
                currentOpenTimer += deltaTime;

                // Animar tapa abriéndose progresivamente
                if (lidTransform != null)
                {
                    float angle = Mathf.Lerp(0f, -65f, currentOpenTimer / openDuration);
                    lidTransform.localRotation = Quaternion.Euler(angle, 0, 0);
                }

                if (currentOpenTimer >= openDuration)
                {
                    CompleteOpening();
                }
            }
        }

        private void CompleteOpening()
        {
            currentState = ChestState.Opened;

            if (BattleRoyaleSim.Simulate.Instance != null && openingPlayer != null)
            {
                BattleRoyaleSim.Simulate.Instance.LogEvent($"{openingPlayer.EntityName} abrió un {(isHotspotChest ? "Cofre de Zona Caliente" : "Cofre común")}!");
            }

            SpawnLoot();
            openingPlayer = null;
        }

        private void SpawnLoot()
        {
            // Spawn de 1 a 2 armas triangulares y posiblemente poción
            int weaponCount = isHotspotChest ? 2 : 1;
            for (int i = 0; i < weaponCount; i++)
            {
                LootRarity rarity = WeaponStats.RollChestRarity(isHotspotChest);
                WeaponStats stats = new WeaponStats(rarity);

                Vector3 offset = new Vector3(Random.Range(-1.2f, 1.2f), 0, Random.Range(-1.2f, 1.2f));
                Vector3 spawnPos = transform.position + offset;

                GameObject lootObj = new GameObject($"Loot_{stats.weaponName}");
                LootItem item = lootObj.AddComponent<LootItem>();
                item.InitializeWeapon(stats, spawnPos);
            }

            // Probabilidad de soltar poción o botiquín
            if (Random.value < 0.6f)
            {
                LootType consumable = Random.value < 0.5f ? LootType.Medkit : LootType.ShieldPotion;
                Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                GameObject consObj = new GameObject($"Loot_{consumable}");
                LootItem item = consObj.AddComponent<LootItem>();
                item.InitializeConsumable(consumable, transform.position + offset);
            }
        }

        public void ResetEntity()
        {
            currentState = ChestState.Closed;
            currentOpenTimer = 0f;
            openingPlayer = null;
            if (lidTransform != null)
            {
                lidTransform.localRotation = Quaternion.identity;
            }
        }
    }
}

// Con apoyo de Gemini
