using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class SelectOtherItemsState : CustomizationState
{
    private List<EquippedPartsItem> items = new List<EquippedPartsItem>();

    public SelectOtherItemsState(CustomizationHandler _handler) : base(_handler) {
    }

    public override void Start() {
        generateOtherItems();

        handler.OtherPartsFrame.gameObject.SetActive(true);
        items[0].GetComponent<Button>().Select();
    }

    public override void End() {
        items.ForEach(e => handler.EquippedPartsItemPool.ReturnObject(e.gameObject));
    }

    public override void PerformUpdate() {
        if (Input.GetKeyUp(KeyCode.Joystick1Button1)) {
            handler.GotoState(CustomizationHandler.StateType.EquippedItemSelect);
        }
    }

    private void generateOtherItems() {
        PartSchematic[] parts = PartsManager.Instance.GetPartsOfType(handler.PickedPartsItem.PartSchematic.GetType());

        float itemAnchorStep = 1f / 9f;
        for (int i = 0; i < parts.Length; ++i) {
            PartSchematic schem = parts[i];

            EquippedPartsItem item = handler.EquippedPartsItemPool.GetObject().GetComponent<EquippedPartsItem>();
            item.transform.SetParent(handler.OtherPartsItemsRoot, false);

            Vector2 anchorMin = new Vector2(0, 1 - (i + 1) * itemAnchorStep);
            Vector2 anchorMax = new Vector2(1, 1 - (i) * itemAnchorStep);

            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2();
            rect.offsetMax = new Vector2();

            item.Init(schem);

            items.Add(item);
        }
    }
}
