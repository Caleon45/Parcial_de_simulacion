using System.Collections.Generic;
using UnityEngine;

namespace BattleRoyaleSim
{
    [System.Serializable]
    public struct HotspotZone
    {
        public string zoneName;
        public Vector3 center;
        public float radius;
        public Color zoneColor;

        public HotspotZone(string name, Vector3 pos, float rad, Color col)
        {
            zoneName = name;
            center = pos;
            radius = rad;
            zoneColor = col;
        }
    }

    // Gestor del mapa y zonas
    public class MapEnvironment : MonoBehaviour
    {
        public static MapEnvironment Instance { get; private set; }

        [Header("Dimensiones del Mapa")]
        public float mapSize = 180f;

        [Header("Puntos de Interés (Hotspots con mejor botín)")]
        public List<HotspotZone> hotspots = new List<HotspotZone>();

        [Header("Contenedores de Elementos")]
        private Transform obstaclesParent;
        private Transform chestsParent;
        private Transform floorLootParent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            DefineHotspots();
        }

        private void DefineHotspots()
        {
            hotspots.Clear();
            hotspots.Add(new HotspotZone("Santuario Central", Vector3.zero, 25f, new Color(0.95f, 0.75f, 0.15f)));
            hotspots.Add(new HotspotZone("Campamento Militar", new Vector3(45f, 0, 45f), 20f, new Color(0.2f, 0.7f, 0.3f)));
            hotspots.Add(new HotspotZone("Ruinas Olvidadas", new Vector3(-45f, 0, -45f), 22f, new Color(0.35f, 0.6f, 0.9f)));
            hotspots.Add(new HotspotZone("Puesto de Avanzada", new Vector3(-45f, 0, 45f), 18f, new Color(0.85f, 0.4f, 0.8f)));
        }

        /// <summary>
        /// Genera el terreno, obstáculos y botín inicial.
        /// </summary>
        public void BuildCompleteMap()
        {
            DefineHotspots();

            // 1. Suelo del mapa
            CreateGround();

            // 2. Muros perimetrales
            CreatePerimeterWalls();

            // 3. Crear Zonas / Plataformas de Puntos de Interés
            CreateHotspotPlatforms();

            // 4. Obstáculos tácticos (cobertura: columnas, rocas, muros)
            CreateObstacles();

            // 5. Generar Cofres de botín
            SpawnChests();

            // 6. Generar Botín de piso disperso
            SpawnFloorLoot();
        }

        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Map_Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(mapSize / 10f, 1f, mapSize / 10f);

