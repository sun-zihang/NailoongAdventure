using System.Collections.Generic;
using UnityEngine;

namespace Nailoong.EditorTools
{
    /// <summary>
    /// 程序化网格构建器：以「椭球 / 锥体 / 管状 / 四边形」为基本图元累积顶点，
    /// 同时写入顶点色与骨骼权重，最后合并为单个带蒙皮信息的 Mesh。
    /// 角色模型完全由代码生成，不依赖任何外部资源。
    /// </summary>
    public class MeshBuilder
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Color32> colors = new List<Color32>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> triangles = new List<int>();
        readonly List<BoneWeight> weights = new List<BoneWeight>();

        public int VertexCount => vertices.Count;

        static BoneWeight Weight(int b0, float w0, int b1 = 0, float w1 = 0f, int b2 = 0, float w2 = 0f, int b3 = 0, float w3 = 0f)
        {
            var bw = new BoneWeight();
            bw.boneIndex0 = b0; bw.weight0 = w0;
            bw.boneIndex1 = b1; bw.weight1 = w1;
            bw.boneIndex2 = b2; bw.weight2 = w2;
            bw.boneIndex3 = b3; bw.weight3 = w3;
            return bw;
        }

        /// <summary>添加一个椭球（可绕任意轴旋转、可沿 Y 锥化）。</summary>
        public void AddEllipsoid(Vector3 center, Vector3 radius, int bone, Color32 color,
            int segments = 14, int rings = 10, Quaternion rotation = default, float taperTop = 1f, float squashBottom = 1f)
        {
            if (rotation == default) rotation = Quaternion.identity;
            int baseIndex = vertices.Count;

            for (int r = 0; r <= rings; r++)
            {
                float v = (float)r / rings;
                float phi = v * Mathf.PI;
                float y = Mathf.Cos(phi);
                float ring = Mathf.Sin(phi);
                float taper = Mathf.Lerp(1f, taperTop, (y + 1f) * 0.5f);
                float bottom = y < 0f ? squashBottom : 1f;

                for (int s = 0; s <= segments; s++)
                {
                    float u = (float)s / segments;
                    float theta = u * Mathf.PI * 2f;
                    Vector3 local = new Vector3(Mathf.Cos(theta) * ring * taper, y, Mathf.Sin(theta) * ring * taper);
                    local.y *= bottom;
                    local.x *= radius.x; local.y *= radius.y; local.z *= radius.z;
                    Vector3 world = center + rotation * local;

                    Vector3 n = new Vector3(Mathf.Cos(theta) * ring, y, Mathf.Sin(theta) * ring);
                    n.y /= Mathf.Max(radius.y, 0.0001f);
                    n.x /= Mathf.Max(radius.x, 0.0001f);
                    n.z /= Mathf.Max(radius.z, 0.0001f);
                    n = (rotation * n).normalized;

                    vertices.Add(world);
                    normals.Add(n);
                    colors.Add(color);
                    uvs.Add(new Vector2(u, 1f - v));
                    weights.Add(Weight(bone, 1f));
                }
            }

            int stride = segments + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = baseIndex + r * stride + s;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }
        }

        /// <summary>添加在两根骨骼间平滑过渡的椭球（用于尾巴、关节）。</summary>
        public void AddBlendEllipsoid(Vector3 center, Vector3 radius, int boneA, int boneB, float wA, Color32 color,
            int segments = 12, int rings = 8, Quaternion rotation = default)
        {
            if (rotation == default) rotation = Quaternion.identity;
            int baseIndex = vertices.Count;
            float wB = 1f - wA;

            for (int r = 0; r <= rings; r++)
            {
                float v = (float)r / rings;
                float phi = v * Mathf.PI;
                float y = Mathf.Cos(phi);
                float ring = Mathf.Sin(phi);
                for (int s = 0; s <= segments; s++)
                {
                    float u = (float)s / segments;
                    float theta = u * Mathf.PI * 2f;
                    Vector3 local = new Vector3(Mathf.Cos(theta) * ring * radius.x, y * radius.y, Mathf.Sin(theta) * ring * radius.z);
                    vertices.Add(center + rotation * local);
                    Vector3 n = new Vector3(Mathf.Cos(theta) * ring / Mathf.Max(radius.x, 0.0001f), y / Mathf.Max(radius.y, 0.0001f), Mathf.Sin(theta) * ring / Mathf.Max(radius.z, 0.0001f));
                    normals.Add((rotation * n).normalized);
                    colors.Add(color);
                    uvs.Add(new Vector2(u, 1f - v));
                    weights.Add(Weight(boneA, wA, boneB, wB));
                }
            }

            int stride = segments + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = baseIndex + r * stride + s;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }
        }

        /// <summary>沿一条折线生成管状体（尾巴、四肢、脖子），可为每段指定骨骼并自动做权重过渡。</summary>
        public void AddTube(IList<Vector3> path, IList<float> radii, IList<int> bones, Color32 color, int radialSegments = 10)
        {
            if (path == null || path.Count < 2) return;
            int baseIndex = vertices.Count;

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 p = path[i];
                Vector3 dir;
                if (i == 0) dir = (path[1] - path[0]).normalized;
                else if (i == path.Count - 1) dir = (path[i] - path[i - 1]).normalized;
                else dir = (path[i + 1] - path[i - 1]).normalized;

                Vector3 up = Mathf.Abs(dir.y) > 0.9f ? Vector3.forward : Vector3.up;
                Vector3 right = Vector3.Cross(dir, up).normalized;
                Vector3 forward = Vector3.Cross(right, dir).normalized;

                // 相邻骨骼权重过渡
                float wA = 1f;
                int boneA = bones[Mathf.Min(i, bones.Count - 1)];
                int boneB = boneA;
                float wB = 0f;
                if (i == path.Count - 1 && bones.Count > i - 1)
                {
                    boneB = bones[Mathf.Max(0, i - 1)];
                    wA = 0.5f; wB = 0.5f;
                }
                else if (i > 0 && i < path.Count - 1)
                {
                    boneB = i + 1 < bones.Count ? bones[i + 1] : boneA;
                    wA = 0.65f; wB = 0.35f;
                }

                for (int s = 0; s <= radialSegments; s++)
                {
                    float a = (float)s / radialSegments * Mathf.PI * 2f;
                    Vector3 offset = right * Mathf.Cos(a) + forward * Mathf.Sin(a);
                    vertices.Add(p + offset * radii[i]);
                    normals.Add(offset.normalized);
                    colors.Add(color);
                    uvs.Add(new Vector2((float)s / radialSegments, (float)i / (path.Count - 1)));
                    weights.Add(Weight(boneA, wA, boneB, wB));
                }
            }

            int stride = radialSegments + 1;
            for (int i = 0; i < path.Count - 1; i++)
            {
                for (int s = 0; s < radialSegments; s++)
                {
                    int a = baseIndex + i * stride + s;
                    int b = a + 1;
                    int c = a + stride;
                    int d = c + 1;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }
        }

        /// <summary>圆锥（角、牙、爪、喙）。</summary>
        public void AddCone(Vector3 baseCenter, Vector3 tip, float radius, int bone, Color32 color, int segments = 10)
        {
            int baseIndex = vertices.Count;
            Vector3 dir = (tip - baseCenter).normalized;
            Vector3 up = Mathf.Abs(dir.y) > 0.9f ? Vector3.forward : Vector3.up;
            Vector3 right = Vector3.Cross(dir, up).normalized;
            Vector3 forward = Vector3.Cross(right, dir).normalized;

            for (int s = 0; s <= segments; s++)
            {
                float a = (float)s / segments * Mathf.PI * 2f;
                Vector3 offset = right * Mathf.Cos(a) + forward * Mathf.Sin(a);
                vertices.Add(baseCenter + offset * radius);
                normals.Add((offset - dir * 0.25f).normalized);
                colors.Add(color);
                uvs.Add(new Vector2((float)s / segments, 0f));
                weights.Add(Weight(bone, 1f));
            }

            vertices.Add(tip);
            normals.Add(dir);
            colors.Add(color);
            uvs.Add(new Vector2(0.5f, 1f));
            weights.Add(Weight(bone, 1f));
            int tipIndex = vertices.Count - 1;

            for (int s = 0; s < segments; s++)
            {
                triangles.Add(baseIndex + s);
                triangles.Add(tipIndex);
                triangles.Add(baseIndex + s + 1);
            }
        }

        /// <summary>四边形（翅膀膜、披风、鳍）。normal 由绕序决定。</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int bone, Color32 color)
        {
            int start = vertices.Count;
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            for (int i = 0; i < 4; i++)
            {
                normals.Add(n);
                colors.Add(color);
                weights.Add(Weight(bone, 1f));
            }
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
        }

        /// <summary>双面四边形（翅膀这类需要正反都可见的面）。</summary>
        public void AddQuadTwoSided(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int bone, Color32 color)
        {
            AddQuad(a, b, c, d, bone, color);
            AddQuad(d, c, b, a, bone, color);
        }

        /// <summary>圆角球体（果冻怪主体这类需要更"胖"的形状）。</summary>
        public void AddBlob(Vector3 center, Vector3 radius, int bone, Color32 color, int segments = 16, int rings = 12, float flatBottom = 0.85f)
        {
            AddEllipsoid(center, radius, bone, color, segments, rings, Quaternion.identity, 1f, flatBottom);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.boneWeights = weights.ToArray();      // Unity 6 仍保留该属性
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
