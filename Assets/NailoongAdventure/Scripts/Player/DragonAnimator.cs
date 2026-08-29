using System;
using UnityEngine;

namespace Nailoong
{
    /// <summary>
    /// 奶龙动画：程序化骨骼驱动（不依赖任何动画资源）。
    /// 每个状态实时计算目标姿态，再按骨骼做阻尼插值，因此任意状态之间都能平滑衔接。
    /// 叠加层：呼吸、尾巴延迟摆动、眨眼、看向、受击抖动、挤压拉伸。
    /// </summary>
    public class DragonAnimator : MonoBehaviour
    {
        // 骨骼命名（须与建模脚本一致）
        static readonly string[] BoneNames =
        {
            "Hips","Spine","Chest","Belly","Neck","Head","Jaw",
            "Tail1","Tail2","Tail3",
            "ArmL","ArmR","HandL","HandR",
            "LegL","LegR","FootL","FootR"
        };

        public enum State { Locomotion, Jump, Fall, Land, Dash, Claw, Tail, Breath, Slam, Hurt, Eat, Victory, Sleep, Fire, ColorChange, Grow }

        [Header("姿态平滑")]
        public float blendSpeed = 12f;
        public float actionBlendSpeed = 22f;

        [Header("挤压拉伸")]
        public float squashAmount = 0.22f;
        public float bobAmount = 0.055f;
        public float bobFrequency = 9f;

        [Header("眨眼")]
        public float blinkInterval = 3.4f;

        // 外部引用
        public Transform mouthPoint;      // 吐息发射点
        public Transform eyeL, eyeR;      // 眼睛（用于眨眼）

        Transform[] bones = new Transform[BoneNames.Length];
        Vector3[] bindEuler = new Vector3[BoneNames.Length];
        Vector3[] targetEuler = new Vector3[BoneNames.Length];
        Vector3[] currentEuler = new Vector3[BoneNames.Length];
        Vector3[] velEuler = new Vector3[BoneNames.Length];

        PlayerController player;
        Damageable dmg;
        Transform hips;
        Vector3 hipsBasePos;

        // 奶龙灵魂：duang~duang 大肚腩弹性抖动
        Transform belly;
        Vector3 bellyBasePos;
        Vector3 bellyBaseScale;
        float bellySquash;
        float bellyVel;
        const float BELLY_STIFFNESS = 220f;
        const float BELLY_DAMPING = 10f;
        float bellyStepPhase;

        State state = State.Locomotion;
        float stateTime, actionDuration;
        float blinkTimer, breathe, tailPhase, landTimer, hurtTimer;
        float squash;
        bool actionFinished = true;

        public State Current => state;
        public bool IsActionPlaying => !actionFinished;

        void Awake()
        {
            for (int i = 0; i < BoneNames.Length; i++)
            {
                bones[i] = FindDeep(transform, BoneNames[i]);
                if (bones[i] != null) bindEuler[i] = bones[i].localEulerAngles;
            }
            hips = FindDeep(transform, "Hips");
            if (hips != null) hipsBasePos = hips.localPosition;

            belly = FindDeep(transform, "Belly");
            if (belly != null)
            {
                bellyBasePos = belly.localPosition;
                bellyBaseScale = belly.localScale;
            }

            player = GetComponent<PlayerController>();
            dmg = GetComponent<Damageable>();

            if (eyeL == null) eyeL = FindDeep(transform, "Eye_L");
            if (eyeR == null) eyeR = FindDeep(transform, "Eye_R");
            if (mouthPoint == null) mouthPoint = FindDeep(transform, "MouthPoint");

            blinkTimer = UnityEngine.Random.Range(1f, blinkInterval);

            if (player != null)
            {
                player.OnJump += () => { ImpulseBelly(0.7f); Play(State.Jump, 0.45f); };
                player.OnDoubleJump += () => { ImpulseBelly(0.55f); Play(State.Jump, 0.5f); };
                player.OnLand += () => { landTimer = 0.28f; ImpulseBelly(1.35f); Play(State.Land, 0.28f); };
                player.OnDashStart += () => { ImpulseBelly(0.45f); Play(State.Dash, 0.5f); };
            }
            if (dmg != null) dmg.Damaged += (d, amount, point) => { hurtTimer = 0.45f; Play(State.Hurt, 0.45f); };
        }

