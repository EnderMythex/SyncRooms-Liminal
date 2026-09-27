using SyncRooms.Core;
using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>
    /// Rowable boat: [E] to board, ZQSD to row/steer, free look, [E] near a dock to get off.
    /// Bobs on the swell and stops against obstacles.
    /// </summary>
    public class Boat : MonoBehaviour, IInteractable
    {
        public Transform seat;
        public float maxSpeed = 5f, turnSpeed = 32f, waterY = 0f;
        public float dockRange = 7f;

        FirstPersonController _p;
        bool _driving;
        float _speed, _yawLook, _pitchLook, _stroke, _boardTime;

        public string Prompt => _driving ? null : "[E] Monter dans la barque";
        public bool CanInteract => !_driving;

        public void Interact(PlayerInteractor player)
        {
            _p = player.Controller;
            _p.Frozen = true;
            _p.Controller.enabled = false;
            _driving = true;
            _yawLook = 0; _pitchLook = 0; _boardTime = Time.time;
            GameManager.Instance.UI.Hud.Notify("ZQSD : ramer · Souris : regarder · E près d'un quai : descendre");
            Sfx.PlayAt("Footsteps/fs_wood", transform.position, 0.8f);
        }

        void Update()
        {
            float t = Time.time;
            var gm = GameManager.Instance;
            Vector2 input = _driving && gm && !gm.InputBlocked ? Controls.Move : Vector2.zero;
            _speed = Mathf.MoveTowards(_speed, input.y * maxSpeed, Time.deltaTime * (Mathf.Abs(input.y) > 0.1f ? 1.2f : 0.5f));
            float yaw = transform.eulerAngles.y + input.x * turnSpeed * Time.deltaTime * (0.4f + Mathf.Abs(_speed) / maxSpeed);
            Vector3 fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            // stop against docks, hulls, piles
            if (Physics.SphereCast(transform.position + Vector3.up * 0.5f, 1.0f, fwd * Mathf.Sign(_speed + 1e-4f), out _, 2.2f, ~((1 << 2) | (1 << 9)), QueryTriggerInteraction.Ignore))
                _speed = 0f;
            var pos = transform.position + fwd * _speed * Time.deltaTime;
            pos.y = waterY + Mathf.Sin(t * 0.9f) * 0.12f + Mathf.Sin(t * 0.37f + 1f) * 0.08f;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(Mathf.Sin(t * 0.8f) * 2.5f, yaw, Mathf.Sin(t * 0.6f + 2f) * 3.5f));

            if (!_driving || _p == null) return;
            if (Mathf.Abs(input.y) > 0.1f)
            {
                _stroke -= Time.deltaTime;
                if (_stroke <= 0f) { _stroke = 1.3f; Sfx.PlayAt("Footsteps/fs_water", transform.position + transform.right * 1.2f, 0.7f, 20f, 0.2f); }
            }
            if (gm && !gm.InputBlocked)
            {
                var look = Controls.Look * gm.Settings.mouseSensitivity;
                _yawLook = Mathf.Clamp(_yawLook + look.x, -150f, 150f);
                _pitchLook = Mathf.Clamp(_pitchLook - look.y, -70f, 70f);
            }
            _p.transform.SetPositionAndRotation(seat.position, Quaternion.Euler(0, transform.eulerAngles.y + _yawLook, 0));
            _p.cameraPivot.localPosition = new Vector3(0, 1.05f, 0);
            _p.cameraPivot.localRotation = Quaternion.Euler(_pitchLook, 0, Mathf.DeltaAngle(0f, transform.eulerAngles.z) * 0.5f);

            if (gm && !gm.InputBlocked && Controls.InteractPressed && Time.time - _boardTime > 0.4f)
            {
                BoatDock best = null; float bd = dockRange;
                foreach (var d in FindObjectsByType<BoatDock>(FindObjectsSortMode.None))
                {
                    float dd = Vector3.Distance(d.transform.position, transform.position);
                    if (dd < bd) { bd = dd; best = d; }
                }
                if (best == null) { gm.UI.Hud.Notify("Aucun quai à proximité. L'eau est glaciale..."); return; }
                _driving = false;
                _speed = 0;
                var ep = best.exitPoint ? best.exitPoint : best.transform;
                _p.Teleport(ep.position, ep.eulerAngles.y);
                _p.cameraPivot.localPosition = new Vector3(0, _p.EyeHeight, 0);
                _p.cameraPivot.localRotation = Quaternion.identity;
                _p.Frozen = false;
                Sfx.PlayAt("Footsteps/fs_wood", ep.position, 0.8f);
            }
        }
    }
}
