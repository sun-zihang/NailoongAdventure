using System;
using UnityEngine;

namespace Nailoong
{
    /// <summary>
    /// 奶龙角色控制：相机相对移动、跳跃/二段跳、翻滚冲刺、地面检测、土狼时间与输入缓冲。
    /// 只负责"运动"，攻击与技能交给 PlayerCombat，动画交给 DragonAnimator 读取本类状态。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动")]
        public float moveSpeed = 6.2f;
        public float airControl = 0.55f;
        public float acceleration = 26f;
        public float turnSpeed = 14f;

        [Header("跳跃")]
        public float jumpForce = 8.2f;
        public float doubleJumpForce = 7.0f;
        public int maxJumps = 2;
        public float coyoteTime = 0.12f;
        public float jumpBuffer = 0.14f;
        public float fallMultiplier = 2.1f;

        [Header("冲刺（咕噜冲撞）")]
        public float dashSpeed = 15f;
        public float dashDuration = 0.38f;
        public float dashCooldown = 0.75f;
        public float dashInvulnerable = 0.32f;

        [Header("地面检测")]
        public float groundCheckRadius = 0.32f;
        public float groundCheckDistance = 0.28f;
        public LayerMask groundMask = ~0;

        // 状态
        public bool IsGrounded { get; private set; }
        public bool IsDashing { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsMoving { get; private set; }
        public float PlanarSpeed { get; private set; }
        public float VerticalSpeed { get; private set; }
        public int JumpsLeft { get; private set; }
        public float DashCooldown01 => 1f - Mathf.Clamp01(dashTimer / dashCooldown);
        public bool CanAct { get; set; } = true;      // 由 PlayerCombat 在出招时置 false

        public event Action OnJump, OnDoubleJump, OnLand, OnDashStart;

        Rigidbody rb;
        CapsuleCollider col;
        Vector2 input;
        Vector3 moveDir;
        Camera cam;

        int jumps;
        float coyote, buffer, dashTimer, dashElapsed;
        bool wasGrounded;
        Vector3 dashDir;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<CapsuleCollider>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            jumps = maxJumps;
        }

        void Start()
        {
            cam = Camera.main;
            if (CameraRig.Instance != null) CameraRig.Instance.target = transform;
        }

        void Update()
        {
            ReadInput();
            CheckGround();
            HandleJump();
            HandleDash();
        }

        void FixedUpdate()
        {
            HandleMove();
            ApplyExtraGravity();
        }

        // ---------- 输入 ----------
        void ReadInput()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            input = new Vector2(h, v).normalized;
            IsSprinting = Input.GetKey(KeyCode.LeftShift) && input.magnitude > 0.1f;

