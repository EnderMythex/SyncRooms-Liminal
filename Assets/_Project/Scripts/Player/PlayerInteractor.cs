using SyncRooms.Core;
using UnityEngine;

namespace SyncRooms.Player
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract { get; }
        void Interact(PlayerInteractor player);
    }

    /// <summary>Looks for IInteractable under the crosshair and triggers it with E.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public Camera cam;
        public float range = 2.6f;
        public LayerMask mask = ~0;

        public FirstPersonController Controller { get; private set; }
        IInteractable _current;

        void Awake() => Controller = GetComponent<FirstPersonController>();

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            _current = null;
            if (!gm.InputBlocked && Physics.SphereCast(cam.transform.position, 0.06f, cam.transform.forward, out var hit, range, mask, QueryTriggerInteraction.Collide))
            {
                _current = hit.collider.GetComponentInParent<IInteractable>();
                if (_current != null && !_current.CanInteract) _current = null;
            }
            gm.UI.Hud.SetPrompt(_current?.Prompt);
            if (_current != null && Controls.InteractPressed) _current.Interact(this);
        }
    }
}
