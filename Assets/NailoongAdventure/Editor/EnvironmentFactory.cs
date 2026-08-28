using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Nailoong.EditorTools
{
    /// <summary>
    /// 环境生成：程序化地形网格（含顶点色分层与 MeshCollider）、水面、渐变天空、光照与雾、植被散布。
    /// </summary>
    public static class EnvironmentFactory
    {
        [Serializable]
        public class Band
        {
            public float height;
            public Color32 color;
        }

        public class TerrainConfig
        {
            public string name = "Terrain";
            public float size = 200f;
            public int segments = 110;
            public Func<float, float, float> height;
            public List<Band> bands = new List<Band>();
            public Color32 cliffColor = new Color32(120, 110, 100, 255);
            public float cliffSlope = 0.55f;
            public float waterLevel = -999f;
            public int seed = 1234;
        }

        /// <summary>生成地形网格物体。</summary>
        public static GameObject BuildTerrain(TerrainConfig config, Material material)
        {
            int n = config.segments;
            float size = config.size;
            float half = size * 0.5f;
            float step = size / n;

            var vertices = new Vector3[(n + 1) * (n + 1)];
            var colors = new Color32[vertices.Length];
            var uvs = new Vector2[vertices.Length];

            for (int z = 0; z <= n; z++)
            {
                for (int x = 0; x <= n; x++)
                {
                    int i = z * (n + 1) + x;
                    float px = -half + x * step;
                    float pz = -half + z * step;
                    float py = config.height != null ? config.height(px, pz) : 0f;
                    vertices[i] = new Vector3(px, py, pz);
                    uvs[i] = new Vector2((float)x / n * 12f, (float)z / n * 12f);
                }
            }

            for (int z = 0; z <= n; z++)
            {
                for (int x = 0; x <= n; x++)
                {
                    int i = z * (n + 1) + x;
                    float h = vertices[i].y;

                    float slope = 0f;
                    if (x > 0 && x < n && z > 0 && z < n)
                    {
                        float dx = vertices[i + 1].y - vertices[i - 1].y;
                        float dz = vertices[i + (n + 1)].y - vertices[i - (n + 1)].y;
                        slope = new Vector2(dx, dz).magnitude / (step * 2f);
                    }

                    Color32 col = config.bands.Count > 0 ? config.bands[0].color : Color.white;
                    foreach (var b in config.bands)
                    {
                        if (h >= b.height) col = b.color;
                    }
                    if (slope > config.cliffSlope)
                    {
                        float k = Mathf.Clamp01((slope - config.cliffSlope) * 2.2f);
                        col = Color32.Lerp(col, config.cliffColor, k);
                    }
                    colors[i] = col;
                }
            }

            var triangles = new int[n * n * 6];
            int t = 0;
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    int a = z * (n + 1) + x;
                    int b = a + 1;
                    int c = a + (n + 1);
                    int d = c + 1;
                    triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                    triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
                }
            }

            var mesh = new Mesh { name = config.name + "_Mesh" };
            mesh.indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.colors32 = colors;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(config.name);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = true;
            go.AddComponent<MeshCollider>();
            return go;
        }

        /// <summary>水面：一个带波动 shader 的平面。</summary>
        public static GameObject BuildWater(float size, float level, Material waterMaterial)
        {
            var go = new GameObject("Water");
            var mf = go.AddComponent<MeshFilter>();
            int seg = 24;
            var verts = new Vector3[(seg + 1) * (seg + 1)];
            var uvs = new Vector2[verts.Length];
            var tris = new int[seg * seg * 6];
            float half = size * 0.5f;
            float step = size / seg;

            for (int z = 0; z <= seg; z++)
                for (int x = 0; x <= seg; x++)
                {
                    int i = z * (seg + 1) + x;
                    verts[i] = new Vector3(-half + x * step, 0f, -half + z * step);
                    uvs[i] = new Vector2((float)x / seg, (float)z / seg);
                }

            int t = 0;
            for (int z = 0; z < seg; z++)
                for (int x = 0; x < seg; x++)
                {
                    int a = z * (seg + 1) + x;
                    int b = a + 1;
                    int c = a + (seg + 1);
                    int d = c + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }

            var mesh = new Mesh { name = "WaterMesh" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = waterMaterial;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            go.transform.position = new Vector3(0f, level, 0f);
            return go;
        }

        /// <summary>天空盒 + 雾 + 环境光。</summary>
        public static void BuildSky(Color top, Color horizon, Color bottom, Color sun, Color fog, float fogDensity, float ambient)
        {
            var shader = Shader.Find("Nailoong/GradientSkybox");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_TopColor", top);
                mat.SetColor("_HorizonColor", horizon);
                mat.SetColor("_BottomColor", bottom);
                mat.SetColor("_SunColor", sun);
                mat.SetFloat("_SunSize", 0.045f);
                RenderSettings.skybox = mat;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(ambient, ambient, ambient * 1.05f);
        }

        /// <summary>主光源：暖色平行光 + 柔和阴影。</summary>
        public static Light BuildSun(Vector3 euler, Color color, float intensity)
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            light.shadowBias = 0.02f;
            light.shadowNormalBias = 0.05f;
            go.transform.rotation = Quaternion.Euler(euler);
            return light;
        }

        /// <summary>在地形上按规则散布装饰物（树、岩石、云）。</summary>
        public static void Scatter(GameObject parent, GameObject prefab, int count, float areaSize, Func<float, float, float> height,
            float minHeight, float maxHeight, float maxSlope, float minScale, float maxScale, int seed)
        {
            if (prefab == null) return;
            var rand = new System.Random(seed);
            float half = areaSize * 0.5f - 8f;

            for (int i = 0; i < count; i++)
            {
                float x = (float)(rand.NextDouble() * 2.0 - 1.0) * half;
                float z = (float)(rand.NextDouble() * 2.0 - 1.0) * half;
                float y = height(x, z);
                if (y < minHeight || y > maxHeight) continue;

                // 坡度检查
                float d = 1.2f;
                float hx = height(x + d, z) - height(x - d, z);
                float hz = height(x, z + d) - height(x, z - d);
                float slope = new Vector2(hx, hz).magnitude / (d * 2f);
                if (slope > maxSlope) continue;

                var inst = UnityEngine.Object.Instantiate(prefab, parent.transform);
                inst.transform.position = new Vector3(x, y, z);
                inst.transform.rotation = Quaternion.Euler(0f, (float)rand.NextDouble() * 360f, 0f);
                float s = Mathf.Lerp(minScale, maxScale, (float)rand.NextDouble());
                inst.transform.localScale = Vector3.one * s;
            }
        }

        /// <summary>在天空中散布云朵。</summary>
        public static void ScatterClouds(GameObject parent, GameObject cloudPrefab, int count, float areaSize, float height, int seed)
        {
            var rand = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var inst = UnityEngine.Object.Instantiate(cloudPrefab, parent.transform);
                float x = (float)(rand.NextDouble() * 2.0 - 1.0) * areaSize * 0.5f;
                float z = (float)(rand.NextDouble() * 2.0 - 1.0) * areaSize * 0.5f;
                float y = height + (float)rand.NextDouble() * 12f;
                inst.transform.position = new Vector3(x, y, z);
                inst.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 2.2f, (float)rand.NextDouble());
            }
        }
    }
}
