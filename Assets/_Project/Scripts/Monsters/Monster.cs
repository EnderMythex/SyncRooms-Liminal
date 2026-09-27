using SyncRooms.Core;
using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.Monsters
{
    public enum MonsterState { Wander, Investigate, Chase, Retreat }

    /// <summary>
    /// Generic entity AI: wanders, notices the player by sight or noise (sprinting), chases by following the
    /// player's breadcrumb trail around corners, hits (sanity damage) then retreats briefly.
    /// Players inside a SafeZone are ignored.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Monster : MonoBehaviour
    {
        [Header("Stats")]
        public float wanderSpeed = 1.2f;
        public float chaseSpeed = 4.2f;
        public float sightRange = 22f;
        [Range(0, 180)] public float sightAngle = 75f;
        public float hearingRange = 14f;
        public float attackRange = 1.5f;
        public float damage = 30f;
        public bool flying;
        public float hoverHeight = 1.7f;
        public bool onlyInDarkness;          // smilers: fade away when lit by the flashlight
        [Header("Audio (Resources/Audio paths)")]
        public string idleSound;
        public string chaseSound;
        public string attackSound;

        public MonsterState State { get; private set; } = MonsterState.Wander;
        public float Speed01 { get; private set; }

        CharacterController _cc;
        Transform _player;
        FirstPersonController _fpc;
        Vector3 _goal;
        float _stateTimer, _idleTimer, _attackCd, _vy, _lostTimer;
        Vector3 _lastPos;
        AudioSource _voice;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            gameObject.layer = 9;
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.spatialBlend = 1f;
            _voice.rolloffMode = AudioRolloffMode.Linear;
            _voice.maxDistance = 30f;
            _voice.minDistance = 2f;
            _voice.dopplerLevel = 0;
            _idleTimer = Random.Range(4f, 12f);
            PickWanderGoal();
        }

        void Locate()
        {
            if (_player) return;
            _fpc = FindAnyObjectByType<FirstPersonController>();
            _player = _fpc ? _fpc.transform : null;
        }

        bool PlayerTargetable => _player && !SafeZone.PlayerInside && GameManager.Instance && !GameManager.Instance.InCutscene;

        Vector3 Eye => transform.position + Vector3.up * (flying ? 0f : 0.9f);

        bool CanSee()
        {
            if (!PlayerTargetable) return false;
            Vector3 head = _player.position + Vector3.up * 1.5f;
            Vector3 d = head - Eye;
            if (d.magnitude > sightRange) return false;
            if (Vector3.Angle(transform.forward, d) > sightAngle && d.magnitude > 3f) return false;
            return !Physics.Raycast(Eye, d.normalized, d.magnitude, ~((1 << 2) | (1 << 9)), QueryTriggerInteraction.Ignore);
        }

        bool CanHear()
        {
            if (!PlayerTargetable || _fpc == null) return false;
            float r = _fpc.IsSprinting ? hearingRange : _fpc.IsCrouching ? 0f : hearingRange * 0.35f;
            return (_player.position - transform.position).sqrMagnitude < r * r;
        }

        void PickWanderGoal()
        {
            for (int i = 0; i < 8; i++)
            {
                var dir = Quaternion.Euler(0, Random.Range(0, 360f), 0) * Vector3.forward;
                float dist = Random.Range(4f, 12f);
                if (!Physics.Raycast(Eye, dir, dist, ~((1 << 2) | (1 << 9)), QueryTriggerInteraction.Ignore))
                {
                    _goal = transform.position + dir * dist;
                    return;
                }
            }
            _goal = transform.position - transform.forward * 3f;
        }

        void Update()
        {
            Locate();
            float dt = Time.deltaTime;
            _stateTimer -= dt;
            _attackCd -= dt;

            if (onlyInDarkness && LitByFlashlight()) { State = MonsterState.Retreat; _stateTimer = 2f; }

            bool sees = CanSee();
            switch (State)
            {
                case MonsterState.Wander:
                    if (sees) StartChase();
                    else if (CanHear()) { State = MonsterState.Investigate; _goal = _player.position; _stateTimer = 8f; }
                    else if ((Flat(_goal - transform.position)).magnitude < 0.8f || _stateTimer < -10f) { PickWanderGoal(); _stateTimer = 0; }
                    break;
                case MonsterState.Investigate:
                    if (sees) StartChase();
                    else if (_stateTimer <= 0 || Flat(_goal - transform.position).magnitude < 1f) { State = MonsterState.Wander; PickWanderGoal(); }
                    break;
                case MonsterState.Chase:
                    if (!PlayerTargetable) { State = MonsterState.Wander; PickWanderGoal(); break; }
                    if (sees) { _goal = _player.position; _lostTimer = 0; }
                    else if (PlayerTrail.NewestVisible(Eye, 30f, out var crumb)) { _goal = crumb; _lostTimer += dt * 0.3f; }
                    else _lostTimer += dt;
                    if (_lostTimer > 6f) { State = MonsterState.Wander; PickWanderGoal(); }
                    if (Flat(_player.position - transform.position).magnitude < attackRange && Mathf.Abs(_player.position.y - transform.position.y) < 2.2f && _attackCd <= 0) Attack();
                    break;
                case MonsterState.Retreat:
                    if (_player) _goal = transform.position + Flat(transform.position - _player.position).normalized * 6f;
                    if (_stateTimer <= 0) { State = MonsterState.Wander; PickWanderGoal(); }
                    break;
            }

            if (State == MonsterState.Wander && _idleTimer > 0)
            {
                _idleTimer -= dt;
                if (_idleTimer <= 0) { Voice(idleSound, 0.6f); _idleTimer = Random.Range(8f, 20f); }
            }

            float speed = State == MonsterState.Chase ? chaseSpeed : State == MonsterState.Retreat ? chaseSpeed * 0.8f : wanderSpeed * (State == MonsterState.Investigate ? 1.6f : 1f);
            MoveTowards(_goal, speed, dt);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void StartChase()
        {
            if (State != MonsterState.Chase) Voice(chaseSound, 1f);
            State = MonsterState.Chase;
            _lostTimer = 0;
        }

        void Attack()
        {
            _attackCd = 2.5f;
            Voice(attackSound, 1f);
            GameManager.Instance.Hurt(damage);
            State = MonsterState.Retreat;
            _stateTimer = 3.5f;
        }

        void Voice(string path, float vol)
        {
            if (string.IsNullOrEmpty(path)) return;
            var c = Sfx.Random(path);
            if (c) _voice.PlayOneShot(c, vol);
        }

        bool LitByFlashlight()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.State.flashlightOn || Camera.main == null) return false;
            var cam = Camera.main.transform;
            var d = transform.position - cam.position;
            return d.magnitude < 18f && Vector3.Angle(cam.forward, d) < 22f;
        }

        void MoveTowards(Vector3 target, float speed, float dt)
        {
            Vector3 to = target - transform.position;
            Vector3 flat = Flat(to);
            Vector3 dir = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.zero;
            // simple obstacle avoidance: steer along walls
            if (dir != Vector3.zero && Physics.SphereCast(Eye, 0.25f, dir, out var hit, 1.2f, ~((1 << 2) | (1 << 9)), QueryTriggerInteraction.Ignore))
                dir = Vector3.ProjectOnPlane(dir, hit.normal).normalized;
            Vector3 vel = dir * speed;
            if (flying)
            {
                float targetY = (State == MonsterState.Chase && _player ? _player.position.y + hoverHeight : target.y + hoverHeight) + Mathf.Sin(Time.time * 2f) * 0.2f;
                vel.y = Mathf.Clamp(targetY - transform.position.y, -2f, 2f) * 2f;
            }
            else
            {
                _vy = _cc.isGrounded ? -2f : _vy - 20f * dt;
                vel.y = _vy;
            }
            if (_cc.enabled) _cc.Move(vel * dt);
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 6f);
            Speed01 = Mathf.Clamp01(Flat(transform.position - _lastPos).magnitude / dt / Mathf.Max(0.1f, chaseSpeed));
            _lastPos = transform.position;
        }
    }
}
