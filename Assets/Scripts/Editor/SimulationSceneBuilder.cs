#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BattleRoyaleSim.Editor
{
    public static class SimulationSceneBuilder
    {
        [MenuItem("Simulación/Configurar Escenario", false, 1)]
        public static void SetupSimulationScene()
        {
            SimulationBootstrap existing = Object.FindAnyObjectByType<SimulationBootstrap>();
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject bootstrapObj = new GameObject("Simulation_Root");
            bootstrapObj.AddComponent<SimulationBootstrap>();

            Light dirLight = Object.FindAnyObjectByType<Light>();
            if (dirLight == null || dirLight.type != LightType.Directional)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Light l = lightObj.AddComponent<Light>();
                l.type = LightType.Directional;
                l.color = new Color(1f, 1f, 1f);
                l.intensity = 1.0f;
                l.shadows = LightShadows.None;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0);
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            if (cam.GetComponent<SimulationCamera>() == null)
            {
                cam.gameObject.AddComponent<SimulationCamera>();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("[Simulación] Escenario configurado.");
        }
    }
}
#endif

// Con apoyo de Gemini