        void Update()
        {
            stateTime += Time.deltaTime;
            if (landTimer > 0f) landTimer -= Time.deltaTime;
            if (hurtTimer > 0f) hurtTimer -= Time.deltaTime;

            ComputeTargetPose();
            ApplyPose();
            UpdateExtras();
        }

        /// <summary>播放一次性动作（攻击、受击等），结束后自动回到移动状态。</summary>
        public void Play(State s, float duration)
        {
            state = s;
            stateTime = 0f;
            actionDuration = duration;
            actionFinished = false;
        }

        void ComputeTargetPose()
        {
            Array.Clear(targetEuler, 0, targetEuler.Length); // 归零后再叠加，避免残留

            float speed = player != null ? player.PlanarSpeed : 0f;
            float move01 = Mathf.Clamp01(speed / 6.5f);
            float t = Time.time;

            if (stateTime >= actionDuration && state != State.Locomotion &&
                state != State.Jump && state != State.Fall && state != State.Dash)
            {
                state = State.Locomotion;
                actionFinished = true;
            }

            switch (state)
            {
                case State.Locomotion: PoseLocomotion(t, move01); break;
                case State.Jump: PoseJump(); break;
                case State.Fall: PoseFall(); break;
                case State.Land: PoseLand(); break;
                case State.Dash: PoseDash(t); break;
                case State.Claw: PoseClaw(); break;
                case State.Tail: PoseTail(); break;
                case State.Breath: PoseBreath(t); break;
                case State.Slam: PoseSlam(); break;
                case State.Hurt: PoseHurt(); break;
                case State.Eat: PoseEat(t); break;
                case State.Victory: PoseVictory(t); break;
                case State.Sleep: PoseSleep(t); break;
                case State.Fire: PoseFire(t); break;
                case State.ColorChange: PoseColorChange(t); break;
                case State.Grow: PoseGrow(t); break;
            }

            // 叠加：呼吸
            breathe = Mathf.Sin(t * 2.1f) * 0.5f + 0.5f;
            Add("Chest", new Vector3(-breathe * 2.2f, 0f, 0f));
            Add("Spine", new Vector3(breathe * 1.4f, 0f, 0f));

            // 叠加：尾巴延迟波浪（相位随骨序号偏移）
            tailPhase += Time.deltaTime * (2.4f + move01 * 5.5f);
            for (int i = 0; i < 4; i++)
            {
                float ph = tailPhase - i * 0.55f;
                Add("Tail" + (i + 1), new Vector3(Mathf.Sin(ph) * 4f * (1f - move01 * 0.3f), Mathf.Sin(ph * 0.8f) * (7f + move01 * 6f), 0f));
            }

            // 叠加：跑动时身体前倾 + 奶龙憨态 waddle（左右摆臀）
            Add("Hips", new Vector3(move01 * 7f, Mathf.Sin(t * (6f + move01 * 5f)) * move01 * 7f, 0f));
            Add("Neck", new Vector3(-move01 * 5f, 0f, 0f));

            // 叠加：走路时肚皮轻微 duang（一步一颤）
            bellyStepPhase += Time.deltaTime * (7f + move01 * 6f);
            float stepWobble = Mathf.Sin(bellyStepPhase) * 0.06f * move01;
            bellyVel += stepWobble - bellySquash * 0.5f * move01;
        }

        // ---------- 各状态姿态 ----------
        void PoseLocomotion(float t, float move01)
        {
            float swing = Mathf.Sin(t * (6f + move01 * 5f));
            float amp = 12f + move01 * 34f;
            if (player != null && !player.IsGrounded)
            {
                if (player.VerticalSpeed > 0.5f) { PoseJump(); return; }
                PoseFall(); return;
            }

            Add("ArmL", new Vector3(swing * amp, 0f, -8f - move01 * 6f));
            Add("ArmR", new Vector3(-swing * amp, 0f, 8f + move01 * 6f));
            Add("LegL", new Vector3(-swing * amp * 0.85f, 0f, 0f));
            Add("LegR", new Vector3(swing * amp * 0.85f, 0f, 0f));
            Add("FootL", new Vector3(Mathf.Max(0f, -swing) * 12f, 0f, 0f));
            Add("FootR", new Vector3(Mathf.Max(0f, swing) * 12f, 0f, 0f));
            Add("Head", new Vector3(-move01 * 4f, Mathf.Sin(t * 1.3f) * 4f, 0f));
            // 奶龙没有翅膀，不生成翅膀摆动
        }

