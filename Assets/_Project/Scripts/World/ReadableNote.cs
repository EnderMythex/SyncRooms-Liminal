using SyncRooms.Core;
using SyncRooms.Player;
using UnityEngine;

namespace SyncRooms.World
{
    /// <summary>A note pinned in the world that can be read in place (not picked up).</summary>
    public class ReadableNote : MonoBehaviour, IInteractable
    {
        public string title = "Note";
        [TextArea(4, 20)] public string text;

        public string Prompt => "[E] Lire";
        public bool CanInteract => true;

        public void Interact(PlayerInteractor player)
        {
            Sfx.Play2D("SFX/sfx_paper", 0.7f);
            GameManager.Instance.UI.ShowNote(title, text);
        }
    }
}
