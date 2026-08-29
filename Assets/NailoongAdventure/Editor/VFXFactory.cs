using UnityEditor;
using UnityEngine;

namespace Nailoong.EditorTools
{
    /// <summary>
    /// 粒子特效预制体工厂：全部用代码配置 ParticleSystem，输出到 Resources/VFX 供运行时加载。
    /// </summary>
    public static class VFXFactory
    {
        static Material particleMaterial;

        public static void GenerateAll(string folder)
        {
            System.IO.Directory.CreateDirectory(folder);
            var shader = Shader.Find("Particles/Standard Unlit");
            particleMaterial = new Material(shader) { name = "VFX_Particle" };
            string matPath = GameBuilder.MAT + "/VFX_Particle.mat";
            System.IO.Directory.CreateDirectory(GameBuilder.MAT);
            AssetDatabase.CreateAsset(particleMaterial, matPath);

            Save(folder, "vfx_hit", Hit());
            Save(folder, "vfx_explode", Explode());
            Save(folder, "vfx_pickup", Pickup());
            Save(folder, "vfx_heal", Heal());
            Save(folder, "vfx_land", Land());
            Save(folder, "vfx_jump", Jump());
            Save(folder, "vfx_dash", Dash());
            Save(folder, "vfx_slam", Slam());
            Save(folder, "vfx_breath", Breath());
            Save(folder, "vfx_shift", Shift());
            AssetDatabase.SaveAssets();
        }

        static void Save(string folder, string name, GameObject go)
        {
            string path = folder + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        // ---------- 基元 ----------
        static GameObject Root(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<ParticleSystem>();
            return go;
        }

        static ParticleSystem Ps(GameObject go)
        {
            var ps = go.GetComponent<ParticleSystem>();
            ps.gameObject.SetActive(true);
            var main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = particleMaterial;
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        static void Burst(ParticleSystem ps, int count, float time = 0f)
        {
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(time, count) });
        }

        static void Fade(ParticleSystem ps, Color start, Color end)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
        }

