using SyncRooms.Core;
using SyncRooms.World;
using UnityEngine;

namespace SyncRooms.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        public Transform cameraPivot;
        public Camera playerCamera;

        [Header("Movement")]
        public float walkSpeed = 2.4f;
        public float sprintSpeed = 4.6f;
        public float crouchSpeed = 1.3f;
        public float acceleration = 10f;
        public float jumpHeight = 0.9f;
        public float gravity = -18f;
        public float standHeight = 1.8f;
        public float crouchHeight = 1.1f;
        public float eyeOffset = 0.12f;

        [Header("Stamina")]
        public float maxStamina = 100f;
        public float sprintDrain = 18f;
        public float staminaRegen = 12f;

        [Header("Head bob")]
        public float bobFrequency = 1.9f;
        public float bobAmplitude = 0.045f;

        public float Stamina { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public WaterZone CurrentWater { get; set; }
        public bool HeadUnderwater { get; private set; }
        public float Speed01 { get; private set; }
        /// <summary>When frozen a cutscene drives the transform and camera pivot.</summary>
        public bool Frozen { get; set; }
        public CharacterController Controller => _cc;
        public float EyeHeight => standHeight - eyeOffset;
        public float Yaw => _yaw;

        public void SetView(float yaw, float pitch) { _yaw = yaw; _pitch = pitch; }

        CharacterController _cc;
        Vector3 _velocity;
        float _yaw, _pitch;
        float _bobPhase;
        float _stepDistance;
        float _currentHeight;
        bool _wasGrounded;
        float _exhaustedUntil;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            Stamina = maxStamina;
            _currentHeight = standHeight;
            _yaw = transform.eulerAngles.y;
        }

        void Start()
        {
            GameManager.Instance?.RefreshCursor();
            ApplyFov();
            if (GameManager.Instance) GameManager.Instance.SettingsChanged += ApplyFov;
        }

        void OnDestroy()
        {
            if (GameManager.Instance) GameManager.Instance.SettingsChanged -= ApplyFov;
        }

        void ApplyFov()
        {
            if (playerCamera && GameManager.Instance)
            {
                playerCamera.fieldOfView = GameManager.Instance.Settings.fov;
                playerCamera.farClipPlane = RenderDistance.FarClip;
            }
        }

        /// <summary>Teleports the player (CharacterController must be disabled to move it).</summary>
        public void Teleport(Vector3 position, float yaw)
        {
            _cc.enabled = false;
            transform.position = position;
            _yaw = yaw;
            _pitch = 0;
            _velocity = Vector3.zero;
            _cc.enabled = true;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (Frozen) return;
            bool blocked = gm != null && gm.InputBlocked;
            float dt = Time.deltaTime;
            PlayerTrail.Record(transform.position);

            // ---- Look
            if (!blocked)
            {
                float sens = gm ? gm.Settings.mouseSensitivity : 2f;
                Vector2 look = Controls.Look * sens;
                _yaw += look.x;
                _pitch += (gm != null && gm.Settings.invertY ? 1 : -1) * look.y;
                _pitch = Mathf.Clamp(_pitch, -85f, 85f);
            }
            transform.rotation = Quaternion.Euler(0, _yaw, 0);

            // ---- Crouch (smooth capsule resize)
            bool wantCrouch = !blocked && Controls.Crouch;
            if (!wantCrouch && IsCrouching)
            {
                // Only stand up if there is room above.
                float r = _cc.radius * 0.95f;
                Vector3 bottom = transform.position + Vector3.up * (r + 0.05f);
                wantCrouch = Physics.SphereCast(bottom, r, Vector3.up, out _, standHeight - r * 2f, ~0, QueryTriggerInteraction.Ignore);
            }
            IsCrouching = wantCrouch;
            float targetH = IsCrouching ? crouchHeight : standHeight;
            _currentHeight = Mathf.MoveTowards(_currentHeight, targetH, dt * 4f);
            _cc.height = _currentHeight;
            _cc.center = Vector3.up * (_currentHeight * 0.5f);

            // ---- Move
            Vector2 input = blocked ? Vector2.zero : Controls.Move;
            bool canSprint = Time.time > _exhaustedUntil && Stamina > 1f && !IsCrouching && input.y > 0.1f;
            IsSprinting = !blocked && Controls.Sprint && canSprint && input.sqrMagnitude > 0.01f;

            float speed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
            if (CurrentWater != null)
                speed *= Mathf.Lerp(1f, 0.45f, CurrentWater.DepthAt(transform.position) / 1.2f);

            Vector3 wish = (transform.right * input.x + transform.forward * input.y) * speed;
            Vector3 horiz = new Vector3(_velocity.x, 0, _velocity.z);
            horiz = Vector3.MoveTowards(horiz, wish, acceleration * speed * dt);
            _velocity.x = horiz.x;
            _velocity.z = horiz.z;

            if (_cc.isGrounded)
            {
                if (_velocity.y < 0) _velocity.y = -2f;
                if (!blocked && Controls.JumpPressed && !IsCrouching && Stamina > 10f)
                {
                    _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    Stamina -= 8f;
                }
            }
            _velocity.y += gravity * dt;
            _cc.Move(_velocity * dt);

            // ---- Stamina
            if (IsSprinting) Stamina -= sprintDrain * dt;
            else Stamina += staminaRegen * (IsCrouching ? 1.4f : 1f) * dt;
            if (Stamina <= 0f) { Stamina = 0f; _exhaustedUntil = Time.time + 2.5f; Sfx.PlayAt("SFX/sfx_breath", transform.position + Vector3.up * 1.6f, 0.5f, 6f, 0.02f); }
            Stamina = Mathf.Min(Stamina, maxStamina);

            // ---- Head bob + footsteps
            float planarSpeed = new Vector3(_cc.velocity.x, 0, _cc.velocity.z).magnitude;
            Speed01 = Mathf.Clamp01(planarSpeed / sprintSpeed);
            bool bob = gm == null || gm.Settings.headBob;
            float eyeY = _currentHeight - eyeOffset;
            Vector3 bobOffset = Vector3.zero;
            if (_cc.isGrounded && planarSpeed > 0.2f)
            {
                float freq = bobFrequency * (planarSpeed / walkSpeed);
                _bobPhase += dt * freq * Mathf.PI * 2f;
                if (bob)
                {
                    float amp = bobAmplitude * Mathf.Clamp(planarSpeed / walkSpeed, 0.5f, 1.6f);
                    bobOffset = new Vector3(Mathf.Cos(_bobPhase * 0.5f) * amp * 0.6f, Mathf.Abs(Mathf.Sin(_bobPhase * 0.5f)) * amp * -1f, 0);
                }
                _stepDistance += planarSpeed * dt;
                float stride = IsSprinting ? 1.25f : IsCrouching ? 0.6f : 0.85f;
                if (_stepDistance > stride)
                {
                    _stepDistance = 0;
                    Footstep(IsCrouching ? 0.25f : IsSprinting ? 0.8f : 0.5f);
                }
            }
            if (_cc.isGrounded && !_wasGrounded && _velocity.y < -4f) Footstep(0.8f);
            _wasGrounded = _cc.isGrounded;

            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, new Vector3(0, eyeY, 0) + bobOffset, dt * 12f);
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0, 0);

            HeadUnderwater = CurrentWater != null && cameraPivot.position.y < CurrentWater.SurfaceHeight;
        }

        void Footstep(float volume)
        {
            SurfaceType surf;
            if (CurrentWater != null && CurrentWater.DepthAt(transform.position) > 0.05f) surf = SurfaceType.Water;
            else
            {
                Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var hit, 1f, ~0, QueryTriggerInteraction.Ignore);
                surf = SurfaceTag.Resolve(hit.collider);
            }
            Sfx.PlayAt(SurfaceTag.FootstepSet(surf), transform.position, volume, 18f, 0.1f);
        }
    }
}
