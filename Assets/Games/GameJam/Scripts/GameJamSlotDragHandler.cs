using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameJamSlotDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public bool isHotbar;
    public int slotIndex;
    public GameJamInventoryPanel panel;

    static GameObject dragIcon;
    static GameJamSlotDragHandler dragSource;
    static Canvas rootCanvas;

    public void OnBeginDrag(PointerEventData eventData)
    {
        var model = panel.Model;
        var slots = isHotbar ? model.hotbarSlots : model.mainSlots;
        if (slotIndex < 0 || slotIndex >= slots.Length) return;
        if (slots[slotIndex].IsEmpty) return;

        dragSource = this;

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        var def = GameJamItemDB.Get(slots[slotIndex].itemId);

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(rootCanvas.transform, false);
        var rect = dragIcon.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50, 50);
        var img = dragIcon.AddComponent<Image>();
        img.color = def != null ? def.iconColor : Color.gray;
        img.raycastTarget = false;
        var cg = dragIcon.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.8f;

        SetDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
            SetDragPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
        dragSource = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (dragSource == null || dragSource == this) return;

        panel.Model.MoveSlot(dragSource.isHotbar, dragSource.slotIndex,
            isHotbar, slotIndex);
        panel.RefreshAllSlots();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return;

        var model = panel.Model;
        var slots = isHotbar ? model.hotbarSlots : model.mainSlots;
        if (slotIndex < 0 || slotIndex >= slots.Length) return;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (!slots[slotIndex].IsEmpty && slots[slotIndex].count > 1)
                panel.ShowSplitDialog(isHotbar, slotIndex);
        }
        else
        {
            if (!slots[slotIndex].IsEmpty)
                panel.SelectSlot(isHotbar, slotIndex);
            else
                panel.ClearSelection();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var model = panel.Model;
        var slots = isHotbar ? model.hotbarSlots : model.mainSlots;
        if (slotIndex >= 0 && slotIndex < slots.Length && !slots[slotIndex].IsEmpty)
            panel.ShowSlotTooltip(isHotbar, slotIndex, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        panel.HideSlotTooltip();
    }

    void SetDragPosition(PointerEventData eventData)
    {
        if (rootCanvas == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform, eventData.position,
            eventData.pressEventCamera, out var localPoint);
        dragIcon.GetComponent<RectTransform>().localPosition = localPoint;
    }
}
