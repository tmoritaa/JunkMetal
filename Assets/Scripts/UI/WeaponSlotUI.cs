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
    private Text nameText;

    [SerializeField]
    private Text keyText;

    private WeaponPart part;
    private bool initialized = false;

    void Update() {
		if (initialized) {
            if (part.IsFireable) {
                reloadFill.gameObject.SetActive(false);
            } else {
                reloadFill.gameObject.SetActive(true);
                RectTransform rectTrans = reloadFill.GetComponent<RectTransform>();

                Vector2 anchorMax = rectTrans.anchorMax;
                anchorMax.x = part.CalcRatioToReloaded();
                rectTrans.anchorMax = anchorMax;
            }
        }
	}

    public void Init(WeaponPart _part) {
        if (_part == null) {
            nameText.gameObject.SetActive(false);
            keyText.gameObject.SetActive(false);
            reloadFill.gameObject.SetActive(false);
        } else {
            part = _part;
            nameText.text = part.Schematic.Name;
            keyText.text = part.GetKeycodeStringForShoot();
            initialized = true;
        }
    }
}