        void PoseJump()
        {
            Add("ArmL", new Vector3(-52f, 0f, -14f));
            Add("ArmR", new Vector3(-52f, 0f, 14f));
            Add("LegL", new Vector3(-32f, 0f, 0f));
            Add("LegR", new Vector3(-26f, 0f, 0f));
            Add("Head", new Vector3(-8f, 0f, 0f));
        }

        void PoseFall()
        {
            Add("ArmL", new Vector3(-24f, 0f, -32f));
            Add("ArmR", new Vector3(-24f, 0f, 32f));
            Add("LegL", new Vector3(24f, 0f, 0f));
            Add("LegR", new Vector3(18f, 0f, 0f));
            Add("Head", new Vector3(10f, 0f, 0f));
        }

        void PoseLand()
        {
            float k = 1f - Mathf.Clamp01(stateTime / Mathf.Max(actionDuration, 0.01f));
            Add("Hips", new Vector3(14f * k, 0f, 0f));
            Add("LegL", new Vector3(-26f * k, 0f, 0f));
            Add("LegR", new Vector3(-26f * k, 0f, 0f));
            Add("ArmL", new Vector3(0f, 0f, -34f * k));
            Add("ArmR", new Vector3(0f, 0f, 34f * k));
            Add("Head", new Vector3(12f * k, 0f, 0f));
            squash = -squashAmount * k;
        }

        void PoseDash(float t)
        {
            float k = Mathf.Clamp01(stateTime / Mathf.Max(actionDuration, 0.01f));
            float roll = k * 720f;
            Add("Hips", new Vector3(roll, 0f, 0f));
            Add("Spine", new Vector3(18f, 0f, 0f));
            Add("ArmL", new Vector3(-58f, 0f, -6f));
            Add("ArmR", new Vector3(-58f, 0f, 6f));
            Add("LegL", new Vector3(-62f, 0f, 0f));
            Add("LegR", new Vector3(-62f, 0f, 0f));
            Add("Head", new Vector3(-16f, 0f, 0f));
            squash = 0.16f * Mathf.Sin(k * Mathf.PI);
        }

        void PoseClaw()
        {
            float k = stateTime / Mathf.Max(actionDuration, 0.01f);
            float s = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);      // 0→1→0
            Add("ArmR", new Vector3(-96f * s, 0f, 24f));
            Add("ArmL", new Vector3(26f * s, 0f, -18f));
            Add("Spine", new Vector3(0f, -26f * s, 0f));
            Add("Chest", new Vector3(0f, 18f * s, 0f));
            Add("Head", new Vector3(6f * s, 12f * s, 0f));
            Add("Jaw", new Vector3(24f * s, 0f, 0f));
        }

        void PoseTail()
        {
            float k = stateTime / Mathf.Max(actionDuration, 0.01f);
            float s = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
            Add("Hips", new Vector3(0f, -46f * s, 0f));
            Add("Tail1", new Vector3(0f, 62f * s, 0f));
            Add("Tail2", new Vector3(0f, 42f * s, 0f));
            Add("ArmL", new Vector3(0f, 0f, -30f * s));
            Add("ArmR", new Vector3(0f, 0f, 30f * s));
            Add("Head", new Vector3(0f, 26f * s, 0f));
        }

        void PoseBreath(float t)
        {
            float k = Mathf.Clamp01(stateTime / Mathf.Max(actionDuration, 0.01f));
            float wind = Mathf.SmoothStep(0f, 0.25f, k);
            float end = 1f - Mathf.SmoothStep(0.8f, 1f, k);
            float s = wind * end;
            Add("Head", new Vector3(-16f * s, 0f, 0f));
            Add("Neck", new Vector3(-10f * s, 0f, 0f));
            Add("Jaw", new Vector3(38f * s + Mathf.Sin(t * 30f) * 3f * s, 0f, 0f));
            Add("ArmL", new Vector3(-46f * s, 0f, -22f));
            Add("ArmR", new Vector3(-46f * s, 0f, 22f));
            Add("Chest", new Vector3(-8f * s, 0f, 0f));
            Add("Belly", new Vector3(-4f * s, 0f, 0f));
        }

