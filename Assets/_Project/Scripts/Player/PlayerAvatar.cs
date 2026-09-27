using SyncRooms.Core;
using SyncRooms.Gen;
using SyncRooms.Inventory;
using UnityEngine;
using UnityEngine.Rendering;

namespace SyncRooms.Player
{
    /// <summary>
    /// First-person body: legs/torso visible when looking down (with shadows), forearms under the camera holding
    /// the selected hotbar item, the flashlight in the left hand when it is on. All procedurally animated.
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        FirstPersonController _fpc;
        Transform _hips, _legL, _legR, _lowL, _lowR, _arms, _armL, _armR, _grip, _gripL;
        Quaternion _rL, _rR, _rlL, _rlR;
        float _phase;
        GameObject _held, _heldLight;
        ItemDefinition _heldDef;
        Vector2 _sway;
        float _useAnim;
        public static PlayerAvatar Instance { get; private set; }

        void Awake() => Instance = this;

        void Start()
        {
            _fpc = GetComponent<FirstPersonController>();
            var bp = Registry.Prefab("PlayerBody");
            if (bp)
            {
                var body = Instantiate(bp, transform);
                body.transform.localPosition = new Vector3(0, 0, -0.14f);
                body.transform.localRotation = Quaternion.identity;
                Strip(body, ShadowCastingMode.On);
                AutoOrient(body.transform, t => Center(Find(t, "Torso")).y - Center(Find(t, "LegL_Low")).y);
                _hips = Find(body.transform, "Hips");
                _legL = Find(body.transform, "LegL"); _legR = Find(body.transform, "LegR");
                _lowL = Find(body.transform, "LegL_Low"); _lowR = Find(body.transform, "LegR_Low");
                if (_legL) { _rL = _legL.localRotation; _rR = _legR.localRotation; _rlL = _lowL.localRotation; _rlR = _lowR.localRotation; }
            }
            var ap = Registry.Prefab("FPArms");
            if (ap && _fpc.playerCamera)
            {
                var arms = Instantiate(ap, _fpc.playerCamera.transform);
                _arms = arms.transform;
                _arms.localPosition = Vector3.zero;
                _arms.localRotation = Quaternion.identity;
                Strip(arms, ShadowCastingMode.Off);
                // the forearms must point forward (+Z) from their elbow pivot
                AutoOrient(_arms, t => { var a = Find(t, "ArmR"); return a ? t.InverseTransformPoint(Center(a)).z - t.InverseTransformPoint(a.position).z : 0f; });
                _armR = Find(_arms, "ArmR"); _armL = Find(_arms, "ArmL");
                if (_armR) { _armR.localPosition = new Vector3(0.21f, -0.26f, 0.02f); _armR.localRotation = Quaternion.Euler(-12, -8, 0); }
                if (_armL) { _armL.localPosition = new Vector3(-0.22f, -0.27f, 0.02f); _armL.localRotation = Quaternion.Euler(-12, 10, 0); }
                _grip = new GameObject("Grip").transform;
                _grip.SetParent(_armR ? _armR : _arms, false);
                _grip.localPosition = new Vector3(0, 0.02f, 0.4f);
                _gripL = new GameObject("GripL").transform;
                _gripL.SetParent(_armL ? _armL : _arms, false);
                _gripL.localPosition = new Vector3(0, 0.02f, 0.4f);
            }
        }

        static Vector3 Center(Transform t)
        {
            if (!t) return Vector3.zero;
            var r = t.GetComponent<Renderer>();
            return r ? r.bounds.center : t.position;
        }

        /// <summary>FBX axis conventions vary: try the plausible root rotations and keep the one that scores best.</summary>
        static void AutoOrient(Transform root, System.Func<Transform, float> score)
        {
            var baseRot = root.localRotation;
            Quaternion best = baseRot; float bestScore = float.MinValue;
            foreach (var e in new[] { Vector3.zero, new Vector3(-90, 0, 0), new Vector3(90, 0, 0), new Vector3(0, 0, 90), new Vector3(0, 0, -90), new Vector3(180, 0, 0) })
            {
                root.localRotation = baseRot * Quaternion.Euler(e);
                foreach (var child in root.GetComponentsInChildren<Transform>()) { } // force transform update
                float s = score(root);
                if (s > bestScore + 0.01f) { bestScore = s; best = root.localRotation; }
            }
            root.localRotation = best;
        }

