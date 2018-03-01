using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class SelectOtherItemsState : CustomizationState
{
    private List<OtherPartsItem> items = new List<OtherPartsItem>();

    public SelectOtherItemsState(CustomizationHandler _handler) : base(_handler) {
    }

    public override void Start() {
        generateOtherItems();

        handler.OtherPartsDisplaySection.gameObject.SetActive(true);
        handler.OtherPartsDisplaySection.UpdateHeaderText(handler.PickedPartsItem.Slot);
        items[0].GetComponent<Button>().Select();
    }

    public override void End() {
        foreach (OtherPartsItem item in items) {
            item.Cleanup();
            handler.OtherPartsItemPool.ReturnObject(item.gameObject);
        }
        items.Clear();
    }

    public override void PerformUpdate() {
        if (Input.GetKeyUp(KeyCode.Joystick1Button1) || Input.GetKeyUp(KeyCode.Mouse1)) {
            handler.GotoState(CustomizationHandler.StateType.EquippedItemSelect);
        }
    }

    private void generateOtherItems() {
        PartSchematic[] parts = PartsManager.Instance.GetPartsOfType(handler.PickedPartsItem.Slot.PartType);

        float itemAnchorStep = 1f / 9f;
        for (int i = 0; i < parts.Length; ++i) {
            PartSchematic schem = parts[i];

            OtherPartsItem item = handler.OtherPartsItemPool.GetObject().GetComponent<OtherPartsItem>();
            item.transform.SetParent(handler.OtherPartsItemsRoot, false);

            Vector2 anchorMin = new Vector2(0, 1 - (i + 1) * itemAnchorStep);
            Vector2 anchorMax = new Vector2(1, 1 - (i) * itemAnchorStep);

            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2();
            rect.offsetMax = new Vector2();

            item.Init(schem);
            item.GetComponent<Button>().onClick.AddListener(delegate { ownedItemSelected(item); });
            items.Add(item);
        }
    }

    private void ownedItemSelected(OtherPartsItem item) {
        handler.UpdateEquippedParts(item.Part);

        handler.GotoState(CustomizationHandler.StateType.EquippedItemSelect);
    }
}
