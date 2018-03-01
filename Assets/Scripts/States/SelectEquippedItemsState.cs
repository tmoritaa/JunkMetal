using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class SelectEquippedItemsState : CustomizationState
{
    private List<EquippedPartsItem> items = new List<EquippedPartsItem>();

    public SelectEquippedItemsState(CustomizationHandler _handler) : base(_handler) {}

    bool initialized = false;

    public override void Start() {
        int highlightItemIdx = (handler.PickedPartsItem != null) ? handler.PickedPartsItem .ItemIdx : 0;
        handler.PickedPartsItem = null;

        foreach (EquippedPartsItem item in items) {
            item.Cleanup();
            handler.EquippedPartsItemPool.ReturnObject(item.gameObject);
        }
        items.Clear();

        generateEquippedPartItems();

        items[highlightItemIdx].GetComponent<Button>().Select();

        handler.OtherPartsFrame.gameObject.SetActive(false);
    }

    public override void End() {
        foreach (EquippedPartsItem item in items) {
            item.GetComponent<Button>().interactable = false;
        }
    }

    public override void PerformUpdate() {
        if (Input.GetKeyUp(KeyCode.Joystick1Button1) || Input.GetKeyUp(KeyCode.Mouse1)) {
            handler.BackToMain();
        }
    }

    private void generateEquippedPartItems() {
        TankSchematic playerSchematic = PlayerManager.Instance.TankSchematic;

        List<PartSchematic> schematics = new List<PartSchematic> {
            playerSchematic.HullSchematic,
            playerSchematic.WheelSchematic,
            playerSchematic.TurretSchematic
        };
        schematics.AddRange(playerSchematic.WeaponSchematics);
        handler.WeaponStartIdx = 3;

        const float itemAnchorStep = 1f / 9f;
        for (int i = 0; i < schematics.Count; ++i) {
            PartSchematic schem = schematics[i];

            EquippedPartsItem item = handler.EquippedPartsItemPool.GetObject().GetComponent<EquippedPartsItem>();
            item.transform.SetParent(handler.EquippedPartsItemRoot, false);

            Vector2 anchorMin = new Vector2(0, 1 - (i + 1) * itemAnchorStep);
            Vector2 anchorMax = new Vector2(1, 1 - (i) * itemAnchorStep);

            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2();
            rect.offsetMax = new Vector2();

            item.Init(schem, i);
            item.GetComponent<Button>().onClick.AddListener(delegate { equippedItemSelected(item); });
            items.Add(item);
        }
    }

    private void equippedItemSelected(EquippedPartsItem partsItem) {
        handler.PickedPartsItem = partsItem;
        handler.GotoState(CustomizationHandler.StateType.OtherItemSelect);
    }
}