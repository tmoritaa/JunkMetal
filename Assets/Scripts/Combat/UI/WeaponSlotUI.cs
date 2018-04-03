using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour 
{
    [SerializeField]
    private Image reloadFill;

    [SerializeField]
    private RectTransform frameRect;

    [SerializeField]
    private Text nameText;

    [SerializeField]
    private Color reloadingColor;

    [SerializeField]
    private Color reloadedColor;

    private WeaponPart part;
    private bool initialized = false;

    void Update() {
		if (initialized) {
            if (part.IsFireable) {
                reloadFill.color = reloadedColor;
            } else {
                reloadFill.color = reloadingColor;
                RectTransform rectTrans = reloadFill.GetComponent<RectTransform>();

                float frameXAnchor = frameRect.anchorMax.x;

                Vector2 anchorMax = rectTrans.anchorMax;

                anchorMax.x = frameXAnchor + (1.0f - frameXAnchor) * part.CalcRatioToReloaded();
                rectTrans.anchorMax = anchorMax;
            }
        }
	}

    public void Init(WeaponPart _part) {
        if (_part == null) {
            nameText.gameObject.SetActive(false);
            reloadFill.gameObject.SetActive(false);
        } else {
            part = _part;
            nameText.text = part.Schematic.Name;
            initialized = true;
        }
    }
}