            if (Input.GetButtonDown("Jump")) buffer = jumpBuffer;
            if (Input.GetKeyDown(KeyCode.LeftShift) && CanAct) TryDash();
        }

        Vector3 CameraRelative(Vector2 dir)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return new Vector3(dir.x, 0f, dir.y);
            Vector3 f = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Vector3 r = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            return (f * dir.y + r * dir.x).normalized;
        }

        // ---------- 地面 ----------
        void CheckGround()
        {
            Vector3 origin = transform.position + Vector3.up * (col.radius + 0.05f);
            IsGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out _,
                groundCheckDistance + col.radius, groundMask, QueryTriggerInteraction.Ignore);

            if (IsGrounded)
            {
                coyote = coyoteTime;
                jumps = maxJumps;
                if (!wasGrounded && rb.linearVelocity.y <= 0.1f)
                {
                    OnLand?.Invoke();
                    if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_land", 0.5f);
                    if (VFXManager.Instance != null)
                        VFXManager.Instance.Play("vfx_land", transform.position, Quaternion.identity, 0.9f);
                }
            }
            else
            {
                coyote -= Time.deltaTime;
            }
            wasGrounded = IsGrounded;
        }

        // ---------- 跳跃 ----------
        void HandleJump()
        {
            if (buffer > 0f) buffer -= Time.deltaTime;
            if (buffer <= 0f || !CanAct) return;

            bool canGroundJump = IsGrounded || coyote > 0f;
            if (canGroundJump)
            {
                DoJump(jumpForce);
                OnJump?.Invoke();
                jumps = maxJumps - 1;
                coyote = 0f;
                buffer = 0f;
            }
            else if (jumps > 0)
            {
                DoJump(doubleJumpForce);
                OnDoubleJump?.Invoke();
                jumps--;
                buffer = 0f;
                if (VFXManager.Instance != null)
                    VFXManager.Instance.Play("vfx_jump", transform.position, Quaternion.identity, 1.1f);
            }
        }

        void DoJump(float force)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y = 0f;
            rb.linearVelocity = vel;
            rb.AddForce(Vector3.up * force, ForceMode.Impulse);
            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_jump", 0.7f);
            if (VFXManager.Instance != null)
                VFXManager.Instance.Play("vfx_jump", transform.position + Vector3.down * 0.4f, Quaternion.identity, 0.85f);
        }

        // ---------- 冲刺 ----------
        void TryDash()
        {
            if (IsDashing || dashTimer > 0f || !CanAct) return;
            dashDir = input.magnitude > 0.1f ? CameraRelative(input) : transform.forward;
            dashDir.y = 0f;
            dashDir.Normalize();
            IsDashing = true;
            dashElapsed = 0f;
            dashTimer = dashCooldown;
            OnDashStart?.Invoke();

            var dmg = GetComponent<Damageable>();
            if (dmg != null) dmg.SetInvulnerable(dashInvulnerable);

            if (AudioManager.Instance != null) AudioManager.Instance.Play("sfx_dash", 0.8f);
            if (VFXManager.Instance != null) VFXManager.Instance.Play("vfx_dash", transform.position, Quaternion.LookRotation(dashDir));
        }

        void HandleDash()
        {
            if (dashTimer > 0f) dashTimer -= Time.deltaTime;
            if (!IsDashing) return;

            dashElapsed += Time.deltaTime;
            Vector3 vel = dashDir * dashSpeed;
            vel.y = rb.linearVelocity.y * 0.35f;
            rb.linearVelocity = vel;

            if (VFXManager.Instance != null && Random.value < 0.6f)
                VFXManager.Instance.Play("vfx_dash", transform.position, Quaternion.LookRotation(-dashDir), 0.7f);

            if (dashElapsed >= dashDuration) IsDashing = false;
        }

        // ---------- 移动 ----------
        void HandleMove()
        {
            if (IsDashing) return;

            moveDir = input.magnitude > 0.1f ? CameraRelative(input) : Vector3.zero;
            float targetSpeed = moveSpeed * (IsSprinting ? 1.45f : 1f);
            Vector3 desired = moveDir * targetSpeed;

            Vector3 flat = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            float control = IsGrounded ? 1f : airControl;
            Vector3 delta = desired - flat;
            delta *= control;
            float maxDelta = acceleration * Time.fixedDeltaTime * (IsGrounded ? 1f : 0.6f);
            delta = Vector3.ClampMagnitude(delta, Mathf.Max(maxDelta, 0.01f));
            rb.linearVelocity = new Vector3(flat.x + delta.x, rb.linearVelocity.y, flat.z + delta.z);

            PlanarSpeed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
            VerticalSpeed = rb.linearVelocity.y;
            IsMoving = PlanarSpeed > 0.35f;

            if (moveDir.magnitude > 0.05f)
            {
                Quaternion want = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.fixedDeltaTime * turnSpeed);
            }
        }

        void ApplyExtraGravity()
        {
            if (IsDashing) return;
            if (rb.linearVelocity.y < 0f)
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }

        public void AddImpulse(Vector3 impulse) => rb.AddForce(impulse, ForceMode.Impulse);

        public void Teleport(Vector3 pos)
        {
            rb.linearVelocity = Vector3.zero;
            transform.position = pos;
        }
    }
}
