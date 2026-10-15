using UnityEngine;

namespace BattleRoyaleSim
{
    public enum LootType
    {
        WeaponTriangle, // Arma representada por un triángulo / prisma 3D
        Medkit,         // Botiquín de salud
        ShieldPotion    // Poción de escudo
    }

    public enum LootRarity
    {
        Common = 0,     // Gris / Plateado
        Uncommon = 1,   // Verde
        Rare = 2,       // Azul
        Epic = 3,       // Púrpura
        Legendary = 4   // Dorado
    }

    [System.Serializable]
    public class WeaponStats
    {
        public string weaponName;
        public LootRarity rarity;
        public float damage;
        public float range;
        public float attackCooldown;
        public Color rarityColor;

        public WeaponStats(LootRarity r)
        {
            rarity = r;
            switch (r)
            {
                case LootRarity.Common:
                    weaponName = "Triángulo Común";
                    damage = 12f;
                    range = 9f;
                    attackCooldown = 1.2f;
                    rarityColor = new Color(0.7f, 0.75f, 0.8f);
                    break;
                case LootRarity.Uncommon:
                    weaponName = "Triángulo Poco Común";
                    damage = 18f;
                    range = 11f;
                    attackCooldown = 1.0f;
                    rarityColor = new Color(0.2f, 0.85f, 0.35f);
                    break;
                case LootRarity.Rare:
                    weaponName = "Triángulo Raro";
                    damage = 26f;
                    range = 13f;
                    attackCooldown = 0.85f;
                    rarityColor = new Color(0.2f, 0.55f, 0.95f);
                    break;
                case LootRarity.Epic:
                    weaponName = "Triángulo Épico";
                    damage = 38f;
                    range = 15f;
                    attackCooldown = 0.7f;
                    rarityColor = new Color(0.68f, 0.32f, 0.95f);
                    break;
                case LootRarity.Legendary:
                default:
                    weaponName = "Triángulo Legendario";
                    damage = 52f;
                    range = 18f;
                    attackCooldown = 0.55f;
                    rarityColor = new Color(1.0f, 0.75f, 0.1f);
                    break;
            }
        }

        public static Color GetRarityColor(LootRarity rarity)
        {
            switch (rarity)
            {
                case LootRarity.Common: return new Color(0.75f, 0.75f, 0.75f);
                case LootRarity.Uncommon: return new Color(0.2f, 0.85f, 0.35f);
                case LootRarity.Rare: return new Color(0.2f, 0.55f, 0.95f);
                case LootRarity.Epic: return new Color(0.68f, 0.32f, 0.95f);
                case LootRarity.Legendary: return new Color(1.0f, 0.75f, 0.1f);
                default: return Color.white;
            }
        }

        public static LootRarity RollFloorRarity()
        {
            // Probabilidades botín de suelo: Común (50%), Poco Común (30%), Raro (14%), Épico (5%), Legendario (1%)
            float roll = Random.value * 100f;
            if (roll < 50f) return LootRarity.Common;
            if (roll < 80f) return LootRarity.Uncommon;
            if (roll < 94f) return LootRarity.Rare;
            if (roll < 99f) return LootRarity.Epic;
            return LootRarity.Legendary;
        }

        public static LootRarity RollChestRarity(bool isHotspot = false)
        {
            // Probabilidades cofre: Más chances de mejor botín
            // Normal: Común (15%), Poco Común (35%), Raro (30%), Épico (15%), Legendario (5%)
            // Hotspot: Poco Común (20%), Raro (40%), Épico (25%), Legendario (15%)
            float roll = Random.value * 100f;
            if (isHotspot)
            {
                if (roll < 20f) return LootRarity.Uncommon;
                if (roll < 60f) return LootRarity.Rare;
                if (roll < 85f) return LootRarity.Epic;
                return LootRarity.Legendary;
            }
            else
            {
                if (roll < 15f) return LootRarity.Common;
                if (roll < 50f) return LootRarity.Uncommon;
                if (roll < 80f) return LootRarity.Rare;
                if (roll < 95f) return LootRarity.Epic;
                return LootRarity.Legendary;
            }
        }
    }
}

// Con apoyo de Gemini