        void PoseSlam()
        {
            float k = stateTime / Mathf.Max(actionDuration, 0.01f);
            float up = 1f - Mathf.Clamp01(k * 2f);          // 前半段腾空
            float down = Mathf.Clamp01((k - 0.45f) / 0.55f); // 后半段砸下
            Add("ArmL", new Vector3(-140f * up + 40f * down, 0f, -18f));
            Add("ArmR", new Vector3(-140f * up + 40f * down, 0f, 18f));
            Add("LegL", new Vector3(-50f * up + 20f * down, 0f, 0f));
            Add("LegR", new Vector3(-50f * up + 20f * down, 0f, 0f));
            Add("Spine", new Vector3(-18f * up + 22f * down, 0f, 0f));
            Add("Head", new Vector3(-22f * up + 26f * down, 0f, 0f));
            Add("Jaw", new Vector3(30f * down, 0f, 0f));
            squash = squashAmount * up - squashAmount * 1.6f * down;
        }

        void PoseHurt()
        {
            float k = 1f - Mathf.Clamp01(stateTime / Mathf.Max(actionDuration, 0.01f));
            float shake = Mathf.Sin(stateTime * 60f) * 6f * k;
            Add("Spine", new Vector3(-22f * k, shake, 0f));
            Add("Head", new Vector3(-26f * k, 0f, 0f));
            Add("Jaw", new Vector3(34f * k, 0f, 0f));
            Add("ArmL", new Vector3(24f * k, 0f, -40f * k));
            Add("ArmR", new Vector3(24f * k, 0f, 40f * k));
        }

        void PoseEat(float t)
        {
            float chew = Mathf.Abs(Mathf.Sin(t * 14f));
            Add("Jaw", new Vector3(26f * chew, 0f, 0f));
            Add("Head", new Vector3(-12f, 0f, 0f));
            Add("ArmL", new Vector3(-58f, 0f, -20f));
            Add("ArmR", new Vector3(-58f, 0f, 20f));
            Add("Chest", new Vector3(-6f + chew * 3f, 0f, 0f));
        }

        void PoseVictory(float t)
        {
            float hop = Mathf.Abs(Mathf.Sin(t * 6f));
            Add("ArmL", new Vector3(-150f - hop * 14f, 0f, -22f));
            Add("ArmR", new Vector3(-150f - hop * 14f, 0f, 22f));
            Add("Head", new Vector3(-14f, Mathf.Sin(t * 3f) * 12f, 0f));
            Add("Jaw", new Vector3(18f + hop * 10f, 0f, 0f));
            Add("Spine", new Vector3(-10f, 0f, 0f));
            squash = 0.14f * hop;
        }

        void PoseSleep(float t)
        {
            float b = Mathf.Sin(t * 1.4f);
            Add("Head", new Vector3(16f + b * 3f, 0f, 0f));
            Add("Spine", new Vector3(8f, 0f, 0f));
            Add("ArmL", new Vector3(20f, 0f, -12f));
            Add("ArmR", new Vector3(20f, 0f, 12f));
            Add("LegL", new Vector3(24f, 0f, 0f));
            Add("LegR", new Vector3(24f, 0f, 0f));
            squash = 0.08f + b * 0.03f;
        }

        void PoseFire(float t)
        {
            float k = Mathf.Clamp01(stateTime / Mathf.Max(actionDuration, 0.01f));
            float wind = Mathf.SmoothStep(0f, 0.2f, k);
            float end = 1f - Mathf.SmoothStep(0.75f, 1f, k);
            float s = wind * end;
            Add("Head", new Vector3(-18f * s, 0f, 0f));
            Add("Neck", new Vector3(-12f * s, 0f, 0f));
            Add("Jaw", new Vector3(42f * s + Mathf.Sin(t * 28f) * 4f * s, 0f, 0f));
            Add("ArmL", new Vector3(-40f * s, 0f, -26f));
            Add("ArmR", new Vector3(-40f * s, 0f, 26f));
            Add("Chest", new Vector3(-6f * s, 0f, 0f));
            Add("Belly", new Vector3(-5f * s, 0f, 0f));
        }

        void PoseColorChange(float t)
        {
            float k = Mathf.Clamp01(stateTime / Mathf.Max(actionDuration, 0.01f));
            float s = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
            Add("Head", new Vector3(-8f * s, Mathf.Sin(t * 12f) * 10f * s, 0f));
            Add("ArmL", new Vector3(0f, 0f, -140f * s));
            Add("ArmR", new Vector3(0f, 0f, 140f * s));
            Add("Jaw", new Vector3(20f * s, 0f, 0f));
            squash = 0.12f * s;
        }

