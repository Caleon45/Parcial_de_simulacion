using UnityEngine;

namespace BattleRoyaleSim
{
    public class PlayerWorldUI : MonoBehaviour
    {
        private PlayerAgent targetPlayer;
        private TextMesh nameTextMesh;
        private TextMesh statusTextMesh;

        public void Initialize(PlayerAgent player)
        {
            targetPlayer = player;
            CreateWorldLabels();
        }

        private void CreateWorldLabels()
        {
            // Nombre del jugador
            GameObject nameObj = new GameObject("PlayerNameTag");
            nameObj.transform.SetParent(transform, false);
            nameObj.transform.localPosition = new Vector3(0, 0.5f, 1.4f);
            nameObj.transform.rotation = Quaternion.Euler(90f, 0, 0);

            nameTextMesh = nameObj.AddComponent<TextMesh>();
            nameTextMesh.text = targetPlayer.playerName.Split(' ')[0];
            nameTextMesh.fontSize = 24;
            nameTextMesh.characterSize = 0.12f;
            nameTextMesh.color = targetPlayer.playerColor;
            nameTextMesh.alignment = TextAlignment.Center;
            nameTextMesh.anchor = TextAnchor.MiddleCenter;

            // Estado y arma
            GameObject statusObj = new GameObject("PlayerStatusTag");
            statusObj.transform.SetParent(transform, false);
            statusObj.transform.localPosition = new Vector3(0, 0.5f, -1.4f);
            statusObj.transform.rotation = Quaternion.Euler(90f, 0, 0);

            statusTextMesh = statusObj.AddComponent<TextMesh>();
            statusTextMesh.fontSize = 20;
            statusTextMesh.characterSize = 0.1f;
            statusTextMesh.color = Color.white;
            statusTextMesh.alignment = TextAlignment.Center;
            statusTextMesh.anchor = TextAnchor.MiddleCenter;
        }

        private void LateUpdate()
        {
            if (targetPlayer == null || targetPlayer.IsDead)
            {
                if (nameTextMesh != null) nameTextMesh.gameObject.SetActive(false);
                if (statusTextMesh != null) statusTextMesh.gameObject.SetActive(false);
                return;
            }

            // Actualizar etiqueta de vida y arma
            if (statusTextMesh != null)
            {
                string wepLabel = targetPlayer.HasWeapon ? $"▲ {targetPlayer.equippedWeapon.weaponName.Substring(10)}" : "Desarmado";
                statusTextMesh.text = $"HP: {targetPlayer.currentHealth:F0} | {wepLabel}";
            }
        }
    }
}

// Con apoyo de Gemini