            MeshRenderer ren = ground.GetComponent<MeshRenderer>();
            ren.material = MeshBuilder.CreateColoredMaterial(new Color(0.18f, 0.22f, 0.25f), 0.3f, false);
        }

        private void CreatePerimeterWalls()
        {
            GameObject wallsRoot = new GameObject("PerimeterWalls");
            wallsRoot.transform.SetParent(transform, false);

            float half = mapSize * 0.5f;
            float wallH = 12f;
            float thickness = 2f;

            CreateWall(wallsRoot.transform, new Vector3(0, wallH * 0.5f, half), new Vector3(mapSize, wallH, thickness));
            CreateWall(wallsRoot.transform, new Vector3(0, wallH * 0.5f, -half), new Vector3(mapSize, wallH, thickness));
            CreateWall(wallsRoot.transform, new Vector3(half, wallH * 0.5f, 0), new Vector3(thickness, wallH, mapSize));
            CreateWall(wallsRoot.transform, new Vector3(-half, wallH * 0.5f, 0), new Vector3(thickness, wallH, mapSize));
        }

        private void CreateWall(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            wall.transform.localScale = size;

            MeshRenderer ren = wall.GetComponent<MeshRenderer>();
            ren.material = MeshBuilder.CreateColoredMaterial(new Color(0.12f, 0.15f, 0.18f), 0.2f, false);
        }

        private void CreateHotspotPlatforms()
        {
            GameObject poiRoot = new GameObject("Hotspots_Visuals");
            poiRoot.transform.SetParent(transform, false);

            for (int i = 0; i < hotspots.Count; i++)
            {
                HotspotZone hz = hotspots[i];

                // Plataforma circular o cuadrada decorativa
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                platform.name = $"Platform_{hz.zoneName}";
                platform.transform.SetParent(poiRoot.transform, false);
                platform.transform.position = hz.center + Vector3.up * 0.05f;
                platform.transform.localScale = new Vector3(hz.radius * 2f, 0.1f, hz.radius * 2f);
                Destroy(platform.GetComponent<Collider>());

                MeshRenderer ren = platform.GetComponent<MeshRenderer>();
                Color col = hz.zoneColor;
                col.a = 0.45f;
                ren.material = MeshBuilder.CreateColoredMaterial(col, 0.5f, true);

                // Pilares esquineros
                for (int p = 0; p < 4; p++)
                {
                    float angle = p * 90f * Mathf.Deg2Rad;
                    Vector3 pillarPos = hz.center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (hz.radius * 0.85f);
                    GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pillar.name = "Pillar";
                    pillar.transform.SetParent(poiRoot.transform, false);
                    pillar.transform.position = pillarPos + Vector3.up * 2.5f;
                    pillar.transform.localScale = new Vector3(1.5f, 2.5f, 1.5f);

                    MeshRenderer pRen = pillar.GetComponent<MeshRenderer>();
                    pRen.material = MeshBuilder.CreateColoredMaterial(new Color(0.28f, 0.32f, 0.38f), 0.4f, false);
                }
            }
        }

        private void CreateObstacles()
        {
            if (obstaclesParent != null) Destroy(obstaclesParent.gameObject);
            obstaclesParent = new GameObject("Obstacles").transform;
            obstaclesParent.SetParent(transform, false);

            int obstacleCount = 45;
            for (int i = 0; i < obstacleCount; i++)
            {
                Vector2 rPos = Random.insideUnitCircle * (mapSize * 0.42f);
                Vector3 pos = new Vector3(rPos.x, 0, rPos.y);

                // Evitar solapar exactamente el centro de un hotspot
                bool insideHotspotCenter = false;
                for (int h = 0; h < hotspots.Count; h++)
                {
                    if (Vector3.Distance(pos, hotspots[h].center) < 7f)
                    {
                        insideHotspotCenter = true;
                        break;
                    }
                }
                if (insideHotspotCenter) continue;

                // Alternar entre columnas, cubos de cobertura o rocas
                PrimitiveType pType = (i % 2 == 0) ? PrimitiveType.Cube : PrimitiveType.Cylinder;
                GameObject obs = GameObject.CreatePrimitive(pType);
                obs.name = $"Obstacle_{i}";
                obs.transform.SetParent(obstaclesParent, false);

                float height = Random.Range(2.5f, 5.0f);
                float width = Random.Range(1.8f, 3.5f);
                obs.transform.position = pos + Vector3.up * (height * 0.5f);
                obs.transform.localScale = new Vector3(width, height, width);
                obs.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

                MeshRenderer ren = obs.GetComponent<MeshRenderer>();
                Color obsColor = Color.Lerp(new Color(0.22f, 0.26f, 0.3f), new Color(0.35f, 0.4f, 0.45f), Random.value);
                ren.material = MeshBuilder.CreateColoredMaterial(obsColor, 0.3f, false);
            }
        }

        private void SpawnChests()
        {
            if (chestsParent != null) Destroy(chestsParent.gameObject);
            chestsParent = new GameObject("Chests_Group").transform;
            chestsParent.SetParent(transform, false);

            int chestIdCounter = 1;

            // 1. Cofres garantizados en cada Hotspot (mayor concentración)
            for (int h = 0; h < hotspots.Count; h++)
            {
                HotspotZone hz = hotspots[h];
                int chestsInZone = (h == 0) ? 5 : 3; // Santuario Central tiene 5 cofres dorados

                for (int c = 0; c < chestsInZone; c++)
                {
                    Vector2 offset2D = Random.insideUnitCircle * (hz.radius * 0.65f);
                    Vector3 chestPos = hz.center + new Vector3(offset2D.x, 0, offset2D.y);

                    GameObject chestObj = new GameObject($"Chest_{hz.zoneName}_{c + 1}");
                    chestObj.transform.SetParent(chestsParent, false);

                    LootChest chest = chestObj.AddComponent<LootChest>();
                    chest.Initialize($"Cofre_{chestIdCounter++}", chestPos, true);
                }
            }

            // 2. Cofres dispersos por los campos del mapa
            int fieldChests = 10;
            for (int i = 0; i < fieldChests; i++)
            {
                Vector2 rPos = Random.insideUnitCircle * (mapSize * 0.38f);
                Vector3 chestPos = new Vector3(rPos.x, 0, rPos.y);

                GameObject chestObj = new GameObject($"Chest_Field_{i + 1}");
                chestObj.transform.SetParent(chestsParent, false);

                LootChest chest = chestObj.AddComponent<LootChest>();
                chest.Initialize($"Cofre_{chestIdCounter++}", chestPos, false);
            }
        }

        private void SpawnFloorLoot()
        {
            if (floorLootParent != null) Destroy(floorLootParent.gameObject);
            floorLootParent = new GameObject("FloorLoot_Group").transform;
            floorLootParent.SetParent(transform, false);

            int itemCount = 26;
            for (int i = 0; i < itemCount; i++)
            {
                Vector2 rPos = Random.insideUnitCircle * (mapSize * 0.42f);
                Vector3 pos = new Vector3(rPos.x, 0, rPos.y);

                GameObject lootObj = new GameObject($"FloorLoot_{i + 1}");
                lootObj.transform.SetParent(floorLootParent, false);
                LootItem item = lootObj.AddComponent<LootItem>();

                // 70% armas triangulares de suelo, 30% consumibles
                if (Random.value < 0.7f)
                {
                    LootRarity rarity = WeaponStats.RollFloorRarity();
                    WeaponStats stats = new WeaponStats(rarity);
                    item.InitializeWeapon(stats, pos);
                }
                else
                {
                    LootType consType = Random.value < 0.5f ? LootType.Medkit : LootType.ShieldPotion;
                    item.InitializeConsumable(consType, pos);
                }
            }
        }

        /// <summary>
        /// Genera 10 posiciones de spawn equidistantes en un anillo exterior
        /// para que los 10 jugadores aterricen y busquen botín.
        /// </summary>
        public List<Vector3> GetPlayerSpawnPositions(int count = 10)
        {
            List<Vector3> spawns = new List<Vector3>();
            float spawnRadius = mapSize * 0.38f;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = (i * angleStep + Random.Range(-10f, 10f)) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * spawnRadius;
                float z = Mathf.Sin(angle) * spawnRadius;
                spawns.Add(new Vector3(x, 1f, z));
            }

            return spawns;
        }
    }
}

// Con apoyo de Gemini