        static void Strip(GameObject g, ShadowCastingMode shadows)
        {
            foreach (var c in g.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = shadows;
        }

        static Transform Find(Transform t, string n)
        {
            if (t.name == n) return t;
            foreach (Transform c in t) { var r = Find(c, n); if (r) return r; }
            return null;
        }

        public void PlayUse() => _useAnim = 1f;

        void LateUpdate()
        {
            if (_fpc == null) return;
            var gm = GameManager.Instance;
            float spd = _fpc.Speed01;
            _phase += Time.deltaTime * Mathf.Lerp(0f, 9.5f, spd);

            // legs
            if (_legL)
            {
                float a = Mathf.Sin(_phase) * 38f * Mathf.Clamp01(spd * 2.5f);
                float crouch = _fpc.IsCrouching ? 1f : 0f;
                _legL.localRotation = _rL * Quaternion.Euler(a - crouch * 55f, 0, 0);
                _legR.localRotation = _rR * Quaternion.Euler(-a - crouch * 55f, 0, 0);
                _lowL.localRotation = _rlL * Quaternion.Euler(Mathf.Max(0, -Mathf.Cos(_phase)) * 45f * Mathf.Clamp01(spd * 2.5f) + crouch * 100f, 0, 0);
                _lowR.localRotation = _rlR * Quaternion.Euler(Mathf.Max(0, Mathf.Cos(_phase)) * 45f * Mathf.Clamp01(spd * 2.5f) + crouch * 100f, 0, 0);
                if (_hips) _hips.parent.localPosition = new Vector3(0, -crouch * 0.55f + Mathf.Abs(Mathf.Sin(_phase)) * 0.03f * spd, -0.14f);
            }

            if (_arms == null || gm == null) return;
            // held item follows the selected hotbar slot
            var inv = gm.Inventory;
            var sel = inv.Slots[inv.SelectedSlot];
            var def = sel.IsEmpty ? null : sel.item;
            if (def != _heldDef) SetHeld(def);
            bool lightOn = gm.State.flashlightOn;
            if (_armR) _armR.gameObject.SetActive(_held != null && !gm.InCutscene);
            if (_armL) _armL.gameObject.SetActive(lightOn && !gm.InCutscene);
            if (_heldLight == null && lightOn) _heldLight = HeldModel(Registry.Item(Flashlight.ItemId), _gripL);
            if (_heldLight) _heldLight.SetActive(lightOn);

            // sway & bob
            Vector2 look = gm.InputBlocked ? Vector2.zero : Controls.Look;
            _sway = Vector2.Lerp(_sway, look * 0.8f, Time.deltaTime * 8f);
            float bob = Mathf.Sin(_phase) * 0.012f * spd;
            _useAnim = Mathf.MoveTowards(_useAnim, 0f, Time.deltaTime * 1.6f);
            float lift = Mathf.Sin(_useAnim * Mathf.PI);
            _arms.localPosition = new Vector3(-_sway.x * 0.004f, bob - _sway.y * 0.004f + lift * 0.12f, lift * 0.05f);
            _arms.localRotation = Quaternion.Euler(_sway.y * 1.2f - lift * 35f, -_sway.x * 1.2f, 0);
        }

        void SetHeld(ItemDefinition def)
        {
            _heldDef = def;
            if (_held) Destroy(_held);
            _held = def ? HeldModel(def, _grip) : null;
        }

        static GameObject HeldModel(ItemDefinition def, Transform parent)
        {
            if (def == null || def.worldPrefab == null || parent == null) return null;
            var g = Instantiate(def.worldPrefab, parent);
            if (g.TryGetComponent(out Rigidbody rb)) { rb.isKinematic = true; Destroy(rb); }
            foreach (var c in g.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var w in g.GetComponentsInChildren<World.WorldItem>()) Destroy(w);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            // normalise the size so everything fits in a hand
            var rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
                float k = max > 0.22f ? 0.22f / max : 1f;
                g.transform.localScale *= k;
            }
            g.transform.localPosition = new Vector3(0, 0.01f, 0);
            g.transform.localRotation = Quaternion.Euler(0, 180, 0);
            return g;
        }
    }
}
