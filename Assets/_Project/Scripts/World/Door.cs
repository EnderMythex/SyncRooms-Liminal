using SyncRooms.Core;
using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>Hinged door. The pivot (this transform) must sit on the hinge axis. Optional key requirement.</summary>
    public class Door : MonoBehaviour, IInteractable
    {
        public float openAngle = 100f;
        public float speed = 2.2f;
        public string requiredKeyId;
        public bool consumeKey;
        public string lockedMessage = "C'est verrouillé.";
        public bool startsOpen;

        bool _open;
        bool _unlocked;
        float _t;
        Quaternion _closed;
        int _dir = 1;

        public string Prompt => _open ? "[E] Fermer" : IsLocked ? "[E] Verrouillé" : "[E] Ouvrir";
        public bool CanInteract => true;
        bool IsLocked => !_unlocked && !string.IsNullOrEmpty(requiredKeyId);

        void Awake()
        {
            _closed = transform.localRotation;
            _open = startsOpen;
            _t = startsOpen ? 1 : 0;
        }

        public void Interact(PlayerInteractor player)
        {
            var gm = GameManager.Instance;
            if (IsLocked)
            {
                if (gm.Inventory.Has(requiredKeyId))
                {
                    _unlocked = true;
                    if (consumeKey) gm.Inventory.Remove(requiredKeyId);
                    Sfx.PlayAt("SFX/sfx_unlock", transform.position, 0.8f);
                    gm.UI.Hud.Notify("Déverrouillé");
                }
                else
                {
                    Sfx.PlayAt("SFX/sfx_door_locked", transform.position, 0.8f);
                    gm.UI.Hud.Notify(lockedMessage);
                    return;
                }
            }
            _open = !_open;
            if (_open)
            {
                // Open away from the player.
                Vector3 toPlayer = player.transform.position - transform.position;
                _dir = Vector3.Dot(transform.parent ? transform.parent.forward : Vector3.forward, toPlayer) > 0 ? -1 : 1;
            }
            Sfx.PlayAt("SFX/sfx_door_open", transform.position, 0.7f);
        }

        void Update()
        {
            _t = Mathf.MoveTowards(_t, _open ? 1f : 0f, Time.deltaTime * speed);
            float e = _t * _t * (3f - 2f * _t);
            transform.localRotation = _closed * Quaternion.Euler(0, openAngle * e * _dir, 0);
        }
    }
}
