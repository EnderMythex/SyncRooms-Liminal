using SyncRooms.Core;
using SyncRooms.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SyncRooms.UI
{
    /// <summary>Full inventory screen: 5x4 grid with drag &amp; drop, detail panel with Use / Drop.</summary>
    public class InventoryUI : MonoBehaviour
    {
        const int Cols = 5;
        const float Cell = 104f, Gap = 10f;

        CanvasGroup _group;
        SlotView[] _slots;
        int _selected = -1;
        Image _detailIcon;
        Text _detailName, _detailDesc, _detailMeta;
        Button _useBtn, _dropBtn;
        Text _useLabel;
        Image _dragGhost;

        public bool IsOpen { get; private set; }

        public static InventoryUI Create(Transform parent)
        {
            var rt = UIFactory.Rect("Inventory", parent).Stretch();
            var ui = rt.gameObject.AddComponent<InventoryUI>();
            ui.Build(rt);
            return ui;
        }

        void Build(RectTransform root)
        {
            _group = root.gameObject.AddComponent<CanvasGroup>();
            var dim = UIFactory.Image("Dim", root, new Color(0, 0, 0, 0.72f));
            dim.rectTransform.Stretch();
            dim.raycastTarget = true;

            int rows = PlayerInventory.SlotCount / Cols;
            float gridW = Cols * Cell + (Cols - 1) * Gap, gridH = rows * Cell + (rows - 1) * Gap;
            var panel = UIFactory.Rect("Panel", root).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(gridW + 560, gridH + 150));

            var title = UIFactory.Text("Title", panel, "INVENTAIRE", 30, UIFactory.Ink, TextAnchor.UpperLeft, UIFactory.MonoBold);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(600, 40));
            var hint = UIFactory.Text("Hint", panel, "Glisser pour déplacer · Double-clic : utiliser · 1-5 / molette : objet en main · Clic gauche : utiliser · TAB : fermer", 16, UIFactory.InkDim, TextAnchor.UpperLeft);
            hint.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -44), new Vector2(gridW + 560, 24));

            var grid = UIFactory.Rect("Grid", panel).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -90), new Vector2(gridW, gridH));
            _slots = new SlotView[PlayerInventory.SlotCount];
            for (int i = 0; i < _slots.Length; i++)
            {
                int x = i % Cols, y = i / Cols;
                var bg = UIFactory.Image("Slot" + i, grid, UIFactory.SlotBg);
                bg.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(x * (Cell + Gap), -y * (Cell + Gap)), new Vector2(Cell, Cell));
                bg.raycastTarget = true;
                if (i < PlayerInventory.HotbarSize)
                {
                    var hk = UIFactory.Text("Key", bg.transform, (i + 1).ToString(), 14, UIFactory.Accent, TextAnchor.UpperLeft);
                    hk.rectTransform.Stretch(6, 2, 2, 3);
                }
                var view = bg.gameObject.AddComponent<SlotView>();
                view.Init(this, i, bg);
                _slots[i] = view;
            }

            // Detail panel
            var det = UIFactory.Image("Details", panel, new Color(1, 1, 1, 0.035f));
            det.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, -90), new Vector2(520, gridH));
            _detailIcon = UIFactory.Image("Icon", det.transform, Color.white);
            _detailIcon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(200, 200));
            _detailIcon.preserveAspect = true;
            _detailName = UIFactory.Text("Name", det.transform, "", 28, UIFactory.Ink, TextAnchor.UpperLeft, UIFactory.MonoBold);
            _detailName.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -236), new Vector2(472, 40));
            _detailMeta = UIFactory.Text("Meta", det.transform, "", 16, UIFactory.Accent, TextAnchor.UpperLeft);
            _detailMeta.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -276), new Vector2(472, 24));
            _detailDesc = UIFactory.Text("Desc", det.transform, "", 18, UIFactory.InkDim, TextAnchor.UpperLeft);
            _detailDesc.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -308), new Vector2(472, 120));

            _useBtn = UIFactory.Button("Use", det.transform, "Utiliser", () => { if (_selected >= 0) ItemActions.Use(_selected); }, 22);
            _useBtn.GetComponent<RectTransform>().Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 16), new Vector2(240, 48));
            _useLabel = _useBtn.GetComponentInChildren<Text>();
            _dropBtn = UIFactory.Button("Drop", det.transform, "Jeter", () => { if (_selected >= 0) ItemActions.Drop(_selected); }, 22);
            _dropBtn.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 16), new Vector2(200, 48));

            _dragGhost = UIFactory.Image("DragGhost", root, new Color(1, 1, 1, 0.8f));
            _dragGhost.rectTransform.sizeDelta = new Vector2(Cell * 0.8f, Cell * 0.8f);
            _dragGhost.preserveAspect = true;
            _dragGhost.enabled = false;

            SetOpen(false);
        }

        void Start() { GameManager.Instance.Inventory.Changed += Refresh; Refresh(); }
        void OnDestroy() { if (GameManager.Instance) GameManager.Instance.Inventory.Changed -= Refresh; }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            _group.alpha = open ? 1 : 0;
            _group.blocksRaycasts = open;
            _group.interactable = open;
            _dragGhost.enabled = false;
            if (open) { Refresh(); Sfx.Play2D("SFX/sfx_bag", 0.5f); }
        }

        public void Select(int i)
        {
            _selected = i;
            Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null || _slots == null) return;
            var inv = gm.Inventory;
            for (int i = 0; i < _slots.Length; i++) _slots[i].Show(inv.Slots[i], i == _selected);

            var st = _selected >= 0 ? inv.Slots[_selected] : null;
            bool has = st != null && !st.IsEmpty;
            _detailIcon.enabled = has && st.item.icon;
            if (has) _detailIcon.sprite = st.item.icon;
            _detailName.text = has ? st.item.displayName : "";
            _detailDesc.text = has ? st.item.description : "Sélectionnez un objet.";
            _detailMeta.text = has ? (st.count > 1 ? $"Quantité : {st.count}" : "") + (st.item.use == ItemUse.Flashlight ? $"   Batterie : {gm.State.battery:0}%" : "") : "";
            string label = has ? ItemActions.UseLabel(st.item) : null;
            _useBtn.gameObject.SetActive(label != null);
            if (label != null) _useLabel.text = label;
            _dropBtn.gameObject.SetActive(has && st.item.worldPrefab != null);
        }

        // ---------------- drag & drop plumbing (called by SlotView)
        int _dragFrom = -1;
        public void BeginDrag(int i)
        {
            var s = GameManager.Instance.Inventory.Slots[i];
            if (s.IsEmpty) return;
            _dragFrom = i;
            _dragGhost.sprite = s.item.icon;
            _dragGhost.enabled = s.item.icon;
            _dragGhost.transform.SetAsLastSibling();
        }
        public void Drag(Vector2 screenPos) { if (_dragFrom >= 0) _dragGhost.transform.position = screenPos; }
        public void EndDrag(int target)
        {
            if (_dragFrom >= 0 && target >= 0 && target != _dragFrom)
            {
                GameManager.Instance.Inventory.Move(_dragFrom, target);
                _selected = target;
            }
            _dragFrom = -1;
            _dragGhost.enabled = false;
            Refresh();
        }
    }

    public class SlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        InventoryUI _owner;
        int _index;
        Image _bg, _icon;
        Text _count;
        bool _hover, _selected;
        static int _dropTarget = -1;

        public void Init(InventoryUI owner, int index, Image bg)
        {
            _owner = owner; _index = index; _bg = bg;
            _icon = UIFactory.Image("Icon", bg.transform, Color.white);
            _icon.rectTransform.Stretch(10, 10, 10, 10);
            _icon.preserveAspect = true;
            _count = UIFactory.Text("Count", bg.transform, "", 18, UIFactory.Ink, TextAnchor.LowerRight, UIFactory.MonoBold);
            _count.rectTransform.Stretch(2, 4, 8, 2);
        }

        public void Show(ItemStack s, bool selected)
        {
            _selected = selected;
            _icon.enabled = !s.IsEmpty && s.item.icon;
            if (!s.IsEmpty) _icon.sprite = s.item.icon;
            _count.text = !s.IsEmpty && s.count > 1 ? "x" + s.count : "";
            Tint();
        }

        void Tint() => _bg.color = _selected ? new Color(0.91f, 0.78f, 0.42f, 0.3f) : _hover ? UIFactory.SlotHover : UIFactory.SlotBg;

        public void OnPointerClick(PointerEventData e)
        {
            _owner.Select(_index);
            if (e.clickCount >= 2) ItemActions.Use(_index);
            else if (e.button == PointerEventData.InputButton.Right) ItemActions.Use(_index);
        }
        public void OnPointerEnter(PointerEventData e) { _hover = true; Tint(); }
        public void OnPointerExit(PointerEventData e) { _hover = false; Tint(); }
        public void OnBeginDrag(PointerEventData e) { _dropTarget = -1; _owner.BeginDrag(_index); }
        public void OnDrag(PointerEventData e) => _owner.Drag(e.position);
        public void OnDrop(PointerEventData e) => _dropTarget = _index;
        public void OnEndDrag(PointerEventData e) => _owner.EndDrag(_dropTarget);
    }
}
