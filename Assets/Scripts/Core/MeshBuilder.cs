using UnityEngine;

namespace BattleRoyaleSim
{
    /// <summary>
    /// Generador procedural de primitivas geométricas 3D para la simulación.
    /// Crea las figuras solicitadas (armas triangulares, cofres, cilindro de tormenta).
    /// </summary>
    public static class MeshBuilder
    {
        private static Mesh trianglePrismMesh;
        private static Mesh diamondMesh;
        private static Mesh cylinderRingMesh;

        public static Mesh GetTrianglePrismMesh()
        {
            if (trianglePrismMesh != null) return trianglePrismMesh;

            Mesh mesh = new Mesh();
            mesh.name = "TrianglePrism";

            float size = 0.6f;
            float height = 0.8f;
            float halfH = height * 0.5f;

            float r = size * 0.5f;
            Vector3 b0 = new Vector3(0, -halfH, r);
            Vector3 b1 = new Vector3(r * Mathf.Sin(2f * Mathf.PI / 3f), -halfH, r * Mathf.Cos(2f * Mathf.PI / 3f));
            Vector3 b2 = new Vector3(r * Mathf.Sin(4f * Mathf.PI / 3f), -halfH, r * Mathf.Cos(4f * Mathf.PI / 3f));

            Vector3 t0 = b0 + Vector3.up * height;
            Vector3 t1 = b1 + Vector3.up * height;
            Vector3 t2 = b2 + Vector3.up * height;

            Vector3[] vertices = new Vector3[]
            {
                b0, b2, b1,
                t0, t1, t2,
                b0, t0, t1,
                b0, t1, b1,
                b1, t1, t2,
                b1, t2, b2,
                b2, t2, t0,
                b2, t0, b0
            };

            int[] triangles = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) triangles[i] = i;

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            trianglePrismMesh = mesh;
            return trianglePrismMesh;
        }

        public static Mesh GetDiamondMesh()
        {
            if (diamondMesh != null) return diamondMesh;

            Mesh mesh = new Mesh();
            mesh.name = "Diamond";

            float s = 0.4f;
            Vector3 top = Vector3.up * s;
            Vector3 bot = Vector3.down * s;
            Vector3 f = Vector3.forward * (s * 0.7f);
            Vector3 b = Vector3.back * (s * 0.7f);
            Vector3 r = Vector3.right * (s * 0.7f);
            Vector3 l = Vector3.left * (s * 0.7f);

            Vector3[] vertices = new Vector3[]
            {
                top, f, r,
                top, r, b,
                top, b, l,
                top, l, f,
                bot, r, f,
                bot, b, r,
                bot, l, b,
                bot, f, l
            };

            int[] triangles = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) triangles[i] = i;

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            diamondMesh = mesh;
            return diamondMesh;
        }

        public static Material CreateColoredMaterial(Color color, float smoothness = 0.5f, bool emission = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");

            Material mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (emission)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", color * 1.5f);
                }
            }

            return mat;
        }
    }
}

// Con apoyo de Gemini