        void PoseGrow(float t)
        {
            float k = Mathf.Clamp01(stateTime / Mathf.Max(actionDuration, 0.01f));
            float s = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
            Add("Head", new Vector3(-10f * s, 0f, 0f));
            Add("Spine", new Vector3(-6f * s, 0f, 0f));
            Add("ArmL", new Vector3(-150f * s, 0f, -20f * s));
            Add("ArmR", new Vector3(-150f * s, 0f, 20f * s));
            Add("LegL", new Vector3(-20f * s, 0f, 0f));
            Add("LegR", new Vector3(-20f * s, 0f, 0f));
            squash = -0.14f * s; // 向上拉伸
        }

        // ---------- 应用 ----------
        void ApplyPose()
        {
            float sp = (state == State.Locomotion) ? blendSpeed : actionBlendSpeed;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                Vector3 goal = targetEuler[i];
                for (int axis = 0; axis < 3; axis++)
                {
                    float cur = currentEuler[i][axis];
                    float tgt = goal[axis];
                    float v = velEuler[i][axis];
                    float smoothed = Mathf.SmoothDamp(cur, tgt, ref v, 1f / sp, Mathf.Infinity, Time.deltaTime);
                    SetAxis(ref currentEuler[i], axis, smoothed);
                    SetAxis(ref velEuler[i], axis, v);
                }
                bones[i].localRotation = Quaternion.Euler(
                    bindEuler[i].x + currentEuler[i].x,
                    bindEuler[i].y + currentEuler[i].y,
                    bindEuler[i].z + currentEuler[i].z);
            }
        }

        static void SetAxis(ref Vector3 v, int axis, float value)
        {
            if (axis == 0) v.x = value;
            else if (axis == 1) v.y = value;
            else v.z = value;
        }

        /// <summary>给肚皮一个弹性冲量：落地/跳跃/冲刺时调用。</summary>
        public void ImpulseBelly(float force)
        {
            bellyVel += force;
        }

        void UpdateBelly(float dt)
        {
            if (belly == null) return;

            // 弹簧阻尼：-kx - cv
            float accel = -bellySquash * BELLY_STIFFNESS - bellyVel * BELLY_DAMPING;
            bellyVel += accel * dt;
            bellySquash += bellyVel * dt;

            // 限制最大形变，防止穿模
            bellySquash = Mathf.Clamp(bellySquash, -0.35f, 0.45f);

            // 体积守恒：Y 拉伸 ↔ XZ 压缩
            float sxz = 1f - bellySquash * 0.55f;
            float sy = 1f + bellySquash;
            belly.localScale = new Vector3(
                bellyBaseScale.x * sxz,
                bellyBaseScale.y * sy,
                bellyBaseScale.z * sxz);

            // 肚皮随抖动上下轻微位移
            belly.localPosition = bellyBasePos + new Vector3(0f, bellySquash * 0.07f, 0f);
        }

        void UpdateExtras()
        {
            UpdateBelly(Time.deltaTime);

            if (hips != null)
            {
                float bob = player != null && player.IsGrounded
                    ? Mathf.Sin(Time.time * bobFrequency) * bobAmount * Mathf.Clamp01(player.PlanarSpeed / 6.5f)
                    : 0f;
                float targetSquash = squash;
                squash = Mathf.Lerp(squash, 0f, Time.deltaTime * 8f);
                float sy = 1f + targetSquash;
                float sxz = 1f - targetSquash * 0.55f;
                hips.localScale = new Vector3(sxz, sy, sxz);
                hips.localPosition = hipsBasePos + new Vector3(0f, bob - targetSquash * 0.12f, 0f);
            }

            // 眨眼
            blinkTimer -= Time.deltaTime;
            float eyeScale = 1f;
            if (blinkTimer <= 0f)
            {
                float k = Mathf.Clamp01(-blinkTimer / 0.12f);
                eyeScale = 1f - Mathf.Sin(k * Mathf.PI) * 0.92f;
                if (k >= 1f) blinkTimer = UnityEngine.Random.Range(1.8f, blinkInterval);
            }
            if (eyeL != null) eyeL.localScale = new Vector3(1f, eyeScale, 1f);
            if (eyeR != null) eyeR.localScale = new Vector3(1f, eyeScale, 1f);
        }

        void Add(string boneName, Vector3 euler)
        {
            int idx = IndexOf(boneName);
            if (idx >= 0) targetEuler[idx] += euler;
        }

        int IndexOf(string name)
        {
            for (int i = 0; i < BoneNames.Length; i++) if (BoneNames[i] == name) return i;
            return -1;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
