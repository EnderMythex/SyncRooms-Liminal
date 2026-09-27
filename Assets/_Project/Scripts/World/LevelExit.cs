using SyncRooms.Core;
using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>Loads another level. Either interactive (elevator button, door) or a trigger volume (stairs, diving in water).</summary>
    public class LevelExit : MonoBehaviour, IInteractable
    {
        public string targetScene;
        public string prompt = "[E] Descendre";
        public bool isTrigger;
        public string requiredItemId;
        public string missingItemMessage;
        public string transitionSfx = "SFX/sfx_elevator_ding";
        [Tooltip("Ends the game: shows the ending card and returns to the main menu.")]
        public bool endGame;
        public ExitKind exitKind = ExitKind.Walk;
        [Tooltip("Where the exit animation heads to (defaults to this transform).")]
        public Transform anchor;
        bool _used;

        public string Prompt => isTrigger ? null : prompt;
        public bool CanInteract => !isTrigger;

        public void Interact(PlayerInteractor player) => TryExit();

        void OnTriggerEnter(Collider other)
        {
            if (isTrigger && other.GetComponent<FirstPersonController>()) TryExit();
        }

        void TryExit()
        {
            var gm = GameManager.Instance;
            if (!string.IsNullOrEmpty(requiredItemId) && !gm.Inventory.Has(requiredItemId))
            {
                gm.UI.Hud.Notify(missingItemMessage);
                Sfx.Play2D("SFX/sfx_door_locked", 0.6f);
                return;
            }
            if (_used) return;
            _used = true;
            if (endGame) gm.EndGame();
            else gm.ExitVia(exitKind, anchor ? anchor : transform, targetScene, transitionSfx);
        }
    }
}
