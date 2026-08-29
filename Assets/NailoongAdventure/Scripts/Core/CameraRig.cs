using UnityEngine;

namespace Nailoong
{
    /// <summary>
    /// 第三人称跟随相机：鼠标环绕 + 障碍避让 + 冲刺推近 + 创伤式抖动 + Boss 锁定。
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        [Header("跟随")]
        public Transform target;
        public Vector3 pivotOffset = new Vector3(0f, 1.25f, 0f);

        [Header("距离与角度")]
        public float distance = 6.2f;
        public float minDistance = 2.2f;
        public float maxDistance = 8.5f;
        public float pitchMin = -25f;
        public float pitchMax = 62f;
        public float yawSpeed = 3.2f;
        public float pitchSpeed = 2.6f;

        [Header("平滑")]
        public float followLerp = 12f;
        public float rotateLerp = 14f;

        [Header("抖动")]
        public float shakeFrequency = 22f;
        public float shakeAmplitude = 0.5f;

        [Header("视野")]
        public float baseFov = 62f;
        public float sprintFov = 74f;
        public float fovLerp = 5f;

        float yaw, pitch = 14f;
        float currentDistance;
        float trauma, traumaDecay = 1f, noiseSeed;
        Camera cam;
        Transform bossTarget;
        float bossWeight;
        PlayerController playerRef;

        /// <summary>
        /// 帧率无关的阻尼系数，替代 Lerp(a, b, dt * k)。
        /// 后者在 144Hz 与 60Hz 下平滑速度不一致，会让相机手感随帧率漂移。
        /// </summary>
        static float DampT(float lambda, float dt) => 1f - Mathf.Exp(-lambda * dt);

        /// <summary>缓存玩家引用，避免 LateUpdate 每帧 GetComponent（原本每帧调用两次）。</summary>
        PlayerController Player
        {
            get
            {
                if (playerRef == null)
                {
                    playerRef = PlayerController.Instance;
                    if (playerRef == null && target != null) playerRef = target.GetComponent<PlayerController>();
                }
                return playerRef;
            }
        }

        void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            currentDistance = distance;
            noiseSeed = Random.Range(0f, 1000f);
        }

        void Start()
        {
            if (target == null)
            {
                var player = PlayerController.Instance;
                if (player != null) target = player.transform;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void LateUpdate()
        {
            if (target == null) return;
            HandleInput();
            UpdateDistance();
            ApplyTransform();
            UpdateFov();
        }

        void HandleInput()
        {
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused) return;
            yaw += Input.GetAxis("Mouse X") * yawSpeed;
            pitch -= Input.GetAxis("Mouse Y") * pitchSpeed;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }

        void UpdateDistance()
        {
            Vector3 pivot = target.position + pivotOffset;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 dir = rot * Vector3.back;

            float wanted = distance;
            var player = Player;
            if (player != null && player.IsDashing) wanted = distance * 0.85f;
            if (bossWeight > 0.1f) wanted = distance * 1.18f;

            if (Physics.SphereCast(pivot, 0.28f, dir, out var hit, wanted, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                wanted = Mathf.Max(minDistance, hit.distance - 0.25f);

            currentDistance = Mathf.Lerp(currentDistance, Mathf.Clamp(wanted, minDistance, maxDistance), DampT(followLerp, Time.deltaTime));
        }

        void ApplyTransform()
        {
            Vector3 pivot = target.position + pivotOffset;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

            bossWeight = Mathf.Lerp(bossWeight, bossTarget != null ? 1f : 0f, DampT(2.5f, Time.deltaTime));
            if (bossWeight > 0.01f && bossTarget != null)
            {
                Vector3 mid = Vector3.Lerp(target.position, bossTarget.position, 0.3f) + pivotOffset;
                pivot = Vector3.Lerp(pivot, mid, bossWeight * 0.55f);
                float wantYaw = Quaternion.LookRotation((bossTarget.position - transform.position).normalized).eulerAngles.y;
                yaw += Mathf.DeltaAngle(yaw, wantYaw) * bossWeight * DampT(2.2f, Time.deltaTime);
                rot = Quaternion.Euler(Mathf.Lerp(pitch, 9f, bossWeight * 0.5f), yaw, 0f);
            }

            Vector3 wantedPos = pivot + rot * Vector3.back * currentDistance;
            transform.position = Vector3.Lerp(transform.position, wantedPos, DampT(followLerp, Time.deltaTime));
            Quaternion look = Quaternion.LookRotation((pivot - transform.position).normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, DampT(rotateLerp, Time.deltaTime));

            ApplyShake();
        }

        void ApplyShake()
        {
            if (trauma <= 0f) return;
            trauma = Mathf.Max(0f, trauma - Time.deltaTime * traumaDecay);
            float s = trauma * trauma;
            float t = Time.time * shakeFrequency;
            float nx = (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(noiseSeed + 17f, t) - 0.5f) * 2f;
            float nz = (Mathf.PerlinNoise(noiseSeed + 41f, t) - 0.5f) * 2f;
            transform.rotation *= Quaternion.Euler(ny * s * shakeAmplitude * 18f, nx * s * shakeAmplitude * 18f, nz * s * shakeAmplitude * 10f);
        }

        void UpdateFov()
        {
            if (cam == null) return;
            var player = Player;
            float goal = baseFov;
            if (player != null && player.IsDashing) goal = sprintFov;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, goal, DampT(fovLerp, Time.deltaTime));
        }

        public void AddTrauma(float amount, float duration)
        {
            trauma = Mathf.Clamp01(trauma + amount);
            traumaDecay = duration <= 0f ? 4f : amount / duration;
        }

        public void LockBoss(Transform boss) => bossTarget = boss;
    }
}
