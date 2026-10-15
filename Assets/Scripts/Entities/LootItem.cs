using UnityEngine;

namespace BattleRoyaleSim
{
    // Entidad de Botin en el suelo
    public class LootItem : MonoBehaviour, ISimulatable
    {
        [Header("Datos de Botín")]
        public LootType lootType = LootType.WeaponTriangle;
        public WeaponStats weaponStats;
        public float healAmount = 35f;

        [Header("Comportamiento Visual")]
        public float bobSpeed = 3f;
        public float bobHeight = 0.15f;
        public float rotateSpeed = 90f;

        private Vector3 startPosition;
        private float animationTimer;
        private bool isPickedUp = false;
        private MeshRenderer meshRenderer;

        public string EntityName => weaponStats != null ? weaponStats.weaponName : lootType.ToString();
        public bool IsActive => !isPickedUp && gameObject.activeInHierarchy;

        public static System.Collections.Generic.List<LootItem> ActiveLootItems = new System.Collections.Generic.List<LootItem>();

        private void OnEnable()
        {
            if (!ActiveLootItems.Contains(this))
                ActiveLootItems.Add(this);

            if (BattleRoyaleSim.Simulate.Instance != null)
                BattleRoyaleSim.Simulate.Instance.RegisterEntity(this);
        }

        private void OnDisable()
        {
            ActiveLootItems.Remove(this);
            if (BattleRoyaleSim.Simulate.Instance != null)
                BattleRoyaleSim.Simulate.Instance.UnregisterEntity(this);
        }

        public void InitializeWeapon(WeaponStats stats, Vector3 spawnPos)
        {
            weaponStats = stats;
            lootType = LootType.WeaponTriangle;
            transform.position = spawnPos;
            startPosition = spawnPos + Vector3.up * 0.5f;

            SetupVisuals();
        }

        public void InitializeConsumable(LootType type, Vector3 spawnPos)
        {
            lootType = type;
            weaponStats = null;
            transform.position = spawnPos;
            startPosition = spawnPos + Vector3.up * 0.4f;

            SetupVisuals();
        }

        private void SetupVisuals()
        {
            // Configurar MeshFilter y MeshRenderer procedurales
            MeshFilter filter = GetComponent<MeshFilter>();
            if (filter == null) filter = gameObject.AddComponent<MeshFilter>();

            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            if (lootType == LootType.WeaponTriangle)
            {
                filter.mesh = MeshBuilder.GetTrianglePrismMesh();
                Color color = weaponStats != null ? weaponStats.rarityColor : Color.white;
                meshRenderer.material = MeshBuilder.CreateColoredMaterial(color, 0.7f, true);
            }
            else
            {
                filter.mesh = MeshBuilder.GetDiamondMesh();
                Color color = lootType == LootType.Medkit ? new Color(0.1f, 0.9f, 0.3f) : new Color(0.2f, 0.7f, 1f);
                meshRenderer.material = MeshBuilder.CreateColoredMaterial(color, 0.8f, true);
            }

            // Collider para interacción
            SphereCollider col = GetComponent<SphereCollider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.8f;
        }

        /// <summary>
        /// Ciclo de simulación orquestado por Simulate.cs
        /// </summary>
        public void Simulate(float deltaTime)
        {
            if (isPickedUp) return;

            animationTimer += deltaTime;

            // Animación suave de flotación y giro continuo
            float newY = startPosition.y + Mathf.Sin(animationTimer * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
            transform.Rotate(Vector3.up, rotateSpeed * deltaTime, Space.World);
        }

        /// <summary>
        /// Llamado cuando un jugador recoge este ítem
        /// </summary>
        public void OnCollectedBy(PlayerAgent player)
        {
            if (isPickedUp) return;
            isPickedUp = true;

            if (BattleRoyaleSim.Simulate.Instance != null)
                BattleRoyaleSim.Simulate.Instance.UnregisterEntity(this);

            Destroy(gameObject);
        }

        public void ResetEntity()
        {
            isPickedUp = false;
            animationTimer = 0f;
        }
    }
}

// Con apoyo de Gemini