        static void Shrink(ParticleSystem ps, float endScale = 0f)
        {
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, endScale));
        }

        // ---------- 各特效 ----------
        static GameObject Hit()
        {
            var go = Root("vfx_hit");
            var ps = Ps(go);
            var main = ps.main;
            main.duration = 0.25f;
            main.startLifetime = 0.35f;
            main.startSpeed = 7f;
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 0.92f, 0.6f, 1f);
            main.gravityModifier = 0.6f;
            Burst(ps, 16);
            Fade(ps, new Color(1f, 0.95f, 0.7f), new Color(1f, 0.55f, 0.2f));
            Shrink(ps);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 34f;
            shape.radius = 0.12f;

            // 星形闪光
            var spark = new GameObject("Spark");
            spark.transform.SetParent(go.transform, false);
            var sp = spark.AddComponent<ParticleSystem>();
            var smain = sp.main;
            smain.playOnAwake = false;
            smain.simulationSpace = ParticleSystemSimulationSpace.World;
            smain.loop = false;
            smain.duration = 0.15f;
            smain.startLifetime = 0.18f;
            smain.startSpeed = 2f;
            smain.startSize = 0.5f;
            smain.startColor = Color.white;
            smain.gravityModifier = 0f;
            Burst(sp, 2);
            Shrink(sp);
            var srend = sp.GetComponent<ParticleSystemRenderer>();
            srend.sharedMaterial = particleMaterial;
            return go;
        }

        static GameObject Explode()
        {
            var go = Root("vfx_explode");
            var ps = Ps(go);
            var main = ps.main;
            main.duration = 0.4f;
            main.startLifetime = 0.6f;
            main.startSpeed = 9f;
            main.startSize = 0.35f;
            main.startColor = new Color(1f, 0.8f, 0.4f, 1f);
            main.gravityModifier = 0.4f;
            Burst(ps, 34);
            Fade(ps, new Color(1f, 0.95f, 0.6f), new Color(0.9f, 0.25f, 0.15f));
            Shrink(ps);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.speedModifier = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            return go;
        }

        static GameObject Pickup()
        {
            var go = Root("vfx_pickup");
            var ps = Ps(go);
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.55f;
            main.startSpeed = 2.4f;
            main.startSize = 0.16f;
            main.startColor = new Color(1f, 0.9f, 0.45f, 1f);
            main.gravityModifier = -0.35f;   // 向上飘
            Burst(ps, 20);
            Fade(ps, new Color(1f, 1f, 0.85f), new Color(1f, 0.7f, 0.25f));
            Shrink(ps);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.32f;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(180f);
            return go;
        }

        static GameObject Heal()
        {
            var go = Root("vfx_heal");
            var ps = Ps(go);
            var main = ps.main;
            main.duration = 0.8f;
            main.startLifetime = 0.9f;
            main.startSpeed = 1.4f;
            main.startSize = 0.2f;
            main.startColor = new Color(0.55f, 1f, 0.7f, 1f);
            main.gravityModifier = -0.6f;
            Burst(ps, 26);
            Fade(ps, new Color(0.7f, 1f, 0.75f), new Color(0.2f, 0.85f, 0.5f));
            Shrink(ps);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.6f;
            return go;
        }

        static GameObject Land()
        {
            var go = Root("vfx_land");
            var ps = Ps(go);
            var main = ps.main;
            main.duration = 0.3f;
            main.startLifetime = 0.4f;
            main.startSpeed = 3.6f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.95f, 0.9f, 0.8f, 0.85f);
            main.gravityModifier = 0.2f;
            Burst(ps, 18);
            Fade(ps, new Color(1f, 0.98f, 0.92f, 0.9f), new Color(0.7f, 0.66f, 0.6f, 0f));
            Shrink(ps, 0.4f);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(1.5f);
            return go;
        }

        static GameObject Jump()
        {
            var go = Root("vfx_jump");
            var ps = Ps(go);
            var main = ps.main;
            main.duration = 0.25f;
            main.startLifetime = 0.3f;
            main.startSpeed = 2.2f;
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 0.95f, 0.75f, 0.8f);
            main.gravityModifier = 0.1f;
            Burst(ps, 14);
            Fade(ps, new Color(1f, 1f, 0.9f, 0.85f), new Color(0.8f, 0.75f, 0.6f, 0f));
            Shrink(ps, 0.3f);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.45f;
            return go;
        }

        static GameObject Dash()
        {
            var go = Root("vfx_dash");
            var ps = Ps(go);
            var main = ps.main;
            main.duration = 0.35f;
            main.startLifetime = 0.3f;
            main.startSpeed = 1.2f;
            main.startSize = 0.28f;
            main.startColor = new Color(1f, 0.85f, 0.5f, 0.75f);
            main.gravityModifier = 0f;
            Burst(ps, 10);
            Fade(ps, new Color(1f, 0.9f, 0.65f, 0.8f), new Color(1f, 0.6f, 0.3f, 0f));
            Shrink(ps);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;
            return go;
        }

        static GameObject Slam()
        {
            var go = Root("vfx_slam");
            // 冲击环
            var ps = Ps(go);
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.5f;
            main.startSpeed = 6f;
            main.startSize = 0.4f;
            main.startColor = new Color(1f, 0.75f, 0.35f, 1f);
            main.gravityModifier = 0.1f;
            Burst(ps, 40);
            Fade(ps, new Color(1f, 0.9f, 0.6f), new Color(0.85f, 0.35f, 0.15f));
            Shrink(ps, 0.2f);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            shape.radiusThickness = 1f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.radial = new ParticleSystem.MinMaxCurve(9f);

            // 碎石
            var debris = new GameObject("Debris");
            debris.transform.SetParent(go.transform, false);
            var dp = debris.AddComponent<ParticleSystem>();
            var dmain = dp.main;
            dmain.playOnAwake = false;
            dmain.simulationSpace = ParticleSystemSimulationSpace.World;
            dmain.loop = false;
            dmain.duration = 0.5f;
            dmain.startLifetime = 0.7f;
            dmain.startSpeed = 7f;
            dmain.startSize = 0.18f;
            dmain.startColor = new Color(0.85f, 0.8f, 0.72f, 1f);
            dmain.gravityModifier = 1.4f;
            Burst(dp, 22);
            Fade(dp, new Color(0.9f, 0.87f, 0.8f), new Color(0.5f, 0.47f, 0.42f));
            var dshape = dp.shape;
            dshape.shapeType = ParticleSystemShapeType.Sphere;
            dshape.radius = 0.3f;
            var drend = dp.GetComponent<ParticleSystemRenderer>();
            drend.sharedMaterial = particleMaterial;
            return go;
        }

        static GameObject Breath()
        {
            var go = Root("vfx_breath");
            var ps = Ps(go);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.6f;
            main.startLifetime = 0.45f;
            main.startSpeed = 14f;
            main.startSize = 0.3f;
            main.startColor = new Color(1f, 0.72f, 0.3f, 1f);
            main.gravityModifier = -0.15f;

            var emission = ps.emission;
            emission.rateOverTime = 90f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.18f;

            Fade(ps, new Color(1f, 0.95f, 0.6f), new Color(1f, 0.35f, 0.15f));
            Shrink(ps, 0.3f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-180f, 180f);
            return go;
        }

        static GameObject Shift()
        {
            var go = Root("vfx_shift");
            var ps = Ps(go);
            var main = ps.main;
            main.duration = 0.7f;
            main.startLifetime = 0.7f;
            main.startSpeed = 3.2f;
            main.startSize = 0.24f;
            main.startColor = new Color(0.6f, 0.85f, 1f, 1f);
            main.gravityModifier = -0.2f;
            Burst(ps, 30);
            Fade(ps, new Color(0.8f, 0.95f, 1f), new Color(0.35f, 0.6f, 1f));
            Shrink(ps);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(-3f);
            return go;
        }
    }
}
